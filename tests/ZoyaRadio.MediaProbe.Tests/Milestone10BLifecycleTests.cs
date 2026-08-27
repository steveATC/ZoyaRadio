using System.Collections.ObjectModel;

using ZoyaRadio.MediaProbe;

internal static class Milestone10BLifecycleTests
{
    public static void PreparingAttachReserveCompleteDisposesOnce()
    {
        PreparedMaterialLifecycle lifecycle = new();
        PreparedMaterialLifecycleState state = PreparedMaterialLifecycleState.Initial;
        PreparedMaterialId materialId = MaterialId("comment-1");
        PreparedPayloadHandle payload = Payload("speech-1");
        PreparedMaterialReservationId reservation = Reservation("air-1");
        MediaSnapshot track = Track("Track A", "Artist A");
        PreparedMaterialSessionId session = Session("firefox-session");
        PreparedMaterialBinding binding = SingleBinding(1, track, session, At(100));

        PreparedMaterialLifecycleResult registered = Step(
            lifecycle,
            ref state,
            new PreparedMaterialRegister(
                materialId,
                PreparedMaterialReusePolicy.SingleOpportunity,
                binding,
                At(0)));
        Has(registered, PreparedMaterialDecisionReason.RegisteredPreparing);
        TestAssert.Equal(PreparedMaterialLifecycleStatus.Preparing, Material(state, materialId).Status);

        PreparedMaterialLifecycleResult attached = Step(
            lifecycle,
            ref state,
            new PreparedMaterialAttachPayload(materialId, payload));
        Has(attached, PreparedMaterialDecisionReason.PreparationReady);
        Has(attached, PreparedMaterialDecisionReason.PayloadOwnershipAccepted);
        TestAssert.Equal(payload, Material(state, materialId).PayloadHandle);

        PreparedMaterialUseContext context = Context(10, 1, track, session);
        PreparedMaterialLifecycleResult reserved = Step(
            lifecycle,
            ref state,
            new PreparedMaterialReserve(materialId, reservation, context));
        Has(reserved, PreparedMaterialDecisionReason.ReservationAcquired);
        TestAssert.Equal(PreparedMaterialLifecycleStatus.Reserved, Material(state, materialId).Status);

        PreparedMaterialLifecycleResult completed = Step(
            lifecycle,
            ref state,
            new PreparedMaterialComplete(
                materialId,
                reservation,
                PreparedMaterialCompletionDisposition.RetireOrConsume,
                Context(20, 1, track, session)));
        Has(completed, PreparedMaterialDecisionReason.CompletedRetired);
        Has(completed, PreparedMaterialDecisionReason.PayloadDisposalRequested);
        TestAssert.Equal(1, Count(completed, PreparedMaterialDecisionReason.PayloadDisposalRequested));
        TestAssert.Equal(PreparedMaterialLifecycleStatus.Terminal, Material(state, materialId).Status);
        TestAssert.Equal(PreparedMaterialTerminalReason.Completed, Material(state, materialId).TerminalReason);
        TestAssert.Equal<PreparedPayloadHandle?>(null, Material(state, materialId).PayloadHandle);

        PreparedMaterialLifecycleState terminalState = state;
        PreparedMaterialLifecycleResult repeated = Step(
            lifecycle,
            ref state,
            new PreparedMaterialComplete(
                materialId,
                reservation,
                PreparedMaterialCompletionDisposition.RetireOrConsume,
                Context(21, 1, track, session)));
        Has(repeated, PreparedMaterialDecisionReason.AlreadyTerminalNoOp);
        TestAssert.Equal(0, Count(repeated, PreparedMaterialDecisionReason.PayloadDisposalRequested));
        TestAssert.True(ReferenceEquals(terminalState, state));
    }

    public static void PreparedAbandonAndFailedPreparationOwnCorrectly()
    {
        PreparedMaterialLifecycle lifecycle = new();
        PreparedMaterialLifecycleState state = PreparedMaterialLifecycleState.Initial;
        PreparedMaterialId readyId = MaterialId("ready-abandon");
        PreparedPayloadHandle payload = Payload("ready-payload");

        Step(
            lifecycle,
            ref state,
            new PreparedMaterialRegister(
                readyId,
                PreparedMaterialReusePolicy.ReusableWithinBinding,
                new PreparedMaterialBinding(),
                At(0),
                payload));
        PreparedMaterialLifecycleResult abandoned = Step(
            lifecycle,
            ref state,
            new PreparedMaterialTerminate(
                readyId,
                PreparedMaterialTerminalReason.Abandoned));
        Has(abandoned, PreparedMaterialDecisionReason.Abandoned);
        Has(abandoned, PreparedMaterialDecisionReason.PayloadDisposalRequested);

        PreparedMaterialId preparingId = MaterialId("preparing-failure");
        Step(
            lifecycle,
            ref state,
            new PreparedMaterialRegister(
                preparingId,
                PreparedMaterialReusePolicy.ReusableWithinBinding,
                new PreparedMaterialBinding(),
                At(1)));
        PreparedMaterialLifecycleResult failed = Step(
            lifecycle,
            ref state,
            new PreparedMaterialTerminate(
                preparingId,
                PreparedMaterialTerminalReason.Failed));
        Has(failed, PreparedMaterialDecisionReason.Failed);
        TestAssert.Equal(0, Count(failed, PreparedMaterialDecisionReason.PayloadDisposalRequested));
        TestAssert.Equal(PreparedMaterialTerminalReason.Failed, Material(state, preparingId).TerminalReason);
    }

