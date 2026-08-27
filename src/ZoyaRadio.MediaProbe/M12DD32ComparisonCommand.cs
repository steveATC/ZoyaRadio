namespace ZoyaRadio.MediaProbe;

internal static class M12DD32ComparisonCommand
{
    internal static async Task<int> RunAsync()
    {
        Console.WriteLine("ZOYA RADIO - M12D D3.2 QUICK-REACTION COMPARISON");
        Console.WriteLine("GENERATE ONLY: no Firefox, media session, audio, speech, GUI, or normal radio runtime is used.");

        if (!Directory.Exists(Path.Combine(Directory.GetCurrentDirectory(), ".git")))
        {
            Console.Error.WriteLine("The comparison must run from the repository root.");
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

        M12DD32RunResult result;
        try
        {
            result = await new M12DD32ComparisonRunner(client).RunAsync(cancellation.Token);
        }
        finally
        {
            Console.CancelKeyPress -= cancelHandler;
        }

        if (result.AttemptsDetail.Count == 32)
        {
            string directory = Path.Combine(
                Directory.GetCurrentDirectory(),
                "artifacts",
                "m12d-d3-2",
                DateTimeOffset.Now.ToString("yyyyMMdd-HHmmss-fff"));
            M12DD32ComparisonLaboratory.WriteArtifacts(directory, result);
            Console.WriteLine("Artifacts: " + directory);
        }

        Console.WriteLine(
            result.Completed
                ? "D3.2 comparison generation completed; Steve review is required."
                : $"D3.2 comparison completed with {result.Failures} failed attempt(s).");
        Console.WriteLine($"Attempts: {result.AttemptsDetail.Count}/32; model unload: {(result.UnloadResult.Success ? "success" : "failed")}.");
        return result.Completed ? 0 : 1;
    }
}
