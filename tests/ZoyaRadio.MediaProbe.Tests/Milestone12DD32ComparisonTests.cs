using System.Text;
using System.Text.Json;
using ZoyaRadio.MediaProbe;

internal static class Milestone12DD32ComparisonTests
{
    public static void ScenarioMatrixIsExactAndOrdered()
    {
        string[] titles =
        [
            "Заберут меня в солдаты",
            "Sweet Home Alabama",
            "Symphony of Destruction",
            "Como Nossos Pais",
            "Paranoid",
            "Master of Puppets",
            "Chop Suey!",
            "Du Riechst so Gut",
        ];
        string[] artists =
        [
            "Отава Ё",
            "Lynyrd Skynyrd",
            "Megadeth",
            "Elis Regina",
            "Black Sabbath",
            "Metallica",
            "System of a Down",
            "Rammstein",
        ];

        TestAssert.Equal(8, M12DD32ComparisonLaboratory.Scenarios.Count);
        for (int i = 0; i < titles.Length; i++)
        {
            TestAssert.Equal($"M12D-D32-0{i + 1}", M12DD32ComparisonLaboratory.Scenarios[i].Id);
            TestAssert.Equal(titles[i], M12DD32ComparisonLaboratory.Scenarios[i].Title);
            TestAssert.Equal(artists[i], M12DD32ComparisonLaboratory.Scenarios[i].Artist);
            TestAssert.Equal(3, M12DD32ComparisonLaboratory.Scenarios[i].RecentAcceptedLines.Count);
        }

        M12DD32Scenario topic = M12DD32ComparisonLaboratory.Scenarios[0];
        TestAssert.Equal("Tommy the cat? Yeah, I’ve seen him—sitting on a shelf, judging my life choices, and judging the coffee too.", topic.RecentAcceptedLines[0]);
        TestAssert.Equal("Tribute has put on a tiny cape and demanded applause.", topic.RecentAcceptedLines[1]);
        TestAssert.Equal("Stillborn just left a very dramatic note on the fridge.", topic.RecentAcceptedLines[2]);

        M12DD32Scenario repeated = M12DD32ComparisonLaboratory.Scenarios[4];
        TestAssert.Equal("Oh, Tribute has put on a tiny cape and demanded applause.", repeated.RecentAcceptedLines[0]);
        TestAssert.Equal("Oh, Stillborn just left a very dramatic note on the fridge.", repeated.RecentAcceptedLines[1]);
        TestAssert.Equal("Oh, Stayin’ Alive is refusing to take the hint.", repeated.RecentAcceptedLines[2]);
    }

    public static void PlanHasExactlyTwoArmsAndTwoSamplesPerScenario()
    {
        IReadOnlyList<M12DD32PlanItem> plan = M12DD32ComparisonLaboratory.CreatePlan();

        TestAssert.Equal(32, plan.Count);
        TestAssert.Equal(32, plan.Select(x => x.BlindedLabel).Distinct().Count());
        TestAssert.Equal(8, plan.Select(x => x.ScenarioId).Distinct().Count());
        TestAssert.Equal(16, plan.Count(x => x.Arm == M12DD32ComparisonArm.D31));
        TestAssert.Equal(16, plan.Count(x => x.Arm == M12DD32ComparisonArm.D32));
        TestAssert.Equal(16, plan.Count(x => x.SampleNumber == 1));
        TestAssert.Equal(16, plan.Count(x => x.SampleNumber == 2));

        for (int scenarioIndex = 0; scenarioIndex < 8; scenarioIndex++)
        {
            IReadOnlyList<M12DD32PlanItem> items = plan
                .Where(x => x.ScenarioId == M12DD32ComparisonLaboratory.Scenarios[scenarioIndex].Id)
                .ToArray();
            TestAssert.Equal(4, items.Count);
            M12DD32ComparisonArm[] expectedArms =
                [M12DD32ComparisonArm.D31, M12DD32ComparisonArm.D31, M12DD32ComparisonArm.D32, M12DD32ComparisonArm.D32];
            int[] expectedSamples = [1, 2, 1, 2];
            for (int i = 0; i < items.Count; i++)
            {
                TestAssert.Equal(expectedArms[i], items[i].Arm);
                TestAssert.Equal(expectedSamples[i], items[i].SampleNumber);
            }
        }
    }

