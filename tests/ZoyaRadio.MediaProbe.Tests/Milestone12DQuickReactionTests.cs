using System.Text;
using System.Text.Json;
using ZoyaRadio.MediaProbe;

internal static class Milestone12DQuickReactionTests
{
    public static void PlanIsExactAndRealContextOnly()
    {
        var plan = M12DQuickReactionLaboratory.CreatePlan();
        TestAssert.Equal(12, M12DQuickReactionLaboratory.Fixtures.Count);
        TestAssert.Equal(36, plan.Count);
        TestAssert.Equal(12, plan.Count(x => x.IsReference));
        TestAssert.Equal(24, plan.Count(x => !x.IsReference));
        TestAssert.Equal(36, plan.Select(x => x.BlindedLabel).Distinct().Count());
        TestAssert.Contains(M12DQuickReactionLaboratory.Fixtures.Single(x => x.Id == "M12D-08").Title, "Alive");
        TestAssert.Equal("Como Nossos Pais", M12DQuickReactionLaboratory.Fixtures.Single(x => x.Id == "M12D-12").Title);
    }

    public static async Task RunnerIsBoundedAndUnloads()
    {
        var fake = new FakeCommentGenerator();
        M12DRunResult result = await new M12DQuickReactionRunner(fake).RunAsync(CancellationToken.None);
        TestAssert.True(result.Completed);
        TestAssert.Equal(36, result.Records.Count);
        TestAssert.Equal(24, fake.GenerateCalls);
        TestAssert.Equal(1, fake.UnloadCalls);
        TestAssert.Contains(fake.LastPrompt!.SystemInstruction, "point of view");
        TestAssert.Contains(fake.LastPrompt.MetadataPrompt, "titleWordplayOpportunity");
    }

    public static async Task RuntimePilotPromptUsesBoundedAntiRepetitionContext()
    {
        var fake = new FakeCommentGenerator();
        var service = new AiCommentService(fake, AiCommentConfiguration.Default, new FakeLog());
        var context = new RadioPreparationContext(
            new MediaSnapshot("Numb", "Linkin Park", null),
            DateTimeOffset.Parse("2026-08-26T12:00:00Z"),
            ["Black Label Society appeared repeatedly."],
            ["Oldest line.", "Oh, a recent line.", "Another recent line.", "Newest recent line."]);

        CommentSelection result = await service.SelectQuickReactionAsync(context, true, CancellationToken.None);

        TestAssert.Equal(CommentTextSource.Ai, result.Source);
        TestAssert.Equal("quick-reaction-v3-topic-isolation", QuickReactionPilotPrompt.AssignmentVersion);
        TestAssert.Contains(fake.LastPrompt!.SystemInstruction, "materially different opening");
        TestAssert.Contains(fake.LastPrompt.SystemInstruction, "no Markdown");
        TestAssert.Contains(fake.LastPrompt.MetadataPrompt, "newest three accepted spoken lines");
        TestAssert.Contains(fake.LastPrompt.MetadataPrompt, "single-word-comma lead");
        TestAssert.Contains(fake.LastPrompt.MetadataPrompt, "older-2");
        TestAssert.Contains(fake.LastPrompt.MetadataPrompt, "newest");
        TestAssert.False(fake.LastPrompt.MetadataPrompt.Contains("Oldest line.", StringComparison.Ordinal));
        TestAssert.False(fake.LastPrompt.MetadataPrompt.Contains("Oh, a recent line.", StringComparison.Ordinal));
        TestAssert.False(fake.LastPrompt.MetadataPrompt.Contains("Newest recent line.", StringComparison.Ordinal));
        TestAssert.False(fake.LastPrompt.MetadataPrompt.Contains("Black Label Society", StringComparison.Ordinal));
    }

