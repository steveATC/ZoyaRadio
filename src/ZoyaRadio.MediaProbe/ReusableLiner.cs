namespace ZoyaRadio.MediaProbe;

internal sealed record ReusableLinerPolicy(
    int MinimumTransitions,
    TimeSpan MinimumElapsed,
    int MinimumTransitionsSinceHostSpeech,
    TimeSpan MaximumDuration)
{
    public static ReusableLinerPolicy M11BExperimental { get; } = new(
        MinimumTransitions: 6,
        MinimumElapsed: TimeSpan.FromMinutes(20),
        MinimumTransitionsSinceHostSpeech: 2,
        MaximumDuration: TimeSpan.FromSeconds(4));

    public void Validate()
    {
        if (MinimumTransitions < 1) throw new ArgumentOutOfRangeException(nameof(MinimumTransitions));
        if (MinimumElapsed <= TimeSpan.Zero) throw new ArgumentOutOfRangeException(nameof(MinimumElapsed));
        if (MinimumTransitionsSinceHostSpeech < 0)
            throw new ArgumentOutOfRangeException(nameof(MinimumTransitionsSinceHostSpeech));
        if (MaximumDuration <= TimeSpan.Zero) throw new ArgumentOutOfRangeException(nameof(MaximumDuration));
    }
}

internal sealed record ReusableLinerTransition(
    long TransitionSequence,
    MediaSnapshot CurrentTrack,
    DateTimeOffset ObservedAt,
    PreparedMaterialSessionId SessionId);

internal sealed record ReusableLinerReservation(
    PreparedMaterialId MaterialId,
    PreparedMaterialReservationId ReservationId,
    ReusableLinerTransition Transition,
    IPreparedAnnouncementSpeech Speech);

internal sealed class ReusableLinerInventory : IDisposable
{
    public const string CandidateId = "reusable-liner";
    public const string FixtureText = "Zoya Radio.";

    private readonly PreparedMaterialLifecycle _lifecycle = new();
    private readonly ReusableLinerPolicy _policy;
    private PreparedMaterialLifecycleState _state = PreparedMaterialLifecycleState.Initial;
    private PreparedMaterialSessionId? _sessionId;
    private PreparedMaterialId? _materialId;
    private PreparedPayloadHandle? _payloadHandle;
    private IPreparedAnnouncementSpeech? _speech;
    private DateTimeOffset _armedAt;
    private long _armedTransition;
    private DateTimeOffset? _lastCompletedAt;
    private long? _lastCompletedTransition;
    private long? _lastHostSpeechTransition;
    private long _reservationSequence;
    private bool _disposed;

    public ReusableLinerInventory(ReusableLinerPolicy? policy = null)
    {
        _policy = policy ?? ReusableLinerPolicy.M11BExperimental;
        _policy.Validate();
    }

    public PreparedMaterialSessionId? SessionId => _sessionId;
    public PreparedMaterialId? MaterialId => _materialId;
    public PreparedMaterialLifecycleStatus? Status =>
        _materialId is not null && _state.Materials.TryGetValue(_materialId.Value, out PreparedMaterialRecord? material)
            ? material.Status
            : null;
    public bool HasPreparedSpeech => _speech is not null;

    public void Arm(PreparedMaterialSessionId sessionId, DateTimeOffset observedAt, long transitionSequence)
    {
        ObjectDisposedException.ThrowIf(_disposed, this);
        if (string.IsNullOrWhiteSpace(sessionId.Value)) throw new ArgumentException("Session identity is required.");
        Invalidate();

        _sessionId = sessionId;
        _materialId = new PreparedMaterialId($"m11b-liner-{sessionId.Value}");
        _payloadHandle = new PreparedPayloadHandle($"m11b-liner-payload-{sessionId.Value}");
        _armedAt = observedAt;
        _armedTransition = transitionSequence;
        _lastCompletedAt = null;
        _lastCompletedTransition = null;
        _lastHostSpeechTransition = null;
        _reservationSequence = 0;

        Apply(new PreparedMaterialRegister(
            _materialId.Value,
            PreparedMaterialReusePolicy.ReusableWithinBinding,
            new PreparedMaterialBinding(SessionId: sessionId),
            observedAt));
    }