    public static async Task RunnerMakesExactly32CallsAndUnloadsOnce()
    {
        var fake = new FakeD32ComparisonGenerator();
        M12DD32RunResult result = await new M12DD32ComparisonRunner(fake).RunAsync(CancellationToken.None);

        TestAssert.True(result.Completed);
        TestAssert.Equal(32, result.Attempts);
        TestAssert.Equal(32, result.AttemptsDetail.Count);
        TestAssert.Equal(32, fake.GenerateCalls);
        TestAssert.Equal(1, fake.UnloadCalls);
        TestAssert.Equal(16, fake.Prompts.Count(x => x.SystemInstruction == M12DD32ComparisonLaboratory.D31Assignment));
        TestAssert.Equal(16, fake.Prompts.Count(x => x.SystemInstruction == QuickReactionPilotPrompt.Assignment));
        TestAssert.Equal(AiCommentConfiguration.Default.NormalKeepAlive, fake.KeepAlives.Distinct().Single());
        TestAssert.Equal(AiCommentConfiguration.Default.GenerationTimeout, fake.GenerationTimeouts.Distinct().Single());
        TestAssert.True(fake.Prompts.Any(x => x.MetadataPrompt.Contains("Tommy the cat?", StringComparison.Ordinal)));
        TestAssert.True(fake.Prompts.Any(x => x.MetadataPrompt.Contains("CURRENT SUBJECT JSON", StringComparison.Ordinal)));
        TestAssert.False(fake.Prompts.Any(x => x.MetadataPrompt.Contains("containsPriorWordsOrTopics", StringComparison.Ordinal)
            && x.MetadataPrompt.Contains("Tommy the cat?", StringComparison.Ordinal)));
    }

    public static async Task D31OutputUsesAcceptedSpokenTextCleanupParity()
    {
        var fake = new FakeD32ComparisonGenerator();
        fake.Actions.Enqueue(_ => new OllamaGenerationResult(
            OllamaGenerationStatus.Success,
            "**That title has arrived with enough confidence to rearrange the furniture, Steve, and frankly I admire the paperwork-free commitment.** ✨",
            null,
            null));

        M12DD32RunResult result = await new M12DD32ComparisonRunner(fake).RunAsync(CancellationToken.None);

        TestAssert.Equal(32, result.Attempts);
        TestAssert.Equal("That title has arrived with enough confidence to rearrange the furniture, Steve, and frankly I admire the paperwork-free commitment.", result.AttemptsDetail[0].Output);
        TestAssert.Equal("Accepted", result.AttemptsDetail[0].Outcome);
    }