    public static void LateAndDuplicatePayloadsRemainCallerOwned()
    {
        PreparedMaterialLifecycle lifecycle = new();
        PreparedMaterialLifecycleState state = PreparedMaterialLifecycleState.Initial;
        PreparedMaterialId lateId = MaterialId("late");
        Step(
            lifecycle,
            ref state,
            new PreparedMaterialRegister(
                lateId,
                PreparedMaterialReusePolicy.ReusableWithinBinding,
                new PreparedMaterialBinding(),
                At(0)));
        Step(
            lifecycle,
            ref state,
            new PreparedMaterialTerminate(
                lateId,
                PreparedMaterialTerminalReason.Discarded));

        PreparedPayloadHandle latePayload = Payload("late-payload");
        PreparedMaterialLifecycleResult late = Step(
            lifecycle,
            ref state,
            new PreparedMaterialAttachPayload(lateId, latePayload));
        Has(late, PreparedMaterialDecisionReason.AlreadyTerminalNoOp);
        Has(late, PreparedMaterialDecisionReason.PayloadOwnershipRefused);
        TestAssert.Equal(0, Count(late, PreparedMaterialDecisionReason.PayloadDisposalRequested));

        PreparedMaterialId duplicateId = MaterialId("duplicate");
        PreparedPayloadHandle original = Payload("original");
        PreparedPayloadHandle offered = Payload("offered");
        Step(
            lifecycle,
            ref state,
            new PreparedMaterialRegister(
                duplicateId,
                PreparedMaterialReusePolicy.ReusableWithinBinding,
                new PreparedMaterialBinding(),
                At(1),
                original));
        PreparedMaterialLifecycleResult duplicate = Step(
            lifecycle,
            ref state,
            new PreparedMaterialRegister(
                duplicateId,
                PreparedMaterialReusePolicy.ReusableWithinBinding,
                new PreparedMaterialBinding(),
                At(2),
                offered));
        Has(duplicate, PreparedMaterialDecisionReason.DuplicateRegistrationRefused);
        Has(duplicate, PreparedMaterialDecisionReason.PayloadOwnershipRefused);
        TestAssert.Equal(0, Count(duplicate, PreparedMaterialDecisionReason.PayloadDisposalRequested));
        TestAssert.Equal(original, Material(state, duplicateId).PayloadHandle);
    }

    public static void ExpiredRegistrationAndBoundaryAreDeterministic()
    {
        PreparedMaterialLifecycle lifecycle = new();
        PreparedMaterialLifecycleState state = PreparedMaterialLifecycleState.Initial;
        PreparedMaterialId refusedId = MaterialId("already-expired");
        PreparedPayloadHandle refusedPayload = Payload("caller-owned");

        PreparedMaterialLifecycleResult refused = Step(
            lifecycle,
            ref state,
            new PreparedMaterialRegister(
                refusedId,
                PreparedMaterialReusePolicy.ReusableWithinBinding,
                new PreparedMaterialBinding(ExpiresAt: At(10)),
                At(10),
                refusedPayload));
        Has(refused, PreparedMaterialDecisionReason.RegistrationExpiredRefused);
        Has(refused, PreparedMaterialDecisionReason.PayloadOwnershipRefused);
        TestAssert.Equal(0, state.Materials.Count);

        PreparedMaterialId boundaryId = MaterialId("expiry-boundary");
        Step(
            lifecycle,
            ref state,
            new PreparedMaterialRegister(
                boundaryId,
                PreparedMaterialReusePolicy.ReusableWithinBinding,
                new PreparedMaterialBinding(ExpiresAt: At(20)),
                At(11),
                Payload("expiry-payload")));
        PreparedMaterialLifecycleResult before = Step(
            lifecycle,
            ref state,
            new PreparedMaterialEvaluateExpiry(boundaryId, At(19)));
        Has(before, PreparedMaterialDecisionReason.ExpiryNotReached);
        TestAssert.Equal(PreparedMaterialLifecycleStatus.Ready, Material(state, boundaryId).Status);

        PreparedMaterialLifecycleResult exact = Step(
            lifecycle,
            ref state,
            new PreparedMaterialEvaluateExpiry(boundaryId, At(20)));
        Has(exact, PreparedMaterialDecisionReason.Expired);
        Has(exact, PreparedMaterialDecisionReason.PayloadDisposalRequested);
    }

