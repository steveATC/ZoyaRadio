using System.Net;
using System.Text;
using ZoyaRadio.MediaProbe;

internal static class Milestone9BTests
{
    public static void CatalogIsVersionedBoundedAndPreservesUnicode()
    {
        M9BContextCatalog catalog = M9BContextCatalog.Load();
        Equal(16, catalog.Cards.Count);
        True(catalog.Cards.All(card => card.Facts.Count is >= 1 and <= 5));
        Equal("Como Nossos Pais", catalog.Cards.Single(card => card.Id == "M9C6-14").Title);
        Equal("Eu Nasci Há Dez Mil Anos Atrás", catalog.Cards.Single(card => card.Id == "M9C6-15").Title);
        True(catalog.Cards.SelectMany(card => card.Facts).All(fact => fact.IsSafe));
    }

    public static void CatalogExplainsThirdPartyContext()
    {
        M9BCatalogCard raul = M9BContextCatalog.Load().Cards.Single(card => card.Id == "M9C6-15");
        Contains(string.Join(" ", raul.Facts.Select(fact => fact.Text)), "Paulo Coelho is a Brazilian writer");
    }

    public static void PromptIsWholeCommentGroundedAndMetadataOmissionIsAllowed()
    {
        M9BCatalogCard card = FirstCard();
        var media = new MediaSnapshot(card.Title, card.Artist, null);
        CommentPrompt prompt = M9BWholeCommentPromptFactory.Create(media, card.ToContextCard(), []);
        Contains(prompt.SystemInstruction, "typed context card as the factual and editorial anchor");
        Contains(prompt.SystemInstruction, "familiar low-risk music knowledge and natural host opinion");
        Contains(prompt.SystemInstruction, "Do not force title/artist repetition");
        Contains(prompt.MetadataPrompt, card.Title);
        M9BCommentValidation validation = M9BCommentValidator.Validate(
            "That answer to Neil Young arrived with enough history to make a simple song introduction feel almost suspiciously well prepared.",
            media,
            card.ToContextCard(),
            [],
            []);
        Equal(M9BCommentValidationKind.Valid, validation.Kind);
    }

    public static void FallbackAndTechnicalFailuresAreHandledDeterministically()
    {
        M9BCatalogCard card = FirstCard();
        var media = new MediaSnapshot(card.Title, card.Artist, null);
        Equal(M9BCommentValidationKind.Fallback, M9BCommentValidator.Validate("FALLBACK", media, card.ToContextCard(), [], []).Kind);
        Equal(M9BCommentValidationKind.Advisory, M9BCommentValidator.Validate("David Bowie released this in 1999.", media, card.ToContextCard(), [], []).Kind);
        Equal(M9BCommentValidationKind.HardDiscard, M9BCommentValidator.Validate("This is Sweet Home Alabama by Black Sabbath.", media, card.ToContextCard(), [], []).Kind);
        M9BCatalogCard rage = M9BContextCatalog.Load().Cards.Single(item => item.Id == "M9C6-11");
        Equal(M9BCommentValidationKind.HardDiscard, M9BCommentValidator.Validate("Rage Against the Machine shut down the New York Stock Exchange.", new MediaSnapshot(rage.Title, rage.Artist, null), rage.ToContextCard(), [], []).Kind);
    }

    public static void OwnerApprovedMusicalLanguageRemainsDeliverable()
    {
        M9BCatalogCard master = M9BContextCatalog.Load().Cards.Single(card => card.Id == "M9C6-06");
        var media = new MediaSnapshot(master.Title, master.Artist, null);
        Equal(M9BCommentValidationKind.Advisory, M9BCommentValidator.Validate(
            "That riff is pure heavy metal menace.", media, master.ToContextCard(), [], []).Kind);
        Equal(M9BCommentValidationKind.Advisory, M9BCommentValidator.Validate(
            "This riff invented heavy metal in 1986.", media, master.ToContextCard(), [], []).Kind);
        Equal(M9BCommentValidationKind.Advisory, M9BCommentValidator.Validate(
            "Sweet Silence Studios played a key role in shaping Metallica's sound.", media, master.ToContextCard(), [], []).Kind);
    }
    public static void OwnerApprovedFinalSmokeFixturesRemainDeliverable()
    {
        int count = 0;
        foreach ((MediaSnapshot media, M9BContextCard card, string output) in OwnerApprovedFinalSmokeFixtures())
        {
            M9BCommentValidation validation = M9BCommentValidator.Validate(output, media, card, [], []);
            if (validation.Kind is not (M9BCommentValidationKind.Valid or M9BCommentValidationKind.Advisory))
            {
                throw new InvalidOperationException($"Owner-approved smoke fixture was not deliverable: {validation.Kind}; {output}");
            }

            count++;
        }

        Equal(12, count);
    }
    public static void QuestionsClichesAndImaginedScenesRemainAdvisory()
    {
        M9BCatalogCard card = FirstCard();
        M9BCommentValidation validation = M9BCommentValidator.Validate(
            "I'm picturing what a track this was for Neil Young, honestly—was that response ever going to arrive quietly?",
            new MediaSnapshot(card.Title, card.Artist, null),
            card.ToContextCard(),
            [],
            []);
        Equal(M9BCommentValidationKind.Advisory, validation.Kind);
        True(validation.AdvisoryFlags.Count >= 2);
    }

