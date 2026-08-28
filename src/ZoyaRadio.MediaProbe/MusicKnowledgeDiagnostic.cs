using System.Text;

namespace ZoyaRadio.MediaProbe;

internal static class MusicKnowledgeFixtureReport
{
    internal static string Create(string fixtureName, IEnumerable<MusicKnowledgeFactResolution> resolutions)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(fixtureName);
        ArgumentNullException.ThrowIfNull(resolutions);
        var builder = new StringBuilder();
        builder.AppendLine("# M13A-I1 Music Knowledge Fixture Report");
        builder.AppendLine();
        builder.AppendLine("- Fixture: " + fixtureName);
        builder.AppendLine("- Mode: recorded fixtures and fake HTTP responses only");
        builder.AppendLine("- Provider requests: none");
        builder.AppendLine("- Runtime integration: none");
        foreach (MusicKnowledgeFactResolution resolution in resolutions
            .OrderBy(item => item.FactType, StringComparer.Ordinal)
            .ThenBy(item => item.EntityId, StringComparer.Ordinal))
        {
            builder.AppendLine();
            builder.AppendLine("## " + resolution.FactType + " / " + resolution.EntityId);
            builder.AppendLine("- Outcome: " + resolution.Outcome);
            builder.AppendLine("- Detail: " + resolution.Detail);
            foreach (MusicKnowledgeFact fact in resolution.Facts.OrderBy(item => item.Value, StringComparer.Ordinal))
            {
                builder.AppendLine("- Evidence: " + fact.Value + " [" + fact.Provenance.Source + ":" + fact.Provenance.SourceRecordId + "]");
            }
        }
        return builder.ToString();
    }
}