    public static void ReservationConflictsAndWrongReservationAreStable()
    {
        PreparedMaterialLifecycle lifecycle = new();
        PreparedMaterialLifecycleState state = ReadyReusable(
            lifecycle,
            MaterialId("conflict"),
            Payload("payload"),
            out PreparedMaterialId materialId);
        PreparedMaterialReservationId first = Reservation("first");
        PreparedMaterialUseContext context = Context(5, 10);

        Step(
            lifecycle,
            ref state,
            new PreparedMaterialReserve(materialId, first, context));
        PreparedMaterialLifecycleState reservedState = state;

        PreparedMaterialLifecycleResult conflict = Step(
            lifecycle,
            ref state,
            new PreparedMaterialReserve(materialId, Reservation("second"), context));
        Has(conflict, PreparedMaterialDecisionReason.ReservationConflict);
        TestAssert.True(ReferenceEquals(reservedState, state));

        PreparedMaterialLifecycleResult wrongRelease = Step(
            lifecycle,
            ref state,
            new PreparedMaterialRelease(
                materialId,
                Reservation("wrong"),
                Context(6, 11)));
        Has(wrongRelease, PreparedMaterialDecisionReason.StaleReservationRefused);
        TestAssert.True(ReferenceEquals(reservedState, state));

        PreparedMaterialLifecycleResult wrongComplete = Step(
            lifecycle,
            ref state,
            new PreparedMaterialComplete(
                materialId,
                Reservation("wrong"),
                PreparedMaterialCompletionDisposition.RetireOrConsume,
                context));
        Has(wrongComplete, PreparedMaterialDecisionReason.StaleReservationRefused);
        TestAssert.True(ReferenceEquals(reservedState, state));

        PreparedMaterialLifecycleResult wrongFailure = Step(
            lifecycle,
            ref state,
            new PreparedMaterialTerminate(
                materialId,
                PreparedMaterialTerminalReason.Failed,
                Reservation("wrong")));
        Has(wrongFailure, PreparedMaterialDecisionReason.StaleReservationRefused);
        TestAssert.Equal(0, Count(wrongFailure, PreparedMaterialDecisionReason.PayloadDisposalRequested));
        TestAssert.True(ReferenceEquals(reservedState, state));
    }
    public static void ReusableReleaseAndCompletionReturnToInventory()
    {
        PreparedMaterialLifecycle lifecycle = new();
        PreparedMaterialLifecycleState state = ReadyReusable(
            lifecycle,
            MaterialId("reusable"),
            Payload("reusable-payload"),
            out PreparedMaterialId materialId);
        PreparedMaterialReservationId first = Reservation("use-1");

        Step(
            lifecycle,
            ref state,
            new PreparedMaterialReserve(materialId, first, Context(5, 10)));
        PreparedMaterialLifecycleResult released = Step(
            lifecycle,
            ref state,
            new PreparedMaterialRelease(materialId, first, Context(6, 11)));
        Has(released, PreparedMaterialDecisionReason.ReservationReleased);
        TestAssert.Equal(0, Count(released, PreparedMaterialDecisionReason.PayloadDisposalRequested));
        TestAssert.Equal(PreparedMaterialLifecycleStatus.Ready, Material(state, materialId).Status);

        PreparedMaterialReservationId second = Reservation("use-2");
        Step(
            lifecycle,
            ref state,
            new PreparedMaterialReserve(materialId, second, Context(7, 12)));
        PreparedMaterialLifecycleResult completed = Step(
            lifecycle,
            ref state,
            new PreparedMaterialComplete(
                materialId,
                second,
                PreparedMaterialCompletionDisposition.ReturnToInventory,
                Context(8, 12)));
        Has(completed, PreparedMaterialDecisionReason.CompletedReturnedToInventory);
        TestAssert.Equal(0, Count(completed, PreparedMaterialDecisionReason.PayloadDisposalRequested));
        TestAssert.Equal(PreparedMaterialLifecycleStatus.Ready, Material(state, materialId).Status);

        PreparedMaterialLifecycleResult third = Step(
            lifecycle,
            ref state,
            new PreparedMaterialReserve(
                materialId,
                Reservation("use-3"),
                Context(9, 13)));
        Has(third, PreparedMaterialDecisionReason.ReservationAcquired);
    }

    public static void ReusableRetireDisposesAndSingleCannotReturn()
    {
        PreparedMaterialLifecycle lifecycle = new();
        PreparedMaterialLifecycleState state = ReadyReusable(
            lifecycle,
            MaterialId("retired"),
            Payload("retired-payload"),
            out PreparedMaterialId reusableId);
        PreparedMaterialReservationId reusableReservation = Reservation("retire-use");
        PreparedMaterialUseContext reusableContext = Context(5, 10);
        Step(
            lifecycle,
            ref state,
            new PreparedMaterialReserve(reusableId, reusableReservation, reusableContext));
        PreparedMaterialLifecycleResult retired = Step(
            lifecycle,
            ref state,
            new PreparedMaterialComplete(
                reusableId,
                reusableReservation,
                PreparedMaterialCompletionDisposition.RetireOrConsume,
                reusableContext));
        Has(retired, PreparedMaterialDecisionReason.CompletedRetired);
        Has(retired, PreparedMaterialDecisionReason.PayloadDisposalRequested);

        PreparedMaterialId singleId = MaterialId("single");
        MediaSnapshot track = Track("Song", "Artist");
        Step(
            lifecycle,
            ref state,
            new PreparedMaterialRegister(
                singleId,
                PreparedMaterialReusePolicy.SingleOpportunity,
                SingleBinding(1, track),
                At(0),
                Payload("single-payload")));
        PreparedMaterialReservationId singleReservation = Reservation("single-use");
        PreparedMaterialUseContext singleContext = Context(2, 1, track);
        Step(
            lifecycle,
            ref state,
            new PreparedMaterialReserve(singleId, singleReservation, singleContext));
        PreparedMaterialLifecycleState reservedState = state;

        PreparedMaterialLifecycleResult invalidReturn = Step(
            lifecycle,
            ref state,
            new PreparedMaterialComplete(
                singleId,
                singleReservation,
                PreparedMaterialCompletionDisposition.ReturnToInventory,
                singleContext));
        Has(invalidReturn, PreparedMaterialDecisionReason.InvalidTransitionRefused);
        TestAssert.True(ReferenceEquals(reservedState, state));

        PreparedMaterialLifecycleResult released = Step(
            lifecycle,
            ref state,
            new PreparedMaterialRelease(singleId, singleReservation, singleContext));
        Has(released, PreparedMaterialDecisionReason.SingleOpportunityReleaseDiscarded);
        Has(released, PreparedMaterialDecisionReason.PayloadDisposalRequested);
        TestAssert.Equal(PreparedMaterialTerminalReason.Abandoned, Material(state, singleId).TerminalReason);
    }

