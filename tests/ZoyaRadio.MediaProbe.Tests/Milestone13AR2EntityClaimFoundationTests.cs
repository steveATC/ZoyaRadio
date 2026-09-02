using System.Reflection;
using System.Reflection.Emit;
using System.Text.Json;
using ZoyaRadio.MediaProbe;

internal static class Milestone13AR2EntityClaimFoundationTests
{
    private static readonly DateTimeOffset RetrievedAt = new(2026, 9, 1, 12, 0, 0, TimeSpan.Zero);

    internal static void OneArtistIsReusedByMultipleTracks()
    {
        (MusicKnowledgeEntityGraph graph, KnowledgeEntityId artist, KnowledgeEntityId freeBird, KnowledgeEntityId sweetHome) = LynyrdGraph();
        Equal(artist, graph.GetTrack(freeBird).ArtistCredits.Single().ArtistId);
        Equal(artist, graph.GetTrack(sweetHome).ArtistCredits.Single().ArtistId);
        Equal(1, graph.Entities.OfType<ArtistEntity>().Count());
    }

    internal static void ArtistClaimsAreStoredOnce()
    {
        (MusicKnowledgeEntityGraph graph, KnowledgeEntityId artist, _, _) = LynyrdGraph();
        AddClaim(graph, Claim(1, artist, "formation-year", "1964", KnowledgeEntityScope.Artist));
        Equal(1, graph.GetArtist(artist).Claims.Count);
    }

    internal static void TracksDoNotReceiveCopiedArtistClaims()
    {
        (MusicKnowledgeEntityGraph graph, KnowledgeEntityId artist, KnowledgeEntityId freeBird, KnowledgeEntityId sweetHome) = LynyrdGraph();
        AddClaim(graph, Claim(1, artist, "formation-year", "1964", KnowledgeEntityScope.Artist));
        Equal(0, graph.GetTrack(freeBird).Claims.Count);
        Equal(0, graph.GetTrack(sweetHome).Claims.Count);
    }

    internal static void OneTrackCanReferenceMultipleArtists()
    {
        var graph = new MusicKnowledgeEntityGraph();
        KnowledgeEntityId first = EntityId(1);
        KnowledgeEntityId second = EntityId(2);
        KnowledgeEntityId track = EntityId(3);
        EstablishArtist(graph, first, "Artist One");
        EstablishArtist(graph, second, "Artist Two");
        EstablishTrack(graph, track, "Shared Song", "Artist One feat. Artist Two");
        AddCredit(graph, track, first, "Artist One", 0, null, "credit-review-1");
        AddCredit(graph, track, second, "Artist Two", 1, "featured artist", "credit-review-2");
        TrackArtistCredit[] credits = graph.GetTrack(track).ArtistCredits.ToArray();
        Equal(2, credits.Length);
        Equal(first, credits[0].ArtistId);
        Equal(second, credits[1].ArtistId);
    }

    internal static void RawObservedArtistCreditIsPreserved()
    {
        const string raw = "Beyoncé  feat.  JAY-Z & Guests";
        var observation = new ObservedTrackDescriptor("Déjà Vu", raw);
        Equal(raw, observation.RawArtistCredit);
    }

    internal static void CollaborationStringsAreNeverGuessedOrSplit()
    {
        var graph = new MusicKnowledgeEntityGraph();
        KnowledgeEntityId track = EntityId(1);
        EstablishTrack(graph, track, "Collaboration", "Alpha feat. Beta & Gamma");
        Equal(0, graph.GetTrack(track).ArtistCredits.Count);
        Equal(0, graph.Entities.OfType<ArtistEntity>().Count());
    }

    internal static void SameNameAmbiguityCannotEstablishFactualIdentity()
    {
        var graph = new MusicKnowledgeEntityGraph();
        KnowledgeEntityId first = EntityId(1);
        KnowledgeEntityId second = EntityId(2);
        EstablishArtist(graph, first, "Phoenix");
        EstablishArtist(graph, second, "Phoenix");
        ObservedSubjectKey key = ObservedSubjectKey.Artist(" phoenix ");
        LinkObservation(graph, key, first, "reviewed-map-one");
        LinkObservation(graph, key, second, "reviewed-map-two");
        ObservedSubjectResolution resolution = graph.ResolveObservedSubject(key);
        Equal(ObservedSubjectResolutionState.Ambiguous, resolution.State);
        Equal(2, resolution.CandidateEntityIds.Count);
        Equal(0, graph.GetArtist(first).Claims.Count + graph.GetArtist(second).Claims.Count);
    }

