using System.Diagnostics;
using System.Reflection;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using System.Text.RegularExpressions;

namespace ZoyaRadio.MediaProbe;

internal static class M9BConstants
{
    internal const string Model = "qwen3:14b-q4_K_M";
    internal const string CatalogResourceSuffix = "Prompts.Milestone9BContextCatalog.json";
    internal const string PromptResourceSuffix = "Prompts.Milestone9BWholeCommentPrompt.txt";
}

internal sealed record M9BCatalogFact(string Text, string SourceName, Uri SourceUrl, string Applicability)
{
    internal bool IsSafe => !string.IsNullOrWhiteSpace(Text)
        && Text.Length <= 320
        && !Text.Contains('\r')
        && !Text.Contains('\n')
        && !Text.Contains('<')
        && !Text.Contains('>')
        && !string.IsNullOrWhiteSpace(SourceName)
        && SourceUrl.IsAbsoluteUri
        && string.Equals(SourceUrl.Scheme, Uri.UriSchemeHttps, StringComparison.OrdinalIgnoreCase);
}

internal sealed record M9BCatalogCard(
    string Id,
    string Artist,
    string Title,
    bool EditoriallyUseful,
    IReadOnlyList<M9BCatalogFact> Facts,
    IReadOnlyList<string> TitleAliases)
{
    internal IReadOnlyList<string> ExactTitles => new[] { Title }.Concat(TitleAliases).ToArray();

    internal bool Matches(MediaSnapshot media) => RadioHistory.SameNormalizedText(Artist, media.Artist)
        && ExactTitles.Any(title => RadioHistory.SameNormalizedText(title, media.Title));

    internal M9BContextCard ToContextCard() => new(Artist, Title, "catalog:" + Id, Facts);
}

internal sealed record M9BContextCard(
    string MatchedArtist,
    string? MatchedTrack,
    string Identity,
    IReadOnlyList<M9BCatalogFact> Facts)
{
    internal bool HasUsefulFacts => Facts.Count is >= 1 and <= 5 && Facts.All(fact => fact.IsSafe);
}

internal enum M9BContextSource
{
    Unknown,
    ReviewedCatalog,
    Cache,
    LiveEnrichment,
}

internal sealed record M9BContextResolution(
    MusicContextRequest Requested,
    MusicContextMatchStatus Status,
    M9BContextCard? Card,
    DateTimeOffset RetrievedAtUtc,
    string SafeExplanation,
    bool FromCache,
    M9BContextSource Source = M9BContextSource.Unknown)
{
    internal bool IsConfirmedWithFacts => Status == MusicContextMatchStatus.Confirmed
        && Card is not null
        && Card.HasUsefulFacts;
}
internal sealed record M9BContextCatalogManifest(string Version, IReadOnlyList<M9BContextCatalogEntry> Cards);
internal sealed record M9BContextCatalogEntry(string Id, string Artist, string Title, string EditorialUsefulness, IReadOnlyList<M9BContextCatalogFactEntry> Facts, IReadOnlyList<string>? TitleAliases = null);
internal sealed record M9BContextCatalogFactEntry(string Text, string SourceName, string Url, string Applicability);

internal sealed class M9BContextCatalog
{
    private readonly IReadOnlyList<M9BCatalogCard> _cards;

    private M9BContextCatalog(IReadOnlyList<M9BCatalogCard> cards)
    {
        _cards = cards;
    }

    internal IReadOnlyList<M9BCatalogCard> Cards => _cards;

