using System.Reflection;
using System.Text.Json;

namespace ZoyaRadio.MediaProbe;

internal sealed record CommentPrompt(string SystemInstruction, string MetadataPrompt);

internal static class CommentPromptFactory
{
    private const string ResourceSuffix = "Prompts.Milestone3CommentPrompt.txt";
    private static readonly Lazy<string> SystemInstruction = new(LoadSystemInstruction);

    public static CommentPrompt Create(MediaSnapshot media)
    {
        if (media.Title is null || media.Artist is null)
        {
            throw new ArgumentException("A title and artist are required for comment generation.", nameof(media));
        }

        string metadataJson = JsonSerializer.Serialize(new
        {
            title = media.Title,
            artist = media.Artist,
        });

        return new CommentPrompt(
            SystemInstruction.Value,
            $"Song metadata JSON: {metadataJson}\nWrite the comment now.");
    }

    private static string LoadSystemInstruction()
    {
        Assembly assembly = typeof(CommentPromptFactory).Assembly;
        string? resourceName = assembly
            .GetManifestResourceNames()
            .SingleOrDefault(name => name.EndsWith(ResourceSuffix, StringComparison.Ordinal));

        if (resourceName is null)
        {
            throw new InvalidOperationException("The Milestone 3 comment prompt resource is missing.");
        }

        using Stream stream = assembly.GetManifestResourceStream(resourceName)
            ?? throw new InvalidOperationException("The Milestone 3 comment prompt could not be opened.");
        using var reader = new StreamReader(stream);
        return reader.ReadToEnd().Trim();
    }
}