    public static void RecentStateIsBoundedAndDetectsRepeatedOpening()
    {
        var state = new M9BRecentCommentState();
        M9BContextCard card = FirstCard().ToContextCard();
        for (int index = 0; index < 8; index++) state.Add($"Tonight this very particular sentence begins number {index} with enough words to survive.", card);
        Equal(6, state.Comments.Count);
        Equal(6, state.FactIdentities.Count);
        M9BCommentValidation validation = M9BCommentValidator.Validate(
            "Tonight this very particular sentence begins with something else entirely, but the opening is still familiar.",
            new MediaSnapshot("Sweet Home Alabama", "Lynyrd Skynyrd", null),
            card,
            state.Comments,
            []);
        Equal(M9BCommentValidationKind.Advisory, validation.Kind);
    }

    public static async Task LocalCatalogAvoidsLiveSourceLookupAndCacheClears()
    {
        string directory = TemporaryDirectory();
        try
        {
            var live = new FakeM9BLiveResolver();
            var cache = new M9BContextCache(directory, MusicContextEnrichmentConfiguration.Default);
            var provider = new M9BContextProvider(M9BContextCatalog.Load(), live, cache, new FakeLog());
            M9BContextResolution result = await provider.ResolveAsync(new MediaSnapshot("Sweet Home Alabama", "Lynyrd Skynyrd", null), CancellationToken.None);
            True(result.IsConfirmedWithFacts);
            Equal(0, live.Calls);
            True(Directory.EnumerateFiles(directory, "*.json").Any());
            provider.ClearCache();
            False(Directory.EnumerateFiles(directory, "*.json", SearchOption.AllDirectories).Any());
        }
        finally { Delete(directory); }
    }

    public static void CacheIsBoundedCorruptSafeAndDoesNotContainPromptMaterial()
    {
        string directory = TemporaryDirectory();
        try
        {
            var cache = new M9BContextCache(directory, MusicContextEnrichmentConfiguration.Default);
            for (int index = 0; index < 101; index++) cache.Store(Resolution("Title " + index, "Artist " + index));
            True(Directory.EnumerateFiles(directory, "*.json").Count() <= 100);
            string path = Directory.EnumerateFiles(directory, "*.json").First();
            string serialized = File.ReadAllText(path);
            False(serialized.Contains("prompt", StringComparison.OrdinalIgnoreCase));
            False(serialized.Contains("generated prose", StringComparison.OrdinalIgnoreCase));
            foreach (string cacheFile in Directory.EnumerateFiles(directory, "*.json")) File.WriteAllText(cacheFile, "{ not JSON", new UTF8Encoding(false));
            False(cache.TryRead(new MusicContextRequest("Title 100", "Artist 100", MusicIdentityRequirement.ArtistAndTrack), out _));
        }
        finally { Delete(directory); }
    }

    public static async Task Missing14BUsesM8FallbackWithoutGeneration()
    {
        string directory = TemporaryDirectory();
        try
        {
            var generator = new M9BFakeGenerator(modelPresent: false);
            var service = CreateService(generator, directory);
            var fallback = new FakeTransitionProvider();
            var provider = new M9BTransitionCommentProvider(fallback, service, new FakeLog());
            CommentSelection selection = await provider.SelectAsync(Context(), true, CancellationToken.None);
            Equal(CommentTextSource.FixedFallback, selection.Source);
            Equal(0, generator.GenerateCalls);
            Equal(1, fallback.Calls);
        }
        finally { Delete(directory); }
    }

