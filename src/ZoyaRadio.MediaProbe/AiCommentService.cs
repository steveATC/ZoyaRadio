using System.Diagnostics;
using System.Globalization;
using System.Text;

namespace ZoyaRadio.MediaProbe;

internal enum CommentTextSource
{
    Ai,
    GroundedAi,
    FixedFallback,
}

internal sealed record CommentSelection(
    string Text,
    CommentTextSource Source,
    int WordCount,
    string Reason,
    OllamaGenerationResult? Generation,
    bool ModelRequestedFallback);

internal sealed class AiCommentService
{
    private readonly ILocalCommentGenerator _generator;
    private readonly AiCommentConfiguration _configuration;
    private readonly IAnnouncementLog _log;
    private readonly HostPersonaConfiguration _persona;

    public AiCommentService(
        ILocalCommentGenerator generator,
        AiCommentConfiguration configuration,
        IAnnouncementLog log,
        HostPersonaConfiguration? persona = null)
    {
        _generator = generator;
        _configuration = configuration;
        _log = log;
        _persona = persona ?? HostPersonaConfiguration.Default;
        _persona.Validate();
    }

    public async Task<CommentSelection> SelectCommentAsync(
        MediaSnapshot media,
        bool aiEnabled,
        bool simulateAiFailure,
        CancellationToken cancellationToken)
    {
        return await SelectCommentCoreAsync(
            media,
            () => CommentPromptFactory.Create(media, _persona),
            [],
            aiEnabled,
            simulateAiFailure,
            cancellationToken);
    }

    public async Task<CommentSelection> SelectRadioCommentAsync(
        RadioCommentContext context,
        bool aiEnabled,
        CancellationToken cancellationToken)
    {
        return await SelectCommentCoreAsync(
            context.PreviousTrack,
            () => CommentPromptFactory.CreateForRadio(context, _persona),
            context.RecentAcceptedAiComments,
            aiEnabled,
            simulateAiFailure: false,
            cancellationToken);
    }