    public bool AcceptPreparedSpeech(IPreparedAnnouncementSpeech speech)
    {
        ArgumentNullException.ThrowIfNull(speech);
        ObjectDisposedException.ThrowIf(_disposed, this);

        if (_materialId is null || _payloadHandle is null || _sessionId is null
            || speech.MeasuredDuration > _policy.MaximumDuration)
        {
            speech.Dispose();
            return false;
        }

        PreparedMaterialLifecycleResult result = Apply(new PreparedMaterialAttachPayload(
            _materialId.Value,
            _payloadHandle.Value));
        bool accepted = result.Decisions.Any(decision =>
            decision.Reason == PreparedMaterialDecisionReason.PayloadOwnershipAccepted);
        if (!accepted)
        {
            speech.Dispose();
            return false;
        }

        _speech = speech;
        return true;
    }

    public EditorialAirtimeCandidate CreateCandidate(
        ReusableLinerTransition transition,
        bool silentMode,
        bool protectedPreparationBoundaryClear,
        bool missedCommentBoundaryClear,
        bool treatmentAvailable)
    {
        PreparedMaterialRecord? material = null;
        if (_materialId is not null)
        {
            _state.Materials.TryGetValue(_materialId.Value, out material);
        }

        bool sessionMatches = _sessionId == transition.SessionId
            && material?.Binding.SessionId == transition.SessionId;
        DateTimeOffset elapsedAnchor = _lastCompletedAt ?? _armedAt;
        long transitionAnchor = _lastCompletedTransition ?? _armedTransition;
        bool cooldownSatisfied = transition.TransitionSequence - transitionAnchor >= _policy.MinimumTransitions
            && transition.ObservedAt - elapsedAnchor >= _policy.MinimumElapsed;
        bool spacingSatisfied = _lastHostSpeechTransition is null
            || transition.TransitionSequence - _lastHostSpeechTransition.Value
                >= _policy.MinimumTransitionsSinceHostSpeech;

        return new EditorialAirtimeCandidate(
            CandidateId,
            transition.TransitionSequence,
            _materialId ?? new PreparedMaterialId("m11b-liner-unarmed"),
            EditorialContentFormat.ReusableLiner,
            EditorialDeliveryTreatment.DuckOverMusic,
            EditorialPolicyPriority.Routine,
            OpportunityMatches: !silentMode && sessionMatches,
            MaterialStatus: material?.Status,
            HasPayload: _speech is not null && material?.PayloadHandle is not null,
            BindingMatches: sessionMatches,
            ExpiresAt: material?.Binding.ExpiresAt,
            CandidatePressure: new EditorialPressureSnapshot(
                treatmentAvailable ? EditorialMusicPressure.SpeechPermitted : EditorialMusicPressure.PreferMusic,
                spacingSatisfied,
                cooldownSatisfied),
            ProtectedPreparationBoundaryClear: protectedPreparationBoundaryClear,
            MissedCommentBoundaryClear: missedCommentBoundaryClear);
    }

    public ReusableLinerReservation? TryReserve(ReusableLinerTransition transition)
    {
        if (_materialId is null || _sessionId != transition.SessionId || _speech is null)
        {
            return null;
        }

        var reservationId = new PreparedMaterialReservationId(
            $"m11b-liner-use-{transition.TransitionSequence}-{++_reservationSequence}");
        PreparedMaterialLifecycleResult result = Apply(new PreparedMaterialReserve(
            _materialId.Value,
            reservationId,
            new PreparedMaterialUseContext(
                transition.ObservedAt,
                SessionId: transition.SessionId)));
        return result.Decisions.Any(decision =>
            decision.Reason == PreparedMaterialDecisionReason.ReservationAcquired)
                ? new ReusableLinerReservation(_materialId.Value, reservationId, transition, _speech)
                : null;
    }