    internal static M9BContextCatalog Load()
    {
        Assembly assembly = typeof(M9BContextCatalog).Assembly;
        string resourceName = assembly.GetManifestResourceNames().Single(name =>
            name.EndsWith(M9BConstants.CatalogResourceSuffix, StringComparison.Ordinal));
        using Stream stream = assembly.GetManifestResourceStream(resourceName)
            ?? throw new InvalidOperationException("The M9B context catalog could not be opened.");
        M9BContextCatalogManifest manifest = JsonSerializer.Deserialize<M9BContextCatalogManifest>(stream,
            new JsonSerializerOptions { PropertyNameCaseInsensitive = true })
            ?? throw new InvalidOperationException("The M9B context catalog was malformed.");

        if (manifest.Version != "1.1" || manifest.Cards.Count != 16)
        {
            throw new InvalidOperationException("M9B requires exactly the reviewed 16-card version 1.1 catalog.");
        }

        var cards = new List<M9BCatalogCard>();
        foreach (M9BContextCatalogEntry entry in manifest.Cards)
        {
            if (string.IsNullOrWhiteSpace(entry.Id)
                || string.IsNullOrWhiteSpace(entry.Title)
                || string.IsNullOrWhiteSpace(entry.Artist)
                || entry.EditorialUsefulness is not "useful" and not "weak"
                || entry.Facts.Count is < 1 or > 5)
            {
                throw new InvalidOperationException("The M9B context catalog contains an invalid card.");
            }

            var facts = new List<M9BCatalogFact>();
            foreach (M9BContextCatalogFactEntry fact in entry.Facts)
            {
                if (!Uri.TryCreate(fact.Url, UriKind.Absolute, out Uri? url))
                {
                    throw new InvalidOperationException("The M9B context catalog contains an invalid source URL.");
                }

                var mapped = new M9BCatalogFact(fact.Text, fact.SourceName, url, fact.Applicability);
                if (!mapped.IsSafe || string.IsNullOrWhiteSpace(mapped.Applicability))
                {
                    throw new InvalidOperationException("The M9B context catalog contains an unsafe fact.");
                }

                facts.Add(mapped);
            }

            string[] aliases = (entry.TitleAliases ?? []).ToArray();
            if (aliases.Any(string.IsNullOrWhiteSpace)
                || aliases.Any(alias => RadioHistory.SameNormalizedText(alias, entry.Title))
                || aliases.Distinct(StringComparer.OrdinalIgnoreCase).Count() != aliases.Length)
            {
                throw new InvalidOperationException("The M9B context catalog contains invalid title aliases.");
            }

            cards.Add(new M9BCatalogCard(
                entry.Id,
                entry.Artist,
                entry.Title,
                string.Equals(entry.EditorialUsefulness, "useful", StringComparison.Ordinal),
                facts,
                aliases));
        }

        if (cards.Select(card => card.Id).Distinct(StringComparer.Ordinal).Count() != cards.Count)
        {
            throw new InvalidOperationException("The M9B context catalog contains duplicate card identifiers.");
        }

        if (cards.SelectMany(card => card.ExactTitles.Select(title => MusicContextCache.Normalize(card.Artist) + "|" + MusicContextCache.Normalize(title)))
            .Distinct(StringComparer.Ordinal).Count() != cards.Sum(card => card.ExactTitles.Count))
        {
            throw new InvalidOperationException("The M9B context catalog contains ambiguous artist/title aliases.");
        }

        return new M9BContextCatalog(cards);
    }

    internal M9BCatalogCard? Find(MediaSnapshot media)
    {
        M9BCatalogCard[] matches = _cards.Where(card => card.Matches(media)).ToArray();
        return matches.Length == 1 ? matches[0] : null;
    }
}

internal static class M9BContextCachePaths
{
    internal static string DefaultDirectory => Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
        "ZoyaRadio",
        "cache",
        "music-context");
}

internal sealed class M9BContextCache
{
    private const string CacheSchemaVersion = "m9b-context-v2";
    private static readonly JsonSerializerOptions JsonOptions = new() { WriteIndented = true };
    private readonly string _directory;
    private readonly MusicContextEnrichmentConfiguration _configuration;

    internal M9BContextCache(string directory, MusicContextEnrichmentConfiguration configuration)
    {
        _directory = directory;
        _configuration = configuration;
        _configuration.Validate();
    }

    internal bool TryRead(MusicContextRequest request, out M9BContextResolution result)
    {
        result = default!;
        string path = PathFor(request);
        if (!File.Exists(path)) return false;
        try
        {
            CacheEntry? entry = JsonSerializer.Deserialize<CacheEntry>(File.ReadAllText(path), JsonOptions);
            if (entry is null || entry.ExpiresAtUtc <= DateTimeOffset.UtcNow || !SameRequest(entry.Result.Requested, request)) return false;
            if (!string.Equals(entry.SchemaVersion, CacheSchemaVersion, StringComparison.Ordinal)
                && entry.Result.Status != MusicContextMatchStatus.Confirmed) return false;
            result = entry.Result with { FromCache = true, Source = M9BContextSource.Cache };
            return true;
        }
        catch (Exception)
        {
            return false;
        }
    }

