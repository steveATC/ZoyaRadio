using System.Diagnostics;
using System.Globalization;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;

namespace ZoyaRadio.MediaProbe;

internal sealed record M9C5PlanItem(
    string ScenarioId,
    int SampleNumber,
    string FixtureId,
    string BlindedArm,
    string Model,
    CommentPrompt Prompt,
    DeterministicContextScenario Scenario,
    ZoyaPersonaMemory? Persona);

internal sealed record M9C5Sample(
    string ScenarioId,
    int SampleNumber,
    string FixtureId,
    string BlindedArm,
    string Model,
    string PromptSha256,
    string? RawGeneratedResponse,
    string FinalComposedResponse,
    string OutputKind,
    string ValidatorOutcome,
    int WordCount,
    double LatencyMilliseconds,
    OllamaGenerationMetrics? Metrics);

internal static class M9C5ComparisonPlan
{
    internal const string Model4B = "qwen3:4b-instruct";
    internal const string Model14B = "qwen3:14b-q4_K_M";
    private static readonly IReadOnlyList<(string Id, IReadOnlyList<string> Links)> Fixtures =
    [
        ("fixture-plain", []),
        ("fixture-distinctive", ["That title feels like a southern punchline."]),
        ("fixture-variety", ["Paperwork has never looked so suspicious.", "That is a bad haircut of a decision."]),
    ];

    internal static IReadOnlyList<M9C5PlanItem> Create(DeterministicContextBenchmarkManifest manifest)
    {
        var items = new List<M9C5PlanItem>();
        for (int scenarioIndex = 0; scenarioIndex < manifest.Scenarios.Count; scenarioIndex++)
        {
            DeterministicContextScenario scenario = manifest.Scenarios[scenarioIndex];
            ZoyaPersonaMemory? persona = scenario.PersonaMemoryId is null ? null : manifest.PersonaMemories.Single(memory => memory.Id == scenario.PersonaMemoryId);
            for (int sample = 1; sample <= manifest.SamplesPerCondition; sample++)
            {
                var fixture = Fixtures[(sample - 1) % Fixtures.Count];
                CommentPrompt prompt = DeterministicContextLinkPromptFactory.Create(scenario, persona, fixture.Links);
                // Arm identities stay fixed; only execution order is counterbalanced.
                var cedar = new M9C5PlanItem(scenario.Id, sample, fixture.Id, "Arm Cedar", Model4B, prompt, scenario, persona);
                var lark = new M9C5PlanItem(scenario.Id, sample, fixture.Id, "Arm Lark", Model14B, prompt, scenario, persona);
                if ((scenarioIndex + sample) % 2 == 0) { items.Add(cedar); items.Add(lark); }
                else { items.Add(lark); items.Add(cedar); }
            }
        }
        if (items.Count != 120 || items.Any(item => item.Model is not Model4B and not Model14B)) throw new InvalidOperationException("M9C.5 plan must contain exactly 120 approved-model requests.");
        return items;
    }

    internal static string Hash(CommentPrompt prompt) => Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(prompt.SystemInstruction + "\n\0\n" + prompt.MetadataPrompt)));
}

internal sealed class M9C5ComparisonRunner
{
    private readonly Func<string, ILocalCommentGenerator> _generatorFactory;
    private readonly AiCommentConfiguration _baseConfiguration;
    internal M9C5ComparisonRunner(Func<string, ILocalCommentGenerator> generatorFactory, AiCommentConfiguration baseConfiguration) { _generatorFactory = generatorFactory; _baseConfiguration = baseConfiguration; }

