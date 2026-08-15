using System.Text.RegularExpressions;

namespace ZoyaRadio.MediaProbe;

internal static class SourceBackedCommentValidator
{
    private static readonly string[] BannedPhrases = ["i'm picturing", "i\\u2019m picturing", "i'm seeing", "i\\u2019m seeing", "i'm choosing to interpret", "i\\u2019m choosing to interpret", "i remember", "i\\u2019m remembering", "i once saw", "sharp edge", "brings energy", "timeless", "classic"];

    public static CommentValidationResult Validate(string? response, MusicContextEnrichmentResult enrichment, IEnumerable<string>? recentAccepted = null)
    {
        if (CommentValidator.IsFallbackSignal(response)) return new CommentValidationResult(true, CommentValidator.FallbackSignal, 0, "Safe FALLBACK sentinel.");
        if (enrichment.FactCard?.CommentaryUsefulness != SourceBackedCommentaryUsefulness.RichHook) return new CommentValidationResult(false, CommentValidator.FallbackSignal, 0, "Only a rich, confirmed fact card may earn commentary.");

        string normalized = CommentValidator.NormalizeWhitespace(response ?? string.Empty);
        int words = WordCount(normalized);
        if (words < 6 || words > 32 || SentenceCount(normalized) > 2) return new CommentValidationResult(false, CommentValidator.FallbackSignal, words, "Source-backed commentary must be one or two sentences totaling 6-32 words.");
        if (BannedPhrases.Any(phrase => normalized.Contains(phrase, StringComparison.OrdinalIgnoreCase))) return new CommentValidationResult(false, CommentValidator.FallbackSignal, words, "Source-backed commentary used a banned scene-starter or generic cliche.");
        if (LooksLikeFactRecitation(normalized, enrichment.FactCard.Facts)) return new CommentValidationResult(false, CommentValidator.FallbackSignal, words, "Source-backed commentary recited or closely copied a fact card.");
        if (recentAccepted?.Any(previous => IsNearDuplicate(normalized, previous)) == true) return new CommentValidationResult(false, CommentValidator.FallbackSignal, words, "Source-backed commentary repeated a recent accepted line.");
        if (Regex.IsMatch(normalized, "^(great|nice|what a|that was)\\b", RegexOptions.IgnoreCase | RegexOptions.CultureInvariant)) return new CommentValidationResult(false, CommentValidator.FallbackSignal, words, "Source-backed commentary used factless generic praise.");
        return new CommentValidationResult(true, normalized, words, "Accepted concise fact-to-link commentary.");
    }

    private static int SentenceCount(string value)
        => Regex.Matches(value, "[.!?]+", RegexOptions.CultureInvariant).Count;
    private static bool LooksLikeFactRecitation(string response, IReadOnlyList<SourceBackedFact> facts)
        => facts.Any(fact => NormalizeForComparison(response).Contains(NormalizeForComparison(fact.Text), StringComparison.Ordinal) || TokenOverlap(response, fact.Text) >= 0.82);

    private static bool IsNearDuplicate(string left, string right)
        => string.Equals(NormalizeForComparison(left), NormalizeForComparison(right), StringComparison.Ordinal) || TokenOverlap(left, right) >= 0.82;

    private static double TokenOverlap(string left, string right)
    {
        HashSet<string> leftTokens = Tokens(left);
        HashSet<string> rightTokens = Tokens(right);
        if (leftTokens.Count == 0 || rightTokens.Count == 0) return 0;
        return leftTokens.Intersect(rightTokens, StringComparer.Ordinal).Count() / (double)Math.Min(leftTokens.Count, rightTokens.Count);
    }

    private static HashSet<string> Tokens(string value) => Regex.Matches(NormalizeForComparison(value), "[A-Z0-9]+", RegexOptions.CultureInvariant).Select(match => match.Value).Where(token => token.Length > 2).ToHashSet(StringComparer.Ordinal);
    private static string NormalizeForComparison(string value) => Regex.Replace(value.ToUpperInvariant(), "[^\\p{L}\\p{N}]+", " ").Trim();
    private static int WordCount(string value) => string.IsNullOrWhiteSpace(value) ? 0 : value.Split(' ', StringSplitOptions.RemoveEmptyEntries).Length;
}