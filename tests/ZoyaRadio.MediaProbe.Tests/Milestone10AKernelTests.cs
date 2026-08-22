using ZoyaRadio.MediaProbe;

internal static class Milestone10AKernelTests
{
    public static void ThresholdTwoGoldenTraceMatchesLegacy()
    {
        RadioBehaviorConfiguration configuration = RadioBehaviorConfiguration.Default;
        var legacyTargets = new LegacyTargetSource(2, 5);
        var legacy = new RadioBehaviorState(configuration, legacyTargets);
        DeterministicOpportunityKernel kernel = CreateKernel(configuration);
        OpportunityKernelState candidate = OpportunityKernelState.Initial;
        DateTimeOffset now = At(0);
        MediaSnapshot initial = Track("Initial", "Artist 0");

        legacy.Arm(initial, now);
        OpportunityKernelResult armed = Step(
            kernel,
            ref candidate,
            new OpportunityKernelArm(initial, now, 2));
        AssertParity(legacy, candidate);
        HasDecision(armed, OpportunityKernelDecisionReason.Armed);
        FreshTargetIs(armed, 2);

        MediaSnapshot subject = Track("Track A", "Artist A");
        RadioTrackObservation legacyPreparation = legacy.ObserveTrack(subject, At(10));
        OpportunityKernelResult preparation = Step(
            kernel,
            ref candidate,
            new OpportunityKernelObserveTrack(subject, At(10)));
        AssertParity(legacy, candidate);
        OpportunityKernelPreparation candidatePreparation = Preparation(preparation);
        TestAssert.Equal(legacyPreparation.PreparationOpportunity!.Id, candidatePreparation.Id);
        SequenceEqual(
            legacyPreparation.TrustedListeningObservations,
            TransitionObservations(preparation));
        TestAssert.Equal(
            legacyPreparation.PreparationOpportunity.Context.SubjectTrack,
            candidatePreparation.SubjectTrack);
        SequenceEqual(
            legacyPreparation.PreparationOpportunity.Context.TrustedListeningObservations,
            candidatePreparation.TrustedListeningObservations);

        TestAssert.True(legacy.MarkPreparationReady(legacyPreparation.PreparationOpportunity.Id));
        OpportunityKernelResult ready = Step(
            kernel,
            ref candidate,
            new OpportunityKernelMarkPreparationReady(candidatePreparation.Id));
        HasDecision(ready, OpportunityKernelDecisionReason.PreparationMarkedReady);

        MediaSnapshot next = Track("Track B", "Artist B");
        RadioTrackObservation legacyDelivery = legacy.ObserveTrack(next, At(20));
        OpportunityKernelResult delivery = Step(
            kernel,
            ref candidate,
            new OpportunityKernelObserveTrack(next, At(20)));
        AssertParity(legacy, candidate);
        OpportunityKernelDelivery candidateDelivery = Delivery(delivery);
        SequenceEqual(
            legacyDelivery.TrustedListeningObservations,
            TransitionObservations(delivery));
        TestAssert.Equal(legacyDelivery.Delivery!.OpportunityId, candidateDelivery.OpportunityId);
        TestAssert.Equal(legacyDelivery.Delivery.CommentedTrack, candidateDelivery.CommentedTrack);
        TestAssert.Equal(legacyDelivery.Delivery.NewTrack, candidateDelivery.NewTrack);
        TestAssert.Equal(legacyDelivery.Delivery.TransitionObservedAt, candidateDelivery.TransitionObservedAt);

        const string acceptedComment = "A sufficiently long accepted comment for parity history.";
        TestAssert.True(legacy.ResolveOpportunity(
            legacyDelivery.Delivery.OpportunityId,
            true,
            acceptedComment));
        OpportunityKernelResult resolved = Step(
            kernel,
            ref candidate,
            new OpportunityKernelResolveOpportunity(
                candidateDelivery.OpportunityId,
                OpportunityKernelResolutionStatus.Completed,
                acceptedComment,
                5));
        AssertParity(legacy, candidate);
        HasDecision(resolved, OpportunityKernelDecisionReason.OpportunityResolvedCompleted);
        FreshTargetIs(resolved, 5);
        TestAssert.Equal((2, 5), legacyTargets.Requests[0]);
        TestAssert.Equal((2, 5), legacyTargets.Requests[1]);
    }

