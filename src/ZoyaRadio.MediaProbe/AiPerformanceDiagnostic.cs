using System.Diagnostics;
using System.Globalization;

namespace ZoyaRadio.MediaProbe;

internal sealed record AiPerformanceResult(
    bool GenerationSucceeded,
    bool ModelObservedLoaded,
    bool ModelUnloadVerified);

internal sealed class AiPerformanceDiagnostic
{
    private readonly ILocalCommentGenerator _generator;
    private readonly AiCommentConfiguration _configuration;
    private readonly IAnnouncementLog _log;

    public AiPerformanceDiagnostic(
        ILocalCommentGenerator generator,
        AiCommentConfiguration configuration,
        IAnnouncementLog log)
    {
        _generator = generator;
        _configuration = configuration;
        _log = log;
    }

    public async Task<AiPerformanceResult> RunAsync(
        MediaSnapshot media,
        CancellationToken cancellationToken)
    {
        CommentPrompt prompt = CommentPromptFactory.Create(media);
        bool generationSucceeded = false;
        bool modelObservedLoaded = false;
        bool unloadVerified = false;

        _log.Write("AI PERFORMANCE DIAGNOSTIC STARTED");
        _log.Write($"Selected model: {_generator.ModelName}");
        _log.Write($"Local Ollama endpoint: {_generator.Endpoint}");
        _log.Write($"Temporary diagnostic keep_alive: {_configuration.MeasurementKeepAlive}");
        _log.Write("No playback or speech commands will be issued in this mode.");

        try
        {
            var wallClock = Stopwatch.StartNew();
            OllamaGenerationResult generation = await _generator.GenerateAsync(
                prompt,
                _configuration.MeasurementKeepAlive,
                _configuration.GenerationTimeout,
                cancellationToken);
            wallClock.Stop();

            var diagnostics = new AiCommentService(_generator, _configuration, _log);
            diagnostics.WriteGenerationDiagnostics(generation, wallClock.Elapsed);
            generationSucceeded = generation.Status == OllamaGenerationStatus.Success;

            if (generationSucceeded)
            {
                if (CommentValidator.IsFallbackSignal(generation.Response))
                {
                    _log.Write("Diagnostic AI output: FALLBACK");
                    _log.Write("Diagnostic output validation: PASS - safe fixed fallback requested.");
                }
                else
                {
                    CommentValidationResult validation = CommentValidator.Validate(generation.Response);
                    _log.Write($"Diagnostic AI output: {validation.NormalizedText}");
                    _log.Write($"Diagnostic output word count: {validation.WordCount}");
                    _log.Write(
                        $"Diagnostic output validation: {(validation.IsValid ? "PASS" : "FAIL")} - {validation.Reason}");
                }
            }
            else
            {
                _log.Write($"AI PERFORMANCE GENERATION FAILED: {generation.ErrorMessage ?? generation.Status.ToString()}");
            }

            OllamaRunningModelsResult running = await _generator.GetRunningModelsAsync(
                _configuration.LocalOperationTimeout,
                cancellationToken);

            if (!running.Success)
            {
                _log.Write($"MEASURED loaded-model information: UNAVAILABLE - {running.Message}");
            }
            else
            {
                OllamaRunningModel? selected = running.Models.FirstOrDefault(IsSelectedModel);
                modelObservedLoaded = selected is not null;

                if (selected is null)
                {
                    _log.Write("MEASURED selected model in /api/ps: NOT OBSERVED");
                    _log.Write("MEASURED parameter size: UNAVAILABLE");
                    _log.Write("MEASURED quantization: UNAVAILABLE");
                    _log.Write("MEASURED loaded size: UNAVAILABLE");
                    _log.Write("MEASURED VRAM size: UNAVAILABLE");
                    _log.Write("MEASURED context length: UNAVAILABLE");
                }
                else
                {
                    _log.Write("MEASURED selected model in /api/ps: OBSERVED");
                    _log.Write($"MEASURED model name: {Display(selected.Name ?? selected.Model)}");
                    _log.Write($"MEASURED parameter size: {Display(selected.Details?.ParameterSize)}");
                    _log.Write($"MEASURED quantization: {Display(selected.Details?.QuantizationLevel)}");
                    _log.Write($"MEASURED loaded size: {FormatBytes(selected.Size)}");
                    _log.Write($"MEASURED VRAM size: {FormatBytes(selected.SizeVram)}");
                    _log.Write($"MEASURED context length: {DisplayNumber(selected.ContextLength)}");
                }

                int unrelated = running.Models.Count(model => !IsSelectedModel(model));
                _log.Write($"Unrelated loaded models observed and left untouched: {unrelated}");
            }
        }
        finally
        {
            _log.Write($"MODEL UNLOAD REQUESTED: {_generator.ModelName} with keep_alive: 0");
            OllamaOperationResult unload = await _generator.UnloadAsync(
                _configuration.LocalOperationTimeout,
                CancellationToken.None);
            _log.Write($"Model unload request result: {(unload.Success ? "ACCEPTED" : "FAILED")} - {unload.Message}");

            for (int attempt = 1; attempt <= 3; attempt++)
            {
                OllamaRunningModelsResult after = await _generator.GetRunningModelsAsync(
                    _configuration.LocalOperationTimeout,
                    CancellationToken.None);

                if (!after.Success)
                {
                    _log.Write($"Selected-model unload verification attempt {attempt}: UNAVAILABLE - {after.Message}");
                }
                else if (!after.Models.Any(IsSelectedModel))
                {
                    unloadVerified = true;
                    _log.Write("Selected-model unload verification: PASS - NOT LOADED");
                    break;
                }
                else
                {
                    _log.Write($"Selected-model unload verification attempt {attempt}: STILL LOADED");
                }

                if (attempt < 3)
                {
                    await Task.Delay(TimeSpan.FromMilliseconds(250));
                }
            }
        }

        bool passed = generationSucceeded && modelObservedLoaded && unloadVerified;
        _log.Write($"AI PERFORMANCE DIAGNOSTIC RESULT: {(passed ? "PASS" : "FAIL")}");
        return new AiPerformanceResult(generationSucceeded, modelObservedLoaded, unloadVerified);
    }

    private bool IsSelectedModel(OllamaRunningModel model)
    {
        return string.Equals(model.Name, _generator.ModelName, StringComparison.OrdinalIgnoreCase)
            || string.Equals(model.Model, _generator.ModelName, StringComparison.OrdinalIgnoreCase);
    }

    private static string Display(string? value)
    {
        return string.IsNullOrWhiteSpace(value) ? "UNAVAILABLE" : value;
    }

    private static string DisplayNumber(int? value)
    {
        return value?.ToString(CultureInfo.InvariantCulture) ?? "UNAVAILABLE";
    }

    internal static string FormatBytes(long? bytes)
    {
        if (bytes is null)
        {
            return "UNAVAILABLE";
        }

        double gibibytes = bytes.Value / 1024d / 1024d / 1024d;
        return $"{bytes.Value.ToString("N0", CultureInfo.InvariantCulture)} bytes ({gibibytes.ToString("0.00", CultureInfo.InvariantCulture)} GiB)";
    }
}
