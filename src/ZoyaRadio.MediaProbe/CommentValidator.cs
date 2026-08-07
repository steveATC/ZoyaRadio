using System.Text.RegularExpressions;

namespace ZoyaRadio.MediaProbe;

internal sealed record CommentValidationResult(
    bool IsValid,
    string NormalizedText,
    int WordCount,
    string Reason);

internal static partial class CommentValidator
{
    public const string FallbackSignal = "FALLBACK";
    public const int MinimumWords = 15;
    public const int MaximumWords = 40;

    private static readonly string[] BannedRadioPhrases =
    [
        "what a track",
        "absolute banger",
        "timeless classic",
        "another classic",
        "ladies and gentlemen",
        "stay tuned",
        "you're listening to",
        "you’re listening to",
        "turn it up",
        "up next",
        "keep those vibes going",
        "on the air",
        "great song",
    ];

    private static readonly string[] ModelCommentaryPhrases =
    [
        "as an ai",
        "language model",
        "based on the provided metadata",
        "based on the metadata",
        "here is the comment",
        "here's the comment",
        "my reasoning",
        "the prompt",
        "i was instructed",
        "i cannot",
        "i can't",
    ];

    public static CommentValidationResult Validate(string? output)
    {
        if (string.IsNullOrWhiteSpace(output))
        {
            return Invalid(string.Empty, 0, "The model returned no comment.");
        }

        if (output.Any(character => char.IsControl(character) && character is not '\r' and not '\n' and not '\t'))
        {
            return Invalid(string.Empty, 0, "The output contains unsupported control characters.");
        }

        if (MultipleParagraphsRegex().IsMatch(output))
        {
            string paragraphText = NormalizeWhitespace(output);
            return Invalid(paragraphText, CountWords(paragraphText), "The output contains multiple paragraphs.");
        }

        string normalized = NormalizeWhitespace(output);
        int wordCount = CountWords(normalized);

        if (wordCount < MinimumWords)
        {
            return Invalid(normalized, wordCount, $"The AI comment is under {MinimumWords} words.");
        }

        if (wordCount > MaximumWords)
        {
            return Invalid(normalized, wordCount, $"The AI comment is over {MaximumWords} words.");
        }

        if (ContainsAny(normalized, BannedRadioPhrases))
        {
            return Invalid(normalized, wordCount, "The output contains a banned commercial-radio cliché.");
        }

        if (ContainsAny(normalized, ModelCommentaryPhrases)
            || normalized.Contains("```", StringComparison.Ordinal)
            || normalized.Contains("<think", StringComparison.OrdinalIgnoreCase)
            || normalized.StartsWith("Comment:", StringComparison.OrdinalIgnoreCase)
            || normalized.StartsWith("Response:", StringComparison.OrdinalIgnoreCase))
        {
            return Invalid(normalized, wordCount, "The output contains model commentary or malformed wrapper text.");
        }

        return new CommentValidationResult(true, normalized, wordCount, "Accepted by mechanical validation.");
    }

    public static bool IsFallbackSignal(string? output)
    {
        return string.Equals(output?.Trim(), FallbackSignal, StringComparison.Ordinal);
    }

    public static string NormalizeWhitespace(string value)
    {
        return WhitespaceRegex().Replace(value, " ").Trim();
    }

    public static int CountWords(string normalizedText)
    {
        return string.IsNullOrWhiteSpace(normalizedText)
            ? 0
            : normalizedText.Split(' ', StringSplitOptions.RemoveEmptyEntries).Length;
    }

    private static bool ContainsAny(string text, IEnumerable<string> phrases)
    {
        return phrases.Any(phrase => text.Contains(phrase, StringComparison.OrdinalIgnoreCase));
    }

    private static CommentValidationResult Invalid(string text, int wordCount, string reason)
    {
        return new CommentValidationResult(false, text, wordCount, reason);
    }

    [GeneratedRegex(@"(?:\r?\n)\s*(?:\r?\n)")]
    private static partial Regex MultipleParagraphsRegex();

    [GeneratedRegex(@"\s+")]
    private static partial Regex WhitespaceRegex();
}