    internal async Task<MusicContextBenchmarkRunResult> RunAsync(string root, CancellationToken cancellationToken)
    {
        DeterministicContextBenchmarkManifest manifest = DeterministicContextBenchmarkManifestLoader.Load();
        IReadOnlyList<M9C5PlanItem> plan = M9C5ComparisonPlan.Create(manifest);
        var generators = new Dictionary<string, ILocalCommentGenerator>(StringComparer.Ordinal) { [M9C5ComparisonPlan.Model4B] = _generatorFactory(M9C5ComparisonPlan.Model4B), [M9C5ComparisonPlan.Model14B] = _generatorFactory(M9C5ComparisonPlan.Model14B) };
        var installed = await generators[M9C5ComparisonPlan.Model4B].GetInstalledModelsAsync(_baseConfiguration.LocalOperationTimeout, cancellationToken);
        if (!installed.Success || !new[] { M9C5ComparisonPlan.Model4B, M9C5ComparisonPlan.Model14B }.All(model => installed.Models.Any(x => string.Equals(x.Name, model, StringComparison.OrdinalIgnoreCase) || string.Equals(x.Model, model, StringComparison.OrdinalIgnoreCase))))
            return new MusicContextBenchmarkRunResult(false, "M9C.5 comparison blocked: both exact approved model tags must already be installed. Nothing was downloaded.", null, 0, 0, 0, false, null);
        string directory = Path.Combine(root, "artifacts", "m9c", DateTimeOffset.Now.ToString("yyyyMMdd-HHmmss-fff", CultureInfo.InvariantCulture)); Directory.CreateDirectory(directory);
        var samples = new List<M9C5Sample>(); var unload = new Dictionary<string, bool?>();
        try
        {
            foreach (M9C5PlanItem item in plan)
            {
                cancellationToken.ThrowIfCancellationRequested();
                Console.WriteLine($"M9C.5 {samples.Count + 1}/120: {item.ScenarioId}, sample {item.SampleNumber}, {item.BlindedArm}");
                var timer = Stopwatch.StartNew();
                OllamaGenerationResult result = await generators[item.Model].GenerateAsync(item.Prompt, _baseConfiguration.MeasurementKeepAlive, _baseConfiguration.GenerationTimeout, cancellationToken); timer.Stop();
                DeterministicLinkValidationResult validation = result.Status == OllamaGenerationStatus.Success ? DeterministicContextLinkValidator.Validate(result.Response, item.Scenario, item.Persona, FixtureLinks(item.FixtureId)) : new(false, false, CommentValidator.FallbackSignal, 0, "Generation failed: " + (result.ErrorMessage ?? result.Status.ToString()));
                DeterministicContextComposition composition = DeterministicContextComposer.Compose(item.Scenario, validation);
                samples.Add(new M9C5Sample(item.ScenarioId, item.SampleNumber, item.FixtureId, item.BlindedArm, item.Model, M9C5ComparisonPlan.Hash(item.Prompt), result.Response, composition.FinalText, result.Status == OllamaGenerationStatus.Success ? composition.OutputKind : "generation-failed", validation.Reason, validation.WordCount, timer.Elapsed.TotalMilliseconds, result.Metrics));
            }
        }
        finally
        {
            foreach (var pair in generators)
            {
                await pair.Value.UnloadAsync(_baseConfiguration.LocalOperationTimeout, CancellationToken.None);
                OllamaRunningModelsResult running = await pair.Value.GetRunningModelsAsync(_baseConfiguration.LocalOperationTimeout, CancellationToken.None);
                unload[pair.Key] = running.Success ? !running.Models.Any(x => string.Equals(x.Name, pair.Key, StringComparison.OrdinalIgnoreCase) || string.Equals(x.Model, pair.Key, StringComparison.OrdinalIgnoreCase)) : null;
                if (pair.Value is IDisposable disposable) disposable.Dispose();
            }
        }
        M9C5Artifacts.Write(directory, manifest, plan, samples, unload, installed.Models);
        int failures = samples.Count(x => x.OutputKind == "generation-failed");
        return new MusicContextBenchmarkRunResult(failures == 0, failures == 0 ? "M9C.5 comparison completed. Owner evaluation is required." : "M9C.5 completed with generation failures; inspect artifacts.", directory, samples.Count, samples.Count - failures, failures, true, unload.Values.All(x => x is true));
    }
    private static IReadOnlyList<string> FixtureLinks(string id) => id switch { "fixture-plain" => [], "fixture-distinctive" => ["That title feels like a southern punchline."], "fixture-variety" => ["Paperwork has never looked so suspicious.", "That is a bad haircut of a decision."], _ => throw new InvalidOperationException("Unknown M9C.5 fixture.") };
}

