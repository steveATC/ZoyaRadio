namespace ZoyaRadio.MediaProbe;

internal static class MusicContextBenchmarkCommand
{
    public static async Task<int> RunAsync(bool clearCacheOnly, MusicContextBenchmarkCorpus corpus = MusicContextBenchmarkCorpus.M9C2SafetyAndFactCards)
    {
        Console.WriteLine(corpus == MusicContextBenchmarkCorpus.M9C3OwnerCurated ? "ZOYA RADIO - MILESTONE 9C.3 OWNER-CURATED CONFIRMED-CONTEXT BENCHMARK" : "ZOYA RADIO - MILESTONE 9C SOURCE-BACKED COMMENTARY BENCHMARK");
        Console.WriteLine("GENERATE ONLY: no Firefox, media, audio, speech, GUI, Core Audio, or normal logs are used.");

        string repositoryRoot = Directory.GetCurrentDirectory();
        if (!Directory.Exists(Path.Combine(repositoryRoot, ".git")))
        {
            Console.Error.WriteLine("Run this command through run-source-backed-commentary-benchmark.cmd from the ZoyaRadio repository.");
            return 1;
        }

        MusicContextEnrichmentConfiguration enrichmentConfiguration = MusicContextEnrichmentConfiguration.Default;
        MusicContextCache cache = new(Path.Combine(repositoryRoot, "artifacts", "m9c", "cache"), enrichmentConfiguration);
        if (clearCacheOnly)
        {
            cache.Clear();
            Console.WriteLine("M9C enrichment cache cleared. No network, Ollama, media, or audio action was used.");
            return 0;
        }

        using var cancellation = new CancellationTokenSource();
        ConsoleCancelEventHandler handler = (_, eventArgs) =>
        {
            eventArgs.Cancel = true;
            cancellation.Cancel();
        };
        Console.CancelKeyPress += handler;
        try
        {
            using var ollama = new OllamaClient(AiCommentConfiguration.Default);
            using var enrichment = new MusicBrainzWikidataEnricher(enrichmentConfiguration, cache);
            var runner = new MusicContextBenchmarkRunner(ollama, enrichment, AiCommentConfiguration.Default);
            MusicContextBenchmarkRunResult result = await runner.RunAsync(repositoryRoot, cancellation.Token, corpus);
            Console.WriteLine(result.Message);
            if (result.ArtifactDirectory is not null)
            {
                Console.WriteLine($"Artifacts: {result.ArtifactDirectory}");
            }

            return result.Completed ? 0 : 1;
        }
        catch (OperationCanceledException)
        {
            Console.Error.WriteLine("M9C benchmark cancelled. The selected Ollama model unload was still requested.");
            return 1;
        }
        catch (Exception exception)
        {
            Console.Error.WriteLine($"M9C benchmark failed safely: {exception.Message}");
            return 1;
        }
        finally
        {
            Console.CancelKeyPress -= handler;
        }
    }
}
