using System.Net;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace ZoyaRadio.MediaProbe;

public enum OllamaReadinessState
{
    Absent,
    InstalledStopped,
    RunningCompatible,
    Unreachable,
    Incompatible,
}

public enum QwenReadinessState
{
    Unavailable,
    Missing,
    Present,
}

public sealed record OllamaReadinessResult(
    OllamaReadinessState Ollama,
    QwenReadinessState Qwen,
    string? Version,
    string? ModelDigest,
    long? ModelSize,
    string Message);

public sealed record OllamaPullProgress(
    string Status,
    long? Completed,
    long? Total,
    double? Percent);

public enum OllamaPullStatus
{
    Completed,
    ConsentRequired,
    InsufficientSpace,
    Unavailable,
    Cancelled,
    TimedOut,
    Failed,
}

public sealed record OllamaPullResult(OllamaPullStatus Status, string Message)
{
    public bool Succeeded => Status == OllamaPullStatus.Completed;
}

public static class OllamaSetupPolicy
{
    public const long ExpectedModelDownloadBytes = 2_900_000_000;
    public const long MinimumFreeBytesBeforePull = 8L * 1024 * 1024 * 1024;

    public static string DownloadDisclosure =>
        "qwen3:4b-instruct is obtained separately through Ollama. " +
        "Expect approximately 2.7 GiB of model data. Zoya Radio requires at least " +
        "8 GiB free before starting so the download, temporary data, package, and recovery " +
        "headroom fit safely. Ollama and the model remain subject to their own terms.";
}

internal interface IOllamaSetupApi : IDisposable
{
    Task<OllamaReadinessResult> CheckAsync(
        bool installed,
        bool running,
        CancellationToken cancellationToken);

    Task<OllamaPullResult> PullModelAsync(
        bool explicitConsent,
        long? availableBytes,
        IProgress<OllamaPullProgress>? progress,
        CancellationToken cancellationToken);
}

