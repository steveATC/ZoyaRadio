using System.Reflection;
using System.Text.Json;

namespace ZoyaRadio.MediaProbe;

internal sealed record CommentPrompt(string SystemInstruction, string MetadataPrompt);

internal static class CommentPromptFactory
{
    private const string HostNameToken = "{{HOST_NAME}}";
    private const string ListenerNameToken = "{{LISTENER_NAME}}";
    private const string ResourceSuffix = "Prompts.Milestone4HostPersonaPrompt.txt";
    private static readonly Lazy<string> SystemInstructionTemplate = new(LoadSystemInstructionTemplate);

    public static CommentPrompt Create(MediaSnapshot media)
    {
        return Create(media, HostPersonaConfiguration.Default);
    }

    public static CommentPrompt Create(
        MediaSnapshot media,
        HostPersonaConfiguration persona)
    {
        if (media.Title is null || media.Artist is null)
        {
            throw new ArgumentException("A title and artist are required for comment generation.", nameof(media));
        }

        persona.Validate();

        string metadataJson = JsonSerializer.Serialize(new
        {
            title = media.Title,
            artist = media.Artist,
        });

        return new CommentPrompt(
            RenderSystemInstruction(persona),
            $"Song metadata JSON: {metadataJson}\nWrite the comment now.");
    }

    public static CommentPrompt CreateForRadio(
        RadioCommentContext context,
        HostPersonaConfiguration persona)
    {
        if (context.PreviousTrack.Title is null
            || context.PreviousTrack.Artist is null
            || context.CurrentTrack.Title is null
            || context.CurrentTrack.Artist is null)
        {
            throw new ArgumentException(
                "Previous and current title/artist metadata are required for an automatic radio comment.",
                nameof(context));
        }

        persona.Validate();

        string contextJson = JsonSerializer.Serialize(new
        {
            previousTitle = context.PreviousTrack.Title,
            previousArtist = context.PreviousTrack.Artist,
            currentTitle = context.CurrentTrack.Title,
            currentArtist = context.CurrentTrack.Artist,
            trustedListeningObservations = context.TrustedListeningObservations,
            recentAcceptedComments = context.RecentAcceptedAiComments,
        });

        return new CommentPrompt(
            RenderSystemInstruction(persona),
            "Automatic radio context JSON: " + contextJson + "\n" +
            "Comment primarily on the previous track. The current track is transition context only. " +
            "Use trustedListeningObservations only as explicitly supplied facts. " +
            "Avoid repeating the opening, joke structure, distinctive metaphor, or unusually similar wording " +
            "from recentAcceptedComments. Write the comment now.");
    }

    public static CommentPrompt CreateForPreparedTransition(
        RadioPreparationContext context,
        HostPersonaConfiguration persona)
    {
        if (context.SubjectTrack.Title is null || context.SubjectTrack.Artist is null)
        {
            throw new ArgumentException(
                "Track-A title and artist metadata are required for transition preparation.",
                nameof(context));
        }

        persona.Validate();

        string contextJson = JsonSerializer.Serialize(new
        {
            title = context.SubjectTrack.Title,
            artist = context.SubjectTrack.Artist,
            trustedListeningObservations = context.TrustedListeningObservations,
            recentAcceptedComments = context.RecentAcceptedAiComments,
        });

        return new CommentPrompt(
            RenderSystemInstruction(persona),
            "Prepared Track-A context JSON: " + contextJson + "\n" +
            "Comment only on the supplied Track-A title and artist. No future Track B is known. " +
            "Use trustedListeningObservations only as explicitly supplied facts. " +
            "Avoid repeating the opening, joke structure, distinctive metaphor, or unusually similar wording " +
            "from recentAcceptedComments. Write the comment now.");
    }

    private static string RenderSystemInstruction(HostPersonaConfiguration persona)
    {
        string rendered = SystemInstructionTemplate.Value
            .Replace(HostNameToken, persona.HostName.Trim(), StringComparison.Ordinal)
            .Replace(ListenerNameToken, persona.ListenerName.Trim(), StringComparison.Ordinal);

        if (rendered.Contains(HostNameToken, StringComparison.Ordinal)
            || rendered.Contains(ListenerNameToken, StringComparison.Ordinal))
        {
            throw new InvalidOperationException("The Milestone 4 host-persona prompt has unresolved identity tokens.");
        }

        return rendered;
    }

    private static string LoadSystemInstructionTemplate()
    {
        Assembly assembly = typeof(CommentPromptFactory).Assembly;
        string? resourceName = assembly
            .GetManifestResourceNames()
            .SingleOrDefault(name => name.EndsWith(ResourceSuffix, StringComparison.Ordinal));

        if (resourceName is null)
        {
            throw new InvalidOperationException("The Milestone 4 host-persona prompt resource is missing.");
        }

        using Stream stream = assembly.GetManifestResourceStream(resourceName)
            ?? throw new InvalidOperationException("The Milestone 4 host-persona prompt could not be opened.");
        using var reader = new StreamReader(stream);
        return reader.ReadToEnd().Trim();
    }
}
