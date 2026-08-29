using System.Net.Http;
using System.Net;
using System.Reflection;
using System.Text.Json;
using ZoyaRadio.MediaProbe;

internal static class Milestone13AMusicKnowledgeTests
{
    private static readonly DateTimeOffset RetrievedAt = new(2026, 8, 27, 12, 0, 0, TimeSpan.Zero);

    internal static void ExactIdentityNormalizesUnicodePunctuationWhitespaceAndCase()
    {
        MusicKnowledgeSnapshot snapshot = Parse(FoundationFixture());
        MusicKnowledgeIdentityResolution resolved = MusicKnowledgeIdentityResolver.Resolve(
            new MusicKnowledgeIdentityRequest("  zØYA   &  the   CAPS  ", "  søng,   the!  "), snapshot);

        Equal(MusicKnowledgeOutcome.ConfirmedFact, resolved.Outcome);
        Equal("artist-zoya", resolved.Identity!.Artist.Id);
        Equal("recording-song", resolved.Identity.Recording!.Id);
    }

    internal static void NestedCollaborativeCreditsAndEditionRelationshipsAreTyped()
    {
        MusicKnowledgeSnapshot snapshot = Parse(FoundationFixture());
        MusicKnowledgeRecording recording = snapshot.Recordings.Single();

        Equal(2, recording.ArtistCredit.Parts.Count);
        True(recording.ArtistCredit.HasStableArtistIds);
        Equal(" & ", recording.ArtistCredit.Parts[0].JoinPhrase);
        Equal("artist-guest", recording.ArtistCredit.Parts[1].Artist!.Id);
        Equal("Deluxe Remaster", recording.Releases.Single().Title);
        Equal("release-group-main", recording.Releases.Single().ReleaseGroup!.Id);
        Equal("Album", recording.Releases.Single().ReleaseGroup!.PrimaryType);
        Equal("wikidata", snapshot.Relationships.Single().Type);
    }

    internal static void ApprovedAliasesResolveButUnapprovedAliasesDoNot()
    {
        MusicKnowledgeSnapshot snapshot = Parse("""
            { "artists": [
              { "id": "artist-alias", "name": "Canonical Artist", "aliases": [
                { "name": "Approved Alias", "type": "approved" },
                { "name": "Missing Type" },
                { "name": "Historical Type", "type": "historical" }
              ] }
            ], "recordings": [] }
            """);
        MusicKnowledgeIdentityResolution approved = MusicKnowledgeIdentityResolver.Resolve(
            new MusicKnowledgeIdentityRequest("approved alias", null), snapshot);

        Equal(MusicKnowledgeOutcome.ConfirmedFact, approved.Outcome);
        foreach (string unapprovedAlias in new[] { "missing type", "historical type" })
        {
            Equal(MusicKnowledgeOutcome.Unknown, MusicKnowledgeIdentityResolver.Resolve(
                new MusicKnowledgeIdentityRequest(unapprovedAlias, null), snapshot).Outcome);
        }
    }

    internal static void DuplicateNamesAndSameTitleRecordingsRemainAmbiguous()
    {
        MusicKnowledgeSnapshot duplicateArtists = Parse("""
            { "artists": [
              { "id": "first", "name": "The Names" },
              { "id": "second", "name": "The Names" }
            ], "recordings": [] }
            """);
        Equal(MusicKnowledgeOutcome.AmbiguousIdentity, MusicKnowledgeIdentityResolver.Resolve(
            new MusicKnowledgeIdentityRequest("the names", null), duplicateArtists).Outcome);

        MusicKnowledgeSnapshot duplicateRecordings = Parse("""
            { "artists": [ { "id": "artist", "name": "One" } ], "recordings": [
              { "id": "studio", "title": "Same", "artist-credit": [ { "name": "One", "artist-id": "artist" } ], "releases": [] },
              { "id": "live", "title": "Same", "artist-credit": [ { "name": "One", "artist-id": "artist" } ], "releases": [] }
            ] }
            """);
        Equal(MusicKnowledgeOutcome.AmbiguousIdentity, MusicKnowledgeIdentityResolver.Resolve(
            new MusicKnowledgeIdentityRequest("One", "Same"), duplicateRecordings).Outcome);
    }

    internal static void IdentityMismatchAndUnsupportedCreditsNeverGuess()
    {
        MusicKnowledgeSnapshot snapshot = Parse(FoundationFixture());
        Equal(MusicKnowledgeOutcome.Unknown, MusicKnowledgeIdentityResolver.Resolve(
            new MusicKnowledgeIdentityRequest("Different Artist", "Søng, The!"), snapshot).Outcome);

        MusicKnowledgeArtist artist = snapshot.Artists.Single(candidate => candidate.Id == "artist-zoya");
        var unsupported = new MusicKnowledgeRecording("unsupported", "Søng, The!", new MusicKnowledgeArtistCredit([
            new MusicKnowledgeArtistCreditPart("mystery credit", null, string.Empty),
        ]), [], artist.Provenance);
        MusicKnowledgeIdentityResolution resolution = MusicKnowledgeIdentityResolver.Resolve(
            new MusicKnowledgeIdentityRequest(artist.Name, unsupported.Title), new MusicKnowledgeSnapshot([artist], [unsupported], []));
        Equal(MusicKnowledgeOutcome.UnsupportedRepresentation, resolution.Outcome);
    }

    internal static void MusicBrainzFixtureFailuresStayCategorical()
    {
        foreach (string? fixture in new string?[] { null, string.Empty, " \t\r\n ", "{ nope" })
        {
            AssertMusicBrainzOutcome(fixture, MusicKnowledgeOutcome.MalformedResponse);
        }

        foreach (string root in NonObjectJsonValues())
        {
            AssertMusicBrainzOutcome(root, MusicKnowledgeOutcome.SchemaFailure);
        }

        foreach (string fixture in MusicBrainzSchemaFailures())
        {
            AssertMusicBrainzOutcome(fixture, MusicKnowledgeOutcome.SchemaFailure);
        }

        MusicKnowledgeIdentityResolution noArtist = MusicBrainzFixtureParser.Parse(MusicBrainzFixture("[]", "[]"), RetrievedAt);
        Equal(MusicKnowledgeOutcome.Unknown, noArtist.Outcome);
        True(noArtist.Identity is null);

        MusicKnowledgeIdentityResolution multipleArtists = MusicBrainzFixtureParser.Parse(FoundationFixture(), RetrievedAt);
        Equal(MusicKnowledgeOutcome.AmbiguousIdentity, multipleArtists.Outcome);
        True(multipleArtists.Identity is null);

        TopLevelArtistsOwnIdentityCandidates();
    }

