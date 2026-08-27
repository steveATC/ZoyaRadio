namespace ZoyaRadio.MediaProbe;

internal static class M12DQuickReactionCommand
{
    internal static async Task<int> RunAsync()
    {
        Console.WriteLine("ZOYA RADIO - M12D D1 QUICK-REACTION LABORATORY");
        Console.WriteLine("GENERATE ONLY: no Firefox, audio, speech, GUI, or normal radio runtime is used.");
        if (!Directory.Exists(Path.Combine(Directory.GetCurrentDirectory(), ".git"))) return 1;
        using var client = new OllamaClient(AiCommentConfiguration.Default);
        M12DRunResult result = await new M12DQuickReactionRunner(client).RunAsync(CancellationToken.None);
        if (result.Records.Count == 36)
        {
            string directory = Path.Combine(Directory.GetCurrentDirectory(), "artifacts", "m12d", DateTimeOffset.Now.ToString("yyyyMMdd-HHmmss-fff"));
            M12DQuickReactionLaboratory.WriteArtifacts(directory, result.Records);
            Console.WriteLine("Artifacts: " + directory);
        }
        Console.WriteLine(result.Completed ? "D1 generation completed; Steve review is required." : $"D1 completed with {result.Failures} unavailable/refused item(s).");
        return result.Completed ? 0 : 1;
    }
}
