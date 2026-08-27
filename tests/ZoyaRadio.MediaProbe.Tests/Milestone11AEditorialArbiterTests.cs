using ZoyaRadio.MediaProbe;

internal static class Milestone11AEditorialArbiterTests
{
    private static readonly IReadOnlySet<EditorialDeliveryTreatment> DuckTreatment =
        new HashSet<EditorialDeliveryTreatment> { EditorialDeliveryTreatment.DuckOverMusic };

    public static void PreparationEligibilityAndDirectMusicAreTyped()
    {
        var arbiter = new EditorialArbiter(EditorialPolicyProfile.M10CEquivalent);
        EditorialPreparationCandidate candidate = Preparation("comment-1", 1);

        EditorialDecision selected = arbiter.DecidePreparation(new EditorialPreparationInput(
            [candidate], DuckTreatment, EditorialPressureSnapshot.Clear));
        TestAssert.Equal(EditorialDecisionStage.Preparation, selected.Stage);
        TestAssert.Equal(EditorialDecisionOutcome.Prepare, selected.Outcome);
        TestAssert.Equal("comment-1", selected.SelectedCandidateId);
        Has(selected, EditorialDecisionReason.CandidateConsidered, "comment-1");
        Has(selected, EditorialDecisionReason.PreparationSelected, "comment-1");
        Lacks(selected, EditorialDecisionReason.DirectMusicContinuationSelected);

        EditorialDecision noCapacity = arbiter.DecidePreparation(new EditorialPreparationInput(
            [candidate with { PreparationCapacityAvailable = false }],
            DuckTreatment,
            EditorialPressureSnapshot.Clear));
        TestAssert.Equal(EditorialDecisionOutcome.ContinueMusic, noCapacity.Outcome);
        Has(noCapacity, EditorialDecisionReason.PreparationCapacityUnavailable, "comment-1");
        Has(noCapacity, EditorialDecisionReason.DirectMusicContinuationSelected);

        EditorialDecision mismatch = arbiter.DecidePreparation(new EditorialPreparationInput(
            [candidate with { OpportunityMatches = false }],
            DuckTreatment,
            EditorialPressureSnapshot.Clear));
        TestAssert.Equal(EditorialDecisionOutcome.ContinueMusic, mismatch.Outcome);
        Has(mismatch, EditorialDecisionReason.OpportunityMismatch, "comment-1");

        EditorialDecision unavailableTreatment = arbiter.DecidePreparation(new EditorialPreparationInput(
            [candidate],
            new HashSet<EditorialDeliveryTreatment>(),
            EditorialPressureSnapshot.Clear));
        TestAssert.Equal(EditorialDecisionOutcome.ContinueMusic, unavailableTreatment.Outcome);
        Has(unavailableTreatment, EditorialDecisionReason.TreatmentUnavailable, "comment-1");
    }

    public static void AirtimeHardEligibilityRefusalsAreTyped()
    {
        var arbiter = new EditorialArbiter(EditorialPolicyProfile.M10CEquivalent);
        EditorialAirtimeCandidate candidate = Airtime("comment-1", 1);

        EditorialDecision selected = arbiter.DecideAirtime(AirtimeInput(candidate));
        TestAssert.Equal(EditorialDecisionOutcome.ReserveForAirtime, selected.Outcome);
        TestAssert.Equal(candidate.MaterialId, selected.SelectedMaterialId);
        Has(selected, EditorialDecisionReason.AirtimeSelected, "comment-1");

        AssertAirtimeRefusal(arbiter, candidate with { OpportunityMatches = false },
            EditorialDecisionReason.OpportunityMismatch);
        AssertAirtimeRefusal(arbiter, candidate with { MaterialStatus = null },
            EditorialDecisionReason.MaterialMissing);
        AssertAirtimeRefusal(arbiter, candidate with { MaterialStatus = PreparedMaterialLifecycleStatus.Reserved },
            EditorialDecisionReason.MaterialNotReady);
        AssertAirtimeRefusal(arbiter, candidate with { ExpiresAt = At(20) },
            EditorialDecisionReason.MaterialExpired);
        AssertAirtimeRefusal(arbiter, candidate with { HasPayload = false },
            EditorialDecisionReason.PayloadUnavailable);
        AssertAirtimeRefusal(arbiter, candidate with { BindingMatches = false },
            EditorialDecisionReason.BindingMismatch);

        EditorialDecision unavailableTreatment = arbiter.DecideAirtime(new EditorialAirtimeInput(
            At(20), [candidate], new HashSet<EditorialDeliveryTreatment>(), EditorialPressureSnapshot.Clear));
        TestAssert.Equal(EditorialDecisionOutcome.ContinueMusic, unavailableTreatment.Outcome);
        Has(unavailableTreatment, EditorialDecisionReason.TreatmentUnavailable, candidate.CandidateId);
    }

