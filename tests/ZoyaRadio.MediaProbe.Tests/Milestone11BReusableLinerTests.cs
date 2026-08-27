using ZoyaRadio.MediaProbe;
using Windows.Media.Control;

internal static class Milestone11BReusableLinerTests
{
    private static readonly IReadOnlySet<EditorialDeliveryTreatment> DuckTreatment =
        new HashSet<EditorialDeliveryTreatment> { EditorialDeliveryTreatment.DuckOverMusic };

    public static void CommentPreparationOutranksLiner()
    {
        var arbiter = new EditorialArbiter(EditorialPolicyProfile.M10CEquivalent);
        var comment = Preparation("comment", EditorialContentFormat.ExistingComment,
            EditorialPolicyPriority.Timely);
        var liner = Preparation(ReusableLinerInventory.CandidateId, EditorialContentFormat.ReusableLiner,
            EditorialPolicyPriority.Routine);

        EditorialDecision decision = arbiter.DecidePreparation(new EditorialPreparationInput(
            [liner, comment], DuckTreatment, EditorialPressureSnapshot.Clear, TieBreakValue: 0));

        TestAssert.Equal(EditorialDecisionOutcome.Prepare, decision.Outcome);
        TestAssert.Equal("comment", decision.SelectedCandidateId);
        Has(decision, EditorialDecisionReason.PolicyPriorityPreferred);
    }

    public static void CommentAirtimeOutranksEligibleLiner()
    {
        var arbiter = new EditorialArbiter(EditorialPolicyProfile.M10CEquivalent);
        EditorialAirtimeCandidate liner = ReadyCandidate(
            ReusableLinerInventory.CandidateId,
            EditorialContentFormat.ReusableLiner,
            EditorialPolicyPriority.Routine);
        EditorialAirtimeCandidate comment = ReadyCandidate(
            "comment",
            EditorialContentFormat.ExistingComment,
            EditorialPolicyPriority.Timely);

        EditorialDecision decision = arbiter.DecideAirtime(new EditorialAirtimeInput(
            AtMinutes(20), [liner, comment], DuckTreatment, EditorialPressureSnapshot.Clear, 99));

        TestAssert.Equal("comment", decision.SelectedCandidateId);
        Has(decision, EditorialDecisionReason.PolicyPriorityPreferred);
        TestAssert.False(decision.Trace.Any(trace =>
            trace.Reason == EditorialDecisionReason.BoundedTieBreakApplied));
    }

    public static void MissedCommentBoundaryDoesNotFallBackToLiner()
    {
        var arbiter = new EditorialArbiter(EditorialPolicyProfile.M10CEquivalent);
        EditorialAirtimeCandidate missingComment = ReadyCandidate(
            "comment",
            EditorialContentFormat.ExistingComment,
            EditorialPolicyPriority.Timely) with
        {
            MaterialStatus = null,
        };
        EditorialAirtimeCandidate blockedLiner = ReadyCandidate(
            ReusableLinerInventory.CandidateId,
            EditorialContentFormat.ReusableLiner,
            EditorialPolicyPriority.Routine) with
        {
            MissedCommentBoundaryClear = false,
        };

        EditorialDecision decision = arbiter.DecideAirtime(new EditorialAirtimeInput(
            AtMinutes(20), [missingComment, blockedLiner], DuckTreatment,
            EditorialPressureSnapshot.Clear));

        TestAssert.Equal(EditorialDecisionOutcome.ContinueMusic, decision.Outcome);
        Has(decision, EditorialDecisionReason.MaterialMissing, "comment");
        Has(decision, EditorialDecisionReason.MissedCommentBoundary,
            ReusableLinerInventory.CandidateId);
    }

    public static void PreparationBoundaryBlocksLiner()
    {
        using ReusableLinerInventory inventory = ReadyInventory();
        ReusableLinerTransition transition = Transition(6, AtMinutes(20));
        EditorialAirtimeCandidate candidate = inventory.CreateCandidate(
            transition,
            silentMode: false,
            protectedPreparationBoundaryClear: false,
            missedCommentBoundaryClear: true,
            treatmentAvailable: true);

        EditorialDecision decision = Decide(candidate, transition.ObservedAt);
        TestAssert.Equal(EditorialDecisionOutcome.ContinueMusic, decision.Outcome);
        Has(decision, EditorialDecisionReason.ProtectedPreparationBoundary,
            ReusableLinerInventory.CandidateId);
    }