    public async Task<CommentSelection> SelectQuickReactionAsync(
        RadioPreparationContext context,
        bool aiEnabled,
        CancellationToken cancellationToken)
    {
        return await SelectCommentCoreAsync(
            context.SubjectTrack,
            () => QuickReactionPilotPrompt.Create(context),
            context.RecentAcceptedAiComments,
            aiEnabled,
            simulateAiFailure: false,
            cancellationToken,
            outputTransform: QuickReactionPilotPrompt.NormalizeForSpeech);
    }
    public async Task<CommentSelection> SelectPreparedTransitionCommentAsync(
        RadioPreparationContext context,
        bool aiEnabled,
        CancellationToken cancellationToken)
    {
        return await SelectCommentCoreAsync(
            context.SubjectTrack,
            () => CommentPromptFactory.CreateForPreparedTransition(context, _persona),
            context.RecentAcceptedAiComments,
            aiEnabled,
            simulateAiFailure: false,
            cancellationToken);
    }
    private async Task<CommentSelection> SelectCommentCoreAsync(
        MediaSnapshot fallbackMedia,
        Func<CommentPrompt> promptFactory,
        IReadOnlyList<string> recentAcceptedAiComments,
        bool aiEnabled,
        bool simulateAiFailure,
        CancellationToken cancellationToken,
        Func<string?, string?>? outputTransform = null)
    {
        if (!AnnouncementText.TryBuild(fallbackMedia, out string? fallback))
        {
            throw new ArgumentException("Title and artist are required for a comment.", nameof(fallbackMedia));
        }

        if (!aiEnabled)
        {
            _log.Write("AI DISABLED: no Ollama request was made.");
            return WriteFinalSelection(
                fallback,
                CommentTextSource.FixedFallback,
                "AI was explicitly disabled with --no-ai.",
                generation: null);
        }

        if (simulateAiFailure)
        {
            _log.Write("SIMULATED AI FAILURE: intentionally skipping the local Ollama request.");
            _log.Write("No model was loaded; no unload request was necessary.");
            return WriteFinalSelection(
                fallback,
                CommentTextSource.FixedFallback,
                "AI failure was intentionally simulated.",
                generation: null);
        }

        CommentPrompt prompt = promptFactory();
        OllamaGenerationResult generation;
        var wallClock = Stopwatch.StartNew();

        _log.Write("AI GENERATION STARTED");
        _log.Write($"Local Ollama endpoint: {_generator.Endpoint}");
        _log.Write($"Selected model: {_generator.ModelName}");
        _log.Write($"Generation timeout: {_configuration.GenerationTimeout.TotalSeconds:0} seconds");
        _log.Write($"keep_alive policy: {_configuration.NormalKeepAlive} (immediate unload)");

        try
        {
            generation = await _generator.GenerateAsync(
                prompt,
                _configuration.NormalKeepAlive,
                _configuration.GenerationTimeout,
                cancellationToken);
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            generation = new OllamaGenerationResult(
                OllamaGenerationStatus.Cancelled,
                null,
                "Comment generation was cancelled.",
                null);
        }
        catch (Exception exception)
        {
            generation = new OllamaGenerationResult(
                OllamaGenerationStatus.ConnectionFailure,
                null,
                $"Local Ollama request failed unexpectedly: {exception.Message}",
                null);
        }
        finally
        {
            wallClock.Stop();
        }

        WriteGenerationDiagnostics(generation, wallClock.Elapsed);

        try
        {
            if (generation.Status == OllamaGenerationStatus.Cancelled
                && cancellationToken.IsCancellationRequested)
            {
                throw new OperationCanceledException(cancellationToken);
            }

            if (generation.Status != OllamaGenerationStatus.Success)
            {
                _log.Write($"AI GENERATION FAILED: {generation.ErrorMessage ?? generation.Status.ToString()}");
                return WriteFinalSelection(
                    fallback,
                    CommentTextSource.FixedFallback,
                    generation.ErrorMessage ?? $"Ollama result: {generation.Status}.",
                    generation);
            }

            if (CommentValidator.IsFallbackSignal(generation.Response))
            {
                _log.Write("AI SAFE FALLBACK REQUESTED: the model returned exactly FALLBACK.");
                _log.Write("FALLBACK is safe successful behavior; the sentinel itself will not be spoken.");
                return WriteFinalSelection(
                    fallback,
                    CommentTextSource.FixedFallback,
                    "The model explicitly requested the safe fixed fallback.",
                    generation,
                    modelRequestedFallback: true);
            }

            string? outputForValidation = outputTransform is null
                ? generation.Response
                : outputTransform(generation.Response);
            CommentValidationResult validation = CommentValidator.Validate(outputForValidation);
            _log.Write($"AI output word count: {validation.WordCount}");
            _log.Write($"AI output validation: {(validation.IsValid ? "PASS" : "FAIL")} - {validation.Reason}");

            if (!validation.IsValid)
            {
                _log.Write($"Rejected AI output: {SafeForLog(generation.Response)}");
                return WriteFinalSelection(
                    fallback,
                    CommentTextSource.FixedFallback,
                    validation.Reason,
                    generation);
            }

            if (IsExactRecentComment(validation.NormalizedText, recentAcceptedAiComments))
            {
                _log.Write("AI output repetition check: FAIL - exact duplicate of a recent accepted AI comment.");
                _log.Write("No regeneration loop is used; selecting the fixed fallback to conserve local GPU work.");
                return WriteFinalSelection(
                    fallback,
                    CommentTextSource.FixedFallback,
                    "The AI output exactly duplicated a recent accepted AI comment.",
                    generation);
            }

            _log.Write($"AI comment: {validation.NormalizedText}");
            return WriteFinalSelection(
                validation.NormalizedText,
                CommentTextSource.Ai,
                validation.Reason,
                generation);
        }
        finally
        {
            await RequestAndVerifyUnloadAsync();
        }
    }

    internal static bool IsExactRecentComment(
        string candidate,
        IReadOnlyList<string> recentAcceptedAiComments)
    {
        string normalizedCandidate = RadioHistory.NormalizeForComparison(candidate);
        return recentAcceptedAiComments.Any(
            recent => string.Equals(
                RadioHistory.NormalizeForComparison(recent),
                normalizedCandidate,
                StringComparison.OrdinalIgnoreCase));
    }

