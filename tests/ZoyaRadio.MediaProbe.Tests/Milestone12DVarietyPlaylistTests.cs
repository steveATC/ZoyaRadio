using System.Text;
using ZoyaRadio.MediaProbe;

internal static class Milestone12DVarietyPlaylistTests
{
    public static void FixturesAreTwoExactUnicodePlaylists()
    {
        (string Title, string Artist)[] cedar =
        [
            ("Paranoid", "Black Sabbath"), ("Como Nossos Pais", "Elis Regina"),
            ("Chop Suey!", "System of a Down"), ("Du Riechst so Gut", "Rammstein"),
            ("Sweet Home Alabama", "Lynyrd Skynyrd"), ("Symphony of Destruction", "Megadeth"),
            ("Stayin’ Alive", "Bee Gees"), ("Amos Moses", "Jerry Reed"),
            ("Master of Puppets", "Metallica"), ("Tribute", "Tenacious D"),
            ("Holy Diver", "Ronnie James Dio"), ("Stillborn", "Black Label Society"),
        ];
        (string Title, string Artist)[] lark =
        [
            ("Stillborn", "Black Label Society"), ("Holy Diver", "Ronnie James Dio"),
            ("Tribute", "Tenacious D"), ("Master of Puppets", "Metallica"),
            ("Amos Moses", "Jerry Reed"), ("Stayin’ Alive", "Bee Gees"),
            ("Symphony of Destruction", "Megadeth"), ("Sweet Home Alabama", "Lynyrd Skynyrd"),
            ("Du Riechst so Gut", "Rammstein"), ("Chop Suey!", "System of a Down"),
            ("Como Nossos Pais", "Elis Regina"), ("Paranoid", "Black Sabbath"),
        ];

        AssertSequence(M12DVarietyPlaylistLaboratory.Cedar, cedar);
        AssertSequence(M12DVarietyPlaylistLaboratory.Lark, lark);
        TestAssert.Equal(24, M12DVarietyPlaylistLaboratory.Fixtures.Count);
        TestAssert.Equal(2, M12DVarietyPlaylistLaboratory.Fixtures.Select(x => x.Sequence).Distinct().Count());
    }

    public static void PlanHasExactlyTwentyFourUniqueNeutralItems()
    {
        IReadOnlyList<M12DVarietyPlaylistPlanItem> plan = M12DVarietyPlaylistLaboratory.CreatePlan();
        TestAssert.Equal(24, plan.Count);
        TestAssert.Equal(24, plan.Select(item => item.NeutralLabel).Distinct().Count());
        TestAssert.Equal(12, plan.Count(item => item.Fixture.Sequence == M12DVarietyPlaylistLaboratory.Cedar));
        TestAssert.Equal(12, plan.Count(item => item.Fixture.Sequence == M12DVarietyPlaylistLaboratory.Lark));

        foreach (string sequence in new[]
                 { M12DVarietyPlaylistLaboratory.Cedar, M12DVarietyPlaylistLaboratory.Lark })
        {
            M12DVarietyPlaylistFixture[] fixtures = plan
                .Where(item => item.Fixture.Sequence == sequence)
                .Select(item => item.Fixture)
                .ToArray();
            TestAssert.Equal(12, fixtures.Select(fixture => (fixture.Title, fixture.Artist)).Distinct().Count());
            for (int index = 0; index < fixtures.Length; index++)
            {
                TestAssert.Equal(index + 1, fixtures[index].Position);
            }
        }
    }

