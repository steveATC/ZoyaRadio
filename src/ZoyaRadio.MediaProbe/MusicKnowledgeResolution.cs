using System.Text.Json;
using System.Text.RegularExpressions;

namespace ZoyaRadio.MediaProbe;

internal static class MusicKnowledgeText
{
    private static readonly Regex Whitespace = new("\\s+", RegexOptions.CultureInvariant);

    internal static string Normalize(string value) => Whitespace.Replace(value.Normalize().Trim(), " ").ToUpperInvariant();

    internal static bool Same(string left, string right) =>
        string.Equals(Normalize(left), Normalize(right), StringComparison.Ordinal);
}

internal static class MusicBrainzFixtureParser
{
    internal static MusicKnowledgeIdentityResolution Parse(string fixture, DateTimeOffset retrievedAtUtc)
    {
        try
        {
            MusicKnowledgeSnapshot snapshot = ParseSnapshotCore(fixture, retrievedAtUtc);
            return snapshot.Artists.Count switch
            {
                1 => new MusicKnowledgeIdentityResolution(MusicKnowledgeOutcome.ConfirmedFact,
                    new MusicKnowledgeResolvedIdentity(snapshot.Artists[0], null, snapshot.Artists[0].LinkedWikidataId),
                    "MusicBrainz fixture contains one positively identified artist."),
                0 => Failed(MusicKnowledgeOutcome.Unknown, "MusicBrainz fixture contained no artist identity."),
                _ => Failed(MusicKnowledgeOutcome.AmbiguousIdentity, "MusicBrainz fixture contains multiple artist identities and none was selected."),
            };
        }
        catch (JsonException)
        {
            return Failed(MusicKnowledgeOutcome.MalformedResponse, "MusicBrainz fixture was not valid JSON.");
        }
        catch (InvalidDataException exception)
        {
            return Failed(MusicKnowledgeOutcome.SchemaFailure, exception.Message);
        }
    }

    internal static MusicKnowledgeSnapshot ParseSnapshot(string fixture, DateTimeOffset retrievedAtUtc, out MusicKnowledgeOutcome outcome)
    {
        try
        {
            MusicKnowledgeSnapshot snapshot = ParseSnapshotCore(fixture, retrievedAtUtc);
            outcome = MusicKnowledgeOutcome.ConfirmedFact;
            return snapshot;
        }
        catch (JsonException)
        {
            outcome = MusicKnowledgeOutcome.MalformedResponse;
            return MusicKnowledgeSnapshot.Empty;
        }
        catch (InvalidDataException)
        {
            outcome = MusicKnowledgeOutcome.SchemaFailure;
            return MusicKnowledgeSnapshot.Empty;
        }
    }

    private static MusicKnowledgeSnapshot ParseSnapshotCore(string fixture, DateTimeOffset retrievedAtUtc)
    {
        if (string.IsNullOrWhiteSpace(fixture))
        {
            throw new JsonException("MusicBrainz fixture was empty.");
        }

        using JsonDocument document = JsonDocument.Parse(fixture);
        JsonElement root = document.RootElement;
        if (root.ValueKind != JsonValueKind.Object)
        {
            throw new InvalidDataException("MusicBrainz fixture root must be an object.");
        }

        List<MusicKnowledgeArtist> artists = ReadArtists(root, retrievedAtUtc);
        List<MusicKnowledgeRecording> recordings = ReadRecordings(root, artists, retrievedAtUtc);
        List<MusicKnowledgeRelationship> relationships = ReadRelationships(root, retrievedAtUtc);
        return new MusicKnowledgeSnapshot(artists, recordings, relationships);
    }

