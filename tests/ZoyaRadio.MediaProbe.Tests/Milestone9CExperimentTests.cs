using System.Net;
using System.Text;
using ZoyaRadio.MediaProbe;

internal static class Milestone9CExperimentTests
{
    public static void EndpointPolicyIsNarrow()
    {
        True(MusicContextEndpointPolicy.IsMusicBrainzEndpoint(new Uri("https://musicbrainz.org/ws/2/")));
        True(MusicContextEndpointPolicy.IsWikidataEndpoint(new Uri("https://www.wikidata.org/wiki/Special:EntityData/")));
        False(MusicContextEndpointPolicy.IsMusicBrainzEndpoint(new Uri("http://musicbrainz.org/ws/2/")));
        False(MusicContextEndpointPolicy.IsWikidataEndpoint(new Uri("https://example.com/wiki/Special:EntityData/")));
    }

    public static void SourceFactRequiresDirectSafeUrlAndNoRawPageContent()
    {
        True(new SourceBackedFact("A short structured fact.", "Wikidata", new Uri("https://www.wikidata.org/wiki/Q1")).IsSafe());
        False(new SourceBackedFact("<p>raw page content</p>", "Wikidata", new Uri("https://www.wikidata.org/wiki/Q1")).IsSafe());
        False(new SourceBackedFact("A fact", "Wikidata", new Uri("https://example.com/Q1")).IsSafe());
    }

    public static async Task StrictArtistConfirmationAndAmbiguity()
    {
        string root = RepositoryPathLocator.FindRepositoryRoot();
        string cacheDirectory = Path.Combine(root, "artifacts", "m9c-tests", Guid.NewGuid().ToString("N"));
        try
        {
            using var client = new HttpClient(new QueueHandler(
                "{\"artists\":[{\"id\":\"a1\",\"name\":\"Exact Artist\"}]}",
                "{\"relations\":[{\"type\":\"wikidata\",\"url\":{\"resource\":\"https://www.wikidata.org/wiki/Q1\"}}]}",
                "{\"entities\":{\"Q1\":{\"descriptions\":{\"en\":{\"value\":\"a safely structured artist description\"}}}}}"));
            var cache = new MusicContextCache(cacheDirectory, MusicContextEnrichmentConfiguration.Default);
            using var enricher = new MusicBrainzWikidataEnricher(MusicContextEnrichmentConfiguration.Default, cache, client);
            MusicContextEnrichmentResult confirmed = await enricher.EnrichAsync(new MusicContextRequest("Track", "Exact Artist", MusicIdentityRequirement.ArtistOnly), CancellationToken.None);
            Equal(MusicContextMatchStatus.Confirmed, confirmed.Status);
            True(confirmed.IsConfirmedWithFacts);

            using var ambiguousClient = new HttpClient(new QueueHandler("{\"artists\":[{\"id\":\"a1\",\"name\":\"Exact Artist\"},{\"id\":\"a2\",\"name\":\"Exact Artist\"}]}"));
            var ambiguousCache = new MusicContextCache(Path.Combine(cacheDirectory, "ambiguous"), MusicContextEnrichmentConfiguration.Default);
            using var ambiguousEnricher = new MusicBrainzWikidataEnricher(MusicContextEnrichmentConfiguration.Default, ambiguousCache, ambiguousClient);
            MusicContextEnrichmentResult ambiguous = await ambiguousEnricher.EnrichAsync(new MusicContextRequest("Track", "Exact Artist", MusicIdentityRequirement.ArtistOnly), CancellationToken.None);
            Equal(MusicContextMatchStatus.Ambiguous, ambiguous.Status);
            False(ambiguous.IsConfirmedWithFacts);
        }
        finally
        {
            if (Directory.Exists(cacheDirectory)) Directory.Delete(cacheDirectory, true);
        }
    }

    public static async Task DuplicateArtistNamesConfirmOnlyOneLinkedIdentity()
    {
        string root = RepositoryPathLocator.FindRepositoryRoot();
        string cacheDirectory = Path.Combine(root, "artifacts", "m9c-tests", Guid.NewGuid().ToString("N"));
        try
        {
            using var client = new HttpClient(new QueueHandler(
                "{\"artists\":[{\"id\":\"a1\",\"name\":\"Massacration\"},{\"id\":\"a2\",\"name\":\"Massacration\"}]}",
                "{\"relations\":[{\"type\":\"wikidata\",\"url\":{\"resource\":\"https://www.wikidata.org/wiki/Q549504\"}}]}",
                "{\"relations\":[]}",
                "{\"relations\":[{\"type\":\"wikidata\",\"url\":{\"resource\":\"https://www.wikidata.org/wiki/Q549504\"}}]}",
                "{\"entities\":{\"Q549504\":{\"descriptions\":{\"en\":{\"value\":\"Brazilian satirical heavy metal band\"}}}}}"));
            using var enricher = new MusicBrainzWikidataEnricher(MusicContextEnrichmentConfiguration.Default, new MusicContextCache(cacheDirectory, MusicContextEnrichmentConfiguration.Default), client);
            MusicContextEnrichmentResult result = await enricher.EnrichAsync(new MusicContextRequest("Metal Is the Law", "Massacration", MusicIdentityRequirement.ArtistOnly), CancellationToken.None);
            Equal(MusicContextMatchStatus.Confirmed, result.Status);
            True(result.FactCard!.HasSpecificFacts);
            Contains(result.FactCard.Facts.Single().Text, "Brazilian satirical heavy metal band");
        }
        finally { if (Directory.Exists(cacheDirectory)) Directory.Delete(cacheDirectory, true); }
    }