    internal void Store(M9BContextResolution result)
    {
        Directory.CreateDirectory(_directory);
        DateTimeOffset expiry = result.Status == MusicContextMatchStatus.Confirmed
            ? DateTimeOffset.UtcNow.Add(_configuration.ConfirmedTtl)
            : DateTimeOffset.UtcNow.Add(_configuration.NegativeTtl);
        string path = PathFor(result.Requested);
        string temporary = path + "." + Guid.NewGuid().ToString("N") + ".tmp";
        try
        {
            File.WriteAllText(temporary, JsonSerializer.Serialize(new CacheEntry(CacheSchemaVersion, expiry, result with { FromCache = false }), JsonOptions), new UTF8Encoding(false));
            File.Move(temporary, path, overwrite: true);
        }
        finally
        {
            if (File.Exists(temporary)) File.Delete(temporary);
        }

        FileInfo[] files = Directory.EnumerateFiles(_directory, "*.json", SearchOption.TopDirectoryOnly)
            .Select(path => new FileInfo(path)).OrderByDescending(file => file.LastWriteTimeUtc).ToArray();
        foreach (FileInfo excess in files.Skip(_configuration.CacheEntryLimit)) excess.Delete();
    }

    internal void Clear()
    {
        if (!Directory.Exists(_directory)) return;
        foreach (string path in Directory.EnumerateFiles(_directory, "*.json", SearchOption.AllDirectories)) File.Delete(path);
    }

    private string PathFor(MusicContextRequest request)
    {
        string normalized = MusicContextCache.Normalize(request.Title) + "\u001F" + MusicContextCache.Normalize(request.Artist) + "\u001F" + request.IdentityRequirement;
        return Path.Combine(_directory, Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(normalized))) + ".json");
    }

    private static bool SameRequest(MusicContextRequest left, MusicContextRequest right) => left.IdentityRequirement == right.IdentityRequirement
        && string.Equals(MusicContextCache.Normalize(left.Title), MusicContextCache.Normalize(right.Title), StringComparison.Ordinal)
        && string.Equals(MusicContextCache.Normalize(left.Artist), MusicContextCache.Normalize(right.Artist), StringComparison.Ordinal);

    private sealed record CacheEntry(string? SchemaVersion, DateTimeOffset ExpiresAtUtc, M9BContextResolution Result);
}

internal sealed class M9BContextProvider
{
    private readonly M9BContextCatalog _catalog;
    private readonly IMusicContextEnricher _liveResolver;
    private readonly M9BContextCache _cache;
    private readonly IAnnouncementLog _log;

    internal M9BContextProvider(M9BContextCatalog catalog, IMusicContextEnricher liveResolver, M9BContextCache cache, IAnnouncementLog log)
    {
        _catalog = catalog;
        _liveResolver = liveResolver;
        _cache = cache;
        _log = log;
    }

    internal async Task<M9BContextResolution> ResolveAsync(MediaSnapshot media, CancellationToken cancellationToken)
    {
        var request = new MusicContextRequest(media.Title ?? string.Empty, media.Artist ?? string.Empty, MusicIdentityRequirement.ArtistAndTrack);
        if (string.IsNullOrWhiteSpace(request.Title) || string.IsNullOrWhiteSpace(request.Artist))
        {
            return new M9BContextResolution(request, MusicContextMatchStatus.Unavailable, null, DateTimeOffset.UtcNow, "Track metadata was incomplete; M9B context was not requested.", false);
        }

        M9BCatalogCard? catalogCard = _catalog.Find(media);
        if (catalogCard is not null)
        {
            var result = new M9BContextResolution(request, MusicContextMatchStatus.Confirmed, catalogCard.ToContextCard(), DateTimeOffset.UtcNow,
                catalogCard.EditoriallyUseful ? "Reviewed local M9B context card selected." : "Reviewed local M9B context card is editorially weak.", false,
                M9BContextSource.ReviewedCatalog);
            _cache.Store(result);
            _log.Write($"M9B CONTEXT: reviewed card {catalogCard.Id} selected before cache lookup; no cited page was fetched.");
            return result;
        }

        if (_cache.TryRead(request, out M9BContextResolution cached))
        {
            _log.Write($"M9B CONTEXT CACHE: {cached.Status} result reused for the current Track-A metadata.");
            return cached;
        }

        MusicContextEnrichmentResult live = await _liveResolver.EnrichAsync(request, cancellationToken);
        M9BContextCard? card = live.FactCard is null ? null : new M9BContextCard(
            live.FactCard.MatchedArtist,
            live.FactCard.MatchedTrack,
            live.FactCard.MusicBrainzArtistId + ":" + live.FactCard.WikidataId,
            live.FactCard.Facts.Select(fact => new M9BCatalogFact(fact.Text, fact.SourceName, fact.SourceUrl, "live-structured")).ToArray());
        var mapped = new M9BContextResolution(request, live.Status, card, live.RetrievedAtUtc, live.SafeExplanation, false, M9BContextSource.LiveEnrichment);
        _cache.Store(mapped);
        _log.Write(mapped.IsConfirmedWithFacts ? "M9B CONTEXT: exact live MusicBrainz identity and approved structured facts confirmed." : $"M9B CONTEXT UNAVAILABLE: {mapped.SafeExplanation}");
        return mapped;
    }