    public static void BindingRefusalUsesExactOpportunityTrackAndSession()
    {
        PreparedMaterialLifecycle lifecycle = new();
        PreparedMaterialLifecycleState state = PreparedMaterialLifecycleState.Initial;
        PreparedMaterialId materialId = MaterialId("bound");
        MediaSnapshot track = Track("  Track   Name ", "ARTIST");
        PreparedMaterialSessionId session = Session("session-a");
        Step(
            lifecycle,
            ref state,
            new PreparedMaterialRegister(
                materialId,
                PreparedMaterialReusePolicy.SingleOpportunity,
                SingleBinding(7, track, session),
                At(0),
                Payload("bound-payload")));
        PreparedMaterialLifecycleState readyState = state;

        PreparedMaterialUseContext[] mismatches =
        [
            Context(1, 8, Track("Track Name", "artist"), session),
            Context(1, 7, Track("Track Name Extended", "artist"), session),
            Context(1, 7, Track("Track Name", "artist"), Session("session-b")),
            Context(1, 7, null, session),
        ];

        for (int index = 0; index < mismatches.Length; index++)
        {
            PreparedMaterialUseContext mismatch = mismatches[index];
            PreparedMaterialLifecycleResult refused = Step(
                lifecycle,
                ref state,
                new PreparedMaterialReserve(
                    materialId,
                    Reservation("refused-" + index),
                    mismatch));
            Has(refused, PreparedMaterialDecisionReason.BindingMismatch);
            TestAssert.True(ReferenceEquals(readyState, state));
        }

        PreparedMaterialLifecycleResult accepted = Step(
            lifecycle,
            ref state,
            new PreparedMaterialReserve(
                materialId,
                Reservation("accepted"),
                Context(2, 7, Track("track name", "artist"), session)));
        Has(accepted, PreparedMaterialDecisionReason.ReservationAcquired);
    }

    public static void ExplicitBindingInvalidationIsTypedAndExact()
    {
        PreparedMaterialLifecycle lifecycle = new();
        PreparedMaterialLifecycleState state = PreparedMaterialLifecycleState.Initial;
        MediaSnapshot track = Track("Bound Song", "Bound Artist");
        PreparedMaterialSessionId session = Session("bound-session");

        PreparedMaterialId opportunityId = MaterialId("opportunity-bound");
        Step(
            lifecycle,
            ref state,
            new PreparedMaterialRegister(
                opportunityId,
                PreparedMaterialReusePolicy.SingleOpportunity,
                SingleBinding(12, track),
                At(0),
                Payload("opportunity-payload")));
        PreparedMaterialLifecycleState beforeMismatch = state;
        PreparedMaterialLifecycleResult mismatch = Step(
            lifecycle,
            ref state,
            new PreparedMaterialInvalidateBinding(
                opportunityId,
                PreparedMaterialBindingKind.Opportunity,
                OpportunityId: 13));
        Has(mismatch, PreparedMaterialDecisionReason.BindingMismatch);
        TestAssert.True(ReferenceEquals(beforeMismatch, state));
        PreparedMaterialLifecycleResult opportunityInvalidated = Step(
            lifecycle,
            ref state,
            new PreparedMaterialInvalidateBinding(
                opportunityId,
                PreparedMaterialBindingKind.Opportunity,
                OpportunityId: 12));
        Has(opportunityInvalidated, PreparedMaterialDecisionReason.BindingInvalidated);
        Has(opportunityInvalidated, PreparedMaterialDecisionReason.PayloadDisposalRequested);

        PreparedMaterialId trackId = MaterialId("track-bound");
        Step(
            lifecycle,
            ref state,
            new PreparedMaterialRegister(
                trackId,
                PreparedMaterialReusePolicy.ReusableWithinBinding,
                new PreparedMaterialBinding(
                    SubjectTrack: PreparedMaterialTrackIdentity.From(track)),
                At(1),
                Payload("track-payload")));
        PreparedMaterialLifecycleResult trackInvalidated = Step(
            lifecycle,
            ref state,
            new PreparedMaterialInvalidateBinding(
                trackId,
                PreparedMaterialBindingKind.Track,
                Track: PreparedMaterialTrackIdentity.From(
                    Track(" bound  song ", "BOUND ARTIST"))));
        Has(trackInvalidated, PreparedMaterialDecisionReason.BindingInvalidated);

        PreparedMaterialId sessionId = MaterialId("session-bound");
        Step(
            lifecycle,
            ref state,
            new PreparedMaterialRegister(
                sessionId,
                PreparedMaterialReusePolicy.ReusableWithinBinding,
                new PreparedMaterialBinding(SessionId: session),
                At(2),
                Payload("session-payload")));
        PreparedMaterialLifecycleResult sessionInvalidated = Step(
            lifecycle,
            ref state,
            new PreparedMaterialInvalidateBinding(
                sessionId,
                PreparedMaterialBindingKind.Session,
                SessionId: session));
        Has(sessionInvalidated, PreparedMaterialDecisionReason.BindingInvalidated);
    }

