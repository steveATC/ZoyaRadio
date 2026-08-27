namespace ZoyaRadio.MediaProbe;

// M10C is deliberately an adapter, not a second scheduling implementation.
// It translates the accepted pure M10A/M10B results into the existing M8
// pipeline boundary types while preserving the accepted live audio path.
internal sealed class M10CRuntimeMigrationState
{
    private readonly DeterministicOpportunityKernel _kernel;
    private readonly PreparedMaterialLifecycle _lifecycle = new();
    private readonly EditorialArbiter _editorialArbiter;
    private readonly EditorialPressureSnapshot _editorialPressure;
    private readonly int _editorialTieBreakValue;
    private readonly ITransitionTargetSource _targetSource;
    private readonly int _minimumTransitions;
    private readonly int _maximumTransitions;
    private OpportunityKernelState _kernelState = OpportunityKernelState.Initial;
    private PreparedMaterialLifecycleState _lifecycleState = PreparedMaterialLifecycleState.Initial;
    private PreparedMaterialSessionId _sessionId = new("m10c-session-0");
    private long _sessionSequence;
    private readonly Dictionary<PreparedPayloadHandle, PreparedTransitionOpportunity> _ownedPrepared = new();

    public M10CRuntimeMigrationState(
        RadioBehaviorConfiguration configuration,
        ITransitionTargetSource targetSource,
        EditorialPolicyProfile? editorialPolicy = null,
        EditorialPressureSnapshot? editorialPressure = null,
        int editorialTieBreakValue = 0)
    {
        ArgumentNullException.ThrowIfNull(configuration);
        ArgumentNullException.ThrowIfNull(targetSource);
        configuration.Validate();
        _kernel = new DeterministicOpportunityKernel(OpportunityKernelConfiguration.FromRadioBehavior(configuration));
        _editorialArbiter = new EditorialArbiter(editorialPolicy ?? EditorialPolicyProfile.M10CEquivalent);
        _editorialPressure = editorialPressure ?? EditorialPressureSnapshot.Clear;
        _editorialTieBreakValue = editorialTieBreakValue;
        _targetSource = targetSource;
        _minimumTransitions = configuration.MinimumTransitionsBetweenComments;
        _maximumTransitions = configuration.MaximumTransitionsBetweenComments;
    }

    public bool Armed => _kernelState.Armed;
    public bool SilentMode => _kernelState.SilentMode;
    public int TransitionCounter => _kernelState.TransitionCounter;
    public int ScheduledThreshold => _kernelState.ScheduledThreshold;
    public int TotalObservedTransitions => _kernelState.TotalObservedTransitions;
    public IReadOnlyList<MediaSnapshot> RecentTracks => _kernelState.RecentTracks;
    public IReadOnlyList<string> RecentAcceptedAiComments => _kernelState.RecentAcceptedAiComments;
    public PreparedMaterialSessionId CurrentSessionId => _sessionId;
    public EditorialDecision? LastPreparationDecision { get; private set; }
    public EditorialDecision? LastAirtimeDecision { get; private set; }

    public void Arm(MediaSnapshot initialTrack, DateTimeOffset observedAt) =>
        Advance(new OpportunityKernelArm(initialTrack, observedAt, NextTarget()));

    public void Disarm() => Advance(new OpportunityKernelDisarm());

    public RadioTrackObservation ObserveTrack(MediaSnapshot track, DateTimeOffset observedAt)
    {
        MediaSnapshot previous = _kernelState.CurrentTrack;
        bool needsFreshTarget = _kernelState.PendingOpportunity is not null
            && (!_kernelState.PreparationReady || _kernelState.DeliveryTransitionObserved);
        OpportunityKernelResult result = Advance(new OpportunityKernelObserveTrack(
            track, observedAt, needsFreshTarget ? NextTarget() : null));

        if (Has(result, OpportunityKernelDecisionReason.UnusableTrackIgnored))
        {
            return CreateObservation(RadioTrackObservationKind.Unusable, null, null, result);
        }

        if (Has(result, OpportunityKernelDecisionReason.DuplicateTrackIgnored))
        {
            return CreateObservation(RadioTrackObservationKind.Duplicate, null, _kernelState.CurrentTrack, result);
        }

        return CreateObservation(RadioTrackObservationKind.Transition, previous, track, result);
    }

    public RadioModeChange SetSilentMode(bool silent)
    {
        bool needsFreshTarget = !silent && _kernelState.SilentMode != silent;
        OpportunityKernelResult result = Advance(new OpportunityKernelSetSilentMode(
            silent, needsFreshTarget ? NextTarget() : null));
        return new RadioModeChange(_kernelState.SilentMode,
            Has(result, OpportunityKernelDecisionReason.PendingOpportunityInvalidated),
            _kernelState.ScheduledThreshold);
    }

