using System.Diagnostics;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;

namespace ZoyaRadio.MediaProbe;

internal sealed record M9BBenchmarkSample(
    string CardId,
    int SampleNumber,
    string Title,
    string Artist,
    string Model,
    string PromptSha256,
    IReadOnlyList<M9BCatalogFact> ContextCard,
    string? RawOutput,
    string DisplayedCandidate,
    string Classification,
    string Reason,
    IReadOnlyList<string> AdvisoryFlags,
    bool Regenerated,
    double LatencyMilliseconds,
    OllamaGenerationMetrics? Metrics);

internal sealed record M9BBenchmarkResult(
    bool Completed,
    string Message,
    string? ArtifactDirectory,
    int Attempted,
    int CompletedRequests,
    int FailedRequests,
    bool? UnloadVerified);

internal sealed class M9BGenerateOnlyBenchmark
{
    internal async Task<M9BBenchmarkResult> RunAsync(string repositoryRoot, CancellationToken cancellationToken)
    {
        M9BContextCatalog catalog = M9BContextCatalog.Load();
        AiCommentConfiguration configuration = AiCommentConfiguration.Default with { Model = M9BConstants.Model };
        using var client = new OllamaClient(configuration);
        OllamaInstalledModelsResult installed = await client.GetInstalledModelsAsync(
            configuration.LocalOperationTimeout,
            cancellationToken);
        bool modelPresent = installed.Success && installed.Models.Any(model =>
            string.Equals(model.Name, M9BConstants.Model, StringComparison.OrdinalIgnoreCase)
            || string.Equals(model.Model, M9BConstants.Model, StringComparison.OrdinalIgnoreCase));
        if (!modelPresent)
        {
            return new M9BBenchmarkResult(false, "M9B evaluation blocked: the approved local 14B model is unavailable.", null, 48, 0, 0, null);
        }

        string directory = Path.Combine(repositoryRoot, "artifacts", "m9b", DateTimeOffset.Now.ToString("yyyyMMdd-HHmmss-fff"));
        Directory.CreateDirectory(directory);
        var samples = new List<M9BBenchmarkSample>();
        bool? unloadVerified = null;
        try
        {
            foreach (M9BCatalogCard card in catalog.Cards)
            {
                var media = new MediaSnapshot(card.Title, card.Artist, null);
                M9BContextCard factCard = card.ToContextCard();
                CommentPrompt prompt = M9BWholeCommentPromptFactory.Create(media, factCard, []);
                for (int sampleNumber = 1; sampleNumber <= 3; sampleNumber++)
                {
                    Stopwatch stopwatch = Stopwatch.StartNew();
                    OllamaGenerationResult generation = await client.GenerateAsync(
                        prompt,
                        configuration.MeasurementKeepAlive,
                        configuration.GenerationTimeout,
                        cancellationToken);
                    stopwatch.Stop();
                    M9BCommentValidation validation = M9BCommentValidator.Validate(
                        generation.Response,
                        media,
                        factCard,
                        [],
                        []);
                    samples.Add(new M9BBenchmarkSample(
                        card.Id,
                        sampleNumber,
                        card.Title,
                        card.Artist,
                        M9BConstants.Model,
                        Hash(prompt),
                        card.Facts,
                        generation.Response,
                        validation.NormalizedText,
                        Classify(generation, validation),
                        generation.Status == OllamaGenerationStatus.Success ? validation.Reason : generation.ErrorMessage ?? generation.Status.ToString(),
                        validation.AdvisoryFlags,
                        false,
                        stopwatch.Elapsed.TotalMilliseconds,
                        generation.Metrics));
                    Console.WriteLine($"M9B {samples.Count}/48: {card.Id}, sample {sampleNumber}");
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

        M9BBenchmarkArtifacts.Write(directory, catalog, samples, unloadVerified);
        int failed = samples.Count(sample => sample.Classification == "generation-failure");
        return new M9BBenchmarkResult(
            failed == 0,
            failed == 0 ? "M9B generate-only evaluation completed; Gate 2 owner review is required." : "M9B evaluation had generation failures; review the artifact.",
            directory,
            48,
            samples.Count - failed,
            failed,
            unloadVerified);
    }

    private static string Hash(CommentPrompt prompt) => Convert.ToHexString(SHA256.HashData(
        Encoding.UTF8.GetBytes(prompt.SystemInstruction + "\n\0\n" + prompt.MetadataPrompt)));

    private static string Classify(OllamaGenerationResult generation, M9BCommentValidation validation)
    {
        if (generation.Status != OllamaGenerationStatus.Success) return "generation-failure";
        return validation.Kind switch
        {
            M9BCommentValidationKind.Fallback => "model-selected-fallback",
            M9BCommentValidationKind.HardDiscard => "hard-discard",
            M9BCommentValidationKind.Advisory => "advisory-candidate",
            _ => "displayable-candidate",
        };
    }
}

internal static class M9BBenchmarkArtifacts
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        WriteIndented = true,
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
    };

    internal static void Write(
        string directory,
        M9BContextCatalog catalog,
        IReadOnlyList<M9BBenchmarkSample> samples,
        bool? unloadVerified)
    {
        File.WriteAllText(
            Path.Combine(directory, "M9B_RESULTS.json"),
            JsonSerializer.Serialize(new { catalogVersion = "1.0", samples }, JsonOptions),
            new UTF8Encoding(false));
        File.WriteAllText(
            Path.Combine(directory, "M9B_MANIFEST.json"),
            JsonSerializer.Serialize(new
            {
                catalogVersion = "1.0",
                model = M9BConstants.Model,
                requests = samples.Count,
                cards = catalog.Cards.Select(card => new { card.Id, card.Title, card.Artist, card.Facts }),
                promptResource = M9BConstants.PromptResourceSuffix,
                promptHashes = samples.Select(sample => sample.PromptSha256).Distinct(),
            }, JsonOptions),
            new UTF8Encoding(false));
        File.WriteAllText(
            Path.Combine(directory, "M9B_EVALUATION.md"),
            BuildEvaluation(catalog, samples),
            new UTF8Encoding(false));
        File.WriteAllText(
            Path.Combine(directory, "M9B_RUN_SUMMARY.md"),
            BuildSummary(samples, unloadVerified),
            new UTF8Encoding(false));
    }

    private static string BuildEvaluation(M9BContextCatalog catalog, IReadOnlyList<M9BBenchmarkSample> samples)
    {
        var builder = new StringBuilder("# M9B Gate 2 generate-only evaluation\n\n");
        builder.AppendLine("This artifact is generate-only. It did not open Firefox, access media sessions, control playback, duck audio, speak, launch the GUI, or run normal radio.");
        builder.AppendLine("Score every displayed candidate. Mechanical classifications are engineering evidence, not a substitute for your judgment.\n");
        foreach (M9BCatalogCard card in catalog.Cards)
        {
            builder.AppendLine($"## {card.Artist} — {card.Title}");
            builder.AppendLine($"Catalog card: `{card.Id}` (reviewed local context; cited pages were not fetched during this run).\n");
            builder.AppendLine("### Source-backed context");
            foreach (M9BCatalogFact fact in card.Facts)
            {
                builder.AppendLine($"- {fact.Text} [{fact.SourceName}]({fact.SourceUrl})");
            }

            foreach (M9BBenchmarkSample sample in samples.Where(item => item.CardId == card.Id).OrderBy(item => item.SampleNumber))
            {
                builder.AppendLine($"\n### Candidate {sample.SampleNumber}\n> {sample.RawOutput}\n\nClassification: `{sample.Classification}`. {sample.Reason}");
                if (sample.Classification is "displayable-candidate" or "advisory-candidate")
                {
                    builder.AppendLine("\n| Factual acceptability | Natural context integration | Zoya personality | Spoken naturalness | Variety | Overall desire | Notes |\n|---|---|---|---|---|---|---|\n| | | | | | | |\n- [ ] Unsupported/questionable claim\n- [ ] Fact recitation\n- [ ] Formulaic fact-then-reaction\n- [ ] Repeated opening/template\n- [ ] Awkward third-party reference\n- [ ] Would want Zoya to say this");
                }
                else
                {
                    builder.AppendLine("\nWould this actually have been acceptable on Zoya Radio? [ ] Yes  [ ] No\nNotes:");
                }
            }

            builder.AppendLine();
        }

        return builder.ToString();
    }

    private static string BuildSummary(IReadOnlyList<M9BBenchmarkSample> samples, bool? unloadVerified)
    {
        double[] latency = samples.Select(sample => sample.LatencyMilliseconds).OrderBy(value => value).ToArray();
        double percentile95 = latency.Length == 0 ? 0 : latency[(int)Math.Ceiling(latency.Length * 0.95) - 1];
        int displayable = samples.Count(sample => sample.Classification == "displayable-candidate");
        int advisory = samples.Count(sample => sample.Classification == "advisory-candidate");
        int hard = samples.Count(sample => sample.Classification == "hard-discard");
        int fallback = samples.Count(sample => sample.Classification == "model-selected-fallback");
        int failed = samples.Count(sample => sample.Classification == "generation-failure");
        return JsonSerializer.Serialize(new
        {
            requests = samples.Count,
            generationFailures = failed,
            displayableCandidates = displayable,
            advisoryCandidates = advisory,
            hardDiscards = hard,
            modelSelectedFallbacks = fallback,
            regenerations = samples.Count(sample => sample.Regenerated),
            candidateAvailabilityRate = samples.Count == 0 ? 0 : (double)(displayable + advisory) / samples.Count,
            latencyMilliseconds = new
            {
                median = latency.Length == 0 ? 0 : latency[latency.Length / 2],
                p95 = percentile95,
                maximum = latency.Length == 0 ? 0 : latency[^1],
            },
            selectedModelUnloadVerified = unloadVerified,
        }, JsonOptions);
    }
}

internal static class M9BGenerateOnlyCommand
{
    internal static async Task<int> RunAsync()
    {
        using var cancellation = new CancellationTokenSource();
        M9BBenchmarkResult result = await new M9BGenerateOnlyBenchmark().RunAsync(
            Directory.GetCurrentDirectory(),
            cancellation.Token);
        Console.WriteLine(result.Message);
        if (result.ArtifactDirectory is not null) Console.WriteLine("Artifacts: " + result.ArtifactDirectory);
        return result.Completed ? 0 : 1;
    }
}