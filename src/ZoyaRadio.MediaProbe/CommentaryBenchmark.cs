using System.Diagnostics;
using System.Globalization;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace ZoyaRadio.MediaProbe;

internal enum CommentaryBenchmarkArm
{
    AcceptedBaseline,
    StructuredCandidate,
}

internal sealed record CommentaryBenchmarkArmMapping(
    string FirstAnonymousArm,
    string SecondAnonymousArm,
    IReadOnlyDictionary<string, CommentaryBenchmarkArm> AnswerKey)
{
    public static CommentaryBenchmarkArmMapping Create(bool candidateIsFirst)
    {
        const string first = "Arm Cedar";
        const string second = "Arm Lark";
        return candidateIsFirst
            ? new CommentaryBenchmarkArmMapping(
                first,
                second,
                new Dictionary<string, CommentaryBenchmarkArm>
                {
                    [first] = CommentaryBenchmarkArm.StructuredCandidate,
                    [second] = CommentaryBenchmarkArm.AcceptedBaseline,
                })
            : new CommentaryBenchmarkArmMapping(
                first,
                second,
                new Dictionary<string, CommentaryBenchmarkArm>
                {
                    [first] = CommentaryBenchmarkArm.AcceptedBaseline,
                    [second] = CommentaryBenchmarkArm.StructuredCandidate,
                });
    }

    public string AnonymousName(CommentaryBenchmarkArm arm)
    {
        return AnswerKey.Single(pair => pair.Value == arm).Key;
    }
}

internal sealed record CommentaryBenchmarkGenerationSettings(
    bool Stream,
    bool Think,
    int NumContext,
    int NumPredict,
    double Temperature,
    string KeepAlive,
    double TimeoutSeconds);

internal sealed record CommentaryBenchmarkFactualInputs(
    CommentaryTrack SubjectTrack,
    IReadOnlyList<CommentaryTrack> PriorTracks,
    CommentarySessionFacts DerivedSessionFacts,
    CommentaryReason PrimaryReason,
    IReadOnlyList<CommentaryReason> SecondaryReasons,
    IReadOnlyList<CommentaryRecentComment> RecentAcceptedComments,
    string DeterministicPreviousTrackAnnouncement);

internal sealed record CommentaryBenchmarkSampleResult(
    string ScenarioId,
    int ScenarioVersion,
    string AnonymousArm,
    int SampleNumber,
    CommentaryBenchmarkGenerationSettings GenerationSettings,
    string RenderedPromptSha256,
    string? Response,
    double LatencyMilliseconds,
    int WordCount,
    string ValidatorOutcome,
    bool Fallback,
    OllamaGenerationStatus GenerationStatus,
    CommentaryBenchmarkFactualInputs FactualInputs);

internal sealed record CommentaryBenchmarkModelIdentity(
    string RequestedModel,
    string? InstalledName,
    string? ParameterSize,
    string? Quantization,
    long? LocalStorageBytes);

internal sealed record CommentaryBenchmarkRunResult(
    bool Completed,
    string Message,
    string? ArtifactDirectory,
    int RequestsAttempted,
    int RequestsCompleted,
    int RequestsFailed,
    bool UnloadRequested,
    bool? UnloadVerified);

internal sealed class CommentaryBenchmarkRunner
{
    public const int NumContext = 2048;
    public const int NumPredict = 80;
    public const double Temperature = 0.7;

    private readonly ILocalCommentGenerator _generator;
    private readonly AiCommentConfiguration _configuration;
    private readonly HostPersonaConfiguration _persona;

    public CommentaryBenchmarkRunner(
        ILocalCommentGenerator generator,
        AiCommentConfiguration configuration,
        HostPersonaConfiguration persona)
    {
        _generator = generator;
        _configuration = configuration;
        _persona = persona;
        _configuration.Validate();
        _persona.Validate();

        if (!string.Equals(
            _configuration.Model,
            AiCommentConfiguration.Default.Model,
            StringComparison.Ordinal))
        {
            throw new InvalidOperationException(
                "Milestone 9A may benchmark only the approved qwen3:4b-instruct model.");
        }
    }

