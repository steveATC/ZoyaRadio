using System.Buffers;
using System.Text;
using System.Text.Json;
using System.Text.RegularExpressions;

namespace ZoyaRadio.MediaProbe;

internal sealed record M13ADiagnosticIdentity(string Title, string Artist)
{
    internal string LogicalId => "m13a-" + Index.ToString("00", System.Globalization.CultureInfo.InvariantCulture);

    internal int Index { get; init; }
}

internal sealed record M13ADiagnosticItemResult(
    int Index,
    string Title,
    string Artist,
    MusicKnowledgeOutcome IdentityOutcome,
    MusicKnowledgeOutcome WikidataOutcome,
    string? ArtistEntityId,
    string? WikidataId,
    int MusicBrainzLogicalRequests,
    int WikidataLogicalRequests,
    int Attempts,
    string? StructuralDiagnostic = null);

internal sealed record M13AProviderDiagnosticResult(
    IReadOnlyList<M13ADiagnosticItemResult> Items,
    int LogicalRequests,
    int Attempts,
    bool Cancelled,
    IReadOnlyList<M13AProviderAttempt>? RequestLedger = null,
    bool IncompleteRateLimited = false,
    string? StopState = null);

internal static class M13AProviderDiagnosticMatrix
{
    internal const int MaximumMusicBrainzLogicalRequestsPerIdentity = 2;
    internal const int MaximumWikidataLogicalRequestsPerIdentity = 1;
    internal const int MaximumLogicalRequests = 24;
    internal static readonly TimeSpan MaximumExecution = TimeSpan.FromMinutes(3);

    internal static IReadOnlyList<M13ADiagnosticIdentity> Identities { get; } =
    [
        new("Wake Up", "Rage Against The Machine") { Index = 1 },
        new("Ouro De Tolo", "Raul Seixas") { Index = 2 },
        new("T.N.T.", "AC/DC") { Index = 3 },
        new("Leraine", "Secede & Kettel") { Index = 4 },
        new("Sweet Home Alabama", "Lynyrd Skynyrd") { Index = 5 },
        new("Chop Suey!", "System of a Down") { Index = 6 },
        new("Заберут меня в солдаты", "Otava Yo") { Index = 7 },
        new("Zoya Radio M13A Impossible Fixture", "Zoya Radio Nonexistent Artist") { Index = 8 },
    ];
}

internal sealed class M13AProviderDiagnostic
{
    private readonly M13AProviderRequestRunner _requests;

    internal M13AProviderDiagnostic(M13AProviderRequestRunner requests) => _requests = requests;