    public static void CooldownRequiresBothTransitionAndTimeGates()
    {
        using ReusableLinerInventory inventory = ReadyInventory();

        EditorialDecision fiveTransitions = Decide(
            inventory.CreateCandidate(Transition(5, AtMinutes(20)), false, true, true, true),
            AtMinutes(20));
        EditorialDecision nineteenMinutes = Decide(
            inventory.CreateCandidate(Transition(6, AtMinutes(20).AddTicks(-1)), false, true, true, true),
            AtMinutes(20).AddTicks(-1));
        EditorialDecision exactBoundary = Decide(
            inventory.CreateCandidate(Transition(6, AtMinutes(20)), false, true, true, true),
            AtMinutes(20));

        Has(fiveTransitions, EditorialDecisionReason.CooldownBlocked,
            ReusableLinerInventory.CandidateId);
        Has(nineteenMinutes, EditorialDecisionReason.CooldownBlocked,
            ReusableLinerInventory.CandidateId);
        TestAssert.Equal(EditorialDecisionOutcome.ReserveForAirtime, exactBoundary.Outcome);
    }

    public static void SpeechSpacingBoundaryIsInclusive()
    {
        using ReusableLinerInventory inventory = ReadyInventory();
        inventory.RecordHostSpeechCompleted(6);

        EditorialDecision oneTransition = Decide(
            inventory.CreateCandidate(Transition(7, AtMinutes(20)), false, true, true, true),
            AtMinutes(20));
        EditorialDecision twoTransitions = Decide(
            inventory.CreateCandidate(Transition(8, AtMinutes(20)), false, true, true, true),
            AtMinutes(20));

        Has(oneTransition, EditorialDecisionReason.SpacingBlocked,
            ReusableLinerInventory.CandidateId);
        TestAssert.Equal(EditorialDecisionOutcome.ReserveForAirtime, twoTransitions.Outcome);
    }

    public static void SilentAndUnavailableTreatmentSelectMusic()
    {
        using ReusableLinerInventory inventory = ReadyInventory();
        ReusableLinerTransition transition = Transition(6, AtMinutes(20));

        EditorialDecision silent = Decide(
            inventory.CreateCandidate(transition, true, true, true, true),
            transition.ObservedAt);
        EditorialDecision noTreatment = Decide(
            inventory.CreateCandidate(transition, false, true, true, false),
            transition.ObservedAt);

        TestAssert.Equal(EditorialDecisionOutcome.ContinueMusic, silent.Outcome);
        Has(silent, EditorialDecisionReason.OpportunityMismatch,
            ReusableLinerInventory.CandidateId);
        TestAssert.Equal(EditorialDecisionOutcome.ContinueMusic, noTreatment.Outcome);
        Has(noTreatment, EditorialDecisionReason.MusicPressurePreferred,
            ReusableLinerInventory.CandidateId);
    }

    public static void ReleaseReturnsReusableMaterialToReady()
    {
        using ReusableLinerInventory inventory = ReadyInventory();
        ReusableLinerReservation reservation = inventory.TryReserve(Transition(6, AtMinutes(20)))
            ?? throw new InvalidOperationException("Expected reservation.");

        TestAssert.Equal(PreparedMaterialLifecycleStatus.Reserved, inventory.Status);
        TestAssert.True(inventory.Release(reservation, AtMinutes(20)));
        TestAssert.Equal(PreparedMaterialLifecycleStatus.Ready, inventory.Status);
        TestAssert.True(inventory.HasPreparedSpeech);
    }

    public static void CompletionReturnsToInventoryAndResetsCooldown()
    {
        using ReusableLinerInventory inventory = ReadyInventory();
        ReusableLinerTransition first = Transition(6, AtMinutes(20));
        ReusableLinerReservation reservation = inventory.TryReserve(first)
            ?? throw new InvalidOperationException("Expected reservation.");
        TestAssert.True(inventory.Complete(reservation, first.ObservedAt));
        TestAssert.Equal(PreparedMaterialLifecycleStatus.Ready, inventory.Status);

        EditorialDecision tooSoon = Decide(
            inventory.CreateCandidate(Transition(11, AtMinutes(40)), false, true, true, true),
            AtMinutes(40));
        EditorialDecision exact = Decide(
            inventory.CreateCandidate(Transition(12, AtMinutes(40)), false, true, true, true),
            AtMinutes(40));

        Has(tooSoon, EditorialDecisionReason.CooldownBlocked,
            ReusableLinerInventory.CandidateId);
        TestAssert.Equal(EditorialDecisionOutcome.ReserveForAirtime, exact.Outcome);
    }