    public bool ClearRecentHistory()
    {
        OpportunityKernelResult result = Advance(new OpportunityKernelClearRecentHistory(NextTarget()));
        return Has(result, OpportunityKernelDecisionReason.PendingOpportunityInvalidated);
    }

    public bool CancelPendingOpportunityAndReschedule()
    {
        bool hasPending = _kernelState.PendingOpportunity is not null;
        Advance(new OpportunityKernelCancelPendingOpportunity(hasPending ? NextTarget() : null));
        return hasPending;
    }

    public bool RebindCurrentTrack(MediaSnapshot media, DateTimeOffset observedAt)
    {
        bool invalidated = _kernelState.PendingOpportunity is not null;
        InvalidateSessionMaterial();
        _sessionId = new PreparedMaterialSessionId($"m10c-session-{++_sessionSequence}");
        Advance(new OpportunityKernelRebindCurrentTrack(media, observedAt, NextTarget()));
        return invalidated;
    }

    public bool MarkPreparationReady(long opportunityId, PreparedTransitionOpportunity prepared)
    {
        ArgumentNullException.ThrowIfNull(prepared);
        PreparedPayloadHandle payloadHandle = new($"m10c-opportunity-{opportunityId}");
        OpportunityKernelResult result = Advance(new OpportunityKernelMarkPreparationReady(opportunityId));
        if (!Has(result, OpportunityKernelDecisionReason.PreparationMarkedReady))
        {
            return false;
        }

        PreparedMaterialLifecycleResult attached = AdvanceLifecycle(new PreparedMaterialAttachPayload(
            MaterialId(opportunityId), payloadHandle));
        bool accepted = attached.Decisions.Any(decision =>
            decision.Reason == PreparedMaterialDecisionReason.PayloadOwnershipAccepted);
        if (accepted)
        {
            _ownedPrepared.Add(payloadHandle, prepared);
        }

        return accepted;
    }

    public EditorialDecision EvaluatePreparation(
        RadioPreparationOpportunity opportunity,
        bool preparationCapacityAvailable)
    {
        ArgumentNullException.ThrowIfNull(opportunity);
        var candidate = new EditorialPreparationCandidate(
            CandidateId(opportunity.Id),
            opportunity.Id,
            EditorialContentFormat.ExistingComment,
            EditorialDeliveryTreatment.DuckOverMusic,
            EditorialPolicyPriority.Timely,
            OpportunityMatches: _kernelState.PendingOpportunity?.Id == opportunity.Id,
            OpportunityAvailable: _kernelState.Armed
                && !_kernelState.SilentMode
                && _kernelState.PendingOpportunity is not null
                && !_kernelState.DeliveryTransitionObserved,
            PreparationCapacityAvailable: preparationCapacityAvailable);
        LastPreparationDecision = _editorialArbiter.DecidePreparation(new EditorialPreparationInput(
            [candidate],
            AvailableTreatments(),
            _editorialPressure,
            _editorialTieBreakValue));
        return LastPreparationDecision;
    }

    public M10CDeliveryReservation? TryReserveDelivery(RadioTransitionDelivery transition, DateTimeOffset observedAt) =>
        TryReserveTransition(transition, linerCandidate: null, observedAt).CommentReservation;

    public EditorialDecision EvaluateLinerPreparation(long opportunityId, bool preparationCapacityAvailable)
    {
        var candidate = new EditorialPreparationCandidate(
            ReusableLinerInventory.CandidateId,
            opportunityId,
            EditorialContentFormat.ReusableLiner,
            EditorialDeliveryTreatment.DuckOverMusic,
            EditorialPolicyPriority.Routine,
            OpportunityMatches: _kernelState.Armed && !_kernelState.SilentMode,
            OpportunityAvailable: _kernelState.Armed && !_kernelState.SilentMode,
            PreparationCapacityAvailable: preparationCapacityAvailable);
        LastPreparationDecision = _editorialArbiter.DecidePreparation(new EditorialPreparationInput(
            [candidate], AvailableTreatments(), EditorialPressureSnapshot.Clear, _editorialTieBreakValue));
        return LastPreparationDecision;
    }