    private static void TopLevelArtistsOwnIdentityCandidates()
    {
        const string topLevelArtist = "{ \"id\": \"artist-top\", \"name\": \"Top Artist\", \"wikidata\": \"Q4242\", \"aliases\": [ { \"name\": \"Top Alias\", \"type\": \"approved\" } ] }";
        const string secondTopLevelArtist = "{ \"id\": \"artist-second\", \"name\": \"Second Artist\" }";
        const string directTopCredit = "{ \"artist-id\": \"artist-top\", \"name\": \"Top Artist\" }";
        const string directUnknownCredit = "{ \"artist-id\": \"artist-collaborator\", \"name\": \"Collaborator\" }";
        const string nestedTopCredit = "{ \"artist\": { \"id\": \"artist-top\", \"name\": \"Top Artist\" } }";
        const string nestedCollaboratorCredit = "{ \"artist\": { \"id\": \"artist-collaborator\", \"name\": \"Collaborator\" } }";
        const string nestedSameNameCollaboratorCredit = "{ \"artist\": { \"id\": \"artist-collaborator\", \"name\": \"Top Artist\" } }";

        string directSameId = MusicBrainzFixture("[ " + topLevelArtist + " ]", "[ " + RecordingWith("\"artist-credit\": [ " + directTopCredit + " ]") + " ]");
        string nestedSameId = MusicBrainzFixture("[ " + topLevelArtist + " ]", "[ " + RecordingWith("\"artist-credit\": [ " + nestedTopCredit + " ]") + " ]");
        string referenceOnlyCollaboratorRecording = RecordingWithIdAndArtistCredit("recording-first", nestedCollaboratorCredit);
        string exactCanonicalRootRecording = RecordingWithIdAndArtistCredit("recording-second", directTopCredit);
        string recordingCollaboratorThenCanonical = MusicBrainzFixture("[ " + topLevelArtist + " ]", "[ "
            + referenceOnlyCollaboratorRecording + ", " + exactCanonicalRootRecording + " ]");
        string recordingCanonicalThenCollaborator = MusicBrainzFixture("[ " + topLevelArtist + " ]", "[ "
            + exactCanonicalRootRecording + ", " + referenceOnlyCollaboratorRecording + " ]");
        string creditsCollaboratorThenCanonical = MusicBrainzFixture("[ " + topLevelArtist + " ]", "[ "
            + RecordingWith("\"artist-credit\": [ " + nestedCollaboratorCredit + ", " + nestedTopCredit + " ]") + " ]");
        string creditsCanonicalThenCollaborator = MusicBrainzFixture("[ " + topLevelArtist + " ]", "[ "
            + RecordingWith("\"artist-credit\": [ " + nestedTopCredit + ", " + nestedCollaboratorCredit + " ]") + " ]");

        IdentityOwnershipCase[] cases =
        {
            new("1. no top-level artists + no recordings", MusicBrainzFixture("[]", "[]"), null, MusicKnowledgeOutcome.Unknown, MusicKnowledgeOutcome.ConfirmedFact, 0, 0, 0, 0, 0),
            new("2. no top-level artists + direct credit ID", MusicBrainzFixture("[]", "[ " + RecordingWith("\"artist-credit\": [ " + directUnknownCredit + " ]") + " ]"), null, MusicKnowledgeOutcome.Unknown, MusicKnowledgeOutcome.ConfirmedFact, 0, 1, 0, 1, 1),
            new("3. no top-level artists + one nested credit artist", MusicBrainzFixture("[]", "[ " + RecordingWith("\"artist-credit\": [ " + nestedCollaboratorCredit + " ]") + " ]"), null, MusicKnowledgeOutcome.Unknown, MusicKnowledgeOutcome.ConfirmedFact, 0, 1, 0, 1, 1),
            new("4. no top-level artists + several nested credit artists", MusicBrainzFixture("[]", "[ " + RecordingWith("\"artist-credit\": [ " + nestedCollaboratorCredit + ", { \"artist\": { \"id\": \"artist-other\", \"name\": \"Other\" } } ]") + " ]"), null, MusicKnowledgeOutcome.Unknown, MusicKnowledgeOutcome.ConfirmedFact, 0, 1, 0, 2, 1),
            new("5. one top-level artist + direct same-ID credit", directSameId, null, MusicKnowledgeOutcome.ConfirmedFact, MusicKnowledgeOutcome.ConfirmedFact, 1, 1, 1, 0, 1, RequiresExactCanonicalCreditBinding: true),
            new("6. one top-level artist + nested same-ID/same-name credit", nestedSameId, null, MusicKnowledgeOutcome.ConfirmedFact, MusicKnowledgeOutcome.ConfirmedFact, 1, 1, 1, 0, 1, RequiresExactCanonicalCreditBinding: true),
            new("7. one top-level artist + nested different-ID collaborator", MusicBrainzFixture("[ " + topLevelArtist + " ]", "[ " + RecordingWith("\"artist-credit\": [ " + nestedCollaboratorCredit + " ]") + " ]"), null, MusicKnowledgeOutcome.ConfirmedFact, MusicKnowledgeOutcome.ConfirmedFact, 1, 1, 0, 1, 1),
            new("8. one top-level artist + direct unknown-ID collaborator", MusicBrainzFixture("[ " + topLevelArtist + " ]", "[ " + RecordingWith("\"artist-credit\": [ " + directUnknownCredit + " ]") + " ]"), null, MusicKnowledgeOutcome.ConfirmedFact, MusicKnowledgeOutcome.ConfirmedFact, 1, 1, 0, 1, 1),
            new("9. one top-level artist + nested same-ID/conflicting-name", MusicBrainzFixture("[ " + topLevelArtist + " ]", "[ " + RecordingWith("\"artist-credit\": [ { \"artist\": { \"id\": \"artist-top\", \"name\": \"Conflicting Top Artist\" } } ]") + " ]"), null, MusicKnowledgeOutcome.SchemaFailure, MusicKnowledgeOutcome.SchemaFailure, -1, -1, -1, -1, -1),
            new("10. one top-level artist + nested different-ID but same canonical name", MusicBrainzFixture("[ " + topLevelArtist + " ]", "[ " + RecordingWith("\"artist-credit\": [ " + nestedSameNameCollaboratorCredit + " ]") + " ]"), null, MusicKnowledgeOutcome.ConfirmedFact, MusicKnowledgeOutcome.ConfirmedFact, 1, 1, 0, 1, 1),
            new("11. multiple top-level artists + credit pointing to one", MusicBrainzFixture("[ " + topLevelArtist + ", " + secondTopLevelArtist + " ]", "[ " + RecordingWith("\"artist-credit\": [ " + directTopCredit + " ]") + " ]"), null, MusicKnowledgeOutcome.AmbiguousIdentity, MusicKnowledgeOutcome.ConfirmedFact, 2, 1, 1, 0, 1),
            new("12. multiple top-level artists + nested same-ID reference", MusicBrainzFixture("[ " + topLevelArtist + ", " + secondTopLevelArtist + " ]", "[ " + RecordingWith("\"artist-credit\": [ " + nestedTopCredit + " ]") + " ]"), null, MusicKnowledgeOutcome.AmbiguousIdentity, MusicKnowledgeOutcome.ConfirmedFact, 2, 1, 1, 0, 1),
            new("13. duplicate stable IDs in top-level artists", MusicBrainzFixture("[ " + topLevelArtist + ", " + topLevelArtist + " ]", "[]"), null, MusicKnowledgeOutcome.SchemaFailure, MusicKnowledgeOutcome.SchemaFailure, -1, -1, -1, -1, -1),
            new("14. nested duplicate references do not create top-level duplicates/ambiguity", MusicBrainzFixture("[ " + topLevelArtist + " ]", "[ " + RecordingWith("\"artist-credit\": [ " + nestedCollaboratorCredit + ", " + nestedCollaboratorCredit + " ]") + " ]"), null, MusicKnowledgeOutcome.ConfirmedFact, MusicKnowledgeOutcome.ConfirmedFact, 1, 1, 0, 2, 1),
            new("15. recording-order permutations do not change identity outcome", recordingCollaboratorThenCanonical, recordingCanonicalThenCollaborator, MusicKnowledgeOutcome.ConfirmedFact, MusicKnowledgeOutcome.ConfirmedFact, 1, 2, 1, 1, 2, RequiresExactlyOneArtistCreditPropertyPerRecording: true),
            new("16. credit-order permutations do not change identity outcome", creditsCollaboratorThenCanonical, creditsCanonicalThenCollaborator, MusicKnowledgeOutcome.ConfirmedFact, MusicKnowledgeOutcome.ConfirmedFact, 1, 1, 1, 1, 1),
            new("17. nested collaborator before canonical credit does not alter identity", creditsCollaboratorThenCanonical, null, MusicKnowledgeOutcome.ConfirmedFact, MusicKnowledgeOutcome.ConfirmedFact, 1, 1, 1, 1, 1),
            new("18. complete normal fixture preserves recording/release/relationship parsing and provenance", FoundationFixture(), null, MusicKnowledgeOutcome.AmbiguousIdentity, MusicKnowledgeOutcome.ConfirmedFact, 2, 1, 2, 0, 1, RelationshipCount: 1, AssertCompleteFixture: true),
        };

        foreach (IdentityOwnershipCase testCase in cases)
        {
            AssertIdentityOwnershipCase(testCase, "primary fixture", testCase.Fixture);
            if (testCase.PermutedFixture is not null) AssertIdentityOwnershipCase(testCase, "permuted fixture", testCase.PermutedFixture);
        }
    }

    private static void AssertIdentityOwnershipCase(IdentityOwnershipCase testCase, string fixtureVariant, string fixture)
    {
        if (testCase.RequiresExactlyOneArtistCreditPropertyPerRecording)
        {
            AssertExactlyOneArtistCreditPropertyPerRecording(testCase, fixtureVariant, fixture);
        }

        MusicKnowledgeIdentityResolution parserResolution = MusicBrainzFixtureParser.Parse(fixture, RetrievedAt);
        OwnershipEqual(testCase, fixtureVariant, "parser outcome", testCase.ParserOutcome, parserResolution.Outcome);
        OwnershipTrue(testCase, fixtureVariant, "parser identity presence", (parserResolution.Identity is null) == (testCase.ParserOutcome != MusicKnowledgeOutcome.ConfirmedFact));

        MusicKnowledgeSnapshot snapshot = MusicBrainzFixtureParser.ParseSnapshot(fixture, RetrievedAt, out MusicKnowledgeOutcome snapshotOutcome);
        OwnershipEqual(testCase, fixtureVariant, "snapshot outcome", testCase.SnapshotOutcome, snapshotOutcome);
        if (snapshotOutcome != MusicKnowledgeOutcome.ConfirmedFact) return;

        OwnershipEqual(testCase, fixtureVariant, "top-level artist count", testCase.TopLevelArtistCount, snapshot.Artists.Count);
        OwnershipEqual(testCase, fixtureVariant, "recording count", testCase.RecordingCount, snapshot.Recordings.Count);
        OwnershipEqual(testCase, fixtureVariant, "release count", testCase.ReleaseCount, snapshot.Recordings.Sum(recording => recording.Releases.Count));
        OwnershipEqual(testCase, fixtureVariant, "relationship count", testCase.RelationshipCount, snapshot.Relationships.Count);

        int exactCanonicalBindings = 0;
        int referenceOnlyCredits = 0;
        foreach (MusicKnowledgeArtistCreditPart credit in snapshot.Recordings.SelectMany(recording => recording.ArtistCredit.Parts))
        {
            if (credit.Artist is null)
            {
                referenceOnlyCredits++;
                continue;
            }

            MusicKnowledgeArtist canonicalArtist = snapshot.Artists.Single(artist => artist.Id == credit.Artist.Id);
            OwnershipTrue(testCase, fixtureVariant, "canonical credit object identity", ReferenceEquals(canonicalArtist, credit.Artist));
            exactCanonicalBindings++;
        }

        OwnershipEqual(testCase, fixtureVariant, "exact canonical binding count", testCase.ExactCanonicalBindingCount, exactCanonicalBindings);
        OwnershipEqual(testCase, fixtureVariant, "reference-only credit count", testCase.ReferenceOnlyCreditCount, referenceOnlyCredits);

        if (testCase.RequiresExactCanonicalCreditBinding)
        {
            MusicKnowledgeArtist topLevelArtist = snapshot.Artists.Single();
            MusicKnowledgeArtist creditedArtist = snapshot.Recordings.Single().ArtistCredit.Parts.Single().Artist!;
            OwnershipTrue(testCase, fixtureVariant, "required direct canonical credit object identity", ReferenceEquals(topLevelArtist, creditedArtist));
        }

        if (testCase.TopLevelArtistCount == 1)
        {
            MusicKnowledgeArtist topLevelArtist = snapshot.Artists.Single();
            MusicKnowledgeIdentityResolution resolved = MusicKnowledgeIdentityResolver.Resolve(
                new MusicKnowledgeIdentityRequest(topLevelArtist.Name, null), snapshot);
            OwnershipEqual(testCase, fixtureVariant, "sole-root resolution outcome", MusicKnowledgeOutcome.ConfirmedFact, resolved.Outcome);
            OwnershipTrue(testCase, fixtureVariant, "sole-root resolution object identity", ReferenceEquals(topLevelArtist, resolved.Identity!.Artist));
        }

        if (!testCase.AssertCompleteFixture) return;

        MusicKnowledgeRecording completeRecording = snapshot.Recordings.Single();
        OwnershipEqual(testCase, fixtureVariant, "recording provenance", "recording-song", completeRecording.Provenance.SourceRecordId);
        OwnershipEqual(testCase, fixtureVariant, "release provenance", "release-deluxe", completeRecording.Releases.Single().Provenance.SourceRecordId);
        OwnershipEqual(testCase, fixtureVariant, "release-group provenance", "release-group-main", completeRecording.Releases.Single().ReleaseGroup!.Provenance.SourceRecordId);
        OwnershipEqual(testCase, fixtureVariant, "relationship provenance", "artist-zoya", snapshot.Relationships.Single().Provenance.SourceRecordId);
    }

