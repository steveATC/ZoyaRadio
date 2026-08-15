using System.Net;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Text.RegularExpressions;

namespace ZoyaRadio.MediaProbe;

internal enum MusicContextMatchStatus
{
    Confirmed,
    Ambiguous,
    Unavailable,
    Failed,
}

internal enum MusicIdentityRequirement
{
    ArtistOnly,
    ArtistAndTrack,
}

internal sealed record MusicContextRequest(
    string Title,
    string Artist,
    MusicIdentityRequirement IdentityRequirement);

internal sealed record SourceBackedFact(string Text, string SourceName, Uri SourceUrl, bool IsSpecific = true)
{
    public bool IsSafe()
    {
        return !string.IsNullOrWhiteSpace(Text)
            && Text.Length <= 320
            && !Text.Contains('\r')
            && !Text.Contains('\n')
            && !Text.Contains('<')
            && !Text.Contains('>')
            && !string.IsNullOrWhiteSpace(SourceName)
            && SourceUrl.IsAbsoluteUri
            && string.Equals(SourceUrl.Scheme, Uri.UriSchemeHttps, StringComparison.OrdinalIgnoreCase)
            && MusicContextEndpointPolicy.IsApprovedSourceUrl(SourceUrl);
    }
}

internal enum SourceBackedCommentaryUsefulness
{
    RichHook,
    GenericFact,
    UnsafeOrUnavailable,
}
internal sealed record MusicContextFactCard(
    string MatchedArtist,
    string? MatchedTrack,
    string MusicBrainzArtistId,
    string WikidataId,
    IReadOnlyList<SourceBackedFact> Facts)
{
    public bool HasSpecificFacts => Facts.Any(fact => fact.IsSpecific && fact.IsSafe());
    public SourceBackedCommentaryUsefulness CommentaryUsefulness => Facts.Count == 0 || Facts.Any(fact => !fact.IsSafe())
        ? SourceBackedCommentaryUsefulness.UnsafeOrUnavailable
        : HasSpecificFacts ? SourceBackedCommentaryUsefulness.RichHook : SourceBackedCommentaryUsefulness.GenericFact;
}

internal sealed record MusicContextEnrichmentResult(
    MusicContextRequest Requested,
    MusicContextMatchStatus Status,
    MusicContextFactCard? FactCard,
    DateTimeOffset RetrievedAtUtc,
    string SafeExplanation,
    bool FromCache)
{
    public bool IsConfirmedWithFacts => Status == MusicContextMatchStatus.Confirmed
        && FactCard is not null
        && FactCard.Facts.Count > 0
        && FactCard.Facts.All(fact => fact.IsSafe());
}

internal interface IMusicContextEnricher
{
    Task<MusicContextEnrichmentResult> EnrichAsync(
        MusicContextRequest request,
        CancellationToken cancellationToken);
}

internal sealed record MusicContextEnrichmentConfiguration(
    Uri MusicBrainzEndpoint,
    Uri WikidataEndpoint,
    string UserAgent,
    TimeSpan RequestTimeout,
    TimeSpan MusicBrainzMinimumInterval,
    TimeSpan ConfirmedTtl,
    TimeSpan NegativeTtl,
    int CacheEntryLimit)
{
    public static MusicContextEnrichmentConfiguration Default { get; } = new(
        new Uri("https://musicbrainz.org/ws/2/", UriKind.Absolute),
        new Uri("https://www.wikidata.org/wiki/Special:EntityData/", UriKind.Absolute),
        "ZoyaRadio/0.8 (private-local-enrichment-diagnostic)",
        TimeSpan.FromSeconds(6),
        TimeSpan.FromSeconds(1),
        TimeSpan.FromHours(24),
        TimeSpan.FromHours(2),
        100);

    public void Validate()
    {
        if (!MusicContextEndpointPolicy.IsMusicBrainzEndpoint(MusicBrainzEndpoint)
            || !MusicContextEndpointPolicy.IsWikidataEndpoint(WikidataEndpoint))
        {
            throw new InvalidOperationException("M9C enrichment endpoints must be the approved HTTPS MusicBrainz and Wikidata endpoints.");
        }

        if (string.IsNullOrWhiteSpace(UserAgent)
            || RequestTimeout <= TimeSpan.Zero
            || MusicBrainzMinimumInterval < TimeSpan.FromSeconds(1)
            || ConfirmedTtl <= TimeSpan.Zero
            || NegativeTtl <= TimeSpan.Zero
            || CacheEntryLimit is < 1 or > 100)
        {
            throw new InvalidOperationException("M9C enrichment configuration has invalid bounded values.");
        }
    }
}