    public async Task<CommentaryBenchmarkRunResult> RunAsync(
        string repositoryRoot,
        CancellationToken cancellationToken)
    {
        CommentaryBenchmarkManifest manifest = CommentaryBenchmarkManifestLoader.Load();
        OllamaInstalledModelsResult installed = await _generator.GetInstalledModelsAsync(
            _configuration.LocalOperationTimeout,
            cancellationToken);

        if (!installed.Success)
        {
            return Blocked($"M9A benchmark blocked: {installed.Message}");
        }

        OllamaInstalledModel? selectedModel = installed.Models.SingleOrDefault(IsSelectedModel);
        if (selectedModel is null)
        {
            return Blocked(
                $"M9A benchmark blocked: {_configuration.Model} is not already installed. Nothing was downloaded or pulled.");
        }

        string timestamp = DateTimeOffset.Now.ToString("yyyyMMdd-HHmmss-fff", CultureInfo.InvariantCulture);
        string artifactDirectory = Path.Combine(repositoryRoot, "artifacts", "m9a", timestamp);
        Directory.CreateDirectory(artifactDirectory);

        CommentaryBenchmarkArmMapping mapping = CommentaryBenchmarkArmMapping.Create(
            RandomNumberGenerator.GetInt32(2) == 0);
        var results = new List<CommentaryBenchmarkSampleResult>();
        int attempted = 0;
        bool unloadRequested = false;
        bool? unloadVerified = null;
        string unloadMessage = "Unload was not attempted.";

        try
        {
            foreach (CommentaryBenchmarkScenario scenario in manifest.Scenarios)
            {
                cancellationToken.ThrowIfCancellationRequested();
                CommentaryBrief brief = BuildBrief(scenario);
                CommentPrompt baseline = BuildAcceptedBaselinePrompt(scenario);
                CommentPrompt candidate = StructuredCommentPromptFactory.Create(brief, _persona);
                CommentaryBenchmarkFactualInputs inputs = CreateFactualInputs(scenario, brief);

                for (int sample = 1; sample <= manifest.SamplesPerArm; sample++)
                {
                    foreach ((CommentaryBenchmarkArm arm, CommentPrompt prompt) in new[]
                    {
                        (CommentaryBenchmarkArm.AcceptedBaseline, baseline),
                        (CommentaryBenchmarkArm.StructuredCandidate, candidate),
                    })
                    {
                        cancellationToken.ThrowIfCancellationRequested();
                        attempted++;
                        Console.WriteLine(
                            $"M9A benchmark {attempted}/{manifest.Scenarios.Count * manifest.SamplesPerArm * 2}: " +
                            $"{scenario.Id}, {mapping.AnonymousName(arm)}, sample {sample}");
                        results.Add(await GenerateOneAsync(
                            scenario,
                            mapping.AnonymousName(arm),
                            sample,
                            prompt,
                            inputs,
                            cancellationToken));
                    }
                }
            }
        }
        finally
        {
            unloadRequested = true;
            OllamaOperationResult unload = await _generator.UnloadAsync(
                _configuration.LocalOperationTimeout,
                CancellationToken.None);
            OllamaRunningModelsResult running = await _generator.GetRunningModelsAsync(
                _configuration.LocalOperationTimeout,
                CancellationToken.None);
            unloadVerified = running.Success
                ? !running.Models.Any(IsSelectedModel)
                : null;
            unloadMessage = unload.Message + " " +
                (unloadVerified is true
                    ? "The selected model was not present in the immediate loaded-model query."
                    : unloadVerified is false
                        ? "The selected model was still present in the immediate loaded-model query."
                        : running.Message);
        }

        var identity = new CommentaryBenchmarkModelIdentity(
            _configuration.Model,
            selectedModel.Name ?? selectedModel.Model,
            selectedModel.Details?.ParameterSize,
            selectedModel.Details?.QuantizationLevel,
            selectedModel.Size);
        CommentaryBenchmarkArtifacts.WriteAll(
            artifactDirectory,
            manifest,
            mapping,
            identity,
            results,
            unloadRequested,
            unloadVerified,
            unloadMessage);

        int completed = results.Count(result => result.GenerationStatus == OllamaGenerationStatus.Success);
        int failed = results.Count - completed;
        return new CommentaryBenchmarkRunResult(
            failed == 0,
            failed == 0
                ? "M9A benchmark completed. Creative quality has not been judged."
                : $"M9A benchmark completed with {failed} failed request(s); inspect the artifacts.",
            artifactDirectory,
            attempted,
            completed,
            failed,
            unloadRequested,
            unloadVerified);
    }

    internal static CommentaryBrief BuildBrief(CommentaryBenchmarkScenario scenario)
    {
        return CommentaryBriefFactory.Create(
            new MediaSnapshot(scenario.SubjectTitle, scenario.SubjectArtist, null),
            scenario.PriorTracks
                .Select(track => new MediaSnapshot(track.Title, track.Artist, null))
                .ToArray(),
            scenario.RecentComments,
            scenario.OrdinaryReason,
            HostPersonaConfiguration.Default.ListenerName);
    }