    private static void AssertExactlyOneArtistCreditPropertyPerRecording(IdentityOwnershipCase testCase, string fixtureVariant, string fixture)
    {
        using JsonDocument document = JsonDocument.Parse(fixture);
        foreach (JsonElement recording in document.RootElement.GetProperty("recordings").EnumerateArray())
        {
            string recordingId = recording.GetProperty("id").GetString() ?? "<missing id>";
            OwnershipEqual(testCase, fixtureVariant, $"recording '{recordingId}' artist-credit property count", 1,
                recording.EnumerateObject().Count(property => property.NameEquals("artist-credit")));
        }
    }

    private static void OwnershipEqual<T>(IdentityOwnershipCase testCase, string fixtureVariant, string dimension, T expected, T actual)
    {
        if (!EqualityComparer<T>.Default.Equals(expected, actual))
        {
            throw new InvalidOperationException($"{testCase.Name} ({fixtureVariant}) {dimension}: Expected '{expected}', got '{actual}'.");
        }
    }

    private static void OwnershipTrue(IdentityOwnershipCase testCase, string fixtureVariant, string dimension, bool value)
    {
        if (!value) throw new InvalidOperationException($"{testCase.Name} ({fixtureVariant}) {dimension}: Expected true.");
    }

    private sealed record IdentityOwnershipCase(
        string Name,
        string Fixture,
        string? PermutedFixture,
        MusicKnowledgeOutcome ParserOutcome,
        MusicKnowledgeOutcome SnapshotOutcome,
        int TopLevelArtistCount,
        int RecordingCount,
        int ExactCanonicalBindingCount,
        int ReferenceOnlyCreditCount,
        int ReleaseCount,
        int RelationshipCount = 0,
        bool RequiresExactCanonicalCreditBinding = false,
        bool AssertCompleteFixture = false,
        bool RequiresExactlyOneArtistCreditPropertyPerRecording = false);

    internal static void WikidataIsReachableOnlyThroughAcceptedMusicBrainzLink()
    {
        MusicKnowledgeIdentityResolution accepted = MusicKnowledgeIdentityResolver.Resolve(
            new MusicKnowledgeIdentityRequest("ZØYA & The CAPS", null), Parse(FoundationFixture()));
        string fixture = """{ "entities": { "Q4242": { "descriptions": { "en": { "value": "fixture artist" } } } } }""";
        MusicKnowledgeFactResolution fact = WikidataFixtureParser.ParseDescription(fixture, accepted, RetrievedAt);
        Equal(MusicKnowledgeOutcome.ConfirmedFact, fact.Outcome);
        Equal("artist-zoya", fact.EntityId);
        Equal("fixture artist", fact.Facts.Single().Value);
        Equal(MusicKnowledgeSource.Wikidata, fact.Provenance.Single().Source);
        Equal("Q4242", fact.Provenance.Single().SourceRecordId);
        Equal(RetrievedAt, fact.Provenance.Single().RetrievedAtUtc);
        Equal("wikidata-fixture", fact.Provenance.Single().FixtureName);
        Equal("artist-zoya", fact.Facts.Single().EntityId);
        Equal("Q4242", fact.Facts.Single().Provenance.SourceRecordId);

        MusicKnowledgeFactResolution blocked = WikidataFixtureParser.ParseDescription(fixture,
            new MusicKnowledgeIdentityResolution(MusicKnowledgeOutcome.AmbiguousIdentity, null, "ambiguous"), RetrievedAt);
        Equal(MusicKnowledgeOutcome.Unknown, blocked.Outcome);

        MusicKnowledgeFactResolution absent = WikidataFixtureParser.ParseDescription("""{ "entities": { "Q4242": {} } }""", accepted, RetrievedAt);
        Equal(MusicKnowledgeOutcome.ConfirmedAbsence, absent.Outcome);
        Equal("artist-zoya", absent.EntityId);
        Equal(MusicKnowledgeSource.Wikidata, absent.Provenance.Single().Source);
        Equal("Q4242", absent.Provenance.Single().SourceRecordId);

        MusicKnowledgeFactResolution missingEnglish = WikidataFixtureParser.ParseDescription(
            "{ \"entities\": { \"Q4242\": { \"descriptions\": {} } } }", accepted, RetrievedAt);
        Equal(MusicKnowledgeOutcome.ConfirmedAbsence, missingEnglish.Outcome);
        Equal("artist-zoya", missingEnglish.EntityId);
        Equal("Q4242", missingEnglish.Provenance.Single().SourceRecordId);

        foreach (string? malformed in new string?[] { null, string.Empty, " \t\r\n ", "{ nope" })
        {
            AssertCanonicalWikidataFailure(malformed, accepted, MusicKnowledgeOutcome.MalformedResponse);
        }
        foreach (string root in NonObjectJsonValues())
        {
            AssertCanonicalWikidataFailure(root, accepted, MusicKnowledgeOutcome.SchemaFailure);
        }
        foreach (string invalidSchema in WikidataSchemaFailures())
        {
            AssertCanonicalWikidataFailure(invalidSchema, accepted, MusicKnowledgeOutcome.SchemaFailure);
        }
    }

    private static void AssertCanonicalWikidataFailure(
        string? fixture,
        MusicKnowledgeIdentityResolution accepted,
        MusicKnowledgeOutcome expectedOutcome)
    {
        MusicKnowledgeFactResolution failure = WikidataFixtureParser.ParseDescription(fixture!, accepted, RetrievedAt);
        Equal(expectedOutcome, failure.Outcome);
        Equal("artist-zoya", failure.EntityId);
        Equal(MusicKnowledgeSource.Wikidata, failure.Provenance.Single().Source);
        Equal("Q4242", failure.Provenance.Single().SourceRecordId);
        Equal(RetrievedAt, failure.Provenance.Single().RetrievedAtUtc);
        Equal("wikidata-fixture", failure.Provenance.Single().FixtureName);
    }

    internal static void ReviewedFactsAndReconciliationPreserveConflictAndProvenance()
    {
        var reviewed = new ReviewedMusicKnowledgeFactSource([
            new ReviewedMusicKnowledgeFact("description", "artist-zoya", "reviewed fixture artist", "review-1"),
        ]);
        MusicKnowledgeFact reviewedFact = reviewed.Resolve("description", "artist-zoya", RetrievedAt).Single();
        MusicKnowledgeFact providerFact = reviewedFact with
        {
            Value = "different provider description",
            Provenance = new MusicKnowledgeProvenance(MusicKnowledgeSource.Wikidata, "Q4242", RetrievedAt, "wikidata-fixture"),
        };
        MusicKnowledgeFactResolution conflict = MusicKnowledgeFactReconciler.Reconcile("description", "artist-zoya", [reviewedFact, providerFact]);
        Equal(MusicKnowledgeOutcome.SourceConflict, conflict.Outcome);
        Equal(2, conflict.Facts.Count);
        Equal(2, conflict.Provenance.Count);

        MusicKnowledgeFactResolution unknown = MusicKnowledgeFactReconciler.Reconcile("description", "missing", []);
        Equal(MusicKnowledgeOutcome.Unknown, unknown.Outcome);
    }

    internal static void FixtureTransportMapsOutageTimeoutRateLimitAndSchemaFailure()
    {
        var recovered = new MusicKnowledgeFakeHttpSequence([
            new MusicKnowledgeFixtureHttpResponse(503, null),
            new MusicKnowledgeFixtureHttpResponse(200, "{}"),
        ]);
        Equal(MusicKnowledgeOutcome.ConfirmedFact, MusicKnowledgeFixtureRequestRunner.Execute(recovered, new MusicKnowledgeRetryPolicy(2)));
        Equal(2, recovered.Calls);

        var timedOut = new MusicKnowledgeFakeHttpSequence([new MusicKnowledgeFixtureHttpResponse(0, null, TimedOut: true)]);
        Equal(MusicKnowledgeOutcome.Timeout, MusicKnowledgeFixtureRequestRunner.Execute(timedOut, new MusicKnowledgeRetryPolicy(1)));
        var rateLimited = new MusicKnowledgeFakeHttpSequence([new MusicKnowledgeFixtureHttpResponse(429, null)]);
        Equal(MusicKnowledgeOutcome.RateLimited, MusicKnowledgeFixtureRequestRunner.Execute(rateLimited, new MusicKnowledgeRetryPolicy(1)));
        var schemaFailure = new MusicKnowledgeFakeHttpSequence([new MusicKnowledgeFixtureHttpResponse(200, null)]);
        Equal(MusicKnowledgeOutcome.SchemaFailure, MusicKnowledgeFixtureRequestRunner.Execute(schemaFailure, new MusicKnowledgeRetryPolicy(1)));
    }