    public void WriteGenerationDiagnostics(OllamaGenerationResult generation, TimeSpan wallClock)
    {
        _log.Write($"AI generation status: {generation.Status}");
        _log.Write($"Observed wall-clock generation time: {FormatDuration(wallClock)}");

        if (generation.Metrics is not null)
        {
            _log.Write(
                $"Ollama total_duration: {FormatNanoseconds(generation.Metrics.TotalDurationNanoseconds)}");
            _log.Write(
                $"Ollama load_duration: {FormatNanoseconds(generation.Metrics.LoadDurationNanoseconds)}");
            _log.Write(
                $"Ollama prompt evaluation: {DisplayCount(generation.Metrics.PromptEvaluationCount)} tokens in " +
                $"{FormatNanoseconds(generation.Metrics.PromptEvaluationDurationNanoseconds)}");
            _log.Write(
                $"Ollama generation: {DisplayCount(generation.Metrics.EvaluationCount)} tokens in " +
                $"{FormatNanoseconds(generation.Metrics.EvaluationDurationNanoseconds)}");
        }
        else
        {
            _log.Write("Ollama timing metrics: <not available>");
        }
    }

    private async Task RequestAndVerifyUnloadAsync()
    {
        _log.Write($"MODEL UNLOAD REQUESTED: {_generator.ModelName} with keep_alive: 0");
        OllamaOperationResult unload = await _generator.UnloadAsync(
            _configuration.LocalOperationTimeout,
            CancellationToken.None);
        _log.Write($"Model unload request result: {(unload.Success ? "ACCEPTED" : "FAILED")} - {unload.Message}");

        OllamaRunningModelsResult running = await _generator.GetRunningModelsAsync(
            _configuration.LocalOperationTimeout,
            CancellationToken.None);

        if (!running.Success)
        {
            _log.Write($"Selected-model unload verification: UNAVAILABLE - {running.Message}");
            return;
        }

        bool selectedStillLoaded = running.Models.Any(IsSelectedModel);
        _log.Write($"Selected-model unload verification: {(selectedStillLoaded ? "FAILED - STILL LOADED" : "PASS - NOT LOADED")}");

        int unrelatedLoaded = running.Models.Count(model => !IsSelectedModel(model));
        _log.Write($"Unrelated loaded models observed and left untouched: {unrelatedLoaded}");
    }

    private CommentSelection WriteFinalSelection(
        string text,
        CommentTextSource source,
        string reason,
        OllamaGenerationResult? generation,
        bool modelRequestedFallback = false)
    {
        int wordCount = CommentValidator.CountWords(CommentValidator.NormalizeWhitespace(text));
        _log.Write($"FINAL TEXT SOURCE: {(source == CommentTextSource.Ai ? "AI" : "FIXED FALLBACK")}");
        _log.Write($"FINAL TEXT: {text}");
        return new CommentSelection(text, source, wordCount, reason, generation, modelRequestedFallback);
    }

    private bool IsSelectedModel(OllamaRunningModel model)
    {
        return string.Equals(model.Name, _generator.ModelName, StringComparison.OrdinalIgnoreCase)
            || string.Equals(model.Model, _generator.ModelName, StringComparison.OrdinalIgnoreCase);
    }

    private static string SafeForLog(string? text)
    {
        if (string.IsNullOrWhiteSpace(text))
        {
            return "<empty>";
        }

        var builder = new StringBuilder(Math.Min(text.Length, 500));

        foreach (char character in text)
        {
            if (builder.Length >= 500)
            {
                builder.Append("...");
                break;
            }

            builder.Append(char.IsControl(character) ? ' ' : character);
        }

        return CommentValidator.NormalizeWhitespace(builder.ToString());
    }

    internal static string FormatNanoseconds(long? nanoseconds)
    {
        return nanoseconds is null
            ? "<not available>"
            : FormatDuration(TimeSpan.FromTicks(nanoseconds.Value / 100));
    }

    private static string FormatDuration(TimeSpan duration)
    {
        return duration.TotalSeconds >= 1
            ? $"{duration.TotalSeconds.ToString("0.000", CultureInfo.InvariantCulture)} s"
            : $"{duration.TotalMilliseconds.ToString("0.0", CultureInfo.InvariantCulture)} ms";
    }

    private static string DisplayCount(int? count)
    {
        return count?.ToString(CultureInfo.InvariantCulture) ?? "<not available>";
    }
}
