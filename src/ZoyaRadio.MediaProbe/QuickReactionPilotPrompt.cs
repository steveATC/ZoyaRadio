using System.Globalization;
using System.Text;
using System.Text.Json;

namespace ZoyaRadio.MediaProbe;

/// <summary>
/// D3.2's deliberately narrow runtime-pilot prompt boundary. This is separate
/// from the accepted D1 laboratory assignment so a live-pilot revision is not
/// silently substituted into the evaluation record.
/// </summary>
internal static class QuickReactionPilotPrompt
{
    internal const string AssignmentVersion = "quick-reaction-v3-topic-isolation";

    internal const string Assignment =
        "You are Zoya Radio, speaking during a short on-air transition. Write one brief, conversational quick reaction whose only subject is the CURRENT SUBJECT supplied below: its current track, current artist, or immediate transition. Have a point of view: notice something, make a dry observation, joke, use literal wordplay, react emotionally, or make a clearly playful imaginary aside that is not presented as fact. Do not make an announcement, review, biography, factual explanation, session summary, or generic commentary. Supplied current-subject facts are optional, never a checklist. Do not invent facts about the artist, track, lyrics, sound, genre, history, listener, or listening habits. AVOIDANCE DATA contains abstract form measurements only. Use it solely to choose a materially different opening and structure, especially when a recent line used a single-word comma lead. It is not source material. Prior accepted lines are deliberately absent: do not guess or recover their words, subjects, facts, premises, quotations, scenes, entities, jokes, or unfinished ideas; do not refer to or continue any prior topic. Sound like a radio host, not an assistant demonstrating metadata. Keep it concise: one or two sentences and one good idea. Return plain spoken text only: no Markdown, asterisks, underscores used as formatting, emoji, stage directions, labels, or wrapper text.";

    internal static CommentPrompt Create(RadioPreparationContext context)
    {
        ArgumentNullException.ThrowIfNull(context);

        string[] recent = (context.RecentAcceptedAiComments ?? [])
            .Where(comment => !string.IsNullOrWhiteSpace(comment))
            .TakeLast(3)
            .Select(comment => CommentValidator.NormalizeWhitespace(comment!))
            .ToArray();

        string currentSubject = JsonSerializer.Serialize(new
        {
            title = context.SubjectTrack.Title,
            artist = context.SubjectTrack.Artist,
        });

        string avoidanceData = JsonSerializer.Serialize(new
        {
            sourceWindow = "newest three accepted spoken lines",
            containsPriorWordsOrTopics = false,
            patterns = recent.Select((line, index) => new
            {
                order = RelativeOrder(index, recent.Length),
                openingShape = OpeningShape(line),
                lengthBand = LengthBand(line),
                sentenceCount = SentenceCount(line),
                endingShape = EndingShape(line),
                usesDash = line.Contains('-', StringComparison.Ordinal)
                    || line.Contains('\u2013', StringComparison.Ordinal)
                    || line.Contains('\u2014', StringComparison.Ordinal),
                usesSemicolon = line.Contains(';', StringComparison.Ordinal),
            }),
        });

        return new CommentPrompt(
            Assignment,
            "CURRENT SUBJECT JSON (the only subject for this reaction): " + currentSubject + "\n" +
            "AVOIDANCE DATA JSON (abstract form only; never subject matter): " + avoidanceData + "\n" +
            "Write one fresh reaction to CURRENT SUBJECT and return only the spoken line.");
    }

    private static string RelativeOrder(int index, int count)
    {
        if (count == 1)
        {
            return "newest";
        }

        return index == count - 1 ? "newest" : $"older-{count - index - 1}";
    }

    private static string OpeningShape(string line)
    {
        int firstWhitespace = line.IndexOfAny([' ', '\t', '\r', '\n']);
        int firstComma = line.IndexOf(',');
        if (firstComma > 0 && (firstWhitespace < 0 || firstComma < firstWhitespace))
        {
            return "single-word-comma lead";
        }

        return "plain lead";
    }

    private static string LengthBand(string line)
    {
        int wordCount = CommentValidator.CountWords(line);
        return wordCount switch
        {
            <= 12 => "short",
            <= 24 => "medium",
            _ => "long",
        };
    }

    private static int SentenceCount(string line)
    {
        int count = 0;
        bool inEndingRun = false;
        foreach (char character in line)
        {
            bool isEnding = character is '.' or '!' or '?';
            if (isEnding && !inEndingRun)
            {
                count++;
            }

            inEndingRun = isEnding;
        }

        return Math.Max(1, count);
    }

    private static string EndingShape(string line)
    {
        if (line.EndsWith("?", StringComparison.Ordinal))
        {
            return "question";
        }

        return line.EndsWith("!", StringComparison.Ordinal)
            ? "exclamation"
            : "statement";
    }

    /// <summary>
    /// Removes presentation-only characters that have no place in spoken radio
    /// copy. It is intentionally not a factual or stylistic claim filter.
    /// </summary>
    internal static string NormalizeForSpeech(string? output)
    {
        if (string.IsNullOrEmpty(output))
        {
            return string.Empty;
        }

        var builder = new StringBuilder(output.Length);
        foreach (Rune rune in output.EnumerateRunes())
        {
            if (rune.Value is '*' or '_')
            {
                continue;
            }

            UnicodeCategory category = Rune.GetUnicodeCategory(rune);
            if (category is UnicodeCategory.OtherSymbol or UnicodeCategory.ModifierSymbol)
            {
                continue;
            }

            builder.Append(rune.ToString());
        }

        return CommentValidator.NormalizeWhitespace(builder.ToString());
    }
}
