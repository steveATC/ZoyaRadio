using System.Reflection;
using System.Text.Encodings.Web;
using System.Text.Json;
using System.Text.Unicode;

namespace ZoyaRadio.MediaProbe;

internal static class SourceBackedCommentPromptFactory
{
    private const string ResourceSuffix = "Prompts.Milestone9CSourceBackedPrompt.txt";
    private static readonly Lazy<string> Template = new(LoadTemplate);
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        Encoder = JavaScriptEncoder.Create(UnicodeRanges.All),
    };

    public static CommentPrompt Create(MediaSnapshot media, MusicContextEnrichmentResult enrichment)
    {
        if (media.Title is null || media.Artist is null)
        {
            throw new ArgumentException("M9C needs exact title and artist metadata.", nameof(media));
        }

        object facts = enrichment.IsConfirmedWithFacts
            ? new
            {
                matchedArtist = enrichment.FactCard!.MatchedArtist,
                matchedTrack = enrichment.FactCard.MatchedTrack,
                commentaryUsefulness = enrichment.FactCard.CommentaryUsefulness.ToString(),
                facts = enrichment.FactCard.Facts.Select(fact => new
                {
                    text = fact.Text,
                    sourceName = fact.SourceName,
                    sourceUrl = fact.SourceUrl.AbsoluteUri,
                }),
            }
            : new { facts = Array.Empty<object>(), noEnrichmentExplanation = enrichment.SafeExplanation };

        return new CommentPrompt(
            Template.Value,
            "<EXACT_METADATA_JSON>\n" + JsonSerializer.Serialize(new { title = media.Title, artist = media.Artist }, JsonOptions) +
            "\n</EXACT_METADATA_JSON>\n<SOURCE_BACKED_FACT_CARD_JSON>\n" + JsonSerializer.Serialize(facts, JsonOptions) +
            "\n</SOURCE_BACKED_FACT_CARD_JSON>\nWrite the comment now.");
    }

    private static string LoadTemplate()
    {
        Assembly assembly = typeof(SourceBackedCommentPromptFactory).Assembly;
        string? resource = assembly.GetManifestResourceNames().SingleOrDefault(name => name.EndsWith(ResourceSuffix, StringComparison.Ordinal));
        if (resource is null)
        {
            throw new InvalidOperationException("The M9C source-backed prompt resource is missing.");
        }

        using Stream stream = assembly.GetManifestResourceStream(resource)
            ?? throw new InvalidOperationException("The M9C source-backed prompt resource could not be opened.");
        using var reader = new StreamReader(stream);
        return reader.ReadToEnd().Trim();
    }
}