    public M11BTransitionSelection TryReserveTransition(
        RadioTransitionDelivery? transition,
        EditorialAirtimeCandidate? linerCandidate,
        DateTimeOffset observedAt)
    {
        var candidates = new List<EditorialAirtimeCandidate>();
        PreparedMaterialId? commentMaterialId = null;
        if (transition is not null)
        {
            EditorialAirtimeCandidate comment = CreateCommentCandidate(transition);
            candidates.Add(comment);
            commentMaterialId = comment.MaterialId;
        }

        if (linerCandidate is not null)
        {
            candidates.Add(linerCandidate);
        }

        LastAirtimeDecision = _editorialArbiter.DecideAirtime(new EditorialAirtimeInput(
            observedAt, candidates, AvailableTreatments(), EditorialPressureSnapshot.Clear,
            _editorialTieBreakValue));
        if (LastAirtimeDecision.Outcome != EditorialDecisionOutcome.ReserveForAirtime)
        {
            return new M11BTransitionSelection(LastAirtimeDecision, null);
        }

        if (linerCandidate is not null
            && LastAirtimeDecision.SelectedCandidateId == linerCandidate.CandidateId)
        {
            return new M11BTransitionSelection(LastAirtimeDecision, null);
        }

        if (transition is null || commentMaterialId is null
            || LastAirtimeDecision.SelectedMaterialId != commentMaterialId)
        {
            return new M11BTransitionSelection(LastAirtimeDecision, null);
        }

        var reservationId = new PreparedMaterialReservationId($"delivery-{transition.OpportunityId}");
        PreparedMaterialLifecycleResult result = AdvanceLifecycle(new PreparedMaterialReserve(
            commentMaterialId.Value, reservationId,
            UseContext(transition.OpportunityId, transition.CommentedTrack, observedAt)));
        M10CDeliveryReservation? reservation = result.Decisions.Any(decision =>
            decision.Reason == PreparedMaterialDecisionReason.ReservationAcquired)
                ? new M10CDeliveryReservation(transition.OpportunityId, reservationId)
                : null;
        return new M11BTransitionSelection(LastAirtimeDecision, reservation);
    }

    private EditorialAirtimeCandidate CreateCommentCandidate(RadioTransitionDelivery transition)
    {
        PreparedMaterialId materialId = MaterialId(transition.OpportunityId);
        _lifecycleState.Materials.TryGetValue(materialId, out PreparedMaterialRecord? material);
        return new EditorialAirtimeCandidate(
            CandidateId(transition.OpportunityId),
            transition.OpportunityId,
            materialId,
            EditorialContentFormat.ExistingComment,
            EditorialDeliveryTreatment.DuckOverMusic,
            EditorialPolicyPriority.Timely,
            OpportunityMatches: _kernelState.PendingOpportunity?.Id == transition.OpportunityId
                && _kernelState.DeliveryTransitionObserved,
            MaterialStatus: material?.Status,
            HasPayload: material?.PayloadHandle is not null,
            BindingMatches: material is not null && BindingMatches(material.Binding, transition),
            ExpiresAt: material?.Binding.ExpiresAt);
    }

    public bool ResolveOpportunity(long opportunityId, bool announcementCompleted, string? acceptedAiComment,
        DateTimeOffset observedAt, M10CDeliveryReservation? reservation = null)
    {
        OpportunityKernelResolutionStatus status = announcementCompleted
            ? OpportunityKernelResolutionStatus.Completed
            : OpportunityKernelResolutionStatus.Abandoned;

        if (reservation is not null)
        {
            AdvanceLifecycle(new PreparedMaterialComplete(MaterialId(opportunityId), reservation.ReservationId,
                PreparedMaterialCompletionDisposition.RetireOrConsume,
                UseContext(opportunityId, _kernelState.PendingOpportunity?.SubjectTrack, observedAt)));
        }
        else
        {
            AdvanceLifecycle(new PreparedMaterialTerminate(MaterialId(opportunityId), announcementCompleted
                ? PreparedMaterialTerminalReason.Completed
                : PreparedMaterialTerminalReason.Abandoned));
        }

        bool matchingPending = _kernelState.PendingOpportunity?.Id == opportunityId;
        Advance(new OpportunityKernelResolveOpportunity(opportunityId, status, acceptedAiComment,
            matchingPending ? NextTarget() : null));
        return matchingPending;
    }

    public RadioRuntimeState GetRuntimeState() => new(_kernelState.Armed, _kernelState.SilentMode,
        _kernelState.CurrentTrack, _kernelState.CurrentTrackStartedAt, _kernelState.PendingOpportunity?.Id);

    private OpportunityKernelResult Advance(OpportunityKernelInput input)
    {
        OpportunityKernelResult result = _kernel.Apply(_kernelState, input);
        _kernelState = result.State;
        foreach (OpportunityKernelDecision decision in result.Decisions)
        {
            if (decision.Reason == OpportunityKernelDecisionReason.PreparationRequested && decision.Preparation is not null)
            {
                RegisterMaterial(decision.Preparation);
            }
            else if (decision.Reason == OpportunityKernelDecisionReason.PendingOpportunityInvalidated && decision.OpportunityId is not null)
            {
                AdvanceLifecycle(new PreparedMaterialTerminate(MaterialId(decision.OpportunityId.Value),
                    PreparedMaterialTerminalReason.Discarded));
            }
        }

        return result;
    }