    public static void ReadyAndReservedExpiryDisposeExactlyOnce()
    {
        PreparedMaterialLifecycle lifecycle = new();
        PreparedMaterialLifecycleState state = PreparedMaterialLifecycleState.Initial;
        PreparedMaterialId readyId = MaterialId("ready-expiry");
        Step(
            lifecycle,
            ref state,
            new PreparedMaterialRegister(
                readyId,
                PreparedMaterialReusePolicy.ReusableWithinBinding,
                new PreparedMaterialBinding(ExpiresAt: At(10)),
                At(0),
                Payload("ready-expiry-payload")));
        PreparedMaterialLifecycleResult readyExpiry = Step(
            lifecycle,
            ref state,
            new PreparedMaterialEvaluateExpiry(readyId, At(10)));
        TestAssert.Equal(1, Count(readyExpiry, PreparedMaterialDecisionReason.PayloadDisposalRequested));
        PreparedMaterialLifecycleResult readyRepeated = Step(
            lifecycle,
            ref state,
            new PreparedMaterialEvaluateExpiry(readyId, At(11)));
        Has(readyRepeated, PreparedMaterialDecisionReason.AlreadyTerminalNoOp);
        TestAssert.Equal(0, Count(readyRepeated, PreparedMaterialDecisionReason.PayloadDisposalRequested));

        PreparedMaterialId reservedId = MaterialId("reserved-expiry");
        Step(
            lifecycle,
            ref state,
            new PreparedMaterialRegister(
                reservedId,
                PreparedMaterialReusePolicy.ReusableWithinBinding,
                new PreparedMaterialBinding(ExpiresAt: At(20)),
                At(0),
                Payload("reserved-expiry-payload")));
        Step(
            lifecycle,
            ref state,
            new PreparedMaterialReserve(
                reservedId,
                Reservation("reserved-expiry-use"),
                Context(15, 20)));
        PreparedMaterialLifecycleResult reservedExpiry = Step(
            lifecycle,
            ref state,
            new PreparedMaterialEvaluateExpiry(reservedId, At(20)));
        TestAssert.Equal(1, Count(reservedExpiry, PreparedMaterialDecisionReason.PayloadDisposalRequested));
        TestAssert.Equal(PreparedMaterialTerminalReason.Expired, Material(state, reservedId).TerminalReason);
    }

    public static void StateCollectionsAreReadOnly()
    {
        PreparedMaterialLifecycle lifecycle = new();
        PreparedMaterialLifecycleState state = PreparedMaterialLifecycleState.Initial;
        PreparedMaterialId materialId = MaterialId("read-only");
        Step(
            lifecycle,
            ref state,
            new PreparedMaterialRegister(
                materialId,
                PreparedMaterialReusePolicy.ReusableWithinBinding,
                new PreparedMaterialBinding(),
                At(0)));

        TestAssert.True(
            state.Materials is ReadOnlyDictionary<PreparedMaterialId, PreparedMaterialRecord>);
        var dictionary =
            (IDictionary<PreparedMaterialId, PreparedMaterialRecord>)state.Materials;
        Throws<NotSupportedException>(() => dictionary.Add(
            MaterialId("mutation"),
            Material(state, materialId)));
    }
    public static void M10AHappyPathCompositionCompletesAndDisposes()
    {
        DeterministicOpportunityKernel kernel = CreateKernel();
        OpportunityKernelState opportunityState = OpportunityKernelState.Initial;
        PreparedMaterialLifecycle lifecycle = new();
        PreparedMaterialLifecycleState materialState = PreparedMaterialLifecycleState.Initial;
        MediaSnapshot subject = Track("Subject", "Artist");
        MediaSnapshot next = Track("Next", "Next Artist");

        StepKernel(
            kernel,
            ref opportunityState,
            new OpportunityKernelArm(Track("Initial", "Initial Artist"), At(0), 2));
        OpportunityKernelResult preparationResult = StepKernel(
            kernel,
            ref opportunityState,
            new OpportunityKernelObserveTrack(subject, At(10)));
        OpportunityKernelPreparation preparation = Preparation(preparationResult);
        PreparedMaterialId materialId = MaterialId("opportunity-" + preparation.Id);
        PreparedPayloadHandle payload = Payload("prepared-" + preparation.Id);
        PreparedMaterialReservationId reservation = Reservation("delivery-" + preparation.Id);

        Step(
            lifecycle,
            ref materialState,
            new PreparedMaterialRegister(
                materialId,
                PreparedMaterialReusePolicy.SingleOpportunity,
                SingleBinding(preparation.Id, preparation.SubjectTrack),
                At(10)));
        Step(
            lifecycle,
            ref materialState,
            new PreparedMaterialAttachPayload(materialId, payload));
        StepKernel(
            kernel,
            ref opportunityState,
            new OpportunityKernelMarkPreparationReady(preparation.Id));
        OpportunityKernelResult deliveryResult = StepKernel(
            kernel,
            ref opportunityState,
            new OpportunityKernelObserveTrack(next, At(20)));
        HasKernel(deliveryResult, OpportunityKernelDecisionReason.DeliveryDue);

        PreparedMaterialUseContext context =
            Context(20, preparation.Id, preparation.SubjectTrack);
        Step(
            lifecycle,
            ref materialState,
            new PreparedMaterialReserve(materialId, reservation, context));
        PreparedMaterialLifecycleResult completed = Step(
            lifecycle,
            ref materialState,
            new PreparedMaterialComplete(
                materialId,
                reservation,
                PreparedMaterialCompletionDisposition.RetireOrConsume,
                context));
        Has(completed, PreparedMaterialDecisionReason.CompletedRetired);
        Has(completed, PreparedMaterialDecisionReason.PayloadDisposalRequested);

        OpportunityKernelResult resolved = StepKernel(
            kernel,
            ref opportunityState,
            new OpportunityKernelResolveOpportunity(
                preparation.Id,
                OpportunityKernelResolutionStatus.Completed,
                "Accepted M10A comment text is recorded only by the opportunity kernel.",
                2));
        HasKernel(resolved, OpportunityKernelDecisionReason.OpportunityResolvedCompleted);
    }