    public static async Task Valid14BCommentUnloadsAndRecordsOnlyAfterDelivery()
    {
        string directory = TemporaryDirectory();
        try
        {
            var generator = new M9BFakeGenerator(modelPresent: true, responses:
            ["Lynyrd Skynyrd answered Neil Young with a response that brought enough history to make a casual introduction feel unexpectedly prepared."]);
            M9BGroundedCommentService service = CreateService(generator, directory);
            M9BCommentSelection selected = await service.TrySelectAsync(Context(), true, CancellationToken.None);
            Equal(CommentTextSource.GroundedAi, selected.Selection!.Source);
            Equal(1, generator.GenerateCalls);
            Equal(1, generator.UnloadCalls);
            service.RecordDelivered(selected.Selection, selected.Card);
            M9BCommentValidation repeated = M9BCommentValidator.Validate(selected.Selection.Text, new MediaSnapshot("Sweet Home Alabama", "Lynyrd Skynyrd", null), selected.Card!, [selected.Selection.Text], []);
            Equal(M9BCommentValidationKind.Advisory, repeated.Kind);
        }
        finally { Delete(directory); }
    }

    public static async Task AdvisoryCandidateRegeneratesAtMostOnce()
    {
        string directory = TemporaryDirectory();
        try
        {
            var generator = new M9BFakeGenerator(modelPresent: true, responses:
            ["I'm picturing Sweet Home Alabama sitting beside Neil Young's response—does anyone mind if I call that wonderfully direct?",
             "Picture this: Sweet Home Alabama replying to Neil Young with a response that has the subtlety of a certified letter."]);
            M9BCommentSelection selected = await CreateService(generator, directory).TrySelectAsync(Context(), true, CancellationToken.None);
            if (selected.Selection is null) throw new InvalidOperationException($"Expected a retained advisory candidate. Reason: {selected.Reason}");
            if (!selected.UsedRegeneration) throw new InvalidOperationException("Expected exactly one advisory regeneration.");
            Equal(CommentTextSource.GroundedAi, selected.Selection.Source);
            Equal(M9BCommentSelectionKind.Ready, selected.OutcomeKind);
            Equal(2, generator.GenerateCalls);
            Equal(1, generator.UnloadCalls);
        }
        finally { Delete(directory); }
    }
    public static async Task RedirectedLiveSourceResponseIsRefused()
    {
        string directory = TemporaryDirectory();
        try
        {
            string root = RepositoryPathLocator.FindRepositoryRoot();
            string source = File.ReadAllText(Path.Combine(root, "src", "ZoyaRadio.MediaProbe", "MusicContextEnrichment.cs"));
            Contains(source, "AllowAutoRedirect = false");
            var handler = new M9BRedirectHandler();
            using var client = new HttpClient(handler);
            using var enricher = new MusicBrainzWikidataEnricher(
                MusicContextEnrichmentConfiguration.Default,
                new MusicContextCache(directory, MusicContextEnrichmentConfiguration.Default),
                client);
            MusicContextEnrichmentResult result = await enricher.EnrichAsync(
                new MusicContextRequest("Exact Track", "Exact Artist", MusicIdentityRequirement.ArtistAndTrack),
                CancellationToken.None);
            Equal(MusicContextMatchStatus.Failed, result.Status);
            False(result.IsConfirmedWithFacts);
            Equal(1, handler.Calls);
        }
        finally { Delete(directory); }
    }
    public static void M9BRuntimeDoesNotUseM9CDiagnosticEntryPoints()
    {
        string root = RepositoryPathLocator.FindRepositoryRoot();
        string runtime = File.ReadAllText(Path.Combine(root, "src", "ZoyaRadio.MediaProbe", "RadioBehaviorTest.cs"));
        False(runtime.Contains("M9C6WholeCommentBenchmark", StringComparison.Ordinal));
        Contains(runtime, "M9BTransitionCommentProvider");
    }

