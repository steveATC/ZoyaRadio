using System.Diagnostics;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;

namespace ZoyaRadio.MediaProbe;

internal sealed record M12DVarietyPlaylistFixture(
    string Sequence,
    int Position,
    string Title,
    string Artist);

internal sealed record M12DVarietyPlaylistPlanItem(
    M12DVarietyPlaylistFixture Fixture,
    string NeutralLabel);

internal sealed record M12DVarietyPlaylistAttempt(
    M12DVarietyPlaylistPlanItem Plan,
    string Output,
    string Status,
    long LatencyMilliseconds,
    bool ValidationPassed,
    string ValidationOutcome,
    IReadOnlyList<string> HistoryLabelsSupplied,
    int HistoryCountBefore,
    int HistoryCountAfter,
    string? Failure);

internal sealed record M12DVarietyPlaylistRunResult(
    bool Completed,
    int Attempts,
    int Failures,
    int Cancellations,
    IReadOnlyList<M12DVarietyPlaylistAttempt> AttemptsDetail,
    OllamaOperationResult UnloadResult);

internal static class M12DVarietyPlaylistLaboratory
{
    internal const string Cedar = "Cedar";
    internal const string Lark = "Lark";

    internal static IReadOnlyList<M12DVarietyPlaylistFixture> Fixtures { get; } =
    [
        new(Cedar, 1, "Paranoid", "Black Sabbath"),
        new(Cedar, 2, "Como Nossos Pais", "Elis Regina"),
        new(Cedar, 3, "Chop Suey!", "System of a Down"),
        new(Cedar, 4, "Du Riechst so Gut", "Rammstein"),
        new(Cedar, 5, "Sweet Home Alabama", "Lynyrd Skynyrd"),
        new(Cedar, 6, "Symphony of Destruction", "Megadeth"),
        new(Cedar, 7, "Stayin’ Alive", "Bee Gees"),
        new(Cedar, 8, "Amos Moses", "Jerry Reed"),
        new(Cedar, 9, "Master of Puppets", "Metallica"),
        new(Cedar, 10, "Tribute", "Tenacious D"),
        new(Cedar, 11, "Holy Diver", "Ronnie James Dio"),
        new(Cedar, 12, "Stillborn", "Black Label Society"),
        new(Lark, 1, "Stillborn", "Black Label Society"),
        new(Lark, 2, "Holy Diver", "Ronnie James Dio"),
        new(Lark, 3, "Tribute", "Tenacious D"),
        new(Lark, 4, "Master of Puppets", "Metallica"),
        new(Lark, 5, "Amos Moses", "Jerry Reed"),
        new(Lark, 6, "Stayin’ Alive", "Bee Gees"),
        new(Lark, 7, "Symphony of Destruction", "Megadeth"),
        new(Lark, 8, "Sweet Home Alabama", "Lynyrd Skynyrd"),
        new(Lark, 9, "Du Riechst so Gut", "Rammstein"),
        new(Lark, 10, "Chop Suey!", "System of a Down"),
        new(Lark, 11, "Como Nossos Pais", "Elis Regina"),
        new(Lark, 12, "Paranoid", "Black Sabbath"),
    ];

    internal static IReadOnlyList<M12DVarietyPlaylistPlanItem> CreatePlan() =>
        Fixtures.Select(fixture => new M12DVarietyPlaylistPlanItem(
            fixture,
            Label(fixture))).ToArray();

    internal static CommentPrompt CreatePrompt(
        M12DVarietyPlaylistFixture fixture,
        IReadOnlyList<string> acceptedHistory)
    {
        var context = new RadioPreparationContext(
            new MediaSnapshot(fixture.Title, fixture.Artist, null),
            DateTimeOffset.UnixEpoch,
            [],
            acceptedHistory);
        return QuickReactionPilotPrompt.Create(context);
    }

