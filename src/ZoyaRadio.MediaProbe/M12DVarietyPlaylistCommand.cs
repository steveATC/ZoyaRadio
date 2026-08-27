namespace ZoyaRadio.MediaProbe;

internal static class M12DVarietyPlaylistCommand
{
    internal static async Task<int> RunAsync()
    {
        Console.WriteLine("ZOYA RADIO - M12D VAR1 SIMULATED-PLAYLIST EVALUATION");
        Console.WriteLine("GENERATE ONLY: no Firefox, media session, audio, speech, GUI, or normal radio runtime is used.");

        if (!Directory.Exists(Path.Combine(Directory.GetCurrentDirectory(), ".git")))
        {
            Console.Error.WriteLine("The evaluation must run from the repository root.");
            return 1;
        }

        using var client = new OllamaClient(AiCommentConfiguration.Default);
        using var cancellation = new CancellationTokenSource();
        ConsoleCancelEventHandler cancelHandler = (_, eventArgs) =>
        {
            eventArgs.Cancel = true;
            cancellation.Cancel();
            Console.Error.WriteLine("Cancellation requested; stopping after the current generation and unloading the selected model.");
        };
        Console.CancelKeyPress += cancelHandler;

        M12DVarietyPlaylistRunResult result;
        try
        {
            result = await new M12DVarietyPlaylistRunner(client).RunAsync(cancellation.Token);
        }
        finally
        {
            Console.CancelKeyPress -= cancelHandler;
        }

        if (result.AttemptsDetail.Count == 24)
        {
            string directory = Path.Combine(
                Directory.GetCurrentDirectory(),
                "artifacts",
                "m12d-var1",
                DateTimeOffset.Now.ToString("yyyyMMdd-HHmmss-fff"));
            M12DVarietyPlaylistLaboratory.WriteArtifacts(directory, result);
            Console.WriteLine("Artifacts: " + directory);
        }

        Console.WriteLine(
            result.Completed
                ? "Simulated-playlist generation completed; Steve review is required."
                : $"Simulated-playlist generation completed with {result.Failures} failed attempt(s).");
        Console.WriteLine($"Attempts: {result.AttemptsDetail.Count}/24; model unload: {(result.UnloadResult.Success ? "success" : "failed")}.");
        return result.Completed ? 0 : 1;
    }
}