    internal void ClearCache()
    {
        _cache.Clear();
        _log.Write("M9B CONTEXT CACHE CLEARED: bounded local context entries were removed.");
    }
}
internal enum M9BCommentValidationKind
{
    Valid,
    Fallback,
    HardDiscard,
    Advisory,
}

internal sealed record M9BCommentValidation(
    M9BCommentValidationKind Kind,
    string NormalizedText,
    string Reason,
    IReadOnlyList<string> AdvisoryFlags)
{
    internal bool IsUsable => Kind is M9BCommentValidationKind.Valid or M9BCommentValidationKind.Advisory;
}

internal static class M9BWholeCommentPromptFactory
{
    private static readonly Lazy<string> Template = new(LoadTemplate);

    internal static CommentPrompt Create(
        MediaSnapshot media,
        M9BContextCard card,
        IReadOnlyList<string> recentComments)
    {
        if (media.Title is null || media.Artist is null)
        {
            throw new ArgumentException("M9B requires title and artist metadata.", nameof(media));
        }

        var context = new
        {
            title = media.Title,
            artist = media.Artist,
            contextCard = new
            {
                matchedArtist = card.MatchedArtist,
                matchedTrack = card.MatchedTrack,
                facts = card.Facts.Select(fact => new
                {
                    fact = fact.Text,
                    source = fact.SourceName,
                    sourceUrl = fact.SourceUrl.ToString(),
                }),
            },
            recentAcceptedComments = recentComments,
        };

        return new CommentPrompt(
            Template.Value,
            "<M9B_CONTEXT_JSON>\n" + JsonSerializer.Serialize(context) + "\n</M9B_CONTEXT_JSON>\nWrite the complete comment now.");
    }

    private static string LoadTemplate()
    {
        Assembly assembly = typeof(M9BWholeCommentPromptFactory).Assembly;
        string resourceName = assembly.GetManifestResourceNames().Single(name =>
            name.EndsWith(M9BConstants.PromptResourceSuffix, StringComparison.Ordinal));
        using Stream stream = assembly.GetManifestResourceStream(resourceName)
            ?? throw new InvalidOperationException("The M9B whole-comment prompt could not be opened.");
        using var reader = new StreamReader(stream);
        return reader.ReadToEnd().Trim();
    }
}

internal static class M9BCommentValidator
{
    private static readonly Regex UrlPattern = new("https?://|www\\.", RegexOptions.IgnoreCase | RegexOptions.CultureInvariant);
    private static readonly Regex MetaPattern = new("as an ai|ignore (all )?previous|system prompt|<m9b_", RegexOptions.IgnoreCase | RegexOptions.CultureInvariant);

    private static readonly Regex SemanticExaggerationPattern = new("\\b(shut down|shut the .*exchange|caused the .*exchange to close)\\b", RegexOptions.IgnoreCase | RegexOptions.CultureInvariant);
    private static readonly Regex ClichePattern = new("\\b(absolute banger|timeless classic|turn it up|stay tuned|you'?re listening to|what a track)\\b", RegexOptions.IgnoreCase | RegexOptions.CultureInvariant);
    private static readonly Regex ImaginedPattern = new("\\b(i'?m picturing|i can see|picture this)\\b", RegexOptions.IgnoreCase | RegexOptions.CultureInvariant);