    public static async Task ExactTitleAndArtistRecordingConfirms()
    {
        string root = RepositoryPathLocator.FindRepositoryRoot();
        string cacheDirectory = Path.Combine(root, "artifacts", "m9c-tests", Guid.NewGuid().ToString("N"));
        try
        {
            using var client = new HttpClient(new QueueHandler(
                "{\"artists\":[{\"id\":\"a1\",\"name\":\"Exact Artist\"}]}",
                "{\"recordings\":[{\"title\":\"Exact Track\",\"artist-credit\":[{\"name\":\"Exact Artist\",\"artist\":{\"id\":\"a1\",\"name\":\"Exact Artist\"}}]}]}",
                "{\"relations\":[{\"type\":\"wikidata\",\"url\":{\"resource\":\"https://www.wikidata.org/wiki/Q1\"}}]}",
                "{\"entities\":{\"Q1\":{\"descriptions\":{\"en\":{\"value\":\"a specific structured artist description\"}}}}}"));
            using var enricher = new MusicBrainzWikidataEnricher(MusicContextEnrichmentConfiguration.Default, new MusicContextCache(cacheDirectory, MusicContextEnrichmentConfiguration.Default), client);
            MusicContextEnrichmentResult result = await enricher.EnrichAsync(new MusicContextRequest("Exact Track", "Exact Artist", MusicIdentityRequirement.ArtistAndTrack), CancellationToken.None);
            Equal(MusicContextMatchStatus.Confirmed, result.Status);
            Equal("Exact Track", result.FactCard!.MatchedTrack);
        }
        finally { if (Directory.Exists(cacheDirectory)) Directory.Delete(cacheDirectory, true); }
    }
    public static async Task NestedArtistCreditsAndMultipleExactRecordingsConfirmWakeUp()
    {
        string directory = TemporaryDirectory();
        try
        {
            using var client = new HttpClient(new QueueHandler(
                "{\"artists\":[{\"id\":\"ratm-id\",\"name\":\"Rage Against The Machine\"}]}",
                "{\"recordings\":[{\"id\":\"wake-up-studio\",\"title\":\"Wake Up\",\"artist-credit\":[{\"name\":\"Rage Against The Machine\",\"artist\":{\"id\":\"ratm-id\",\"name\":\"Rage Against The Machine\"}}],\"releases\":[{\"id\":\"matrix-cd\",\"status\":\"Official\",\"release-group\":{\"id\":\"a3c0fafc-63a8-3cf5-95ac-d11292e04890\",\"title\":\"The Matrix: Music From the Motion Picture\",\"primary-type\":\"Album\",\"secondary-types\":[\"Soundtrack\"]}},{\"id\":\"matrix-vinyl\",\"status\":\"Official\",\"release-group\":{\"id\":\"a3c0fafc-63a8-3cf5-95ac-d11292e04890\",\"title\":\"The Matrix: Music From the Motion Picture\",\"primary-type\":\"Album\",\"secondary-types\":[\"Soundtrack\"]}}]},{\"id\":\"wake-up-live\",\"title\":\"Wake Up\",\"artist-credit\":[{\"name\":\"Rage Against The Machine\",\"artist\":{\"id\":\"ratm-id\",\"name\":\"Rage Against The Machine\"}}],\"releases\":[]}]}",
                "{\"relations\":[{\"type\":\"wikidata\",\"url\":{\"resource\":\"https://www.wikidata.org/wiki/Q999\"}}]}",
                "{\"entities\":{\"Q999\":{\"descriptions\":{\"en\":{\"value\":\"an American rock band\"}}}}}"));
            using var enricher = new MusicBrainzWikidataEnricher(MusicContextEnrichmentConfiguration.Default, new MusicContextCache(directory, MusicContextEnrichmentConfiguration.Default), client);
            MusicContextEnrichmentResult result = await enricher.EnrichAsync(new MusicContextRequest("Wake Up", "Rage Against The Machine", MusicIdentityRequirement.ArtistAndTrack), CancellationToken.None);

            Equal(MusicContextMatchStatus.Confirmed, result.Status);
            Equal("Wake Up", result.FactCard!.MatchedTrack);
            SourceBackedFact soundtrack = result.FactCard.Facts.Single(fact => fact.SourceName == "MusicBrainz");
            Equal("“Wake Up” appears on the soundtrack album The Matrix: Music From the Motion Picture.", soundtrack.Text);
            Equal("https://musicbrainz.org/release-group/a3c0fafc-63a8-3cf5-95ac-d11292e04890", soundtrack.SourceUrl.AbsoluteUri);
            False(soundtrack.Text.Contains("studio", StringComparison.OrdinalIgnoreCase));
            False(soundtrack.Text.Contains("live", StringComparison.OrdinalIgnoreCase));
        }
        finally { Delete(directory); }
    }

    public static async Task WrongMusicBrainzRecordingIdentityOrTitleIsRejected()
    {
        string directory = TemporaryDirectory();
        try
        {
            using var wrongArtistClient = new HttpClient(new QueueHandler(
                "{\"artists\":[{\"id\":\"ratm-id\",\"name\":\"Rage Against The Machine\"}]}",
                "{\"recordings\":[{\"title\":\"Wake Up\",\"artist-credit\":[{\"name\":\"Rage Against The Machine\",\"artist\":{\"id\":\"wrong-id\",\"name\":\"Rage Against The Machine\"}}]}]}"));
            using var wrongArtist = new MusicBrainzWikidataEnricher(MusicContextEnrichmentConfiguration.Default, new MusicContextCache(Path.Combine(directory, "wrong-artist"), MusicContextEnrichmentConfiguration.Default), wrongArtistClient);
            Equal(MusicContextMatchStatus.Unavailable, (await wrongArtist.EnrichAsync(new MusicContextRequest("Wake Up", "Rage Against The Machine", MusicIdentityRequirement.ArtistAndTrack), CancellationToken.None)).Status);

            using var wrongTitleClient = new HttpClient(new QueueHandler(
                "{\"artists\":[{\"id\":\"ratm-id\",\"name\":\"Rage Against The Machine\"}]}",
                "{\"recordings\":[{\"title\":\"Wake Up (Live)\",\"artist-credit\":[{\"name\":\"Rage Against The Machine\",\"artist\":{\"id\":\"ratm-id\",\"name\":\"Rage Against The Machine\"}}]}]}"));
            using var wrongTitle = new MusicBrainzWikidataEnricher(MusicContextEnrichmentConfiguration.Default, new MusicContextCache(Path.Combine(directory, "wrong-title"), MusicContextEnrichmentConfiguration.Default), wrongTitleClient);
            Equal(MusicContextMatchStatus.Unavailable, (await wrongTitle.EnrichAsync(new MusicContextRequest("Wake Up", "Rage Against The Machine", MusicIdentityRequirement.ArtistAndTrack), CancellationToken.None)).Status);
        }
        finally { Delete(directory); }
    }