    public static async Task RunnerUsesCurrentD32PromptExactlyOncePerTrackAndUnloadsOnce()
    {
        var fake = new FakeVarietyPlaylistGenerator();
        M12DVarietyPlaylistRunResult result = await new M12DVarietyPlaylistRunner(fake).RunAsync(CancellationToken.None);

        TestAssert.True(result.Completed);
        TestAssert.Equal(24, result.Attempts);
        TestAssert.Equal(24, fake.GenerateCalls);
        TestAssert.Equal(1, fake.UnloadCalls);
        TestAssert.Equal(0, result.Failures);
        TestAssert.Equal(0, result.Cancellations);
        TestAssert.Equal(AiCommentConfiguration.Default.NormalKeepAlive, fake.KeepAlives.Distinct().Single());
        TestAssert.Equal(AiCommentConfiguration.Default.GenerationTimeout, fake.GenerationTimeouts.Distinct().Single());
        TestAssert.True(fake.Prompts.All(prompt => prompt.SystemInstruction == QuickReactionPilotPrompt.Assignment));
        TestAssert.True(fake.Prompts.All(prompt => prompt.MetadataPrompt.Contains("CURRENT SUBJECT JSON", StringComparison.Ordinal)));
        TestAssert.False(fake.Prompts.Any(prompt => prompt.SystemInstruction.Contains("quick-reaction-v2-variety-spoken-text", StringComparison.Ordinal)));
    }

    public static async Task AcceptedOnlyHistoryIsBoundedOrderedAndSequenceIsolated()
    {
        var fake = new FakeVarietyPlaylistGenerator();
        fake.Actions.Enqueue(_ => Success("**Cedar one has arrived with a suitcase full of confidence, and frankly the furniture should brace itself.** ✨"));
        fake.Actions.Enqueue(_ => Success("Too short."));
        fake.Actions.Enqueue(_ => Success("FALLBACK"));
        fake.Actions.Enqueue(_ => new OllamaGenerationResult(OllamaGenerationStatus.ConnectionFailure, null, "fake unavailable", null));
        fake.Actions.Enqueue(_ => Success("Cedar five has arrived with enough confidence to rearrange the furniture, and frankly the wallpaper applauds politely."));
        fake.Actions.Enqueue(_ => Success("Cedar six enters wearing a tiny crown, and nobody has the paperwork to question that decision today."));
        fake.Actions.Enqueue(_ => Success("Cedar seven has made a dramatic entrance, and the coffee is already pretending it saw nothing unusual."));
        fake.Actions.Enqueue(_ => Success("Cedar eight has brought a weather report for imaginary planets, and somehow the forecast feels deeply official."));

        M12DVarietyPlaylistRunResult result = await new M12DVarietyPlaylistRunner(fake).RunAsync(CancellationToken.None);

        TestAssert.Equal(24, result.Attempts);
        TestAssert.Equal(3, result.Failures);
        TestAssert.Equal("Accepted", result.AttemptsDetail[0].Status);
        TestAssert.Equal(0, result.AttemptsDetail[0].HistoryCountBefore);
        TestAssert.Equal(1, result.AttemptsDetail[0].HistoryCountAfter);
        TestAssert.Equal("Rejected", result.AttemptsDetail[1].Status);
        TestAssert.Equal("Fallback", result.AttemptsDetail[2].Status);
        TestAssert.Equal("ConnectionFailure", result.AttemptsDetail[3].Status);
        TestAssert.Equal("<generation unavailable>", result.AttemptsDetail[1].Output);
        TestAssert.Equal(1, result.AttemptsDetail[3].HistoryCountBefore);
        TestAssert.Equal(1, result.AttemptsDetail[3].HistoryCountAfter);

        M12DVarietyPlaylistAttempt eighthCedar = result.AttemptsDetail[7];
        TestAssert.Equal(3, eighthCedar.HistoryCountBefore);
        TestAssert.Equal(3, eighthCedar.HistoryCountAfter);
        TestAssert.Equal(
            result.AttemptsDetail[4].Plan.NeutralLabel,
            eighthCedar.HistoryLabelsSupplied[0]);
        TestAssert.Equal(
            result.AttemptsDetail[6].Plan.NeutralLabel,
            eighthCedar.HistoryLabelsSupplied[2]);

        M12DVarietyPlaylistAttempt firstLark = result.AttemptsDetail[12];
        TestAssert.Equal(0, firstLark.HistoryCountBefore);
        TestAssert.Equal(0, firstLark.HistoryLabelsSupplied.Count);
        TestAssert.Equal("Stillborn", firstLark.Plan.Fixture.Title);
    }