    internal static void BoundedMemoryCacheExpiresAndRefusesCorruption()
    {
        var cache = new MusicKnowledgeMemoryCache(2, TimeSpan.FromMinutes(5), TimeSpan.FromMinutes(1));
        MusicKnowledgeFactResolution first = Fact("one", MusicKnowledgeOutcome.ConfirmedFact);
        cache.Store("z", first, RetrievedAt);
        cache.Store("a", Fact("two", MusicKnowledgeOutcome.Unknown), RetrievedAt);
        cache.Store("m", Fact("three", MusicKnowledgeOutcome.Unknown), RetrievedAt);
        Equal(2, cache.Count);
        False(cache.Read("z", RetrievedAt).Found);
        MusicKnowledgeCacheReadResult stale = cache.Read("a", RetrievedAt.AddMinutes(2));
        True(stale.Found);
        Equal(MusicKnowledgeOutcome.StaleEvidence, stale.Resolution!.Outcome);
        cache.InjectCorruptEntryForTesting("corrupt");
        MusicKnowledgeCacheReadResult corrupt = cache.Read("corrupt", RetrievedAt);
        True(corrupt.Found);
        Equal(MusicKnowledgeOutcome.CorruptCache, corrupt.Resolution!.Outcome);
    }

    internal static void FixtureReportIsDeterministicReadableAndHasNoStylePromotion()
    {
        MusicKnowledgeFactResolution beta = Fact("beta", MusicKnowledgeOutcome.Unknown);
        MusicKnowledgeFactResolution alpha = Fact("alpha", MusicKnowledgeOutcome.ConfirmedAbsence);
        string first = MusicKnowledgeFixtureReport.Create("fixed-fixture", [beta, alpha]);
        string second = MusicKnowledgeFixtureReport.Create("fixed-fixture", [alpha, beta]);
        Equal(first, second);
        Contains("recorded fixtures and fake HTTP responses only", first);
        Contains("Runtime integration: none", first);
        False(first.Contains("genre", StringComparison.OrdinalIgnoreCase));
        False(first.Contains("style", StringComparison.OrdinalIgnoreCase));
    }

    internal static void M13ATypesHaveNoHttpClientOrNormalRuntimeBinding()
    {
        Type[] types = typeof(MusicKnowledgeFixtureReport).Assembly.GetTypes()
            .Where(type => type.Namespace == typeof(MusicKnowledgeFixtureReport).Namespace && type.Name.StartsWith("MusicKnowledge", StringComparison.Ordinal))
            .ToArray();
        False(types.Any(type => typeof(HttpClient).IsAssignableFrom(type)));
        False(typeof(ZoyaRadio.MediaProbe.Program).GetFields(BindingFlags.Static | BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic)
            .Any(field => types.Contains(field.FieldType)));
    }

    private static void AssertMusicBrainzOutcome(string? fixture, MusicKnowledgeOutcome expectedOutcome)
    {
        MusicKnowledgeIdentityResolution resolution = MusicBrainzFixtureParser.Parse(fixture!, RetrievedAt);
        if (resolution.Outcome != expectedOutcome)
        {
            throw new InvalidOperationException($"MusicBrainz fixture expected {expectedOutcome}, got {resolution.Outcome}: {fixture}");
        }
        if (expectedOutcome != MusicKnowledgeOutcome.ConfirmedFact)
        {
            True(resolution.Identity is null);
        }

        _ = MusicBrainzFixtureParser.ParseSnapshot(fixture!, RetrievedAt, out MusicKnowledgeOutcome snapshotOutcome);
        MusicKnowledgeOutcome expectedSnapshotOutcome = expectedOutcome is MusicKnowledgeOutcome.Unknown or MusicKnowledgeOutcome.AmbiguousIdentity
            ? MusicKnowledgeOutcome.ConfirmedFact
            : expectedOutcome;
        if (snapshotOutcome != expectedSnapshotOutcome)
        {
            throw new InvalidOperationException($"MusicBrainz snapshot expected {expectedSnapshotOutcome}, got {snapshotOutcome}: {fixture}");
        }
    }

    private static IEnumerable<string> NonObjectJsonValues()
    {
        yield return "null";
        yield return "[]";
        yield return "\"fixture\"";
        yield return "42";
        yield return "true";
    }

    private static IEnumerable<string> MusicBrainzSchemaFailures()
    {
        yield return "{}";
        yield return "{ \"artists\": [] }";
        yield return "{ \"recordings\": [] }";

        foreach (string shape in InvalidRepresentedShapes())
        {
            if (shape != "[]")
            {
                yield return MusicBrainzFixture(shape, "[]");
                yield return MusicBrainzFixture("[]", shape);
                yield return MusicBrainzFixture("[]", "[]", shape);
            }
            yield return MusicBrainzFixture("[ " + shape + " ]", "[]");
            yield return MusicBrainzFixture("[ " + ArtistWith("\"id\": " + shape) + " ]", "[]");
            yield return MusicBrainzFixture("[ " + ArtistWith("\"name\": " + shape) + " ]", "[]");
            yield return MusicBrainzFixture("[ " + ArtistWith("\"wikidata\": " + shape) + " ]", "[]");
            if (shape != "[]")
            {
                yield return MusicBrainzFixture("[ " + ArtistWith("\"aliases\": " + shape) + " ]", "[]");
            }
            yield return MusicBrainzFixture("[ " + ArtistWith("\"aliases\": [ " + shape + " ]") + " ]", "[]");
            yield return MusicBrainzFixture("[ " + ArtistWith("\"aliases\": [ " + AliasWith("\"name\": " + shape) + " ]") + " ]", "[]");
            yield return MusicBrainzFixture("[ " + ArtistWith("\"aliases\": [ " + AliasWith("\"type\": " + shape) + " ]") + " ]", "[]");

            yield return MusicBrainzFixture("[ " + Artist + " ]", "[ " + shape + " ]");
            yield return MusicBrainzFixture("[ " + Artist + " ]", "[ " + RecordingWith("\"id\": " + shape) + " ]");
            yield return MusicBrainzFixture("[ " + Artist + " ]", "[ " + RecordingWith("\"title\": " + shape) + " ]");
            if (shape != "[]")
            {
                yield return MusicBrainzFixture("[ " + Artist + " ]", "[ " + RecordingWith("\"artist-credit\": " + shape) + " ]");
            }
            yield return MusicBrainzFixture("[ " + Artist + " ]", "[ " + RecordingWith("\"artist-credit\": [ " + shape + " ]") + " ]");
            yield return MusicBrainzFixture("[ " + Artist + " ]", "[ " + RecordingWith("\"artist-credit\": [ " + CreditWith("\"artist-id\": " + shape) + " ]") + " ]");
            yield return MusicBrainzFixture("[ " + Artist + " ]", "[ " + RecordingWith("\"artist-credit\": [ " + CreditWith("\"artist\": " + shape) + " ]") + " ]");
            yield return MusicBrainzFixture("[ " + Artist + " ]", "[ " + RecordingWith("\"artist-credit\": [ " + CreditWith("\"name\": " + shape) + " ]") + " ]");
            yield return MusicBrainzFixture("[ " + Artist + " ]", "[ " + RecordingWith("\"artist-credit\": [ " + CreditWith("\"joinphrase\": " + shape) + " ]") + " ]");
            yield return MusicBrainzFixture("[ " + Artist + " ]", "[ " + RecordingWith("\"artist-credit\": [ { \"artist\": " + NestedArtistWith("\"id\": " + shape) + " } ]") + " ]");
            yield return MusicBrainzFixture("[ " + Artist + " ]", "[ " + RecordingWith("\"artist-credit\": [ { \"artist\": " + NestedArtistWith("\"name\": " + shape) + " } ]") + " ]");
            if (shape != "[]")
            {
                yield return MusicBrainzFixture("[ " + Artist + " ]", "[ " + RecordingWith("\"releases\": " + shape) + " ]");
            }
            yield return MusicBrainzFixture("[ " + Artist + " ]", "[ " + RecordingWith("\"releases\": [ " + shape + " ]") + " ]");
            yield return MusicBrainzFixture("[ " + Artist + " ]", "[ " + RecordingWith("\"releases\": [ " + ReleaseWith("\"id\": " + shape) + " ]") + " ]");
            yield return MusicBrainzFixture("[ " + Artist + " ]", "[ " + RecordingWith("\"releases\": [ " + ReleaseWith("\"title\": " + shape) + " ]") + " ]");
            yield return MusicBrainzFixture("[ " + Artist + " ]", "[ " + RecordingWith("\"releases\": [ " + ReleaseWith("\"status\": " + shape) + " ]") + " ]");
            yield return MusicBrainzFixture("[ " + Artist + " ]", "[ " + RecordingWith("\"releases\": [ " + ReleaseWith("\"release-group\": " + shape) + " ]") + " ]");
            yield return MusicBrainzFixture("[ " + Artist + " ]", "[ " + RecordingWith("\"releases\": [ " + ReleaseWith("\"release-group\": " + ReleaseGroupWith("\"id\": " + shape)) + " ]") + " ]");
            yield return MusicBrainzFixture("[ " + Artist + " ]", "[ " + RecordingWith("\"releases\": [ " + ReleaseWith("\"release-group\": " + ReleaseGroupWith("\"title\": " + shape)) + " ]") + " ]");
            yield return MusicBrainzFixture("[ " + Artist + " ]", "[ " + RecordingWith("\"releases\": [ " + ReleaseWith("\"release-group\": " + ReleaseGroupWith("\"primary-type\": " + shape)) + " ]") + " ]");
            yield return MusicBrainzFixture("[ " + Artist + " ]", "[]", "[ " + shape + " ]");
            yield return MusicBrainzFixture("[ " + Artist + " ]", "[]", "[ " + RelationshipWith("\"type\": " + shape) + " ]");
            yield return MusicBrainzFixture("[ " + Artist + " ]", "[]", "[ " + RelationshipWith("\"source-id\": " + shape) + " ]");
            yield return MusicBrainzFixture("[ " + Artist + " ]", "[]", "[ " + RelationshipWith("\"target-id\": " + shape) + " ]");
        }

        yield return MusicBrainzFixture("[ " + ArtistWith() + ", " + ArtistWith() + " ]", "[]");
        yield return MusicBrainzFixture("[ " + Artist + " ]", "[ " + RecordingWith("\"artist-credit\": [ { \"artist-id\": \"artist-zoya\", \"artist\": " + NestedArtist + " } ]") + " ]");
        yield return MusicBrainzFixture("[ " + Artist + " ]", "[ " + RecordingWith("\"artist-credit\": [ { \"artist\": {} } ]") + " ]");
        yield return MusicBrainzFixture("[ " + Artist + " ]", "[ " + RecordingWith("\"artist-credit\": [ { \"artist\": { \"id\": \"artist-guest\" } } ]") + " ]");
        yield return MusicBrainzFixture("[ " + Artist + " ]", "[ " + RecordingWith("\"artist-credit\": [ { \"artist\": { \"id\": \"artist-zoya\", \"name\": \"Contradiction\" } } ]") + " ]");
    }

