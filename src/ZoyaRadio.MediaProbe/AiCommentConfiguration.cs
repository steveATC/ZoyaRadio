namespace ZoyaRadio.MediaProbe;

internal sealed record AiCommentConfiguration(
    Uri Endpoint,
    string Model,
    TimeSpan GenerationTimeout,
    string NormalKeepAlive,
    string MeasurementKeepAlive,
    TimeSpan LocalOperationTimeout)
{
    public static AiCommentConfiguration Default { get; } = new(
        new Uri("http://localhost:11434/api/", UriKind.Absolute),
        "qwen3:4b-instruct",
        TimeSpan.FromSeconds(20),
        "0",
        "30s",
        TimeSpan.FromSeconds(5));

    public void Validate()
    {
        if (!OllamaEndpointPolicy.IsAllowed(Endpoint))
        {
            throw new InvalidOperationException(
                "The Ollama endpoint must be an HTTP loopback URL ending in /api/. " +
                "Remote Ollama endpoints are forbidden.");
        }

        if (string.IsNullOrWhiteSpace(Model))
        {
            throw new InvalidOperationException("The local Ollama model name is required.");
        }
    }
}

internal static class OllamaEndpointPolicy
{
    private static readonly string[] AllowedHosts = ["localhost", "127.0.0.1", "::1"];

    public static bool IsAllowed(Uri endpoint)
    {
        if (!endpoint.IsAbsoluteUri
            || !string.Equals(endpoint.Scheme, Uri.UriSchemeHttp, StringComparison.OrdinalIgnoreCase)
            || !string.IsNullOrEmpty(endpoint.UserInfo)
            || !string.IsNullOrEmpty(endpoint.Query)
            || !string.IsNullOrEmpty(endpoint.Fragment))
        {
            return false;
        }

        string host = endpoint.Host.Trim('[', ']');
        bool allowedHost = AllowedHosts.Any(
            allowed => string.Equals(host, allowed, StringComparison.OrdinalIgnoreCase));
        string path = endpoint.AbsolutePath.TrimEnd('/');
        return allowedHost && string.Equals(path, "/api", StringComparison.Ordinal);
    }
}
