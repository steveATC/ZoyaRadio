using System.Net;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace ZoyaRadio.MediaProbe;

internal interface ILocalCommentGenerator
{
    string ModelName { get; }

    Uri Endpoint { get; }

    Task<OllamaGenerationResult> GenerateAsync(
        CommentPrompt prompt,
        string keepAlive,
        TimeSpan timeout,
        CancellationToken cancellationToken);

    Task<OllamaOperationResult> UnloadAsync(
        TimeSpan timeout,
        CancellationToken cancellationToken);

    Task<OllamaRunningModelsResult> GetRunningModelsAsync(
        TimeSpan timeout,
        CancellationToken cancellationToken);

    Task<OllamaInstalledModelsResult> GetInstalledModelsAsync(
        TimeSpan timeout,
        CancellationToken cancellationToken);
}

internal enum OllamaGenerationStatus
{
    Success,
    HttpFailure,
    ConnectionFailure,
    TimedOut,
    Cancelled,
    MalformedResponse,
}

internal sealed record OllamaGenerationMetrics(
    long? TotalDurationNanoseconds,
    long? LoadDurationNanoseconds,
    int? PromptEvaluationCount,
    long? PromptEvaluationDurationNanoseconds,
    int? EvaluationCount,
    long? EvaluationDurationNanoseconds);

internal sealed record OllamaGenerationResult(
    OllamaGenerationStatus Status,
    string? Response,
    string? ErrorMessage,
    OllamaGenerationMetrics? Metrics);

internal sealed record OllamaOperationResult(bool Success, string Message);

internal sealed record OllamaModelDetails(
    [property: JsonPropertyName("parameter_size")] string? ParameterSize,
    [property: JsonPropertyName("quantization_level")] string? QuantizationLevel);

internal sealed record OllamaRunningModel(
    [property: JsonPropertyName("name")] string? Name,
    [property: JsonPropertyName("model")] string? Model,
    [property: JsonPropertyName("size")] long? Size,
    [property: JsonPropertyName("size_vram")] long? SizeVram,
    [property: JsonPropertyName("context_length")] int? ContextLength,
    [property: JsonPropertyName("details")] OllamaModelDetails? Details);

internal sealed record OllamaRunningModelsResult(
    bool Success,
    IReadOnlyList<OllamaRunningModel> Models,
    string Message);

internal sealed record OllamaInstalledModel(
    [property: JsonPropertyName("name")] string? Name,
    [property: JsonPropertyName("model")] string? Model,
    [property: JsonPropertyName("size")] long? Size,
    [property: JsonPropertyName("details")] OllamaModelDetails? Details);

internal sealed record OllamaInstalledModelsResult(
    bool Success,
    IReadOnlyList<OllamaInstalledModel> Models,
    string Message);