    internal static void ProviderIdentifiersAreReferencesNotEntityIds()
    {
        var graph = new MusicKnowledgeEntityGraph();
        KnowledgeEntityId local = EntityId(1);
        EstablishArtist(graph, local, "Local Artist");
        var reference = new ExternalIdentityReference(
            "Wikidata", "artist", "Q123", ExternalIdentityCorrespondence.IdentifiesEntity,
            KnowledgeEntityScope.Artist, "reviewed-provider-link");
        Added(graph.AddExternalIdentity(local, reference));
        Equal(local, graph.GetArtist(local).Id);
        Equal("Q123", graph.GetArtist(local).ExternalIdentities.Single().ProviderRecordId);
        Equal(typeof(Guid), typeof(KnowledgeEntityId).GetProperty("Value", BindingFlags.Instance | BindingFlags.NonPublic)!.PropertyType);

        var wrongScope = new ExternalIdentityReference(
            "fixture-provider", "track", "provider-track-1", ExternalIdentityCorrespondence.IdentifiesEntity,
            KnowledgeEntityScope.Track, "reviewed-wrong-scope-link");
        Equal(EntityGraphOperationOutcome.WrongEntityScope, graph.AddExternalIdentity(local, wrongScope).Outcome);
    }

    internal static void MusicBrainzRecordingNarrowsWithoutRedefiningTrack()
    {
        var graph = new MusicKnowledgeEntityGraph();
        KnowledgeEntityId track = EntityId(1);
        EstablishTrack(graph, track, "Free Bird", "Lynyrd Skynyrd");
        var reference = new ExternalIdentityReference(
            "MusicBrainz", "recording", "mb-recording-123", ExternalIdentityCorrespondence.NarrowsEntity,
            KnowledgeEntityScope.Recording, "reviewed-mb-link");
        Added(graph.AddExternalIdentity(track, reference));
        Equal(track, graph.GetTrack(track).Id);
        Equal(ExternalIdentityCorrespondence.NarrowsEntity, graph.GetTrack(track).ExternalIdentities.Single().Correspondence);
    }

    internal static void WrongScopeEvidenceCannotBecomeBroaderClaim()
    {
        var graph = new MusicKnowledgeEntityGraph();
        KnowledgeEntityId artist = EntityId(1);
        EstablishArtist(graph, artist, "An Artist");
        KnowledgeClaim claim = Claim(1, artist, "origin", "Somewhere", KnowledgeEntityScope.Artist, KnowledgeEntityScope.Track);
        Equal(EntityGraphOperationOutcome.WrongEvidenceScope, graph.AddClaim(claim).Outcome);
        Equal(0, graph.GetArtist(artist).Claims.Count);

        KnowledgeEntityId track = EntityId(2);
        EstablishTrack(graph, track, "A Track", "An Artist");
        Equal(EntityGraphOperationOutcome.WrongEntityScope,
            graph.AddClaim(Claim(2, track, "origin", "Somewhere", KnowledgeEntityScope.Artist)).Outcome);
    }

    internal static void RecordingAndReleaseClaimsAreRefusedUntilModeled()
    {
        var graph = new MusicKnowledgeEntityGraph();
        KnowledgeEntityId track = EntityId(1);
        EstablishTrack(graph, track, "A Track", "An Artist");
        Equal(EntityGraphOperationOutcome.UnsupportedEntityScope,
            graph.AddClaim(Claim(1, track, "mix", "2026 remaster", KnowledgeEntityScope.Recording)).Outcome);
        Equal(EntityGraphOperationOutcome.UnsupportedEntityScope,
            graph.AddClaim(Claim(2, track, "edition", "Deluxe", KnowledgeEntityScope.Release)).Outcome);
        Equal(0, graph.GetTrack(track).Claims.Count);
    }

    internal static void ConflictingClaimsCoexist()
    {
        var graph = new MusicKnowledgeEntityGraph();
        KnowledgeEntityId artist = EntityId(1);
        EstablishArtist(graph, artist, "An Artist");
        KnowledgeClaim first = Claim(1, artist, "formation-year", "1964", KnowledgeEntityScope.Artist,
            state: KnowledgeClaimState.Conflicting, conflictsWith: ClaimId(2));
        KnowledgeClaim second = Claim(2, artist, "formation-year", "1965", KnowledgeEntityScope.Artist,
            state: KnowledgeClaimState.Conflicting, conflictsWith: ClaimId(1));
        AddClaim(graph, first);
        AddClaim(graph, second);
        Equal(2, graph.GetArtist(artist).Claims.Count);
        True(graph.GetArtist(artist).Claims.All(claim => claim.State == KnowledgeClaimState.Conflicting));
    }