    public static void ThresholdFivePreparesOnlyOneAhead()
    {
        var legacy = new RadioBehaviorState(
            RadioBehaviorConfiguration.Default,
            new LegacyTargetSource(5));
        DeterministicOpportunityKernel kernel = CreateKernel();
        OpportunityKernelState candidate = OpportunityKernelState.Initial;
        legacy.Arm(Track("Initial", "Artist 0"), At(0));
        Step(kernel, ref candidate, new OpportunityKernelArm(Track("Initial", "Artist 0"), At(0), 5));

        for (int transition = 1; transition <= 3; transition++)
        {
            MediaSnapshot track = Track($"Track {transition}", $"Artist {transition}");
            RadioTrackObservation legacyObservation = legacy.ObserveTrack(track, At(transition * 10));
            OpportunityKernelResult candidateObservation = Step(
                kernel,
                ref candidate,
                new OpportunityKernelObserveTrack(track, At(transition * 10)));
            TestAssert.True(legacyObservation.PreparationOpportunity is null);
            TestAssert.False(HasDecisionOptional(
                candidateObservation,
                OpportunityKernelDecisionReason.PreparationRequested));
            AssertParity(legacy, candidate);
        }

        MediaSnapshot subject = Track("Track 4", "Artist 4");
        RadioTrackObservation legacyPreparation = legacy.ObserveTrack(subject, At(40));
        OpportunityKernelResult candidatePreparation = Step(
            kernel,
            ref candidate,
            new OpportunityKernelObserveTrack(subject, At(40)));

        TestAssert.True(legacyPreparation.PreparationOpportunity is not null);
        TestAssert.Equal(4, candidate.TransitionCounter);
        TestAssert.Equal(subject, Preparation(candidatePreparation).SubjectTrack);
        AssertParity(legacy, candidate);
    }

    public static void DuplicateAndUnusableObservationsDoNotAdvance()
    {
        var legacy = new RadioBehaviorState(
            RadioBehaviorConfiguration.Default,
            new LegacyTargetSource(5));
        DeterministicOpportunityKernel kernel = CreateKernel();
        OpportunityKernelState candidate = OpportunityKernelState.Initial;
        legacy.Arm(Track("Initial", "Artist 0"), At(0));
        Step(kernel, ref candidate, new OpportunityKernelArm(Track("Initial", "Artist 0"), At(0), 5));

        MediaSnapshot current = Track("Track B", "Artist B");
        legacy.ObserveTrack(current, At(10));
        Step(kernel, ref candidate, new OpportunityKernelObserveTrack(current, At(10)));

        var duplicate = new MediaSnapshot("  track   b ", "ARTIST B", "Later album");
        RadioTrackObservation legacyDuplicate = legacy.ObserveTrack(duplicate, At(11));
        OpportunityKernelResult candidateDuplicate = Step(
            kernel,
            ref candidate,
            new OpportunityKernelObserveTrack(duplicate, At(11)));
        TestAssert.Equal(RadioTrackObservationKind.Duplicate, legacyDuplicate.Kind);
        HasDecision(candidateDuplicate, OpportunityKernelDecisionReason.DuplicateTrackIgnored);
        AssertParity(legacy, candidate);

        var unusable = new MediaSnapshot("No artist", null, null);
        RadioTrackObservation legacyUnusable = legacy.ObserveTrack(unusable, At(12));
        OpportunityKernelResult candidateUnusable = Step(
            kernel,
            ref candidate,
            new OpportunityKernelObserveTrack(unusable, At(12)));
        TestAssert.Equal(RadioTrackObservationKind.Unusable, legacyUnusable.Kind);
        HasDecision(candidateUnusable, OpportunityKernelDecisionReason.UnusableTrackIgnored);
        AssertParity(legacy, candidate);
        TestAssert.Equal(1, candidate.TransitionCounter);
        TestAssert.Equal(1, candidate.TotalObservedTransitions);
    }