    public static void M10AMissedAndRapidTransitionsDisposeCorrectly()
    {
        DeterministicOpportunityKernel kernel = CreateKernel();
        OpportunityKernelState opportunityState = OpportunityKernelState.Initial;
        PreparedMaterialLifecycle lifecycle = new();
        PreparedMaterialLifecycleState materialState = PreparedMaterialLifecycleState.Initial;
        StepKernel(
            kernel,
            ref opportunityState,
            new OpportunityKernelArm(Track("Initial", "Artist 0"), At(0), 2));
        OpportunityKernelResult preparationResult = StepKernel(
            kernel,
            ref opportunityState,
            new OpportunityKernelObserveTrack(Track("Missed", "Artist 1"), At(10)));
        OpportunityKernelPreparation missedPreparation = Preparation(preparationResult);
        PreparedMaterialId missedId = MaterialId("missed-" + missedPreparation.Id);
        Step(
            lifecycle,
            ref materialState,
            new PreparedMaterialRegister(
                missedId,
                PreparedMaterialReusePolicy.SingleOpportunity,
                SingleBinding(missedPreparation.Id, missedPreparation.SubjectTrack),
                At(10)));

        OpportunityKernelResult missedTransition = StepKernel(
            kernel,
            ref opportunityState,
            new OpportunityKernelObserveTrack(
                Track("After Miss", "Artist 2"),
                At(20),
                2));
        HasKernel(missedTransition, OpportunityKernelDecisionReason.PreparationMissedTransition);
        PreparedMaterialLifecycleResult abandoned = Step(
            lifecycle,
            ref materialState,
            new PreparedMaterialTerminate(
                missedId,
                PreparedMaterialTerminalReason.Abandoned));
        TestAssert.Equal(0, Count(abandoned, PreparedMaterialDecisionReason.PayloadDisposalRequested));
        PreparedMaterialLifecycleResult late = Step(
            lifecycle,
            ref materialState,
            new PreparedMaterialAttachPayload(missedId, Payload("late-missed")));
        Has(late, PreparedMaterialDecisionReason.PayloadOwnershipRefused);

        OpportunityKernelResult rapidPreparationResult = StepKernel(
            kernel,
            ref opportunityState,
            new OpportunityKernelObserveTrack(Track("Rapid Subject", "Artist 3"), At(30)));
        OpportunityKernelPreparation rapidPreparation = Preparation(rapidPreparationResult);
        PreparedMaterialId rapidId = MaterialId("rapid-" + rapidPreparation.Id);
        PreparedMaterialReservationId rapidReservation = Reservation("rapid-delivery");
        Step(
            lifecycle,
            ref materialState,
            new PreparedMaterialRegister(
                rapidId,
                PreparedMaterialReusePolicy.SingleOpportunity,
                SingleBinding(rapidPreparation.Id, rapidPreparation.SubjectTrack),
                At(30),
                Payload("rapid-payload")));
        StepKernel(
            kernel,
            ref opportunityState,
            new OpportunityKernelMarkPreparationReady(rapidPreparation.Id));
        StepKernel(
            kernel,
            ref opportunityState,
            new OpportunityKernelObserveTrack(Track("Rapid B", "Artist 4"), At(40)));
        Step(
            lifecycle,
            ref materialState,
            new PreparedMaterialReserve(
                rapidId,
                rapidReservation,
                Context(40, rapidPreparation.Id, rapidPreparation.SubjectTrack)));

        OpportunityKernelResult rapidInvalidation = StepKernel(
            kernel,
            ref opportunityState,
            new OpportunityKernelObserveTrack(
                Track("Rapid C", "Artist 5"),
                At(50),
                2));
        HasKernel(
            rapidInvalidation,
            OpportunityKernelDecisionReason.RapidTransitionInvalidatedDelivery);
        PreparedMaterialLifecycleResult rapidAbandoned = Step(
            lifecycle,
            ref materialState,
            new PreparedMaterialTerminate(
                rapidId,
                PreparedMaterialTerminalReason.Abandoned,
                rapidReservation));
        Has(rapidAbandoned, PreparedMaterialDecisionReason.PayloadDisposalRequested);
    }

