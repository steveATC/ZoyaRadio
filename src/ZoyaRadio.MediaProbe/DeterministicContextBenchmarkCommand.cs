namespace ZoyaRadio.MediaProbe;

internal static class DeterministicContextBenchmarkCommand
{
    public static async Task<int> RunAsync()
    {
        Console.WriteLine("ZOYA RADIO - MILESTONE 9C.4 DETERMINISTIC-CONTEXT BENCHMARK");
        Console.WriteLine("GENERATE ONLY: no Firefox, media, audio, speech, GUI, Core Audio, enrichment network, or normal logs are used.");

        string repositoryRoot = Directory.GetCurrentDirectory();
        if (!Directory.Exists(Path.Combine(repositoryRoot, ".git")))
        {
            Console.Error.WriteLine("Run this command through run-deterministic-context-benchmark.cmd from the ZoyaRadio repository.");
            return 1;
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
            var runner = new DeterministicContextBenchmarkRunner(ollama, AiCommentConfiguration.Default);
            MusicContextBenchmarkRunResult result = await runner.RunAsync(repositoryRoot, cancellation.Token);
            Console.WriteLine(result.Message);
            if (result.ArtifactDirectory is not null)
            {
                Console.WriteLine($"Artifacts: {result.ArtifactDirectory}");
            }

            return result.Completed ? 0 : 1;
        }
        catch (OperationCanceledException)
        {
            Console.Error.WriteLine("M9C.4 benchmark cancelled. The selected Ollama model unload was still requested.");
            return 1;
        }
        catch (Exception exception)
        {
            Console.Error.WriteLine($"M9C.4 benchmark failed safely: {exception.Message}");
            return 1;
        }
        finally
        {
            Console.CancelKeyPress -= handler;
        }
    }
}