internal sealed class OllamaClient : ILocalCommentGenerator, IDisposable
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true,
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
    };

    private readonly HttpClient _httpClient;
    private readonly bool _ownsHttpClient;

    public OllamaClient(AiCommentConfiguration configuration, HttpClient? httpClient = null)
    {
        configuration.Validate();
        Endpoint = configuration.Endpoint;
        ModelName = configuration.Model;
        _httpClient = httpClient ?? new HttpClient { Timeout = Timeout.InfiniteTimeSpan };
        _ownsHttpClient = httpClient is null;
    }

    public string ModelName { get; }

    public Uri Endpoint { get; }

    public async Task<OllamaGenerationResult> GenerateAsync(
        CommentPrompt prompt,
        string keepAlive,
        TimeSpan timeout,
        CancellationToken cancellationToken)
    {
        var requestBody = new OllamaGenerateRequest(
            ModelName,
            prompt.MetadataPrompt,
            prompt.SystemInstruction,
            Stream: false,
            Think: false,
            KeepAlive: keepAlive,
            new OllamaGenerationOptions(
                NumContext: 2048,
                NumPredict: 80,
                Temperature: 0.7));

        try
        {
            using HttpResponseMessage response = await SendJsonAsync(
                "generate",
                requestBody,
                timeout,
                cancellationToken);
            string body = await response.Content.ReadAsStringAsync(cancellationToken);

            if (!response.IsSuccessStatusCode)
            {
                return new OllamaGenerationResult(
                    OllamaGenerationStatus.HttpFailure,
                    null,
                    $"Local Ollama returned HTTP {(int)response.StatusCode} ({response.StatusCode}): {ReadError(body)}",
                    null);
            }

            OllamaGenerateResponse? generated;

            try
            {
                generated = JsonSerializer.Deserialize<OllamaGenerateResponse>(body, JsonOptions);
            }
            catch (JsonException exception)
            {
                return new OllamaGenerationResult(
                    OllamaGenerationStatus.MalformedResponse,
                    null,
                    $"Local Ollama returned invalid JSON: {exception.Message}",
                    null);
            }

            if (generated is null || !generated.Done || generated.Response is null)
            {
                return new OllamaGenerationResult(
                    OllamaGenerationStatus.MalformedResponse,
                    null,
                    "Local Ollama returned an incomplete non-streaming response.",
                    null);
            }

            var metrics = new OllamaGenerationMetrics(
                generated.TotalDuration,
                generated.LoadDuration,
                generated.PromptEvalCount,
                generated.PromptEvalDuration,
                generated.EvalCount,
                generated.EvalDuration);
            return new OllamaGenerationResult(
                OllamaGenerationStatus.Success,
                generated.Response,
                null,
                metrics);
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            return new OllamaGenerationResult(
                OllamaGenerationStatus.Cancelled,
                null,
                "Comment generation was cancelled.",
                null);
        }
        catch (OperationCanceledException)
        {
            return new OllamaGenerationResult(
                OllamaGenerationStatus.TimedOut,
                null,
                $"Local generation did not complete within {timeout.TotalSeconds:0.#} seconds.",
                null);
        }
        catch (HttpRequestException exception)
        {
            return new OllamaGenerationResult(
                OllamaGenerationStatus.ConnectionFailure,
                null,
                $"Could not reach local Ollama: {exception.Message}",
                null);
        }
        catch (Exception exception)
        {
            return new OllamaGenerationResult(
                OllamaGenerationStatus.ConnectionFailure,
                null,
                $"Local Ollama request failed: {exception.Message}",
                null);
        }
    }

    public async Task<OllamaOperationResult> UnloadAsync(
        TimeSpan timeout,
        CancellationToken cancellationToken)
    {
        try
        {
            var request = new OllamaUnloadRequest(ModelName, "0");
            using HttpResponseMessage response = await SendJsonAsync(
                "generate",
                request,
                timeout,
                cancellationToken);
            string body = await response.Content.ReadAsStringAsync(cancellationToken);

            return response.IsSuccessStatusCode
                ? new OllamaOperationResult(true, "Local Ollama accepted the selected-model unload request.")
                : new OllamaOperationResult(
                    false,
                    $"Unload returned HTTP {(int)response.StatusCode} ({response.StatusCode}): {ReadError(body)}");
        }
        catch (OperationCanceledException)
        {
            return new OllamaOperationResult(false, "The selected-model unload request timed out or was cancelled.");
        }
        catch (Exception exception)
        {
            return new OllamaOperationResult(false, $"Selected-model unload failed: {exception.Message}");
        }
    }

    public async Task<OllamaRunningModelsResult> GetRunningModelsAsync(
        TimeSpan timeout,
        CancellationToken cancellationToken)
    {
        try
        {
            using HttpResponseMessage response = await SendAsync(
                HttpMethod.Get,
                "ps",
                content: null,
                timeout,
                cancellationToken);
            string body = await response.Content.ReadAsStringAsync(cancellationToken);

            if (!response.IsSuccessStatusCode)
            {
                return new OllamaRunningModelsResult(
                    false,
                    [],
                    $"Loaded-model query returned HTTP {(int)response.StatusCode} ({response.StatusCode}).");
            }

            OllamaPsResponse? parsed = JsonSerializer.Deserialize<OllamaPsResponse>(body, JsonOptions);
            return parsed?.Models is null
                ? new OllamaRunningModelsResult(false, [], "Loaded-model query returned malformed JSON.")
                : new OllamaRunningModelsResult(true, parsed.Models, "Loaded-model query succeeded.");
        }
        catch (OperationCanceledException)
        {
            return new OllamaRunningModelsResult(false, [], "Loaded-model query timed out or was cancelled.");
        }
        catch (Exception exception)
        {
            return new OllamaRunningModelsResult(false, [], $"Loaded-model query failed: {exception.Message}");
        }
    }

    public async Task<OllamaInstalledModelsResult> GetInstalledModelsAsync(
        TimeSpan timeout,
        CancellationToken cancellationToken)
    {
        try
        {
            using HttpResponseMessage response = await SendAsync(
                HttpMethod.Get,
                "tags",
                content: null,
                timeout,
                cancellationToken);
            string body = await response.Content.ReadAsStringAsync(cancellationToken);

            if (!response.IsSuccessStatusCode)
            {
                return new OllamaInstalledModelsResult(
                    false,
                    [],
                    $"Installed-model query returned HTTP {(int)response.StatusCode} ({response.StatusCode}).");
            }

            OllamaTagsResponse? parsed = JsonSerializer.Deserialize<OllamaTagsResponse>(body, JsonOptions);
            return parsed?.Models is null
                ? new OllamaInstalledModelsResult(false, [], "Installed-model query returned malformed JSON.")
                : new OllamaInstalledModelsResult(true, parsed.Models, "Installed-model query succeeded.");
        }
        catch (OperationCanceledException)
        {
            return new OllamaInstalledModelsResult(false, [], "Installed-model query timed out or was cancelled.");
        }
        catch (Exception exception)
        {
            return new OllamaInstalledModelsResult(false, [], $"Installed-model query failed: {exception.Message}");
        }
    }

    public void Dispose()
    {
        if (_ownsHttpClient)
        {
            _httpClient.Dispose();
        }
    }

    private async Task<HttpResponseMessage> SendJsonAsync<T>(
        string relativePath,
        T body,
        TimeSpan timeout,
        CancellationToken cancellationToken)
    {
        string json = JsonSerializer.Serialize(body, JsonOptions);
        var content = new StringContent(json, Encoding.UTF8, "application/json");
        return await SendAsync(HttpMethod.Post, relativePath, content, timeout, cancellationToken);
    }

    private async Task<HttpResponseMessage> SendAsync(
        HttpMethod method,
        string relativePath,
        HttpContent? content,
        TimeSpan timeout,
        CancellationToken cancellationToken)
    {
        using var request = new HttpRequestMessage(method, new Uri(Endpoint, relativePath))
        {
            Content = content,
        };
        using var timeoutCancellation = new CancellationTokenSource(timeout);
        using var linkedCancellation = CancellationTokenSource.CreateLinkedTokenSource(
            cancellationToken,
            timeoutCancellation.Token);
        return await _httpClient.SendAsync(request, HttpCompletionOption.ResponseContentRead, linkedCancellation.Token);
    }

    private static string ReadError(string body)
    {
        try
        {
            OllamaErrorResponse? error = JsonSerializer.Deserialize<OllamaErrorResponse>(body, JsonOptions);
            if (!string.IsNullOrWhiteSpace(error?.Error))
            {
                return LimitForLog(error.Error);
            }
        }
        catch (JsonException)
        {
        }

        return string.IsNullOrWhiteSpace(body) ? "no error details" : LimitForLog(body);
    }

    private static string LimitForLog(string value)
    {
        string normalized = CommentValidator.NormalizeWhitespace(value);
        return normalized.Length <= 300 ? normalized : $"{normalized[..300]}...";
    }

    private sealed record OllamaGenerateRequest(
        [property: JsonPropertyName("model")] string Model,
        [property: JsonPropertyName("prompt")] string Prompt,
        [property: JsonPropertyName("system")] string System,
        [property: JsonPropertyName("stream")] bool Stream,
        [property: JsonPropertyName("think")] bool Think,
        [property: JsonPropertyName("keep_alive")] string KeepAlive,
        [property: JsonPropertyName("options")] OllamaGenerationOptions Options);

    private sealed record OllamaGenerationOptions(
        [property: JsonPropertyName("num_ctx")] int NumContext,
        [property: JsonPropertyName("num_predict")] int NumPredict,
        [property: JsonPropertyName("temperature")] double Temperature);

    private sealed record OllamaUnloadRequest(
        [property: JsonPropertyName("model")] string Model,
        [property: JsonPropertyName("keep_alive")] string KeepAlive);

    private sealed record OllamaGenerateResponse(
        [property: JsonPropertyName("response")] string? Response,
        [property: JsonPropertyName("done")] bool Done,
        [property: JsonPropertyName("total_duration")] long? TotalDuration,
        [property: JsonPropertyName("load_duration")] long? LoadDuration,
        [property: JsonPropertyName("prompt_eval_count")] int? PromptEvalCount,
        [property: JsonPropertyName("prompt_eval_duration")] long? PromptEvalDuration,
        [property: JsonPropertyName("eval_count")] int? EvalCount,
        [property: JsonPropertyName("eval_duration")] long? EvalDuration);

    private sealed record OllamaErrorResponse(
        [property: JsonPropertyName("error")] string? Error);

    private sealed record OllamaPsResponse(
        [property: JsonPropertyName("models")] IReadOnlyList<OllamaRunningModel>? Models);

    private sealed record OllamaTagsResponse(
        [property: JsonPropertyName("models")] IReadOnlyList<OllamaInstalledModel>? Models);
}
