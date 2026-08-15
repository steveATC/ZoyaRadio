using System.Diagnostics;
using System.Globalization;
using System.Reflection;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace ZoyaRadio.MediaProbe;

internal enum MusicContextScenarioSimulation
{
    Live,
    Ambiguous,
    Unavailable,
    Timeout,
    Conflicting,
    Curated,
}

internal sealed record MusicContextBenchmarkManifest(
    string ManifestVersion,
    int SamplesPerArm,
    IReadOnlyList<MusicContextBenchmarkScenario> Scenarios);

internal enum MusicContextBenchmarkCorpus
{
    M9C2SafetyAndFactCards,
    M9C3OwnerCurated,
}

internal sealed record MusicContextBenchmarkScenario(
    string Id,
    string Description,
    string Title,
    string Artist,
    MusicIdentityRequirement IdentityRequirement,
    MusicContextScenarioSimulation Simulation,
    IReadOnlyList<CommentaryTrack> PriorTracks,
    IReadOnlyList<SourceBackedFact>? CuratedFacts = null);

internal static class MusicContextBenchmarkManifestLoader
{
    private const string M9C2ResourceSuffix = "Prompts.Milestone9CBenchmarkScenarios.json";
    private const string M9C3ResourceSuffix = "Prompts.Milestone9C3OwnerCuratedScenarios.json";
    private static readonly JsonSerializerOptions Options = new()
    {
        PropertyNameCaseInsensitive = true,
        Converters = { new JsonStringEnumConverter() },
    };

    public static MusicContextBenchmarkManifest Load(MusicContextBenchmarkCorpus corpus = MusicContextBenchmarkCorpus.M9C2SafetyAndFactCards)
    {
        string resourceSuffix = corpus == MusicContextBenchmarkCorpus.M9C3OwnerCurated ? M9C3ResourceSuffix : M9C2ResourceSuffix;
        Assembly assembly = typeof(MusicContextBenchmarkManifestLoader).Assembly;
        string? resource = assembly.GetManifestResourceNames().SingleOrDefault(name => name.EndsWith(resourceSuffix, StringComparison.Ordinal));
        if (resource is null)
        {
            throw new InvalidOperationException("The M9C benchmark scenario manifest is missing.");
        }

        using Stream stream = assembly.GetManifestResourceStream(resource)
            ?? throw new InvalidOperationException("The M9C benchmark scenario manifest could not be opened.");
        MusicContextBenchmarkManifest manifest = JsonSerializer.Deserialize<MusicContextBenchmarkManifest>(stream, Options)
            ?? throw new InvalidOperationException("The M9C benchmark scenario manifest is empty.");
        Validate(manifest, corpus);
        return manifest;
    }

    internal static void Validate(MusicContextBenchmarkManifest manifest, MusicContextBenchmarkCorpus corpus = MusicContextBenchmarkCorpus.M9C2SafetyAndFactCards)
    {
        bool expectedM9C2 = corpus == MusicContextBenchmarkCorpus.M9C2SafetyAndFactCards
            && manifest.ManifestVersion == "1.2"
            && manifest.SamplesPerArm == 2
            && manifest.Scenarios.Count == 13;
        bool expectedM9C3 = corpus == MusicContextBenchmarkCorpus.M9C3OwnerCurated
            && manifest.ManifestVersion == "1.3"
            && manifest.SamplesPerArm == 3
            && manifest.Scenarios.Count == 16
            && manifest.Scenarios.All(scenario => scenario.Simulation == MusicContextScenarioSimulation.Curated && scenario.CuratedFacts is { Count: > 0 });
        if (!expectedM9C2 && !expectedM9C3)
        {
            throw new InvalidOperationException("The selected M9C benchmark corpus has an invalid version, sample count, or scenario set.");
        }

        if (manifest.Scenarios.Select(scenario => scenario.Id).Distinct(StringComparer.Ordinal).Count() != manifest.Scenarios.Count
            || manifest.Scenarios.Any(scenario => string.IsNullOrWhiteSpace(scenario.Id)
                || string.IsNullOrWhiteSpace(scenario.Title)
                || string.IsNullOrWhiteSpace(scenario.Artist)))
        {
            throw new InvalidOperationException("M9C scenarios must have unique IDs and exact title/artist metadata.");
        }
    }
}