internal sealed class OllamaSetupClient : IOllamaSetupApi
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true,
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
    };

    private readonly HttpClient _httpClient;
    private readonly bool _ownsHttpClient;
    private readonly AiCommentConfiguration _configuration;

    public OllamaSetupClient(
        AiCommentConfiguration configuration,
        HttpClient? httpClient = null)
    {
        configuration.Validate();
        _configuration = configuration;
        _httpClient = httpClient ?? new HttpClient { Timeout = Timeout.InfiniteTimeSpan };
        _ownsHttpClient = httpClient is null;
    }

    public async Task<OllamaReadinessResult> CheckAsync(
        bool installed,
        bool running,
        CancellationToken cancellationToken)
    {
        if (!installed)
        {
            return new OllamaReadinessResult(
                OllamaReadinessState.Absent,
                QwenReadinessState.Unavailable,
                null,
                null,
                null,
                "Ollama is not installed. Install it separately from the official Ollama website.");
        }

        if (!running)
        {
            return new OllamaReadinessResult(
                OllamaReadinessState.InstalledStopped,
                QwenReadinessState.Unavailable,
                null,
                null,
                null,
                "Ollama is installed but stopped. Start Ollama yourself, then check readiness again.");
        }

        try
        {
            using HttpResponseMessage versionResponse = await SendAsync(
                HttpMethod.Get,
                "version",
                content: null,
                TimeSpan.FromSeconds(5),
                cancellationToken);
            string versionBody = await versionResponse.Content.ReadAsStringAsync(cancellationToken);

            if (!versionResponse.IsSuccessStatusCode)
            {
                return Incompatible(
                    $"Ollama version check returned HTTP {(int)versionResponse.StatusCode}.");
            }

            OllamaVersionResponse? version;
            try
            {
                version = JsonSerializer.Deserialize<OllamaVersionResponse>(versionBody, JsonOptions);
            }
            catch (JsonException)
            {
                return Incompatible("Ollama returned an unreadable version response.");
            }

            if (string.IsNullOrWhiteSpace(version?.Version))
            {
                return Incompatible("Ollama did not provide the required version capability.");
            }

            using HttpResponseMessage tagsResponse = await SendAsync(
                HttpMethod.Get,
                "tags",
                content: null,
                TimeSpan.FromSeconds(5),
                cancellationToken);
            string tagsBody = await tagsResponse.Content.ReadAsStringAsync(cancellationToken);

            if (!tagsResponse.IsSuccessStatusCode)
            {
                return Incompatible(
                    $"Ollama model-list check returned HTTP {(int)tagsResponse.StatusCode}.");
            }

            OllamaTagsResponse? tags;
            try
            {
                tags = JsonSerializer.Deserialize<OllamaTagsResponse>(tagsBody, JsonOptions);
            }
            catch (JsonException)
            {
                return Incompatible("Ollama returned an unreadable model-list response.");
            }

            if (tags?.Models is null)
            {
                return Incompatible("Ollama did not provide the required model-list capability.");
            }

            OllamaTagModel? selected = tags.Models.FirstOrDefault(model =>
                string.Equals(
                    model.Name,
                    _configuration.Model,
                    StringComparison.OrdinalIgnoreCase)
                || string.Equals(
                    model.Model,
                    _configuration.Model,
                    StringComparison.OrdinalIgnoreCase));

            if (selected is null)
            {
                return new OllamaReadinessResult(
                    OllamaReadinessState.RunningCompatible,
                    QwenReadinessState.Missing,
                    version.Version,
                    null,
                    null,
                    $"Ollama {version.Version} is running and compatible; {_configuration.Model} is missing.");
            }

            return new OllamaReadinessResult(
                OllamaReadinessState.RunningCompatible,
                QwenReadinessState.Present,
                version.Version,
                selected.Digest,
                selected.Size,
                $"Ollama {version.Version} is running and {_configuration.Model} is present.");
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            throw;
        }
        catch (OperationCanceledException)
        {
            return Unreachable("Ollama readiness check timed out.");
        }
        catch (HttpRequestException exception)
        {
            return Unreachable($"Ollama is running but its loopback API is unreachable: {exception.Message}");
        }
        catch (Exception exception)
        {
            return Unreachable($"Ollama readiness check failed safely: {exception.Message}");
        }
    }

    public async Task<OllamaPullResult> PullModelAsync(
        bool explicitConsent,
        long? availableBytes,
        IProgress<OllamaPullProgress>? progress,
        CancellationToken cancellationToken)
    {
        if (!explicitConsent)
        {
            return new OllamaPullResult(
                OllamaPullStatus.ConsentRequired,
                "No model request was sent because explicit consent was not provided.");
        }

        if (availableBytes is null
            || availableBytes.Value < OllamaSetupPolicy.MinimumFreeBytesBeforePull)
        {
            return new OllamaPullResult(
                OllamaPullStatus.InsufficientSpace,
                "No model request was sent because at least 8 GiB of free space could not be confirmed.");
        }

        var body = new OllamaPullRequest(_configuration.Model, Stream: true);
        string json = JsonSerializer.Serialize(body, JsonOptions);
        using var content = new StringContent(json, Encoding.UTF8, "application/json");
        using var pullTimeoutCancellation = new CancellationTokenSource(TimeSpan.FromHours(2));
        using var pullCancellation = CancellationTokenSource.CreateLinkedTokenSource(
            cancellationToken,
            pullTimeoutCancellation.Token);

        try
        {
            using HttpResponseMessage response = await SendAsync(
                HttpMethod.Post,
                "pull",
                content,
                TimeSpan.FromHours(2),
                pullCancellation.Token,
                HttpCompletionOption.ResponseHeadersRead);

            if (!response.IsSuccessStatusCode)
            {
                string error = await response.Content.ReadAsStringAsync(pullCancellation.Token);
                return new OllamaPullResult(
                    OllamaPullStatus.Failed,
                    $"Ollama model request returned HTTP {(int)response.StatusCode}: {Limit(error)}");
            }

            await using Stream stream = await response.Content.ReadAsStreamAsync(pullCancellation.Token);
            using var reader = new StreamReader(stream, Encoding.UTF8);
            bool completed = false;

            while (true)
            {
                string? line = await reader.ReadLineAsync(pullCancellation.Token);
                if (line is null)
                {
                    break;
                }

                if (string.IsNullOrWhiteSpace(line))
                {
                    continue;
                }

                OllamaPullResponse? update;
                try
                {
                    update = JsonSerializer.Deserialize<OllamaPullResponse>(line, JsonOptions);
                }
                catch (JsonException)
                {
                    return new OllamaPullResult(
                        OllamaPullStatus.Failed,
                        "Ollama returned malformed model-download progress.");
                }

                if (!string.IsNullOrWhiteSpace(update?.Error))
                {
                    return new OllamaPullResult(
                        OllamaPullStatus.Failed,
                        $"Ollama model download failed: {Limit(update.Error)}");
                }

                string status = string.IsNullOrWhiteSpace(update?.Status)
                    ? "Working"
                    : update.Status;
                double? percent = update?.Total > 0 && update.Completed is not null
                    ? update.Completed.Value * 100d / update.Total.Value
                    : null;
                progress?.Report(
                    new OllamaPullProgress(status, update?.Completed, update?.Total, percent));

                if (string.Equals(status, "success", StringComparison.OrdinalIgnoreCase))
                {
                    completed = true;
                }
            }

            return completed
                ? new OllamaPullResult(
                    OllamaPullStatus.Completed,
                    $"{_configuration.Model} was obtained through the installed local Ollama application.")
                : new OllamaPullResult(
                    OllamaPullStatus.Failed,
                    "The Ollama model-download stream ended before success was confirmed.");
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            return new OllamaPullResult(
                OllamaPullStatus.Cancelled,
                "The model download was cancelled. Check Ollama before trying again.");
        }
        catch (OperationCanceledException)
        {
            return new OllamaPullResult(
                OllamaPullStatus.TimedOut,
                "The model download exceeded the bounded two-hour setup window.");
        }
        catch (Exception exception)
        {
            return new OllamaPullResult(
                OllamaPullStatus.Failed,
                $"The model download failed safely: {exception.Message}");
        }
    }

    public void Dispose()
    {
        if (_ownsHttpClient)
        {
            _httpClient.Dispose();
        }
    }

    private async Task<HttpResponseMessage> SendAsync(
        HttpMethod method,
        string relativePath,
        HttpContent? content,
        TimeSpan timeout,
        CancellationToken cancellationToken,
        HttpCompletionOption completion = HttpCompletionOption.ResponseContentRead)
    {
        using var request = new HttpRequestMessage(
            method,
            new Uri(_configuration.Endpoint, relativePath))
        {
            Content = content,
        };
        using var timeoutCancellation = new CancellationTokenSource(timeout);
        using var linked = CancellationTokenSource.CreateLinkedTokenSource(
            cancellationToken,
            timeoutCancellation.Token);
        return await _httpClient.SendAsync(request, completion, linked.Token);
    }

    private static OllamaReadinessResult Incompatible(string message)
    {
        return new OllamaReadinessResult(
            OllamaReadinessState.Incompatible,
            QwenReadinessState.Unavailable,
            null,
            null,
            null,
            message);
    }

    private static OllamaReadinessResult Unreachable(string message)
    {
        return new OllamaReadinessResult(
            OllamaReadinessState.Unreachable,
            QwenReadinessState.Unavailable,
            null,
            null,
            null,
            message);
    }

    private static string Limit(string value)
    {
        string normalized = CommentValidator.NormalizeWhitespace(value);
        return normalized.Length <= 240 ? normalized : normalized[..240] + "...";
    }

    private sealed record OllamaVersionResponse(
        [property: JsonPropertyName("version")] string? Version);

    private sealed record OllamaTagsResponse(
        [property: JsonPropertyName("models")] IReadOnlyList<OllamaTagModel>? Models);

    private sealed record OllamaTagModel(
        [property: JsonPropertyName("name")] string? Name,
        [property: JsonPropertyName("model")] string? Model,
        [property: JsonPropertyName("digest")] string? Digest,
        [property: JsonPropertyName("size")] long? Size);

    private sealed record OllamaPullRequest(
        [property: JsonPropertyName("model")] string Model,
        [property: JsonPropertyName("stream")] bool Stream);

    private sealed record OllamaPullResponse(
        [property: JsonPropertyName("status")] string? Status,
        [property: JsonPropertyName("digest")] string? Digest,
        [property: JsonPropertyName("total")] long? Total,
        [property: JsonPropertyName("completed")] long? Completed,
        [property: JsonPropertyName("error")] string? Error);
}