    private static List<MusicKnowledgeArtist> ReadArtists(JsonElement root, DateTimeOffset retrievedAtUtc)
    {
        if (!root.TryGetProperty("artists", out JsonElement items) || items.ValueKind != JsonValueKind.Array)
        {
            throw new InvalidDataException("MusicBrainz fixture requires an artists array.");
        }

        var artists = new List<MusicKnowledgeArtist>();
        foreach (JsonElement item in items.EnumerateArray())
        {
            RequireObject(item, "MusicBrainz artist entry");
            string id = Required(item, "id");
            string name = Required(item, "name");
            IReadOnlyList<MusicKnowledgeAlias> aliases = item.TryGetProperty("aliases", out JsonElement aliasItems)
                ? ReadAliases(aliasItems, retrievedAtUtc, id)
                : [];
            string? wikidataId = item.TryGetProperty("wikidata", out JsonElement wikidata)
                ? ValidateWikidataId(RequiredStringValue(wikidata, "wikidata"))
                : null;
            artists.Add(new MusicKnowledgeArtist(id, name, aliases, wikidataId, Provenance(retrievedAtUtc, id)));
        }

        return artists;
    }

    private static IReadOnlyList<MusicKnowledgeAlias> ReadAliases(JsonElement aliases, DateTimeOffset retrievedAtUtc, string ownerId)
    {
        if (aliases.ValueKind != JsonValueKind.Array) throw new InvalidDataException("MusicBrainz aliases must be an array.");
        var result = new List<MusicKnowledgeAlias>();
        foreach (JsonElement alias in aliases.EnumerateArray())
        {
            RequireObject(alias, "MusicBrainz alias entry");
            string type = alias.TryGetProperty("type", out JsonElement typeElement)
                ? RequiredStringValue(typeElement, "alias type")
                : string.Empty;
            result.Add(new MusicKnowledgeAlias(Required(alias, "name"), type, Provenance(retrievedAtUtc, ownerId)));
        }

        return result;
    }

    private static List<MusicKnowledgeRecording> ReadRecordings(JsonElement root, IReadOnlyList<MusicKnowledgeArtist> artists, DateTimeOffset retrievedAtUtc)
    {
        if (!root.TryGetProperty("recordings", out JsonElement items) || items.ValueKind != JsonValueKind.Array)
        {
            throw new InvalidDataException("MusicBrainz fixture requires a recordings array.");
        }

        var byId = new Dictionary<string, MusicKnowledgeArtist>(StringComparer.Ordinal);
        foreach (MusicKnowledgeArtist artist in artists)
        {
            if (!byId.TryAdd(artist.Id, artist))
            {
                throw new InvalidDataException("MusicBrainz fixture contains duplicate artist identifiers.");
            }
        }

        var recordings = new List<MusicKnowledgeRecording>();
        foreach (JsonElement item in items.EnumerateArray())
        {
            RequireObject(item, "MusicBrainz recording entry");
            string id = Required(item, "id");
            JsonElement credits = RequiredElement(item, "artist-credit", JsonValueKind.Array);
            var parts = new List<MusicKnowledgeArtistCreditPart>();
            foreach (JsonElement credit in credits.EnumerateArray())
            {
                RequireObject(credit, "MusicBrainz artist-credit entry");
                string artistId;
                string? nestedArtistName = null;
                bool hasDirectArtistId = credit.TryGetProperty("artist-id", out JsonElement artistIdElement);
                bool hasNestedArtist = credit.TryGetProperty("artist", out JsonElement artistElement);
                if (hasDirectArtistId && hasNestedArtist)
                {
                    throw new InvalidDataException("MusicBrainz artist-credit cannot represent both artist-id and nested artist.");
                }
                if (hasDirectArtistId)
                {
                    artistId = RequiredStringValue(artistIdElement, "artist-id");
                }
                else if (hasNestedArtist)
                {
                    RequireObject(artistElement, "MusicBrainz nested artist");
                    artistId = Required(artistElement, "id");
                    nestedArtistName = Required(artistElement, "name");
                    if (byId.TryGetValue(artistId, out MusicKnowledgeArtist? existing))
                    {
                        if (!string.Equals(existing.Name, nestedArtistName, StringComparison.Ordinal))
                        {
                            throw new InvalidDataException("MusicBrainz nested artist conflicts with the existing stable artist identifier.");
                        }
                    }
                }
                else
                {
                    throw new InvalidDataException("MusicBrainz artist-credit requires artist-id or nested artist.");
                }
                string creditedName = credit.TryGetProperty("name", out JsonElement creditName)
                    ? RequiredStringValue(creditName, "artist-credit name")
                    : nestedArtistName ?? byId.GetValueOrDefault(artistId)?.Name ?? throw new InvalidDataException("MusicBrainz artist-credit requires name.");
                byId.TryGetValue(artistId, out MusicKnowledgeArtist? artist);
                string joinPhrase = credit.TryGetProperty("joinphrase", out JsonElement join)
                    ? JoinPhrase(join)
                    : string.Empty;
                parts.Add(new MusicKnowledgeArtistCreditPart(creditedName, artist, joinPhrase));
            }

            IReadOnlyList<MusicKnowledgeRelease> releases = item.TryGetProperty("releases", out JsonElement releaseItems)
                ? ReadReleases(releaseItems, retrievedAtUtc)
                : [];
            recordings.Add(new MusicKnowledgeRecording(id, Required(item, "title"), new MusicKnowledgeArtistCredit(parts), releases, Provenance(retrievedAtUtc, id)));
        }

        return recordings;
    }