internal static class M9C5Artifacts
{
    private static readonly JsonSerializerOptions Json = new() { WriteIndented = true, PropertyNamingPolicy = JsonNamingPolicy.CamelCase };
    internal static void Write(string dir, DeterministicContextBenchmarkManifest manifest, IReadOnlyList<M9C5PlanItem> plan, IReadOnlyList<M9C5Sample> samples, IReadOnlyDictionary<string, bool?> unload, IReadOnlyList<OllamaInstalledModel> installed)
    {
        bool promptEquality = plan.GroupBy(x => (x.ScenarioId, x.SampleNumber)).All(g => g.Count() == 2 && g.Select(x => M9C5ComparisonPlan.Hash(x.Prompt)).Distinct().Count() == 1);
        File.WriteAllText(Path.Combine(dir, "M9C5_RESULTS.json"), JsonSerializer.Serialize(new { models = new[] { M9C5ComparisonPlan.Model4B, M9C5ComparisonPlan.Model14B }, samples }, Json), new UTF8Encoding(false));
        File.WriteAllText(Path.Combine(dir, "M9C5_ANSWER_KEY.json"), JsonSerializer.Serialize(plan.Select(x => new { x.ScenarioId, x.SampleNumber, x.BlindedArm, x.Model, promptSha256 = M9C5ComparisonPlan.Hash(x.Prompt) }), Json), new UTF8Encoding(false));
        var summary = new StringBuilder("# M9C.5 local-model comparison summary\n\n- Requests: 120 (20 scenarios × 3 samples × 2 arms)\n- Prompt equality across arms: " + promptEquality + "\n- No generated response influenced any prompt.\n");
        foreach (string model in new[] { M9C5ComparisonPlan.Model4B, M9C5ComparisonPlan.Model14B }) { var s = samples.Where(x => x.Model == model).ToList(); var lat = s.Select(x => x.LatencyMilliseconds).OrderBy(x => x).ToList(); summary.AppendLine($"\n## {model}\n- Total: {s.Count}\n- Successful: {s.Count(x => x.OutputKind != "generation-failed")}\n- Accepted anchor-plus-link: {s.Count(x => x.OutputKind == "anchor-plus-link")}\n- Model FALLBACK / validator rejected: {s.Count(x => x.OutputKind == "fallback")}\n- Distinct raw outputs: {s.Select(x => x.RawGeneratedResponse).Distinct().Count()}\n- Exact duplicates: {s.Count - s.Select(x => x.RawGeneratedResponse).Distinct().Count()}\n- Median latency ms: {(lat.Count == 0 ? 0 : lat[lat.Count / 2]):0}\n- P95 latency ms: {(lat.Count == 0 ? 0 : lat[(int)Math.Ceiling(lat.Count * .95) - 1]):0}\n- Selected-model unload verified: {unload[model]?.ToString() ?? "not observable"}"); }
        File.WriteAllText(Path.Combine(dir, "M9C5_RUN_SUMMARY.md"), summary.ToString(), new UTF8Encoding(false));
        var evaluation = new StringBuilder("# M9C.5 blinded local-model comparison\n\nDo not open `M9C5_ANSWER_KEY.json` until scoring is complete. Score only accepted `anchor-plus-link` rows; FALLBACK is shown but not scored.\n");
        foreach (var group in samples.GroupBy(x => (x.ScenarioId, x.SampleNumber)).OrderBy(x => x.Key.ScenarioId).ThenBy(x => x.Key.SampleNumber)) { var scenario = manifest.Scenarios.Single(x => x.Id == group.Key.ScenarioId); evaluation.AppendLine($"\n## {scenario.Id}, sample {group.Key.SampleNumber}\n- Anchor: {scenario.AnchorText}\n- Source: [{scenario.SourceName}]({scenario.SourceUrl})"); foreach (var s in group.OrderBy(x => x.BlindedArm)) { evaluation.AppendLine($"\n### {s.BlindedArm}\n> {s.FinalComposedResponse}\n\nKind: `{s.OutputKind}`. {s.ValidatorOutcome}"); if (s.OutputKind == "anchor-plus-link") evaluation.AppendLine("\n| Grounding | Anchor-to-link flow | Zoya personality | Persona coherence | Spoken phrasing | Variety | Overall desire | Notes |\n| --- | --- | --- | --- | --- | --- | --- | --- |\n|  |  |  |  |  |  |  |  |\n\n- [ ] Unsupported claim\n- [ ] Invented biography\n- [ ] Persona conflict\n- [ ] Anchor recitation\n- [ ] Awkward join\n- [ ] Repetition\n- [ ] Generic DJ language"); } }
        File.WriteAllText(Path.Combine(dir, "M9C5_EVALUATION.md"), evaluation.ToString(), new UTF8Encoding(false));
    }
}

internal static class M9C5ComparisonCommand
{
    internal static async Task<int> RunAsync()
    {
        Console.WriteLine("ZOYA RADIO - M9C.5 LOCAL-MODEL COMPARISON (GENERATE ONLY)");
        string root = Directory.GetCurrentDirectory(); if (!Directory.Exists(Path.Combine(root, ".git"))) return 1;
        using var cts = new CancellationTokenSource(); ConsoleCancelEventHandler handler = (_, e) => { e.Cancel = true; cts.Cancel(); }; Console.CancelKeyPress += handler;
        try { var runner = new M9C5ComparisonRunner(model => new OllamaClient(AiCommentConfiguration.Default with { Model = model }), AiCommentConfiguration.Default); MusicContextBenchmarkRunResult result = await runner.RunAsync(root, cts.Token); Console.WriteLine(result.Message); if (result.ArtifactDirectory is not null) Console.WriteLine("Artifacts: " + result.ArtifactDirectory); return result.Completed ? 0 : 1; }
        finally { Console.CancelKeyPress -= handler; }
    }
}