    internal static M9BCommentValidation Validate(
        string? output,
        MediaSnapshot media,
        M9BContextCard card,
        IReadOnlyList<string> recentComments,
        IReadOnlyList<string> recentFactIdentities)
    {
        string normalized = CommentValidator.NormalizeWhitespace(output ?? string.Empty);
        if (string.Equals(normalized, CommentValidator.FallbackSignal, StringComparison.Ordinal))
        {
            return new M9BCommentValidation(M9BCommentValidationKind.Fallback, normalized, "The model selected FALLBACK.", []);
        }

        int words = normalized.Split(' ', StringSplitOptions.RemoveEmptyEntries).Length;
        if (string.IsNullOrWhiteSpace(normalized)
            || normalized.Contains('\uFFFD')
            || normalized.Contains('\r')
            || normalized.Contains('\n')
            || words > 70
            || UrlPattern.IsMatch(normalized)
            || MetaPattern.IsMatch(normalized))
        {
            return new M9BCommentValidation(M9BCommentValidationKind.HardDiscard, normalized, "Output was malformed, unsafe, or prompt/meta/URL text.", []);
        }

        if ((media.Title?.Any(character => character > 127) == true || media.Artist?.Any(character => character > 127) == true)
            && normalized.Contains('?', StringComparison.Ordinal))
        {
            return new M9BCommentValidation(M9BCommentValidationKind.HardDiscard, normalized, "Output appears to substitute Unicode metadata with question marks.", []);
        }

        string combinedFacts = string.Join(" ", card.Facts.Select(fact => fact.Text));
        if (SemanticExaggerationPattern.IsMatch(normalized)
            && combinedFacts.Contains("doors to close", StringComparison.OrdinalIgnoreCase))
        {
            return new M9BCommentValidation(M9BCommentValidationKind.HardDiscard, normalized, "Output materially exaggerates the supplied exchange-door fact.", []);
        }

        if (HasExplicitCurrentMetadataContradiction(normalized, media))
        {
            return new M9BCommentValidation(M9BCommentValidationKind.HardDiscard, normalized, "Output explicitly contradicts the current title or artist.", []);
        }

        var flags = new List<string>();
        if (ContainsNumberOutsideCard(normalized, media, card)) flags.Add("Contains a precise number or date outside the supplied card.");
        if (ContainsKnownEntityOutsideCard(normalized, media, card)) flags.Add("Contains a music-related named entity outside the supplied card.");
        if (words < 12 || words > 45) flags.Add("Outside the normal 12-45 spoken-word target.");
        if (Regex.Matches(normalized, "[.!?]+(?=\\s|$)").Count > 2) flags.Add("More than two sentences.");
        if (normalized.TrimEnd().EndsWith("?", StringComparison.Ordinal)) flags.Add("Rhetorical question.");
        if (ClichePattern.IsMatch(normalized)) flags.Add("Generic DJ cliché.");
        if (ImaginedPattern.IsMatch(normalized)) flags.Add("Imagined-scene phrasing.");
        if (card.Facts.Any(fact => normalized.Contains(fact.Text, StringComparison.OrdinalIgnoreCase))) flags.Add("Near-verbatim fact-card recitation.");
        if (IsRecentDuplicate(normalized, recentComments)) flags.Add("Exact or repeated opening/template.");
        if (recentFactIdentities.Contains(FactIdentity(card), StringComparer.Ordinal)) flags.Add("The same selected fact identity was recently used.");

        return new M9BCommentValidation(
            flags.Count == 0 ? M9BCommentValidationKind.Valid : M9BCommentValidationKind.Advisory,
            normalized,
            flags.Count == 0 ? "Grounded mechanical checks passed." : "Candidate has advisory quality/repetition flags.",
            flags);
    }

