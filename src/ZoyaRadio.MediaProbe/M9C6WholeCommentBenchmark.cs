using System.Diagnostics;
using System.Reflection;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;

namespace ZoyaRadio.MediaProbe;

internal sealed record M9C6Fact(string Text, string Url);
internal sealed record M9C6Scenario(string Id, string Artist, string Title, IReadOnlyList<M9C6Fact> Facts);
internal sealed record M9C6Manifest(string Version, int SamplesPerScenario, IReadOnlyList<M9C6Scenario> Scenarios);
internal sealed record M9C6Sample(string ScenarioId, int SampleNumber, string BlindedArm, string PromptMode, string Model, string PromptSha256, IReadOnlyList<M9C6Fact> ContextPack, string? RawOutput, string FinalDisplayedCandidate, string Classification, string? HardRejectionReason, IReadOnlyList<string> SoftFlags, int WordCount, int SentenceCount, double LatencyMilliseconds, OllamaGenerationMetrics? Metrics);

internal static class M9C6WholeCommentBenchmark
{
    internal const string Model = "qwen3:14b-q4_K_M";
    internal static M9C6Manifest Load()
    {
        Assembly a = typeof(M9C6WholeCommentBenchmark).Assembly; string n = a.GetManifestResourceNames().Single(x => x.EndsWith("Prompts.Milestone9C6ContextPacks.json", StringComparison.Ordinal)); using Stream s = a.GetManifestResourceStream(n)!; M9C6Manifest m = JsonSerializer.Deserialize<M9C6Manifest>(s, new JsonSerializerOptions { PropertyNameCaseInsensitive = true })!;
        if (m.Version != "1.0" || m.SamplesPerScenario != 3 || m.Scenarios.Count != 16 || m.Scenarios.Any(x => x.Facts.Count is < 3 or > 5 || x.Facts.Any(f => !Uri.TryCreate(f.Url, UriKind.Absolute, out Uri? u) || u.Scheme != "https" || string.IsNullOrWhiteSpace(f.Text)))) throw new InvalidOperationException("M9C.6 requires 16 sourced three-to-five-fact context packs."); return m;
    }
    internal static CommentPrompt Prompt(M9C6Scenario s, bool training)
    {
        string system = "You are Zoya, a concise late-night radio host. Write one complete natural spoken comment about the current artist or track. Use supplied context naturally; do not enumerate it or produce fact then reaction. One or two sentences, 12-45 spoken words. A rhetorical question is permitted but never required. Output only the comment or exactly FALLBACK. " + (training ? "The supplied pack is authoritative, but you may use relevant training knowledge if it improves the comment; do not contradict the pack." : "Use only supplied facts for externally verifiable claims; do not add new factual claims.");
        string body = JsonSerializer.Serialize(new { title = s.Title, artist = s.Artist, context = s.Facts.Select(f => new { fact = f.Text, sourceUrl = f.Url }) }); return new CommentPrompt(system, "<M9C6_CONTEXT_JSON>\n" + body + "\n</M9C6_CONTEXT_JSON>\nWrite the complete comment now.");
    }
    internal static M9C6Sample Classify(M9C6Scenario s, int sample, string arm, bool training, CommentPrompt p, OllamaGenerationResult g, double ms)
    {
        string raw = g.Response ?? ""; string normalized = string.Join(" ", raw.Split((char[]?)null, StringSplitOptions.RemoveEmptyEntries)); int words = normalized.Split(' ', StringSplitOptions.RemoveEmptyEntries).Length; int sentences = System.Text.RegularExpressions.Regex.Matches(normalized, "[.!?]+(?=\\s|$)").Count;
        string hash = Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(p.SystemInstruction + "\n\0\n" + p.MetadataPrompt))); var flags = new List<string>(); string classification;
        string? hard = null;
        if (g.Status != OllamaGenerationStatus.Success) { classification = "generation-failure"; hard = g.ErrorMessage ?? g.Status.ToString(); }
        else if (string.Equals(normalized, "FALLBACK", StringComparison.Ordinal)) { classification = "model-selected-fallback"; }
        else if (string.IsNullOrWhiteSpace(normalized) || words > 70 || normalized.Contains("as an ai", StringComparison.OrdinalIgnoreCase) || normalized.Contains("ignore previous", StringComparison.OrdinalIgnoreCase)) { classification = "hard-rejected"; hard = "Empty, grossly overlong, meta, or instruction-following output."; }
        else if (!normalized.Contains(s.Artist, StringComparison.OrdinalIgnoreCase) && !normalized.Contains(s.Title, StringComparison.OrdinalIgnoreCase)) { classification = "hard-rejected"; hard = "The comment does not identify the supplied artist or track."; }
        else { if (sentences > 2) flags.Add("More than two sentences."); if (words < 12 || words > 45) flags.Add("Outside target spoken-word range."); if (System.Text.RegularExpressions.Regex.IsMatch(normalized, "(?i)\b(classic|legendary|timeless|always|never|best|greatest)\b")) flags.Add("Possible overstatement or training-derived claim."); if (s.Facts.Any(f => normalized.Contains(f.Text, StringComparison.OrdinalIgnoreCase))) flags.Add("Near-verbatim context recitation."); if (System.Text.RegularExpressions.Regex.IsMatch(normalized, "(?i)\b(then|and then)\b.*\b(that|this)\b")) flags.Add("Possible formulaic fact-then-reaction construction."); classification = flags.Count == 0 ? "clean-candidate" : "soft-flagged-candidate"; }
        return new(s.Id, sample, arm, training ? "training-augmented" : "grounded", Model, hash, s.Facts, raw, normalized, classification, hard, flags, words, sentences, ms, g.Metrics);
    }
}

