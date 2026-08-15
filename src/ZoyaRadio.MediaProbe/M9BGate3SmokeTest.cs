using System.Diagnostics;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace ZoyaRadio.MediaProbe;

internal sealed record M9BGate3SmokeIdentity(string Artist, string Title, string ExpectedCardId);

internal sealed record M9BGate3SmokeSample(
    string CardId,
    int SampleNumber,
    string Title,
    string Artist,
    string PromptSha256,
    M9BContextSource ContextSource,
    string? RawOutput,
    string NormalizedOutput,
    M9BCommentValidationKind? ValidationKind,
    string Classification,
    string Reason,
    IReadOnlyList<string> AdvisoryFlags,
    double ElapsedMilliseconds,
    OllamaGenerationMetrics? Metrics);

internal sealed record M9BGate3SmokeResult(
    bool Completed,
    string Message,
    string? ArtifactDirectory,
    int ReadyCandidates,
    int TotalCandidates,
    bool? SelectedModelUnloaded,
    IReadOnlyList<M9BGate3SmokeSample> Samples);

internal sealed class M9BGate3SmokeTest
{
    private static readonly M9BGate3SmokeIdentity[] Identities =
    [
        new("Lynyrd Skynyrd", "Sweet Home Alabama", "M9C6-01"),
        new("Black Sabbath", "Paranoid (2009 Remaster)", "M9C6-02"),
        new("Metallica", "Master of Puppets", "M9C6-06"),
        new("Johnny Cash", "Cocaine Blues (Live at Folsom State Prison, Folsom, CA - January 1968)", "M9C6-16"),
    ];