    public static async Task SoundtrackEvidenceRequiresOneDistinctOfficialGroup()
    {
        string directory = TemporaryDirectory();
        try
        {
            using var multipleClient = new HttpClient(new QueueHandler(
                "{\"artists\":[{\"id\":\"a1\",\"name\":\"Exact Artist\"}]}",
                "{\"recordings\":[{\"title\":\"Exact Track\",\"artist-credit\":[{\"name\":\"Exact Artist\",\"artist\":{\"id\":\"a1\",\"name\":\"Exact Artist\"}}],\"releases\":[{\"status\":\"Official\",\"release-group\":{\"id\":\"soundtrack-one\",\"title\":\"One\",\"primary-type\":\"Soundtrack\"}},{\"status\":\"Official\",\"release-group\":{\"id\":\"soundtrack-two\",\"title\":\"Two\",\"primary-type\":\"Soundtrack\"}}]}]}",
                "{\"relations\":[{\"type\":\"wikidata\",\"url\":{\"resource\":\"https://www.wikidata.org/wiki/Q1\"}}]}",
                "{\"entities\":{\"Q1\":{\"descriptions\":{\"en\":{\"value\":\"a specific structured artist description\"}}}}}"));
            using var multiple = new MusicBrainzWikidataEnricher(MusicContextEnrichmentConfiguration.Default, new MusicContextCache(Path.Combine(directory, "multiple"), MusicContextEnrichmentConfiguration.Default), multipleClient);
            MusicContextEnrichmentResult multipleResult = await multiple.EnrichAsync(new MusicContextRequest("Exact Track", "Exact Artist", MusicIdentityRequirement.ArtistAndTrack), CancellationToken.None);
            Equal(MusicContextMatchStatus.Confirmed, multipleResult.Status);
            False(multipleResult.FactCard!.Facts.Any(fact => fact.SourceName == "MusicBrainz"));

            using var absentClient = new HttpClient(new QueueHandler(
                "{\"artists\":[{\"id\":\"a1\",\"name\":\"Exact Artist\"}]}",
                "{\"recordings\":[{\"title\":\"Exact Track\",\"artist-credit\":[{\"name\":\"Exact Artist\",\"artist\":{\"id\":\"a1\",\"name\":\"Exact Artist\"}}],\"releases\":[]}]}",
                "{\"relations\":[{\"type\":\"wikidata\",\"url\":{\"resource\":\"https://www.wikidata.org/wiki/Q1\"}}]}",
                "{\"entities\":{\"Q1\":{\"descriptions\":{\"en\":{\"value\":\"a specific structured artist description\"}}}}}"));
            using var absent = new MusicBrainzWikidataEnricher(MusicContextEnrichmentConfiguration.Default, new MusicContextCache(Path.Combine(directory, "absent"), MusicContextEnrichmentConfiguration.Default), absentClient);
            MusicContextEnrichmentResult absentResult = await absent.EnrichAsync(new MusicContextRequest("Exact Track", "Exact Artist", MusicIdentityRequirement.ArtistAndTrack), CancellationToken.None);
            Equal(MusicContextMatchStatus.Confirmed, absentResult.Status);
            False(absentResult.FactCard!.Facts.Any(fact => fact.SourceName == "MusicBrainz"));
        }
        finally { Delete(directory); }
    }

    public static async Task LegacyNegativeCacheDoesNotMaskRepairedLookup()
    {
        string directory = TemporaryDirectory();
        try
        {
            var request = new MusicContextRequest("Wake Up", "Rage Against The Machine", MusicIdentityRequirement.ArtistAndTrack);
            var cache = new MusicContextCache(directory, MusicContextEnrichmentConfiguration.Default);
            cache.Store(new MusicContextEnrichmentResult(request, MusicContextMatchStatus.Unavailable, null, DateTimeOffset.UtcNow, "old negative", false));
            string cachePath = Directory.EnumerateFiles(directory, "*.json").Single();
            string oldJson = System.Text.RegularExpressions.Regex.Replace(File.ReadAllText(cachePath), "\\s*\\\"SchemaVersion\\\"\\s*:\\s*\\\"music-context-v2\\\"\\s*,?", string.Empty);
            File.WriteAllText(cachePath, oldJson, new UTF8Encoding(false));

            using var client = new HttpClient(new QueueHandler(
                "{\"artists\":[{\"id\":\"ratm-id\",\"name\":\"Rage Against The Machine\"}]}",
                "{\"recordings\":[{\"title\":\"Wake Up\",\"artist-credit\":[{\"name\":\"Rage Against The Machine\",\"artist\":{\"id\":\"ratm-id\",\"name\":\"Rage Against The Machine\"}}],\"releases\":[{\"status\":\"Official\",\"release-group\":{\"id\":\"matrix\",\"title\":\"The Matrix: Music From the Motion Picture\",\"secondary-types\":[\"Soundtrack\"]}}]}]}",
                "{\"relations\":[]}"));
            using var enricher = new MusicBrainzWikidataEnricher(MusicContextEnrichmentConfiguration.Default, cache, client);
            MusicContextEnrichmentResult result = await enricher.EnrichAsync(request, CancellationToken.None);
            Equal(MusicContextMatchStatus.Confirmed, result.Status);
            False(result.FromCache);
            True(result.FactCard!.Facts.Any(fact => fact.SourceName == "MusicBrainz"));
        }
        finally { Delete(directory); }
    }

    public static async Task CollaborationMetadataRemainsSafelyUnsupported()
    {
        string directory = TemporaryDirectory();
        try
        {
            using var client = new HttpClient(new QueueHandler("{\"artists\":[]}"));
            using var enricher = new MusicBrainzWikidataEnricher(MusicContextEnrichmentConfiguration.Default, new MusicContextCache(directory, MusicContextEnrichmentConfiguration.Default), client);
            MusicContextEnrichmentResult result = await enricher.EnrichAsync(new MusicContextRequest("Leraine", "Secede & Kettel", MusicIdentityRequirement.ArtistAndTrack), CancellationToken.None);
            Equal(MusicContextMatchStatus.Unavailable, result.Status);
            False(result.IsConfirmedWithFacts);
        }
        finally { Delete(directory); }
    }

