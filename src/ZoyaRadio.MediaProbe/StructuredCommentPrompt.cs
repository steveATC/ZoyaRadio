using System.Text.Encodings.Web;
using System.Reflection;
using System.Text.Json;
using System.Text.Unicode;

namespace ZoyaRadio.MediaProbe;

internal static class StructuredCommentPromptFactory
{
    private const string HostNameToken = "{{HOST_NAME}}";
    private const string ListenerNameToken = "{{LISTENER_NAME}}";
    private const string ResourceSuffix = "Prompts.Milestone9AStructuredCommentPrompt.txt";
    private static readonly Lazy<string> SystemInstructionTemplate = new(LoadSystemInstructionTemplate);
    private static readonly JsonSerializerOptions MetadataJsonOptions = new()
    {
        Encoder = JavaScriptEncoder.Create(UnicodeRanges.All),
    };


    public static CommentPrompt Create(CommentaryBrief brief, HostPersonaConfiguration persona)
    {
        persona.Validate();

        string trustedFacts = JsonSerializer.Serialize(new
        {
            subjectTrack = brief.SubjectTrack,
            previousTrack = brief.PreviousTrack,
            sessionFacts = brief.SessionFacts,
        }, MetadataJsonOptions);
        string recentContext = JsonSerializer.Serialize(new
        {
            recentAcceptedComments = brief.RecentAcceptedComments,
        }, MetadataJsonOptions);

        string request =
            "<TRUSTED_FACTS_JSON>\n" + trustedFacts + "\n</TRUSTED_FACTS_JSON>\n" +
            "<REQUESTED_RHETORICAL_FUNCTION>\n" + brief.PrimaryReason + "\n" +
            "Secondary observations: " +
            (brief.SecondaryReasons.Count == 0
                ? "none"
                : string.Join(", ", brief.SecondaryReasons)) + "\n" +
            "</REQUESTED_RHETORICAL_FUNCTION>\n" +
            "<RECENT_CONTEXT_JSON>\n" + recentContext + "\n</RECENT_CONTEXT_JSON>\n" +
            "Write the comment now.";

        return new CommentPrompt(RenderSystemInstruction(persona), request);
    }

    internal static string RenderSystemInstruction(HostPersonaConfiguration persona)
    {
        string rendered = SystemInstructionTemplate.Value
            .Replace(HostNameToken, persona.HostName.Trim(), StringComparison.Ordinal)
            .Replace(ListenerNameToken, persona.ListenerName.Trim(), StringComparison.Ordinal);

        if (rendered.Contains(HostNameToken, StringComparison.Ordinal)
            || rendered.Contains(ListenerNameToken, StringComparison.Ordinal))
        {
            throw new InvalidOperationException("The Milestone 9A candidate prompt has unresolved identity tokens.");
        }

        return rendered;
    }

    private static string LoadSystemInstructionTemplate()
    {
        Assembly assembly = typeof(StructuredCommentPromptFactory).Assembly;
        string? resourceName = assembly
            .GetManifestResourceNames()
            .SingleOrDefault(name => name.EndsWith(ResourceSuffix, StringComparison.Ordinal));

        if (resourceName is null)
        {
            throw new InvalidOperationException("The Milestone 9A candidate prompt resource is missing.");
        }

        using Stream stream = assembly.GetManifestResourceStream(resourceName)
            ?? throw new InvalidOperationException("The Milestone 9A candidate prompt could not be opened.");
        using var reader = new StreamReader(stream);
        return reader.ReadToEnd().Trim();
    }
}