    private static IEnumerable<string> WikidataSchemaFailures()
    {
        yield return "{}";
        yield return "{ \"entities\": {} }";
        foreach (string shape in InvalidNonObjectShapes())
        {
            yield return "{ \"entities\": " + shape + " }";
            yield return "{ \"entities\": { \"Q4242\": " + shape + " } }";
            yield return "{ \"entities\": { \"Q4242\": { \"descriptions\": " + shape + " } } }";
            yield return "{ \"entities\": { \"Q4242\": { \"descriptions\": { \"en\": " + shape + " } } } }";
        }
        foreach (string shape in InvalidRepresentedShapes())
        {
            yield return "{ \"entities\": { \"Q4242\": { \"descriptions\": { \"en\": { \"value\": " + shape + " } } } } }";
        }
    }

    private static IEnumerable<string> InvalidRepresentedShapes()
    {
        yield return "null";
        yield return "{}";
        yield return "[]";
        yield return "\" \"";
        yield return "42";
        yield return "true";
    }

    private static IEnumerable<string> InvalidNonObjectShapes()
    {
        yield return "null";
        yield return "[]";
        yield return "\" \"";
        yield return "42";
        yield return "true";
    }

    private static string MusicBrainzFixture(string artists, string recordings, string relationships = "[]") =>
        "{ \"artists\": " + artists + ", \"recordings\": " + recordings + ", \"relationships\": " + relationships + " }";

    private const string Artist = "{ \"id\": \"artist-zoya\", \"name\": \"ZØYA & The CAPS\" }";
    private const string NestedArtist = "{ \"id\": \"artist-guest\", \"name\": \"Guest Artist\" }";
    private const string Alias = "{ \"name\": \"Zoya Caps\", \"type\": \"approved\" }";
    private const string Credit = "{ \"artist-id\": \"artist-zoya\", \"name\": \"ZØYA & The CAPS\", \"joinphrase\": \"\" }";
    private const string ReleaseGroup = "{ \"id\": \"release-group-main\", \"title\": \"Søng, The!\", \"primary-type\": \"Album\" }";
    private const string Release = "{ \"id\": \"release-main\", \"title\": \"Main Release\", \"status\": \"Official\", \"release-group\": " + ReleaseGroup + " }";
    private const string Recording = "{ \"id\": \"recording-song\", \"title\": \"Søng, The!\", \"artist-credit\": [ " + Credit + " ], \"releases\": [ " + Release + " ] }";
    private const string Relationship = "{ \"type\": \"wikidata\", \"source-id\": \"artist-zoya\", \"target-id\": \"Q4242\" }";

    private static string ArtistWith(string replacement = "") => ReplaceProperty(Artist, replacement);
    private static string AliasWith(string replacement) => ReplaceProperty(Alias, replacement);
    private static string CreditWith(string replacement) => ReplaceProperty(Credit, replacement);
    private static string NestedArtistWith(string replacement) => ReplaceProperty(NestedArtist, replacement);
    private static string RecordingWith(string replacement) => ReplaceProperty(Recording, replacement);
    private static string RecordingWithIdAndArtistCredit(string id, string artistCredit) =>
        "{ \"id\": \"" + id + "\", \"title\": \"Søng, The!\", \"artist-credit\": [ " + artistCredit + " ], \"releases\": [ " + Release + " ] }";
    private static string ReleaseWith(string replacement) => ReplaceProperty(Release, replacement);
    private static string ReleaseGroupWith(string replacement) => ReplaceProperty(ReleaseGroup, replacement);
    private static string RelationshipWith(string replacement) => ReplaceProperty(Relationship, replacement);

    private static string ReplaceProperty(string jsonObject, string replacement)
    {
        if (string.IsNullOrEmpty(replacement)) return jsonObject;
        int nameStart = replacement.IndexOf('"');
        int nameEnd = replacement.IndexOf('"', nameStart + 1);
        string name = replacement[(nameStart + 1)..nameEnd];
        int propertyStart = jsonObject.IndexOf("\"" + name + "\"", StringComparison.Ordinal);
        if (propertyStart < 0)
        {
            return "{ " + replacement + ", " + jsonObject[2..];
        }

        int colon = jsonObject.IndexOf(':', propertyStart);
        int valueStart = colon + 1;
        while (char.IsWhiteSpace(jsonObject[valueStart])) valueStart++;
        int valueEnd = JsonValueEnd(jsonObject, valueStart);
        return jsonObject[..propertyStart] + replacement + jsonObject[valueEnd..];
    }

    private static int JsonValueEnd(string value, int start)
    {
        if (value[start] == '"')
        {
            for (int index = start + 1; index < value.Length; index++)
            {
                if (value[index] == '\\') { index++; continue; }
                if (value[index] == '"') return index + 1;
            }
        }

        if (value[start] is '{' or '[')
        {
            char open = value[start];
            char close = open == '{' ? '}' : ']';
            int depth = 0;
            for (int index = start; index < value.Length; index++)
            {
                if (value[index] == '"')
                {
                    index = JsonValueEnd(value, index) - 1;
                    continue;
                }
                if (value[index] == open) depth++;
                if (value[index] == close && --depth == 0) return index + 1;
            }
        }

        int scalarEnd = start;
        while (scalarEnd < value.Length && value[scalarEnd] is not ',' and not '}') scalarEnd++;
        return scalarEnd;
    }

    private static MusicKnowledgeSnapshot Parse(string fixture)
    {
        MusicKnowledgeSnapshot snapshot = MusicBrainzFixtureParser.ParseSnapshot(fixture, RetrievedAt, out MusicKnowledgeOutcome outcome);
        Equal(MusicKnowledgeOutcome.ConfirmedFact, outcome);
        return snapshot;
    }

    private static MusicKnowledgeFactResolution Fact(string entityId, MusicKnowledgeOutcome outcome) =>
        new("description", entityId, outcome, [], [], "fixture");

    private static string FoundationFixture() => """
        {
          "artists": [
            { "id": "artist-zoya", "name": "ZØYA & The CAPS", "wikidata": "Q4242",
              "aliases": [
                { "name": "Zoya Caps", "type": "approved" },
                { "name": "Zoya Old Spelling", "type": "historical" }
              ] },
            { "id": "artist-guest", "name": "Guest Artist" }
          ],
          "recordings": [
            { "id": "recording-song", "title": "Søng, The!",
              "artist-credit": [
                { "name": "ZØYA & The CAPS", "artist-id": "artist-zoya", "joinphrase": " & " },
                { "artist": { "id": "artist-guest", "name": "Guest Artist" } }
              ],
              "releases": [
                { "id": "release-deluxe", "title": "Deluxe Remaster", "status": "Official",
                  "release-group": { "id": "release-group-main", "title": "Søng, The!", "primary-type": "Album" } }
              ]
            }
          ],
          "relationships": [ { "type": "wikidata", "source-id": "artist-zoya", "target-id": "Q4242" } ]
        }
        """;

