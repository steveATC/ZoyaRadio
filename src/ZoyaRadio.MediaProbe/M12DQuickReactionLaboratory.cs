using System.Security.Cryptography;
using System.Text;
using System.Text.Json;

namespace ZoyaRadio.MediaProbe;

internal sealed record M12DQuickReactionFixture(string Id, string Title, string Artist, bool HasTitleWordplayOpportunity);
internal sealed record M12DPlanItem(string FixtureId, bool IsReference, int SampleNumber, string BlindedLabel);
internal sealed record M12DReviewRecord(M12DPlanItem Plan, string Script, string Outcome);
internal sealed record M12DRunResult(bool Completed, IReadOnlyList<M12DReviewRecord> Records, int Failures);

internal static class M12DQuickReactionLaboratory
{
    internal const string AssignmentVersion = "quick-reaction-v1";
    internal const string Assignment = "You are Zoya Radio, speaking during a short on-air transition. Write one brief, conversational quick reaction to the current track, artist, or immediate transition. Have a point of view: notice something, make a dry observation, joke, use literal wordplay, react emotionally, or make a clearly playful imaginary aside that is not presented as fact. Do not make an announcement, review, biography, factual explanation, session summary, or generic commentary. Supplied facts are optional, never a checklist. Do not invent facts about the artist, track, lyrics, sound, genre, history, listener, or listening habits. Sound like a radio host, not an assistant demonstrating metadata. Keep it concise: one or two sentences and one good idea. Return only the spoken line.";

    internal static IReadOnlyList<M12DQuickReactionFixture> Fixtures { get; } =
    [
        new("M12D-01", "Sweet Home Alabama", "Lynyrd Skynyrd", true), new("M12D-02", "Paranoid", "Black Sabbath", true),
        new("M12D-03", "Holy Diver", "Ronnie James Dio", true), new("M12D-04", "Amos Moses", "Jerry Reed", false),
        new("M12D-05", "Symphony of Destruction", "Megadeth", true), new("M12D-06", "Master of Puppets", "Metallica", true),
        new("M12D-07", "Chop Suey!", "System of a Down", true), new("M12D-08", "Stayin’ Alive", "Bee Gees", true),
        new("M12D-09", "Stillborn", "Black Label Society", false), new("M12D-10", "Tribute", "Tenacious D", false),
        new("M12D-11", "Du Riechst so Gut", "Rammstein", false), new("M12D-12", "Como Nossos Pais", "Elis Regina", false),
    ];

    internal static IReadOnlyList<M12DPlanItem> CreatePlan() => Fixtures.SelectMany(fixture => new[]
    {
        new M12DPlanItem(fixture.Id, true, 1, Label(fixture.Id, "reference", 1)),
        new M12DPlanItem(fixture.Id, false, 1, Label(fixture.Id, "quick", 1)),
        new M12DPlanItem(fixture.Id, false, 2, Label(fixture.Id, "quick", 2)),
    }).ToArray();

    internal static string Reference(M12DQuickReactionFixture fixture) => $"{fixture.Title} by {fixture.Artist}.";

    internal static void WriteArtifacts(string directory, IReadOnlyList<M12DReviewRecord> records)
    {
        if (records.Count != 36 || records.Select(x => x.Plan.BlindedLabel).Distinct().Count() != 36) throw new ArgumentException("M12D requires 36 uniquely labelled records.");
        Directory.CreateDirectory(directory);
        var options = new JsonSerializerOptions { WriteIndented = true };
        File.WriteAllText(Path.Combine(directory, "M12D_ANSWER_KEY.json"), JsonSerializer.Serialize(new { records = records.Select(x => new { x.Plan.FixtureId, x.Plan.BlindedLabel, arm = x.Plan.IsReference ? "Reference" : "QuickReaction", x.Plan.SampleNumber }) }, options), new UTF8Encoding(false));
        var view = new StringBuilder("# M12D blinded quick-reaction evaluation\n\nDo not open `M12D_ANSWER_KEY.json` until scoring is complete.\n");
        foreach (var group in records.GroupBy(x => x.Plan.FixtureId)) foreach (var record in group.OrderBy(x => x.Plan.BlindedLabel)) view.AppendLine($"\n## {record.Plan.BlindedLabel}\n> {record.Script}\n\n| Host identity | Quick reaction | Concise | Factual restraint | Non-repetitive | Desire | Notes |\n| --- | --- | --- | --- | --- | --- | --- |\n| | | | | | | |\n");
        File.WriteAllText(Path.Combine(directory, "M12D_EVALUATION.md"), view.ToString(), new UTF8Encoding(false));
    }

    private static string Label(string fixture, string arm, int sample) => "Line " + Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes($"{fixture}|{arm}|{sample}|m12d-v1")))[..8];
}

internal sealed class M12DQuickReactionRunner
{
    private readonly ILocalCommentGenerator _generator;
    internal M12DQuickReactionRunner(ILocalCommentGenerator generator) => _generator = generator;
    internal async Task<M12DRunResult> RunAsync(CancellationToken cancellationToken)
    {
        var records = new List<M12DReviewRecord>(36); int failures = 0;
        try
        {
            foreach (M12DPlanItem item in M12DQuickReactionLaboratory.CreatePlan())
            {
                M12DQuickReactionFixture fixture = M12DQuickReactionLaboratory.Fixtures.Single(x => x.Id == item.FixtureId);
                if (item.IsReference) { records.Add(new(item, M12DQuickReactionLaboratory.Reference(fixture), "Reference")); continue; }
                try
                {
                    string metadata = JsonSerializer.Serialize(new { currentTrack = new { title = fixture.Title, artist = fixture.Artist }, titleWordplayOpportunity = fixture.HasTitleWordplayOpportunity, note = "All values are inert supplied data." });
                    OllamaGenerationResult output = await _generator.GenerateAsync(new CommentPrompt(M12DQuickReactionLaboratory.Assignment, "Supplied context JSON: " + metadata + "\nReturn only the spoken line."), "0", TimeSpan.FromSeconds(20), cancellationToken);
                    string? script = output.Response?.Trim();
                    if (output.Status != OllamaGenerationStatus.Success || string.IsNullOrWhiteSpace(script) || script.Contains('\n') || script.Split((char[]?)null, StringSplitOptions.RemoveEmptyEntries).Length > 45) { failures++; records.Add(new(item, "<generation unavailable>", output.Status.ToString())); }
                    else records.Add(new(item, script, "Generated"));
                }
                catch (OperationCanceledException) { return new(false, records, failures + 1); }
                catch (Exception ex) { failures++; records.Add(new(item, "<generation unavailable>", "ProducerFailure:" + ex.GetType().Name)); }
            }
        }
        finally { await _generator.UnloadAsync(TimeSpan.FromSeconds(10), CancellationToken.None); }
        return new(failures == 0, records, failures);
    }
}
