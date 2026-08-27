namespace ZoyaRadio.MediaProbe;

internal static class M12CHostFormatLaboratoryCommand
{
    internal static async Task<int> RunAsync()
    {
        Console.WriteLine("ZOYA RADIO - M12C HOST FORMAT LABORATORY, BATCH 1");
        Console.WriteLine("GENERATE ONLY: no media observation/control, audio changes, GUI, speech, or normal radio runtime is used.");
        Console.WriteLine("The laboratory contacts only the configured local Ollama service.");
        string repositoryRoot = Directory.GetCurrentDirectory();
        if (!Directory.Exists(Path.Combine(repositoryRoot, ".git")))
        {
            Console.Error.WriteLine("Run this laboratory through run-m12c-host-format-lab.cmd from the ZoyaRadio repository.");
            return 1;
        }

        AiCommentConfiguration configuration = AiCommentConfiguration.Default;
        try { configuration.Validate(); }
        catch (Exception exception)
        {
            Console.Error.WriteLine($"M12C laboratory configuration is invalid: {exception.Message}");
            return 1;
        }

        using var cancellation = new CancellationTokenSource();
        ConsoleCancelEventHandler cancelHandler = (_, eventArgs) => { eventArgs.Cancel = true; cancellation.Cancel(); };
        Console.CancelKeyPress += cancelHandler;
        try
        {
            using var ollama = new OllamaClient(configuration);
            var runner = new M12CLaboratoryRunner(ollama, configuration);
            M12CLaboratoryRunResult result = await runner.RunAsync(cancellation.Token);
            if (result.Records.Count == 40)
            {
                string directory = Path.Combine(repositoryRoot, "artifacts", "m12c", DateTimeOffset.Now.ToString("yyyyMMdd-HHmmss-fff"));
                M12CHostFormatLaboratory.WriteArtifacts(directory, result.Records);
                Console.WriteLine("Artifacts: " + directory);
            }
            else
            {
                Console.Error.WriteLine("M12C laboratory stopped before the complete 40-item review set; no review artifact was written.");
            }

            Console.WriteLine(result.Completed
                ? "M12C generation completed. Steve review of the blinded evaluation artifact is required."
                : $"M12C generation finished with {result.LocalFailures} unavailable or refused item(s); review the artifact before deciding whether to rerun.");
            return result.Completed ? 0 : 1;
        }
        catch (Exception exception)
        {
            Console.Error.WriteLine($"M12C laboratory failed safely: {exception.Message}");
            return 1;
        }
        finally { Console.CancelKeyPress -= cancelHandler; }
    }
}