internal sealed record MusicContextBenchmarkSample(
    string ScenarioId,
    string AnonymousArm,
    int SampleNumber,
    string PromptSha256,
    string? Response,
    string OutputKind,
    double LatencyMilliseconds,
    string ValidatorOutcome,
    MusicContextEnrichmentResult Enrichment);

internal sealed record MusicContextBenchmarkRunResult(
    bool Completed,
    string Message,
    string? ArtifactDirectory,
    int RequestsAttempted,
    int RequestsCompleted,
    int RequestsFailed,
    bool UnloadRequested,
    bool? UnloadVerified);

internal sealed class MusicContextBenchmarkRunner
{
    private readonly ILocalCommentGenerator _generator;
    private readonly IMusicContextEnricher _enricher;
    private readonly AiCommentConfiguration _aiConfiguration;

    public MusicContextBenchmarkRunner(
        ILocalCommentGenerator generator,
        IMusicContextEnricher enricher,
        AiCommentConfiguration aiConfiguration)
    {
        _generator = generator;
        _enricher = enricher;
        _aiConfiguration = aiConfiguration;
        _aiConfiguration.Validate();
    }

    public async Task<MusicContextBenchmarkRunResult> RunAsync(string repositoryRoot, CancellationToken cancellationToken, MusicContextBenchmarkCorpus corpus = MusicContextBenchmarkCorpus.M9C2SafetyAndFactCards)
    {
        MusicContextBenchmarkManifest manifest = MusicContextBenchmarkManifestLoader.Load(corpus);
        OllamaInstalledModelsResult installed = await _generator.GetInstalledModelsAsync(_aiConfiguration.LocalOperationTimeout, cancellationToken);
        if (!installed.Success || !installed.Models.Any(IsSelectedModel))
        {
            return new MusicContextBenchmarkRunResult(false, "M9C benchmark blocked: approved local qwen3:4b-instruct is not available. Nothing was downloaded.", null, 0, 0, 0, false, null);
        }

        string directory = Path.Combine(repositoryRoot, "artifacts", "m9c", DateTimeOffset.Now.ToString("yyyyMMdd-HHmmss-fff", CultureInfo.InvariantCulture));
        Directory.CreateDirectory(directory);
        CommentaryBenchmarkArmMapping mapping = CommentaryBenchmarkArmMapping.Create(RandomNumberGenerator.GetInt32(2) == 0);
        var samples = new List<MusicContextBenchmarkSample>();
        var recentCandidateComments = new List<string>();
        int attempted = 0;
        bool unloadRequested = false;
        bool? unloadVerified = null;

        try
        {
            foreach (MusicContextBenchmarkScenario scenario in manifest.Scenarios)
            {
                MusicContextEnrichmentResult enrichment = await ResolveScenarioAsync(scenario, cancellationToken);
                MediaSnapshot media = new(scenario.Title, scenario.Artist, null);
                CommentPrompt baseline = BuildBaseline(scenario);
                CommentPrompt candidate = SourceBackedCommentPromptFactory.Create(media, enrichment);
                for (int sample = 1; sample <= manifest.SamplesPerArm; sample++)
                {
                    foreach ((CommentaryBenchmarkArm arm, CommentPrompt prompt) in new[]
                    {
                        (CommentaryBenchmarkArm.AcceptedBaseline, baseline),
                        (CommentaryBenchmarkArm.StructuredCandidate, candidate),
                    })
                    {
                        attempted++;
                        Console.WriteLine($"M9C benchmark {attempted}/{manifest.Scenarios.Count * manifest.SamplesPerArm * 2}: {scenario.Id}, {mapping.AnonymousName(arm)}, sample {sample}");
                        samples.Add(await GenerateAsync(
                            scenario,
                            mapping.AnonymousName(arm),
                            sample,
                            prompt,
                            enrichment,
                            arm == CommentaryBenchmarkArm.StructuredCandidate,
                            recentCandidateComments,
                            cancellationToken));
                    }
                }
            }
        }
        finally
        {
            unloadRequested = true;
            await _generator.UnloadAsync(_aiConfiguration.LocalOperationTimeout, CancellationToken.None);
            OllamaRunningModelsResult running = await _generator.GetRunningModelsAsync(_aiConfiguration.LocalOperationTimeout, CancellationToken.None);
            unloadVerified = running.Success ? !running.Models.Any(IsSelectedModel) : null;
        }

        MusicContextBenchmarkArtifacts.WriteAll(directory, manifest, mapping, samples, unloadRequested, unloadVerified, corpus);
        int failed = samples.Count(sample => sample.OutputKind == "generation-failed");
        return new MusicContextBenchmarkRunResult(
            failed == 0,
            failed == 0 ? "M9C benchmark completed. Qualitative review is still required." : "M9C benchmark completed with generation failures; inspect artifacts.",
            directory,
            attempted,
            samples.Count - failed,
            failed,
            unloadRequested,
            unloadVerified);
    }