    internal static string FactIdentity(M9BContextCard card)
    {
        string source = card.Facts.FirstOrDefault()?.SourceUrl.ToString() ?? card.Identity;
        return Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(card.Identity + "\u001F" + card.MatchedTrack + "\u001F" + source)));
    }

    private static bool HasExplicitCurrentMetadataContradiction(string text, MediaSnapshot media)
    {
        if (string.IsNullOrWhiteSpace(media.Title) || string.IsNullOrWhiteSpace(media.Artist)) return false;
        string escapedTitle = Regex.Escape(media.Title);
        Match directAttribution = Regex.Match(
            text,
            $"\\b(?:this is|that was|now playing)\\s+[\\\"“]?\\s*{escapedTitle}\\s*[\\\"”]?\\s+by\\s+(?<artist>[^.!?]+)",
            RegexOptions.IgnoreCase | RegexOptions.CultureInvariant);
        if (directAttribution.Success
            && !directAttribution.Groups["artist"].Value.Trim().StartsWith(media.Artist, StringComparison.OrdinalIgnoreCase))
        {
            return true;
        }

        if (!Regex.IsMatch(text, "\\b(?:this is|that was|now playing)\\b", RegexOptions.IgnoreCase | RegexOptions.CultureInvariant)) return false;
        return M9BContextCatalog.Load().Cards.Any(card =>
            !string.Equals(card.Title, media.Title, StringComparison.OrdinalIgnoreCase)
            && text.Contains(card.Title, StringComparison.OrdinalIgnoreCase));
    }

    private static bool ContainsNumberOutsideCard(string text, MediaSnapshot media, M9BContextCard card)
    {
        string allowed = string.Join(" ", new[] { media.Title, media.Artist, card.MatchedArtist, card.MatchedTrack }.Concat(card.Facts.Select(fact => fact.Text)));
        return Regex.Matches(text, "\\b\\d{1,4}\\b").Any(number => !allowed.Contains(number.Value, StringComparison.Ordinal));
    }

    private static bool ContainsKnownEntityOutsideCard(string text, MediaSnapshot media, M9BContextCard card)
    {
        string allowed = string.Join(" ", new[] { media.Title, media.Artist, card.MatchedArtist, card.MatchedTrack }.Concat(card.Facts.Select(fact => fact.Text)));
        string[] known = ["Neil Young", "Belchior", "Chet Atkins", "David Bowie", "Michael Moore", "Wyclef Jean", "Paulo Coelho", "Ozzy Osbourne", "Vinny Appice", "New York Stock Exchange", "Folsom State Prison", "Ed King"];
        return known.Any(entity => text.Contains(entity, StringComparison.OrdinalIgnoreCase)
            && !allowed.Contains(entity, StringComparison.OrdinalIgnoreCase));
    }

    private static bool IsRecentDuplicate(string candidate, IReadOnlyList<string> recent)
    {
        string opening = Signature(candidate);
        return recent.Any(item => string.Equals(CommentValidator.NormalizeWhitespace(item), candidate, StringComparison.OrdinalIgnoreCase)
            || string.Equals(Signature(item), opening, StringComparison.OrdinalIgnoreCase));
    }

    internal static string Signature(string text)
    {
        string[] words = CommentValidator.NormalizeWhitespace(text)
            .Split(' ', StringSplitOptions.RemoveEmptyEntries)
            .Take(6)
            .Select(word => Regex.Replace(word, "[^\\p{L}\\p{N}]", string.Empty).ToLowerInvariant())
            .Where(word => word.Length > 0)
            .ToArray();
        return string.Join(' ', words);
    }
}

internal sealed class M9BRecentCommentState
{
    private readonly List<string> _comments = [];
    private readonly List<string> _factIdentities = [];

    internal IReadOnlyList<string> Comments => _comments.ToArray();
    internal IReadOnlyList<string> FactIdentities => _factIdentities.ToArray();

    internal void Add(string comment, M9BContextCard card)
    {
        _comments.Add(CommentValidator.NormalizeWhitespace(comment));
        _factIdentities.Add(M9BCommentValidator.FactIdentity(card));
        while (_comments.Count > 6) _comments.RemoveAt(0);
        while (_factIdentities.Count > 6) _factIdentities.RemoveAt(0);
    }

    internal void Clear()
    {
        _comments.Clear();
        _factIdentities.Clear();
    }
}

internal enum M9BCommentSelectionKind
{
    Unknown,
    Ready,
    AiDisabled,
    EnrichmentUnavailable,
    ModelUnavailable,
    GenerationFailed,
    ModelFallback,
    HardDiscard,
    AdvisoryFallback,
}

internal sealed record M9BGenerationDiagnostic(
    int Attempt,
    string? RawOutput,
    string? NormalizedOutput,
    M9BCommentValidationKind? ValidationKind,
    string Reason,
    IReadOnlyList<string> AdvisoryFlags,
    OllamaGenerationMetrics? Metrics);

internal sealed record M9BCommentSelection(
    CommentSelection? Selection,
    M9BContextCard? Card,
    string Reason,
    bool UsedRegeneration,
    bool ModelUnavailable,
    OllamaGenerationMetrics? Metrics = null,
    bool ContextFromCache = false,
    M9BCommentSelectionKind OutcomeKind = M9BCommentSelectionKind.Unknown,
    M9BContextSource ContextSource = M9BContextSource.Unknown,
    IReadOnlyList<M9BGenerationDiagnostic>? GenerationDiagnostics = null);

internal sealed class M9BGroundedCommentService
{
    private readonly M9BContextProvider _contextProvider;
    private readonly ILocalCommentGenerator _generator;
    private readonly AiCommentConfiguration _configuration;
    private readonly M9BRecentCommentState _recent;
    private readonly IAnnouncementLog _log;

    internal M9BGroundedCommentService(
        M9BContextProvider contextProvider,
        ILocalCommentGenerator generator,
        AiCommentConfiguration configuration,
        M9BRecentCommentState recent,
        IAnnouncementLog log)
    {
        _contextProvider = contextProvider;
        _generator = generator;
        _configuration = configuration;
        _recent = recent;
        _log = log;
    }