    public static void MissedPreparationStartsFreshSchedule()
    {
        var legacy = new RadioBehaviorState(
            RadioBehaviorConfiguration.Default,
            new LegacyTargetSource(2, 4));
        DeterministicOpportunityKernel kernel = CreateKernel();
        OpportunityKernelState candidate = OpportunityKernelState.Initial;
        legacy.Arm(Track("Initial", "Artist 0"), At(0));
        Step(kernel, ref candidate, new OpportunityKernelArm(Track("Initial", "Artist 0"), At(0), 2));

        RadioTrackObservation legacyPreparation = legacy.ObserveTrack(Track("Track A", "Artist A"), At(10));
        Step(kernel, ref candidate, new OpportunityKernelObserveTrack(Track("Track A", "Artist A"), At(10)));
        TestAssert.True(legacyPreparation.PreparationOpportunity is not null);

        RadioTrackObservation legacyMiss = legacy.ObserveTrack(Track("Track B", "Artist B"), At(20));
        OpportunityKernelResult candidateMiss = Step(
            kernel,
            ref candidate,
            new OpportunityKernelObserveTrack(Track("Track B", "Artist B"), At(20), 4));

        TestAssert.True(legacyMiss.PreparationMissedTransition);
        TestAssert.True(legacyMiss.PendingOpportunityInvalidated);
        HasDecision(candidateMiss, OpportunityKernelDecisionReason.PreparationMissedTransition);
        HasDecision(candidateMiss, OpportunityKernelDecisionReason.PendingOpportunityInvalidated);
        FreshTargetIs(candidateMiss, 4);
        AssertParity(legacy, candidate);
    }

    public static void RapidTransitionInvalidatesUnresolvedDelivery()
    {
        var legacy = new RadioBehaviorState(
            RadioBehaviorConfiguration.Default,
            new LegacyTargetSource(2, 5));
        DeterministicOpportunityKernel kernel = CreateKernel();
        OpportunityKernelState candidate = OpportunityKernelState.Initial;
        legacy.Arm(Track("Initial", "Artist 0"), At(0));
        Step(kernel, ref candidate, new OpportunityKernelArm(Track("Initial", "Artist 0"), At(0), 2));

        RadioTrackObservation legacyPreparation = legacy.ObserveTrack(Track("Track A", "Artist A"), At(10));
        OpportunityKernelResult candidatePreparation = Step(
            kernel,
            ref candidate,
            new OpportunityKernelObserveTrack(Track("Track A", "Artist A"), At(10)));
        long opportunityId = Preparation(candidatePreparation).Id;
        TestAssert.True(legacy.MarkPreparationReady(legacyPreparation.PreparationOpportunity!.Id));
        Step(kernel, ref candidate, new OpportunityKernelMarkPreparationReady(opportunityId));

        RadioTrackObservation legacyDelivery = legacy.ObserveTrack(Track("Track B", "Artist B"), At(20));
        OpportunityKernelResult candidateDelivery = Step(
            kernel,
            ref candidate,
            new OpportunityKernelObserveTrack(Track("Track B", "Artist B"), At(20)));
        TestAssert.True(legacyDelivery.Delivery is not null);
        TestAssert.Equal(opportunityId, Delivery(candidateDelivery).OpportunityId);

        RadioTrackObservation legacyRapid = legacy.ObserveTrack(Track("Track C", "Artist C"), At(21));
        OpportunityKernelResult candidateRapid = Step(
            kernel,
            ref candidate,
            new OpportunityKernelObserveTrack(Track("Track C", "Artist C"), At(21), 5));
        TestAssert.True(legacyRapid.PendingOpportunityInvalidated);
        HasDecision(candidateRapid, OpportunityKernelDecisionReason.RapidTransitionInvalidatedDelivery);
        HasDecision(candidateRapid, OpportunityKernelDecisionReason.PendingOpportunityInvalidated);
        FreshTargetIs(candidateRapid, 5);
        AssertParity(legacy, candidate);
    }