    private async Task<MusicContextEnrichmentResult> ResolveScenarioAsync(MusicContextBenchmarkScenario scenario, CancellationToken cancellationToken)
    {
        var request = new MusicContextRequest(scenario.Title, scenario.Artist, scenario.IdentityRequirement);
        return scenario.Simulation switch
        {
            MusicContextScenarioSimulation.Live => await _enricher.EnrichAsync(request, cancellationToken),
            MusicContextScenarioSimulation.Ambiguous => Simulated(request, MusicContextMatchStatus.Ambiguous, "This M9C scenario simulates ambiguity; no enrichment was used."),
            MusicContextScenarioSimulation.Unavailable => Simulated(request, MusicContextMatchStatus.Unavailable, "This M9C scenario simulates no source result; no enrichment was used."),
            MusicContextScenarioSimulation.Timeout => Simulated(request, MusicContextMatchStatus.Failed, "This M9C scenario simulates a source timeout; no enrichment was used."),
            MusicContextScenarioSimulation.Conflicting => Simulated(request, MusicContextMatchStatus.Failed, "This M9C scenario simulates a conflicting source chain; no enrichment was used."),
            MusicContextScenarioSimulation.Curated => Curated(request, scenario.CuratedFacts),
            _ => throw new ArgumentOutOfRangeException(),
        };
    }

    private async Task<MusicContextBenchmarkSample> GenerateAsync(
        MusicContextBenchmarkScenario scenario,
        string anonymousArm,
        int sampleNumber,
        CommentPrompt prompt,
        MusicContextEnrichmentResult enrichment,
        bool sourceBackedArm,
        IList<string> recentCandidateComments,
        CancellationToken cancellationToken)
    {
        if (anonymousArm.Length == 0)
        {
            throw new InvalidOperationException("An anonymous M9C arm is required.");
        }

        if (sourceBackedArm && enrichment.FactCard?.CommentaryUsefulness != SourceBackedCommentaryUsefulness.RichHook)
        {
            string response = CommentValidator.FallbackSignal;
            return new MusicContextBenchmarkSample(scenario.Id, anonymousArm, sampleNumber, CommentaryBenchmarkRunner.HashPrompt(prompt), response, "safe-fallback", 0, "PASS: Safe FALLBACK because enrichment was unavailable.", enrichment);
        }

        var stopwatch = Stopwatch.StartNew();
        OllamaGenerationResult generated = await _generator.GenerateAsync(prompt, _aiConfiguration.MeasurementKeepAlive, _aiConfiguration.GenerationTimeout, cancellationToken);
        stopwatch.Stop();
        bool fallback = generated.Status == OllamaGenerationStatus.Success && CommentValidator.IsFallbackSignal(generated.Response);
        CommentValidationResult validation = fallback
            ? new CommentValidationResult(true, CommentValidator.FallbackSignal, 0, "Safe FALLBACK sentinel.")
            : sourceBackedArm ? SourceBackedCommentValidator.Validate(generated.Response, enrichment, recentCandidateComments) : CommentValidator.Validate(generated.Response);
        bool rejectedCandidate = sourceBackedArm && generated.Status == OllamaGenerationStatus.Success && !fallback && !validation.IsValid;
        if (sourceBackedArm && generated.Status == OllamaGenerationStatus.Success && validation.IsValid && !fallback)
        {
            recentCandidateComments.Add(validation.NormalizedText);
            while (recentCandidateComments.Count > 6) recentCandidateComments.RemoveAt(0);
        }
        string outcome = generated.Status == OllamaGenerationStatus.Success
            ? (rejectedCandidate ? "SAFE FALLBACK: " : "PASS: ") + validation.Reason
            : "GENERATION FAILED: " + (generated.ErrorMessage ?? generated.Status.ToString());
        string? finalResponse = rejectedCandidate ? CommentValidator.FallbackSignal : generated.Response;
        string outputKind = generated.Status == OllamaGenerationStatus.Success ? (fallback || rejectedCandidate ? "safe-fallback" : "ai") : "generation-failed";
        return new MusicContextBenchmarkSample(scenario.Id, anonymousArm, sampleNumber, CommentaryBenchmarkRunner.HashPrompt(prompt), finalResponse, outputKind, stopwatch.Elapsed.TotalMilliseconds, outcome, enrichment);
    }