    private static IReadOnlyList<MusicKnowledgeRelease> ReadReleases(JsonElement releases, DateTimeOffset retrievedAtUtc)
    {
        if (releases.ValueKind != JsonValueKind.Array) throw new InvalidDataException("MusicBrainz releases must be an array.");
        var result = new List<MusicKnowledgeRelease>();
        foreach (JsonElement release in releases.EnumerateArray())
        {
            RequireObject(release, "MusicBrainz release entry");
            JsonElement group = RequiredElement(release, "release-group", JsonValueKind.Object);
            string releaseId = Required(release, "id");
            string releaseGroupId = Required(group, "id");
            var releaseGroup = new MusicKnowledgeReleaseGroup(releaseGroupId, Required(group, "title"), Optional(group, "primary-type"), Provenance(retrievedAtUtc, releaseGroupId));
            result.Add(new MusicKnowledgeRelease(releaseId, Required(release, "title"), Optional(release, "status"), releaseGroup, Provenance(retrievedAtUtc, releaseId)));
        }

        return result;
    }

    private static List<MusicKnowledgeRelationship> ReadRelationships(JsonElement root, DateTimeOffset retrievedAtUtc)
    {
        if (!root.TryGetProperty("relationships", out JsonElement items)) return [];
        if (items.ValueKind != JsonValueKind.Array) throw new InvalidDataException("MusicBrainz relationships must be an array.");
        var result = new List<MusicKnowledgeRelationship>();
        foreach (JsonElement item in items.EnumerateArray())
        {
            RequireObject(item, "MusicBrainz relationship entry");
            string sourceId = Required(item, "source-id");
            result.Add(new MusicKnowledgeRelationship(
                Required(item, "type"), sourceId, Required(item, "target-id"), Provenance(retrievedAtUtc, sourceId)));
        }

        return result;
    }

    private static string Required(JsonElement element, string name)
    {
        RequireObject(element, "MusicBrainz object");
        if (!element.TryGetProperty(name, out JsonElement value))
        {
            throw new InvalidDataException($"MusicBrainz fixture requires non-empty string {name}.");
        }
        return RequiredStringValue(value, name);
    }

    private static JsonElement RequiredElement(JsonElement element, string name, JsonValueKind kind)
    {
        RequireObject(element, "MusicBrainz object");
        if (!element.TryGetProperty(name, out JsonElement value) || value.ValueKind != kind)
        {
            throw new InvalidDataException($"MusicBrainz fixture requires {name} as {kind}.");
        }
        return value;
    }

    private static string? Optional(JsonElement element, string name) =>
        element.TryGetProperty(name, out JsonElement value) ? RequiredStringValue(value, name) : null;

    private static void RequireObject(JsonElement element, string subject)
    {
        if (element.ValueKind != JsonValueKind.Object)
        {
            throw new InvalidDataException($"{subject} must be an object.");
        }
    }

    private static string RequiredStringValue(JsonElement value, string name)
    {
        string result = StringValue(value, name);
        if (string.IsNullOrWhiteSpace(result))
        {
            throw new InvalidDataException($"MusicBrainz fixture requires non-empty string {name}.");
        }

        return result;
    }