    public static void MusicPressureSpacingAndCooldownCanSelectMusic()
    {
        var arbiter = new EditorialArbiter(EditorialPolicyProfile.M10CEquivalent);
        EditorialAirtimeCandidate candidate = Airtime("comment-1", 1);

        AssertPressureRefusal(arbiter, candidate,
            new EditorialPressureSnapshot(EditorialMusicPressure.PreferMusic, true, true),
            EditorialDecisionReason.MusicPressurePreferred);
        AssertPressureRefusal(arbiter, candidate,
            new EditorialPressureSnapshot(EditorialMusicPressure.SpeechPermitted, false, true),
            EditorialDecisionReason.SpacingBlocked);
        AssertPressureRefusal(arbiter, candidate,
            new EditorialPressureSnapshot(EditorialMusicPressure.SpeechPermitted, true, false),
            EditorialDecisionReason.CooldownBlocked);

        var disabled = new EditorialArbiter(new EditorialPolicyProfile(true, false));
        EditorialDecision policySilence = disabled.DecideAirtime(AirtimeInput(candidate));
        TestAssert.Equal(EditorialDecisionOutcome.ContinueMusic, policySilence.Outcome);
        Has(policySilence, EditorialDecisionReason.AirtimePolicyDisabled, candidate.CandidateId);
        Has(policySilence, EditorialDecisionReason.DirectMusicContinuationSelected);
    }

    public static void PriorityAndBoundedTieBreakAreDeterministic()
    {
        var arbiter = new EditorialArbiter(EditorialPolicyProfile.M10CEquivalent);
        EditorialAirtimeCandidate routine = Airtime("routine", 1);
        EditorialAirtimeCandidate timely = Airtime("timely", 2) with
        {
            Priority = EditorialPolicyPriority.Timely,
            MaterialId = new PreparedMaterialId("material-2"),
        };

        EditorialDecision priority = arbiter.DecideAirtime(new EditorialAirtimeInput(
            At(20), [routine, timely], DuckTreatment, EditorialPressureSnapshot.Clear, TieBreakValue: 99));
        TestAssert.Equal("timely", priority.SelectedCandidateId);
        Has(priority, EditorialDecisionReason.PolicyPriorityPreferred);
        Lacks(priority, EditorialDecisionReason.BoundedTieBreakApplied);

        EditorialAirtimeCandidate second = Airtime("second", 2) with
        {
            MaterialId = new PreparedMaterialId("material-2"),
        };
        EditorialDecision tieA = arbiter.DecideAirtime(new EditorialAirtimeInput(
            At(20), [routine, second], DuckTreatment, EditorialPressureSnapshot.Clear, TieBreakValue: 0));
        EditorialDecision tieB = arbiter.DecideAirtime(new EditorialAirtimeInput(
            At(20), [routine, second], DuckTreatment, EditorialPressureSnapshot.Clear, TieBreakValue: 1));
        EditorialDecision tieBRepeat = arbiter.DecideAirtime(new EditorialAirtimeInput(
            At(20), [routine, second], DuckTreatment, EditorialPressureSnapshot.Clear, TieBreakValue: 1));

        TestAssert.Equal("routine", tieA.SelectedCandidateId);
        TestAssert.Equal("second", tieB.SelectedCandidateId);
        TestAssert.Equal(tieB.SelectedCandidateId, tieBRepeat.SelectedCandidateId);
        Has(tieA, EditorialDecisionReason.BoundedTieBreakApplied);
        Has(tieB, EditorialDecisionReason.BoundedTieBreakApplied);
    }