    public static void RuntimePilotSeparatesCurrentSubjectFromContentFreeAvoidanceData()
    {
        var context = new RadioPreparationContext(
            new MediaSnapshot("Заберут меня в солдаты", "Otava Yo", null),
            DateTimeOffset.Parse("2026-08-26T12:00:00Z"),
            [],
            ["Tommy the Cat has wandered into a bass-shaped courtroom."]);

        CommentPrompt prompt = QuickReactionPilotPrompt.Create(context);
        (string subjectJson, string avoidanceJson) = PromptSections(prompt.MetadataPrompt);

        using JsonDocument subject = JsonDocument.Parse(subjectJson);
        TestAssert.Equal("Заберут меня в солдаты", subject.RootElement.GetProperty("title").GetString());
        TestAssert.Equal("Otava Yo", subject.RootElement.GetProperty("artist").GetString());
        TestAssert.False(subjectJson.Contains("patterns", StringComparison.Ordinal));

        using JsonDocument avoidance = JsonDocument.Parse(avoidanceJson);
        TestAssert.False(avoidance.RootElement.GetProperty("containsPriorWordsOrTopics").GetBoolean());
        TestAssert.Equal(1, avoidance.RootElement.GetProperty("patterns").GetArrayLength());
        TestAssert.False(avoidanceJson.Contains("Tommy", StringComparison.Ordinal));
        TestAssert.False(avoidanceJson.Contains("Cat", StringComparison.Ordinal));
        TestAssert.False(avoidanceJson.Contains("Otava", StringComparison.Ordinal));
        TestAssert.False(avoidanceJson.Contains("солдаты", StringComparison.Ordinal));
    }

    public static void RuntimePilotContextIsDeterministicAndHandlesNullOrWhitespace()
    {
        var track = new MediaSnapshot("  Café 東京  ", "Beyoncé & Молчат Дома", null);
        var withWhitespace = new RadioPreparationContext(
            track,
            DateTimeOffset.Parse("2026-08-26T12:00:00Z"),
            [],
            [null!, "  ", "  Oh,  one idea!  ", "A second idea -- really?  "]);
        var withNormalizedLines = withWhitespace with
        {
            RecentAcceptedAiComments = ["Oh, one idea!", "A second idea -- really?"],
        };
        var withNull = withWhitespace with { RecentAcceptedAiComments = null! };

        CommentPrompt first = QuickReactionPilotPrompt.Create(withWhitespace);
        CommentPrompt second = QuickReactionPilotPrompt.Create(withNormalizedLines);
        CommentPrompt empty = QuickReactionPilotPrompt.Create(withNull);

        TestAssert.Equal(first, second);
        TestAssert.Contains(first.MetadataPrompt, "single-word-comma lead");
        TestAssert.Contains(first.MetadataPrompt, "exclamation");
        TestAssert.Contains(first.MetadataPrompt, "question");
        (string subjectJson, _) = PromptSections(first.MetadataPrompt);
        using JsonDocument subject = JsonDocument.Parse(subjectJson);
        TestAssert.Equal("  Café 東京  ", subject.RootElement.GetProperty("title").GetString());
        TestAssert.Equal("Beyoncé & Молчат Дома", subject.RootElement.GetProperty("artist").GetString());
        TestAssert.Contains(empty.MetadataPrompt, "\"patterns\":[]");
    }

    public static void RuntimePilotAssignmentExplicitlyRejectsPriorTopicReuse()
    {
        TestAssert.Contains(QuickReactionPilotPrompt.Assignment, "only subject is the CURRENT SUBJECT");
        TestAssert.Contains(QuickReactionPilotPrompt.Assignment, "not source material");
        TestAssert.Contains(QuickReactionPilotPrompt.Assignment, "do not guess or recover their words, subjects, facts, premises, quotations, scenes, entities, jokes, or unfinished ideas");
        TestAssert.Contains(QuickReactionPilotPrompt.Assignment, "do not refer to or continue any prior topic");

        // These are deterministic prompt-contract checks, not a claim that they
        // can semantically prove the prose produced by a future model request.
    }