internal static class MusicContextEndpointPolicy
{
    public static bool IsMusicBrainzEndpoint(Uri uri) => IsExactBase(uri, "musicbrainz.org", "/ws/2");

    public static bool IsWikidataEndpoint(Uri uri) => IsExactBase(uri, "www.wikidata.org", "/wiki/Special:EntityData");

    public static bool IsApprovedSourceUrl(Uri uri)
    {
        return uri.IsAbsoluteUri
            && string.Equals(uri.Scheme, Uri.UriSchemeHttps, StringComparison.OrdinalIgnoreCase)
            && (string.Equals(uri.Host, "musicbrainz.org", StringComparison.OrdinalIgnoreCase)
                || string.Equals(uri.Host, "www.wikidata.org", StringComparison.OrdinalIgnoreCase));
    }

    private static bool IsExactBase(Uri uri, string host, string path)
    {
        return uri.IsAbsoluteUri
            && string.Equals(uri.Scheme, Uri.UriSchemeHttps, StringComparison.OrdinalIgnoreCase)
            && string.Equals(uri.Host, host, StringComparison.OrdinalIgnoreCase)
            && string.IsNullOrEmpty(uri.UserInfo)
            && string.IsNullOrEmpty(uri.Query)
            && string.IsNullOrEmpty(uri.Fragment)
            && string.Equals(uri.AbsolutePath.TrimEnd('/'), path, StringComparison.Ordinal);
    }
}

internal sealed class MusicContextCache
{
    private static readonly JsonSerializerOptions JsonOptions = new() { WriteIndented = true };
    private readonly string _directory;
    private readonly MusicContextEnrichmentConfiguration _configuration;

    public MusicContextCache(string directory, MusicContextEnrichmentConfiguration configuration)
    {
        _directory = directory;
        _configuration = configuration;
        _configuration.Validate();
    }

    public bool TryRead(MusicContextRequest request, out MusicContextEnrichmentResult result)
    {
        result = default!;
        string path = GetPath(request);
        if (!File.Exists(path))
        {
            return false;
        }

        try
        {
            CacheEntry? entry = JsonSerializer.Deserialize<CacheEntry>(File.ReadAllText(path), JsonOptions);
            if (entry is null || entry.ExpiresAtUtc <= DateTimeOffset.UtcNow || !SameRequest(entry.Result.Requested, request))
            {
                return false;
            }

            result = entry.Result with { FromCache = true };
            return true;
        }
        catch (Exception)
        {
            return false;
        }
    }

    public void Store(MusicContextEnrichmentResult result)
    {
        Directory.CreateDirectory(_directory);
        DateTimeOffset expiry = result.Status == MusicContextMatchStatus.Confirmed
            ? DateTimeOffset.UtcNow.Add(_configuration.ConfirmedTtl)
            : DateTimeOffset.UtcNow.Add(_configuration.NegativeTtl);
        string path = GetPath(result.Requested);
        string temporaryPath = path + "." + Guid.NewGuid().ToString("N") + ".tmp";
        try
        {
            File.WriteAllText(
                temporaryPath,
                JsonSerializer.Serialize(new CacheEntry(expiry, result with { FromCache = false }), JsonOptions),
                new UTF8Encoding(false));
            File.Move(temporaryPath, path, overwrite: true);
        }
        finally
        {
            if (File.Exists(temporaryPath))
            {
                File.Delete(temporaryPath);
            }
        }

        Prune();
    }

    public void Clear()
    {
        if (!Directory.Exists(_directory))
        {
            return;
        }

        foreach (string path in Directory.EnumerateFiles(_directory, "*.json", SearchOption.TopDirectoryOnly))
        {
            File.Delete(path);
        }
    }

    private void Prune()
    {
        FileInfo[] files = Directory.EnumerateFiles(_directory, "*.json", SearchOption.TopDirectoryOnly)
            .Select(path => new FileInfo(path))
            .OrderByDescending(file => file.LastWriteTimeUtc)
            .ToArray();
        foreach (FileInfo excess in files.Skip(_configuration.CacheEntryLimit))
        {
            excess.Delete();
        }
    }