    internal async Task<M13AProviderDiagnosticResult> ExecuteAsync(CancellationToken cancellationToken)
    {
        var items = new List<M13ADiagnosticItemResult>();
        var requestLedger = new List<M13AProviderAttempt>();
        int logicalRequests = 0;
        int consecutiveFinalRateLimits = 0;

        foreach (M13ADiagnosticIdentity identity in M13AProviderDiagnosticMatrix.Identities)
        {
            if (cancellationToken.IsCancellationRequested) return new(items, logicalRequests, _requests.TotalAttempts, Cancelled: true, requestLedger);
            if (!M13AProviderRequestFactory.TryCreateArtist(identity.Artist, identity.LogicalId + "-artist", out M13AProviderRequest? artistRequest))
            {
                throw new InvalidOperationException("The fixed diagnostic artist request failed its own allowlist validation.");
            }

            logicalRequests++;
            M13AProviderRequestResult artist = await _requests.ExecuteAsync(artistRequest!, cancellationToken);
            requestLedger.AddRange(artist.AttemptLedger ?? []);
            if (artist.Cancelled) return new(items, logicalRequests, _requests.TotalAttempts, Cancelled: true, requestLedger);

            MusicKnowledgeIdentityResolution artistResolution = ResolveArtist(identity.Artist, artist);
            if (!artistResolution.IsAccepted)
            {
                items.Add(Item(identity, artistResolution.Outcome, MusicKnowledgeOutcome.Unknown, null, null, 1, 0, artist.Attempts, artistResolution.Outcome == MusicKnowledgeOutcome.SchemaFailure ? artistResolution.Detail : null));
                if (IsFinalRateLimited(artistResolution.Outcome, artist.Attempts) && ++consecutiveFinalRateLimits == 2)
                {
                    return new(items, logicalRequests, _requests.TotalAttempts, Cancelled: false, requestLedger, IncompleteRateLimited: true, StopState: "stopped-after-two-consecutive-final-rate-limited-identities");
                }
                if (!IsFinalRateLimited(artistResolution.Outcome, artist.Attempts)) consecutiveFinalRateLimits = 0;
                continue;
            }

            if (!M13AProviderRequestFactory.TryCreateRecording(identity.Title, identity.Artist, identity.LogicalId + "-recording", out M13AProviderRequest? recordingRequest))
            {
                throw new InvalidOperationException("The fixed diagnostic recording request failed its own allowlist validation.");
            }

            logicalRequests++;
            M13AProviderRequestResult recording = await _requests.ExecuteAsync(recordingRequest!, cancellationToken);
            requestLedger.AddRange(recording.AttemptLedger ?? []);
            if (recording.Cancelled) return new(items, logicalRequests, _requests.TotalAttempts, Cancelled: true, requestLedger);

            MusicKnowledgeIdentityResolution resolved = ResolveRecording(identity, artist, recording);
            string? qid = resolved.IsAccepted ? resolved.Identity!.LinkedWikidataId : null;
            MusicKnowledgeOutcome wikidataOutcome = MusicKnowledgeOutcome.Unknown;
            int wikidataRequests = 0;
            int attempts = artist.Attempts + recording.Attempts;

            if (resolved.IsAccepted && IsValidQid(qid)
                && M13AProviderRequestFactory.TryCreateWikidata(qid!, identity.LogicalId + "-wikidata", out M13AProviderRequest? wikidataRequest))
            {
                logicalRequests++;
                wikidataRequests = 1;
                M13AProviderRequestResult wikidata = await _requests.ExecuteAsync(wikidataRequest!, cancellationToken);
                requestLedger.AddRange(wikidata.AttemptLedger ?? []);
                if (wikidata.Cancelled) return new(items, logicalRequests, _requests.TotalAttempts, Cancelled: true, requestLedger);
                wikidataOutcome = ResolveWikidata(resolved, wikidata);
                attempts += wikidata.Attempts;
            }

            items.Add(Item(identity, resolved.Outcome, wikidataOutcome, resolved.Identity?.Artist.Id, qid, 2, wikidataRequests, attempts, resolved.Outcome == MusicKnowledgeOutcome.SchemaFailure ? resolved.Detail : null));
            if (IsFinalRateLimited(resolved.Outcome, recording.Attempts) && ++consecutiveFinalRateLimits == 2)
            {
                return new(items, logicalRequests, _requests.TotalAttempts, Cancelled: false, requestLedger, IncompleteRateLimited: true, StopState: "stopped-after-two-consecutive-final-rate-limited-identities");
            }
            if (!IsFinalRateLimited(resolved.Outcome, recording.Attempts)) consecutiveFinalRateLimits = 0;
        }

        if (logicalRequests > M13AProviderDiagnosticMatrix.MaximumLogicalRequests || _requests.TotalAttempts > M13AProviderRequestRunner.MaximumTotalAttempts)
        {
            throw new InvalidOperationException("The provider diagnostic exceeded its bounded request budget.");
        }
        return new(items, logicalRequests, _requests.TotalAttempts, Cancelled: false, requestLedger);
    }

    private static M13ADiagnosticItemResult Item(
        M13ADiagnosticIdentity identity,
        MusicKnowledgeOutcome identityOutcome,
        MusicKnowledgeOutcome wikidataOutcome,
        string? artistEntityId,
        string? wikidataId,
        int musicBrainzRequests,
        int wikidataRequests,
        int attempts,
        string? structuralDiagnostic = null) =>
        new(identity.Index, identity.Title, identity.Artist, identityOutcome, wikidataOutcome, artistEntityId, wikidataId, musicBrainzRequests, wikidataRequests, attempts, structuralDiagnostic);

    private static bool IsFinalRateLimited(MusicKnowledgeOutcome outcome, int attempts) =>
        outcome == MusicKnowledgeOutcome.RateLimited && attempts == M13AProviderRequestRunner.MaximumAttemptsPerLogicalRequest;