    private static string StringValue(JsonElement value, string name)
    {
        if (value.ValueKind != JsonValueKind.String)
        {
            throw new InvalidDataException($"MusicBrainz fixture requires {name} as a string.");
        }

        return value.GetString()!;
    }

    private static string JoinPhrase(JsonElement value)
    {
        string joinPhrase = StringValue(value, "artist-credit joinphrase");
        if (joinPhrase.Length > 0 && string.IsNullOrWhiteSpace(joinPhrase))
        {
            throw new InvalidDataException("MusicBrainz fixture artist-credit joinphrase cannot be whitespace only.");
        }

        return joinPhrase;
    }

    private static string? ValidateWikidataId(string? value) =>
        value is not null && Regex.IsMatch(value, "^Q[1-9][0-9]*$", RegexOptions.CultureInvariant)
            ? value
            : throw new InvalidDataException("MusicBrainz fixture Wikidata identifier is invalid.");

    private static MusicKnowledgeProvenance Provenance(DateTimeOffset retrievedAtUtc, string sourceRecordId) =>
        new(MusicKnowledgeSource.MusicBrainz, sourceRecordId, retrievedAtUtc, "musicbrainz-fixture");

    private static MusicKnowledgeIdentityResolution Failed(MusicKnowledgeOutcome outcome, string detail) =>
        new(outcome, null, detail);
}

internal static class MusicKnowledgeIdentityResolver
{
    internal static MusicKnowledgeIdentityResolution Resolve(MusicKnowledgeIdentityRequest request, MusicKnowledgeSnapshot snapshot)
    {
        if (string.IsNullOrWhiteSpace(request.Artist))
        {
            return new MusicKnowledgeIdentityResolution(MusicKnowledgeOutcome.UnsupportedRepresentation, null, "An artist string is required for positive identity resolution.");
        }

        MusicKnowledgeArtist[] artistCandidates = snapshot.Artists.Where(artist =>
            MusicKnowledgeText.Same(artist.Name, request.Artist)
            || artist.Aliases.Any(alias => string.Equals(alias.Type, "approved", StringComparison.OrdinalIgnoreCase)
                && MusicKnowledgeText.Same(alias.Name, request.Artist))).ToArray();
        if (artistCandidates.Length == 0)
        {
            return new MusicKnowledgeIdentityResolution(MusicKnowledgeOutcome.Unknown, null, "No positively matching MusicBrainz artist is present in the fixture.");
        }
        if (artistCandidates.Length > 1)
        {
            return new MusicKnowledgeIdentityResolution(MusicKnowledgeOutcome.AmbiguousIdentity, null, "Multiple MusicBrainz artists match the requested name or approved alias.");
        }

        MusicKnowledgeArtist artist = artistCandidates[0];
        if (string.IsNullOrWhiteSpace(request.RecordingTitle))
        {
            return Confirmed(artist, null);
        }

        MusicKnowledgeRecording[] titleCandidates = snapshot.Recordings
            .Where(recording => MusicKnowledgeText.Same(recording.Title, request.RecordingTitle!))
            .ToArray();
        if (titleCandidates.Any(recording => !recording.ArtistCredit.HasStableArtistIds))
        {
            return new MusicKnowledgeIdentityResolution(MusicKnowledgeOutcome.UnsupportedRepresentation, null, "A same-title recording lacks a stable structured artist credit.");
        }

        MusicKnowledgeRecording[] matches = titleCandidates.Where(recording =>
            recording.ArtistCredit.Parts.Any(part => string.Equals(part.Artist?.Id, artist.Id, StringComparison.Ordinal))).ToArray();
        if (matches.Length == 0)
        {
            return new MusicKnowledgeIdentityResolution(MusicKnowledgeOutcome.Unknown, null, "No exact title recording positively credits the resolved artist.");
        }
        if (matches.Length > 1)
        {
            return new MusicKnowledgeIdentityResolution(MusicKnowledgeOutcome.AmbiguousIdentity, null, "Multiple exact recordings positively credit the resolved artist; edition, remaster, or live status was not guessed.");
        }

        return Confirmed(artist, matches[0]);
    }

