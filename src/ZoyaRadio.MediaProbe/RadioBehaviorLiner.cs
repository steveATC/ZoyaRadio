namespace ZoyaRadio.MediaProbe;

internal sealed partial class RadioBehaviorTest
{
    private void StartLinerPreparation()
    {
        DateTimeOffset now = _clock.Now;
        bool capacityAvailable;
        lock (_pipelineLock)
        {
            capacityAvailable = (_pipelineTask is null || _pipelineTask.IsCompleted)
                && _preparedOpportunity is null;
        }

        lock (_linerLock)
        {
            if (_linerInventory.HasPreparedSpeech
                || (_linerPreparationTask is not null && !_linerPreparationTask.IsCompleted)
                || now < _nextLinerPreparationAttemptAt)
            {
                return;
            }
        }

        EditorialDecision decision;
        long opportunityId;
        lock (_stateLock)
        {
            opportunityId = Math.Max(1, _state.TotalObservedTransitions + 1);
            decision = _state.EvaluateLinerPreparation(opportunityId, capacityAvailable);
        }

        WriteEditorialDecision(decision);
        if (decision.Outcome != EditorialDecisionOutcome.Prepare)
        {
            return;
        }

        var lifetime = new OperationLifetime(_shutdownToken);
        lock (_linerLock)
        {
            if (_linerInventory.HasPreparedSpeech
                || (_linerPreparationTask is not null && !_linerPreparationTask.IsCompleted))
            {
                lifetime.RequestTerminalDisposal();
                return;
            }

            _linerPreparationLifetime = lifetime;
            _linerPreparationTask = Task.Run(() => RunLinerPreparationAsync(lifetime, lifetime.Token));
        }

        _log.Write("REUSABLE LINER PREPARATION STARTED: fixed local copy only; music is untouched.");
    }

    private async Task RunLinerPreparationAsync(OperationLifetime lifetime, CancellationToken cancellationToken)
    {
        try
        {
            (ReusableLinerPreparationResult result, bool accepted) =
                await PrepareAndAcceptLinerSpeechAsync(
                    _linerPipeline,
                    cancellationToken,
                    _linerInventory,
                    _linerLock);

            _log.Write(
                $"REUSABLE LINER PREPARATION: {(accepted ? "READY" : result.Status.ToString().ToUpperInvariant())} - {result.Reason}.");
            if (!accepted)
            {
                lock (_linerLock)
                {
                    _nextLinerPreparationAttemptAt = _clock.Now + TimeSpan.FromMinutes(1);
                }
            }
        }
        catch (Exception exception)
        {
            _log.Warning("Unexpected reusable-liner preparation cleanup failure", exception);
            lock (_linerLock)
            {
                _nextLinerPreparationAttemptAt = _clock.Now + TimeSpan.FromMinutes(1);
            }
        }
        finally
        {
            FinalizeLinerPreparationLifetime(lifetime);
        }
    }


    internal static async Task<(ReusableLinerPreparationResult Result, bool Accepted)>
        PrepareAndAcceptLinerSpeechAsync(
            ReusableLinerPipeline pipeline,
            CancellationToken cancellationToken,
            ReusableLinerInventory inventory,
            object inventoryLock)
    {
        ReusableLinerPreparationResult result =
            await pipeline.PrepareAsync(cancellationToken);
        bool accepted;
        lock (inventoryLock)
        {
            accepted = TryAcceptPreparedLinerSpeech(
                result,
                cancellationToken,
                inventory);
        }

        return (result, accepted);
    }

    internal static bool TryAcceptPreparedLinerSpeech(
        ReusableLinerPreparationResult result,
        CancellationToken cancellationToken,
        ReusableLinerInventory inventory)
    {
        IPreparedAnnouncementSpeech? speech = result.Speech;
        bool accepted = false;
        try
        {
            if (result.Status != ReusableLinerPreparationStatus.Ready || speech is null)
            {
                return false;
            }

            accepted = inventory.TryAcceptPreparedSpeech(speech, cancellationToken);
            return accepted;
        }
        finally
        {
            if (!accepted)
            {
                speech?.Dispose();
            }
        }
    }

    internal void CancelLinerPreparation(string reason)
    {
        OperationLifetime.CancellationLease? lease;
        _cancellationGatesForTesting?.ContenderBeforeLock?.Invoke();
        lock (_linerLock)
        {
            _cancellationGatesForTesting?.LockAcquired?.Invoke();
            lease = _linerPreparationLifetime?.TryAcquireCancellationLease();
            if (lease is not null) _cancellationGatesForTesting?.LeaseAcquired?.Invoke();
        }
        if (lease is null) return;
        Exception? failure = null;
        try
        {
            try { WriteCancellationLog($"CANCELLING LOW-PRIORITY LINER PREPARATION: {reason}."); }
            catch (Exception exception) { failure ??= exception; }
            try
            {
                _cancellationGatesForTesting?.BeforeCancellation?.Invoke();
                lease.Cancel();
            }
            catch (Exception exception) { failure ??= exception; }
        }
        finally
        {
            try { lease.Dispose(); } catch (Exception exception) { failure ??= exception; }
        }
        if (failure is not null) throw failure;
    }

    internal void FinalizeLinerPreparationLifetime(OperationLifetime lifetime)
    {
        lock (_linerLock)
        {
            if (ReferenceEquals(_linerPreparationLifetime, lifetime))
            {
                _linerPreparationLifetime = null;
                _linerPreparationTask = null;
            }
        }
        lifetime.RequestTerminalDisposal();
    }

    internal void InstallLinerPreparationForTesting(OperationLifetime lifetime, Task? task = null)
    {
        lock (_linerLock)
        {
            _linerPreparationLifetime = lifetime;
            _linerPreparationTask = task;
        }
    }