    internal async Task<M9BGate3SmokeResult> RunAsync(string repositoryRoot, CancellationToken cancellationToken)
    {
        M9BContextCatalog catalog = M9BContextCatalog.Load();
        var resolved = new List<(M9BGate3SmokeIdentity Identity, M9BCatalogCard Card)>();
        foreach (M9BGate3SmokeIdentity identity in Identities)
        {
            M9BCatalogCard? card = catalog.Find(new MediaSnapshot(identity.Title, identity.Artist, null));
            if (card is null || !string.Equals(card.Id, identity.ExpectedCardId, StringComparison.Ordinal))
            {
                return new M9BGate3SmokeResult(false, $"M9B Gate 3 smoke test blocked: reviewed catalog identity did not resolve: {identity.Artist} — {identity.Title}.", null, 0, 12, null, []);
            }

            resolved.Add((identity, card));
        }

        AiCommentConfiguration configuration = AiCommentConfiguration.Default with { Model = M9BConstants.Model };
        using var client = new OllamaClient(configuration);
        OllamaInstalledModelsResult installed = await client.GetInstalledModelsAsync(configuration.LocalOperationTimeout, cancellationToken);
        bool modelPresent = installed.Success && installed.Models.Any(model =>
            string.Equals(model.Name, M9BConstants.Model, StringComparison.OrdinalIgnoreCase)
            || string.Equals(model.Model, M9BConstants.Model, StringComparison.OrdinalIgnoreCase));
        if (!modelPresent)
        {
            return new M9BGate3SmokeResult(false, "M9B Gate 3 smoke test blocked: approved local 14B model is unavailable.", null, 0, 12, null, []);
        }

        string directory = Path.Combine(repositoryRoot, "artifacts", "m9b", "gate3-smoke", DateTimeOffset.Now.ToString("yyyyMMdd-HHmmss-fff"));
        Directory.CreateDirectory(directory);
        var samples = new List<M9BGate3SmokeSample>();
        bool? unloadVerified = null;
        try
        {
            foreach ((M9BGate3SmokeIdentity identity, M9BCatalogCard card) in resolved)
            {
                var media = new MediaSnapshot(identity.Title, identity.Artist, null);
                M9BContextCard contextCard = card.ToContextCard();
                CommentPrompt prompt = M9BWholeCommentPromptFactory.Create(media, contextCard, []);
                string promptHash = Hash(prompt);
                for (int sampleNumber = 1; sampleNumber <= 3; sampleNumber++)
                {
                    Stopwatch stopwatch = Stopwatch.StartNew();
                    OllamaGenerationResult generation = await client.GenerateAsync(
                        prompt,
                        configuration.MeasurementKeepAlive,
                        configuration.GenerationTimeout,
                        cancellationToken);
                    stopwatch.Stop();
                    M9BCommentValidation validation = M9BCommentValidator.Validate(generation.Response, media, contextCard, [], []);
                    string classification = generation.Status == OllamaGenerationStatus.Success
                        ? Classify(validation.Kind)
                        : "generation-failure";
                    string reason = generation.Status == OllamaGenerationStatus.Success
                        ? validation.Reason
                        : generation.ErrorMessage ?? generation.Status.ToString();
                    samples.Add(new M9BGate3SmokeSample(
                        card.Id,
                        sampleNumber,
                        identity.Title,
                        identity.Artist,
                        promptHash,
                        M9BContextSource.ReviewedCatalog,
                        generation.Response,
                        validation.NormalizedText,
                        generation.Status == OllamaGenerationStatus.Success ? validation.Kind : null,
                        classification,
                        reason,
                        validation.AdvisoryFlags,
                        stopwatch.Elapsed.TotalMilliseconds,
                        generation.Metrics));
                    Console.WriteLine($"M9B Gate 3 smoke {samples.Count}/12: {card.Id}, sample {sampleNumber}; {classification}.");
                }
            }
        }
        finally
        {
            await client.UnloadAsync(configuration.LocalOperationTimeout, CancellationToken.None);
            for (int attempt = 0; attempt < 3; attempt++)
            {
                OllamaRunningModelsResult running = await client.GetRunningModelsAsync(configuration.LocalOperationTimeout, CancellationToken.None);
                unloadVerified = running.Success
                    ? !running.Models.Any(model => string.Equals(model.Name, M9BConstants.Model, StringComparison.OrdinalIgnoreCase)
                        || string.Equals(model.Model, M9BConstants.Model, StringComparison.OrdinalIgnoreCase))
                    : null;
                if (unloadVerified is true) break;
                await Task.Delay(250, CancellationToken.None);
            }
        }

        M9BGate3SmokeArtifacts.Write(directory, resolved, samples, unloadVerified);
        int ready = samples.Count(sample => sample.ValidationKind == M9BCommentValidationKind.Valid);
        string message = $"M9B Gate 3 smoke completed: {ready}/{samples.Count} validator-ready candidates; selected-model unload={(unloadVerified is true ? "verified" : "not verified")}.";
        return new M9BGate3SmokeResult(true, message, directory, ready, samples.Count, unloadVerified, samples);
    }

    private static string Hash(CommentPrompt prompt) => Convert.ToHexString(SHA256.HashData(
        Encoding.UTF8.GetBytes(prompt.SystemInstruction + "\n\0\n" + prompt.MetadataPrompt)));

    private static string Classify(M9BCommentValidationKind kind) => kind switch
    {
        M9BCommentValidationKind.Valid => "validator-ready",
        M9BCommentValidationKind.Fallback => "model-selected-fallback",
        M9BCommentValidationKind.HardDiscard => "hard-discard",
        M9BCommentValidationKind.Advisory => "advisory-candidate",
        _ => "unknown",
    };
}