    public static void ArmsUseTheExactDistinctPromptContracts()
    {
        TestAssert.Equal("quick-reaction-v2-variety-spoken-text", M12DD32ComparisonLaboratory.D31AssignmentVersion);
        TestAssert.Equal("quick-reaction-v3-topic-isolation", QuickReactionPilotPrompt.AssignmentVersion);
        TestAssert.Contains(M12DD32ComparisonLaboratory.D31Assignment, "Recent accepted spoken lines are anti-repetition context only");
        TestAssert.Contains(QuickReactionPilotPrompt.Assignment, "Prior accepted lines are deliberately absent");
        TestAssert.False(
            M12DD32ComparisonLaboratory.D31Assignment.Equals(
                QuickReactionPilotPrompt.Assignment,
                StringComparison.Ordinal));

        M12DD32Scenario scenario = M12DD32ComparisonLaboratory.Scenarios[0];
        CommentPrompt d31 = M12DD32ComparisonLaboratory.CreateD31Prompt(scenario);
        CommentPrompt d32 = M12DD32ComparisonLaboratory.CreateD32Prompt(scenario);
        TestAssert.Equal(M12DD32ComparisonLaboratory.D31Assignment, d31.SystemInstruction);
        TestAssert.Equal(QuickReactionPilotPrompt.Assignment, d32.SystemInstruction);
        TestAssert.Contains(d31.MetadataPrompt, "recentAcceptedSpokenLines");
        TestAssert.Contains(d31.MetadataPrompt, "Tommy the cat?");
        TestAssert.Contains(d32.MetadataPrompt, "AVOIDANCE DATA JSON");
        TestAssert.False(d32.MetadataPrompt.Contains("Tommy the cat?", StringComparison.Ordinal));
    }