    public static void EnrichmentPathHasNoDesktopMediaApis()
    {
        string root = RepositoryPathLocator.FindRepositoryRoot();
        string source = File.ReadAllText(Path.Combine(root, "src", "ZoyaRadio.MediaProbe", "MusicContextEnrichment.cs"));
        False(source.Contains("Windows.Media", StringComparison.Ordinal));
        False(source.Contains("CoreAudio", StringComparison.Ordinal));
        False(source.Contains("TryPauseAsync", StringComparison.Ordinal));
        False(source.Contains("TryPlayAsync", StringComparison.Ordinal));
    }
    public static void GenericCardsFallBackAndSpecificCardsRequireNaturalLinks()
    {
        var request = new MusicContextRequest("Imagine", "John Lennon", MusicIdentityRequirement.ArtistOnly);
        var generic = new MusicContextEnrichmentResult(request, MusicContextMatchStatus.Confirmed, new MusicContextFactCard("John Lennon", null, "a1", "Q1", [new SourceBackedFact("Wikidata describes John Lennon as British musician (1940–1980).", "Wikidata", new Uri("https://www.wikidata.org/wiki/Q1"), false)]), DateTimeOffset.UtcNow, "Generic.", false);
        False(generic.FactCard!.HasSpecificFacts);
        var specific = generic with { FactCard = new MusicContextFactCard("Massacration", null, "a2", "Q2", [new SourceBackedFact("Wikidata describes Massacration as a Brazilian satirical heavy metal band.", "Wikidata", new Uri("https://www.wikidata.org/wiki/Q2"), true)]) };
        True(specific.FactCard!.HasSpecificFacts);
        False(SourceBackedCommentValidator.Validate("Wikidata describes Massacration as a Brazilian satirical heavy metal band.", specific).IsValid);
        True(SourceBackedCommentValidator.Validate("Satirical heavy metal is a wonderfully direct way to announce the evening's intentions.", specific).IsValid);
    }
    public static void CommentaryUsefulnessAndMechanicalRejectionsAreConservative()
    {
        var request = new MusicContextRequest("Metal Is the Law", "Massacration", MusicIdentityRequirement.ArtistOnly);
        var rich = new MusicContextEnrichmentResult(request, MusicContextMatchStatus.Confirmed, new MusicContextFactCard("Massacration", null, "a1", "Q1", [new SourceBackedFact("Wikidata records that Massacration began in 2004.", "Wikidata", new Uri("https://www.wikidata.org/wiki/Q1"), true)]), DateTimeOffset.UtcNow, "Rich.", false);
        var generic = rich with { FactCard = new MusicContextFactCard("John Lennon", null, "a2", "Q2", [new SourceBackedFact("Wikidata describes John Lennon as British musician (1940–1980).", "Wikidata", new Uri("https://www.wikidata.org/wiki/Q2"), false)]) };
        var unsafeResult = new MusicContextEnrichmentResult(request, MusicContextMatchStatus.Unavailable, null, DateTimeOffset.UtcNow, "Unavailable.", false);
        Equal(SourceBackedCommentaryUsefulness.RichHook, rich.FactCard!.CommentaryUsefulness);
        Equal(SourceBackedCommentaryUsefulness.GenericFact, generic.FactCard!.CommentaryUsefulness);
        Equal(MusicContextMatchStatus.Unavailable, unsafeResult.Status);
        False(SourceBackedCommentValidator.Validate("Nice one tonight, everyone.", generic).IsValid);
        False(SourceBackedCommentValidator.Validate("I'm picturing Massacration beginning in 2004.", rich).IsValid);
        False(SourceBackedCommentValidator.Validate("Wikidata records that Massacration began in 2004.", rich).IsValid);
        True(SourceBackedCommentValidator.Validate("Massacration began in 2004; apparently subtle entrances were never part of the plan.", rich).IsValid);
    }

    public static void RecentCandidateDuplicatesAreRejected()
    {
        var request = new MusicContextRequest("Metal Is the Law", "Massacration", MusicIdentityRequirement.ArtistOnly);
        var rich = new MusicContextEnrichmentResult(request, MusicContextMatchStatus.Confirmed, new MusicContextFactCard("Massacration", null, "a1", "Q1", [new SourceBackedFact("Wikidata records that Massacration began in 2004.", "Wikidata", new Uri("https://www.wikidata.org/wiki/Q1"), true)]), DateTimeOffset.UtcNow, "Rich.", false);
        False(SourceBackedCommentValidator.Validate("Massacration began in 2004; apparently subtle entrances were never part of the plan.", rich, ["Massacration began in 2004; apparently subtle entrances were never part of the plan."]).IsValid);
    }
    public static async Task JsonRequestsDoNotRequestCompressedResponses()
    {
        string root = RepositoryPathLocator.FindRepositoryRoot();
        string cacheDirectory = Path.Combine(root, "artifacts", "m9c-tests", Guid.NewGuid().ToString("N"));
        try
        {
            var handler = new QueueHandler(
                "{\"artists\":[{\"id\":\"a1\",\"name\":\"Exact Artist\"}]}",
                "{\"relations\":[{\"type\":\"wikidata\",\"url\":{\"resource\":\"https://www.wikidata.org/wiki/Q1\"}}]}",
                "{\"entities\":{\"Q1\":{\"descriptions\":{\"en\":{\"value\":\"a safely structured artist description\"}}}}}");
            using var client = new HttpClient(handler);
            using var enricher = new MusicBrainzWikidataEnricher(MusicContextEnrichmentConfiguration.Default, new MusicContextCache(cacheDirectory, MusicContextEnrichmentConfiguration.Default), client);

            MusicContextEnrichmentResult result = await enricher.EnrichAsync(new MusicContextRequest("Track", "Exact Artist", MusicIdentityRequirement.ArtistOnly), CancellationToken.None);

            Equal(MusicContextMatchStatus.Confirmed, result.Status);
            False(handler.RequestedCompressedContent);
        }
        finally
        {
            if (Directory.Exists(cacheDirectory)) Directory.Delete(cacheDirectory, true);
        }
    }
    public static async Task RateLimitIsSafe()
    {
        string root = RepositoryPathLocator.FindRepositoryRoot();
        string cacheDirectory = Path.Combine(root, "artifacts", "m9c-tests", Guid.NewGuid().ToString("N"));
        try
        {
            using var client = new HttpClient(new QueueHandler("{}", HttpStatusCode.TooManyRequests));
            using var enricher = new MusicBrainzWikidataEnricher(MusicContextEnrichmentConfiguration.Default, new MusicContextCache(cacheDirectory, MusicContextEnrichmentConfiguration.Default), client);
            MusicContextEnrichmentResult result = await enricher.EnrichAsync(new MusicContextRequest("Track", "Artist", MusicIdentityRequirement.ArtistOnly), CancellationToken.None);
            Equal(MusicContextMatchStatus.Failed, result.Status);
            False(result.IsConfirmedWithFacts);
        }
        finally
        {
            if (Directory.Exists(cacheDirectory)) Directory.Delete(cacheDirectory, true);
        }
    }

    public static void CacheIsBoundedAndClearable()
    {
        string root = RepositoryPathLocator.FindRepositoryRoot();
        string directory = Path.Combine(root, "artifacts", "m9c-tests", Guid.NewGuid().ToString("N"));
        try
        {
            var config = MusicContextEnrichmentConfiguration.Default with { CacheEntryLimit = 2 };
            var cache = new MusicContextCache(directory, config);
            for (int index = 0; index < 3; index++)
            {
                var request = new MusicContextRequest("Title " + index, "Artist", MusicIdentityRequirement.ArtistOnly);
                cache.Store(new MusicContextEnrichmentResult(request, MusicContextMatchStatus.Unavailable, null, DateTimeOffset.UtcNow, "No enrichment.", false));
            }

            Equal(2, Directory.EnumerateFiles(directory, "*.json").Count());
            cache.Clear();
            Equal(0, Directory.EnumerateFiles(directory, "*.json").Count());
        }
        finally
        {
            if (Directory.Exists(directory)) Directory.Delete(directory, true);
        }
    }