    public static void StaleReadyAndResolveAreRefusedWithoutReschedule()
    {
        var legacyTargets = new LegacyTargetSource(2, 5);
        var legacy = new RadioBehaviorState(RadioBehaviorConfiguration.Default, legacyTargets);
        DeterministicOpportunityKernel kernel = CreateKernel();
        OpportunityKernelState candidate = OpportunityKernelState.Initial;
        legacy.Arm(Track("Initial", "Artist 0"), At(0));
        Step(kernel, ref candidate, new OpportunityKernelArm(Track("Initial", "Artist 0"), At(0), 2));

        RadioTrackObservation legacyPreparation = legacy.ObserveTrack(Track("Track A", "Artist A"), At(10));
        OpportunityKernelResult candidatePreparation = Step(
            kernel,
            ref candidate,
            new OpportunityKernelObserveTrack(Track("Track A", "Artist A"), At(10)));
        long opportunityId = Preparation(candidatePreparation).Id;

        TestAssert.False(legacy.MarkPreparationReady(999));
        OpportunityKernelResult staleReady = Step(
            kernel,
            ref candidate,
            new OpportunityKernelMarkPreparationReady(999));
        HasDecision(staleReady, OpportunityKernelDecisionReason.StaleOpportunityRefused);
        AssertParity(legacy, candidate);
        TestAssert.Equal(1, legacyTargets.Requests.Count);

        TestAssert.True(legacy.MarkPreparationReady(legacyPreparation.PreparationOpportunity!.Id));
        Step(kernel, ref candidate, new OpportunityKernelMarkPreparationReady(opportunityId));
        legacy.ObserveTrack(Track("Track B", "Artist B"), At(20));
        Step(kernel, ref candidate, new OpportunityKernelObserveTrack(Track("Track B", "Artist B"), At(20)));

        TestAssert.False(legacy.ResolveOpportunity(999, true, "This must not enter history."));
        OpportunityKernelResult staleResolve = Step(
            kernel,
            ref candidate,
            new OpportunityKernelResolveOpportunity(
                999,
                OpportunityKernelResolutionStatus.Completed,
                "This must not enter history."));
        HasDecision(staleResolve, OpportunityKernelDecisionReason.StaleOpportunityRefused);
        AssertParity(legacy, candidate);
        TestAssert.Equal(1, legacyTargets.Requests.Count);

        TestAssert.True(legacy.ResolveOpportunity(opportunityId, false, null));
        OpportunityKernelResult abandoned = Step(
            kernel,
            ref candidate,
            new OpportunityKernelResolveOpportunity(
                opportunityId,
                OpportunityKernelResolutionStatus.Abandoned,
                null,
                5));
        HasDecision(abandoned, OpportunityKernelDecisionReason.OpportunityResolvedAbandoned);
        AssertParity(legacy, candidate);
    }

    public static void FailedResolutionIsTypedAndDoesNotCommitComment()
    {
        var legacy = new RadioBehaviorState(
            RadioBehaviorConfiguration.Default,
            new LegacyTargetSource(2, 4));
        DeterministicOpportunityKernel kernel = CreateKernel();
        OpportunityKernelState candidate = OpportunityKernelState.Initial;
        legacy.Arm(Track("Initial", "Artist 0"), At(0));
        Step(kernel, ref candidate, new OpportunityKernelArm(Track("Initial", "Artist 0"), At(0), 2));

        RadioTrackObservation legacyPreparation = legacy.ObserveTrack(Track("Track A", "Artist A"), At(10));
        OpportunityKernelResult candidatePreparation = Step(
            kernel,
            ref candidate,
            new OpportunityKernelObserveTrack(Track("Track A", "Artist A"), At(10)));
        long opportunityId = Preparation(candidatePreparation).Id;
        TestAssert.True(legacy.MarkPreparationReady(legacyPreparation.PreparationOpportunity!.Id));
        Step(kernel, ref candidate, new OpportunityKernelMarkPreparationReady(opportunityId));
        legacy.ObserveTrack(Track("Track B", "Artist B"), At(20));
        Step(kernel, ref candidate, new OpportunityKernelObserveTrack(Track("Track B", "Artist B"), At(20)));

        TestAssert.True(legacy.ResolveOpportunity(
            opportunityId,
            false,
            "A failed delivery must not commit this comment."));
        OpportunityKernelResult failed = Step(
            kernel,
            ref candidate,
            new OpportunityKernelResolveOpportunity(
                opportunityId,
                OpportunityKernelResolutionStatus.Failed,
                "A failed delivery must not commit this comment.",
                4));
        HasDecision(failed, OpportunityKernelDecisionReason.OpportunityResolvedFailed);
        TestAssert.Equal(0, candidate.RecentAcceptedAiComments.Count);
        AssertParity(legacy, candidate);
    }