    public static async Task FailedAndCancelledOutputNeverEntersHistoryAndNeverRetries()
    {
        var failed = new FakeVarietyPlaylistGenerator();
        failed.Actions.Enqueue(_ => new OllamaGenerationResult(OllamaGenerationStatus.TimedOut, null, "fake timeout", null));
        failed.Actions.Enqueue(_ => throw new InvalidOperationException("fake producer failure"));
        M12DVarietyPlaylistRunResult failureResult = await new M12DVarietyPlaylistRunner(failed).RunAsync(CancellationToken.None);

        TestAssert.False(failureResult.Completed);
        TestAssert.Equal(24, failureResult.Attempts);
        TestAssert.Equal(2, failureResult.Failures);
        TestAssert.Equal(24, failed.GenerateCalls);
        TestAssert.Equal(1, failed.UnloadCalls);
        TestAssert.Equal(0, failureResult.AttemptsDetail[0].HistoryCountAfter);
        TestAssert.Equal(0, failureResult.AttemptsDetail[1].HistoryCountBefore);
        TestAssert.Equal("ProducerFailure", failureResult.AttemptsDetail[1].Status);

        using var cancellation = new CancellationTokenSource();
        cancellation.Cancel();
        var cancelled = new FakeVarietyPlaylistGenerator();
        M12DVarietyPlaylistRunResult cancellationResult = await new M12DVarietyPlaylistRunner(cancelled)
            .RunAsync(cancellation.Token);

        TestAssert.False(cancellationResult.Completed);
        TestAssert.Equal(1, cancellationResult.Attempts);
        TestAssert.Equal(1, cancellationResult.Failures);
        TestAssert.Equal(1, cancellationResult.Cancellations);
        TestAssert.Equal(1, cancelled.GenerateCalls);
        TestAssert.Equal(1, cancelled.UnloadCalls);
        TestAssert.Equal("Cancelled", cancellationResult.AttemptsDetail[0].Status);
        TestAssert.Equal(0, cancellationResult.AttemptsDetail[0].HistoryCountAfter);
    }

    public static async Task ArtifactsKeepEvaluationAnswerKeyAndSummarySeparate()
    {
        var fake = new FakeVarietyPlaylistGenerator();
        M12DVarietyPlaylistRunResult result = await new M12DVarietyPlaylistRunner(fake).RunAsync(CancellationToken.None);
        string directory = Path.Combine(
            RepositoryPathLocator.FindRepositoryRoot(),
            "artifacts",
            "m12d-var1",
            DateTimeOffset.UtcNow.ToString("yyyyMMdd-HHmmss-fff"));
        try
        {
            M12DVarietyPlaylistLaboratory.WriteArtifacts(directory, result);
            string evaluation = File.ReadAllText(Path.Combine(directory, "M12D_VAR1_EVALUATION.md"), Encoding.UTF8);
            string answerKey = File.ReadAllText(Path.Combine(directory, "M12D_VAR1_ANSWER_KEY.json"), Encoding.UTF8);
            string summary = File.ReadAllText(Path.Combine(directory, "M12D_VAR1_RUN_SUMMARY.json"), Encoding.UTF8);

            TestAssert.Contains(evaluation, "## Cedar");
            TestAssert.Contains(evaluation, "## Lark");
            TestAssert.Contains(evaluation, "1. Paranoid — Black Sabbath");
            TestAssert.Contains(evaluation, "1. Stillborn — Black Label Society");
            TestAssert.Contains(evaluation, "Prior-topic carryover");
            TestAssert.Contains(evaluation, "Repetition versus preceding three");
            TestAssert.Contains(evaluation, "Distinctive form label");
            TestAssert.Contains(answerKey, "\"sampleIdentity\": \"Cedar-01\"");
            TestAssert.Contains(answerKey, "\"acceptedHistoryLabelsSupplied\"");
            TestAssert.Contains(summary, "\"attempts\": 24");
            TestAssert.Contains(summary, "\"latencyMilliseconds\"");
            TestAssert.Contains(summary, "\"historyCountBefore\"");
            TestAssert.Contains(summary, "\"cancellations\": 0");
            TestAssert.Contains(summary, "\"unload\"");
        }
        finally
        {
            if (Directory.Exists(directory))
            {
                Directory.Delete(directory, true);
            }
        }
    }