    private static MusicKnowledgeIdentityResolution ResolveArtist(string artist, M13AProviderRequestResult response)
    {
        if (response.Outcome != MusicKnowledgeOutcome.ConfirmedFact) return new(response.Outcome, null, "Provider response did not establish an artist identity.");
        if (!TryParseArtistSnapshot(response.Body, out MusicKnowledgeSnapshot snapshot, out MusicKnowledgeOutcome outcome, out string? structuralDiagnostic))
        {
            return new(outcome, null, structuralDiagnostic ?? "MusicBrainz artist response was not accepted schema.");
        }
        if (outcome != MusicKnowledgeOutcome.ConfirmedFact) return new(outcome, null, "MusicBrainz artist response was not accepted schema.");
        return MusicKnowledgeIdentityResolver.Resolve(new MusicKnowledgeIdentityRequest(artist, null), snapshot);
    }

    private static MusicKnowledgeIdentityResolution ResolveRecording(
        M13ADiagnosticIdentity identity,
        M13AProviderRequestResult artist,
        M13AProviderRequestResult recording)
    {
        if (recording.Outcome != MusicKnowledgeOutcome.ConfirmedFact) return new(recording.Outcome, null, "Provider response did not establish a recording identity.");
        try
        {
            using JsonDocument artistJson = JsonDocument.Parse(NormalizeArtistBody(artist.Body!));
            using JsonDocument recordingJson = JsonDocument.Parse(recording.Body!);
            JsonElement artists = artistJson.RootElement.TryGetProperty("artists", out JsonElement artistArray) ? artistArray : default;
            JsonElement recordings = recordingJson.RootElement.TryGetProperty("recordings", out JsonElement recordingArray) ? recordingArray : default;
            if (artists.ValueKind != JsonValueKind.Array || recordings.ValueKind != JsonValueKind.Array)
            {
                return new(MusicKnowledgeOutcome.SchemaFailure, null, DescribeRecordingShape(recording.Body!));
            }

            string combined = "{\"artists\":" + artists.GetRawText() + ",\"recordings\":" + recordings.GetRawText() + ",\"relationships\":"
                + ExtractRelationships(artistJson.RootElement) + "}";
            MusicKnowledgeSnapshot snapshot = MusicBrainzFixtureParser.ParseSnapshot(combined, DateTimeOffset.UtcNow, out MusicKnowledgeOutcome outcome);
            return outcome == MusicKnowledgeOutcome.ConfirmedFact
                ? MusicKnowledgeIdentityResolver.Resolve(new MusicKnowledgeIdentityRequest(identity.Artist, identity.Title), snapshot)
                : new MusicKnowledgeIdentityResolution(outcome, null, outcome == MusicKnowledgeOutcome.SchemaFailure ? DescribeRecordingShape(recording.Body!) : "Combined MusicBrainz response was not accepted schema.");
        }
        catch (JsonException)
        {
            return new(MusicKnowledgeOutcome.MalformedResponse, null, "MusicBrainz endpoint response was not valid JSON.");
        }
        catch (Exception exception) when (exception is InvalidDataException or InvalidOperationException or ArgumentException)
        {
            return new(MusicKnowledgeOutcome.SchemaFailure, null, DescribeRecordingShape(recording.Body!));
        }
    }

    private static string ExtractRelationships(JsonElement root) =>
        root.TryGetProperty("relationships", out JsonElement relationships) && relationships.ValueKind == JsonValueKind.Array
            ? relationships.GetRawText()
            : "[]";

