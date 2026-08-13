namespace ZoyaRadio.MediaProbe;

internal static class CommentaryBenchmarkCommand
{
    public static async Task<int> RunAsync()
    {
        Console.WriteLine("ZOYA RADIO - MILESTONE 9A STRUCTURED COMMENTARY BENCHMARK");
        Console.WriteLine("GENERATE ONLY: no media observation/control, volume changes, GUI, or speech are used.");
        Console.WriteLine("The benchmark contacts only the configured loopback Ollama service.");

        string repositoryRoot = Directory.GetCurrentDirectory();
        if (!Directory.Exists(Path.Combine(repositoryRoot, ".git")))
        {
            Console.Error.WriteLine(
                "Run this benchmark through run-commentary-benchmark.cmd from the ZoyaRadio repository.");
            return 1;
        }

        AiCommentConfiguration configuration = AiCommentConfiguration.Default;
        HostPersonaConfiguration persona = HostPersonaConfiguration.Default;

        try
        {
            configuration.Validate();
            persona.Validate();
        }
        catch (Exception exception)
        {
            Console.Error.WriteLine($"M9A benchmark configuration is invalid: {exception.Message}");
            return 1;
        }

        using var cancellation = new CancellationTokenSource();
        ConsoleCancelEventHandler cancelHandler = (_, eventArgs) =>
        {
            eventArgs.Cancel = true;
            cancellation.Cancel();
        };
        Console.CancelKeyPress += cancelHandler;

        try
        {
            using var ollama = new OllamaClient(configuration);
            var runner = new CommentaryBenchmarkRunner(ollama, configuration, persona);
            CommentaryBenchmarkRunResult result = await runner.RunAsync(
                repositoryRoot,
                cancellation.Token);
            Console.WriteLine(result.Message);
            if (result.ArtifactDirectory is not null)
            {
                Console.WriteLine($"Artifacts: {result.ArtifactDirectory}");
            }

            return result.Completed ? 0 : 1;
        }
        catch (OperationCanceledException)
        {
            Console.Error.WriteLine("M9A benchmark cancelled. Selected-model unload was still requested.");
            return 1;
        }
        catch (Exception exception)
        {
            Console.Error.WriteLine($"M9A benchmark failed safely: {exception.Message}");
            return 1;
        }
        finally
        {
            Console.CancelKeyPress -= cancelHandler;
        }
    }
}