    public static void SilentModeInvalidatesAndResumesFresh()
    {
        var legacy = new RadioBehaviorState(
            RadioBehaviorConfiguration.Default,
            new LegacyTargetSource(2, 4));
        DeterministicOpportunityKernel kernel = CreateKernel();
        OpportunityKernelState candidate = OpportunityKernelState.Initial;
        legacy.Arm(Track("Initial", "Artist 0"), At(0));
        Step(kernel, ref candidate, new OpportunityKernelArm(Track("Initial", "Artist 0"), At(0), 2));
        legacy.ObserveTrack(Track("Track A", "Artist A"), At(10));
        Step(kernel, ref candidate, new OpportunityKernelObserveTrack(Track("Track A", "Artist A"), At(10)));

        RadioModeChange legacyEnabled = legacy.SetSilentMode(true);
        OpportunityKernelResult enabled = Step(
            kernel,
            ref candidate,
            new OpportunityKernelSetSilentMode(true));
        TestAssert.True(legacyEnabled.PendingOpportunityInvalidated);
        HasDecision(enabled, OpportunityKernelDecisionReason.PendingOpportunityInvalidated);
        HasDecision(enabled, OpportunityKernelDecisionReason.SilentModeEnabled);
        AssertParity(legacy, candidate);

        RadioModeChange legacyUnchanged = legacy.SetSilentMode(true);
        OpportunityKernelResult unchanged = Step(
            kernel,
            ref candidate,
            new OpportunityKernelSetSilentMode(true));
        TestAssert.False(legacyUnchanged.PendingOpportunityInvalidated);
        HasDecision(unchanged, OpportunityKernelDecisionReason.SilentModeUnchanged);
        AssertParity(legacy, candidate);

        legacy.ObserveTrack(Track("Track B", "Artist B"), At(20));
        Step(kernel, ref candidate, new OpportunityKernelObserveTrack(Track("Track B", "Artist B"), At(20)));
        TestAssert.Equal(0, candidate.TransitionCounter);

        legacy.SetSilentMode(false);
        OpportunityKernelResult disabled = Step(
            kernel,
            ref candidate,
            new OpportunityKernelSetSilentMode(false, 4));
        HasDecision(disabled, OpportunityKernelDecisionReason.SilentModeDisabled);
        FreshTargetIs(disabled, 4);
        AssertParity(legacy, candidate);
    }

    public static void ClearHistoryMatchesLegacyWithAndWithoutPending()
    {
        var legacy = new RadioBehaviorState(
            RadioBehaviorConfiguration.Default,
            new LegacyTargetSource(2, 5, 3));
        DeterministicOpportunityKernel kernel = CreateKernel();
        OpportunityKernelState candidate = OpportunityKernelState.Initial;
        legacy.Arm(Track("Initial", "Artist 0"), At(0));
        Step(kernel, ref candidate, new OpportunityKernelArm(Track("Initial", "Artist 0"), At(0), 2));
        legacy.ObserveTrack(Track("Track A", "Artist A"), At(10));
        Step(kernel, ref candidate, new OpportunityKernelObserveTrack(Track("Track A", "Artist A"), At(10)));

        TestAssert.True(legacy.ClearRecentHistory());
        OpportunityKernelResult firstClear = Step(
            kernel,
            ref candidate,
            new OpportunityKernelClearRecentHistory(5));
        HasDecision(firstClear, OpportunityKernelDecisionReason.PendingOpportunityInvalidated);
        HasDecision(firstClear, OpportunityKernelDecisionReason.RecentHistoryCleared);
        FreshTargetIs(firstClear, 5);
        AssertParity(legacy, candidate);

        TestAssert.False(legacy.ClearRecentHistory());
        OpportunityKernelResult secondClear = Step(
            kernel,
            ref candidate,
            new OpportunityKernelClearRecentHistory(3));
        TestAssert.False(HasDecisionOptional(
            secondClear,
            OpportunityKernelDecisionReason.PendingOpportunityInvalidated));
        FreshTargetIs(secondClear, 3);
        AssertParity(legacy, candidate);
    }

    public static void RebindMatchesLegacyWithAndWithoutPending()
    {
        var legacy = new RadioBehaviorState(
            RadioBehaviorConfiguration.Default,
            new LegacyTargetSource(2, 4, 5));
        DeterministicOpportunityKernel kernel = CreateKernel();
        OpportunityKernelState candidate = OpportunityKernelState.Initial;
        legacy.Arm(Track("Initial", "Artist 0"), At(0));
        Step(kernel, ref candidate, new OpportunityKernelArm(Track("Initial", "Artist 0"), At(0), 2));
        legacy.ObserveTrack(Track("Track A", "Artist A"), At(10));
        Step(kernel, ref candidate, new OpportunityKernelObserveTrack(Track("Track A", "Artist A"), At(10)));

        MediaSnapshot reboundTrack = Track("Rebound", "Artist R");
        TestAssert.True(legacy.RebindCurrentTrack(reboundTrack, At(15)));
        OpportunityKernelResult firstRebind = Step(
            kernel,
            ref candidate,
            new OpportunityKernelRebindCurrentTrack(reboundTrack, At(15), 4));
        HasDecision(firstRebind, OpportunityKernelDecisionReason.PendingOpportunityInvalidated);
        HasDecision(firstRebind, OpportunityKernelDecisionReason.CurrentTrackRebound);
        AssertParity(legacy, candidate);

        MediaSnapshot secondTrack = Track("Rebound Again", "Artist R2");
        TestAssert.False(legacy.RebindCurrentTrack(secondTrack, At(16)));
        OpportunityKernelResult secondRebind = Step(
            kernel,
            ref candidate,
            new OpportunityKernelRebindCurrentTrack(secondTrack, At(16), 5));
        TestAssert.False(HasDecisionOptional(
            secondRebind,
            OpportunityKernelDecisionReason.PendingOpportunityInvalidated));
        AssertParity(legacy, candidate);
    }