    private static bool TryParseArtistSnapshot(string? body, out MusicKnowledgeSnapshot snapshot, out MusicKnowledgeOutcome outcome, out string? structuralDiagnostic)
    {
        snapshot = MusicKnowledgeSnapshot.Empty;
        outcome = MusicKnowledgeOutcome.SchemaFailure;
        structuralDiagnostic = null;
        if (string.IsNullOrWhiteSpace(body))
        {
            structuralDiagnostic = "$.body:empty";
            return false;
        }
        try
        {
            snapshot = MusicBrainzFixtureParser.ParseSnapshot(NormalizeArtistBody(body), DateTimeOffset.UtcNow, out outcome);
            if (outcome == MusicKnowledgeOutcome.SchemaFailure) structuralDiagnostic = DescribeArtistShape(body);
            return outcome == MusicKnowledgeOutcome.ConfirmedFact;
        }
        catch (JsonException)
        {
            outcome = MusicKnowledgeOutcome.MalformedResponse;
            structuralDiagnostic = "$.body:malformed-json";
            return false;
        }
        catch (Exception exception) when (exception is InvalidDataException or InvalidOperationException or ArgumentException)
        {
            outcome = MusicKnowledgeOutcome.SchemaFailure;
            structuralDiagnostic = DescribeArtistShape(body);
            return false;
        }
    }

    private static string NormalizeArtistBody(string body)
    {
        using JsonDocument document = JsonDocument.Parse(body);
        if (!document.RootElement.TryGetProperty("artists", out JsonElement artists) || artists.ValueKind != JsonValueKind.Array) return body;

        var normalized = new StringBuilder("{\"artists\":[");
        bool first = true;
        foreach (JsonElement artist in artists.EnumerateArray())
        {
            if (!first) normalized.Append(',');
            first = false;
            string? qid = TryExtractLinkedQid(artist);
            normalized.Append(NormalizeArtistElement(artist, qid));
        }
        normalized.Append("]");
        if (document.RootElement.TryGetProperty("recordings", out JsonElement recordings)) normalized.Append(",\"recordings\":").Append(recordings.GetRawText());
        else normalized.Append(",\"recordings\":[]");
        if (document.RootElement.TryGetProperty("relationships", out JsonElement relationships)) normalized.Append(",\"relationships\":").Append(relationships.GetRawText());
        normalized.Append('}');
        return normalized.ToString();
    }

    private static string NormalizeArtistElement(JsonElement artist, string? qid)
    {
        if (artist.ValueKind != JsonValueKind.Object) return artist.GetRawText();
        var buffer = new ArrayBufferWriter<byte>();
        using var writer = new Utf8JsonWriter(buffer);
        writer.WriteStartObject();
        bool hasWikidata = false;
        foreach (JsonProperty property in artist.EnumerateObject())
        {
            if (property.NameEquals("wikidata")) hasWikidata = true;
            if (property.NameEquals("aliases") && property.Value.ValueKind == JsonValueKind.Array)
            {
                writer.WritePropertyName("aliases");
                writer.WriteStartArray();
                foreach (JsonElement alias in property.Value.EnumerateArray()) WriteAliasWithoutNullableType(alias, writer);
                writer.WriteEndArray();
            }
            else
            {
                property.WriteTo(writer);
            }
        }
        if (qid is not null && !hasWikidata) writer.WriteString("wikidata", qid);
        writer.WriteEndObject();
        writer.Flush();
        return Encoding.UTF8.GetString(buffer.WrittenSpan);
    }

    private static void WriteAliasWithoutNullableType(JsonElement alias, Utf8JsonWriter writer)
    {
        if (alias.ValueKind != JsonValueKind.Object)
        {
            alias.WriteTo(writer);
            return;
        }
        writer.WriteStartObject();
        foreach (JsonProperty property in alias.EnumerateObject())
        {
            if (property.NameEquals("type") && property.Value.ValueKind == JsonValueKind.Null) continue;
            property.WriteTo(writer);
        }
        writer.WriteEndObject();
    }

    private static string DescribeArtistShape(string body)
    {
        try
        {
            using JsonDocument document = JsonDocument.Parse(body);
            JsonElement root = document.RootElement;
            if (root.ValueKind != JsonValueKind.Object) return "$:" + root.ValueKind;
            if (!root.TryGetProperty("artists", out JsonElement artists)) return "$.artists:missing";
            if (artists.ValueKind != JsonValueKind.Array) return "$.artists:" + artists.ValueKind;
            int index = 0;
            foreach (JsonElement artist in artists.EnumerateArray())
            {
                string prefix = "$.artists[" + index.ToString(System.Globalization.CultureInfo.InvariantCulture) + "]";
                if (artist.ValueKind != JsonValueKind.Object) return prefix + ":" + artist.ValueKind;
                if (!artist.TryGetProperty("id", out JsonElement id)) return prefix + ".id:missing";
                if (id.ValueKind != JsonValueKind.String) return prefix + ".id:" + id.ValueKind;
                if (!artist.TryGetProperty("name", out JsonElement name)) return prefix + ".name:missing";
                if (name.ValueKind != JsonValueKind.String) return prefix + ".name:" + name.ValueKind;
                if (artist.TryGetProperty("aliases", out JsonElement aliases) && aliases.ValueKind != JsonValueKind.Array) return prefix + ".aliases:" + aliases.ValueKind;
                index++;
            }
            return "$:schema-invalid";
        }
        catch (JsonException)
        {
            return "$.body:malformed-json";
        }
    }