    internal async Task<M9BCommentSelection> TrySelectAsync(
        RadioPreparationContext context,
        bool aiEnabled,
        CancellationToken cancellationToken)
    {
        if (!aiEnabled)
        {
            return new M9BCommentSelection(null, null, "M9B skipped because AI is disabled.", false, false,
                OutcomeKind: M9BCommentSelectionKind.AiDisabled);
        }

        M9BContextResolution resolved = await _contextProvider.ResolveAsync(context.SubjectTrack, cancellationToken);
        if (!resolved.IsConfirmedWithFacts || resolved.Card is null || !resolved.Card.HasUsefulFacts)
        {
            return new M9BCommentSelection(null, null, "M9B enrichment was unavailable, ambiguous, weak, or unsafe.", false, false,
                ContextFromCache: resolved.FromCache,
                OutcomeKind: M9BCommentSelectionKind.EnrichmentUnavailable,
                ContextSource: resolved.Source,
                GenerationDiagnostics: []);
        }

        M9BCommentSelection Result(
            CommentSelection? selection,
            string reason,
            bool usedRegeneration,
            bool modelUnavailable,
            OllamaGenerationMetrics? metrics,
            M9BCommentSelectionKind outcome,
            IReadOnlyList<M9BGenerationDiagnostic> diagnostics) => new(
                selection,
                resolved.Card,
                reason,
                usedRegeneration,
                modelUnavailable,
                metrics,
                resolved.FromCache,
                outcome,
                resolved.Source,
                diagnostics);

        OllamaInstalledModelsResult installed = await _generator.GetInstalledModelsAsync(
            _configuration.LocalOperationTimeout,
            cancellationToken);
        bool modelPresent = installed.Success && installed.Models.Any(model =>
            string.Equals(model.Name, M9BConstants.Model, StringComparison.OrdinalIgnoreCase)
            || string.Equals(model.Model, M9BConstants.Model, StringComparison.OrdinalIgnoreCase));
        if (!modelPresent)
        {
            return Result(null, "M9B 14B model is absent or unavailable; preserving M8 fallback.", false, true, null,
                M9BCommentSelectionKind.ModelUnavailable, []);
        }

        CommentPrompt prompt = M9BWholeCommentPromptFactory.Create(context.SubjectTrack, resolved.Card, _recent.Comments);
        var diagnostics = new List<M9BGenerationDiagnostic>();
        bool regenerated = false;
        try
        {
            for (int attempt = 0; attempt < 2; attempt++)
            {
                _log.Write($"M9B GROUNDED GENERATION STARTED: attempt {attempt + 1} of 2; 14B remains local and Track A keeps Playing.");
                OllamaGenerationResult generation = await _generator.GenerateAsync(
                    prompt,
                    _configuration.NormalKeepAlive,
                    _configuration.GenerationTimeout,
                    cancellationToken);
                if (generation.Status == OllamaGenerationStatus.Cancelled && cancellationToken.IsCancellationRequested)
                {
                    throw new OperationCanceledException(cancellationToken);
                }

                if (generation.Status != OllamaGenerationStatus.Success)
                {
                    string reason = generation.ErrorMessage ?? "M9B generation failed.";
                    diagnostics.Add(new M9BGenerationDiagnostic(
                        attempt + 1,
                        generation.Response,
                        CommentValidator.NormalizeWhitespace(generation.Response ?? string.Empty),
                        null,
                        reason,
                        [],
                        generation.Metrics));
                    return Result(null, reason, regenerated, false, generation.Metrics,
                        M9BCommentSelectionKind.GenerationFailed, diagnostics);
                }

                M9BCommentValidation validation = M9BCommentValidator.Validate(
                    generation.Response,
                    context.SubjectTrack,
                    resolved.Card,
                    _recent.Comments,
                    _recent.FactIdentities);
                diagnostics.Add(new M9BGenerationDiagnostic(
                    attempt + 1,
                    generation.Response,
                    validation.NormalizedText,
                    validation.Kind,
                    validation.Reason,
                    validation.AdvisoryFlags,
                    generation.Metrics));
                if (validation.Kind == M9BCommentValidationKind.Fallback)
                {
                    return Result(null, "M9B model selected FALLBACK.", regenerated, false, generation.Metrics,
                        M9BCommentSelectionKind.ModelFallback, diagnostics);
                }

                if (validation.Kind == M9BCommentValidationKind.HardDiscard)
                {
                    return Result(null, validation.Reason, regenerated, false, generation.Metrics,
                        M9BCommentSelectionKind.HardDiscard, diagnostics);
                }

                if (validation.Kind == M9BCommentValidationKind.Advisory && attempt == 0)
                {
                    regenerated = true;
                    _log.Write($"M9B ADVISORY REGENERATION: {string.Join("; ", validation.AdvisoryFlags)}");
                    continue;
                }

                if (validation.Kind == M9BCommentValidationKind.Advisory)
                {
                    return Result(
                        new CommentSelection(validation.NormalizedText, CommentTextSource.GroundedAi,
                            validation.NormalizedText.Split(' ', StringSplitOptions.RemoveEmptyEntries).Length,
                            "M9B advisory candidate retained after bounded regeneration.", generation, false),
                        "M9B advisory candidate is ready after bounded regeneration.", true, false, generation.Metrics,
                        M9BCommentSelectionKind.Ready, diagnostics);
                }

                return Result(
                    new CommentSelection(validation.NormalizedText, CommentTextSource.GroundedAi,
                        validation.NormalizedText.Split(' ', StringSplitOptions.RemoveEmptyEntries).Length,
                        "M9B grounded context candidate passed mechanical checks.", generation, false),
                    "M9B grounded candidate ready.", regenerated, false, generation.Metrics,
                    M9BCommentSelectionKind.Ready, diagnostics);
            }
        }
        finally
        {
            OllamaOperationResult unload = await _generator.UnloadAsync(_configuration.LocalOperationTimeout, CancellationToken.None);
            _log.Write($"M9B 14B UNLOAD: {(unload.Success ? "requested" : "not confirmed")} - {unload.Message}");
        }

        return Result(null, "M9B candidate was unavailable.", regenerated, false, null,
            M9BCommentSelectionKind.GenerationFailed, diagnostics);
    }
    internal void RecordDelivered(CommentSelection selection, M9BContextCard? card)
    {
        if (selection.Source == CommentTextSource.GroundedAi && card is not null)
        {
            _recent.Add(selection.Text, card);
        }
    }

