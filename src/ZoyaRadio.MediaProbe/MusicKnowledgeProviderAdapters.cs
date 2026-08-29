using System.Net;
using System.Net.Http.Headers;
using System.Text;
using System.Text.RegularExpressions;

namespace ZoyaRadio.MediaProbe;

// M13A-I2 is a deliberately isolated diagnostic transport. It is reachable only
// through the explicit MediaProbe command and never from the radio runtime.
internal enum M13AProviderRoute
{
    MusicBrainzArtist,
    MusicBrainzRecording,
    WikidataEntity,
}

internal sealed record M13AProviderRequest(M13AProviderRoute Route, Uri Uri, string LogicalId);

internal sealed record M13AProviderResponse(
    int StatusCode,
    string? Body,
    bool TimedOut = false,
    TimeSpan? RetryAfter = null,
    bool BodyTooLarge = false,
    bool NetworkFailed = false,
    int BodyByteCount = 0);

internal interface IM13AProviderTransport
{
    Task<M13AProviderResponse> SendAsync(M13AProviderRequest request, CancellationToken cancellationToken);
}

internal interface IM13AProviderClock
{
    DateTimeOffset UtcNow { get; }

    Task DelayAsync(TimeSpan delay, CancellationToken cancellationToken);
}

internal sealed class M13ASystemClock : IM13AProviderClock
{
    internal static M13ASystemClock Instance { get; } = new();

    public DateTimeOffset UtcNow => DateTimeOffset.UtcNow;

    public Task DelayAsync(TimeSpan delay, CancellationToken cancellationToken) => Task.Delay(delay, cancellationToken);
}

internal static class M13AProviderRequestFactory
{
    internal const int MaximumMetadataCharacters = 200;
    internal const int MaximumUriCharacters = 2_048;

    private static readonly Regex WikidataId = new("^Q[1-9][0-9]{0,11}$", RegexOptions.CultureInvariant);

    internal static bool TryCreateArtist(string artist, string logicalId, out M13AProviderRequest? request)
    {
        request = null;
        return IsBoundedMetadata(artist)
            && TryCreateMusicBrainz(M13AProviderRoute.MusicBrainzArtist, "artist:\"" + artist + "\"", logicalId, out request);
    }

    internal static bool TryCreateRecording(string title, string artist, string logicalId, out M13AProviderRequest? request)
    {
        request = null;
        return IsBoundedMetadata(title)
            && IsBoundedMetadata(artist)
            && TryCreateMusicBrainz(
                M13AProviderRoute.MusicBrainzRecording,
                "recording:\"" + title + "\" AND artist:\"" + artist + "\"",
                logicalId,
                out request);
    }

    internal static bool TryCreateWikidata(string qid, string logicalId, out M13AProviderRequest? request)
    {
        request = null;
        if (!WikidataId.IsMatch(qid)) return false;

        Uri uri = new("https://www.wikidata.org/wiki/Special:EntityData/" + qid + ".json", UriKind.Absolute);
        return TryAccept(M13AProviderRoute.WikidataEntity, uri, logicalId, out request);
    }

    internal static bool IsAllowed(M13AProviderRequest request) =>
        TryAccept(request.Route, request.Uri, request.LogicalId, out _);

    private static bool TryCreateMusicBrainz(
        M13AProviderRoute route,
        string query,
        string logicalId,
        out M13AProviderRequest? request)
    {
        request = null;
        if (!IsBoundedMetadata(query) || !IsBoundedLogicalId(logicalId)) return false;

        string path = route == M13AProviderRoute.MusicBrainzArtist ? "artist" : "recording";
        Uri uri = new(
            "https://musicbrainz.org/ws/2/" + path + "?query=" + Uri.EscapeDataString(query) + "&fmt=json&limit=5&inc=url-rels",
            UriKind.Absolute);
        return TryAccept(route, uri, logicalId, out request);
    }