    private static string DescribeRecordingShape(string body)
    {
        try
        {
            using JsonDocument document = JsonDocument.Parse(body);
            JsonElement root = document.RootElement;
            if (root.ValueKind != JsonValueKind.Object) return "$:" + root.ValueKind;
            if (!root.TryGetProperty("recordings", out JsonElement recordings)) return "$.recordings:missing";
            if (recordings.ValueKind != JsonValueKind.Array) return "$.recordings:" + recordings.ValueKind;
            if (recordings.GetArrayLength() == 0) return "$.recordings:empty";
            JsonElement recording = recordings[0];
            if (recording.ValueKind != JsonValueKind.Object) return "$.recordings[0]:" + recording.ValueKind;
            if (!recording.TryGetProperty("id", out JsonElement id)) return "$.recordings[0].id:missing";
            if (id.ValueKind != JsonValueKind.String) return "$.recordings[0].id:" + id.ValueKind;
            if (!recording.TryGetProperty("title", out JsonElement title)) return "$.recordings[0].title:missing";
            if (title.ValueKind != JsonValueKind.String) return "$.recordings[0].title:" + title.ValueKind;
            if (!recording.TryGetProperty("artist-credit", out JsonElement credit)) return "$.recordings[0].artist-credit:missing";
            return credit.ValueKind == JsonValueKind.Array ? "$:schema-invalid" : "$.recordings[0].artist-credit:" + credit.ValueKind;
        }
        catch (JsonException)
        {
            return "$.body:malformed-json";
        }
    }

    private static string? TryExtractLinkedQid(JsonElement artist)
    {
        if (artist.TryGetProperty("wikidata", out JsonElement direct) && direct.ValueKind == JsonValueKind.String && IsValidQid(direct.GetString())) return direct.GetString();
        if (!artist.TryGetProperty("relations", out JsonElement relations) || relations.ValueKind != JsonValueKind.Array) return null;
        foreach (JsonElement relation in relations.EnumerateArray())
        {
            if (!relation.TryGetProperty("type", out JsonElement type) || !string.Equals(type.GetString(), "wikidata", StringComparison.OrdinalIgnoreCase)) continue;
            if (!relation.TryGetProperty("url", out JsonElement url) || !url.TryGetProperty("resource", out JsonElement resource)) continue;
            if (Uri.TryCreate(resource.GetString(), UriKind.Absolute, out Uri? uri))
            {
                string candidate = uri.Segments.LastOrDefault()?.TrimEnd('/') ?? string.Empty;
                if (IsValidQid(candidate)) return candidate;
            }
        }
        return null;
    }

    private static MusicKnowledgeOutcome ResolveWikidata(MusicKnowledgeIdentityResolution resolved, M13AProviderRequestResult response)
    {
        if (response.Outcome != MusicKnowledgeOutcome.ConfirmedFact) return response.Outcome;
        return WikidataFixtureParser.ParseDescription(response.Body!, resolved, DateTimeOffset.UtcNow).Outcome;
    }

    private static bool IsValidQid(string? qid) => qid is not null && Regex.IsMatch(qid, "^Q[1-9][0-9]{0,11}$", RegexOptions.CultureInvariant);
}