    public static async Task ReviewedAliasesOverrideNegativeCacheWithoutNetwork()
    {
        string directory = TemporaryDirectory();
        try
        {
            var live = new FakeM9BLiveResolver();
            var cache = new M9BContextCache(directory, MusicContextEnrichmentConfiguration.Default);
            var provider = new M9BContextProvider(M9BContextCatalog.Load(), live, cache, new FakeLog());
            (string Artist, string Title, string CardId)[] identities =
            [
                ("Black Sabbath", "Paranoid (2009 Remaster)", "M9C6-02"),
                ("Johnny Cash", "Cocaine Blues (Live at Folsom State Prison, Folsom, CA - January 1968)", "M9C6-16"),
            ];
            foreach ((string artist, string title, string cardId) in identities)
            {
                var request = new MusicContextRequest(title, artist, MusicIdentityRequirement.ArtistAndTrack);
                cache.Store(new M9BContextResolution(request, MusicContextMatchStatus.Unavailable, null, DateTimeOffset.UtcNow, "stale negative test entry", false, M9BContextSource.LiveEnrichment));
                M9BContextResolution result = await provider.ResolveAsync(new MediaSnapshot(title, artist, null), CancellationToken.None);
                Equal(cardId, result.Card!.Identity.Replace("catalog:", string.Empty, StringComparison.Ordinal));
                Equal(M9BContextSource.ReviewedCatalog, result.Source);
                False(result.FromCache);
            }

            Equal(0, live.Calls);
        }
        finally { Delete(directory); }
    }

    public static async Task NonCatalogContextStillUsesCacheBeforeLiveResolver()
    {
        string directory = TemporaryDirectory();
        try
        {
            var live = new FakeM9BLiveResolver();
            var cache = new M9BContextCache(directory, MusicContextEnrichmentConfiguration.Default);
            var provider = new M9BContextProvider(M9BContextCatalog.Load(), live, cache, new FakeLog());
            var request = new MusicContextRequest("Unreviewed Runtime Title", "Unreviewed Runtime Artist", MusicIdentityRequirement.ArtistAndTrack);
            cache.Store(new M9BContextResolution(request, MusicContextMatchStatus.Unavailable, null, DateTimeOffset.UtcNow, "cached non-catalog result", false, M9BContextSource.LiveEnrichment));
            M9BContextResolution result = await provider.ResolveAsync(new MediaSnapshot(request.Title, request.Artist, null), CancellationToken.None);
            True(result.FromCache);
            Equal(M9BContextSource.Cache, result.Source);
            Equal(0, live.Calls);
        }
        finally { Delete(directory); }
    }

    public static async Task LegacyNegativeM9BContextCacheDoesNotMaskLiveResolution()
    {
        string directory = TemporaryDirectory();
        try
        {
            var request = new MusicContextRequest("Unreviewed Runtime Title", "Unreviewed Runtime Artist", MusicIdentityRequirement.ArtistAndTrack);
            var cache = new M9BContextCache(directory, MusicContextEnrichmentConfiguration.Default);
            cache.Store(new M9BContextResolution(request, MusicContextMatchStatus.Unavailable, null, DateTimeOffset.UtcNow, "legacy negative", false, M9BContextSource.LiveEnrichment));
            string cachePath = Directory.EnumerateFiles(directory, "*.json").Single();
            string oldJson = System.Text.RegularExpressions.Regex.Replace(File.ReadAllText(cachePath), "\\s*\\\"SchemaVersion\\\"\\s*:\\s*\\\"m9b-context-v2\\\"\\s*,?", string.Empty);
            File.WriteAllText(cachePath, oldJson, new UTF8Encoding(false));

            var live = new FakeM9BLiveResolver();
            var provider = new M9BContextProvider(M9BContextCatalog.Load(), live, cache, new FakeLog());
            M9BContextResolution result = await provider.ResolveAsync(new MediaSnapshot(request.Title, request.Artist, null), CancellationToken.None);
            Equal(1, live.Calls);
            False(result.FromCache);
            Equal(M9BContextSource.LiveEnrichment, result.Source);
        }
        finally { Delete(directory); }
    }
    public static async Task RejectedGenerationDiagnosticsPreserveTypedLocalEvidence()
    {
        string directory = TemporaryDirectory();
        try
        {
            var generator = new M9BFakeGenerator(true, ["This is Sweet Home Alabama by Black Sabbath."]);
            M9BCommentSelection selection = await CreateService(generator, directory).TrySelectAsync(Context(), true, CancellationToken.None);
            Equal(M9BCommentSelectionKind.HardDiscard, selection.OutcomeKind);
            Equal(M9BContextSource.ReviewedCatalog, selection.ContextSource);
            IReadOnlyList<M9BGenerationDiagnostic> selectedDiagnostics = selection.GenerationDiagnostics ?? throw new InvalidOperationException("Expected local generation diagnostics.");
            Equal("This is Sweet Home Alabama by Black Sabbath.", selectedDiagnostics.Single().RawOutput);
            Equal(M9BCommentValidationKind.HardDiscard, selectedDiagnostics.Single().ValidationKind);

            var diagnostics = new[]
            {
                new M9BGenerationDiagnostic(1, "valid raw", "valid raw", M9BCommentValidationKind.Valid, "valid", [], null),
                new M9BGenerationDiagnostic(2, "FALLBACK", "FALLBACK", M9BCommentValidationKind.Fallback, "fallback", [], null),
                new M9BGenerationDiagnostic(3, "advisory raw", "advisory raw", M9BCommentValidationKind.Advisory, "advisory", ["test flag"], null),
                new M9BGenerationDiagnostic(4, "hard raw", "hard raw", M9BCommentValidationKind.HardDiscard, "hard", [], null),
            };
            var evidence = new M9BGate3RunEvidence(12, 1, 0, 0, 0, 0, 0, 0, true,
                [new M9BGate3PreparationRecord(1, "Title", "Artist", false, M9BGate3PreparationOutcome.HardDiscard, "hard", 125, 100, 50, false, "catalog:test", M9BContextSource.ReviewedCatalog, diagnostics)]);
            string json = System.Text.Json.JsonSerializer.Serialize(evidence);
            M9BGate3RunEvidence roundTrip = System.Text.Json.JsonSerializer.Deserialize<M9BGate3RunEvidence>(json)!;
            Equal("catalog:test", roundTrip.Preparations.Single().ContextCardIdentity);
            Equal(M9BContextSource.ReviewedCatalog, roundTrip.Preparations.Single().ContextSource);
            Equal(4, roundTrip.Preparations.Single().GenerationDiagnostics!.Count);
            Equal(M9BCommentValidationKind.Valid, roundTrip.Preparations.Single().GenerationDiagnostics![0].ValidationKind);
            Equal(M9BCommentValidationKind.Fallback, roundTrip.Preparations.Single().GenerationDiagnostics![1].ValidationKind);
            Equal(M9BCommentValidationKind.Advisory, roundTrip.Preparations.Single().GenerationDiagnostics![2].ValidationKind);
            Equal(M9BCommentValidationKind.HardDiscard, roundTrip.Preparations.Single().GenerationDiagnostics![3].ValidationKind);
        }
        finally { Delete(directory); }
    }