    private static bool TryAccept(M13AProviderRoute route, Uri uri, string logicalId, out M13AProviderRequest? request)
    {
        request = null;
        if (!IsBoundedLogicalId(logicalId) || uri.OriginalString.Length > MaximumUriCharacters || uri.Scheme != Uri.UriSchemeHttps || !uri.IsDefaultPort) return false;

        bool accepted = route switch
        {
            M13AProviderRoute.MusicBrainzArtist =>
                uri.Host.Equals("musicbrainz.org", StringComparison.OrdinalIgnoreCase)
                && uri.AbsolutePath == "/ws/2/artist"
                && IsExactMusicBrainzQuery(uri.Query),
            M13AProviderRoute.MusicBrainzRecording =>
                uri.Host.Equals("musicbrainz.org", StringComparison.OrdinalIgnoreCase)
                && uri.AbsolutePath == "/ws/2/recording"
                && IsExactMusicBrainzQuery(uri.Query),
            M13AProviderRoute.WikidataEntity =>
                uri.Host.Equals("www.wikidata.org", StringComparison.OrdinalIgnoreCase)
                && uri.Query.Length == 0
                && Regex.IsMatch(uri.AbsolutePath, "^/wiki/Special:EntityData/Q[1-9][0-9]{0,11}\\.json$", RegexOptions.CultureInvariant),
            _ => false,
        };
        if (!accepted) return false;

        request = new M13AProviderRequest(route, uri, logicalId);
        return true;
    }

    private static bool IsExactMusicBrainzQuery(string query)
    {
        string[] parts = query.TrimStart('?').Split('&', StringSplitOptions.None);
        if (parts.Length != 4) return false;
        var keys = new HashSet<string>(StringComparer.Ordinal);
        foreach (string part in parts)
        {
            int separator = part.IndexOf('=');
            if (separator <= 0 || separator != part.LastIndexOf('=') || !keys.Add(part[..separator])) return false;
        }
        return keys.SetEquals(["query", "fmt", "limit", "inc"])
            && parts.Contains("fmt=json", StringComparer.Ordinal)
            && parts.Contains("limit=5", StringComparer.Ordinal)
            && parts.Contains("inc=url-rels", StringComparer.Ordinal)
            && parts.Single(part => part.StartsWith("query=", StringComparison.Ordinal)).Length > "query=".Length;
    }

    private static bool IsBoundedMetadata(string value) =>
        !string.IsNullOrWhiteSpace(value)
        && value.Length <= MaximumMetadataCharacters
        && value.All(character => !char.IsControl(character));

    private static bool IsBoundedLogicalId(string value) =>
        !string.IsNullOrWhiteSpace(value)
        && value.Length <= 96
        && value.All(character => char.IsLetterOrDigit(character) || character is '-' or '_' or '.');
}

internal static class M13AProviderContact
{
    internal static bool IsMeaningful(string? contact)
    {
        if (string.IsNullOrWhiteSpace(contact) || contact.Length > 320 || contact.Any(character => char.IsControl(character) || character is '(' or ')' || character > 0x7f)) return false;
        string trimmed = contact.Trim();
        if (Uri.TryCreate(trimmed, UriKind.Absolute, out Uri? uri)
            && uri.Scheme == Uri.UriSchemeHttps
            && !string.IsNullOrWhiteSpace(uri.Host)) return true;

        int at = trimmed.IndexOf('@');
        return at > 0 && at == trimmed.LastIndexOf('@') && at < trimmed.Length - 3 && trimmed.IndexOf('.', at) > at + 1;
    }
}

internal sealed class M13AHttpProviderTransport : IM13AProviderTransport, IDisposable
{
    internal const int MaximumResponseBodyBytes = 512 * 1024;
    private readonly HttpClient _client;

    internal M13AHttpProviderTransport(string contact)
        : this(contact, CreateDefaultHandler())
    {
    }

    internal M13AHttpProviderTransport(string contact, HttpMessageHandler handler)
    {
        if (!M13AProviderContact.IsMeaningful(contact)) throw new ArgumentException("A meaningful diagnostic contact is required.", nameof(contact));
        _client = new HttpClient(handler, disposeHandler: true)
        {
            Timeout = TimeSpan.FromSeconds(20),
        };
        _client.DefaultRequestHeaders.UserAgent.Add(new ProductInfoHeaderValue("ZoyaRadioM13ADiagnostic", "1.0"));
        _client.DefaultRequestHeaders.UserAgent.Add(new ProductInfoHeaderValue("(contact " + contact.Trim() + ")"));
    }