    internal static void WriteArtifacts(string directory, M12DVarietyPlaylistRunResult result)
    {
        if (result.Attempts != 24
            || result.AttemptsDetail.Count != 24
            || result.AttemptsDetail.Select(attempt => attempt.Plan.NeutralLabel).Distinct().Count() != 24)
        {
            throw new ArgumentException(
                "M12D VAR1 requires exactly 24 uniquely labelled attempts.",
                nameof(result));
        }

        Directory.CreateDirectory(directory);
        var options = new JsonSerializerOptions { WriteIndented = true };

        var answerKey = new
        {
            records = result.AttemptsDetail.Select(attempt => new
            {
                label = attempt.Plan.NeutralLabel,
                sequence = attempt.Plan.Fixture.Sequence,
                position = attempt.Plan.Fixture.Position,
                title = attempt.Plan.Fixture.Title,
                artist = attempt.Plan.Fixture.Artist,
                sampleIdentity = $"{attempt.Plan.Fixture.Sequence}-{attempt.Plan.Fixture.Position:D2}",
                acceptedHistoryLabelsSupplied = attempt.HistoryLabelsSupplied,
            }),
        };
        File.WriteAllText(
            Path.Combine(directory, "M12D_VAR1_ANSWER_KEY.json"),
            JsonSerializer.Serialize(answerKey, options),
            new UTF8Encoding(false));

        var evaluation = new StringBuilder(
            "# M12D VAR1 simulated-playlist evaluation\n\n" +
            "Evaluation evidence only. Do not use this artifact as runtime history.\n");
        foreach (string sequence in new[] { Cedar, Lark })
        {
            evaluation.AppendLine();
            evaluation.AppendLine($"## {sequence}");
            foreach (M12DVarietyPlaylistAttempt attempt in result.AttemptsDetail
                         .Where(item => item.Plan.Fixture.Sequence == sequence)
                         .OrderBy(item => item.Plan.Fixture.Position))
            {
                evaluation.AppendLine();
                evaluation.AppendLine($"### {attempt.Plan.Fixture.Position}. {attempt.Plan.Fixture.Title} — {attempt.Plan.Fixture.Artist}");
                evaluation.AppendLine($"Label: {attempt.Plan.NeutralLabel}");
                evaluation.AppendLine($"Output: {attempt.Output}");
                evaluation.AppendLine($"Status: {attempt.Status}");
                evaluation.AppendLine();
                evaluation.AppendLine("| Relevance | Host identity | Naturalness | Concision | Factual restraint | Prior-topic carryover | Repetition versus preceding three | Distinctive form label | Desire | Notes |");
                evaluation.AppendLine("| --- | --- | --- | --- | --- | --- | --- | --- | --- | --- |");
                evaluation.AppendLine("| | | | | | | | | | |");
            }
        }

        File.WriteAllText(
            Path.Combine(directory, "M12D_VAR1_EVALUATION.md"),
            evaluation.ToString(),
            new UTF8Encoding(false));

        var summary = new
        {
            attempts = result.Attempts,
            completed = result.Completed,
            failures = result.Failures,
            cancellations = result.Cancellations,
            unload = new
            {
                attempted = true,
                success = result.UnloadResult.Success,
                message = result.UnloadResult.Message,
            },
            records = result.AttemptsDetail.Select(attempt => new
            {
                label = attempt.Plan.NeutralLabel,
                sequence = attempt.Plan.Fixture.Sequence,
                position = attempt.Plan.Fixture.Position,
                status = attempt.Status,
                latencyMilliseconds = attempt.LatencyMilliseconds,
                validationPassed = attempt.ValidationPassed,
                validationOutcome = attempt.ValidationOutcome,
                historyCountBefore = attempt.HistoryCountBefore,
                historyCountAfter = attempt.HistoryCountAfter,
                failure = attempt.Failure,
            }),
        };
        File.WriteAllText(
            Path.Combine(directory, "M12D_VAR1_RUN_SUMMARY.json"),
            JsonSerializer.Serialize(summary, options),
            new UTF8Encoding(false));
    }

    private static string Label(M12DVarietyPlaylistFixture fixture)
    {
        string identity = $"m12d-var1|{fixture.Sequence}|{fixture.Position}|{fixture.Title}|{fixture.Artist}";
        return "Sample " + Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(identity)))[..10];
    }
}

internal sealed class M12DVarietyPlaylistRunner
{
    private readonly ILocalCommentGenerator _generator;
    private readonly AiCommentConfiguration _configuration;

    internal M12DVarietyPlaylistRunner(
        ILocalCommentGenerator generator,
        AiCommentConfiguration? configuration = null)
    {
        _generator = generator;
        _configuration = configuration ?? AiCommentConfiguration.Default;
    }