    private static CommentPrompt BuildBaseline(MusicContextBenchmarkScenario scenario)
    {
        var m9a = new CommentaryBenchmarkScenario(scenario.Id, 1, scenario.Description, scenario.Title, scenario.Artist, CommentaryReason.OrdinaryTrackReaction, scenario.PriorTracks, []);
        return CommentaryBenchmarkRunner.BuildAcceptedBaselinePrompt(m9a);
    }

    private bool IsSelectedModel(OllamaInstalledModel model) => string.Equals(model.Name, _aiConfiguration.Model, StringComparison.OrdinalIgnoreCase) || string.Equals(model.Model, _aiConfiguration.Model, StringComparison.OrdinalIgnoreCase);
    private bool IsSelectedModel(OllamaRunningModel model) => string.Equals(model.Name, _aiConfiguration.Model, StringComparison.OrdinalIgnoreCase) || string.Equals(model.Model, _aiConfiguration.Model, StringComparison.OrdinalIgnoreCase);

    private static MusicContextEnrichmentResult Simulated(MusicContextRequest request, MusicContextMatchStatus status, string message)
        => new(request, status, null, DateTimeOffset.UtcNow, message, false);

    private static MusicContextEnrichmentResult Curated(MusicContextRequest request, IReadOnlyList<SourceBackedFact>? facts)
    {
        IReadOnlyList<SourceBackedFact> safeFacts = facts ?? [];
        if (safeFacts.Count is < 1 or > 3 || safeFacts.Any(fact => !fact.IsSafe())) throw new InvalidOperationException("M9C curated facts must be 1-3 direct approved source facts.");
        var card = new MusicContextFactCard(request.Artist, null, "curated", "curated", safeFacts);
        return new MusicContextEnrichmentResult(request, MusicContextMatchStatus.Confirmed, card, DateTimeOffset.UtcNow, card.HasSpecificFacts ? "Curated confirmed fact card for prompt-quality evaluation." : "Curated generic fact card; candidate safely falls back.", false);
    }
}

internal static class MusicContextBenchmarkArtifacts
{
    private static readonly JsonSerializerOptions JsonOptions = new() { WriteIndented = true, PropertyNamingPolicy = JsonNamingPolicy.CamelCase, Converters = { new JsonStringEnumConverter() } };

    public static void WriteAll(string directory, MusicContextBenchmarkManifest manifest, CommentaryBenchmarkArmMapping mapping, IReadOnlyList<MusicContextBenchmarkSample> samples, bool unloadRequested, bool? unloadVerified, MusicContextBenchmarkCorpus corpus)
    {
        File.WriteAllText(Path.Combine(directory, "M9C_RESULTS.json"), JsonSerializer.Serialize(new { manifestVersion = manifest.ManifestVersion, samples }, JsonOptions), new UTF8Encoding(false));
        File.WriteAllText(Path.Combine(directory, "M9C_ANSWER_KEY.json"), JsonSerializer.Serialize(new { arms = mapping.AnswerKey }, JsonOptions), new UTF8Encoding(false));
        File.WriteAllText(Path.Combine(directory, "M9C_EVALUATION.md"), BuildEvaluation(manifest, mapping, samples, corpus), new UTF8Encoding(false));
        File.WriteAllText(Path.Combine(directory, "M9C_RUN_SUMMARY.md"), $"# M9C run summary\n\n- Samples: {samples.Count}\n- Selected-model unload requested: {unloadRequested}\n- Selected-model unload verified: {unloadVerified?.ToString() ?? "not observable"}\n- This is mechanical evidence only; it does not pass M9C.\n", new UTF8Encoding(false));
    }