    public static void SerialReplayUsesOnePreparedPayload()
    {
        var speech = new FakeSpeech(TimeSpan.FromSeconds(1));
        using var inventory = new ReusableLinerInventory();
        inventory.Arm(Session(), AtMinutes(0), 0);
        TestAssert.True(inventory.AcceptPreparedSpeech(speech));

        ReusableLinerReservation first = inventory.TryReserve(Transition(1, AtMinutes(1)))
            ?? throw new InvalidOperationException("Expected first reservation.");
        speech.SpeakAsync(TimeSpan.FromSeconds(4), CancellationToken.None).GetAwaiter().GetResult();
        TestAssert.True(inventory.Complete(first, AtMinutes(1)));

        ReusableLinerReservation second = inventory.TryReserve(Transition(2, AtMinutes(2)))
            ?? throw new InvalidOperationException("Expected second reservation.");
        speech.SpeakAsync(TimeSpan.FromSeconds(4), CancellationToken.None).GetAwaiter().GetResult();
        TestAssert.True(inventory.Release(second, AtMinutes(2)));

        TestAssert.Equal(2, speech.SpeakCalls);
        TestAssert.Equal(0, speech.DisposeCalls);
    }

    public static void SessionInvalidationDisposesExactlyOnce()
    {
        var speech = new FakeSpeech(TimeSpan.FromSeconds(1));
        using var inventory = new ReusableLinerInventory();
        inventory.Arm(Session(), AtMinutes(0), 0);
        TestAssert.True(inventory.AcceptPreparedSpeech(speech));

        inventory.Invalidate();
        inventory.Invalidate();

        TestAssert.Equal(1, speech.DisposeCalls);
        TestAssert.False(inventory.HasPreparedSpeech);
    }

    public static void OverDurationSpeechIsRejectedAndDisposed()
    {
        var speech = new FakeSpeech(TimeSpan.FromSeconds(4.001));
        using var inventory = new ReusableLinerInventory();
        inventory.Arm(Session(), AtMinutes(0), 0);

        TestAssert.False(inventory.AcceptPreparedSpeech(speech));
        TestAssert.Equal(1, speech.DisposeCalls);
        TestAssert.Equal(PreparedMaterialLifecycleStatus.Preparing, inventory.Status);
    }

    public static async Task FixedPreparationUsesOnlyApprovedCopy()
    {
        var speech = new FakeSpeech(TimeSpan.FromSeconds(1));
        var host = new FakeHost(speech);
        var pipeline = new ReusableLinerPipeline(host, new FakeLog());

        ReusableLinerPreparationResult result =
            await pipeline.PrepareAsync(CancellationToken.None);

        TestAssert.Equal(ReusableLinerPreparationStatus.Ready, result.Status);
        TestAssert.Equal("Zoya Radio.", host.PreparedText);
        TestAssert.Equal(1, host.PrepareCalls);
        TestAssert.Equal(0, host.DuckedLinerCalls);
        result.Speech?.Dispose();
    }

    public static async Task DeliveryUsesSharedDuckHostAndPreparedSpeech()
    {
        var speech = new FakeSpeech(TimeSpan.FromSeconds(1));
        var host = new FakeHost(speech);
        var pipeline = new ReusableLinerPipeline(host, new FakeLog());
        using var inventory = new ReusableLinerInventory();
        inventory.Arm(Session(), AtMinutes(0), 0);
        TestAssert.True(inventory.AcceptPreparedSpeech(speech));
        ReusableLinerReservation reservation = inventory.TryReserve(Transition(6, AtMinutes(20)))
            ?? throw new InvalidOperationException("Expected reservation.");

        TransitionDeliveryResult result =
            await pipeline.DeliverAsync(reservation, CancellationToken.None);

        TestAssert.Equal(TransitionDeliveryStatus.Completed, result.Status);
        TestAssert.Equal(1, host.DuckedLinerCalls);
        TestAssert.Equal(1, speech.SpeakCalls);
        TestAssert.Equal(TimeSpan.FromSeconds(4), host.MaximumDuration);
        TestAssert.Equal(AtMinutes(20).AddSeconds(2), host.Deadline);
    }