internal static class M13AProviderDiagnosticArtifacts
{
    internal static string CreateEvidence(M13AProviderDiagnosticResult result)
    {
        var payload = new
        {
            artifactType = "evidence",
            category = "source-identity-diagnostic",
            applicability = "diagnostic-only; not runtime enrichment",
            incompleteRateLimited = result.IncompleteRateLimited,
            stopState = result.StopState,
            structuralDiagnostics = result.Items.Where(item => item.StructuralDiagnostic is not null).OrderBy(item => item.Index).Select(item => new
            {
                item.Index,
                outcome = item.IdentityOutcome.ToString(),
                diagnostic = item.StructuralDiagnostic,
            }),
            canonicalIdentities = result.Items.OrderBy(item => item.Index)
                .Where(item => !string.IsNullOrWhiteSpace(item.ArtistEntityId))
                .Select(item => new
                {
                    item.Index,
                    item.Title,
                    item.Artist,
                    canonicalEntityId = item.ArtistEntityId,
                    provider = "MusicBrainz",
                    directEntityUrl = "https://musicbrainz.org/artist/" + item.ArtistEntityId,
                    retrievalTimeUtc = FirstRetrieval(result, item.Index, M13AProviderRoute.MusicBrainzArtist),
                    evidenceCategory = item.IdentityOutcome.ToString(),
                    applicability = "accepted-root-artist-identity",
                    wikidataProvenance = item.WikidataId is null ? null : new
                    {
                        provider = "Wikidata",
                        stableId = item.WikidataId,
                        directEntityUrl = "https://www.wikidata.org/wiki/" + item.WikidataId,
                        retrievalTimeUtc = FirstRetrieval(result, item.Index, M13AProviderRoute.WikidataEntity),
                        evidenceCategory = item.WikidataOutcome.ToString(),
                        applicability = "linked-supplemental-provenance-only",
                    },
                }),
        };
        return Serialize(payload);
    }

    internal static string CreateLedger(M13AProviderDiagnosticResult result)
    {
        var payload = new
        {
            artifactType = "request-ledger",
            logicalRequests = result.LogicalRequests,
            attempts = result.Attempts,
            cancelled = result.Cancelled,
            incompleteRateLimited = result.IncompleteRateLimited,
            stopState = result.StopState,
            structuralDiagnostics = result.Items.Where(item => item.StructuralDiagnostic is not null).OrderBy(item => item.Index).Select(item => new
            {
                item.Index,
                outcome = item.IdentityOutcome.ToString(),
                diagnostic = item.StructuralDiagnostic,
            }),
            requests = (result.RequestLedger ?? []).OrderBy(item => item.RetrievedAtUtc).ThenBy(item => item.Attempt).Select(item => new
            {
                item.Provider,
                item.Route,
                item.SafeUrl,
                item.LogicalId,
                item.HttpStatus,
                outcome = item.Outcome.ToString(),
                item.RetrievedAtUtc,
                item.Attempt,
                item.Retried,
                item.BodyByteCount,
                item.BodySha256,
                item.ClassificationDetail,
            }),
        };
        return Serialize(payload);
    }

    internal static string CreateReport(M13AProviderDiagnosticResult result)
    {
        var builder = new StringBuilder();
        builder.AppendLine("# M13A-I2 Provider Diagnostic Report");
        builder.AppendLine();
        builder.AppendLine("This diagnostic contains only fixed metadata, categorical outcomes, stable IDs, and bounded request counts.");
        builder.AppendLine();
        builder.AppendLine("| # | Title | Artist | Identity | Wikidata | MB requests | Wikidata requests | Attempts |");
        builder.AppendLine("| --- | --- | --- | --- | --- | ---: | ---: | ---: |");
        foreach (M13ADiagnosticItemResult item in result.Items.OrderBy(item => item.Index))
        {
            builder.Append("| ").Append(item.Index).Append(" | ").Append(item.Title).Append(" | ").Append(item.Artist)
                .Append(" | ").Append(item.IdentityOutcome).Append(" | ").Append(item.WikidataOutcome)
                .Append(" | ").Append(item.MusicBrainzLogicalRequests).Append(" | ").Append(item.WikidataLogicalRequests)
                .Append(" | ").Append(item.Attempts).AppendLine(" |");
        }
        builder.AppendLine();
        builder.Append("Logical requests: ").Append(result.LogicalRequests).AppendLine();
        builder.Append("Attempts: ").Append(result.Attempts).AppendLine();
        builder.Append("Cancelled: ").Append(result.Cancelled ? "yes" : "no").AppendLine();
        builder.Append("Incomplete rate-limited stop: ").Append(result.IncompleteRateLimited ? "yes" : "no").AppendLine();
        if (!string.IsNullOrWhiteSpace(result.StopState)) builder.Append("Stop state: ").Append(result.StopState).AppendLine();
        foreach (M13ADiagnosticItemResult item in result.Items.Where(item => item.StructuralDiagnostic is not null).OrderBy(item => item.Index))
        {
            builder.Append("Structural diagnostic #").Append(item.Index).Append(": ").Append(item.StructuralDiagnostic).AppendLine();
        }
        return builder.ToString();
    }