    public static void ExplicitCancellationMatchesLegacyAndNoOpIsStable()
    {
        var legacyTargets = new LegacyTargetSource(2, 4);
        var legacy = new RadioBehaviorState(RadioBehaviorConfiguration.Default, legacyTargets);
        DeterministicOpportunityKernel kernel = CreateKernel();
        OpportunityKernelState candidate = OpportunityKernelState.Initial;
        legacy.Arm(Track("Initial", "Artist 0"), At(0));
        Step(kernel, ref candidate, new OpportunityKernelArm(Track("Initial", "Artist 0"), At(0), 2));
        legacy.ObserveTrack(Track("Track A", "Artist A"), At(10));
        Step(kernel, ref candidate, new OpportunityKernelObserveTrack(Track("Track A", "Artist A"), At(10)));

        TestAssert.True(legacy.CancelPendingOpportunityAndReschedule());
        OpportunityKernelResult cancelled = Step(
            kernel,
            ref candidate,
            new OpportunityKernelCancelPendingOpportunity(4));
        HasDecision(cancelled, OpportunityKernelDecisionReason.PendingOpportunityCancelled);
        FreshTargetIs(cancelled, 4);
        AssertParity(legacy, candidate);

        TestAssert.False(legacy.CancelPendingOpportunityAndReschedule());
        OpportunityKernelResult noOp = Step(
            kernel,
            ref candidate,
            new OpportunityKernelCancelPendingOpportunity());
        HasDecision(noOp, OpportunityKernelDecisionReason.NoPendingOpportunityToCancel);
        TestAssert.Equal(2, legacyTargets.Requests.Count);
        AssertParity(legacy, candidate);
    }

    public static void DisarmAndRearmPreserveMonotonicOpportunityId()
    {
        var legacy = new RadioBehaviorState(
            RadioBehaviorConfiguration.Default,
            new LegacyTargetSource(2, 3));
        DeterministicOpportunityKernel kernel = CreateKernel();
        OpportunityKernelState candidate = OpportunityKernelState.Initial;
        legacy.Arm(Track("Initial", "Artist 0"), At(0));
        Step(kernel, ref candidate, new OpportunityKernelArm(Track("Initial", "Artist 0"), At(0), 2));
        RadioTrackObservation firstLegacy = legacy.ObserveTrack(Track("Track A", "Artist A"), At(10));
        OpportunityKernelResult firstCandidate = Step(
            kernel,
            ref candidate,
            new OpportunityKernelObserveTrack(Track("Track A", "Artist A"), At(10)));
        TestAssert.Equal(1L, firstLegacy.PreparationOpportunity!.Id);
        TestAssert.Equal(1L, Preparation(firstCandidate).Id);

        legacy.Disarm();
        OpportunityKernelResult disarmed = Step(kernel, ref candidate, new OpportunityKernelDisarm());
        HasDecision(disarmed, OpportunityKernelDecisionReason.PendingOpportunityInvalidated);
        HasDecision(disarmed, OpportunityKernelDecisionReason.Disarmed);
        AssertParity(legacy, candidate);

        MediaSnapshot whileDisarmed = Track("Observed While Disarmed", "Artist X");
        legacy.ObserveTrack(whileDisarmed, At(20));
        Step(kernel, ref candidate, new OpportunityKernelObserveTrack(whileDisarmed, At(20)));
        TestAssert.Equal(1, candidate.TransitionCounter);
        AssertParity(legacy, candidate);

        MediaSnapshot rearmedTrack = Track("Rearmed", "Artist R");
        legacy.Arm(rearmedTrack, At(30));
        Step(kernel, ref candidate, new OpportunityKernelArm(rearmedTrack, At(30), 3));
        legacy.ObserveTrack(Track("After 1", "Artist 1"), At(40));
        Step(kernel, ref candidate, new OpportunityKernelObserveTrack(Track("After 1", "Artist 1"), At(40)));
        RadioTrackObservation secondLegacy = legacy.ObserveTrack(Track("After 2", "Artist 2"), At(50));
        OpportunityKernelResult secondCandidate = Step(
            kernel,
            ref candidate,
            new OpportunityKernelObserveTrack(Track("After 2", "Artist 2"), At(50)));
        TestAssert.Equal(2L, secondLegacy.PreparationOpportunity!.Id);
        TestAssert.Equal(2L, Preparation(secondCandidate).Id);
        AssertParity(legacy, candidate);
    }