    public static void RuntimePilotOptInAndM11BDefaultPathRemainExact()
    {
        string root = RepositoryPathLocator.FindRepositoryRoot();
        string sourceRoot = Path.Combine(root, "src", "ZoyaRadio.MediaProbe");
        string factory = File.ReadAllText(
            Path.Combine(sourceRoot, "WindowsRadioRuntimeSessionFactory.cs"),
            Encoding.UTF8);
        string runtime = File.ReadAllText(
            Path.Combine(sourceRoot, "RadioBehaviorTest.cs"),
            Encoding.UTF8);
        string aiService = File.ReadAllText(
            Path.Combine(sourceRoot, "AiCommentService.cs"),
            Encoding.UTF8);

        TestAssert.Contains(factory, "Environment.GetEnvironmentVariable(\"ZOYA_QUICK_REACTION_PILOT\"), \"1\", StringComparison.Ordinal");
        TestAssert.Contains(runtime, "bool quickReactionPilotEnabled = false");
        TestAssert.Contains(runtime, "quickReactionPilotEnabled ? new QuickReactionTransitionCommentProvider(aiService) : new LocalTransitionCommentProvider(aiService)");
        TestAssert.Contains(aiService, "CommentPromptFactory.CreateForPreparedTransition(context, _persona)");
        TestAssert.Equal("quick-reaction-v1", M12CHostFormatLaboratory.QuickReactionAssignment.Version);
    }

    public static async Task RuntimePilotRemovesPresentationOnlyCharactersBeforeSpeech()
    {
        var fake = new FakeCommentGenerator
        {
            GenerationResult = new OllamaGenerationResult(
                OllamaGenerationStatus.Success,
                "Oh, *Numb* has had enough dramatic punctuation for one afternoon, so I am putting the asterisks back in their little drawer. 😄",
                null,
                null),
        };
        var service = new AiCommentService(fake, AiCommentConfiguration.Default, new FakeLog());
        var context = new RadioPreparationContext(
            new MediaSnapshot("Numb", "Linkin Park", null),
            DateTimeOffset.Parse("2026-08-26T12:00:00Z"),
            [],
            []);

        CommentSelection result = await service.SelectQuickReactionAsync(context, true, CancellationToken.None);

        TestAssert.Equal(CommentTextSource.Ai, result.Source);
        TestAssert.Equal("Oh, Numb has had enough dramatic punctuation for one afternoon, so I am putting the asterisks back in their little drawer.", result.Text);
        TestAssert.False(result.Text.Contains('*'));
        TestAssert.False(result.Text.Contains('\ud83d'));
    }

    public static async Task RuntimePilotChecksDuplicatesAfterSpokenTextCleanup()
    {
        const string spoken = "Numb has had enough dramatic punctuation for one afternoon, so I am putting the asterisks back in their little drawer.";
        var fake = new FakeCommentGenerator
        {
            GenerationResult = new OllamaGenerationResult(
                OllamaGenerationStatus.Success,
                "*Numb* has had enough dramatic punctuation for one afternoon, so I am putting the asterisks back in their little drawer.",
                null,
                null),
        };
        var service = new AiCommentService(fake, AiCommentConfiguration.Default, new FakeLog());
        var context = new RadioPreparationContext(
            new MediaSnapshot("Numb", "Linkin Park", null),
            DateTimeOffset.Parse("2026-08-26T12:00:00Z"),
            [],
            [spoken]);

        CommentSelection result = await service.SelectQuickReactionAsync(context, true, CancellationToken.None);

        TestAssert.Equal(CommentTextSource.FixedFallback, result.Source);
        TestAssert.Equal("That was Numb by Linkin Park.", result.Text);
    }

    private static (string SubjectJson, string AvoidanceJson) PromptSections(string metadataPrompt)
    {
        const string subjectPrefix = "CURRENT SUBJECT JSON (the only subject for this reaction): ";
        const string avoidancePrefix = "AVOIDANCE DATA JSON (abstract form only; never subject matter): ";
        int avoidanceStart = metadataPrompt.IndexOf(avoidancePrefix, StringComparison.Ordinal);
        int finalInstructionStart = metadataPrompt.IndexOf(
            "Write one fresh reaction",
            avoidanceStart,
            StringComparison.Ordinal);

        string subjectJson = metadataPrompt[
            subjectPrefix.Length..(avoidanceStart - 1)];
        string avoidanceJson = metadataPrompt[
            (avoidanceStart + avoidancePrefix.Length)..(finalInstructionStart - 1)];
        return (subjectJson, avoidanceJson);
    }
}