    public static void DeliverySafetyRequiresExactCurrentPlayingTarget()
    {
        ReusableLinerTransition transition = Transition(6, AtMinutes(20));
        RadioOpportunitySnapshot valid = Snapshot(transition.CurrentTrack);

        TestAssert.True(ReusableLinerDeliverySafety.Evaluate(
            transition, valid, transition.ObservedAt, TimeSpan.FromSeconds(2)).CanProceed);
        TestAssert.False(ReusableLinerDeliverySafety.Evaluate(
            transition,
            valid with { MatchingFirefoxSessionCount = 2 },
            transition.ObservedAt,
            TimeSpan.FromSeconds(2)).CanProceed);
        TestAssert.False(ReusableLinerDeliverySafety.Evaluate(
            transition,
            valid with { CurrentMedia = Track("Other", "Artist") },
            transition.ObservedAt,
            TimeSpan.FromSeconds(2)).CanProceed);
        TestAssert.False(ReusableLinerDeliverySafety.Evaluate(
            transition,
            valid with
            {
                PlaybackStatus =
                    GlobalSystemMediaTransportControlsSessionPlaybackStatus.Paused,
            },
            transition.ObservedAt,
            TimeSpan.FromSeconds(2)).CanProceed);
        TestAssert.False(ReusableLinerDeliverySafety.Evaluate(
            transition,
            valid,
            transition.ObservedAt.AddSeconds(2).AddTicks(1),
            TimeSpan.FromSeconds(2)).CanProceed);
    }

    public static void ConcurrentReuseIsRefusedUntilRelease()
    {
        using ReusableLinerInventory inventory = ReadyInventory();
        ReusableLinerReservation first = inventory.TryReserve(Transition(6, AtMinutes(20)))
            ?? throw new InvalidOperationException("Expected first reservation.");

        TestAssert.True(inventory.TryReserve(Transition(7, AtMinutes(21))) is null);
        TestAssert.True(inventory.Release(first, AtMinutes(21)));
        TestAssert.True(inventory.TryReserve(Transition(8, AtMinutes(22))) is not null);
    }

    public static async Task PreCancelledPreparationCreatesNoPayload()
    {
        var speech = new FakeSpeech(TimeSpan.FromSeconds(1));
        var host = new FakeHost(speech);
        var pipeline = new ReusableLinerPipeline(host, new FakeLog());
        using var cancellation = new CancellationTokenSource();
        cancellation.Cancel();

        ReusableLinerPreparationResult result = await pipeline.PrepareAsync(cancellation.Token);

        TestAssert.Equal(ReusableLinerPreparationStatus.Abandoned, result.Status);
        TestAssert.True(result.Speech is null);
        TestAssert.Equal(0, host.PrepareCalls);
        TestAssert.Equal(0, speech.DisposeCalls);
    }
    private static ReusableLinerInventory ReadyInventory()
    {
        var inventory = new ReusableLinerInventory();
        inventory.Arm(Session(), AtMinutes(0), 0);
        TestAssert.True(inventory.AcceptPreparedSpeech(new FakeSpeech(TimeSpan.FromSeconds(1))));
        return inventory;
    }

    private static EditorialDecision Decide(EditorialAirtimeCandidate candidate, DateTimeOffset observedAt) =>
        new EditorialArbiter(EditorialPolicyProfile.M10CEquivalent).DecideAirtime(
            new EditorialAirtimeInput(
                observedAt,
                [candidate],
                DuckTreatment,
                EditorialPressureSnapshot.Clear));

    private static EditorialPreparationCandidate Preparation(
        string id,
        EditorialContentFormat format,
        EditorialPolicyPriority priority) => new(
        id,
        1,
        format,
        EditorialDeliveryTreatment.DuckOverMusic,
        priority,
        OpportunityMatches: true,
        OpportunityAvailable: true,
        PreparationCapacityAvailable: true);

    private static EditorialAirtimeCandidate ReadyCandidate(
        string id,
        EditorialContentFormat format,
        EditorialPolicyPriority priority) => new(
        id,
        6,
        new PreparedMaterialId($"material-{id}"),
        format,
        EditorialDeliveryTreatment.DuckOverMusic,
        priority,
        OpportunityMatches: true,
        MaterialStatus: PreparedMaterialLifecycleStatus.Ready,
        HasPayload: true,
        BindingMatches: true,
        ExpiresAt: null,
        CandidatePressure: EditorialPressureSnapshot.Clear);