    internal static void ProviderRoutesAreExactBoundedAndUnicodeSafe()
    {
        True(M13AProviderRequestFactory.TryCreateArtist("Заберут меня в солдаты", "case-01", out M13AProviderRequest? artist));
        Equal(M13AProviderRoute.MusicBrainzArtist, artist!.Route);
        Equal("musicbrainz.org", artist.Uri.Host);
        Equal("/ws/2/artist", artist.Uri.AbsolutePath);
        True(artist.Uri.Query.Contains("fmt=json", StringComparison.Ordinal));
        True(artist.Uri.Query.Contains("limit=5", StringComparison.Ordinal));
        True(M13AProviderRequestFactory.TryCreateRecording("T.N.T.", "AC/DC", "case-01-recording", out M13AProviderRequest? recording));
        Equal("/ws/2/recording", recording!.Uri.AbsolutePath);
        True(M13AProviderRequestFactory.TryCreateWikidata("Q4242", "case-01-wikidata", out M13AProviderRequest? wikidata));
        Equal("/wiki/Special:EntityData/Q4242.json", wikidata!.Uri.AbsolutePath);

        var forged = new M13AProviderRequest(M13AProviderRoute.MusicBrainzArtist, new Uri("https://musicbrainz.org/ws/2/release?fmt=json"), "forged");
        False(M13AProviderRequestFactory.IsAllowed(forged));
        var wrongPort = new M13AProviderRequest(M13AProviderRoute.MusicBrainzArtist, new Uri("https://musicbrainz.org:444/ws/2/artist" + artist.Uri.Query), "wrong-port");
        False(M13AProviderRequestFactory.IsAllowed(wrongPort));
        False(M13AProviderRequestFactory.IsAllowed(new M13AProviderRequest(M13AProviderRoute.MusicBrainzArtist, new Uri("https://musicbrainz.org/ws/2/artist?query=x&queryExtra=x&fmt=json&limit=5&inc=url-rels"), "query-extra")));
        False(M13AProviderRequestFactory.IsAllowed(new M13AProviderRequest(M13AProviderRoute.MusicBrainzArtist, new Uri("https://musicbrainz.org/ws/2/artist?query=x&query=y&fmt=json&limit=5&inc=url-rels"), "query-duplicate")));
        False(M13AProviderRequestFactory.IsAllowed(new M13AProviderRequest(M13AProviderRoute.MusicBrainzArtist, new Uri("https://musicbrainz.org/ws/2/artist?query&fmt=json&limit=5&inc=url-rels"), "query-malformed")));
        False(M13AProviderRequestFactory.TryCreateWikidata("Q0", "invalid", out _));
        False(M13AProviderRequestFactory.TryCreateArtist(new string('x', 201), "oversized", out _));
        False(M13AProviderRequestFactory.TryCreateArtist("artist\nheader", "control", out _));
        Equal(MusicKnowledgeOutcome.UnsupportedRepresentation, M13AProviderRequestRunner.Map(artist, new M13AProviderResponse(302, null)));
        Equal(MusicKnowledgeOutcome.SchemaFailure, M13AProviderRequestRunner.Map(artist, new M13AProviderResponse(200, null, BodyTooLarge: true)));
        Equal(MusicKnowledgeOutcome.RateLimited, M13AProviderRequestRunner.Map(artist, new M13AProviderResponse(503, null)));
        Equal(MusicKnowledgeOutcome.ProviderOutage, M13AProviderRequestRunner.Map(wikidata!, new M13AProviderResponse(503, null)));

        using HttpClientHandler defaultHandler = M13AHttpProviderTransport.CreateDefaultHandler();
        False(defaultHandler.AllowAutoRedirect);
        False(defaultHandler.UseCookies);
        var redirectHandler = new M13AHttpHandler(_ => new HttpResponseMessage(HttpStatusCode.Found)
        {
            Headers = { Location = new Uri("https://example.test/redirect") },
        });
        using var redirectTransport = new M13AHttpProviderTransport("steve@example.test", redirectHandler);
        M13AProviderResponse redirect = redirectTransport.SendAsync(artist, CancellationToken.None).GetAwaiter().GetResult();
        Equal(302, redirect.StatusCode);
        False(redirectHandler.SawCookie);
    }

    internal static async Task M13AI2ContactAndFixtureOnlyCommandRefuseLiveWorkSafely()
    {
        False(M13AProviderContact.IsMeaningful(null));
        False(M13AProviderContact.IsMeaningful("not-a-contact"));
        False(M13AProviderContact.IsMeaningful("person(@example.test"));
        True(M13AProviderContact.IsMeaningful("steve@example.test"));
        True(M13AProviderContact.IsMeaningful("https://example.test/contact"));
        int exitCode = await M13AProviderDiagnosticCommand.RunAsync(live: false);
        Equal(0, exitCode);
    }

    internal static async Task M13AI2ProviderRetryPacingAndCancellationAreBounded()
    {
        var clock = new M13AFakeClock();
        var transport = new M13ARecordingTransport(
            new M13AProviderResponse(429, null, RetryAfter: TimeSpan.FromMinutes(5)),
            new M13AProviderResponse(200, "{}"),
            new M13AProviderResponse(200, "{}"));
        var runner = new M13AProviderRequestRunner(transport, clock);
        True(M13AProviderRequestFactory.TryCreateArtist("Artist", "pace-artist", out M13AProviderRequest? artist));
        M13AProviderRequestResult retried = await runner.ExecuteAsync(artist!, CancellationToken.None);
        Equal(MusicKnowledgeOutcome.ConfirmedFact, retried.Outcome);
        Equal(2, retried.Attempts);
        Equal(TimeSpan.FromSeconds(60), clock.Delays[0]);

        True(M13AProviderRequestFactory.TryCreateRecording("Song", "Artist", "pace-recording", out M13AProviderRequest? recording));
        await runner.ExecuteAsync(recording!, CancellationToken.None);
        Equal(3, transport.Calls);
        Equal(TimeSpan.FromSeconds(1.5), clock.Delays[^1]);
        Equal(TimeSpan.FromSeconds(1.5), M13AProviderRequestRunner.MusicBrainzMinimumSpacing);

        using var cancelled = new CancellationTokenSource();
        cancelled.Cancel();
        M13AProviderRequestResult stopped = await runner.ExecuteAsync(recording!, cancelled.Token);
        True(stopped.Cancelled);
        Equal(3, transport.Calls);
        True(M13AProviderRequestRunner.ShouldRetry(new M13AProviderResponse(0, null, TimedOut: true), MusicKnowledgeOutcome.Timeout, 1));
        True(M13AProviderRequestRunner.ShouldRetry(new M13AProviderResponse(429, null), MusicKnowledgeOutcome.RateLimited, 1));
        True(M13AProviderRequestRunner.ShouldRetry(new M13AProviderResponse(503, null), MusicKnowledgeOutcome.ProviderOutage, 1));
        False(M13AProviderRequestRunner.ShouldRetry(new M13AProviderResponse(501, null), MusicKnowledgeOutcome.ProviderOutage, 1));
        False(M13AProviderRequestRunner.ShouldRetry(new M13AProviderResponse(505, null), MusicKnowledgeOutcome.ProviderOutage, 1));
        True(M13AProviderRequestRunner.IsTemporaryServerStatus(500));
        True(M13AProviderRequestRunner.IsTemporaryServerStatus(502));
        True(M13AProviderRequestRunner.IsTemporaryServerStatus(503));
        True(M13AProviderRequestRunner.IsTemporaryServerStatus(504));
        False(M13AProviderRequestRunner.IsTemporaryServerStatus(501));
        False(M13AProviderRequestRunner.IsTemporaryServerStatus(505));

        var refusedTransport = new M13ARecordingTransport(new M13AProviderResponse(501, null), new M13AProviderResponse(200, "{}"));
        M13AProviderRequestResult refused = await new M13AProviderRequestRunner(refusedTransport, new M13AFakeClock()).ExecuteAsync(artist!, CancellationToken.None);
        Equal(1, refused.Attempts);
        Equal(1, refusedTransport.Calls);

        using var networkTransport = new M13AHttpProviderTransport("steve@example.test", new M13AHttpHandler(_ => throw new HttpRequestException("local fake network failure")));
        M13AProviderRequestResult network = await new M13AProviderRequestRunner(networkTransport, new M13AFakeClock()).ExecuteAsync(artist!, CancellationToken.None);
        Equal(MusicKnowledgeOutcome.ProviderOutage, network.Outcome);
        Equal(2, network.Attempts);
        Equal(2, network.AttemptLedger!.Count);

        using var cancellationSource = new CancellationTokenSource();
        int cancellationHandlerCalls = 0;
        using var cancellationTransport = new M13AHttpProviderTransport("steve@example.test", new M13AHttpHandler(_ =>
        {
            cancellationHandlerCalls++;
            if (cancellationHandlerCalls == 1) return new HttpResponseMessage((HttpStatusCode)429);
            cancellationSource.Cancel();
            throw new HttpRequestException("local fake failure concurrent with cancellation");
        }));
        M13AProviderRequestResult cancellationWins = await new M13AProviderRequestRunner(cancellationTransport, new M13AFakeClock()).ExecuteAsync(artist!, cancellationSource.Token);
        True(cancellationWins.Cancelled);
        Equal(MusicKnowledgeOutcome.Unknown, cancellationWins.Outcome);
        Equal(2, cancellationHandlerCalls);

        var fallbackClock = new M13AFakeClock();
        var fallbackTransport = new M13ARecordingTransport(
            new M13AProviderResponse(503, null), new M13AProviderResponse(503, null), new M13AProviderResponse(200, "{}"));
        var fallbackRunner = new M13AProviderRequestRunner(fallbackTransport, fallbackClock);
        M13AProviderRequestResult finalRateLimited = await fallbackRunner.ExecuteAsync(artist!, CancellationToken.None);
        Equal(MusicKnowledgeOutcome.RateLimited, finalRateLimited.Outcome);
        Equal(2, finalRateLimited.Attempts);
        Equal(TimeSpan.FromSeconds(5), fallbackClock.Delays[0]);
        await fallbackRunner.ExecuteAsync(recording!, CancellationToken.None);
        Equal(TimeSpan.FromSeconds(5), fallbackClock.Delays[1]);
        Equal(2, fallbackClock.Delays.Count);

        var retryAfterClock = new M13AFakeClock();
        var retryAfterTransport = new M13ARecordingTransport(new M13AProviderResponse(503, null, RetryAfter: TimeSpan.FromSeconds(8)), new M13AProviderResponse(200, "{}"));
        M13AProviderRequestResult retryAfter = await new M13AProviderRequestRunner(retryAfterTransport, retryAfterClock).ExecuteAsync(artist!, CancellationToken.None);
        Equal(MusicKnowledgeOutcome.ConfirmedFact, retryAfter.Outcome);
        Equal(TimeSpan.FromSeconds(8), retryAfterClock.Delays.Single());

        var earlyWakeClock = new M13AEarlyWakeClock(
            TimeSpan.FromSeconds(1),
            TimeSpan.FromSeconds(-0.25),
            TimeSpan.FromSeconds(4.25),
            TimeSpan.FromSeconds(1),
            TimeSpan.FromSeconds(0.5));
        var earlyWakeTransport = new M13ARecordingTransport(
            new M13AProviderResponse(503, null),
            new M13AProviderResponse(200, "{}"),
            new M13AProviderResponse(200, "{}"));
        earlyWakeTransport.OnSend = () => earlyWakeClock.RequestStarts.Add(earlyWakeClock.UtcNow);
        var earlyWakeRunner = new M13AProviderRequestRunner(earlyWakeTransport, earlyWakeClock);
        await earlyWakeRunner.ExecuteAsync(artist!, CancellationToken.None);
        await earlyWakeRunner.ExecuteAsync(recording!, CancellationToken.None);
        Equal(3, earlyWakeClock.RequestStarts.Count);
        Equal(TimeSpan.FromSeconds(5), earlyWakeClock.RequestStarts[1] - earlyWakeClock.RequestStarts[0]);
        Equal(TimeSpan.FromSeconds(1.5), earlyWakeClock.RequestStarts[2] - earlyWakeClock.RequestStarts[1]);
        Equal(5, earlyWakeClock.Delays.Count);
        foreach (int temporaryStatus in new[] { 500, 502, 504 })
        {
            Equal(MusicKnowledgeOutcome.ProviderOutage, M13AProviderRequestRunner.Map(artist!, new M13AProviderResponse(temporaryStatus, null)));
        }
    }