    internal static void WithdrawingOneClaimDoesNotReplaceEntity()
    {
        var graph = new MusicKnowledgeEntityGraph();
        KnowledgeEntityId artist = EntityId(1);
        EstablishArtist(graph, artist, "An Artist");
        AddClaim(graph, Claim(1, artist, "origin", "Old", KnowledgeEntityScope.Artist));
        AddClaim(graph, Claim(2, artist, "origin", "New", KnowledgeEntityScope.Artist, replaces: ClaimId(1)));
        Equal(EntityGraphOperationOutcome.Updated,
            graph.SetClaimState(ClaimId(1), KnowledgeClaimState.Withdrawn, ClaimId(2)).Outcome);
        Equal(artist, graph.GetArtist(artist).Id);
        Equal(2, graph.GetArtist(artist).Claims.Count);
        Equal(KnowledgeClaimState.Withdrawn, graph.GetArtist(artist).Claims.Single(claim => claim.Id == ClaimId(1)).State);
    }

    internal static void GenreAndStyleClaimsRemainMultiValued()
    {
        var graph = new MusicKnowledgeEntityGraph();
        KnowledgeEntityId artist = EntityId(1);
        EstablishArtist(graph, artist, "Rage Against the Machine");
        AddClaim(graph, Claim(1, artist, "genre-style", "alternative metal", KnowledgeEntityScope.Artist));
        AddClaim(graph, Claim(2, artist, "genre-style", "rap metal", KnowledgeEntityScope.Artist));
        AddClaim(graph, Claim(3, artist, "genre-style", "funk metal", KnowledgeEntityScope.Artist));
        Equal(3, graph.GetArtist(artist).Claims.Count(claim => claim.FactType == "genre-style"));
    }

    internal static void ClaimFreshnessIsIndependent()
    {
        var graph = new MusicKnowledgeEntityGraph();
        KnowledgeEntityId artist = EntityId(1);
        EstablishArtist(graph, artist, "An Artist");
        AddClaim(graph, Claim(1, artist, "origin", "Somewhere", KnowledgeEntityScope.Artist));
        AddClaim(graph, Claim(2, artist, "formation-year", "2000", KnowledgeEntityScope.Artist));
        Equal(EntityGraphOperationOutcome.Updated, graph.SetClaimState(ClaimId(1), KnowledgeClaimState.Stale).Outcome);
        Equal(KnowledgeClaimState.Stale, graph.GetArtist(artist).Claims.Single(claim => claim.Id == ClaimId(1)).State);
        Equal(KnowledgeClaimState.SourceBacked, graph.GetArtist(artist).Claims.Single(claim => claim.Id == ClaimId(2)).State);
    }

    internal static void ArtistFactsSurviveAmbiguousTrackLookup()
    {
        var graph = new MusicKnowledgeEntityGraph();
        KnowledgeEntityId artist = EntityId(1);
        KnowledgeEntityId studio = EntityId(2);
        KnowledgeEntityId live = EntityId(3);
        EstablishArtist(graph, artist, "An Artist");
        EstablishTrack(graph, studio, "Same Song", "An Artist");
        EstablishTrack(graph, live, "Same Song", "An Artist");
        AddCredit(graph, studio, artist, "An Artist", 0, null, "studio-credit");
        AddCredit(graph, live, artist, "An Artist", 0, null, "live-credit");
        AddClaim(graph, Claim(1, artist, "origin", "Somewhere", KnowledgeEntityScope.Artist));
        ObservedSubjectKey key = ObservedSubjectKey.Track("Same Song", "An Artist");
        LinkObservation(graph, key, studio, "studio-map");
        LinkObservation(graph, key, live, "live-map");
        Equal(ObservedSubjectResolutionState.Ambiguous, graph.ResolveObservedSubject(key).State);
        Equal("Somewhere", graph.GetArtist(artist).Claims.Single().Value);
    }