    private static void Has(
        EditorialDecision decision,
        EditorialDecisionReason reason,
        string? candidateId = null) =>
        TestAssert.True(decision.Trace.Any(trace =>
            trace.Reason == reason
            && (candidateId is null || trace.CandidateId == candidateId)));

    private static PreparedMaterialSessionId Session() => new("m11b-test-session");

    private static ReusableLinerTransition Transition(long sequence, DateTimeOffset observedAt) =>
        new(sequence, Track($"Track {sequence}", "Artist"), observedAt, Session());

    private static MediaSnapshot Track(string title, string artist) => new(title, artist, null);

    private static DateTimeOffset AtMinutes(double minutes) =>
        new DateTimeOffset(2026, 8, 24, 12, 0, 0, TimeSpan.Zero).AddMinutes(minutes);

    private static RadioOpportunitySnapshot Snapshot(MediaSnapshot track) => new(
        Armed: true,
        SilentMode: false,
        OpportunityStillPending: true,
        SessionListReadSucceeded: true,
        MatchingFirefoxSessionCount: 1,
        SameSelectedSession: true,
        SourceId: FirefoxSessionSelector.VerifiedSourceId,
        SourceReadSucceeded: true,
        CurrentMedia: track,
        MediaReadSucceeded: true,
        PlaybackStatus: GlobalSystemMediaTransportControlsSessionPlaybackStatus.Playing,
        PlaybackReadSucceeded: true);

    private sealed class FakeSpeech : IPreparedAnnouncementSpeech
    {
        public FakeSpeech(TimeSpan duration)
        {
            MeasuredDuration = duration;
        }

        public TimeSpan MeasuredDuration { get; }
        public int SpeakCalls { get; private set; }
        public int DisposeCalls { get; private set; }

        public Task<SpeechPlaybackResult> SpeakAsync(
            TimeSpan timeout,
            CancellationToken cancellationToken)
        {
            cancellationToken.ThrowIfCancellationRequested();
            SpeakCalls++;
            return Task.FromResult(new SpeechPlaybackResult(SpeechPlaybackStatus.Completed));
        }

        public void Dispose() => DisposeCalls++;
    }

    private sealed class FakeHost : ITransitionRadioHost
    {
        private readonly IPreparedAnnouncementSpeech _speech;

        public FakeHost(IPreparedAnnouncementSpeech speech)
        {
            _speech = speech;
        }

        public string? PreparedText { get; private set; }
        public int PrepareCalls { get; private set; }
        public int DuckedLinerCalls { get; private set; }
        public TimeSpan MaximumDuration { get; private set; }
        public DateTimeOffset Deadline { get; private set; }

        public Task<RadioOpportunitySnapshot> ReadSnapshotAsync(
            long opportunityId,
            CancellationToken cancellationToken) =>
            Task.FromResult(Snapshot(Track("Track", "Artist")));

        public Task<IPreparedAnnouncementSpeech> PrepareSpeechAsync(
            string text,
            CancellationToken cancellationToken)
        {
            cancellationToken.ThrowIfCancellationRequested();
            PreparedText = text;
            PrepareCalls++;
            return Task.FromResult(_speech);
        }

        public Task<TransitionDuckingResult> RunDuckedAnnouncementAsync(
            RadioTransitionDelivery transition,
            IPreparedAnnouncementSpeech preparedSpeech,
            TimeSpan maximumSpeechDuration,
            DateTimeOffset speechStartDeadline,
            bool simulatePostDuckFailure,
            CancellationToken cancellationToken) =>
            throw new InvalidOperationException("Comment delivery was not expected.");

        public async Task<TransitionDuckingResult> RunDuckedLinerAsync(
            ReusableLinerTransition transition,
            IPreparedAnnouncementSpeech preparedSpeech,
            TimeSpan maximumSpeechDuration,
            DateTimeOffset speechStartDeadline,
            CancellationToken cancellationToken)
        {
            DuckedLinerCalls++;
            MaximumDuration = maximumSpeechDuration;
            Deadline = speechStartDeadline;
            await preparedSpeech.SpeakAsync(maximumSpeechDuration, cancellationToken);
            return new TransitionDuckingResult(
                TransitionDuckingStatus.Completed,
                "fake liner completed with restoration",
                null);
        }
    }

    private sealed class FakeLog : IAnnouncementLog
    {
        public void Write(string message) { }
        public void Warning(string context, Exception exception) { }
    }
}