    public bool Release(ReusableLinerReservation reservation, DateTimeOffset observedAt)
    {
        ArgumentNullException.ThrowIfNull(reservation);
        PreparedMaterialLifecycleResult result = Apply(new PreparedMaterialRelease(
            reservation.MaterialId,
            reservation.ReservationId,
            new PreparedMaterialUseContext(observedAt, SessionId: reservation.Transition.SessionId)));
        return result.Decisions.Any(decision =>
            decision.Reason == PreparedMaterialDecisionReason.ReservationReleased);
    }

    public bool Complete(ReusableLinerReservation reservation, DateTimeOffset observedAt)
    {
        ArgumentNullException.ThrowIfNull(reservation);
        PreparedMaterialLifecycleResult result = Apply(new PreparedMaterialComplete(
            reservation.MaterialId,
            reservation.ReservationId,
            PreparedMaterialCompletionDisposition.ReturnToInventory,
            new PreparedMaterialUseContext(observedAt, SessionId: reservation.Transition.SessionId)));
        bool completed = result.Decisions.Any(decision =>
            decision.Reason == PreparedMaterialDecisionReason.CompletedReturnedToInventory);
        if (completed)
        {
            _lastCompletedAt = observedAt;
            _lastCompletedTransition = reservation.Transition.TransitionSequence;
            _lastHostSpeechTransition = reservation.Transition.TransitionSequence;
        }

        return completed;
    }

    public void RecordHostSpeechCompleted(long transitionSequence)
    {
        if (transitionSequence > 0)
        {
            _lastHostSpeechTransition = transitionSequence;
        }
    }

    public void Invalidate()
    {
        if (_materialId is not null && _sessionId is not null
            && _state.Materials.TryGetValue(_materialId.Value, out PreparedMaterialRecord? material)
            && material.Status != PreparedMaterialLifecycleStatus.Terminal)
        {
            Apply(new PreparedMaterialInvalidateBinding(
                _materialId.Value,
                PreparedMaterialBindingKind.Session,
                SessionId: _sessionId));
        }

        DisposeSpeech();
        _sessionId = null;
        _materialId = null;
        _payloadHandle = null;
    }

    public void Dispose()
    {
        if (_disposed) return;
        Invalidate();
        _disposed = true;
    }

    private PreparedMaterialLifecycleResult Apply(PreparedMaterialLifecycleInput input)
    {
        PreparedMaterialLifecycleResult result = _lifecycle.Apply(_state, input);
        _state = result.State;
        if (result.Decisions.Any(decision =>
            decision.Reason == PreparedMaterialDecisionReason.PayloadDisposalRequested))
        {
            DisposeSpeech();
        }

        return result;
    }

    private void DisposeSpeech()
    {
        IPreparedAnnouncementSpeech? speech = Interlocked.Exchange(ref _speech, null);
        speech?.Dispose();
    }
}

internal static class ReusableLinerDeliverySafety
{
    public static RadioSafetyDecision Evaluate(
        ReusableLinerTransition transition,
        RadioOpportunitySnapshot snapshot,
        DateTimeOffset now,
        TimeSpan deliveryWindow)
    {
        if (!snapshot.Armed) return Block("automatic behavior is not armed");
        if (snapshot.SilentMode) return Block("Silent Mode is ON");
        if (!snapshot.SessionListReadSucceeded || snapshot.MatchingFirefoxSessionCount != 1)
            return Block("the exact Firefox media-session target is missing or ambiguous");
        if (!snapshot.SameSelectedSession)
            return Block("the Firefox media session is not the originally selected object");
        if (!snapshot.SourceReadSucceeded
            || !string.Equals(snapshot.SourceId, FirefoxSessionSelector.VerifiedSourceId,
                StringComparison.OrdinalIgnoreCase))
            return Block("the selected Firefox source identifier could not be revalidated");
        if (!snapshot.MediaReadSucceeded
            || !AnnouncementText.HasSameTitleAndArtist(transition.CurrentTrack, snapshot.CurrentMedia))
            return Block("the transition track changed before liner delivery could begin");
        if (!snapshot.PlaybackReadSucceeded || !PlaybackControlRules.CanStart(snapshot.PlaybackStatus))
            return Block("the transition track is not Playing");

        TimeSpan elapsed = now - transition.ObservedAt;
        if (elapsed < TimeSpan.Zero || elapsed > deliveryWindow)
            return Block($"the {deliveryWindow.TotalSeconds:0.0}-second liner delivery window was missed");

        return new RadioSafetyDecision(true,
            "the transition track and original Firefox media session are current and Playing");
    }