    internal static async Task M13AI2DiagnosticSequencePreservesIdentityAndWikidataGating()
    {
        var acceptedTransport = new M13ARecordingTransport(
            new M13AProviderResponse(200, ArtistBody("Wake Up", "Rage Against The Machine", "mb-rage", "Q4242")),
            new M13AProviderResponse(200, RecordingBody("Wake Up", "mb-rage", "Rage Against The Machine")),
            new M13AProviderResponse(200, """{ "entities": { "Q4242": { "descriptions": { "en": { "value": "fixture description" } } } } }"""),
            new M13AProviderResponse(404, null), new M13AProviderResponse(404, null), new M13AProviderResponse(404, null),
            new M13AProviderResponse(404, null), new M13AProviderResponse(404, null), new M13AProviderResponse(404, null), new M13AProviderResponse(404, null));
        var accepted = await new M13AProviderDiagnostic(new M13AProviderRequestRunner(acceptedTransport, new M13AFakeClock())).ExecuteAsync(CancellationToken.None);
        M13ADiagnosticItemResult first = accepted.Items.Single(item => item.Index == 1);
        Equal(MusicKnowledgeOutcome.ConfirmedFact, first.IdentityOutcome);
        Equal(MusicKnowledgeOutcome.ConfirmedFact, first.WikidataOutcome);
        Equal("mb-rage", first.ArtistEntityId);
        Equal("Q4242", first.WikidataId);
        Equal(1, first.WikidataLogicalRequests);
        Equal(10, accepted.LogicalRequests);
        Equal(accepted.LogicalRequests, accepted.Attempts);
        Equal(M13AProviderRoute.MusicBrainzArtist, acceptedTransport.Requests[0].Route);
        Equal(M13AProviderRoute.MusicBrainzRecording, acceptedTransport.Requests[1].Route);
        Equal(M13AProviderRoute.WikidataEntity, acceptedTransport.Requests[2].Route);

        var nestedTransport = new M13ARecordingTransport(
            new M13AProviderResponse(200, ArtistBody("Wake Up", "Rage Against The Machine", "mb-rage", "Q4242")),
            new M13AProviderResponse(200, """{ "recordings": [ { "id": "recording-nested", "title": "Wake Up", "artist-credit": [ { "artist": { "id": "guest", "name": "Guest" } } ], "releases": [] } ] }"""),
            new M13AProviderResponse(404, null), new M13AProviderResponse(404, null), new M13AProviderResponse(404, null),
            new M13AProviderResponse(404, null), new M13AProviderResponse(404, null), new M13AProviderResponse(404, null), new M13AProviderResponse(404, null));
        M13AProviderDiagnosticResult nested = await new M13AProviderDiagnostic(new M13AProviderRequestRunner(nestedTransport, new M13AFakeClock())).ExecuteAsync(CancellationToken.None);
        Equal(MusicKnowledgeOutcome.UnsupportedRepresentation, nested.Items[0].IdentityOutcome);
        Equal(0, nested.Items[0].WikidataLogicalRequests);
        False(nestedTransport.Requests.Take(2).Any(request => request.Route == M13AProviderRoute.WikidataEntity));

        var malformedTransport = new M13ARecordingTransport(new M13AProviderResponse(200, "{ not json"));
        M13AProviderDiagnosticResult malformed = await new M13AProviderDiagnostic(new M13AProviderRequestRunner(malformedTransport, new M13AFakeClock())).ExecuteAsync(CancellationToken.None);
        Equal(MusicKnowledgeOutcome.MalformedResponse, malformed.Items[0].IdentityOutcome);

        var schemaTransport = new M13ARecordingTransport(new M13AProviderResponse(200, """{ "artists": [ { "id": "mb-missing-name" } ] }"""));
        M13AProviderDiagnosticResult schema = await new M13AProviderDiagnostic(new M13AProviderRequestRunner(schemaTransport, new M13AFakeClock())).ExecuteAsync(CancellationToken.None);
        Equal(MusicKnowledgeOutcome.SchemaFailure, schema.Items[0].IdentityOutcome);

        var nullRequiredTransport = new M13ARecordingTransport(new M13AProviderResponse(200, """{ "artists": [ { "id": null, "name": "raw-secret-value" } ] }"""));
        M13AProviderDiagnosticResult nullRequired = await new M13AProviderDiagnostic(new M13AProviderRequestRunner(nullRequiredTransport, new M13AFakeClock())).ExecuteAsync(CancellationToken.None);
        Equal(MusicKnowledgeOutcome.SchemaFailure, nullRequired.Items[0].IdentityOutcome);
        Equal("$.artists[0].id:Null", nullRequired.Items[0].StructuralDiagnostic);
        False(nullRequired.Items[0].StructuralDiagnostic!.Contains("raw-secret-value", StringComparison.Ordinal));

        var nullableAliasTransport = new M13ARecordingTransport(new M13AProviderResponse(200, """{ "artists": [ { "id": "mb-null-alias", "name": "Different Artist", "aliases": [ { "name": "Rage Against The Machine", "type": null } ] } ] }"""));
        M13AProviderDiagnosticResult nullableAlias = await new M13AProviderDiagnostic(new M13AProviderRequestRunner(nullableAliasTransport, new M13AFakeClock())).ExecuteAsync(CancellationToken.None);
        Equal(MusicKnowledgeOutcome.Unknown, nullableAlias.Items[0].IdentityOutcome);
        Equal(1, nullableAlias.Items[0].MusicBrainzLogicalRequests);

        var unknownFieldTransport = new M13ARecordingTransport(new M13AProviderResponse(200, """{ "artists": [ { "id": "mb-unknown", "name": "Different Artist", "unknown-identity-candidate": "Rage Against The Machine" } ] }"""));
        M13AProviderDiagnosticResult unknownField = await new M13AProviderDiagnostic(new M13AProviderRequestRunner(unknownFieldTransport, new M13AFakeClock())).ExecuteAsync(CancellationToken.None);
        Equal(MusicKnowledgeOutcome.Unknown, unknownField.Items[0].IdentityOutcome);
        Equal(1, unknownField.Items[0].MusicBrainzLogicalRequests);

        var circuitTransport = new M13ARecordingTransport(
            new M13AProviderResponse(503, null), new M13AProviderResponse(503, null),
            new M13AProviderResponse(503, null), new M13AProviderResponse(503, null),
            new M13AProviderResponse(200, ArtistBody("not requested", "Raul Seixas", "mb-raul", "Q7")));
        M13AProviderDiagnosticResult circuit = await new M13AProviderDiagnostic(new M13AProviderRequestRunner(circuitTransport, new M13AFakeClock())).ExecuteAsync(CancellationToken.None);
        True(circuit.IncompleteRateLimited);
        Equal("stopped-after-two-consecutive-final-rate-limited-identities", circuit.StopState);
        Equal(2, circuit.Items.Count);
        Equal(2, circuit.LogicalRequests);
        Equal(4, circuit.Attempts);
        True(circuit.Items.All(item => item.IdentityOutcome == MusicKnowledgeOutcome.RateLimited));
        Equal(4, circuitTransport.Calls);
    }