    internal static HttpClientHandler CreateDefaultHandler() => new()
    {
        AllowAutoRedirect = false,
        UseCookies = false,
    };

    public async Task<M13AProviderResponse> SendAsync(M13AProviderRequest request, CancellationToken cancellationToken)
    {
        if (!M13AProviderRequestFactory.IsAllowed(request)) throw new InvalidOperationException("The diagnostic refused a non-allowlisted provider request.");

        try
        {
            using var message = new HttpRequestMessage(HttpMethod.Get, request.Uri);
            using HttpResponseMessage response = await _client.SendAsync(message, HttpCompletionOption.ResponseHeadersRead, cancellationToken);
            if ((int)response.StatusCode is >= 300 and <= 399)
            {
                return new M13AProviderResponse((int)response.StatusCode, null);
            }

            (string? body, bool tooLarge, int bytes) = await ReadBodyAsync(response, cancellationToken);
            return new M13AProviderResponse((int)response.StatusCode, body, RetryAfter: RetryAfter(response), BodyTooLarge: tooLarge, BodyByteCount: bytes);
        }
        catch (TaskCanceledException) when (!cancellationToken.IsCancellationRequested)
        {
            return new M13AProviderResponse(0, null, TimedOut: true);
        }
        catch (HttpRequestException)
        {
            cancellationToken.ThrowIfCancellationRequested();
            return new M13AProviderResponse(0, null, NetworkFailed: true);
        }
    }

    private static async Task<(string? Body, bool TooLarge, int ByteCount)> ReadBodyAsync(HttpResponseMessage response, CancellationToken cancellationToken)
    {
        if (response.Content.Headers.ContentLength is long length && length > MaximumResponseBodyBytes) return (null, true, (int)Math.Min(length, int.MaxValue));

        await using Stream stream = await response.Content.ReadAsStreamAsync(cancellationToken);
        byte[] bytes = new byte[MaximumResponseBodyBytes + 1];
        int offset = 0;
        while (offset < bytes.Length)
        {
            int read = await stream.ReadAsync(bytes.AsMemory(offset, bytes.Length - offset), cancellationToken);
            if (read == 0) break;
            offset += read;
        }
        if (offset > MaximumResponseBodyBytes) return (null, true, offset);
        return (Encoding.UTF8.GetString(bytes, 0, offset), false, offset);
    }

    private static TimeSpan? RetryAfter(HttpResponseMessage response)
    {
        RetryConditionHeaderValue? retry = response.Headers.RetryAfter;
        if (retry?.Delta is TimeSpan delay) return delay;
        if (retry?.Date is DateTimeOffset at) return at - DateTimeOffset.UtcNow;
        return null;
    }

    public void Dispose() => _client.Dispose();
}

internal sealed record M13AProviderRequestResult(
    M13AProviderRequest Request,
    MusicKnowledgeOutcome Outcome,
    int Attempts,
    string? Body,
    bool Cancelled = false,
    IReadOnlyList<M13AProviderAttempt>? AttemptLedger = null);

internal sealed record M13AProviderAttempt(
    string Provider,
    string Route,
    string SafeUrl,
    string LogicalId,
    int HttpStatus,
    MusicKnowledgeOutcome Outcome,
    DateTimeOffset RetrievedAtUtc,
    int Attempt,
    bool Retried,
    int BodyByteCount,
    string BodySha256,
    string? ClassificationDetail = null);

internal sealed class M13AProviderRequestRunner
{
    internal const int MaximumAttemptsPerLogicalRequest = 2;
    internal const int MaximumTotalAttempts = 32;
    internal static readonly TimeSpan MusicBrainzMinimumSpacing = TimeSpan.FromSeconds(1.5);
    internal static readonly TimeSpan MaximumRetryAfter = TimeSpan.FromSeconds(60);
    internal static readonly TimeSpan MusicBrainz503FallbackCooldown = TimeSpan.FromSeconds(5);
    private const int MaximumPacingDelayChecks = 180;