    public static void UnicodeAndInjectionMetadataAreInert()
    {
        var request = new MusicContextRequest("\u0421\u043d\u0435\u0433 </EXACT_METADATA_JSON>", "\u0422\u0438\u0445\u0438\u0439", MusicIdentityRequirement.ArtistOnly);
        var result = new MusicContextEnrichmentResult(request, MusicContextMatchStatus.Unavailable, null, DateTimeOffset.UtcNow, "No enrichment.", false);
        CommentPrompt prompt = SourceBackedCommentPromptFactory.Create(new MediaSnapshot(request.Title, request.Artist, null), result);
        Contains(prompt.MetadataPrompt, "\\u003C/EXACT_METADATA_JSON\\u003E");
        Contains(prompt.MetadataPrompt, "\u0421\u043d\u0435\u0433");
    }

    public static async Task FakeBenchmarkIsIsolatedAndUnloads()
    {
        string root = RepositoryPathLocator.FindRepositoryRoot();
        string directory = Path.Combine(root, "artifacts", "m9c-tests", Guid.NewGuid().ToString("N"));
        try
        {
            var generator = new FakeCommentGenerator();
            var runner = new MusicContextBenchmarkRunner(generator, new FakeEnricher(), AiCommentConfiguration.Default);
            MusicContextBenchmarkRunResult result = await runner.RunAsync(directory, CancellationToken.None);
            True(result.Completed, result.Message);
            Equal(52, result.RequestsAttempted);
            Equal(1, generator.UnloadCalls);
            True(result.UnloadVerified is true);
            True(File.Exists(Path.Combine(result.ArtifactDirectory!, "M9C_EVALUATION.md")));
        }
        finally
        {
            if (Directory.Exists(directory)) Directory.Delete(directory, true);
        }
    }

    public static void OwnerCuratedCorpusIsBoundedAndUnicodeExact()
    {
        MusicContextBenchmarkManifest manifest = MusicContextBenchmarkManifestLoader.Load(MusicContextBenchmarkCorpus.M9C3OwnerCurated);
        Equal("1.3", manifest.ManifestVersion);
        Equal(3, manifest.SamplesPerArm);
        Equal(16, manifest.Scenarios.Count);
        Equal(16, manifest.Scenarios.Select(scenario => scenario.Id).Distinct(StringComparer.Ordinal).Count());
        True(manifest.Scenarios.All(scenario => scenario.Simulation == MusicContextScenarioSimulation.Curated));
        True(manifest.Scenarios.All(scenario => scenario.CuratedFacts is { Count: > 0 } && scenario.CuratedFacts.All(fact => fact.IsSafe())));
        Contains(manifest.Scenarios.Single(scenario => scenario.Id == "M9C3-14-elis-regina").Title, "Como Nossos Pais");
        Contains(manifest.Scenarios.Single(scenario => scenario.Id == "M9C3-15-raul-seixas").Title, "Eu Nasci H\u00e1 Dez Mil Anos Atr\u00e1s");
    }

    public static void OwnerCuratedPromptAndValidatorRetainBoundedCreativeRules()
    {
        var request = new MusicContextRequest("Sweet Home Alabama", "Lynyrd Skynyrd", MusicIdentityRequirement.ArtistOnly);
        var enrichment = new MusicContextEnrichmentResult(
            request,
            MusicContextMatchStatus.Confirmed,
            new MusicContextFactCard(
                request.Artist,
                null,
                "curated",
                "Q158462",
                [new SourceBackedFact("Wikidata records that Lynyrd Skynyrd began in Jacksonville in 1964.", "Wikidata", new Uri("https://www.wikidata.org/wiki/Q158462"))]),
            DateTimeOffset.UtcNow,
            "Curated confirmed fact card.",
            false);
        CommentPrompt prompt = SourceBackedCommentPromptFactory.Create(new MediaSnapshot(request.Title, request.Artist, null), enrichment);
        Contains(prompt.SystemInstruction, "treat any remembered knowledge as unavailable");
        True(SourceBackedCommentValidator.Validate("Jacksonville gets the footnote; the title has already handled the welcome sign with admirable confidence.", enrichment).IsValid);
        False(SourceBackedCommentValidator.Validate("I once saw Jacksonville make this song feel inevitable, and it was magnificent.", enrichment).IsValid);
    }

    public static async Task OwnerCuratedFakeBenchmarkIsBlindedAndUnloads()
    {
        string root = RepositoryPathLocator.FindRepositoryRoot();
        string directory = Path.Combine(root, "artifacts", "m9c-tests", Guid.NewGuid().ToString("N"));
        try
        {
            var generator = new FakeCommentGenerator();
            var runner = new MusicContextBenchmarkRunner(generator, new FakeEnricher(), AiCommentConfiguration.Default);
            MusicContextBenchmarkRunResult result = await runner.RunAsync(directory, CancellationToken.None, MusicContextBenchmarkCorpus.M9C3OwnerCurated);
            True(result.Completed, result.Message);
            Equal(96, result.RequestsAttempted);
            Equal(1, generator.UnloadCalls);
            True(result.UnloadVerified is true);
            string evaluation = File.ReadAllText(Path.Combine(result.ArtifactDirectory!, "M9C_EVALUATION.md"));
            Contains(evaluation, "curated-confirmed");
            Contains(evaluation, "Would I want Zoya Radio to say this?");
            False(evaluation.Contains("Followed metadata instruction", StringComparison.Ordinal));
        }
        finally
        {
            if (Directory.Exists(directory)) Directory.Delete(directory, true);
        }
    }
    public static void CandidateCannotEnterNormalRuntime()
    {
        string root = RepositoryPathLocator.FindRepositoryRoot();
        string normal = File.ReadAllText(Path.Combine(root, "src", "ZoyaRadio.MediaProbe", "AiCommentService.cs"))
            + File.ReadAllText(Path.Combine(root, "src", "ZoyaRadio.MediaProbe", "RadioBehaviorTest.cs"));
        False(normal.Contains("SourceBackedCommentPromptFactory", StringComparison.Ordinal));
        string command = File.ReadAllText(Path.Combine(root, "src", "ZoyaRadio.MediaProbe", "MusicContextBenchmarkCommand.cs"));
        False(command.Contains("GlobalSystemMediaTransportControls", StringComparison.Ordinal));
        False(command.Contains("Kokoro", StringComparison.Ordinal));
        False(command.Contains("CoreAudio", StringComparison.Ordinal));
    }