    public static void M10AControlInvalidationsDisposeBoundMaterial()
    {
        string[] actions = ["silent", "clear", "rebind", "cancel", "disarm"];
        foreach (string action in actions)
        {
            DeterministicOpportunityKernel kernel = CreateKernel();
            OpportunityKernelState opportunityState = OpportunityKernelState.Initial;
            PreparedMaterialLifecycle lifecycle = new();
            PreparedMaterialLifecycleState materialState = PreparedMaterialLifecycleState.Initial;
            StepKernel(
                kernel,
                ref opportunityState,
                new OpportunityKernelArm(Track("Initial", "Artist 0"), At(0), 2));
            OpportunityKernelPreparation preparation = Preparation(StepKernel(
                kernel,
                ref opportunityState,
                new OpportunityKernelObserveTrack(
                    Track("Subject " + action, "Artist"),
                    At(10))));
            PreparedMaterialId materialId = MaterialId(action + "-" + preparation.Id);
            Step(
                lifecycle,
                ref materialState,
                new PreparedMaterialRegister(
                    materialId,
                    PreparedMaterialReusePolicy.SingleOpportunity,
                    SingleBinding(preparation.Id, preparation.SubjectTrack),
                    At(10),
                    Payload(action + "-payload")));

            OpportunityKernelResult invalidated = action switch
            {
                "silent" => StepKernel(
                    kernel,
                    ref opportunityState,
                    new OpportunityKernelSetSilentMode(true)),
                "clear" => StepKernel(
                    kernel,
                    ref opportunityState,
                    new OpportunityKernelClearRecentHistory(2)),
                "rebind" => StepKernel(
                    kernel,
                    ref opportunityState,
                    new OpportunityKernelRebindCurrentTrack(
                        Track("Rebound", "Rebound Artist"),
                        At(11),
                        2)),
                "cancel" => StepKernel(
                    kernel,
                    ref opportunityState,
                    new OpportunityKernelCancelPendingOpportunity(2)),
                "disarm" => StepKernel(
                    kernel,
                    ref opportunityState,
                    new OpportunityKernelDisarm()),
                _ => throw new InvalidOperationException("Unknown invalidation trace."),
            };
            HasKernel(
                invalidated,
                action == "cancel"
                    ? OpportunityKernelDecisionReason.PendingOpportunityCancelled
                    : OpportunityKernelDecisionReason.PendingOpportunityInvalidated);

            PreparedMaterialLifecycleResult disposed = Step(
                lifecycle,
                ref materialState,
                new PreparedMaterialTerminate(
                    materialId,
                    PreparedMaterialTerminalReason.Abandoned));
            TestAssert.Equal(
                1,
                Count(disposed, PreparedMaterialDecisionReason.PayloadDisposalRequested));
        }
    }

    public static void M10AResolutionAndStaleIdentityRemainIsolated()
    {
        foreach (OpportunityKernelResolutionStatus status in new[]
                 {
                     OpportunityKernelResolutionStatus.Abandoned,
                     OpportunityKernelResolutionStatus.Failed,
                 })
        {
            DeterministicOpportunityKernel kernel = CreateKernel();
            OpportunityKernelState opportunityState = OpportunityKernelState.Initial;
            PreparedMaterialLifecycle lifecycle = new();
            PreparedMaterialLifecycleState materialState = PreparedMaterialLifecycleState.Initial;
            StepKernel(
                kernel,
                ref opportunityState,
                new OpportunityKernelArm(Track("Initial", "Artist 0"), At(0), 2));
            OpportunityKernelPreparation preparation = Preparation(StepKernel(
                kernel,
                ref opportunityState,
                new OpportunityKernelObserveTrack(Track("Subject", "Artist"), At(10))));
            PreparedMaterialId materialId = MaterialId(status + "-" + preparation.Id);
            Step(
                lifecycle,
                ref materialState,
                new PreparedMaterialRegister(
                    materialId,
                    PreparedMaterialReusePolicy.SingleOpportunity,
                    SingleBinding(preparation.Id, preparation.SubjectTrack),
                    At(10),
                    Payload(status + "-payload")));

            OpportunityKernelResult resolved = StepKernel(
                kernel,
                ref opportunityState,
                new OpportunityKernelResolveOpportunity(preparation.Id, status, null, 2));
            HasKernel(
                resolved,
                status == OpportunityKernelResolutionStatus.Abandoned
                    ? OpportunityKernelDecisionReason.OpportunityResolvedAbandoned
                    : OpportunityKernelDecisionReason.OpportunityResolvedFailed);
            PreparedMaterialLifecycleResult terminal = Step(
                lifecycle,
                ref materialState,
                new PreparedMaterialTerminate(
                    materialId,
                    status == OpportunityKernelResolutionStatus.Abandoned
                        ? PreparedMaterialTerminalReason.Abandoned
                        : PreparedMaterialTerminalReason.Failed));
            Has(terminal, PreparedMaterialDecisionReason.PayloadDisposalRequested);
        }

        PreparedMaterialLifecycle staleLifecycle = new();
        PreparedMaterialLifecycleState staleState = PreparedMaterialLifecycleState.Initial;
        PreparedMaterialId currentId = MaterialId("current-opportunity");
        Step(
            staleLifecycle,
            ref staleState,
            new PreparedMaterialRegister(
                currentId,
                PreparedMaterialReusePolicy.SingleOpportunity,
                SingleBinding(50, Track("Current", "Artist")),
                At(0),
                Payload("current-payload")));
        PreparedMaterialLifecycleState currentState = staleState;
        PreparedMaterialLifecycleResult staleReserve = Step(
            staleLifecycle,
            ref staleState,
            new PreparedMaterialReserve(
                currentId,
                Reservation("stale"),
                Context(1, 49, Track("Current", "Artist"))));
        Has(staleReserve, PreparedMaterialDecisionReason.BindingMismatch);
        TestAssert.True(ReferenceEquals(currentState, staleState));
        TestAssert.Equal(0, Count(staleReserve, PreparedMaterialDecisionReason.PayloadDisposalRequested));
    }