internal sealed class M9C6Runner
{
    internal async Task<MusicContextBenchmarkRunResult> RunAsync(string root, CancellationToken ct)
    {
        M9C6Manifest manifest = M9C6WholeCommentBenchmark.Load(); var config = AiCommentConfiguration.Default with { Model = M9C6WholeCommentBenchmark.Model }; using var client = new OllamaClient(config); var installed = await client.GetInstalledModelsAsync(config.LocalOperationTimeout, ct); if (!installed.Success || !installed.Models.Any(x => string.Equals(x.Name, M9C6WholeCommentBenchmark.Model, StringComparison.OrdinalIgnoreCase) || string.Equals(x.Model, M9C6WholeCommentBenchmark.Model, StringComparison.OrdinalIgnoreCase))) return new(false, "M9C.6 blocked: exact approved 14B model unavailable.", null, 0, 0, 0, false, null);
        var plan = manifest.Scenarios.SelectMany(s => Enumerable.Range(1, 3).SelectMany(n => new[] { (s, n, training: false, arm: "Arm Cedar"), (s, n, training: true, arm: "Arm Lark") })).ToList(); var prompts = plan.Select(x => (x, M9C6WholeCommentBenchmark.Prompt(x.s, x.training))).ToList(); var samples = new List<M9C6Sample>(); string dir = Path.Combine(root, "artifacts", "m9c", DateTimeOffset.Now.ToString("yyyyMMdd-HHmmss-fff")); Directory.CreateDirectory(dir); bool? unloaded = null;
        try { foreach (var pair in prompts) { var sw = Stopwatch.StartNew(); var g = await client.GenerateAsync(pair.Item2, config.MeasurementKeepAlive, config.GenerationTimeout, ct); sw.Stop(); samples.Add(M9C6WholeCommentBenchmark.Classify(pair.x.s, pair.x.n, pair.x.arm, pair.x.training, pair.Item2, g, sw.Elapsed.TotalMilliseconds)); Console.WriteLine($"M9C.6 {samples.Count}/96: {pair.x.s.Id} {pair.x.arm}"); } }
        finally { await client.UnloadAsync(config.LocalOperationTimeout, CancellationToken.None); for (int i = 0; i < 3; i++) { var ps = await client.GetRunningModelsAsync(config.LocalOperationTimeout, CancellationToken.None); unloaded = ps.Success ? !ps.Models.Any(x => string.Equals(x.Name, M9C6WholeCommentBenchmark.Model, StringComparison.OrdinalIgnoreCase) || string.Equals(x.Model, M9C6WholeCommentBenchmark.Model, StringComparison.OrdinalIgnoreCase)) : null; if (unloaded is true) break; await Task.Delay(250, CancellationToken.None); } }
        M9C6Artifacts.Write(dir, samples, unloaded); int failed = samples.Count(x => x.Classification == "generation-failure"); return new(failed == 0, "M9C.6 complete; owner evaluation required.", dir, 96, 96 - failed, failed, true, unloaded);
    }
}
internal static class M9C6Artifacts
{
    static readonly JsonSerializerOptions J = new() { WriteIndented = true, PropertyNamingPolicy = JsonNamingPolicy.CamelCase };
    internal static void Write(string d, IReadOnlyList<M9C6Sample> s, bool? unloaded) { File.WriteAllText(Path.Combine(d, "M9C6_RESULTS.json"), JsonSerializer.Serialize(new { samples = s }, J), new UTF8Encoding(false)); File.WriteAllText(Path.Combine(d, "M9C6_ANSWER_KEY.json"), JsonSerializer.Serialize(s.Select(x => new { x.ScenarioId, x.SampleNumber, x.BlindedArm, x.PromptMode, x.Model, x.PromptSha256 }), J), new UTF8Encoding(false)); var b = new StringBuilder("# M9C.6 blinded whole-comment evaluation\n\nDo not open the answer key until scoring is complete.\n"); foreach (var g in s.GroupBy(x => new { x.ScenarioId, x.SampleNumber })) { b.AppendLine($"\n## {g.Key.ScenarioId}, sample {g.Key.SampleNumber}"); foreach (var x in g.OrderBy(x => x.BlindedArm)) { b.AppendLine($"\n### {x.BlindedArm}\n> {x.RawOutput}\n\nClassification: `{x.Classification}`. {x.HardRejectionReason} {string.Join("; ", x.SoftFlags)}"); if (x.Classification is "clean-candidate" or "soft-flagged-candidate") b.AppendLine("\n| Factual acceptability | Natural context integration | Zoya personality | Spoken naturalness | Variety | Overall desire | Notes |\n|---|---|---|---|---|---|---|\n| | | | | | | |\n- [ ] Fact recitation\n- [ ] Formulaic fact-then-reaction\n- [ ] Unsupported/questionable claim"); else if (x.Classification == "hard-rejected") b.AppendLine("\nWould this actually have been acceptable on Zoya Radio? [ ] Yes  [ ] No\nNotes:"); } } File.WriteAllText(Path.Combine(d, "M9C6_EVALUATION.md"), b.ToString(), new UTF8Encoding(false)); var summary = s.GroupBy(x => new { x.BlindedArm, x.PromptMode }).Select(g => new { g.Key.BlindedArm, g.Key.PromptMode, total = g.Count(), clean = g.Count(x => x.Classification == "clean-candidate"), soft = g.Count(x => x.Classification == "soft-flagged-candidate"), hard = g.Count(x => x.Classification == "hard-rejected"), fallback = g.Count(x => x.Classification == "model-selected-fallback"), fail = g.Count(x => x.Classification == "generation-failure") }); File.WriteAllText(Path.Combine(d, "M9C6_RUN_SUMMARY.md"), JsonSerializer.Serialize(new { requests = s.Count, unloadVerified = unloaded, arms = summary }, J), new UTF8Encoding(false)); }
}
internal static class M9C6Command { internal static async Task<int> RunAsync() { using var c = new CancellationTokenSource(); var r = await new M9C6Runner().RunAsync(Directory.GetCurrentDirectory(), c.Token); Console.WriteLine(r.Message); if (r.ArtifactDirectory is not null) Console.WriteLine("Artifacts: " + r.ArtifactDirectory); return r.Completed ? 0 : 1; } }