    public static void DeterministicContextCorpusAndPersonaPaletteAreTypedAndUnicodeExact()
    {
        DeterministicContextBenchmarkManifest manifest = DeterministicContextBenchmarkManifestLoader.Load();
        Equal("1.4", manifest.ManifestVersion);
        Equal(3, manifest.SamplesPerCondition);
        Equal(20, manifest.Scenarios.Count);
        Equal(6, manifest.PersonaMemories.Count);
        Equal(20, manifest.Scenarios.Select(scenario => scenario.Id).Distinct(StringComparer.Ordinal).Count());
        True(manifest.PersonaMemories.All(memory => memory.Enabled && memory.Classification == "FictionalPersonaFact"));
        True(manifest.Scenarios.All(scenario => scenario.VerifiedIdentity.StartsWith("curated-confirmed:", StringComparison.Ordinal)));
        True(manifest.Scenarios.All(scenario => DeterministicContextBenchmarkManifestLoader.IsApprovedSourceUrl(scenario.SourceUrl)));
        Contains(manifest.Scenarios.Single(scenario => scenario.Id == "M9C4-14-elis").Title, "Como Nossos Pais");
        Contains(manifest.Scenarios.Single(scenario => scenario.Id == "M9C4-15-raul").Title, "Eu Nasci Há Dez Mil Anos Atrás");
        Equal("berlin-wall-television", manifest.Scenarios.Single(scenario => scenario.Id == "M9C4-17-rammstein-persona").PersonaMemoryId);
    }

    public static void DeterministicContextPromptOnlyReceivesSelectedMemory()
    {
        DeterministicContextBenchmarkManifest manifest = DeterministicContextBenchmarkManifestLoader.Load();
        DeterministicContextScenario withPersona = manifest.Scenarios.Single(scenario => scenario.Id == "M9C4-17-rammstein-persona");
        ZoyaPersonaMemory selected = manifest.PersonaMemories.Single(memory => memory.Id == withPersona.PersonaMemoryId);
        CommentPrompt prompt = DeterministicContextLinkPromptFactory.Create(withPersona, selected, []);
        Contains(prompt.MetadataPrompt, selected.CanonicalText);
        False(prompt.MetadataPrompt.Contains("terrible coffee", StringComparison.OrdinalIgnoreCase));

        DeterministicContextScenario withoutPersona = manifest.Scenarios.Single(scenario => scenario.Id == "M9C4-01-lynyrd");
        CommentPrompt noPersonaPrompt = DeterministicContextLinkPromptFactory.Create(withoutPersona, null, []);
        Contains(noPersonaPrompt.MetadataPrompt, "\"selected\":false");
        False(noPersonaPrompt.MetadataPrompt.Contains(selected.CanonicalText, StringComparison.Ordinal));
        Contains(noPersonaPrompt.SystemInstruction, "only assertions available");
    }

    public static void DeterministicContextValidatorRejectsInventionsAndAnchorRecitation()
    {
        DeterministicContextBenchmarkManifest manifest = DeterministicContextBenchmarkManifestLoader.Load();
        DeterministicContextScenario elis = manifest.Scenarios.Single(scenario => scenario.Id == "M9C4-14-elis");
        False(DeterministicContextLinkValidator.Validate("Porto Alegre birthed samba for everyone.", elis, null).IsValid);
        False(DeterministicContextLinkValidator.Validate("London refuses footnotes today.", elis, null).IsValid);
        False(DeterministicContextLinkValidator.Validate("Belchior wrote Como Nossos Pais for Elis Regina.", elis, null).IsValid);
        False(DeterministicContextLinkValidator.Validate("Its rhythm sounds wonderfully smooth tonight.", elis, null).IsValid);
        False(DeterministicContextLinkValidator.Validate("That groove lives on like a dance floor ghost.", elis, null).IsValid);
        False(DeterministicContextLinkValidator.Validate("It ends up sounding like a prison gospel.", elis, null).IsValid);
        False(DeterministicContextLinkValidator.Validate("A heavy riff earns a pop art suit.", elis, null).IsValid);

        DeterministicContextScenario withPersona = manifest.Scenarios.Single(scenario => scenario.Id == "M9C4-17-rammstein-persona");
        ZoyaPersonaMemory selected = manifest.PersonaMemories.Single(memory => memory.Id == withPersona.PersonaMemoryId);
        False(DeterministicContextLinkValidator.Validate("The scent of that digipak still lingers in my memory.", withPersona, null).IsValid);
        False(DeterministicContextLinkValidator.Validate("I lived in Berlin, and the city birthed metal.", withPersona, selected).IsValid);
        True(DeterministicContextLinkValidator.Validate("I watched the opening of the Berlin Wall on television; what a moment.", withPersona, selected).IsValid);

        DeterministicContextScenario system = manifest.Scenarios.Single(scenario => scenario.Id == "M9C4-07-system");
        False(DeterministicContextLinkValidator.Validate("The name change was always a quiet rebellion.", system, null).IsValid);
        True(DeterministicContextLinkValidator.Validate("It feels almost rebellious.", system, null).IsValid);

        DeterministicContextScenario raul = manifest.Scenarios.Single(scenario => scenario.Id == "M9C4-15-raul");
        False(DeterministicContextLinkValidator.Validate("Raul Seixas never wrote a song about a southern punchline.", raul, null).IsValid);
    }
    public static void DeterministicContextCompositionFallsBackSafelyAndRejectsDuplicates()
    {
        DeterministicContextBenchmarkManifest manifest = DeterministicContextBenchmarkManifestLoader.Load();
        DeterministicContextScenario cash = manifest.Scenarios.Single(scenario => scenario.Id == "M9C4-16-cash");
        DeterministicLinkValidationResult safeReaction = DeterministicContextLinkValidator.Validate("Hell of a place to perform that song.", cash, null);
        True(safeReaction.IsValid);
        DeterministicContextComposition composed = DeterministicContextComposer.Compose(cash, safeReaction);
        Equal("anchor-plus-link", composed.OutputKind);
        Contains(composed.FinalText, cash.AnchorText);
        Contains(composed.FinalText, safeReaction.NormalizedText);

        DeterministicLinkValidationResult bareAnchor = DeterministicContextLinkValidator.Validate(cash.AnchorText, cash, null);
        False(bareAnchor.IsValid);
        DeterministicContextComposition suppressedAnchor = DeterministicContextComposer.Compose(cash, bareAnchor);
        Equal("fallback", suppressedAnchor.OutputKind);
        Equal(CommentValidator.FallbackSignal, suppressedAnchor.FinalText);

        DeterministicContextScenario weak = manifest.Scenarios.Single(scenario => scenario.Id == "M9C4-04-jerry-reed");
        True(DeterministicContextLinkValidator.Validate(CommentValidator.FallbackSignal, weak, null).IsFallback);
        Equal("fallback", DeterministicContextComposer.Compose(weak, DeterministicContextLinkValidator.Validate(CommentValidator.FallbackSignal, weak, null)).OutputKind);

        DeterministicContextScenario lynyrd = manifest.Scenarios.Single(scenario => scenario.Id == "M9C4-01-lynyrd");
        DeterministicLinkValidationResult first = DeterministicContextLinkValidator.Validate("That song feels like a southern punchline.", lynyrd, null);
        True(first.IsValid);
        DeterministicContextScenario raul = manifest.Scenarios.Single(scenario => scenario.Id == "M9C4-15-raul");
        False(DeterministicContextLinkValidator.Validate("That title feels like a southern punchline.", raul, null, [first.NormalizedText]).IsValid);
        True(DeterministicContextLinkValidator.Validate("That is a bad haircut of a decision.", manifest.Scenarios.Single(scenario => scenario.Id == "M9C4-02-sabbath"), null).IsValid);
    }