    public static void MalformedIdentityAndPolicyInputsAreRejected()
    {
        PreparedMaterialLifecycle lifecycle = new();
        PreparedMaterialLifecycleState state = PreparedMaterialLifecycleState.Initial;

        Throws<ArgumentException>(() => lifecycle.Apply(
            state,
            new PreparedMaterialRegister(
                MaterialId(" "),
                PreparedMaterialReusePolicy.ReusableWithinBinding,
                new PreparedMaterialBinding(),
                At(0))));
        Throws<ArgumentException>(() => lifecycle.Apply(
            state,
            new PreparedMaterialRegister(
                MaterialId("single-without-opportunity"),
                PreparedMaterialReusePolicy.SingleOpportunity,
                new PreparedMaterialBinding(),
                At(0))));
        Throws<ArgumentException>(() => lifecycle.Apply(
            state,
            new PreparedMaterialRegister(
                MaterialId("reusable-with-opportunity"),
                PreparedMaterialReusePolicy.ReusableWithinBinding,
                new PreparedMaterialBinding(OpportunityId: 1),
                At(0))));
        Throws<ArgumentException>(() => PreparedMaterialTrackIdentity.From(
            new MediaSnapshot("Title", null, null)));
        Throws<ArgumentOutOfRangeException>(() => lifecycle.Apply(
            state,
            new PreparedMaterialRegister(
                MaterialId("invalid-policy"),
                (PreparedMaterialReusePolicy)999,
                new PreparedMaterialBinding(),
                At(0))));
    }

    private static PreparedMaterialLifecycleState ReadyReusable(
        PreparedMaterialLifecycle lifecycle,
        PreparedMaterialId requestedId,
        PreparedPayloadHandle payload,
        out PreparedMaterialId materialId)
    {
        materialId = requestedId;
        PreparedMaterialLifecycleState state = PreparedMaterialLifecycleState.Initial;
        Step(
            lifecycle,
            ref state,
            new PreparedMaterialRegister(
                materialId,
                PreparedMaterialReusePolicy.ReusableWithinBinding,
                new PreparedMaterialBinding(),
                At(0),
                payload));
        return state;
    }

    private static PreparedMaterialBinding SingleBinding(
        long opportunityId,
        MediaSnapshot track,
        PreparedMaterialSessionId? session = null,
        DateTimeOffset? expiresAt = null)
    {
        return new PreparedMaterialBinding(
            opportunityId,
            PreparedMaterialTrackIdentity.From(track),
            session,
            expiresAt);
    }

    private static PreparedMaterialLifecycleResult Step(
        PreparedMaterialLifecycle lifecycle,
        ref PreparedMaterialLifecycleState state,
        PreparedMaterialLifecycleInput input)
    {
        PreparedMaterialLifecycleResult result = lifecycle.Apply(state, input);
        state = result.State;
        return result;
    }

    private static OpportunityKernelResult StepKernel(
        DeterministicOpportunityKernel kernel,
        ref OpportunityKernelState state,
        OpportunityKernelInput input)
    {
        OpportunityKernelResult result = kernel.Apply(state, input);
        state = result.State;
        return result;
    }

    private static DeterministicOpportunityKernel CreateKernel()
    {
        return new DeterministicOpportunityKernel(
            OpportunityKernelConfiguration.FromRadioBehavior(
                RadioBehaviorConfiguration.Default));
    }

    private static OpportunityKernelPreparation Preparation(
        OpportunityKernelResult result)
    {
        return result.Decisions.Single(
            decision => decision.Reason == OpportunityKernelDecisionReason.PreparationRequested)
            .Preparation!;
    }

    private static PreparedMaterialRecord Material(
        PreparedMaterialLifecycleState state,
        PreparedMaterialId materialId)
    {
        return state.Materials[materialId];
    }

    private static void Has(
        PreparedMaterialLifecycleResult result,
        PreparedMaterialDecisionReason reason)
    {
        TestAssert.True(result.Decisions.Any(decision => decision.Reason == reason));
    }

    private static void HasKernel(
        OpportunityKernelResult result,
        OpportunityKernelDecisionReason reason)
    {
        TestAssert.True(result.Decisions.Any(decision => decision.Reason == reason));
    }

    private static int Count(
        PreparedMaterialLifecycleResult result,
        PreparedMaterialDecisionReason reason)
    {
        return result.Decisions.Count(decision => decision.Reason == reason);
    }

    private static PreparedMaterialUseContext Context(
        int seconds,
        long? opportunityId = null,
        MediaSnapshot? track = null,
        PreparedMaterialSessionId? session = null)
    {
        return new PreparedMaterialUseContext(
            At(seconds),
            opportunityId,
            track,
            session);
    }

    private static PreparedMaterialId MaterialId(string value) => new(value);

    private static PreparedPayloadHandle Payload(string value) => new(value);

    private static PreparedMaterialReservationId Reservation(string value) => new(value);

    private static PreparedMaterialSessionId Session(string value) => new(value);

    private static DateTimeOffset At(int seconds)
    {
        return DateTimeOffset.Parse("2026-08-22T12:00:00Z").AddSeconds(seconds);
    }

    private static MediaSnapshot Track(string title, string artist)
    {
        return new MediaSnapshot(title, artist, null);
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
}