    internal static void M13AI2MatrixArtifactsAndCliBoundaryStayDeterministic()
    {
        string[] expected =
        [
            "Wake Up|Rage Against The Machine", "Ouro De Tolo|Raul Seixas", "T.N.T.|AC/DC", "Leraine|Secede & Kettel",
            "Sweet Home Alabama|Lynyrd Skynyrd", "Chop Suey!|System of a Down", "Заберут меня в солдаты|Otava Yo",
            "Zoya Radio M13A Impossible Fixture|Zoya Radio Nonexistent Artist",
        ];
        Equal(8, M13AProviderDiagnosticMatrix.Identities.Count);
        Equal(string.Join("\n", expected), string.Join("\n", M13AProviderDiagnosticMatrix.Identities.Select(item => item.Title + "|" + item.Artist)));
        Equal(2, M13AProviderDiagnosticMatrix.MaximumMusicBrainzLogicalRequestsPerIdentity);
        Equal(1, M13AProviderDiagnosticMatrix.MaximumWikidataLogicalRequestsPerIdentity);
        Equal(24, M13AProviderDiagnosticMatrix.MaximumLogicalRequests);
        Equal(32, M13AProviderRequestRunner.MaximumTotalAttempts);

        var attempt = new M13AProviderAttempt(
            "MusicBrainz", "MusicBrainzArtist", "https://musicbrainz.org/ws/2/artist", "m13a-01-artist", 200,
            MusicKnowledgeOutcome.ConfirmedFact, new DateTimeOffset(2026, 8, 28, 12, 0, 0, TimeSpan.Zero), 1, false, 12,
            "0123456789abcdef0123456789abcdef0123456789abcdef0123456789abcdef", "MusicBrainz HTTP 503 is treated as rate-limited; user-agent, source-IP, global throttling, and capacity cannot be distinguished.");
        var result = new M13AProviderDiagnosticResult(
            [new M13ADiagnosticItemResult(1, "Wake Up", "Rage Against The Machine", MusicKnowledgeOutcome.ConfirmedFact, MusicKnowledgeOutcome.ConfirmedFact, "mb-rage", "Q4242", 2, 1, 3, "$.artists[0].id:Null")],
            3,
            3,
            false,
            [attempt],
            true,
            "stopped-after-two-consecutive-final-rate-limited-identities");
        string evidence = M13AProviderDiagnosticArtifacts.CreateEvidence(result);
        string ledger = M13AProviderDiagnosticArtifacts.CreateLedger(result);
        string report = M13AProviderDiagnosticArtifacts.CreateReport(result);
        Equal(evidence, M13AProviderDiagnosticArtifacts.CreateEvidence(result));
        Equal(report, M13AProviderDiagnosticArtifacts.CreateReport(result));
        Contains("\"artifactType\": \"evidence\"", evidence);
        Contains("\"artifactType\": \"request-ledger\"", ledger);
        Contains("canonicalIdentities", evidence);
        Contains("canonicalEntityId", evidence);
        Contains("directEntityUrl", evidence);
        Contains("retrievalTimeUtc", evidence);
        Contains("applicability", evidence);
        Contains("HttpStatus", ledger);
        Contains("BodyByteCount", ledger);
        Contains("BodySha256", ledger);
        Contains("SafeUrl", ledger);
        Contains("ClassificationDetail", ledger);
        Contains("structuralDiagnostics", evidence);
        Contains("$.artists[0].id:Null", evidence);
        Contains("incompleteRateLimited", ledger);
        Contains("stopped-after-two-consecutive-final-rate-limited-identities", report);
        Contains("M13A-I2 Provider Diagnostic Report", report);
        False((evidence + ledger + report).Contains("secret-contact", StringComparison.Ordinal));
        False((evidence + ledger + report).Contains("raw-secret-response", StringComparison.Ordinal));
        False((evidence + ledger + report).Contains("raw-secret-value", StringComparison.Ordinal));
        False((evidence + ledger + report).Contains("raw response", StringComparison.OrdinalIgnoreCase));
        False(typeof(Program).GetFields(BindingFlags.Static | BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic)
            .Any(field => field.FieldType == typeof(M13AProviderDiagnostic) || field.FieldType == typeof(M13AHttpProviderTransport)));
        Equal("--m13a-i2-provider-diagnostic", M13AProviderDiagnosticCommand.DiagnosticArgument);
        Equal("--live", M13AProviderDiagnosticCommand.LiveArgument);

        True(M13AProviderRequestFactory.TryCreateArtist("Body test", "body-test", out M13AProviderRequest? bodyRequest));
        byte[] oversized = new byte[M13AHttpProviderTransport.MaximumResponseBodyBytes + 1];
        Array.Fill(oversized, (byte)'x');
        using var bodyTransport = new M13AHttpProviderTransport("steve@example.test", new M13AHttpHandler(_ => new HttpResponseMessage(HttpStatusCode.OK) { Content = new M13AUnknownLengthContent(oversized) }));
        M13AProviderResponse oversizedResponse = bodyTransport.SendAsync(bodyRequest!, CancellationToken.None).GetAwaiter().GetResult();
        True(oversizedResponse.BodyTooLarge);
        Equal(M13AHttpProviderTransport.MaximumResponseBodyBytes + 1, oversizedResponse.BodyByteCount);
    }

    private static string ArtistBody(string ignoredTitle, string artist, string artistId, string qid) =>
        "{ \"artists\": [ { \"id\": \"" + artistId + "\", \"name\": \"" + artist + "\", \"wikidata\": \"" + qid + "\" } ], \"recordings\": [], \"relationships\": [ { \"type\": \"wikidata\", \"source-id\": \"" + artistId + "\", \"target-id\": \"" + qid + "\" } ] }";

    private static string RecordingBody(string title, string artistId, string artist) =>
        "{ \"recordings\": [ { \"id\": \"recording-wake-up\", \"title\": \"" + title + "\", \"artist-credit\": [ { \"artist-id\": \"" + artistId + "\", \"name\": \"" + artist + "\" } ], \"releases\": [] } ] }";

    private sealed class M13ARecordingTransport : IM13AProviderTransport
    {
        private readonly Queue<M13AProviderResponse> _responses;

        internal M13ARecordingTransport(params M13AProviderResponse[] responses) => _responses = new(responses);

        internal int Calls { get; private set; }

        internal List<M13AProviderRequest> Requests { get; } = [];

        internal Action? OnSend { get; set; }

        public Task<M13AProviderResponse> SendAsync(M13AProviderRequest request, CancellationToken cancellationToken)
        {
            Calls++;
            Requests.Add(request);
            OnSend?.Invoke();
            return Task.FromResult(_responses.Count > 0 ? _responses.Dequeue() : new M13AProviderResponse(503, null));
        }
    }

    private sealed class M13AHttpHandler : HttpMessageHandler
    {
        private readonly Func<HttpRequestMessage, HttpResponseMessage> _response;

        internal M13AHttpHandler(Func<HttpRequestMessage, HttpResponseMessage> response) => _response = response;

        internal bool SawCookie { get; private set; }

        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            SawCookie = request.Headers.Contains("Cookie");
            return Task.FromResult(_response(request));
        }
    }

    private sealed class M13AUnknownLengthContent : HttpContent
    {
        private readonly byte[] _bytes;

        internal M13AUnknownLengthContent(byte[] bytes) => _bytes = bytes;

        protected override Task SerializeToStreamAsync(Stream stream, TransportContext? context) => stream.WriteAsync(_bytes).AsTask();

        protected override bool TryComputeLength(out long length)
        {
            length = 0;
            return false;
        }
    }

    private sealed class M13AFakeClock : IM13AProviderClock
    {
        private DateTimeOffset _now = new(2026, 8, 28, 12, 0, 0, TimeSpan.Zero);

        internal List<TimeSpan> Delays { get; } = [];

        public DateTimeOffset UtcNow => _now;

        public Task DelayAsync(TimeSpan delay, CancellationToken cancellationToken)
        {
            Delays.Add(delay);
            _now = _now.Add(delay);
            return Task.CompletedTask;
        }
    }

    private sealed class M13AEarlyWakeClock : IM13AProviderClock
    {
        private readonly Queue<TimeSpan> _advances;
        private DateTimeOffset _now = new(2026, 8, 28, 12, 0, 0, TimeSpan.Zero);

        internal M13AEarlyWakeClock(params TimeSpan[] advances) => _advances = new(advances);

        internal List<TimeSpan> Delays { get; } = [];

        internal List<DateTimeOffset> RequestStarts { get; } = [];

        public DateTimeOffset UtcNow => _now;

        public Task DelayAsync(TimeSpan delay, CancellationToken cancellationToken)
        {
            Delays.Add(delay);
            _now = _now.Add(_advances.Count > 0 ? _advances.Dequeue() : TimeSpan.Zero);
            return Task.CompletedTask;
        }
    }

    private static void True(bool value)
    {
        if (!value) throw new InvalidOperationException("Expected true.");
    }

    private static void False(bool value)
    {
        if (value) throw new InvalidOperationException("Expected false.");
    }

    private static void Equal<T>(T expected, T actual)
    {
        if (!EqualityComparer<T>.Default.Equals(expected, actual)) throw new InvalidOperationException($"Expected '{expected}', got '{actual}'.");
    }

    private static void Contains(string expected, string actual)
    {
        if (!actual.Contains(expected, StringComparison.Ordinal)) throw new InvalidOperationException($"Expected '{expected}' in text.");
    }
}