    public static void Gate3SmokeRunnerIsReviewedCatalogOnlyAndSilent()
    {
        string root = RepositoryPathLocator.FindRepositoryRoot();
        string source = File.ReadAllText(Path.Combine(root, "src", "ZoyaRadio.MediaProbe", "M9BGate3SmokeTest.cs"));
        Contains(source, "Paranoid (2009 Remaster)");
        Contains(source, "Cocaine Blues (Live at Folsom State Prison, Folsom, CA - January 1968)");
        Contains(source, "M9BContextSource.ReviewedCatalog");
        foreach (string forbidden in new[] { "GlobalSystemMediaTransportControls", "WindowsTransitionAudioDucker", "IAnnouncementSpeechService", "TryPauseAsync", "SetVolume" })
        {
            False(source.Contains(forbidden, StringComparison.Ordinal));
        }
    }
    public static void CatalogUsesOnlyReviewedExactArtistSpecificAliases()
    {
        M9BContextCatalog catalog = M9BContextCatalog.Load();
        Equal("M9C6-02", catalog.Find(new MediaSnapshot("Paranoid (2009 Remaster)", "Black Sabbath", null))!.Id);
        Equal("M9C6-16", catalog.Find(new MediaSnapshot("Cocaine Blues (Live at Folsom State Prison, Folsom, CA - January 1968)", "Johnny Cash", null))!.Id);
        Equal<M9BCatalogCard?>(null, catalog.Find(new MediaSnapshot("Paranoid (2009 Remaster)", "Not Black Sabbath", null)));
        Equal<M9BCatalogCard?>(null, catalog.Find(new MediaSnapshot("Paranoid (2010 Remaster)", "Black Sabbath", null)));
        Equal<M9BCatalogCard?>(null, catalog.Find(new MediaSnapshot("Cocaine Blues Live", "Johnny Cash", null)));
    }

    public static void PromptExplicitlyForbidsObservedUnsupportedClaims()
    {
        M9BCatalogCard card = FirstCard();
        CommentPrompt prompt = M9BWholeCommentPromptFactory.Create(new MediaSnapshot(card.Title, card.Artist, null), card.ToContextCard(), []);
        Contains(prompt.SystemInstruction, "familiar low-risk music knowledge and natural host opinion");
        Contains(prompt.SystemInstruction, "Avoid inventing precise dates, quotations, credits, historical incidents, or severe causal claims");
        Contains(prompt.SystemInstruction, "return exactly FALLBACK");
    }