    private static MusicKnowledgeIdentityResolution Confirmed(MusicKnowledgeArtist artist, MusicKnowledgeRecording? recording) =>
        new(MusicKnowledgeOutcome.ConfirmedFact, new MusicKnowledgeResolvedIdentity(artist, recording, artist.LinkedWikidataId), "MusicBrainz fixture identity was positively resolved by stable ID and exact normalized metadata.");
}

internal static class WikidataFixtureParser
{
    internal static MusicKnowledgeFactResolution ParseDescription(string fixture, MusicKnowledgeIdentityResolution musicBrainzIdentity, DateTimeOffset retrievedAtUtc)
    {
        if (!musicBrainzIdentity.IsAccepted || string.IsNullOrWhiteSpace(musicBrainzIdentity.Identity!.LinkedWikidataId))
        {
            return MusicKnowledgeFactResolution.Unknown("description", "unresolved");
        }

        string linkedId = musicBrainzIdentity.Identity.LinkedWikidataId;
        if (string.IsNullOrWhiteSpace(fixture))
        {
            return Failure(musicBrainzIdentity.Identity.Artist.Id, linkedId, retrievedAtUtc, MusicKnowledgeOutcome.MalformedResponse, "Wikidata fixture was empty.");
        }

        try
        {
            using JsonDocument document = JsonDocument.Parse(fixture);
            JsonElement root = document.RootElement;
            if (root.ValueKind != JsonValueKind.Object
                || !root.TryGetProperty("entities", out JsonElement entities)
                || entities.ValueKind != JsonValueKind.Object
                || !entities.TryGetProperty(linkedId, out JsonElement entity)
                || entity.ValueKind != JsonValueKind.Object)
            {
                return Failure(musicBrainzIdentity.Identity.Artist.Id, linkedId, retrievedAtUtc, MusicKnowledgeOutcome.SchemaFailure, "Wikidata fixture does not contain the accepted MusicBrainz-linked entity.");
            }

            if (!entity.TryGetProperty("descriptions", out JsonElement descriptions))
            {
                return ConfirmedAbsent(musicBrainzIdentity.Identity.Artist.Id, linkedId, retrievedAtUtc);
            }
            if (descriptions.ValueKind != JsonValueKind.Object)
            {
                return Failure(musicBrainzIdentity.Identity.Artist.Id, linkedId, retrievedAtUtc, MusicKnowledgeOutcome.SchemaFailure, "Wikidata descriptions must be an object when represented.");
            }
            if (!descriptions.TryGetProperty("en", out JsonElement english))
            {
                return ConfirmedAbsent(musicBrainzIdentity.Identity.Artist.Id, linkedId, retrievedAtUtc);
            }
            if (english.ValueKind != JsonValueKind.Object
                || !english.TryGetProperty("value", out JsonElement value)
                || value.ValueKind != JsonValueKind.String)
            {
                return Failure(musicBrainzIdentity.Identity.Artist.Id, linkedId, retrievedAtUtc, MusicKnowledgeOutcome.SchemaFailure, "Wikidata English description must have a string value when represented.");
            }

            string description = value.GetString()!;
            if (string.IsNullOrWhiteSpace(description)) return Failure(musicBrainzIdentity.Identity.Artist.Id, linkedId, retrievedAtUtc, MusicKnowledgeOutcome.SchemaFailure, "Wikidata description was empty.");
            MusicKnowledgeProvenance provenance = new(MusicKnowledgeSource.Wikidata, linkedId, retrievedAtUtc, "wikidata-fixture");
            MusicKnowledgeFact fact = new("description", musicBrainzIdentity.Identity.Artist.Id, description, provenance);
            return new MusicKnowledgeFactResolution("description", fact.EntityId, MusicKnowledgeOutcome.ConfirmedFact, [fact], [provenance], "Wikidata fixture description is available only through the accepted MusicBrainz-linked identifier.");
        }
        catch (JsonException)
        {
            return Failure(musicBrainzIdentity.Identity.Artist.Id, linkedId, retrievedAtUtc, MusicKnowledgeOutcome.MalformedResponse, "Wikidata fixture was not valid JSON.");
        }
    }