    private string GetPath(MusicContextRequest request)
    {
        string normalized = Normalize(request.Title) + "\u001F" + Normalize(request.Artist) + "\u001F" + request.IdentityRequirement;
        string key = Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(normalized)));
        return Path.Combine(_directory, key + ".json");
    }

    private static bool SameRequest(MusicContextRequest left, MusicContextRequest right)
    {
        return left.IdentityRequirement == right.IdentityRequirement
            && string.Equals(Normalize(left.Title), Normalize(right.Title), StringComparison.Ordinal)
            && string.Equals(Normalize(left.Artist), Normalize(right.Artist), StringComparison.Ordinal);
    }

    internal static string Normalize(string value) => Regex.Replace(value.Trim(), "\\s+", " ").ToUpperInvariant();

    private sealed record CacheEntry(DateTimeOffset ExpiresAtUtc, MusicContextEnrichmentResult Result);
}

internal sealed class MusicBrainzWikidataEnricher : IMusicContextEnricher, IDisposable
{
    private static readonly JsonSerializerOptions JsonOptions = new() { PropertyNameCaseInsensitive = true };
    private static readonly Regex WikidataIdPattern = new("^Q[1-9][0-9]*$", RegexOptions.CultureInvariant);
    private readonly HttpClient _httpClient;
    private readonly bool _ownsHttpClient;
    private readonly MusicContextEnrichmentConfiguration _configuration;
    private readonly MusicContextCache _cache;
    private DateTimeOffset _lastMusicBrainzRequestUtc = DateTimeOffset.MinValue;

    public MusicBrainzWikidataEnricher(
        MusicContextEnrichmentConfiguration configuration,
        MusicContextCache cache,
        HttpClient? httpClient = null)
    {
        _configuration = configuration;
        _configuration.Validate();
        _cache = cache;
        _httpClient = httpClient ?? new HttpClient(new HttpClientHandler { AllowAutoRedirect = false }) { Timeout = Timeout.InfiniteTimeSpan };
        _ownsHttpClient = httpClient is null;
    }

    public async Task<MusicContextEnrichmentResult> EnrichAsync(MusicContextRequest request, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(request.Title) || string.IsNullOrWhiteSpace(request.Artist))
        {
            return Result(request, MusicContextMatchStatus.Unavailable, null, "Title and artist are required for enrichment.");
        }

        if (_cache.TryRead(request, out MusicContextEnrichmentResult cached))
        {
            return cached;
        }

        MusicContextEnrichmentResult result;
        try
        {
            result = await ResolveAsync(request, cancellationToken);
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            throw;
        }
        catch (OperationCanceledException)
        {
            result = Result(request, MusicContextMatchStatus.Failed, null, "An approved enrichment source timed out; no enrichment was used.");
        }
        catch (HttpRequestException)
        {
            result = Result(request, MusicContextMatchStatus.Failed, null, "An approved enrichment source was unavailable; no enrichment was used.");
        }
        catch (Exception)
        {
            result = Result(request, MusicContextMatchStatus.Failed, null, "The enrichment response was unusable; no enrichment was used.");
        }