    public static void Gate3VerdictUsesOperationalAcceptanceCriteria()
    {
        M9BGate3Verdict zeroReady = M9BGate3Verdict.Evaluate(Gate3Evidence(12, true, 0, 0, 1, Record(M9BGate3PreparationOutcome.ModelFallback)));
        False(zeroReady.Passed);
        Contains(string.Join("; ", zeroReady.Reasons), "no candidate became ready");

        M9BGate3Verdict noSuppression = M9BGate3Verdict.Evaluate(Gate3Evidence(12, true, 0, 0, 0, Record(M9BGate3PreparationOutcome.Ready)));
        False(noSuppression.Passed);
        Contains(string.Join("; ", noSuppression.Reasons), "no prepared delivery was safely suppressed");

        M9BGate3Verdict operationalPass = M9BGate3Verdict.Evaluate(Gate3Evidence(12, true, 0, 0, 1,
            Record(M9BGate3PreparationOutcome.Ready),
            Record(M9BGate3PreparationOutcome.HardDiscard),
            Record(M9BGate3PreparationOutcome.Cancelled),
            Record(M9BGate3PreparationOutcome.StaleDiscarded)));
        True(operationalPass.Passed);
        Equal(2, operationalPass.EligibleSteadyStateOpportunities);
        Equal(0.5, operationalPass.ReadinessRate);
        Equal(1, operationalPass.HardDiscardFailures);

        False(M9BGate3Verdict.Evaluate(Gate3Evidence(11, true, 0, 0, 1, Record(M9BGate3PreparationOutcome.Ready))).Passed);
        False(M9BGate3Verdict.Evaluate(Gate3Evidence(12, false, 0, 0, 1, Record(M9BGate3PreparationOutcome.Ready))).Passed);
        False(M9BGate3Verdict.Evaluate(Gate3Evidence(12, true, 1, 0, 1, Record(M9BGate3PreparationOutcome.Ready))).Passed);
    }
    public static void Gate3RunnerIsSilentAndFirefoxOnly()
    {
        string root = RepositoryPathLocator.FindRepositoryRoot();
        string source = File.ReadAllText(Path.Combine(root, "src", "ZoyaRadio.MediaProbe", "M9BGate3SilentRunner.cs"));
        Contains(source, "M9BGate3SilentRunner");
        Contains(source, "FirefoxSessionSelector.VerifiedSourceId");
        Contains(source, "GATE 3 DELIVERY SUPPRESSED");
        Contains(source, "M9BGate3RunEvidence");
        foreach (string forbidden in new[]
        {
            "TryPauseAsync",
            "TryPlayAsync",
            "TryTogglePlayPauseAsync",
            "IAnnouncementSpeechService",
            "KokoroSpeechService",
            "WindowsTransitionAudioDucker",
            "SetVolume",
            "RunDuckedAnnouncementAsync",
        })
        {
            False(source.Contains(forbidden, StringComparison.Ordinal));
        }

        var evidence = new M9BGate3RunEvidence(
            12,
            2,
            2,
            2,
            1,
            1,
            0,
            0,
            true,
            [new M9BGate3PreparationRecord(1, "Как дела", "Исполнитель", true, M9BGate3PreparationOutcome.Ready, "ready", 125, 100, 50, false)]);
        string json = System.Text.Json.JsonSerializer.Serialize(evidence);
        M9BGate3RunEvidence roundTrip = System.Text.Json.JsonSerializer.Deserialize<M9BGate3RunEvidence>(json)!;
        Equal("Как дела", roundTrip.Preparations.Single().Title);
        Equal("Исполнитель", roundTrip.Preparations.Single().Artist);
    }
    private static M9BGate3PreparationRecord Record(M9BGate3PreparationOutcome outcome) => new(
        1,
        "Track",
        "Artist",
        outcome == M9BGate3PreparationOutcome.Ready,
        outcome,
        outcome.ToString(),
        125,
        100,
        50,
        outcome == M9BGate3PreparationOutcome.Cancelled);

