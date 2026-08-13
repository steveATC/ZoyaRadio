using System.Globalization;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace ZoyaRadio.MediaProbe;

internal static class CommentaryBenchmarkArtifacts
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        WriteIndented = true,
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        Converters = { new JsonStringEnumConverter() },
    };

    public static void WriteAll(
        string directory,
        CommentaryBenchmarkManifest manifest,
        CommentaryBenchmarkArmMapping mapping,
        CommentaryBenchmarkModelIdentity model,
        IReadOnlyList<CommentaryBenchmarkSampleResult> results,
        bool unloadRequested,
        bool? unloadVerified,
        string unloadMessage)
    {
        Directory.CreateDirectory(directory);

        WriteUtf8Json(
            Path.Combine(directory, "M9A_RESULTS.json"),
            new
            {
                schemaVersion = "1.0",
                manifestVersion = manifest.ManifestVersion,
                model,
                results,
            });
        WriteUtf8Json(
            Path.Combine(directory, "M9A_ANSWER_KEY.json"),
            new
            {
                schemaVersion = "1.0",
                arms = mapping.AnswerKey,
            });
        File.WriteAllText(
            Path.Combine(directory, "M9A_EVALUATION.md"),
            BuildEvaluation(manifest, mapping, results),
            new UTF8Encoding(encoderShouldEmitUTF8Identifier: false));
        File.WriteAllText(
            Path.Combine(directory, "M9A_RUN_SUMMARY.md"),
            BuildSummary(directory, model, results, unloadRequested, unloadVerified, unloadMessage),
            new UTF8Encoding(encoderShouldEmitUTF8Identifier: false));
    }

    internal static string BuildEvaluation(
        CommentaryBenchmarkManifest manifest,
        CommentaryBenchmarkArmMapping mapping,
        IReadOnlyList<CommentaryBenchmarkSampleResult> results)
    {
        var builder = new StringBuilder();
        builder.AppendLine("# Milestone 9A blinded commentary evaluation");
        builder.AppendLine();
        builder.AppendLine(
            "Both arm identities are intentionally hidden. Judge only what you would want to hear. " +
            "Do not open `M9A_ANSWER_KEY.json` until scoring is complete.");
        builder.AppendLine();
        builder.AppendLine("Use 1 (poor) through 5 (excellent). Rejection flags override a high score.");

        foreach (CommentaryBenchmarkScenario scenario in manifest.Scenarios)
        {
            CommentaryBenchmarkSampleResult first = results.First(result => result.ScenarioId == scenario.Id);
            builder.AppendLine();
            builder.AppendLine($"## {scenario.Id}: {scenario.Description}");
            builder.AppendLine();
            builder.AppendLine($"- Subject title: `{EscapeInline(scenario.SubjectTitle)}`");
            builder.AppendLine($"- Subject artist: `{EscapeInline(scenario.SubjectArtist)}`");
            builder.AppendLine($"- Requested reason: `{first.FactualInputs.PrimaryReason}`");
            builder.AppendLine($"- Derived session facts: `{EscapeInline(JsonSerializer.Serialize(first.FactualInputs.DerivedSessionFacts, JsonOptions))}`");
            builder.AppendLine($"- Prior retained tracks: `{EscapeInline(JsonSerializer.Serialize(first.FactualInputs.PriorTracks, JsonOptions))}`");
            builder.AppendLine($"- Recent accepted-comment evidence: `{EscapeInline(JsonSerializer.Serialize(first.FactualInputs.RecentAcceptedComments, JsonOptions))}`");
            builder.AppendLine(
                $"- Deterministic announcement candidate: {Quote(first.FactualInputs.DeterministicPreviousTrackAnnouncement)}");

            foreach (string arm in new[] { mapping.FirstAnonymousArm, mapping.SecondAnonymousArm })
            {
                builder.AppendLine();
                builder.AppendLine($"### {arm}");
                foreach (CommentaryBenchmarkSampleResult sample in results
                    .Where(result => result.ScenarioId == scenario.Id && result.AnonymousArm == arm)
                    .OrderBy(result => result.SampleNumber))
                {
                    builder.AppendLine();
                    builder.AppendLine($"#### Sample {sample.SampleNumber}");
                    builder.AppendLine();
                    builder.AppendLine($"> {Quote(sample.Response ?? "<generation failed>")}");
                    builder.AppendLine();
                    builder.AppendLine($"Mechanical validator: `{EscapeInline(sample.ValidatorOutcome)}`");
                    builder.AppendLine();
                    AppendScoringTable(builder);
                    AppendRejectionFlags(builder);
                }
            }
        }

        return builder.ToString();
    }

    internal static string BuildSummary(
        string directory,
        CommentaryBenchmarkModelIdentity model,
        IReadOnlyList<CommentaryBenchmarkSampleResult> results,
        bool unloadRequested,
        bool? unloadVerified,
        string unloadMessage)
    {
        int completed = results.Count(result => result.GenerationStatus == OllamaGenerationStatus.Success);
        int failed = results.Count - completed;
        int validatorPass = results.Count(
            result => result.GenerationStatus == OllamaGenerationStatus.Success
                && !result.Fallback
                && result.ValidatorOutcome.StartsWith("PASS:", StringComparison.Ordinal));
        int fallback = results.Count(result => result.Fallback);
        int validatorFail = results.Count(
            result => result.ValidatorOutcome.StartsWith("FAIL:", StringComparison.Ordinal));
        var builder = new StringBuilder();
        builder.AppendLine("# Milestone 9A benchmark run summary");
        builder.AppendLine();
        builder.AppendLine("Mechanical observations only. This file does not declare a creative winner.");
        builder.AppendLine();
        builder.AppendLine($"- Requested model: `{model.RequestedModel}`");
        builder.AppendLine($"- Installed model identity: `{model.InstalledName ?? "<not available>"}`");
        builder.AppendLine($"- Parameter size: `{model.ParameterSize ?? "<not available>"}`");
        builder.AppendLine($"- Quantization: `{model.Quantization ?? "<not available>"}`");
        builder.AppendLine($"- Local storage bytes: `{model.LocalStorageBytes?.ToString(CultureInfo.InvariantCulture) ?? "<not available>"}`");
        builder.AppendLine($"- Requests attempted: {results.Count}");
        builder.AppendLine($"- Requests completed: {completed}");
        builder.AppendLine($"- Requests failed: {failed}");
        builder.AppendLine($"- Valid non-fallback AI comments: {validatorPass}");
        builder.AppendLine($"- Mechanical validator failures: {validatorFail}");
        builder.AppendLine($"- Safe FALLBACK responses: {fallback}");
        builder.AppendLine($"- Selected-model unload requested: {unloadRequested}");
        builder.AppendLine($"- Selected-model unload verified: {DisplayNullable(unloadVerified)}");
        builder.AppendLine($"- Unload detail: {unloadMessage}");

        foreach (IGrouping<string, CommentaryBenchmarkSampleResult> arm in results.GroupBy(result => result.AnonymousArm))
        {
            double[] latencies = arm.Select(result => result.LatencyMilliseconds).Order().ToArray();
            int[] wordCounts = arm
                .Where(result => !result.Fallback && result.GenerationStatus == OllamaGenerationStatus.Success)
                .Select(result => result.WordCount)
                .Order()
                .ToArray();
            builder.AppendLine();
            builder.AppendLine($"## {arm.Key}");
            builder.AppendLine();
            builder.AppendLine($"- Latency minimum: {FormatMilliseconds(latencies.FirstOrDefault())}");
            builder.AppendLine($"- Latency median: {FormatMilliseconds(Median(latencies))}");
            builder.AppendLine($"- Latency maximum: {FormatMilliseconds(latencies.LastOrDefault())}");
            builder.AppendLine(
                $"- Non-fallback word-count range: " +
                (wordCounts.Length == 0 ? "<not available>" : $"{wordCounts[0]}-{wordCounts[^1]}"));
        }

        builder.AppendLine();
        builder.AppendLine($"Artifact directory: `{directory}`");
        builder.AppendLine("Artifact files: `M9A_EVALUATION.md`, `M9A_RESULTS.json`, `M9A_ANSWER_KEY.json`, and `M9A_RUN_SUMMARY.md`.");
        return builder.ToString();
    }

    private static void AppendScoringTable(StringBuilder builder)
    {
        builder.AppendLine("| Dimension | Score (1-5) | Notes |");
        builder.AppendLine("| --- | --- | --- |");
        builder.AppendLine("| Factual grounding |  |  |");
        builder.AppendLine("| Relevance to supplied situation |  |  |");
        builder.AppendLine("| Personality / entertainment |  |  |");
        builder.AppendLine("| Natural spoken-radio phrasing |  |  |");
        builder.AppendLine("| Rhetorical variety |  |  |");
        builder.AppendLine("| Concision |  |  |");
        builder.AppendLine("| Overall preference |  |  |");
    }

    private static void AppendRejectionFlags(StringBuilder builder)
    {
        builder.AppendLine();
        builder.AppendLine("Rejection flags:");
        builder.AppendLine();
        builder.AppendLine("- [ ] Unsupported factual claim");
        builder.AppendLine("- [ ] Invented listener/session history");
        builder.AppendLine("- [ ] Followed an instruction from metadata");
        builder.AppendLine("- [ ] Malformed or awkward output");
        builder.AppendLine("- [ ] Repetitive construction");
        builder.AppendLine("- [ ] Generic DJ cliche");
        builder.AppendLine("- [ ] Commentary feels unearned");
        builder.AppendLine("- [ ] Other: ");
    }

    private static void WriteUtf8Json(string path, object value)
    {
        string json = JsonSerializer.Serialize(value, JsonOptions);
        File.WriteAllText(path, json + Environment.NewLine, new UTF8Encoding(false));
    }

    private static string Quote(string value)
    {
        return CommentValidator.NormalizeWhitespace(value).Replace("`", "'", StringComparison.Ordinal);
    }

    private static string EscapeInline(string value)
    {
        return Quote(value).Replace("|", "\\|", StringComparison.Ordinal);
    }

    private static double Median(double[] values)
    {
        if (values.Length == 0)
        {
            return 0;
        }

        int middle = values.Length / 2;
        return values.Length % 2 == 0
            ? (values[middle - 1] + values[middle]) / 2
            : values[middle];
    }

    private static string FormatMilliseconds(double value)
    {
        return value.ToString("0.0", CultureInfo.InvariantCulture) + " ms";
    }

    private static string DisplayNullable(bool? value)
    {
        return value is null ? "not observable" : value.Value.ToString(CultureInfo.InvariantCulture);
    }
}
