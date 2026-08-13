using System.Reflection;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace ZoyaRadio.MediaProbe;

internal sealed record CommentaryBenchmarkManifest(
    string ManifestVersion,
    int SamplesPerArm,
    IReadOnlyList<CommentaryBenchmarkScenario> Scenarios);

internal sealed record CommentaryBenchmarkScenario(
    string Id,
    int Version,
    string Description,
    string SubjectTitle,
    string SubjectArtist,
    CommentaryReason OrdinaryReason,
    IReadOnlyList<CommentaryTrack> PriorTracks,
    IReadOnlyList<string> RecentComments);

internal static class CommentaryBenchmarkManifestLoader
{
    private const string ResourceSuffix = "Prompts.Milestone9ABenchmarkScenarios.json";
    private static readonly JsonSerializerOptions Options = new()
    {
        PropertyNameCaseInsensitive = true,
        Converters = { new JsonStringEnumConverter() },
    };

    public static CommentaryBenchmarkManifest Load()
    {
        Assembly assembly = typeof(CommentaryBenchmarkManifestLoader).Assembly;
        string? resourceName = assembly
            .GetManifestResourceNames()
            .SingleOrDefault(name => name.EndsWith(ResourceSuffix, StringComparison.Ordinal));
        if (resourceName is null)
        {
            throw new InvalidOperationException("The Milestone 9A benchmark scenario manifest is missing.");
        }

        using Stream stream = assembly.GetManifestResourceStream(resourceName)
            ?? throw new InvalidOperationException("The Milestone 9A benchmark scenario manifest could not be opened.");
        CommentaryBenchmarkManifest manifest = JsonSerializer.Deserialize<CommentaryBenchmarkManifest>(stream, Options)
            ?? throw new InvalidOperationException("The Milestone 9A benchmark scenario manifest is empty.");
        Validate(manifest);
        return manifest;
    }

    internal static void Validate(CommentaryBenchmarkManifest manifest)
    {
        if (manifest.ManifestVersion != "1.0")
        {
            throw new InvalidOperationException("The M9A scenario manifest version must be 1.0.");
        }

        if (manifest.SamplesPerArm < 2)
        {
            throw new InvalidOperationException("The M9A benchmark requires at least two samples per arm.");
        }

        int requestCount = manifest.Scenarios.Count * manifest.SamplesPerArm * 2;
        if (requestCount > 60)
        {
            throw new InvalidOperationException("The M9A benchmark may not exceed 60 model requests.");
        }

        if (manifest.Scenarios.Count != 12)
        {
            throw new InvalidOperationException("The M9A version 1.0 manifest must contain exactly 12 scenarios.");
        }

        string[] ids = manifest.Scenarios.Select(scenario => scenario.Id).ToArray();
        if (ids.Distinct(StringComparer.Ordinal).Count() != ids.Length
            || ids.Any(id => string.IsNullOrWhiteSpace(id)))
        {
            throw new InvalidOperationException("M9A scenario IDs must be non-empty and unique.");
        }

        foreach (CommentaryBenchmarkScenario scenario in manifest.Scenarios)
        {
            if (scenario.Version != 1
                || string.IsNullOrWhiteSpace(scenario.Description)
                || string.IsNullOrWhiteSpace(scenario.SubjectTitle)
                || string.IsNullOrWhiteSpace(scenario.SubjectArtist))
            {
                throw new InvalidOperationException($"M9A scenario '{scenario.Id}' is incomplete.");
            }
        }
    }
}