    public static void M10CEquivalentProfileRoutesExistingComment()
    {
        var state = new M10CRuntimeMigrationState(
            RadioBehaviorConfiguration.Default,
            new TargetSource(2, 5));
        state.Arm(Track("Initial", "Artist 0"), At(0));
        RadioPreparationOpportunity preparation = state.ObserveTrack(Track("Track A", "Artist A"), At(10))
            .PreparationOpportunity ?? throw new InvalidOperationException("Expected preparation.");

        EditorialDecision preparationDecision = state.EvaluatePreparation(preparation, true);
        TestAssert.Equal(EditorialDecisionOutcome.Prepare, preparationDecision.Outcome);
        var speech = new FakePreparedSpeech();
        TestAssert.True(state.MarkPreparationReady(preparation.Id, Prepared(preparation, speech)));

        RadioTransitionDelivery delivery = state.ObserveTrack(Track("Track B", "Artist B"), At(20))
            .Delivery ?? throw new InvalidOperationException("Expected delivery.");
        M10CDeliveryReservation? reservation = state.TryReserveDelivery(delivery, At(20));
        TestAssert.True(reservation is not null);
        TestAssert.Equal(EditorialDecisionOutcome.ReserveForAirtime, state.LastAirtimeDecision!.Outcome);

        TestAssert.True(state.ResolveOpportunity(delivery.OpportunityId, true, "Accepted comment.", At(21), reservation));
        TestAssert.Equal(1, speech.DisposeCalls);
    }

    public static void AdapterPolicySuppressionDoesNotReserveMaterial()
    {
        var state = new M10CRuntimeMigrationState(
            RadioBehaviorConfiguration.Default,
            new TargetSource(2, 5),
            new EditorialPolicyProfile(PreparationEnabled: true, AirtimeEnabled: false));
        state.Arm(Track("Initial", "Artist 0"), At(0));
        RadioPreparationOpportunity preparation = state.ObserveTrack(Track("Track A", "Artist A"), At(10))
            .PreparationOpportunity ?? throw new InvalidOperationException("Expected preparation.");
        TestAssert.Equal(EditorialDecisionOutcome.Prepare, state.EvaluatePreparation(preparation, true).Outcome);
        var speech = new FakePreparedSpeech();
        TestAssert.True(state.MarkPreparationReady(preparation.Id, Prepared(preparation, speech)));

        RadioTransitionDelivery delivery = state.ObserveTrack(Track("Track B", "Artist B"), At(20))
            .Delivery ?? throw new InvalidOperationException("Expected delivery.");
        TestAssert.True(state.TryReserveDelivery(delivery, At(20)) is null);
        TestAssert.Equal(EditorialDecisionOutcome.ContinueMusic, state.LastAirtimeDecision!.Outcome);
        Has(state.LastAirtimeDecision, EditorialDecisionReason.AirtimePolicyDisabled, "existing-comment-1");
        TestAssert.True(state.ResolveOpportunity(delivery.OpportunityId, false, null, At(21)));
        TestAssert.Equal(1, speech.DisposeCalls);
    }

    private static void AssertAirtimeRefusal(
        EditorialArbiter arbiter,
        EditorialAirtimeCandidate candidate,
        EditorialDecisionReason reason)
    {
        EditorialDecision decision = arbiter.DecideAirtime(AirtimeInput(candidate));
        TestAssert.Equal(EditorialDecisionOutcome.ContinueMusic, decision.Outcome);
        Has(decision, reason, candidate.CandidateId);
        Has(decision, EditorialDecisionReason.DirectMusicContinuationSelected);
    }