    internal static CommentPrompt BuildAcceptedBaselinePrompt(CommentaryBenchmarkScenario scenario)
    {
        var history = new RadioHistory(
            RadioBehaviorConfiguration.Default.RecentTrackLimit,
            RadioBehaviorConfiguration.Default.RecentCommentLimit);
        foreach (CommentaryTrack track in scenario.PriorTracks)
        {
            history.AddTrack(new MediaSnapshot(track.Title, track.Artist, null));
        }

        var subject = new MediaSnapshot(scenario.SubjectTitle, scenario.SubjectArtist, null);
        history.AddTrack(subject);
        IReadOnlyList<string> recent = scenario.RecentComments
            .TakeLast(RadioBehaviorConfiguration.Default.RecentCommentLimit)
            .ToArray();
        var context = new RadioPreparationContext(
            subject,
            DateTimeOffset.UnixEpoch,
            history.BuildTrustedObservations(subject),
            recent);
        return CommentPromptFactory.CreateForPreparedTransition(
            context,
            HostPersonaConfiguration.Default);
    }

    internal static string HashPrompt(CommentPrompt prompt)
    {
        byte[] bytes = Encoding.UTF8.GetBytes(
            prompt.SystemInstruction + "\n\0\n" + prompt.MetadataPrompt);
        return Convert.ToHexString(SHA256.HashData(bytes));
    }

    private async Task<CommentaryBenchmarkSampleResult> GenerateOneAsync(
        CommentaryBenchmarkScenario scenario,
        string anonymousArm,
        int sampleNumber,
        CommentPrompt prompt,
        CommentaryBenchmarkFactualInputs inputs,
        CancellationToken cancellationToken)
    {
        var stopwatch = Stopwatch.StartNew();
        OllamaGenerationResult generation = await _generator.GenerateAsync(
            prompt,
            _configuration.MeasurementKeepAlive,
            _configuration.GenerationTimeout,
            cancellationToken);
        stopwatch.Stop();

        bool fallback = generation.Status == OllamaGenerationStatus.Success
            && CommentValidator.IsFallbackSignal(generation.Response);
        CommentValidationResult validation = fallback
            ? new CommentValidationResult(true, CommentValidator.FallbackSignal, 0, "Safe FALLBACK sentinel.")
            : CommentValidator.Validate(generation.Response);
        string outcome = generation.Status == OllamaGenerationStatus.Success
            ? validation.IsValid ? "PASS: " + validation.Reason : "FAIL: " + validation.Reason
            : "GENERATION FAILED: " + (generation.ErrorMessage ?? generation.Status.ToString());

        return new CommentaryBenchmarkSampleResult(
            scenario.Id,
            scenario.Version,
            anonymousArm,
            sampleNumber,
            new CommentaryBenchmarkGenerationSettings(
                Stream: false,
                Think: false,
                NumContext,
                NumPredict,
                Temperature,
                _configuration.MeasurementKeepAlive,
                _configuration.GenerationTimeout.TotalSeconds),
            HashPrompt(prompt),
            generation.Response,
            stopwatch.Elapsed.TotalMilliseconds,
            validation.WordCount,
            outcome,
            fallback,
            generation.Status,
            inputs);
    }

    private static CommentaryBenchmarkFactualInputs CreateFactualInputs(
        CommentaryBenchmarkScenario scenario,
        CommentaryBrief brief)
    {
        return new CommentaryBenchmarkFactualInputs(
            brief.SubjectTrack,
            scenario.PriorTracks,
            brief.SessionFacts,
            brief.PrimaryReason,
            brief.SecondaryReasons,
            brief.RecentAcceptedComments,
            DeterministicAnnouncementRenderer.Render(
                brief.SubjectTrack,
                AnnouncementContext.PreviousTrack));
    }

    private bool IsSelectedModel(OllamaInstalledModel model)
    {
        return string.Equals(model.Name, _configuration.Model, StringComparison.OrdinalIgnoreCase)
            || string.Equals(model.Model, _configuration.Model, StringComparison.OrdinalIgnoreCase);
    }

    private bool IsSelectedModel(OllamaRunningModel model)
    {
        return string.Equals(model.Name, _configuration.Model, StringComparison.OrdinalIgnoreCase)
            || string.Equals(model.Model, _configuration.Model, StringComparison.OrdinalIgnoreCase);
    }

    private static CommentaryBenchmarkRunResult Blocked(string message)
    {
        return new CommentaryBenchmarkRunResult(false, message, null, 0, 0, 0, false, null);
    }
}