    private void RegisterMaterial(OpportunityKernelPreparation preparation) => AdvanceLifecycle(new PreparedMaterialRegister(
        MaterialId(preparation.Id), PreparedMaterialReusePolicy.SingleOpportunity,
        new PreparedMaterialBinding(preparation.Id, PreparedMaterialTrackIdentity.From(preparation.SubjectTrack), _sessionId),
        preparation.SubjectTrackStartedAt));

    private void InvalidateSessionMaterial()
    {
        foreach (PreparedMaterialRecord material in _lifecycleState.Materials.Values
            .Where(material => material.Status != PreparedMaterialLifecycleStatus.Terminal && material.Binding.SessionId == _sessionId)
            .ToArray())
        {
            AdvanceLifecycle(new PreparedMaterialInvalidateBinding(material.Id,
                PreparedMaterialBindingKind.Session, SessionId: _sessionId));
        }
    }

    private PreparedMaterialLifecycleResult AdvanceLifecycle(PreparedMaterialLifecycleInput input)
    {
        PreparedMaterialLifecycleResult result = _lifecycle.Apply(_lifecycleState, input);
        _lifecycleState = result.State;
        foreach (PreparedMaterialDecision decision in result.Decisions)
        {
            if (decision.Reason == PreparedMaterialDecisionReason.PayloadDisposalRequested
                && decision.PayloadHandle is not null
                && _ownedPrepared.Remove(decision.PayloadHandle.Value, out PreparedTransitionOpportunity? prepared))
            {
                prepared.Dispose();
            }
        }

        return result;
    }

    private int NextTarget() => _targetSource.NextInclusive(_minimumTransitions, _maximumTransitions);

    private PreparedMaterialUseContext UseContext(long opportunityId, MediaSnapshot? track, DateTimeOffset observedAt) =>
        new(observedAt, opportunityId, track, _sessionId);

    private static PreparedMaterialId MaterialId(long opportunityId) => new($"m10c-opportunity-{opportunityId}");

    private static string CandidateId(long opportunityId) => $"existing-comment-{opportunityId}";

    private static IReadOnlySet<EditorialDeliveryTreatment> AvailableTreatments() =>
        new HashSet<EditorialDeliveryTreatment> { EditorialDeliveryTreatment.DuckOverMusic };

    private bool BindingMatches(PreparedMaterialBinding binding, RadioTransitionDelivery transition) =>
        binding.OpportunityId == transition.OpportunityId
        && binding.SubjectTrack is not null
        && binding.SubjectTrack.Matches(transition.CommentedTrack)
        && binding.SessionId == _sessionId;

    private RadioTrackObservation CreateObservation(RadioTrackObservationKind kind, MediaSnapshot? previous,
        MediaSnapshot? current, OpportunityKernelResult result)
    {
        OpportunityKernelDecision? preparationDecision = result.Decisions.FirstOrDefault(
            decision => decision.Reason == OpportunityKernelDecisionReason.PreparationRequested);
        OpportunityKernelDecision? deliveryDecision = result.Decisions.FirstOrDefault(
            decision => decision.Reason == OpportunityKernelDecisionReason.DeliveryDue);
        IReadOnlyList<string> observations = result.Decisions.FirstOrDefault(
            decision => decision.Reason == OpportunityKernelDecisionReason.TransitionObserved)
            ?.TrustedListeningObservations ?? Array.Empty<string>();
        return new RadioTrackObservation(kind, previous, current, _kernelState.TransitionCounter,
            _kernelState.ScheduledThreshold, _kernelState.TotalObservedTransitions,
            Has(result, OpportunityKernelDecisionReason.PendingOpportunityInvalidated),
            Has(result, OpportunityKernelDecisionReason.PreparationMissedTransition),
            preparationDecision?.Preparation is null ? null : ToPreparation(preparationDecision.Preparation),
            deliveryDecision?.Delivery is null ? null : ToDelivery(deliveryDecision.Delivery), observations);
    }

    private static RadioPreparationOpportunity ToPreparation(OpportunityKernelPreparation preparation) => new(preparation.Id,
        new RadioPreparationContext(preparation.SubjectTrack, preparation.SubjectTrackStartedAt,
            preparation.TrustedListeningObservations, preparation.RecentAcceptedAiComments));

    private static RadioTransitionDelivery ToDelivery(OpportunityKernelDelivery delivery) => new(delivery.OpportunityId,
        delivery.CommentedTrack, delivery.NewTrack, delivery.TransitionObservedAt);

    private static bool Has(OpportunityKernelResult result, OpportunityKernelDecisionReason reason) =>
        result.Decisions.Any(decision => decision.Reason == reason);
}

internal sealed record M10CDeliveryReservation(long OpportunityId, PreparedMaterialReservationId ReservationId);
internal sealed record M11BTransitionSelection(EditorialDecision Decision, M10CDeliveryReservation? CommentReservation);