    public static async Task ArtifactsSeparateBlindedEvaluationAndAnswerKey()
    {
        var fake = new FakeD32ComparisonGenerator();
        M12DD32RunResult result = await new M12DD32ComparisonRunner(fake).RunAsync(CancellationToken.None);
        string directory = Path.Combine(
            RepositoryPathLocator.FindRepositoryRoot(),
            "artifacts",
            "m12d-d3-2",
            DateTimeOffset.UtcNow.ToString("yyyyMMdd-HHmmss-fff"));
        try
        {
            M12DD32ComparisonLaboratory.WriteArtifacts(directory, result);
            string evaluation = File.ReadAllText(
                Path.Combine(directory, "M12D_D3-2_EVALUATION.md"),
                Encoding.UTF8);
            string answerKey = File.ReadAllText(
                Path.Combine(directory, "M12D_D3-2_ANSWER_KEY.json"),
                Encoding.UTF8);
            string summary = File.ReadAllText(
                Path.Combine(directory, "M12D_D3-2_RUN_SUMMARY.json"),
                Encoding.UTF8);

            TestAssert.Contains(evaluation, "Topic isolation");
            TestAssert.Contains(evaluation, "Prior-topic reuse");
            TestAssert.Contains(evaluation, "Repeated opening");
            TestAssert.Contains(evaluation, "Host identity");
            TestAssert.Contains(evaluation, "Spoken naturalness");
            TestAssert.Contains(evaluation, "Concision");
            TestAssert.Contains(evaluation, "Factual restraint");
            TestAssert.Contains(evaluation, "Desire to hear");
            TestAssert.Contains(evaluation, "Prior-context packet (oldest to newest");
            TestAssert.Contains(evaluation, "Заберут меня в солдаты");
            TestAssert.False(evaluation.Contains("\"arm\"", StringComparison.OrdinalIgnoreCase));
            TestAssert.False(evaluation.Contains("D3.1", StringComparison.Ordinal));
            TestAssert.Contains(answerKey, "\"arm\": \"D3.1\"");
            TestAssert.Contains(answerKey, "\"arm\": \"D3.2\"");
            TestAssert.False(answerKey.Contains("This title just winked", StringComparison.Ordinal));
            TestAssert.Contains(summary, "\"attempts\": 32");
            TestAssert.Contains(summary, "\"latencyMilliseconds\"");
            TestAssert.Contains(summary, "\"failures\": 0");
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

    public static async Task FailureAndCancellationAccountingIsBounded()
    {
        var failureFake = new FakeD32ComparisonGenerator();
        failureFake.Actions.Enqueue(_ => new OllamaGenerationResult(
            OllamaGenerationStatus.ConnectionFailure,
            null,
            "fake connection failure",
            null));
        failureFake.Actions.Enqueue(_ => throw new InvalidOperationException("fake producer failure"));
        M12DD32RunResult failures = await new M12DD32ComparisonRunner(failureFake).RunAsync(CancellationToken.None);

        TestAssert.False(failures.Completed);
        TestAssert.Equal(32, failures.Attempts);
        TestAssert.Equal(2, failures.Failures);
        TestAssert.Equal(32, failureFake.GenerateCalls);
        TestAssert.Equal(1, failureFake.UnloadCalls);
        TestAssert.Equal(2, failures.AttemptsDetail.Count(x => x.Outcome is "ConnectionFailure" or "ProducerFailure"));

        using var cancellation = new CancellationTokenSource();
        cancellation.Cancel();
        var cancelledFake = new FakeD32ComparisonGenerator();
        M12DD32RunResult cancelled = await new M12DD32ComparisonRunner(cancelledFake).RunAsync(cancellation.Token);

        TestAssert.False(cancelled.Completed);
        TestAssert.Equal(1, cancelled.Attempts);
        TestAssert.Equal(1, cancelled.Failures);
        TestAssert.Equal(1, cancelledFake.GenerateCalls);
        TestAssert.Equal(1, cancelledFake.UnloadCalls);
        TestAssert.False(cancelled.UnloadResult.Message.Contains("not attempted", StringComparison.OrdinalIgnoreCase));

        using var returnedCancellation = new CancellationTokenSource();
        returnedCancellation.Cancel();
        var returnedStatusFake = new FakeD32ComparisonGenerator();
        returnedStatusFake.Actions.Enqueue(_ => new OllamaGenerationResult(
            OllamaGenerationStatus.Cancelled,
            null,
            "fake returned cancellation",
            null));
        M12DD32RunResult returnedStatus = await new M12DD32ComparisonRunner(returnedStatusFake)
            .RunAsync(returnedCancellation.Token);

        TestAssert.False(returnedStatus.Completed);
        TestAssert.Equal(1, returnedStatus.Attempts);
        TestAssert.Equal(1, returnedStatus.Failures);
        TestAssert.Equal(1, returnedStatusFake.GenerateCalls);
        TestAssert.Equal(1, returnedStatusFake.UnloadCalls);
        TestAssert.Equal("Cancelled", returnedStatus.AttemptsDetail[0].Outcome);
        TestAssert.False(returnedStatus.UnloadResult.Message.Contains("not attempted", StringComparison.OrdinalIgnoreCase));
    }

    public static void ComparisonToolingHasNoRuntimeOrExternalMediaDependency()
    {
        string root = RepositoryPathLocator.FindRepositoryRoot();
        string source = File.ReadAllText(
                Path.Combine(root, "src", "ZoyaRadio.MediaProbe", "M12DD32ComparisonLaboratory.cs"),
                Encoding.UTF8)
            + File.ReadAllText(
                Path.Combine(root, "src", "ZoyaRadio.MediaProbe", "M12DD32ComparisonCommand.cs"),
                Encoding.UTF8)
            + File.ReadAllText(
                Path.Combine(root, "run-m12d-d3-2-comparison.cmd"),
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
        })
        {
            TestAssert.False(source.Contains(forbidden, StringComparison.Ordinal));
        }

        TestAssert.Contains(source, "GENERATE ONLY");
        TestAssert.Contains(source, "AiCommentConfiguration.Default");
        TestAssert.Contains(source, "Console.CancelKeyPress");
    }

    private sealed class FakeD32ComparisonGenerator : ILocalCommentGenerator
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
            bool hasQueuedAction = Actions.Count > 0;
            Func<CancellationToken, OllamaGenerationResult> action =
                hasQueuedAction
                    ? Actions.Dequeue()
                    : _ => new OllamaGenerationResult(
                        OllamaGenerationStatus.Success,
                        "That title has arrived with enough confidence to rearrange the furniture, Steve, and frankly I admire the paperwork-free commitment.",
                        null,
                        null);
            if (!hasQueuedAction)
            {
                cancellationToken.ThrowIfCancellationRequested();
            }

            return Task.FromResult(action(cancellationToken));
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