    internal static void ContextProjectionFollowsRelationshipsWithoutCopying()
    {
        (MusicKnowledgeEntityGraph graph, KnowledgeEntityId artist, KnowledgeEntityId track, _) = LynyrdGraph();
        KnowledgeClaim artistClaim = Claim(1, artist, "formation-year", "1964", KnowledgeEntityScope.Artist);
        KnowledgeClaim trackClaim = Claim(2, track, "release-year", "1973", KnowledgeEntityScope.Track);
        AddClaim(graph, artistClaim);
        AddClaim(graph, trackClaim);
        var builder = new MusicKnowledgeContextProjectionBuilder();
        MusicKnowledgeContextProjection projection = builder.Build(graph, track, [trackClaim.Id, artistClaim.Id], 2);
        Equal(2, projection.Claims.Count);
        True(projection.Claims.Any(claim => claim.SubjectScope == KnowledgeEntityScope.Artist));
        True(projection.Claims.Any(claim => claim.SubjectScope == KnowledgeEntityScope.Track));
        Equal(1, graph.GetTrack(track).Claims.Count);
        Equal(1, graph.GetArtist(artist).Claims.Count);
    }

    internal static void RawProviderPayloadCannotReachProjection()
    {
        Type[] projectionTypes = [
            typeof(MusicKnowledgeContextProjectionBuilder),
            typeof(MusicKnowledgeContextProjection),
            typeof(MusicKnowledgeContextClaimView),
            typeof(ClaimEvidence),
        ];
        foreach (Type type in projectionTypes)
        {
            Type[] exposedTypes = type.GetProperties(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic)
                .Select(property => property.PropertyType)
                .Concat(type.GetMethods(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.DeclaredOnly)
                    .SelectMany(method => method.GetParameters().Select(parameter => parameter.ParameterType).Append(method.ReturnType)))
                .ToArray();
            False(exposedTypes.Any(IsRawProviderPayloadType));
        }
    }

    internal static void ExistingM13IdentityBehaviorRemainsAvailable()
    {
        var provenance = new MusicKnowledgeProvenance(MusicKnowledgeSource.ReviewedLocal, "accepted-fixture", RetrievedAt, "r2-regression");
        var artist = new MusicKnowledgeArtist("accepted-id", "Accepted Artist", [], null, provenance);
        var snapshot = new MusicKnowledgeSnapshot([artist], [], []);
        MusicKnowledgeIdentityResolution result = MusicKnowledgeIdentityResolver.Resolve(
            new MusicKnowledgeIdentityRequest("accepted artist", null), snapshot);
        Equal(MusicKnowledgeOutcome.ConfirmedFact, result.Outcome);
        Equal("accepted-id", result.Identity!.Artist.Id);
    }

    internal static void ProductionCompositionRootCannotReachR2Foundation()
    {
        Assembly assembly = typeof(MusicKnowledgeEntityGraph).Assembly;
        HashSet<Type> r2Types =
        [
            typeof(KnowledgeEntityId), typeof(KnowledgeClaimId), typeof(IdentityBasis), typeof(ObservedLabel),
            typeof(ObservedSubjectKey), typeof(ObservedTrackDescriptor), typeof(ObservedSubjectResolution),
            typeof(ExternalIdentityReference), typeof(ClaimEvidence), typeof(KnowledgeClaim),
            typeof(ArtistEntity), typeof(TrackEntity), typeof(TrackArtistCredit), typeof(MusicKnowledgeEntityGraph),
            typeof(KnowledgeLookupCacheKey), typeof(KnowledgeLookupCacheEntry), typeof(MusicKnowledgeLookupCache),
            typeof(MusicKnowledgeContextClaimView), typeof(MusicKnowledgeContextProjection),
            typeof(MusicKnowledgeContextProjectionBuilder),
        ];
        Type[] compositionTypes = assembly.GetTypes().Where(IsProductionProgramType).ToArray();
        True(compositionTypes.Length > 0);
        foreach (Type compositionType in compositionTypes)
        {
            IEnumerable<Type> referenced = compositionType.GetFields(BindingFlags.Static | BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic)
                .Select(field => field.FieldType)
                .Concat(compositionType.GetMethods(BindingFlags.Static | BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.DeclaredOnly)
                    .SelectMany(method => method.GetParameters().Select(parameter => parameter.ParameterType).Append(method.ReturnType)));
            False(referenced.Any(reference => ReferencesAnyR2Type(reference, r2Types)));
            foreach (MethodBase method in compositionType.GetMethods(BindingFlags.Static | BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.DeclaredOnly)
                .Cast<MethodBase>()
                .Concat(compositionType.GetConstructors(BindingFlags.Static | BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic))
                .Concat(compositionType.TypeInitializer is null ? [] : [compositionType.TypeInitializer]))
            {
                False(ReadIlMembers(method).Any(member => MemberReferencesAnyR2Type(member, r2Types)));
            }
        }
    }