    private readonly IM13AProviderTransport _transport;
    private readonly IM13AProviderClock _clock;
    private DateTimeOffset? _lastMusicBrainzAttempt;
    private DateTimeOffset? _musicBrainzCooldownUntil;
    private int _totalAttempts;

    internal M13AProviderRequestRunner(IM13AProviderTransport transport, IM13AProviderClock clock)
    {
        _transport = transport;
        _clock = clock;
    }

    internal int TotalAttempts => _totalAttempts;

    internal async Task<M13AProviderRequestResult> ExecuteAsync(M13AProviderRequest request, CancellationToken cancellationToken)
    {
        if (!M13AProviderRequestFactory.IsAllowed(request)) throw new InvalidOperationException("The diagnostic refused a non-allowlisted provider request.");
        var ledger = new List<M13AProviderAttempt>();

        for (int attempt = 1; attempt <= MaximumAttemptsPerLogicalRequest; attempt++)
        {
            try
            {
                if (cancellationToken.IsCancellationRequested) return new(request, MusicKnowledgeOutcome.Unknown, attempt - 1, null, Cancelled: true, ledger);
                if (_totalAttempts >= MaximumTotalAttempts) return new(request, MusicKnowledgeOutcome.ProviderOutage, attempt - 1, null, AttemptLedger: ledger);

                await PaceMusicBrainzAsync(request, cancellationToken);
                if (cancellationToken.IsCancellationRequested) return new(request, MusicKnowledgeOutcome.Unknown, attempt - 1, null, Cancelled: true, ledger);

                _totalAttempts++;
                M13AProviderResponse response = await _transport.SendAsync(request, cancellationToken);
                MusicKnowledgeOutcome outcome = Map(request, response);
                bool retry = ShouldRetry(response, outcome, attempt);
                ledger.Add(CreateAttempt(request, response, outcome, _clock.UtcNow, attempt, retry));
                ApplyMusicBrainz503Cooldown(request, response);
                if (!retry) return new(request, outcome, attempt, response.Body, AttemptLedger: ledger);

                TimeSpan retryDelay = BoundedRetryAfter(response.RetryAfter);
                if (retryDelay > TimeSpan.Zero) await _clock.DelayAsync(retryDelay, cancellationToken);
            }
            catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
            {
                return new(request, MusicKnowledgeOutcome.Unknown, attempt - 1, null, Cancelled: true, ledger);
            }
        }

        return new(request, MusicKnowledgeOutcome.ProviderOutage, MaximumAttemptsPerLogicalRequest, null, AttemptLedger: ledger);
    }

    private async Task PaceMusicBrainzAsync(M13AProviderRequest request, CancellationToken cancellationToken)
    {
        if (request.Route is not (M13AProviderRoute.MusicBrainzArtist or M13AProviderRoute.MusicBrainzRecording)) return;
        DateTimeOffset deadline = _lastMusicBrainzAttempt is DateTimeOffset previous
            ? previous.Add(MusicBrainzMinimumSpacing)
            : _clock.UtcNow;
        if (_musicBrainzCooldownUntil is DateTimeOffset cooldown && cooldown > deadline) deadline = cooldown;

        for (int check = 0; _clock.UtcNow < deadline; check++)
        {
            if (check == MaximumPacingDelayChecks)
            {
                throw new TimeoutException("MusicBrainz pacing did not reach its required request-start deadline.");
            }

            cancellationToken.ThrowIfCancellationRequested();
            await _clock.DelayAsync(deadline - _clock.UtcNow, cancellationToken);
        }
        _lastMusicBrainzAttempt = _clock.UtcNow;
    }