        _cache.Store(result);
        return result;
    }

    public void Dispose()
    {
        if (_ownsHttpClient)
        {
            _httpClient.Dispose();
        }
    }

    private async Task<MusicContextEnrichmentResult> ResolveAsync(MusicContextRequest request, CancellationToken cancellationToken)
    {
        MusicBrainzArtistSearch? artists = await GetMusicBrainzAsync<MusicBrainzArtistSearch>(
            "artist?fmt=json&limit=5&query=" + Uri.EscapeDataString("artist:\"" + request.Artist + "\""), cancellationToken);
        MusicBrainzArtist[] exactArtists = (artists?.Artists ?? []).Where(artist => Same(artist.Name, request.Artist)).ToArray();
        if (exactArtists.Length == 0) return Result(request, MusicContextMatchStatus.Unavailable, null, "No exact MusicBrainz artist identity was found; no enrichment was used.");

        MusicBrainzArtist? artist = exactArtists.Length == 1 ? exactArtists[0] : await ResolveDuplicateArtistAsync(exactArtists, request, cancellationToken);
        if (artist is null) return Result(request, MusicContextMatchStatus.Ambiguous, null, "More than one exact MusicBrainz artist identity remained after deterministic published-evidence checks; no enrichment was used.");

        string? matchedTrack = null;
        if (request.IdentityRequirement == MusicIdentityRequirement.ArtistAndTrack)
        {
            MusicBrainzRecordingSearch? recordings = await GetMusicBrainzAsync<MusicBrainzRecordingSearch>(
                "recording?fmt=json&limit=5&query=" + Uri.EscapeDataString("recording:\"" + request.Title + "\" AND artist:\"" + request.Artist + "\""), cancellationToken);
            MusicBrainzRecording[] exactRecordings = (recordings?.Recordings ?? [])
                .Where(recording => Same(recording.Title, request.Title) && (recording.ArtistCredit ?? []).Any(credit => Same(credit.Name, request.Artist) && string.Equals(credit.Id, artist.Id, StringComparison.Ordinal)))
                .ToArray();
            if (exactRecordings.Length == 0) return Result(request, MusicContextMatchStatus.Unavailable, null, "No exact MusicBrainz title-and-artist recording identity was found; no enrichment was used.");
            if (exactRecordings.Length != 1) return Result(request, MusicContextMatchStatus.Ambiguous, null, "More than one exact MusicBrainz recording identity matched; no enrichment was used.");
            matchedTrack = exactRecordings[0].Title;
        }

        MusicBrainzArtistDetails? details = await GetMusicBrainzAsync<MusicBrainzArtistDetails>("artist/" + Uri.EscapeDataString(artist.Id) + "?fmt=json&inc=url-rels", cancellationToken);
        string? wikidataId = LinkedWikidataId(details);
        if (wikidataId is null) return Result(request, MusicContextMatchStatus.Unavailable, null, "The confirmed MusicBrainz identity had no approved linked Wikidata identity; no enrichment was used.");
        WikidataEntityEnvelope? wikidata = await GetWikidataAsync(wikidataId, cancellationToken);
        if (wikidata?.Entities is null || !wikidata.Entities.TryGetValue(wikidataId, out WikidataEntity? entity)) return Result(request, MusicContextMatchStatus.Failed, null, "The linked Wikidata identity could not be read; no enrichment was used.");
        string? description = entity.Descriptions?.TryGetValue("en", out WikidataText? english) == true ? english.Value : null;
        if (string.IsNullOrWhiteSpace(description) || description.Length > 240 || description.Contains('\r') || description.Contains('\n')) return Result(request, MusicContextMatchStatus.Unavailable, null, "The confirmed linked identity had no concise supported structured fact; no enrichment was used.");
        Uri wikidataUrl = new("https://www.wikidata.org/wiki/" + wikidataId, UriKind.Absolute);
        var facts = new List<SourceBackedFact> { new($"Wikidata describes {artist.Name} as {description}.", "Wikidata", wikidataUrl, IsSpecificDescription(description)) };
        if (TryGetInceptionYear(entity, out int year))
        {
            string? origin = await TryGetOriginLabelAsync(entity, cancellationToken);
            facts.Add(new SourceBackedFact(origin is null ? $"Wikidata records that {artist.Name} began in {year}." : $"Wikidata records that {artist.Name} began in {origin} in {year}.", "Wikidata", wikidataUrl, true));
        }
        if (facts.Any(fact => !fact.IsSafe())) return Result(request, MusicContextMatchStatus.Failed, null, "The linked fact did not meet M9C source-card safety rules; no enrichment was used.");
        var card = new MusicContextFactCard(artist.Name, matchedTrack, artist.Id, wikidataId, facts);
        return Result(request, MusicContextMatchStatus.Confirmed, card, card.HasSpecificFacts ? "Exact MusicBrainz identity and linked specific Wikidata fact confirmed." : "Exact MusicBrainz identity has only a generic linked fact; candidate safely falls back.");
    }

    private async Task<MusicBrainzArtist?> ResolveDuplicateArtistAsync(IReadOnlyList<MusicBrainzArtist> candidates, MusicContextRequest request, CancellationToken cancellationToken)
    {
        var linked = new List<MusicBrainzArtist>();
        foreach (MusicBrainzArtist candidate in candidates)
        {
            MusicBrainzArtistDetails? details = await GetMusicBrainzAsync<MusicBrainzArtistDetails>("artist/" + Uri.EscapeDataString(candidate.Id) + "?fmt=json&inc=url-rels", cancellationToken);
            if (LinkedWikidataId(details) is not null) linked.Add(candidate);
        }
        return linked.Count == 1 ? linked[0] : null;
    }

    private static string? LinkedWikidataId(MusicBrainzArtistDetails? details)
        => (details?.Relations ?? []).Where(relation => string.Equals(relation.Type, "wikidata", StringComparison.OrdinalIgnoreCase)).Select(relation => TryGetWikidataId(relation.Url?.Resource)).Where(id => id is not null).Cast<string>().Distinct(StringComparer.Ordinal).SingleOrDefault();

    private async Task<string?> TryGetOriginLabelAsync(WikidataEntity entity, CancellationToken cancellationToken)
    {
        if (!TryGetClaimEntityId(entity, "P740", out string? originId)) return null;
        WikidataEntityEnvelope? origin = await GetWikidataAsync(originId!, cancellationToken);
        return origin?.Entities?.TryGetValue(originId!, out WikidataEntity? place) == true && place.Labels?.TryGetValue("en", out WikidataText? label) == true && !string.IsNullOrWhiteSpace(label.Value) ? label.Value : null;
    }

    private static bool TryGetClaimEntityId(WikidataEntity entity, string property, out string? id)
    {
        id = null;
        if (entity.Claims.ValueKind != JsonValueKind.Object || !entity.Claims.TryGetProperty(property, out JsonElement statements) || statements.ValueKind != JsonValueKind.Array) return false;
        foreach (JsonElement statement in statements.EnumerateArray())
        {
            if (!statement.TryGetProperty("mainsnak", out JsonElement snak) || !snak.TryGetProperty("datavalue", out JsonElement dataValue) || !dataValue.TryGetProperty("value", out JsonElement value) || value.ValueKind != JsonValueKind.Object || !value.TryGetProperty("id", out JsonElement entityId)) continue;
            string? candidate = entityId.GetString();
            if (WikidataIdPattern.IsMatch(candidate ?? string.Empty)) { id = candidate; return true; }
        }
        return false;
    }
    private static bool TryGetInceptionYear(WikidataEntity entity, out int year)
    {
        year = 0;
        if (entity.Claims.ValueKind != JsonValueKind.Object || !entity.Claims.TryGetProperty("P571", out JsonElement statements) || statements.ValueKind != JsonValueKind.Array) return false;
        foreach (JsonElement statement in statements.EnumerateArray())
        {
            if (!statement.TryGetProperty("mainsnak", out JsonElement snak) || !snak.TryGetProperty("datavalue", out JsonElement dataValue) || !dataValue.TryGetProperty("value", out JsonElement value) || value.ValueKind != JsonValueKind.Object || !value.TryGetProperty("time", out JsonElement time)) continue;
            Match match = Regex.Match(time.GetString() ?? string.Empty, "^\\+?(?<year>[0-9]{4})", RegexOptions.CultureInvariant);
            if (match.Success && int.TryParse(match.Groups["year"].Value, out year)) return true;
        }
        return false;
    }
    private static bool IsSpecificDescription(string description)
    {
        string normalized = MusicContextCache.Normalize(description);
        return Regex.Matches(description, "[\\p{L}\\p{N}]+", RegexOptions.CultureInvariant).Count >= 4
            && !Regex.IsMatch(normalized, "^(AMERICAN|BRITISH|ENGLISH|CANADIAN|AUSTRALIAN|GERMAN|FRENCH)? ?(MUSICIAN|SINGER|BAND|ROCK BAND)( \\([0-9]{4}[^)]*\\))?$", RegexOptions.CultureInvariant);
    }
    private async Task<T?> GetMusicBrainzAsync<T>(string relativePath, CancellationToken cancellationToken)
    {
        TimeSpan wait = _configuration.MusicBrainzMinimumInterval - (DateTimeOffset.UtcNow - _lastMusicBrainzRequestUtc);
        if (wait > TimeSpan.Zero)
        {
            await Task.Delay(wait, cancellationToken);
        }

        _lastMusicBrainzRequestUtc = DateTimeOffset.UtcNow;
        return await GetJsonAsync<T>(new Uri(_configuration.MusicBrainzEndpoint, relativePath), cancellationToken);
    }

    private Task<WikidataEntityEnvelope?> GetWikidataAsync(string wikidataId, CancellationToken cancellationToken)
    {
        return GetJsonAsync<WikidataEntityEnvelope>(
            new Uri(_configuration.WikidataEndpoint, wikidataId + ".json"),
            cancellationToken);
    }

    private async Task<T?> GetJsonAsync<T>(Uri uri, CancellationToken cancellationToken)
    {
        using var request = new HttpRequestMessage(HttpMethod.Get, uri);
        request.Headers.TryAddWithoutValidation("User-Agent", _configuration.UserAgent);
        request.Headers.TryAddWithoutValidation("Accept", "application/json");
        using var timeout = new CancellationTokenSource(_configuration.RequestTimeout);
        using var linked = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken, timeout.Token);
        using HttpResponseMessage response = await _httpClient.SendAsync(request, HttpCompletionOption.ResponseContentRead, linked.Token);
        if (response.StatusCode == HttpStatusCode.TooManyRequests)
        {
            throw new HttpRequestException("Approved enrichment source rate limited the request.");
        }

        response.EnsureSuccessStatusCode();
        string body = await response.Content.ReadAsStringAsync(linked.Token);
        return JsonSerializer.Deserialize<T>(body, JsonOptions);
    }

    private static string? TryGetWikidataId(string? resource)
    {
        if (!Uri.TryCreate(resource, UriKind.Absolute, out Uri? uri)
            || !string.Equals(uri.Host, "www.wikidata.org", StringComparison.OrdinalIgnoreCase))
        {
            return null;
        }

        string id = uri.AbsolutePath.Trim('/').Split('/').LastOrDefault() ?? string.Empty;
        return WikidataIdPattern.IsMatch(id) ? id : null;
    }

    private static MusicContextEnrichmentResult Result(
        MusicContextRequest request,
        MusicContextMatchStatus status,
        MusicContextFactCard? card,
        string explanation)
    {
        return new MusicContextEnrichmentResult(request, status, card, DateTimeOffset.UtcNow, explanation, false);
    }

    private static bool Same(string? left, string right)
    {
        return string.Equals(MusicContextCache.Normalize(left ?? string.Empty), MusicContextCache.Normalize(right), StringComparison.Ordinal);
    }

    private sealed record MusicBrainzArtistSearch([property: JsonPropertyName("artists")] IReadOnlyList<MusicBrainzArtist>? Artists);
    private sealed record MusicBrainzArtist([property: JsonPropertyName("id")] string Id, [property: JsonPropertyName("name")] string Name);
    private sealed record MusicBrainzRecordingSearch([property: JsonPropertyName("recordings")] IReadOnlyList<MusicBrainzRecording>? Recordings);
    private sealed record MusicBrainzRecording([property: JsonPropertyName("title")] string Title, [property: JsonPropertyName("artist-credit")] IReadOnlyList<MusicBrainzArtistCredit>? ArtistCredit);
    private sealed record MusicBrainzArtistCredit([property: JsonPropertyName("id")] string? Id, [property: JsonPropertyName("name")] string? Name);
    private sealed record MusicBrainzArtistDetails([property: JsonPropertyName("relations")] IReadOnlyList<MusicBrainzRelation>? Relations);
    private sealed record MusicBrainzRelation([property: JsonPropertyName("type")] string? Type, [property: JsonPropertyName("url")] MusicBrainzUrl? Url);
    private sealed record MusicBrainzUrl([property: JsonPropertyName("resource")] string? Resource);
    private sealed record WikidataEntityEnvelope([property: JsonPropertyName("entities")] IReadOnlyDictionary<string, WikidataEntity>? Entities);
    private sealed record WikidataEntity([property: JsonPropertyName("labels")] IReadOnlyDictionary<string, WikidataText>? Labels, [property: JsonPropertyName("descriptions")] IReadOnlyDictionary<string, WikidataText>? Descriptions, [property: JsonPropertyName("claims")] JsonElement Claims);
    private sealed record WikidataText([property: JsonPropertyName("value")] string? Value);
}