    internal static string Write(M13AProviderDiagnosticResult result, DateTimeOffset runAtUtc)
    {
        string runId = runAtUtc.ToUniversalTime().ToString("yyyyMMddTHHmmssfffZ", System.Globalization.CultureInfo.InvariantCulture);
        string directory = Path.Combine(Environment.CurrentDirectory, "artifacts", "m13a-i2", runId);
        Directory.CreateDirectory(directory);
        File.WriteAllText(Path.Combine(directory, "evidence.json"), CreateEvidence(result), new UTF8Encoding(encoderShouldEmitUTF8Identifier: false));
        File.WriteAllText(Path.Combine(directory, "request-ledger.json"), CreateLedger(result), new UTF8Encoding(encoderShouldEmitUTF8Identifier: false));
        File.WriteAllText(Path.Combine(directory, "report.md"), CreateReport(result), new UTF8Encoding(encoderShouldEmitUTF8Identifier: false));
        return directory;
    }

    private static DateTimeOffset? FirstRetrieval(M13AProviderDiagnosticResult result, int index, M13AProviderRoute route) =>
        (result.RequestLedger ?? []).Where(item => item.Route == route.ToString() && item.LogicalId.StartsWith("m13a-" + index.ToString("00", System.Globalization.CultureInfo.InvariantCulture), StringComparison.Ordinal))
            .Select(item => (DateTimeOffset?)item.RetrievedAtUtc).FirstOrDefault();

    private static string Serialize<T>(T payload) =>
        JsonSerializer.Serialize(payload, new JsonSerializerOptions { WriteIndented = true }) + Environment.NewLine;
}

internal static class M13AProviderDiagnosticCommand
{
    internal const string DiagnosticArgument = "--m13a-i2-provider-diagnostic";
    internal const string LiveArgument = "--live";

    internal static async Task<int> RunAsync(bool live, CancellationToken cancellationToken = default)
    {
        if (!live)
        {
            Console.WriteLine("M13A-I2 provider diagnostic: fixtures-only mode; no provider request was made.");
            Console.WriteLine("RESULT: 8 fixed identities, 0 provider requests, 0 artifacts written");
            return 0;
        }

        string? contact = Environment.GetEnvironmentVariable("ZOYA_MUSIC_KNOWLEDGE_CONTACT");
        if (!M13AProviderContact.IsMeaningful(contact))
        {
            Console.Error.WriteLine("M13A-I2 provider diagnostic refused live execution: a meaningful ZOYA_MUSIC_KNOWLEDGE_CONTACT is required.");
            return 2;
        }

        using var transport = new M13AHttpProviderTransport(contact!);
        var runner = new M13AProviderRequestRunner(transport, M13ASystemClock.Instance);
        using var executionLimit = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
        executionLimit.CancelAfter(M13AProviderDiagnosticMatrix.MaximumExecution);
        M13AProviderDiagnosticResult result = await new M13AProviderDiagnostic(runner).ExecuteAsync(executionLimit.Token);
        string artifactDirectory = M13AProviderDiagnosticArtifacts.Write(result, DateTimeOffset.UtcNow);
        Console.WriteLine("M13A-I2 provider diagnostic completed: " + result.Items.Count + " fixed identities, " + result.LogicalRequests + " logical requests, " + result.Attempts + " attempts.");
        Console.WriteLine("M13A-I2 provider diagnostic artifacts: " + artifactDirectory);
        return result.Cancelled ? 1 : 0;
    }
}
