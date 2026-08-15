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
        Contains(prompt.SystemInstruction, "only factual material");
        Contains(prompt.SystemInstruction, "Training knowledge may improve language and tone only");
        Contains(prompt.SystemInstruction, "force title/artist repetition");
        Contains(prompt.MetadataPrompt, card.Title);
        M9BCommentValidation validation = M9BCommentValidator.Validate(
            "That answer to Neil Young arrived with enough history to make a simple song introduction feel almost suspiciously well prepared.",
            media,
            card.ToContextCard(),
            [],
            []);
        Equal(M9BCommentValidationKind.Valid, validation.Kind);
    }

    public static void FallbackAndUnsafeAssertionsAreHandledDeterministically()
    {
        M9BCatalogCard card = FirstCard();
        var media = new MediaSnapshot(card.Title, card.Artist, null);
        Equal(M9BCommentValidationKind.Fallback, M9BCommentValidator.Validate("FALLBACK", media, card.ToContextCard(), [], []).Kind);
        Equal(M9BCommentValidationKind.HardDiscard, M9BCommentValidator.Validate("David Bowie released this in 1999.", media, card.ToContextCard(), [], []).Kind);
        Equal(M9BCommentValidationKind.HardDiscard, M9BCommentValidator.Validate("The riff makes this a heavy metal storm.", media, card.ToContextCard(), [], []).Kind);
        M9BCatalogCard rage = M9BContextCatalog.Load().Cards.Single(item => item.Id == "M9C6-11");
        Equal(M9BCommentValidationKind.HardDiscard, M9BCommentValidator.Validate("Rage Against the Machine shut down the New York Stock Exchange.", new MediaSnapshot(rage.Title, rage.Artist, null), rage.ToContextCard(), [], []).Kind);
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
             "Sweet Home Alabama turns its response to Neil Young into a piece of correspondence with impressively direct mailing-label energy."]);
            M9BCommentSelection selected = await CreateService(generator, directory).TrySelectAsync(Context(), true, CancellationToken.None);
            if (selected.Selection is null) throw new InvalidOperationException($"Expected a regenerated candidate. Reason: {selected.Reason}");
            if (!selected.UsedRegeneration) throw new InvalidOperationException("Expected exactly one advisory regeneration.");
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