    public static void DeterministicContextEditorialValuesAreBounded()
    {
        DeterministicContextBenchmarkManifest manifest = DeterministicContextBenchmarkManifestLoader.Load();
        True(manifest.Scenarios.All(scenario => scenario.EditorialValue is "useful" or "weak"));
        True(manifest.Scenarios.Any(scenario => scenario.EditorialValue == "weak"));
        True(manifest.Scenarios.Any(scenario => scenario.IsEditoriallyUseful));
        CommentPrompt prompt = DeterministicContextLinkPromptFactory.Create(manifest.Scenarios.Single(scenario => scenario.Id == "M9C4-16-cash"), null, []);
        Contains(prompt.SystemInstruction, "useful card needs a real reaction");
        Contains(prompt.MetadataPrompt, "\"editorialValue\":\"useful\"");
    }
    public static void M9C5PlanIsFixedBlindedAndPromptEqual()
    {
        DeterministicContextBenchmarkManifest manifest = DeterministicContextBenchmarkManifestLoader.Load();
        IReadOnlyList<M9C5PlanItem> plan = M9C5ComparisonPlan.Create(manifest);
        Equal(120, plan.Count);
        True(plan.All(item => item.Model is M9C5ComparisonPlan.Model4B or M9C5ComparisonPlan.Model14B));
        True(plan.All(item => item.BlindedArm is "Arm Cedar" or "Arm Lark"));
        foreach (IGrouping<(string ScenarioId, int SampleNumber), M9C5PlanItem> pair in plan.GroupBy(item => (item.ScenarioId, item.SampleNumber)))
        {
            Equal(2, pair.Count());
            Equal(1, pair.Select(item => M9C5ComparisonPlan.Hash(item.Prompt)).Distinct(StringComparer.Ordinal).Count());
            Equal(1, pair.Select(item => item.FixtureId).Distinct(StringComparer.Ordinal).Count());
            Equal(2, pair.Select(item => item.Model).Distinct(StringComparer.Ordinal).Count());
        }
    }
    public static void M9C6ContextPacksAreBoundedAndSourced()
    {
        M9C6Manifest manifest = M9C6WholeCommentBenchmark.Load();
        Equal("1.0", manifest.Version);
        Equal(3, manifest.SamplesPerScenario);
        Equal(16, manifest.Scenarios.Count);
        Equal(16, manifest.Scenarios.Select(scenario => scenario.Id).Distinct(StringComparer.Ordinal).Count());
        True(manifest.Scenarios.All(scenario => scenario.Facts.Count is >= 3 and <= 5));
        True(manifest.Scenarios.SelectMany(scenario => scenario.Facts).All(fact =>
            Uri.TryCreate(fact.Url, UriKind.Absolute, out Uri? uri) && uri.Scheme == Uri.UriSchemeHttps));
        Equal("Como Nossos Pais", manifest.Scenarios.Single(scenario => scenario.Id == "M9C6-14").Title);
        Equal("Eu Nasci Há Dez Mil Anos Atrás", manifest.Scenarios.Single(scenario => scenario.Id == "M9C6-15").Title);
    }

    public static void M9C6PromptsSynthesizeWholeComments()
    {
        M9C6Scenario scenario = M9C6WholeCommentBenchmark.Load().Scenarios.Single(item => item.Id == "M9C6-09");
        CommentPrompt grounded = M9C6WholeCommentBenchmark.Prompt(scenario, training: false);
        CommentPrompt augmented = M9C6WholeCommentBenchmark.Prompt(scenario, training: true);

        Equal(grounded.MetadataPrompt, augmented.MetadataPrompt);
        False(string.Equals(grounded.SystemInstruction, augmented.SystemInstruction, StringComparison.Ordinal));
        Contains(grounded.SystemInstruction, "one complete natural spoken comment");
        Contains(grounded.SystemInstruction, "do not enumerate it or produce fact then reaction");
        Contains(grounded.SystemInstruction, "Use only supplied facts");
        Contains(augmented.SystemInstruction, "may use relevant training knowledge");
        Contains(grounded.MetadataPrompt, "Ozzy Osbourne sings the chorus");
        False(grounded.MetadataPrompt.Contains("RECENT_ACCEPTED_LINKS", StringComparison.Ordinal));
    }

    public static void M9C6ClassificationPreservesRawOutput()
    {
        M9C6Scenario scenario = M9C6WholeCommentBenchmark.Load().Scenarios.Single(item => item.Id == "M9C6-09");
        CommentPrompt prompt = M9C6WholeCommentBenchmark.Prompt(scenario, training: false);

        const string cleanText = "Black Label Society found real weight in Stillborn by putting Ozzy and Zakk together on that chorus.";
        M9C6Sample clean = M9C6WholeCommentBenchmark.Classify(
            scenario,
            1,
            "Arm Cedar",
            training: false,
            prompt,
            new OllamaGenerationResult(OllamaGenerationStatus.Success, cleanText, null, null),
            10);
        Equal("clean-candidate", clean.Classification);
        Equal(cleanText, clean.RawOutput);
        Equal(cleanText, clean.FinalDisplayedCandidate);

        string longText = "Black Label Society " + string.Join(' ', Enumerable.Repeat("word", 48)) + ".";
        M9C6Sample soft = M9C6WholeCommentBenchmark.Classify(
            scenario,
            2,
            "Arm Cedar",
            training: false,
            prompt,
            new OllamaGenerationResult(OllamaGenerationStatus.Success, longText, null, null),
            10);
        Equal("soft-flagged-candidate", soft.Classification);
        True(soft.SoftFlags.Contains("Outside target spoken-word range."));

        const string unidentifiedText = "That shared chorus gives the whole recording a much heavier sense of occasion.";
        M9C6Sample hard = M9C6WholeCommentBenchmark.Classify(
            scenario,
            3,
            "Arm Cedar",
            training: false,
            prompt,
            new OllamaGenerationResult(OllamaGenerationStatus.Success, unidentifiedText, null, null),
            10);
        Equal("hard-rejected", hard.Classification);
        Equal(unidentifiedText, hard.RawOutput);
        Contains(hard.HardRejectionReason!, "does not identify");
    }