    internal static void DefaultIdsAreRefusedAtEveryGraphBoundary()
    {
        var graph = new MusicKnowledgeEntityGraph();
        IdentityBasis artistBasis = Basis(KnowledgeEntityScope.Artist, "artist-review");
        IdentityBasis trackBasis = Basis(KnowledgeEntityScope.Track, "track-review");
        Equal(EntityGraphOperationOutcome.InvalidEntityId,
            graph.EstablishArtist(default, [new ObservedLabel("Artist")], artistBasis).Outcome);
        Equal(EntityGraphOperationOutcome.InvalidEntityId,
            graph.EstablishTrack(default, new ObservedTrackDescriptor("Track", "Artist"), trackBasis).Outcome);

        KnowledgeEntityId artist = EntityId(1);
        KnowledgeEntityId track = EntityId(2);
        EstablishArtist(graph, artist, "Artist");
        EstablishTrack(graph, track, "Track", "Artist");
        Equal(EntityGraphOperationOutcome.InvalidEntityId,
            graph.LinkArtistCredit(new TrackArtistCredit(default, artist, "Artist", 0, null, "credit")).Outcome);
        Equal(EntityGraphOperationOutcome.InvalidEntityId,
            graph.LinkArtistCredit(new TrackArtistCredit(track, default, "Artist", 0, null, "credit")).Outcome);
        Equal(EntityGraphOperationOutcome.InvalidEntityId,
            graph.AddExternalIdentity(default, new ExternalIdentityReference(
                "provider", "artist", "provider-id", ExternalIdentityCorrespondence.IdentifiesEntity,
                KnowledgeEntityScope.Artist, "provider-review")).Outcome);
        Equal(EntityGraphOperationOutcome.InvalidEntityId,
            graph.LinkObservedSubject(ObservedSubjectKey.Artist("Artist"), default, artistBasis).Outcome);

        Equal(EntityGraphOperationOutcome.InvalidClaimId,
            graph.AddClaim(new KnowledgeClaim(default, artist, "origin", "Somewhere", KnowledgeEntityScope.Artist,
                KnowledgeClaimState.SourceBacked, [Evidence(KnowledgeEntityScope.Artist)], RetrievedAt)).Outcome);
        Equal(EntityGraphOperationOutcome.InvalidEntityId,
            graph.AddClaim(new KnowledgeClaim(ClaimId(1), default, "origin", "Somewhere", KnowledgeEntityScope.Artist,
                KnowledgeClaimState.SourceBacked, [Evidence(KnowledgeEntityScope.Artist)], RetrievedAt)).Outcome);
        Equal(EntityGraphOperationOutcome.InvalidClaimId,
            graph.AddClaim(new KnowledgeClaim(ClaimId(2), artist, "origin", "Somewhere", KnowledgeEntityScope.Artist,
                KnowledgeClaimState.Conflicting, [Evidence(KnowledgeEntityScope.Artist)], RetrievedAt, conflictsWith: default(KnowledgeClaimId))).Outcome);
        Equal(EntityGraphOperationOutcome.InvalidClaimId,
            graph.AddClaim(new KnowledgeClaim(ClaimId(3), artist, "origin", "Somewhere", KnowledgeEntityScope.Artist,
                KnowledgeClaimState.SourceBacked, [Evidence(KnowledgeEntityScope.Artist)], RetrievedAt, replaces: default(KnowledgeClaimId))).Outcome);
        Equal(EntityGraphOperationOutcome.InvalidClaimId,
            graph.SetClaimState(default, KnowledgeClaimState.Stale).Outcome);

        AddClaim(graph, Claim(4, artist, "origin", "Somewhere", KnowledgeEntityScope.Artist));
        Equal(EntityGraphOperationOutcome.InvalidClaimId,
            graph.SetClaimState(ClaimId(4), KnowledgeClaimState.Withdrawn, default(KnowledgeClaimId)).Outcome);
        Equal(KnowledgeClaimState.SourceBacked, graph.GetArtist(artist).Claims.Single().State);
        Equal(2, graph.Entities.Count);
    }