    private static M9BGate3RunEvidence Gate3Evidence(
        int transitions,
        bool? unloaded,
        int forbiddenActivityCount,
        int sessionSafetyDisposals,
        int suppressedDeliveries,
        params M9BGate3PreparationRecord[] preparations) => new(
        transitions,
        preparations.Length,
        preparations.Count(item => item.CandidateReady),
        suppressedDeliveries,
        preparations.Count(item => item.OutcomeKind == M9BGate3PreparationOutcome.Cancelled),
        preparations.Count(item => item.OutcomeKind == M9BGate3PreparationOutcome.StaleDiscarded),
        sessionSafetyDisposals,
        forbiddenActivityCount,
        unloaded,
        preparations);
    private static IEnumerable<(MediaSnapshot Media, M9BContextCard Card, string Output)> OwnerApprovedFinalSmokeFixtures()
    {
        M9BContextCatalog catalog = M9BContextCatalog.Load();
        M9BCatalogCard lynyrd = catalog.Cards.Single(card => card.Id == "M9C6-01");
        M9BCatalogCard sabbath = catalog.Cards.Single(card => card.Id == "M9C6-02");
        M9BCatalogCard metallica = catalog.Cards.Single(card => card.Id == "M9C6-06");
        M9BCatalogCard cash = catalog.Cards.Single(card => card.Id == "M9C6-16");

        foreach (string output in new[]
        {
            "It's interesting to know that \"Sweet Home Alabama\" was written as a response to Neil Young's \"Southern Man,\" and you can really feel that back-and-forth in the lyrics. The opening riff is instantly recognizable, and it's no wonder this song became one of their biggest hits.",
            "It's fascinating how \"Sweet Home Alabama\" came about as a reply to Neil Young's \"Southern Man\"—a fiery back-and-forth that really captured the spirit of the time. That opening riff still slaps after all these years.",
            "It's interesting to know that \"Sweet Home Alabama\" was written as a response to Neil Young's \"Southern Man,\" and you can really hear that fiery spirit in Ed King's iconic guitar riff.",
        }) yield return (new MediaSnapshot("Sweet Home Alabama", "Lynyrd Skynyrd", null), lynyrd.ToContextCard(), output);

        foreach (string output in new[]
        {
            "It's fascinating how \"Paranoid\" came together so quickly as a last-minute addition to the album—yet it went on to become Black Sabbath's only top-five UK single.",
            "It's fascinating how \"Paranoid\" came together so quickly—almost like it was destined to become one of Black Sabbath's most iconic tracks.",
            "It’s fascinating how \"Paranoid\" came together so quickly as a last-minute addition to the album—yet it ended up being one of Black Sabbath’s most iconic tracks.",
        }) yield return (new MediaSnapshot("Paranoid (2009 Remaster)", "Black Sabbath", null), sabbath.ToContextCard(), output);

        foreach (string output in new[]
        {
            "It's fascinating to think that \"Master of Puppets\" was recorded at Sweet Silence Studios in Copenhagen — a place that helped shape some of metal's most iconic sounds.",
            "You know, it's interesting how this track was recorded in Copenhagen — a city not typically associated with metal, but somehow it fits perfectly. The way the guitars cut through the mix, it's like they're pulling the strings of the whole album.",
            "I guess it's no surprise that \"Master of Puppets\" was recorded in Copenhagen—it's got that edge, that raw energy that feels like it was born in a place where metal really takes hold.",
        }) yield return (new MediaSnapshot("Master of Puppets", "Metallica", null), metallica.ToContextCard(), output);

        foreach (string output in new[]
        {
            "You know, there's something haunting about Cash's voice echoing through those prison walls, making a song like \"Cocaine Blues\" feel both raw and real.",
            "You know, it's wild to think that a song like \"Cocaine Blues\" was played in a place like Folsom State Prison — it's almost like the setting gave the track an extra layer of raw, real energy.",
            "You know, there's something haunting about hearing Johnny Cash sing \"Cocaine Blues\" from the very place where he recorded it — Folsom State Prison. The raw, gravelly tone of his voice just adds to the weight of those words.",
        }) yield return (new MediaSnapshot("Cocaine Blues (Live at Folsom State Prison, Folsom, CA - January 1968)", "Johnny Cash", null), cash.ToContextCard(), output);
    }
    private static M9BCatalogCard FirstCard() => M9BContextCatalog.Load().Cards.Single(card => card.Id == "M9C6-01");
    private static RadioPreparationContext Context() => new(new MediaSnapshot("Sweet Home Alabama", "Lynyrd Skynyrd", null), DateTimeOffset.UtcNow, [], []);
    private static M9BGroundedCommentService CreateService(M9BFakeGenerator generator, string directory)
    {
        var provider = new M9BContextProvider(M9BContextCatalog.Load(), new FakeM9BLiveResolver(), new M9BContextCache(directory, MusicContextEnrichmentConfiguration.Default), new FakeLog());
        return new M9BGroundedCommentService(provider, generator, AiCommentConfiguration.Default with { Model = M9BConstants.Model }, new M9BRecentCommentState(), new FakeLog());
    }
    private static M9BContextResolution Resolution(string title, string artist) => new(new MusicContextRequest(title, artist, MusicIdentityRequirement.ArtistAndTrack), MusicContextMatchStatus.Unavailable, null, DateTimeOffset.UtcNow, "safe test result", false);
    private static string TemporaryDirectory()
    {
        string path = Path.Combine(RepositoryPathLocator.FindRepositoryRoot(), "artifacts", "m9b-tests", Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(path);
        return path;
    }
    private static void Delete(string path) { if (Directory.Exists(path)) Directory.Delete(path, true); }
    private static void True(bool value) { if (!value) throw new InvalidOperationException("Expected true."); }
    private static void False(bool value) => True(!value);
    private static void Equal<T>(T expected, T actual) { if (!EqualityComparer<T>.Default.Equals(expected, actual)) throw new InvalidOperationException($"Expected {expected}, got {actual}."); }
    private static void Contains(string value, string expected) { if (!value.Contains(expected, StringComparison.Ordinal)) throw new InvalidOperationException($"Expected '{expected}'."); }

    private sealed class FakeM9BLiveResolver : IMusicContextEnricher
    {
        internal int Calls { get; private set; }
        public Task<MusicContextEnrichmentResult> EnrichAsync(MusicContextRequest request, CancellationToken cancellationToken)
        {
            Calls++;
            return Task.FromResult(new MusicContextEnrichmentResult(request, MusicContextMatchStatus.Unavailable, null, DateTimeOffset.UtcNow, "fake unavailable", false));
        }
    }

    private sealed class M9BRedirectHandler : HttpMessageHandler
    {
        internal int Calls { get; private set; }
        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            Calls++;
            var response = new HttpResponseMessage(HttpStatusCode.Found);
            response.Headers.Location = new Uri("https://example.invalid/escaped", UriKind.Absolute);
            return Task.FromResult(response);
        }
    }
    private sealed class FakeTransitionProvider : ITransitionCommentProvider
    {
        internal int Calls { get; private set; }
        public Task<CommentSelection> SelectAsync(RadioPreparationContext context, bool aiEnabled, CancellationToken cancellationToken)
        {
            Calls++;
            return Task.FromResult(new CommentSelection("That was Sweet Home Alabama by Lynyrd Skynyrd.", CommentTextSource.FixedFallback, 8, "fake M8 fallback", null, false));
        }
    }