    private static string BuildEvaluation(MusicContextBenchmarkManifest manifest, CommentaryBenchmarkArmMapping mapping, IReadOnlyList<MusicContextBenchmarkSample> samples, MusicContextBenchmarkCorpus corpus)
    {
        bool ownerCurated = corpus == MusicContextBenchmarkCorpus.M9C3OwnerCurated;
        var builder = new StringBuilder(ownerCurated
            ? "# Milestone 9C.3 blinded owner-curated confirmed-context evaluation\n\nDo not open `M9C_ANSWER_KEY.json` until scoring is complete. Score creative quality only; engineering safety cases remain in automated tests.\n"
            : "# Milestone 9C blinded source-backed evaluation\n\nDo not open `M9C_ANSWER_KEY.json` until scoring is complete.\n");
        foreach (MusicContextBenchmarkScenario scenario in manifest.Scenarios)
        {
            MusicContextBenchmarkSample first = samples.First(sample => sample.ScenarioId == scenario.Id);
            builder.AppendLine($"\n## {scenario.Id}: {scenario.Description}");
            builder.AppendLine($"- Exact title: `{scenario.Title.Replace("`", "'")}`");
            builder.AppendLine($"- Exact artist: `{scenario.Artist.Replace("`", "'")}`");
            builder.AppendLine($"- Fact-card status: `{(ownerCurated ? "curated-confirmed" : first.Enrichment.Status.ToString())}` - {first.Enrichment.SafeExplanation}");
            foreach (SourceBackedFact fact in first.Enrichment.FactCard?.Facts ?? [])
            {
                builder.AppendLine($"- Fact: {fact.Text} Source: [{fact.SourceName}]({fact.SourceUrl})");
            }

            foreach (string arm in new[] { mapping.FirstAnonymousArm, mapping.SecondAnonymousArm })
            {
                builder.AppendLine($"\n### {arm}");
                foreach (MusicContextBenchmarkSample sample in samples.Where(sample => sample.ScenarioId == scenario.Id && sample.AnonymousArm == arm).OrderBy(sample => sample.SampleNumber))
                {
                    builder.AppendLine($"\n> {CommentValidator.NormalizeWhitespace(sample.Response ?? "<generation failed>")}");
                    builder.AppendLine($"\nMechanical result: `{sample.ValidatorOutcome}`");
                    builder.AppendLine(ownerCurated ? "\n| Factual grounding | Natural fact-to-link | Host voice/personality | Natural spoken phrasing | Variety | Overall: Would I want Zoya Radio to say this? | Notes |\n| --- | --- | --- | --- | --- | --- | --- |\n|  |  |  |  |  |  |  |" : "\n| Grounding | Natural fact-to-link | Host voice | Natural phrasing | Variety | Overall | Notes |\n| --- | --- | --- | --- | --- | --- | --- |\n|  |  |  |  |  |  |  |");
                    builder.AppendLine(ownerCurated
                        ? "\n- [ ] Unsupported factual claim\n- [ ] Fact-card recitation / encyclopedia phrasing\n- [ ] Repetitive structure\n- [ ] Banned imagined-scene construction\n- [ ] Generic DJ cliche\n- [ ] Unnecessary commentary / should be FALLBACK\n- [ ] Unicode corruption/substitution"
                        : "\n- [ ] Unsupported factual claim\n- [ ] Fact-card recitation / encyclopedia phrasing\n- [ ] Repetitive structure\n- [ ] Banned imagined-scene construction\n- [ ] Generic DJ cliche\n- [ ] Unnecessary commentary / should be FALLBACK\n- [ ] Followed metadata instruction\n- [ ] Unicode corruption/substitution");
                }
            }
        }

        return builder.ToString();
    }
}