    internal (OperationLifetime? Lifetime, Task? Task) LinerPreparationForTesting()
    {
        lock (_linerLock) return (_linerPreparationLifetime, _linerPreparationTask);
    }

    private async Task WaitForLinerPreparationAsync()
    {
        Task? task;
        lock (_linerLock)
        {
            task = _linerPreparationTask;
        }

        if (task is not null)
        {
            await task;
        }
    }

    private void TryStartLiner(RadioTrackObservation observation)
    {
        if (observation.Kind != RadioTrackObservationKind.Transition
            || observation.CurrentTrack is null)
        {
            return;
        }

        long transitionSequence;
        PreparedMaterialSessionId sessionId;
        bool silentMode;
        lock (_stateLock)
        {
            transitionSequence = _state.TotalObservedTransitions;
            sessionId = _state.CurrentSessionId;
            silentMode = _state.SilentMode;
        }

        bool pipelineAvailable;
        lock (_pipelineLock)
        {
            pipelineAvailable = (_pipelineTask is null || _pipelineTask.IsCompleted)
                && _preparedOpportunity is null;
        }

        var transition = new ReusableLinerTransition(
            transitionSequence,
            observation.CurrentTrack,
            _clock.Now,
            sessionId);
        EditorialAirtimeCandidate candidate;
        lock (_linerLock)
        {
            candidate = _linerInventory.CreateCandidate(
                transition,
                silentMode,
                protectedPreparationBoundaryClear: observation.PreparationOpportunity is null,
                missedCommentBoundaryClear: !observation.PreparationMissedTransition
                    && observation.Delivery is null,
                treatmentAvailable: pipelineAvailable);
        }

        M11BTransitionSelection selection;
        lock (_stateLock)
        {
            selection = _state.TryReserveTransition(
                transition: null,
                linerCandidate: candidate,
                transition.ObservedAt);
        }

        WriteEditorialDecision(selection.Decision);
        if (selection.Decision.Outcome != EditorialDecisionOutcome.ReserveForAirtime
            || selection.Decision.SelectedCandidateId != ReusableLinerInventory.CandidateId)
        {
            StartLinerPreparation();
            return;
        }

        ReusableLinerReservation? reservation;
        lock (_linerLock)
        {
            reservation = _linerInventory.TryReserve(transition);
        }

        if (reservation is null)
        {
            _log.Write("REUSABLE LINER ABANDONED: selected material could not be reserved. Music continues.");
            return;
        }

        var lifetime = new OperationLifetime(_shutdownToken);
        lock (_pipelineLock)
        {
            if ((_pipelineTask is not null && !_pipelineTask.IsCompleted)
                || _preparedOpportunity is not null)
            {
                lock (_linerLock)
                {
                    _linerInventory.Release(reservation, _clock.Now);
                }

                lifetime.RequestTerminalDisposal();
                _log.Write("REUSABLE LINER RELEASED: another protected pipeline won the boundary.");
                return;
            }

            _pendingLifetime = lifetime;
            _pipelinePhase = PipelineOperationPhase.Delivering;
            _pipelineTask = Task.Run(() => RunLinerDeliveryAndFinalizeAsync(reservation, lifetime, lifetime.Token));
        }

        _log.Write(
            $"REUSABLE LINER RESERVED: transition {transition.TransitionSequence}; final Firefox safety will be revalidated.");
        ReportActivity(new RadioActivitySnapshot(RadioActivityState.ValidatingFirefoxTarget));
    }

    private async Task RunLinerDeliveryAndFinalizeAsync(
        ReusableLinerReservation reservation,
        OperationLifetime lifetime,
        CancellationToken cancellationToken)
    {
        try
        {
            TransitionDeliveryResult result =
                await _linerPipeline.DeliverAsync(reservation, cancellationToken);
            bool returned;
            lock (_linerLock)
            {
                returned = result.Status == TransitionDeliveryStatus.Completed
                    ? _linerInventory.Complete(reservation, _clock.Now)
                    : _linerInventory.Release(reservation, _clock.Now);
            }

            _log.Write(
                $"REUSABLE LINER RESULT: {result.Status.ToString().ToUpperInvariant()} - {result.Reason}. " +
                $"Lifecycle {(returned ? "returned to Ready inventory" : "was invalidated or already resolved")}.");
            ReportActivity(result.Status == TransitionDeliveryStatus.Completed
                ? CreateObservingActivity()
                : new RadioActivitySnapshot(RadioActivityState.OpportunityAbandoned));
        }
        catch (Exception exception)
        {
            _log.Warning("Unexpected reusable-liner delivery cleanup failure", exception);
            lock (_linerLock)
            {
                _linerInventory.Release(reservation, _clock.Now);
            }

            ReportActivity(new RadioActivitySnapshot(RadioActivityState.OpportunityAbandoned));
        }
        finally
        {
            lock (_pipelineLock)
            {
                if (ReferenceEquals(_pendingLifetime, lifetime))
                {
                    _pendingLifetime = null;
                    _pipelinePhase = PipelineOperationPhase.None;
                    _pipelineTask = null;
                }
            }
            lifetime.RequestTerminalDisposal();
        }
    }

    private void InvalidateLinerInventory()
    {
        CancelLinerPreparation("the logical Firefox runtime session is ending");
        lock (_linerLock)
        {
            _linerInventory.Invalidate();
        }
    }

    private void RearmLinerInventory()
    {
        lock (_linerLock)
        {
            _linerInventory.Arm(_state.CurrentSessionId, _clock.Now, _state.TotalObservedTransitions);
            _nextLinerPreparationAttemptAt = DateTimeOffset.MinValue;
        }

        StartLinerPreparation();
    }
}