    public static void BoundedHistoryAndCommentsMatchLegacy()
    {
        RadioBehaviorConfiguration configuration = RadioBehaviorConfiguration.Default with
        {
            RecentTrackLimit = 3,
            RecentCommentLimit = 2,
        };
        var legacy = new RadioBehaviorState(
            configuration,
            new LegacyTargetSource(2, 2, 2, 2));
        DeterministicOpportunityKernel kernel = CreateKernel(configuration);
        OpportunityKernelState candidate = OpportunityKernelState.Initial;
        legacy.Arm(Track("Initial", "Artist 0"), At(0));
        Step(kernel, ref candidate, new OpportunityKernelArm(Track("Initial", "Artist 0"), At(0), 2));

        for (int cycle = 1; cycle <= 3; cycle++)
        {
            MediaSnapshot subject = Track($"Subject {cycle}", "Repeated Artist");
            MediaSnapshot next = Track($"Next {cycle}", $"Next Artist {cycle}");
            RadioTrackObservation legacyPreparation = legacy.ObserveTrack(subject, At(cycle * 20 - 10));
            OpportunityKernelResult candidatePreparation = Step(
                kernel,
                ref candidate,
                new OpportunityKernelObserveTrack(subject, At(cycle * 20 - 10)));
            long opportunityId = Preparation(candidatePreparation).Id;
            TestAssert.True(legacy.MarkPreparationReady(legacyPreparation.PreparationOpportunity!.Id));
            Step(kernel, ref candidate, new OpportunityKernelMarkPreparationReady(opportunityId));
            legacy.ObserveTrack(next, At(cycle * 20));
            Step(kernel, ref candidate, new OpportunityKernelObserveTrack(next, At(cycle * 20)));
            string comment = $"Accepted comment number {cycle} has enough words for history.";
            TestAssert.True(legacy.ResolveOpportunity(opportunityId, true, comment));
            Step(
                kernel,
                ref candidate,
                new OpportunityKernelResolveOpportunity(
                    opportunityId,
                    OpportunityKernelResolutionStatus.Completed,
                    comment,
                    2));
            AssertParity(legacy, candidate);
        }

        TestAssert.Equal(3, candidate.RecentTracks.Count);
        TestAssert.Equal(2, candidate.RecentAcceptedAiComments.Count);
        TestAssert.Equal(
            "Accepted comment number 2 has enough words for history.",
            candidate.RecentAcceptedAiComments[0]);
        TestAssert.Equal(
            "Accepted comment number 3 has enough words for history.",
            candidate.RecentAcceptedAiComments[1]);
    }

    public static void InclusiveTargetsAndOutOfRangeValuesAreDeterministic()
    {
        DeterministicOpportunityKernel kernel = CreateKernel();
        OpportunityKernelState initial = OpportunityKernelState.Initial;

        OpportunityKernelResult minimum = kernel.Apply(
            initial,
            new OpportunityKernelArm(Track("Minimum", "Artist"), At(0), 2));
        TestAssert.Equal(2, minimum.State.ScheduledThreshold);

        OpportunityKernelResult maximum = kernel.Apply(
            initial,
            new OpportunityKernelArm(Track("Maximum", "Artist"), At(0), 5));
        TestAssert.Equal(5, maximum.State.ScheduledThreshold);

        Throws<InvalidOperationException>(() => kernel.Apply(
            initial,
            new OpportunityKernelArm(Track("Low", "Artist"), At(0), 1)));
        Throws<InvalidOperationException>(() => kernel.Apply(
            initial,
            new OpportunityKernelArm(Track("High", "Artist"), At(0), 6)));
        TestAssert.Equal(OpportunityKernelState.Initial, initial);

        Throws<InvalidOperationException>(() => new DeterministicOpportunityKernel(
            new OpportunityKernelConfiguration(1, 5, 10, 6)));
        Throws<InvalidOperationException>(() => new DeterministicOpportunityKernel(
            new OpportunityKernelConfiguration(5, 4, 10, 6)));
    }