    private static RadioSafetyDecision Block(string reason) => new(false, reason);
}
internal enum ReusableLinerPreparationStatus { Ready, Abandoned, Failed }
internal sealed record ReusableLinerPreparationResult(
    ReusableLinerPreparationStatus Status,
    string Reason,
    IPreparedAnnouncementSpeech? Speech);

internal sealed class ReusableLinerPipeline
{
    private readonly ITransitionRadioHost _host;
    private readonly IAnnouncementLog _log;
    private readonly ReusableLinerPolicy _policy;

    public ReusableLinerPipeline(
        ITransitionRadioHost host,
        IAnnouncementLog log,
        ReusableLinerPolicy? policy = null)
    {
        _host = host;
        _log = log;
        _policy = policy ?? ReusableLinerPolicy.M11BExperimental;
        _policy.Validate();
    }

    public async Task<ReusableLinerPreparationResult> PrepareAsync(CancellationToken cancellationToken)
    {
        IPreparedAnnouncementSpeech? speech = null;
        try
        {
            speech = await _host.PrepareSpeechAsync(ReusableLinerInventory.FixtureText, cancellationToken);
            cancellationToken.ThrowIfCancellationRequested();
            if (speech.MeasuredDuration > _policy.MaximumDuration)
            {
                IPreparedAnnouncementSpeech rejected = speech;
                speech = null;
                return new ReusableLinerPreparationResult(
                    ReusableLinerPreparationStatus.Abandoned,
                    $"liner measured {rejected.MeasuredDuration.TotalSeconds:0.###} seconds, exceeding 4 seconds",
                    rejected);
            }

            IPreparedAnnouncementSpeech ready = speech;
            speech = null;
            return new ReusableLinerPreparationResult(
                ReusableLinerPreparationStatus.Ready,
                "fixed reusable liner speech is ready in memory",
                ready);
        }
        catch (OperationCanceledException)
        {
            return new ReusableLinerPreparationResult(
                ReusableLinerPreparationStatus.Abandoned,
                "liner preparation was cancelled",
                null);
        }
        catch (Exception exception)
        {
            _log.Warning("Reusable liner preparation failed", exception);
            return new ReusableLinerPreparationResult(
                ReusableLinerPreparationStatus.Failed,
                exception.Message,
                null);
        }
        finally
        {
            speech?.Dispose();
        }
    }

    public async Task<TransitionDeliveryResult> DeliverAsync(
        ReusableLinerReservation reservation,
        CancellationToken cancellationToken)
    {
        try
        {
            DateTimeOffset deadline = reservation.Transition.ObservedAt + TimeSpan.FromSeconds(2);
            TransitionDuckingResult result = await _host.RunDuckedLinerAsync(
                reservation.Transition,
                reservation.Speech,
                _policy.MaximumDuration,
                deadline,
                cancellationToken);
            return result.Status switch
            {
                TransitionDuckingStatus.Completed => new(
                    TransitionDeliveryStatus.Completed,
                    "reusable liner completed and exact volume restoration passed",
                    result),
                TransitionDuckingStatus.Refused => new(
                    TransitionDeliveryStatus.Abandoned,
                    result.Reason,
                    result),
                TransitionDuckingStatus.Cancelled => new(
                    TransitionDeliveryStatus.Abandoned,
                    result.Reason,
                    result),
                _ => new(TransitionDeliveryStatus.Failed, result.Reason, result),
            };
        }
        catch (OperationCanceledException)
        {
            return new TransitionDeliveryResult(
                TransitionDeliveryStatus.Abandoned,
                "liner delivery was cancelled; protected restoration completed",
                null);
        }
        catch (Exception exception)
        {
            _log.Warning("Reusable liner delivery failed", exception);
            return new TransitionDeliveryResult(TransitionDeliveryStatus.Failed, exception.Message, null);
        }
    }
}