    internal static void ClaimIdsAreGloballyUniqueAndStateMutationUsesOwnerIndex()
    {
        var graph = new MusicKnowledgeEntityGraph();
        KnowledgeEntityId firstArtist = EntityId(1);
        KnowledgeEntityId secondArtist = EntityId(2);
        EstablishArtist(graph, firstArtist, "First Artist");
        EstablishArtist(graph, secondArtist, "Second Artist");
        KnowledgeClaim first = Claim(1, firstArtist, "origin", "First Place", KnowledgeEntityScope.Artist);
        KnowledgeClaim duplicate = Claim(1, secondArtist, "origin", "Second Place", KnowledgeEntityScope.Artist);
        AddClaim(graph, first);
        Equal(EntityGraphOperationOutcome.DuplicateClaim, graph.AddClaim(duplicate).Outcome);
        Equal(1, graph.GetArtist(firstArtist).Claims.Count);
        Equal(0, graph.GetArtist(secondArtist).Claims.Count);

        Equal(EntityGraphOperationOutcome.Updated, graph.SetClaimState(ClaimId(1), KnowledgeClaimState.Stale).Outcome);
        Equal(KnowledgeClaimState.Stale, graph.GetArtist(firstArtist).Claims.Single().State);
        Equal(0, graph.GetArtist(secondArtist).Claims.Count);
    }

    internal static void ObservedLabelsAloneCannotEstablishEntity()
    {
        var graph = new MusicKnowledgeEntityGraph();
        KnowledgeEntityId artist = EntityId(1);
        Equal(EntityGraphOperationOutcome.InvalidIdentityBasis,
            graph.EstablishArtist(artist, [new ObservedLabel("Phoenix")], null).Outcome);
        Equal(EntityGraphOperationOutcome.InvalidIdentityBasis,
            graph.EstablishTrack(EntityId(2), new ObservedTrackDescriptor("A Song", "Phoenix"), null).Outcome);
        Equal(0, graph.Entities.Count);
    }

    internal static void ReviewedUniqueObservationMappingCanBeReused()
    {
        var graph = new MusicKnowledgeEntityGraph();
        KnowledgeEntityId artist = EntityId(1);
        EstablishArtist(graph, artist, "An Artist");
        ObservedSubjectKey key = ObservedSubjectKey.Artist("  AN   ARTIST ");
        LinkObservation(graph, key, artist, "reviewed-map");
        ObservedSubjectResolution first = graph.ResolveObservedSubject(ObservedSubjectKey.Artist("an artist"));
        ObservedSubjectResolution second = graph.ResolveObservedSubject(ObservedSubjectKey.Artist("AN ARTIST"));
        Equal(ObservedSubjectResolutionState.UniqueEstablished, first.State);
        Equal(artist, first.CandidateEntityIds.Single());
        Equal(first.State, second.State);
        Equal(first.CandidateEntityIds.Single(), second.CandidateEntityIds.Single());
    }

    internal static void AmbiguousLookupCacheCannotPromoteClaims()
    {
        DateTimeOffset now = RetrievedAt;
        var cache = new MusicKnowledgeLookupCache(2, () => now);
        var graph = new MusicKnowledgeEntityGraph();
        KnowledgeEntityId artist = EntityId(1);
        EstablishArtist(graph, artist, "Phoenix");
        var key = new KnowledgeLookupCacheKey(ObservedSubjectKey.Artist("Phoenix"), "fixture-provider", "biography", KnowledgeEntityScope.Artist);
        cache.Store(key, KnowledgeLookupOutcome.Ambiguous, TimeSpan.FromMinutes(5));
        Equal(KnowledgeLookupOutcome.Ambiguous, cache.Read(key)!.Outcome);
        Equal(0, graph.GetArtist(artist).Claims.Count);
    }

    internal static void ExpiredNegativeLookupDoesNotChangeDurableClaims()
    {
        DateTimeOffset now = RetrievedAt;
        var cache = new MusicKnowledgeLookupCache(2, () => now);
        var graph = new MusicKnowledgeEntityGraph();
        KnowledgeEntityId artist = EntityId(1);
        EstablishArtist(graph, artist, "An Artist");
        AddClaim(graph, Claim(1, artist, "origin", "Somewhere", KnowledgeEntityScope.Artist));
        var key = new KnowledgeLookupCacheKey(ObservedSubjectKey.Artist("An Artist"), "fixture-provider", "genre", KnowledgeEntityScope.Artist);
        cache.Store(key, KnowledgeLookupOutcome.Negative, TimeSpan.FromMinutes(1));
        now = now.AddMinutes(2);
        Equal<KnowledgeLookupCacheEntry?>(null, cache.Read(key));
        Equal("Somewhere", graph.GetArtist(artist).Claims.Single().Value);
    }