    private static DeterministicOpportunityKernel CreateKernel(
        RadioBehaviorConfiguration? configuration = null)
    {
        return new DeterministicOpportunityKernel(
            OpportunityKernelConfiguration.FromRadioBehavior(
                configuration ?? RadioBehaviorConfiguration.Default));
    }

    private static OpportunityKernelResult Step(
        DeterministicOpportunityKernel kernel,
        ref OpportunityKernelState state,
        OpportunityKernelInput input)
    {
        OpportunityKernelResult result = kernel.Apply(state, input);
        state = result.State;
        return result;
    }

    private static void AssertParity(
        RadioBehaviorState legacy,
        OpportunityKernelState candidate)
    {
        RadioRuntimeState runtime = legacy.GetRuntimeState();
        TestAssert.Equal(legacy.Armed, candidate.Armed);
        TestAssert.Equal(legacy.SilentMode, candidate.SilentMode);
        TestAssert.Equal(legacy.TransitionCounter, candidate.TransitionCounter);
        TestAssert.Equal(legacy.ScheduledThreshold, candidate.ScheduledThreshold);
        TestAssert.Equal(legacy.TotalObservedTransitions, candidate.TotalObservedTransitions);
        TestAssert.Equal(runtime.CurrentTrack, candidate.CurrentTrack);
        TestAssert.Equal(runtime.CurrentTrackStartedAt, candidate.CurrentTrackStartedAt);
        TestAssert.Equal(runtime.PendingOpportunityId, candidate.PendingOpportunity?.Id);
        SequenceEqual(legacy.RecentTracks, candidate.RecentTracks);
        SequenceEqual(legacy.RecentAcceptedAiComments, candidate.RecentAcceptedAiComments);
    }

    private static OpportunityKernelPreparation Preparation(OpportunityKernelResult result)
    {
        return result.Decisions.Single(
            decision => decision.Reason == OpportunityKernelDecisionReason.PreparationRequested)
            .Preparation!;
    }

    private static OpportunityKernelDelivery Delivery(OpportunityKernelResult result)
    {
        return result.Decisions.Single(
            decision => decision.Reason == OpportunityKernelDecisionReason.DeliveryDue)
            .Delivery!;
    }

    private static IReadOnlyList<string> TransitionObservations(
        OpportunityKernelResult result)
    {
        return result.Decisions.Single(
            decision => decision.Reason == OpportunityKernelDecisionReason.TransitionObserved)
            .TrustedListeningObservations
            ?? throw new InvalidOperationException("Transition observations were missing.");
    }

    private static void FreshTargetIs(OpportunityKernelResult result, int expected)
    {
        OpportunityKernelDecision decision = result.Decisions.Single(
            item => item.Reason == OpportunityKernelDecisionReason.FreshScheduleSelected);
        TestAssert.Equal<int?>(expected, decision.ScheduledThreshold);
    }

    private static void HasDecision(
        OpportunityKernelResult result,
        OpportunityKernelDecisionReason reason)
    {
        TestAssert.True(HasDecisionOptional(result, reason));
    }

    private static bool HasDecisionOptional(
        OpportunityKernelResult result,
        OpportunityKernelDecisionReason reason)
    {
        return result.Decisions.Any(decision => decision.Reason == reason);
    }

    private static void SequenceEqual<T>(
        IReadOnlyList<T> expected,
        IReadOnlyList<T> actual)
    {
        TestAssert.Equal(expected.Count, actual.Count);
        for (int index = 0; index < expected.Count; index++)
        {
            TestAssert.Equal(expected[index], actual[index]);
        }
    }

    private static void Throws<TException>(Action action)
        where TException : Exception
    {
        try
        {
            action();
        }
        catch (TException)
        {
            return;
        }

        throw new InvalidOperationException($"Expected {typeof(TException).Name}.");
    }

    private static DateTimeOffset At(int seconds)
    {
        return DateTimeOffset.Parse("2026-08-21T12:00:00Z").AddSeconds(seconds);
    }

    private static MediaSnapshot Track(string title, string artist)
    {
        return new MediaSnapshot(title, artist, null);
    }

    private sealed class LegacyTargetSource(params int[] values) : ITransitionTargetSource
    {
        private readonly Queue<int> _values = new(values);

        public List<(int Minimum, int Maximum)> Requests { get; } = [];

        public int NextInclusive(int minimum, int maximum)
        {
            Requests.Add((minimum, maximum));
            if (_values.Count == 0)
            {
                throw new InvalidOperationException("No legacy transition target remained for the trace.");
            }

            return _values.Dequeue();
        }
    }
}