    private static void AssertPressureRefusal(
        EditorialArbiter arbiter,
        EditorialAirtimeCandidate candidate,
        EditorialPressureSnapshot pressure,
        EditorialDecisionReason reason)
    {
        EditorialDecision decision = arbiter.DecideAirtime(new EditorialAirtimeInput(
            At(20), [candidate], DuckTreatment, pressure));
        TestAssert.Equal(EditorialDecisionOutcome.ContinueMusic, decision.Outcome);
        Has(decision, reason, candidate.CandidateId);
        Has(decision, EditorialDecisionReason.DirectMusicContinuationSelected);
    }

    private static EditorialPreparationCandidate Preparation(string candidateId, long opportunityId) => new(
        candidateId,
        opportunityId,
        EditorialContentFormat.ExistingComment,
        EditorialDeliveryTreatment.DuckOverMusic,
        EditorialPolicyPriority.Routine,
        OpportunityMatches: true,
        OpportunityAvailable: true,
        PreparationCapacityAvailable: true);

    private static EditorialAirtimeCandidate Airtime(string candidateId, long opportunityId) => new(
        candidateId,
        opportunityId,
        new PreparedMaterialId($"material-{opportunityId}"),
        EditorialContentFormat.ExistingComment,
        EditorialDeliveryTreatment.DuckOverMusic,
        EditorialPolicyPriority.Routine,
        OpportunityMatches: true,
        MaterialStatus: PreparedMaterialLifecycleStatus.Ready,
        HasPayload: true,
        BindingMatches: true,
        ExpiresAt: At(30));

    private static EditorialAirtimeInput AirtimeInput(EditorialAirtimeCandidate candidate) => new(
        At(20), [candidate], DuckTreatment, EditorialPressureSnapshot.Clear);

    private static void Has(
        EditorialDecision decision,
        EditorialDecisionReason reason,
        string? candidateId = null) =>
        TestAssert.True(decision.Trace.Any(item =>
            item.Reason == reason && (candidateId is null || item.CandidateId == candidateId)));

    private static void Lacks(EditorialDecision decision, EditorialDecisionReason reason) =>
        TestAssert.False(decision.Trace.Any(item => item.Reason == reason));

    private static PreparedTransitionOpportunity Prepared(
        RadioPreparationOpportunity opportunity,
        FakePreparedSpeech speech) => new(
        opportunity,
        new CommentSelection("A bounded test comment.", CommentTextSource.FixedFallback, 4, "test", null, false),
        speech,
        At(11));

    private static MediaSnapshot Track(string title, string artist) => new(title, artist, null);

    private static DateTimeOffset At(int seconds) =>
        new DateTimeOffset(2026, 8, 23, 12, 0, 0, TimeSpan.Zero).AddSeconds(seconds);

    private sealed class TargetSource(params int[] values) : ITransitionTargetSource
    {
        private int _next;

        public int NextInclusive(int minimumInclusive, int maximumInclusive)
        {
            int value = values[_next++];
            if (value < minimumInclusive || value > maximumInclusive)
            {
                throw new InvalidOperationException("Test target was outside accepted bounds.");
            }

            return value;
        }
    }

    private sealed class FakePreparedSpeech : IPreparedAnnouncementSpeech
    {
        public int DisposeCalls { get; private set; }
        public TimeSpan MeasuredDuration => TimeSpan.FromSeconds(1);

        public Task<SpeechPlaybackResult> SpeakAsync(TimeSpan timeout, CancellationToken cancellationToken) =>
            Task.FromResult(new SpeechPlaybackResult(SpeechPlaybackStatus.Completed));

        public void Dispose() => DisposeCalls++;
    }
}