    private static (MusicKnowledgeEntityGraph Graph, KnowledgeEntityId Artist, KnowledgeEntityId FreeBird, KnowledgeEntityId SweetHome) LynyrdGraph()
    {
        var graph = new MusicKnowledgeEntityGraph();
        KnowledgeEntityId artist = EntityId(1);
        KnowledgeEntityId freeBird = EntityId(2);
        KnowledgeEntityId sweetHome = EntityId(3);
        EstablishArtist(graph, artist, "Lynyrd Skynyrd");
        EstablishTrack(graph, freeBird, "Free Bird", "Lynyrd Skynyrd");
        EstablishTrack(graph, sweetHome, "Sweet Home Alabama", "Lynyrd Skynyrd");
        AddCredit(graph, freeBird, artist, "Lynyrd Skynyrd", 0, null, "free-bird-credit");
        AddCredit(graph, sweetHome, artist, "Lynyrd Skynyrd", 0, null, "sweet-home-credit");
        return (graph, artist, freeBird, sweetHome);
    }

    private static void EstablishArtist(MusicKnowledgeEntityGraph graph, KnowledgeEntityId id, string label) =>
        Added(graph.EstablishArtist(id, [new ObservedLabel(label)], Basis(KnowledgeEntityScope.Artist, "artist-review-" + id)));

    private static void EstablishTrack(MusicKnowledgeEntityGraph graph, KnowledgeEntityId id, string title, string rawCredit) =>
        Added(graph.EstablishTrack(id, new ObservedTrackDescriptor(title, rawCredit), Basis(KnowledgeEntityScope.Track, "track-review-" + id)));

    private static void AddCredit(MusicKnowledgeEntityGraph graph, KnowledgeEntityId track, KnowledgeEntityId artist, string name, int order, string? relationship, string evidence) =>
        Added(graph.LinkArtistCredit(new TrackArtistCredit(track, artist, name, order, relationship, evidence)));

    private static void LinkObservation(MusicKnowledgeEntityGraph graph, ObservedSubjectKey key, KnowledgeEntityId id, string evidence) =>
        Added(graph.LinkObservedSubject(key, id, Basis(key.Scope, evidence)));

    private static IdentityBasis Basis(KnowledgeEntityScope scope, string evidence) =>
        new(IdentityBasisKind.ReviewedEvidence, evidence, scope);

    private static KnowledgeClaim Claim(
        int id,
        KnowledgeEntityId subject,
        string factType,
        string value,
        KnowledgeEntityScope scope,
        KnowledgeEntityScope? evidenceScope = null,
        KnowledgeClaimState state = KnowledgeClaimState.SourceBacked,
        KnowledgeClaimId? conflictsWith = null,
        KnowledgeClaimId? replaces = null) =>
        new(ClaimId(id), subject, factType, value, scope, state,
            [new ClaimEvidence("fixture-source", "fixture:" + id, RetrievedAt, evidenceScope ?? scope, ClaimEvidenceRole.Supports)],
            RetrievedAt, conflictsWith, replaces);

    private static ClaimEvidence Evidence(KnowledgeEntityScope scope) =>
        new("fixture-source", "fixture:boundary", RetrievedAt, scope, ClaimEvidenceRole.Supports);

    private static void AddClaim(MusicKnowledgeEntityGraph graph, KnowledgeClaim claim) => Added(graph.AddClaim(claim));

    private static KnowledgeEntityId EntityId(int value) => new(Guid.Parse($"{value:x8}-0000-0000-0000-000000000000"));

    private static KnowledgeClaimId ClaimId(int value) => new(Guid.Parse($"{value:x8}-1111-1111-1111-111111111111"));

    private static bool IsRawProviderPayloadType(Type type)
    {
        Type unwrapped = Unwrap(type);
        return unwrapped == typeof(JsonDocument)
            || unwrapped == typeof(JsonElement)
            || unwrapped == typeof(Stream)
            || unwrapped == typeof(byte[])
            || unwrapped == typeof(HttpResponseMessage);
    }

    private static Type Unwrap(Type type)
    {
        if (type.IsArray) return type.GetElementType()!;
        if (type.IsGenericType) return type.GetGenericArguments().FirstOrDefault() ?? type;
        return type;
    }