    private static MusicKnowledgeFactResolution ConfirmedAbsent(string artistId, string linkedId, DateTimeOffset retrievedAtUtc)
    {
        MusicKnowledgeProvenance provenance = new(MusicKnowledgeSource.Wikidata, linkedId, retrievedAtUtc, "wikidata-fixture");
        return new MusicKnowledgeFactResolution("description", artistId, MusicKnowledgeOutcome.ConfirmedAbsence, [], [provenance], "The accepted linked Wikidata entity confirms no English description is represented.");
    }

    private static MusicKnowledgeFactResolution Failure(
        string artistId,
        string linkedId,
        DateTimeOffset retrievedAtUtc,
        MusicKnowledgeOutcome outcome,
        string detail)
    {
        MusicKnowledgeProvenance provenance = new(MusicKnowledgeSource.Wikidata, linkedId, retrievedAtUtc, "wikidata-fixture");
        return new MusicKnowledgeFactResolution("description", artistId, outcome, [], [provenance], detail);
    }
}

internal static class MusicKnowledgeFactReconciler
{
    internal static MusicKnowledgeFactResolution Reconcile(string factType, string entityId, IEnumerable<MusicKnowledgeFact> evidence)
    {
        MusicKnowledgeFact[] facts = evidence.Where(fact =>
            string.Equals(fact.FactType, factType, StringComparison.Ordinal)
            && string.Equals(fact.EntityId, entityId, StringComparison.Ordinal)
            && !string.IsNullOrWhiteSpace(fact.Value)).ToArray();
        if (facts.Length == 0) return MusicKnowledgeFactResolution.Unknown(factType, entityId);

        IReadOnlyList<MusicKnowledgeProvenance> provenance = facts.Select(fact => fact.Provenance).ToArray();
        int distinctValues = facts.Select(fact => MusicKnowledgeText.Normalize(fact.Value)).Distinct(StringComparer.Ordinal).Count();
        return distinctValues == 1
            ? new MusicKnowledgeFactResolution(factType, entityId, MusicKnowledgeOutcome.ConfirmedFact, facts, provenance, "Independent fixture evidence agrees for this fact type.")
            : new MusicKnowledgeFactResolution(factType, entityId, MusicKnowledgeOutcome.SourceConflict, facts, provenance, "Fixture evidence conflicts; every source value and provenance was preserved.");
    }
}

internal sealed record MusicKnowledgeFixtureHttpResponse(int StatusCode, string? Body, bool TimedOut = false);

internal sealed class MusicKnowledgeFakeHttpSequence
{
    private readonly Queue<MusicKnowledgeFixtureHttpResponse> _responses;

    internal MusicKnowledgeFakeHttpSequence(IEnumerable<MusicKnowledgeFixtureHttpResponse> responses) => _responses = new(responses);

    internal int Calls { get; private set; }

    internal MusicKnowledgeFixtureHttpResponse Send()
    {
        Calls++;
        return _responses.Count > 0 ? _responses.Dequeue() : new MusicKnowledgeFixtureHttpResponse(503, null);
    }
}

internal static class MusicKnowledgeFixtureRequestRunner
{
    internal static MusicKnowledgeOutcome Execute(MusicKnowledgeFakeHttpSequence transport, MusicKnowledgeRetryPolicy retryPolicy)
    {
        for (int attempt = 1; ; attempt++)
        {
            MusicKnowledgeFixtureHttpResponse response = transport.Send();
            MusicKnowledgeOutcome outcome = response.TimedOut ? MusicKnowledgeOutcome.Timeout : response.StatusCode switch
            {
                200 when string.IsNullOrWhiteSpace(response.Body) => MusicKnowledgeOutcome.SchemaFailure,
                200 => MusicKnowledgeOutcome.ConfirmedFact,
                429 => MusicKnowledgeOutcome.RateLimited,
                >= 500 and <= 599 => MusicKnowledgeOutcome.ProviderOutage,
                _ => MusicKnowledgeOutcome.UnsupportedRepresentation,
            };
            if (!retryPolicy.ShouldRetry(outcome, attempt)) return outcome;
        }
    }
}