    internal async Task<M12DVarietyPlaylistRunResult> RunAsync(CancellationToken cancellationToken)
    {
        var attempts = new List<M12DVarietyPlaylistAttempt>(24);
        var histories = new Dictionary<string, List<(string Label, string SpokenLine)>>
        {
            [M12DVarietyPlaylistLaboratory.Cedar] = [],
            [M12DVarietyPlaylistLaboratory.Lark] = [],
        };
        int failures = 0;
        int cancellations = 0;
        bool cancelled = false;
        OllamaOperationResult unloadResult;

        try
        {
            foreach (M12DVarietyPlaylistPlanItem item in M12DVarietyPlaylistLaboratory.CreatePlan())
            {
                List<(string Label, string SpokenLine)> history = histories[item.Fixture.Sequence];
                string[] suppliedLabels = history.Select(entry => entry.Label).ToArray();
                string[] suppliedLines = history.Select(entry => entry.SpokenLine).ToArray();
                Stopwatch stopwatch = Stopwatch.StartNew();

                try
                {
                    OllamaGenerationResult generation = await _generator.GenerateAsync(
                        M12DVarietyPlaylistLaboratory.CreatePrompt(item.Fixture, suppliedLines),
                        _configuration.NormalKeepAlive,
                        _configuration.GenerationTimeout,
                        cancellationToken);
                    stopwatch.Stop();

                    string spoken = QuickReactionPilotPrompt.NormalizeForSpeech(generation.Response);
                    CommentValidationResult validation = CommentValidator.Validate(spoken);
                    bool fallback = generation.Status == OllamaGenerationStatus.Success
                        && CommentValidator.IsFallbackSignal(spoken);
                    bool accepted = generation.Status == OllamaGenerationStatus.Success
                        && !fallback
                        && validation.IsValid;
                    string status = accepted
                        ? "Accepted"
                        : fallback
                            ? "Fallback"
                            : generation.Status == OllamaGenerationStatus.Success
                                ? "Rejected"
                                : generation.Status.ToString();
                    string output = accepted ? validation.NormalizedText : Marker(status);
                    string? failure = accepted
                        ? null
                        : generation.ErrorMessage ?? (fallback
                            ? "The safe fallback signal is not simulated accepted history."
                            : validation.Reason);

                    if (!accepted)
                    {
                        failures++;
                        if (status == "Cancelled")
                        {
                            cancellations++;
                        }
                    }
                    else
                    {
                        history.Add((item.NeutralLabel, validation.NormalizedText));
                        if (history.Count > 3)
                        {
                            history.RemoveAt(0);
                        }
                    }

                    attempts.Add(new(
                        item,
                        output,
                        status,
                        stopwatch.ElapsedMilliseconds,
                        validation.IsValid,
                        validation.Reason,
                        suppliedLabels,
                        suppliedLabels.Length,
                        history.Count,
                        failure));

                    if (status == "Cancelled" && cancellationToken.IsCancellationRequested)
                    {
                        cancelled = true;
                        break;
                    }
                }
                catch (OperationCanceledException exception)
                {
                    stopwatch.Stop();
                    failures++;
                    cancellations++;
                    attempts.Add(new(
                        item,
                        Marker("Cancelled"),
                        "Cancelled",
                        stopwatch.ElapsedMilliseconds,
                        false,
                        "Generation cancelled before mechanical validation.",
                        suppliedLabels,
                        suppliedLabels.Length,
                        history.Count,
                        exception.Message));
                    if (cancellationToken.IsCancellationRequested)
                    {
                        cancelled = true;
                        break;
                    }
                }
                catch (Exception exception)
                {
                    stopwatch.Stop();
                    failures++;
                    attempts.Add(new(
                        item,
                        Marker("ProducerFailure"),
                        "ProducerFailure",
                        stopwatch.ElapsedMilliseconds,
                        false,
                        "Generation failed before mechanical validation.",
                        suppliedLabels,
                        suppliedLabels.Length,
                        history.Count,
                        exception.Message));
                }
            }
        }
        finally
        {
            try
            {
                unloadResult = await _generator.UnloadAsync(
                    _configuration.LocalOperationTimeout,
                    CancellationToken.None);
            }
            catch (Exception exception)
            {
                unloadResult = new(false, $"Unload failed: {exception.Message}");
            }
        }

        return new(
            !cancelled && failures == 0 && unloadResult.Success,
            attempts.Count,
            failures + (unloadResult.Success ? 0 : 1),
            cancellations,
            attempts,
            unloadResult);
    }

    private static string Marker(string status) => status switch
    {
        "Cancelled" => "<generation cancelled>",
        _ => "<generation unavailable>",
    };
}