    private static bool IsProductionProgramType(Type type)
    {
        for (Type? current = type; current is not null; current = current.DeclaringType)
        {
            if (current.Name.Contains("Program", StringComparison.Ordinal)) return true;
        }

        return false;
    }

    private static bool ReferencesAnyR2Type(Type type, IReadOnlySet<Type> r2Types)
    {
        if (type.IsByRef || type.IsPointer || type.IsArray) return ReferencesAnyR2Type(type.GetElementType()!, r2Types);
        if (r2Types.Contains(type)) return true;
        return type.IsGenericType && type.GetGenericArguments().Any(argument => ReferencesAnyR2Type(argument, r2Types));
    }

    private static bool MemberReferencesAnyR2Type(MemberInfo member, IReadOnlySet<Type> r2Types)
    {
        if (member is Type type && ReferencesAnyR2Type(type, r2Types)) return true;
        if (member.DeclaringType is not null && ReferencesAnyR2Type(member.DeclaringType, r2Types)) return true;
        if (member is FieldInfo field && ReferencesAnyR2Type(field.FieldType, r2Types)) return true;
        if (member is MethodInfo method && ReferencesAnyR2Type(method.ReturnType, r2Types)) return true;
        return member is MethodBase methodBase
            && methodBase.GetParameters().Any(parameter => ReferencesAnyR2Type(parameter.ParameterType, r2Types));
    }

    private static IReadOnlyList<MemberInfo> ReadIlMembers(MethodBase method)
    {
        byte[]? il = method.GetMethodBody()?.GetILAsByteArray();
        if (il is null) return [];
        var members = new List<MemberInfo>();
        IReadOnlyDictionary<ushort, OpCode> opCodes = IlOpCodes.Value;
        int offset = 0;
        while (offset < il.Length)
        {
            ushort value = il[offset++];
            if (value == 0xFE) value = (ushort)(0xFE00 | il[offset++]);
            if (!opCodes.TryGetValue(value, out OpCode opCode)) throw new InvalidOperationException($"Unknown IL opcode 0x{value:X4}.");
            int operandOffset = offset;
            int operandSize = OperandSize(opCode.OperandType, il, operandOffset);
            if (opCode.OperandType is OperandType.InlineField or OperandType.InlineMethod or OperandType.InlineTok or OperandType.InlineType)
            {
                int token = BitConverter.ToInt32(il, operandOffset);
                try
                {
                    Type[]? typeArguments = method.DeclaringType?.IsGenericType == true ? method.DeclaringType.GetGenericArguments() : null;
                    Type[]? methodArguments = method.IsGenericMethod ? method.GetGenericArguments() : null;
                    MemberInfo? member = method.Module.ResolveMember(token, typeArguments, methodArguments);
                    if (member is not null) members.Add(member);
                }
                catch (ArgumentException)
                {
                    // A token outside this module cannot name an R2 type in this assembly.
                }
            }

            offset += operandSize;
        }

        return members;
    }

    private static int OperandSize(OperandType operandType, byte[] il, int offset) => operandType switch
    {
        OperandType.InlineNone => 0,
        OperandType.ShortInlineBrTarget or OperandType.ShortInlineI or OperandType.ShortInlineVar => 1,
        OperandType.InlineVar => 2,
        OperandType.InlineBrTarget or OperandType.InlineField or OperandType.InlineI or OperandType.InlineMethod
            or OperandType.InlineSig or OperandType.InlineString or OperandType.InlineTok or OperandType.InlineType
            or OperandType.ShortInlineR => 4,
        OperandType.InlineI8 or OperandType.InlineR => 8,
        OperandType.InlineSwitch => 4 + (BitConverter.ToInt32(il, offset) * 4),
        _ => throw new InvalidOperationException($"Unsupported IL operand type {operandType}."),
    };

    private static class IlOpCodes
    {
        internal static readonly IReadOnlyDictionary<ushort, OpCode> Value = typeof(OpCodes)
            .GetFields(BindingFlags.Public | BindingFlags.Static)
            .Where(field => field.FieldType == typeof(OpCode))
            .Select(field => (OpCode)field.GetValue(null)!)
            .ToDictionary(opCode => unchecked((ushort)opCode.Value));
    }

    private static void Added(EntityGraphOperationResult result)
    {
        if (!result.Succeeded) throw new InvalidOperationException($"Expected a successful graph operation, got {result.Outcome}: {result.Detail}");
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
}
