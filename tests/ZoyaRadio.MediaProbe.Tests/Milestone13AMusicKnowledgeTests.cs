using System.Net.Http;
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