    internal static MusicKnowledgeOutcome Map(M13AProviderRequest request, M13AProviderResponse response) =>
        response.TimedOut ? MusicKnowledgeOutcome.Timeout : response.NetworkFailed ? MusicKnowledgeOutcome.ProviderOutage : response.BodyTooLarge ? MusicKnowledgeOutcome.SchemaFailure : response.StatusCode switch
        {
            200 when string.IsNullOrWhiteSpace(response.Body) => MusicKnowledgeOutcome.SchemaFailure,
            200 => MusicKnowledgeOutcome.ConfirmedFact,
            404 => MusicKnowledgeOutcome.ConfirmedAbsence,
            429 => MusicKnowledgeOutcome.RateLimited,
            503 when request.Route is M13AProviderRoute.MusicBrainzArtist or M13AProviderRoute.MusicBrainzRecording => MusicKnowledgeOutcome.RateLimited,
            >= 500 and <= 599 => MusicKnowledgeOutcome.ProviderOutage,
            >= 300 and <= 399 => MusicKnowledgeOutcome.UnsupportedRepresentation,
            _ => MusicKnowledgeOutcome.UnsupportedRepresentation,
        };

    internal static bool ShouldRetry(M13AProviderResponse response, MusicKnowledgeOutcome outcome, int completedAttempts) =>
        completedAttempts < MaximumAttemptsPerLogicalRequest
        && (outcome is MusicKnowledgeOutcome.Timeout or MusicKnowledgeOutcome.RateLimited
            || (outcome == MusicKnowledgeOutcome.ProviderOutage && IsTemporaryServerStatus(response.StatusCode))
            || response.NetworkFailed);

    internal static bool IsTemporaryServerStatus(int statusCode) => statusCode is 500 or 502 or 503 or 504;

    internal static string? ClassificationDetail(M13AProviderRequest request, M13AProviderResponse response) =>
        response.StatusCode == 503 && request.Route is (M13AProviderRoute.MusicBrainzArtist or M13AProviderRoute.MusicBrainzRecording)
            ? "MusicBrainz HTTP 503 is treated as rate-limited; user-agent, source-IP, global throttling, and capacity cannot be distinguished."
            : null;

    private static M13AProviderAttempt CreateAttempt(M13AProviderRequest request, M13AProviderResponse response, MusicKnowledgeOutcome outcome, DateTimeOffset retrievedAtUtc, int attempt, bool retry) =>
        new(
            request.Route == M13AProviderRoute.WikidataEntity ? "Wikidata" : "MusicBrainz",
            request.Route.ToString(),
            SafeUrl(request),
            request.LogicalId,
            response.StatusCode,
            outcome,
            retrievedAtUtc,
            attempt,
            retry,
            response.BodyByteCount > 0 ? response.BodyByteCount : response.Body is null ? 0 : Encoding.UTF8.GetByteCount(response.Body),
            Convert.ToHexString(System.Security.Cryptography.SHA256.HashData(Encoding.UTF8.GetBytes(response.Body ?? string.Empty))).ToLowerInvariant(),
            ClassificationDetail(request, response));

    private static string SafeUrl(M13AProviderRequest request) => request.Route switch
    {
        M13AProviderRoute.MusicBrainzArtist => "https://musicbrainz.org/ws/2/artist",
        M13AProviderRoute.MusicBrainzRecording => "https://musicbrainz.org/ws/2/recording",
        M13AProviderRoute.WikidataEntity => "https://www.wikidata.org/wiki/Special:EntityData/{QID}.json",
        _ => "refused",
    };

    private void ApplyMusicBrainz503Cooldown(M13AProviderRequest request, M13AProviderResponse response)
    {
        if (response.StatusCode != 503 || request.Route is not (M13AProviderRoute.MusicBrainzArtist or M13AProviderRoute.MusicBrainzRecording)) return;
        TimeSpan cooldown = BoundedRetryAfter(response.RetryAfter);
        if (cooldown <= TimeSpan.Zero) cooldown = MusicBrainz503FallbackCooldown;
        DateTimeOffset until = _clock.UtcNow.Add(cooldown);
        if (_musicBrainzCooldownUntil is null || until > _musicBrainzCooldownUntil) _musicBrainzCooldownUntil = until;
    }

    internal static TimeSpan BoundedRetryAfter(TimeSpan? retryAfter)
    {
        if (retryAfter is not TimeSpan delay || delay <= TimeSpan.Zero) return TimeSpan.Zero;
        return delay > MaximumRetryAfter ? MaximumRetryAfter : delay;
    }
}
