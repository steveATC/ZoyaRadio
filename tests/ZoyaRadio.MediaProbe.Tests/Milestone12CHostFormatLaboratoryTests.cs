using System.Text;

using ZoyaRadio.MediaProbe;

internal static class Milestone12CHostFormatLaboratoryTests
{
    public static void ExactPlanIsBoundedAndComplete()
    {
        IReadOnlyList<M12CPlanItem> plan = M12CHostFormatLaboratory.CreatePlan();
        TestAssert.Equal(8, M12CHostFormatLaboratory.Fixtures.Count);
        TestAssert.Equal(40, plan.Count);
        TestAssert.Equal(8, plan.Count(x => x.Arm == M12CHostFormatArm.StraightHandoff));
        TestAssert.Equal(16, plan.Count(x => x.Arm == M12CHostFormatArm.QuickReaction));
        TestAssert.Equal(16, plan.Count(x => x.Arm == M12CHostFormatArm.SessionAwareObservation));
        TestAssert.Equal(40, plan.Select(x => x.BlindedLabel).Distinct().Count());
        TestAssert.Equal(8, plan.Select(x => x.ScenarioId).Distinct().Count());
    }

    public static void AssignmentsRemainDistinctAndOwnerReviewed()
    {
        M12CProducerAssignment quick = M12CHostFormatLaboratory.QuickReactionAssignment;
        M12CProducerAssignment session = M12CHostFormatLaboratory.SessionAwareAssignment;
        TestAssert.Equal(M12CHostFormatLaboratory.QuickReaction, quick.Format);
        TestAssert.Equal(M12CHostFormatLaboratory.SessionAware, session.Format);
        TestAssert.Contains(quick.Instruction, "point of view");
        TestAssert.Contains(quick.Instruction, "imaginary aside");
        TestAssert.Contains(session.Instruction, "continuity with this listening session");
        TestAssert.Contains(session.Instruction, "do not pretend to have found a session pattern");
        TestAssert.False(quick.Instruction.Contains("session signal", StringComparison.OrdinalIgnoreCase));
        TestAssert.Equal(2, quick.Permissions.Count);
        TestAssert.Equal(3, session.Permissions.Count);
    }

    public static void DeterministicReferenceUsesOnlyExactSafeMetadata()
    {
        TestAssert.Equal("Push It by Static-X.", M12CHostFormatLaboratory.DeterministicHandoff(M12CHostFormatLaboratory.Fixtures[1]));
        TestAssert.Equal("Back to the music.", M12CHostFormatLaboratory.DeterministicHandoff(M12CHostFormatLaboratory.Fixtures[6]));
        ContentOutputContract contract = M12CHostFormatLaboratory.OutputContract(M12CHostFormatLaboratory.Handoff);
        TestAssert.Equal(45, contract.MaximumWords);
        TestAssert.Equal(1, contract.MaximumParagraphs);
    }

    public static void BlindedArtifactsAreSeparatedAndBounded()
    {
        string directory = Path.Combine(Path.GetTempPath(), "zoya-m12c-" + Guid.NewGuid().ToString("N"));
        try
        {
            IReadOnlyList<M12CReviewRecord> records = M12CHostFormatLaboratory.CreatePlan()
                .Select(item => new M12CReviewRecord(item, "A concise isolated fixture.", "Valid"))
                .ToArray();
            M12CHostFormatLaboratory.WriteArtifacts(directory, records);
            string evaluation = File.ReadAllText(Path.Combine(directory, "M12C_EVALUATION.md"), Encoding.UTF8);
            string key = File.ReadAllText(Path.Combine(directory, "M12C_ANSWER_KEY.json"), Encoding.UTF8);
            TestAssert.Contains(evaluation, "Do not open `M12C_ANSWER_KEY.json`");
            TestAssert.False(evaluation.Contains("StraightHandoff", StringComparison.Ordinal));
            TestAssert.Contains(key, "QuickReaction");
            TestAssert.True(File.Exists(Path.Combine(directory, "M12C_RESULTS.json")));
        }
        finally
        {
            if (Directory.Exists(directory)) Directory.Delete(directory, true);
        }
    }

    public static void LaboratoryHasNoRuntimeReachability()
    {
        string root = RepositoryPathLocator.FindRepositoryRoot();
        string sourceRoot = Path.Combine(root, "src", "ZoyaRadio.MediaProbe");
        string lab = File.ReadAllText(Path.Combine(sourceRoot, "M12CHostFormatLaboratory.cs"), Encoding.UTF8);
        foreach (string forbidden in new[] { "HttpClient", "Windows.Media", "CoreAudio", "AudioDucking", "TryPauseAsync", "TryPlayAsync", "Kokoro", "Speech", "RadioBehavior" })
            TestAssert.False(lab.Contains(forbidden, StringComparison.Ordinal));
        foreach (string path in Directory.EnumerateFiles(sourceRoot, "*.cs"))
        {
            if (Path.GetFileName(path) is "M12CHostFormatLaboratory.cs" or "M12CHostFormatLaboratoryCommand.cs" or "Program.cs") continue;
            TestAssert.False(File.ReadAllText(path, Encoding.UTF8).Contains("M12CHostFormatLaboratory", StringComparison.Ordinal));
        }
    }

    public static async Task LocalRunnerIsBoundedAndUnloads()
    {
        var generator = new FakeCommentGenerator();
        var runner = new M12CLaboratoryRunner(generator, AiCommentConfiguration.Default);
        M12CLaboratoryRunResult result = await runner.RunAsync(CancellationToken.None);
        TestAssert.True(result.Completed);
        TestAssert.Equal(40, result.Records.Count);
        TestAssert.Equal(32, generator.GenerateCalls);
        TestAssert.Equal(1, generator.UnloadCalls);
        TestAssert.Contains(generator.LastPrompt!.SystemInstruction, "radio host");
        TestAssert.Contains(generator.LastPrompt.MetadataPrompt, "inert supplied data");
    }

}