    internal void ClearHistoryAndCache()
    {
        _recent.Clear();
        _contextProvider.ClearCache();
    }

    internal void ClearEphemeralHistory() => _recent.Clear();
}

internal sealed class M9BTransitionCommentProvider : ITransitionCommentProvider
{
    private readonly ITransitionCommentProvider _m8Fallback;
    private readonly M9BGroundedCommentService _m9b;
    private readonly IAnnouncementLog _log;
    private readonly Dictionary<string, M9BContextCard> _preparedCards = new(StringComparer.Ordinal);

    internal M9BTransitionCommentProvider(
        ITransitionCommentProvider m8Fallback,
        M9BGroundedCommentService m9b,
        IAnnouncementLog log)
    {
        _m8Fallback = m8Fallback;
        _m9b = m9b;
        _log = log;
    }

    public async Task<CommentSelection> SelectAsync(
        RadioPreparationContext context,
        bool aiEnabled,
        CancellationToken cancellationToken)
    {
        M9BCommentSelection enriched = await _m9b.TrySelectAsync(context, aiEnabled, cancellationToken);
        if (enriched.Selection is not null)
        {
            _preparedCards[Fingerprint(context)] = enriched.Card!;
            _log.Write("M9B STATUS: enriched candidate ready; it will be used only if the existing delivery safety checks pass.");
            return enriched.Selection;
        }

        _log.Write($"M9B STATUS: enriched candidate discarded - {enriched.Reason}");
        _log.Write("M9B STATUS: accepted M8 fallback selection begins now; no simultaneous Ollama request is used.");
        return await _m8Fallback.SelectAsync(context, aiEnabled, cancellationToken);
    }

    internal void RecordCompleted(RadioPreparationOpportunity opportunity, CommentSelection selection)
    {
        string key = Fingerprint(opportunity.Context);
        if (_preparedCards.Remove(key, out M9BContextCard? card))
        {
            _m9b.RecordDelivered(selection, card);
        }
    }

    internal void Discard(RadioPreparationOpportunity opportunity)
    {
        _preparedCards.Remove(Fingerprint(opportunity.Context));
    }

    internal void ClearHistoryAndCache()
    {
        _preparedCards.Clear();
        _m9b.ClearHistoryAndCache();
    }

    internal void ClearEphemeralHistory()
    {
        _preparedCards.Clear();
        _m9b.ClearEphemeralHistory();
    }

    private static string Fingerprint(RadioPreparationContext context)
    {
        return MusicContextCache.Normalize(context.SubjectTrack.Title ?? string.Empty)
            + "\u001F" + MusicContextCache.Normalize(context.SubjectTrack.Artist ?? string.Empty)
            + "\u001F" + context.SubjectTrackStartedAt.UtcTicks;
    }
}