    public static void ToolingHasNoRuntimeOrExternalMediaDependency()
    {
        string root = RepositoryPathLocator.FindRepositoryRoot();
        string source = File.ReadAllText(
                Path.Combine(root, "src", "ZoyaRadio.MediaProbe", "M12DVarietyPlaylistLaboratory.cs"),
                Encoding.UTF8)
            + File.ReadAllText(
                Path.Combine(root, "src", "ZoyaRadio.MediaProbe", "M12DVarietyPlaylistCommand.cs"),
                Encoding.UTF8)
            + File.ReadAllText(
                Path.Combine(root, "run-m12d-var1-simulated-playlist.cmd"),
                Encoding.UTF8);

        foreach (string forbidden in new[]
        {
            "Windows.Media",
            "CoreAudio",
            "GlobalSystemMediaTransportControlsSession",
            "RadioBehavior",
            "TransitionRadioPipeline",
            "IAnnouncementSpeechService",
            "HttpClient",
            "M12DD32Comparison",
            "M12DQuickReactionLaboratory",
        })
        {
            TestAssert.False(source.Contains(forbidden, StringComparison.Ordinal));
        }

        TestAssert.Contains(source, "GENERATE ONLY");
        TestAssert.Contains(source, "QuickReactionPilotPrompt.Create");
        TestAssert.Contains(source, "AiCommentConfiguration.Default");
        TestAssert.Contains(source, "Console.CancelKeyPress");
    }

    private static void AssertSequence(
        string sequence,
        IReadOnlyList<(string Title, string Artist)> expected)
    {
        M12DVarietyPlaylistFixture[] actual = M12DVarietyPlaylistLaboratory.Fixtures
            .Where(fixture => fixture.Sequence == sequence)
            .ToArray();
        TestAssert.Equal(12, actual.Length);
        for (int index = 0; index < expected.Count; index++)
        {
            TestAssert.Equal(index + 1, actual[index].Position);
            TestAssert.Equal(expected[index].Title, actual[index].Title);
            TestAssert.Equal(expected[index].Artist, actual[index].Artist);
        }
    }

    private static OllamaGenerationResult Success(string text) => new(
        OllamaGenerationStatus.Success,
        text,
        null,
        null);

    private sealed class FakeVarietyPlaylistGenerator : ILocalCommentGenerator
    {
        internal List<CommentPrompt> Prompts { get; } = [];
        internal List<string> KeepAlives { get; } = [];
        internal List<TimeSpan> GenerationTimeouts { get; } = [];
        internal Queue<Func<CancellationToken, OllamaGenerationResult>> Actions { get; } = [];
        internal int GenerateCalls { get; private set; }
        internal int UnloadCalls { get; private set; }

        public string ModelName => "qwen3:4b-instruct";
        public Uri Endpoint => new("http://localhost:11434/api/");

        public Task<OllamaGenerationResult> GenerateAsync(
            CommentPrompt prompt,
            string keepAlive,
            TimeSpan timeout,
            CancellationToken cancellationToken)
        {
            GenerateCalls++;
            Prompts.Add(prompt);
            KeepAlives.Add(keepAlive);
            GenerationTimeouts.Add(timeout);
            if (Actions.Count == 0)
            {
                cancellationToken.ThrowIfCancellationRequested();
                return Task.FromResult(Success(
                    "That title has arrived with enough confidence to rearrange the furniture, Steve, and frankly I admire the paperwork-free commitment."));
            }

            return Task.FromResult(Actions.Dequeue()(cancellationToken));
        }

        public Task<OllamaOperationResult> UnloadAsync(
            TimeSpan timeout,
            CancellationToken cancellationToken)
        {
            UnloadCalls++;
            return Task.FromResult(new OllamaOperationResult(true, "fake selected-model unload succeeded"));
        }

        public Task<OllamaRunningModelsResult> GetRunningModelsAsync(
            TimeSpan timeout,
            CancellationToken cancellationToken) =>
            Task.FromResult(new OllamaRunningModelsResult(true, [], "not used"));

        public Task<OllamaInstalledModelsResult> GetInstalledModelsAsync(
            TimeSpan timeout,
            CancellationToken cancellationToken) =>
            Task.FromResult(new OllamaInstalledModelsResult(true, [], "not used"));
    }
}