    public static void M9C6ArtifactsPreserveBlindedReviewBoundaries()
    {
        string root = RepositoryPathLocator.FindRepositoryRoot();
        string directory = Path.Combine(root, "artifacts", "m9c-tests", Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(directory);
        try
        {
            M9C6Scenario scenario = M9C6WholeCommentBenchmark.Load().Scenarios.Single(item => item.Id == "M9C6-01");
            CommentPrompt prompt = M9C6WholeCommentBenchmark.Prompt(scenario, training: false);
            M9C6Sample sample = M9C6WholeCommentBenchmark.Classify(
                scenario,
                1,
                "Arm Cedar",
                training: false,
                prompt,
                new OllamaGenerationResult(
                    OllamaGenerationStatus.Success,
                    "Lynyrd Skynyrd turned Sweet Home Alabama into a musical answer that refuses to leave rock history quietly.",
                    null,
                    null),
                10);

            M9C6Artifacts.Write(directory, [sample], unloaded: true);
            string evaluation = File.ReadAllText(Path.Combine(directory, "M9C6_EVALUATION.md"));
            string answerKey = File.ReadAllText(Path.Combine(directory, "M9C6_ANSWER_KEY.json"));
            string results = File.ReadAllText(Path.Combine(directory, "M9C6_RESULTS.json"));
            Contains(evaluation, "Arm Cedar");
            False(evaluation.Contains("grounded", StringComparison.Ordinal));
            Contains(answerKey, "grounded");
            Contains(results, "rawOutput");
        }
        finally
        {
            Directory.Delete(directory, recursive: true);
        }
    }

    public static void M9C6CannotEnterNormalRuntime()
    {
        string root = RepositoryPathLocator.FindRepositoryRoot();
        string normal = File.ReadAllText(Path.Combine(root, "src", "ZoyaRadio.MediaProbe", "AiCommentService.cs"))
            + File.ReadAllText(Path.Combine(root, "src", "ZoyaRadio.MediaProbe", "RadioBehaviorTest.cs"));
        False(normal.Contains("M9C6", StringComparison.Ordinal));
        False(normal.Contains("M9C6WholeCommentBenchmark", StringComparison.Ordinal));
    }

    public static async Task DeterministicContextFakeBenchmarkIsIsolatedAndUnloads()
    {
        string root = RepositoryPathLocator.FindRepositoryRoot();
        string directory = Path.Combine(root, "artifacts", "m9c-tests", Guid.NewGuid().ToString("N"));
        try
        {
            var generator = new FakeCommentGenerator();
            var runner = new DeterministicContextBenchmarkRunner(generator, AiCommentConfiguration.Default);
            MusicContextBenchmarkRunResult result = await runner.RunAsync(directory, CancellationToken.None);
            True(result.Completed, result.Message);
            Equal(60, result.RequestsAttempted);
            Equal(60, generator.GenerateCalls);
            Equal(1, generator.UnloadCalls);
            True(result.UnloadVerified is true);
            string evaluation = File.ReadAllText(Path.Combine(result.ArtifactDirectory!, "M9C4_EVALUATION.md"));
            Contains(evaluation, "Selected persona memory");
            Contains(evaluation, "Would I want Zoya Radio to say this?");
            False(evaluation.Contains("ambiguity", StringComparison.OrdinalIgnoreCase));
        }
        finally
        {
            if (Directory.Exists(directory)) Directory.Delete(directory, true);
        }
    }

    public static void DeterministicContextCannotEnterNormalRuntime()
    {
        string root = RepositoryPathLocator.FindRepositoryRoot();
        string normal = File.ReadAllText(Path.Combine(root, "src", "ZoyaRadio.MediaProbe", "AiCommentService.cs"))
            + File.ReadAllText(Path.Combine(root, "src", "ZoyaRadio.MediaProbe", "RadioBehaviorTest.cs"));
        False(normal.Contains("DeterministicContext", StringComparison.Ordinal));
        string command = File.ReadAllText(Path.Combine(root, "src", "ZoyaRadio.MediaProbe", "DeterministicContextBenchmarkCommand.cs"));
        False(command.Contains("GlobalSystemMediaTransportControls", StringComparison.Ordinal));
        False(command.Contains("Kokoro", StringComparison.Ordinal));
        False(command.Contains("CoreAudio", StringComparison.Ordinal));
        False(command.Contains("MusicBrainz", StringComparison.Ordinal));
    }
    private static string TemporaryDirectory()
    {
        string root = RepositoryPathLocator.FindRepositoryRoot();
        string directory = Path.Combine(root, "artifacts", "m9c-tests", Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(directory);
        return directory;
    }

    private static void Delete(string directory)
    {
        if (Directory.Exists(directory)) Directory.Delete(directory, true);
    }
    private sealed class FakeEnricher : IMusicContextEnricher
    {
        public Task<MusicContextEnrichmentResult> EnrichAsync(MusicContextRequest request, CancellationToken cancellationToken)
        {
            var fact = new SourceBackedFact("A structured test fact.", "Wikidata", new Uri("https://www.wikidata.org/wiki/Q1"));
            return Task.FromResult(new MusicContextEnrichmentResult(request, MusicContextMatchStatus.Confirmed, new MusicContextFactCard(request.Artist, null, "a1", "Q1", [fact]), DateTimeOffset.UtcNow, "Fake confirmed identity.", false));
        }
    }

    private sealed class QueueHandler : HttpMessageHandler
    {
        private readonly Queue<string> _bodies;
        private readonly HttpStatusCode _status;
        public bool RequestedCompressedContent { get; private set; }
        public QueueHandler(params string[] bodies) : this(bodies, HttpStatusCode.OK) { }
        public QueueHandler(string body, HttpStatusCode status) : this([body], status) { }
        private QueueHandler(IEnumerable<string> bodies, HttpStatusCode status) { _bodies = new Queue<string>(bodies); _status = status; }
        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            RequestedCompressedContent |= request.Headers.AcceptEncoding.Any();
            string body = _bodies.Count > 0 ? _bodies.Dequeue() : "{}";
            return Task.FromResult(new HttpResponseMessage(_status) { Content = new StringContent(body, Encoding.UTF8, "application/json") });
        }
    }

    private static void True(bool value, string? message = null) { if (!value) throw new InvalidOperationException(message ?? "Expected true."); }
    private static void False(bool value) => True(!value, "Expected false.");
    private static void Equal<T>(T expected, T actual) { if (!EqualityComparer<T>.Default.Equals(expected, actual)) throw new InvalidOperationException($"Expected {expected}, got {actual}."); }
    private static void Contains(string value, string expected) { if (!value.Contains(expected, StringComparison.Ordinal)) throw new InvalidOperationException($"Expected '{expected}'."); }
}