internal static class M9BGate3SmokeArtifacts
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        WriteIndented = true,
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        Converters = { new JsonStringEnumConverter() },
    };

    internal static void Write(
        string directory,
        IReadOnlyList<(M9BGate3SmokeIdentity Identity, M9BCatalogCard Card)> resolved,
        IReadOnlyList<M9BGate3SmokeSample> samples,
        bool? unloadVerified)
    {
        File.WriteAllText(
            Path.Combine(directory, "M9B_GATE3_SMOKE_RESULTS.json"),
            JsonSerializer.Serialize(new { samples }, JsonOptions),
            new UTF8Encoding(false));
        File.WriteAllText(
            Path.Combine(directory, "M9B_GATE3_SMOKE_SUMMARY.md"),
            BuildSummary(resolved, samples, unloadVerified),
            new UTF8Encoding(false));
    }

    private static string BuildSummary(
        IReadOnlyList<(M9BGate3SmokeIdentity Identity, M9BCatalogCard Card)> resolved,
        IReadOnlyList<M9BGate3SmokeSample> samples,
        bool? unloadVerified)
    {
        var text = new StringBuilder("# M9B Gate 3 reviewed-context generate-only smoke test\n\n");
        text.AppendLine("This test used the in-process reviewed catalog and loopback Ollama only. It did not access Firefox, GSMTC, Windows audio, speech, ducking, playback, volume, GUI, cache, or network enrichment.");
        text.AppendLine();
        text.AppendLine("## Reviewed alias-resolution evidence");
        foreach ((M9BGate3SmokeIdentity identity, M9BCatalogCard card) in resolved)
        {
            text.AppendLine($"- {identity.Artist} — {identity.Title} -> `{card.Id}` (`{card.Title}`), source=`ReviewedCatalog`.");
        }

        int ready = samples.Count(sample => sample.ValidationKind == M9BCommentValidationKind.Valid);
        text.AppendLine();
        text.AppendLine($"- Aggregate validator-ready: {ready}/{samples.Count} ({(samples.Count == 0 ? 0 : (double)ready / samples.Count):P0}).");
        text.AppendLine($"- Selected-model unload verified: {unloadVerified?.ToString() ?? "unavailable"}.");
        foreach (IGrouping<string, M9BGate3SmokeSample> track in samples.GroupBy(sample => sample.CardId))
        {
            int trackReady = track.Count(sample => sample.ValidationKind == M9BCommentValidationKind.Valid);
            text.AppendLine($"- {track.First().Artist} — {track.First().Title}: {trackReady}/{track.Count()} validator-ready ({(double)trackReady / track.Count():P0}).");
        }

        text.AppendLine();
        text.AppendLine("## Raw local generation evidence");
        foreach (M9BGate3SmokeSample sample in samples)
        {
            text.AppendLine($"### {sample.Artist} — {sample.Title}, sample {sample.SampleNumber}");
            text.AppendLine($"- Card: `{sample.CardId}`; prompt SHA-256: `{sample.PromptSha256}`; context source: `{sample.ContextSource}`.");
            text.AppendLine($"- Classification: `{sample.Classification}`; reason: {sample.Reason}");
            text.AppendLine($"- Advisory flags: {(sample.AdvisoryFlags.Count == 0 ? "none" : string.Join("; ", sample.AdvisoryFlags))}");
            text.AppendLine($"- Timing: elapsed={sample.ElapsedMilliseconds:0} ms; total-ns={sample.Metrics?.TotalDurationNanoseconds?.ToString() ?? "unavailable"}; load-ns={sample.Metrics?.LoadDurationNanoseconds?.ToString() ?? "unavailable"}.");
            text.AppendLine($"- Raw output: {sample.RawOutput ?? "<no model output>"}");
            text.AppendLine($"- Normalized output: {sample.NormalizedOutput}");
            text.AppendLine();
        }

        return text.ToString();
    }
}

internal static class M9BGate3SmokeCommand
{
    internal static async Task<int> RunAsync()
    {
        M9BGate3SmokeResult result = await new M9BGate3SmokeTest().RunAsync(Directory.GetCurrentDirectory(), CancellationToken.None);
        Console.WriteLine(result.Message);
        if (result.ArtifactDirectory is not null) Console.WriteLine("Artifacts: " + result.ArtifactDirectory);
        return result.Completed ? 0 : 1;
    }
}