    private sealed class M9BFakeGenerator : ILocalCommentGenerator
    {
        private readonly Queue<string> _responses;
        private readonly bool _modelPresent;
        internal M9BFakeGenerator(bool modelPresent, IEnumerable<string>? responses = null) { _modelPresent = modelPresent; _responses = new Queue<string>(responses ?? []); }
        public string ModelName => M9BConstants.Model;
        public Uri Endpoint => new("http://localhost:11434/api/");
        internal int GenerateCalls { get; private set; }
        internal int UnloadCalls { get; private set; }
        public Task<OllamaGenerationResult> GenerateAsync(CommentPrompt prompt, string keepAlive, TimeSpan timeout, CancellationToken cancellationToken)
        {
            GenerateCalls++;
            string response = _responses.Count > 0 ? _responses.Dequeue() : "FALLBACK";
            return Task.FromResult(new OllamaGenerationResult(OllamaGenerationStatus.Success, response, null, null));
        }
        public Task<OllamaOperationResult> UnloadAsync(TimeSpan timeout, CancellationToken cancellationToken) { UnloadCalls++; return Task.FromResult(new OllamaOperationResult(true, "fake unload")); }
        public Task<OllamaRunningModelsResult> GetRunningModelsAsync(TimeSpan timeout, CancellationToken cancellationToken) => Task.FromResult(new OllamaRunningModelsResult(true, [], "fake ps"));
        public Task<OllamaInstalledModelsResult> GetInstalledModelsAsync(TimeSpan timeout, CancellationToken cancellationToken) => Task.FromResult(new OllamaInstalledModelsResult(true, _modelPresent ? [new OllamaInstalledModel(ModelName, ModelName, 9_300_000_000, new OllamaModelDetails("14.8B", "Q4_K_M"))] : [], "fake tags"));
    }
}