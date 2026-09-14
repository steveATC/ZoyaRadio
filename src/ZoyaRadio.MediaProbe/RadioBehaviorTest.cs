using Windows.Media.Control;

namespace ZoyaRadio.MediaProbe;

internal sealed partial class RadioBehaviorTest : IRadioRuntimeSession
{
    private readonly GlobalSystemMediaTransportControlsSessionManager _manager;
    private readonly LocalLog _log;
    private readonly RadioBehaviorConfiguration _radioConfiguration;
    private readonly IRadioClock _clock;
    private readonly M10CRuntimeMigrationState _state;
    private readonly RadioSessionAccess _sessionAccess;
    private readonly TransitionRadioPipeline _pipeline;
    private readonly ReusableLinerInventory _linerInventory;
    private readonly ReusableLinerPipeline _linerPipeline;
    private readonly M9BTransitionCommentProvider? _m9bCommentProvider;
    private readonly object _runtimeLifecycleLock = new();
    private readonly object _statusLock = new();
    private readonly object _stateLock = new();
    private readonly object _pipelineLock = new();
    private readonly object _linerLock = new();
    private readonly SemaphoreSlim _mediaEventGate = new(1, 1);
    private GlobalSystemMediaTransportControlsSession? _selectedSession;
    private CancellationToken _shutdownToken;
    private OperationLifetime? _pendingLifetime;
    private PipelineOperationPhase _pipelinePhase;
    private Task? _pipelineTask;
    private PreparedTransitionOpportunity? _preparedOpportunity;
    private OperationLifetime? _linerPreparationLifetime;
    private Task? _linerPreparationTask;
    private DateTimeOffset _nextLinerPreparationAttemptAt = DateTimeOffset.MinValue;
    private bool _simulateNextTransitionFailure;
    private bool _mediaSubscribed;
    private bool _managerSubscribed;
    private CancellationTokenSource? _runtimeCancellation;
    private Task? _runtimeStopTask;
    private bool _runtimeStarted;
    private RadioRuntimeStatusUpdate? _lastStatus;
    private RadioActivitySnapshot _activity = new(RadioActivityState.Stopped);
    private Action<string>? _cancellationLogForTesting;
    private Action<PreparedTransitionOpportunity, string>? _discardPreparedForTesting;
    private CancellationCoordinatorTestGates? _cancellationGatesForTesting;

    internal enum PipelineOperationPhase { None, Preparing, Ready, Delivering }

    internal sealed class OperationLifetime
    {
        private readonly object _gate = new();
        private readonly CancellationTokenSource _source;
        private int _leases;
        private bool _terminal;
        private bool _disposed;

        public OperationLifetime(CancellationToken shutdownToken)
        {
            _source = CancellationTokenSource.CreateLinkedTokenSource(shutdownToken);
            Token = _source.Token;
        }

        internal OperationLifetime(CancellationTokenSource source)
        {
            _source = source;
            Token = source.Token;
        }

        public CancellationToken Token { get; }
        public bool IsCancellationRequested => Token.IsCancellationRequested;
        internal bool IsDisposed
        {
            get
            {
                lock (_gate) return _disposed;
            }
        }

        public CancellationLease? TryAcquireCancellationLease()
        {
            lock (_gate)
            {
                if (_terminal || _disposed) return null;
                _leases++;
                return new CancellationLease(this);
            }
        }

        public void RequestTerminalDisposal() => CompleteLease(null);

        private void CompleteLease(CancellationLease? lease)
        {
            CancellationTokenSource? dispose = null;
            lock (_gate)
            {
                if (lease is not null) _leases--;
                else _terminal = true;
                if (_terminal && _leases == 0 && !_disposed) { _disposed = true; dispose = _source; }
            }
            dispose?.Dispose();
        }

        private void Cancel() => _source.Cancel();

        public sealed class CancellationLease : IDisposable
        {
            private OperationLifetime? _owner;
            internal CancellationLease(OperationLifetime owner) => _owner = owner;
            public void Cancel() => _owner?.Cancel();
            public void Dispose()
            {
                OperationLifetime? owner = Interlocked.Exchange(ref _owner, null);
                owner?.CompleteLease(this);
            }
        }
    }

    internal sealed class CancellationCoordinatorTestGates
    {
        internal Action? StateContenderBeforeLock { get; init; }
        internal Action? StateLockAcquired { get; init; }
        internal Action? ContenderBeforeLock { get; init; }
        internal Action? LockAcquired { get; init; }
        internal Action? LeaseAcquired { get; init; }
        internal Action? StateTransitioned { get; init; }
        internal Action? BeforeCancellation { get; init; }
    }

    public RadioBehaviorTest(
        GlobalSystemMediaTransportControlsSessionManager manager,
        LocalLog log,
        IAnnouncementSpeechService speechService,
        ILocalCommentGenerator generator,
        AiCommentConfiguration aiConfiguration,
        HostPersonaConfiguration persona,
        RadioBehaviorConfiguration? radioConfiguration = null,
        ITransitionTargetSource? targetSource = null,
        IRadioClock? clock = null,
        ILocalCommentGenerator? m9bGenerator = null,
        AiCommentConfiguration? m9bConfiguration = null,
        M9BContextProvider? m9bContextProvider = null,
        bool quickReactionPilotEnabled = false)
    {
        _manager = manager;
        _log = log;
        _radioConfiguration = radioConfiguration ?? RadioBehaviorConfiguration.Default;
        _radioConfiguration.Validate();
        _clock = clock ?? new SystemRadioClock();
        _state = new M10CRuntimeMigrationState(
            _radioConfiguration,
            targetSource ?? new RandomTransitionTargetSource());
        _sessionAccess = new RadioSessionAccess(manager, log);

        var aiService = new AiCommentService(generator, aiConfiguration, log, persona);
        ITransitionCommentProvider commentProvider = quickReactionPilotEnabled ? new QuickReactionTransitionCommentProvider(aiService) : new LocalTransitionCommentProvider(aiService);
        if (quickReactionPilotEnabled) log.Write("M12D PILOT: quick reaction explicitly enabled; lifecycle and overlay safety remain controlling.");
        if (!quickReactionPilotEnabled && m9bGenerator is not null && m9bConfiguration is not null && m9bContextProvider is not null)
        {
            var recent = new M9BRecentCommentState();
            var grounded = new M9BGroundedCommentService(
                m9bContextProvider,
                m9bGenerator,
                m9bConfiguration,
                recent,
                log);
            _m9bCommentProvider = new M9BTransitionCommentProvider(commentProvider, grounded, log);
            commentProvider = _m9bCommentProvider;
            log.Write("M9B STATUS: grounded 14B candidate is configured; M8 fallback remains available.");
        }
        else
        {
            log.Write("M9B STATUS: grounded candidate is not configured; accepted M8 behavior remains in use.");
        }

        var transitionDucker = new WindowsTransitionAudioDucker(log, clock: _clock, activityChanged: ReportActivity);
        var opportunityHost = new WindowsRadioOpportunityHost(
            () => _selectedSession,
            GetRuntimeState,
            _sessionAccess,
            speechService,
            _clock,
            transitionDucker,
            log);
        _pipeline = new TransitionRadioPipeline(
            _radioConfiguration,
            commentProvider,
            opportunityHost,
            _clock,
            log,
            ReportActivity);
        _linerInventory = new ReusableLinerInventory();
        _linerPipeline = new ReusableLinerPipeline(opportunityHost, log);
    }

    internal RadioBehaviorTest(LocalLog log, IRadioClock clock, ITransitionTargetSource targetSource)
    {
        _manager = null!;
        _log = log;
        _radioConfiguration = RadioBehaviorConfiguration.Default;
        _clock = clock;
        _state = new M10CRuntimeMigrationState(_radioConfiguration, targetSource);
        _sessionAccess = null!;
        _pipeline = null!;
        _linerInventory = new ReusableLinerInventory();
        _linerPipeline = null!;
    }

    private async void OnMediaPropertiesChanged(
        GlobalSystemMediaTransportControlsSession sender,
        MediaPropertiesChangedEventArgs args)
    {
        try
        {
            await HandleMediaPropertiesChangedAsync(sender);
        }
        catch (Exception exception)
        {
            _log.Warning("Unexpected automatic track-change observer failure", exception);
        }
    }

    private async Task HandleMediaPropertiesChangedAsync(
        GlobalSystemMediaTransportControlsSession eventSession)
    {
        await _mediaEventGate.WaitAsync();
        RadioPreparationOpportunity? preparation = null;
        RadioTransitionDelivery? delivery = null;
        RadioTrackObservation? observation = null;

        try
        {
            if (_selectedSession is null || !ReferenceEquals(eventSession, _selectedSession))
            {
                _log.Write("STALE MEDIA EVENT IGNORED: it came from a Firefox GSMTC object that is no longer selected.");
                return;
            }

            MediaSnapshot? current = await _sessionAccess.TryReadMediaAsync(_selectedSession);
            if (current is null)
            {
                return;
            }


            lock (_stateLock)
            {
                observation = _state.ObserveTrack(current, _clock.Now);
            }

            if (observation.Kind == RadioTrackObservationKind.Duplicate)
            {
                return;
            }

            if (observation.Kind == RadioTrackObservationKind.Unusable)
            {
                _log.Write("MEDIA EVENT IGNORED: current title or artist was unavailable.");
                return;
            }

            WriteTrackTransition(observation);
            RadioRuntimeState runtime = GetRuntimeState();
            PublishRuntimeStatus(
                observation.CurrentTrack!.Title,
                observation.CurrentTrack.Artist,
                FirefoxTargetState.Safe,
                runtime.SilentMode,
                runtime.SilentMode ? "Running silently" : "Track transition observed",
                CreateTransitionActivity(observation, runtime.SilentMode));

            if (observation.PendingOpportunityInvalidated)
            {
                _log.Write(
                    observation.PreparationMissedTransition
                        ? "PENDING COMMENT ABANDONED: Track A changed before AI/Nicole preparation finished."
                        : "PENDING COMMENT ABANDONED: a rapid additional transition made the prepared material stale.");
                CancelPendingPipeline(
                    observation.PreparationMissedTransition
                        ? "Track A changed before preparation completed"
                        : "Track B changed again before delivery completed");
                WriteNextSchedule();
            }

            preparation = observation.PreparationOpportunity;
            delivery = observation.Delivery;
        }
        finally
        {
            _mediaEventGate.Release();
        }

        if (delivery is not null)
        {
            StartDelivery(delivery);
        }
        else if (preparation is not null)
        {
            StartPreparation(preparation);
        }
        else
        {
            TryStartLiner(observation!);
        }
    }

    private async void OnSessionsChanged(
        GlobalSystemMediaTransportControlsSessionManager sender,
        SessionsChangedEventArgs args)
    {
        try
        {
            await HandleSessionsChangedAsync();
        }
        catch (Exception exception)
        {
            _log.Warning("Could not validate a Windows session-list change", exception);
            InvalidatePendingForSessionSafety(
                "the Windows media-session list could not be validated");
        }
    }

    private async Task HandleSessionsChangedAsync()
    {
        await _mediaEventGate.WaitAsync();

        try
        {
            RadioRuntimeState runtime = GetRuntimeState();
            if (!runtime.Armed || _selectedSession is null)
            {
                return;
            }

            bool sessionListReadSucceeded = _sessionAccess.TryReadExactFirefoxMatches(
                out IReadOnlyList<GlobalSystemMediaTransportControlsSession> matches);
            FirefoxMediaSessionChange<GlobalSystemMediaTransportControlsSession> change =
                FirefoxMediaSessionLifecycle.Evaluate(
                    sessionListReadSucceeded,
                    matches);

            switch (change.Status)
            {
                case FirefoxMediaSessionChangeStatus.SessionListUnavailable:
                    _log.Write("FIREFOX GSMTC SESSION: the media-session list is unavailable.");
                    PublishTargetStatus(FirefoxTargetState.Unavailable, "Windows media-session list unavailable");
                    InvalidatePendingForSessionSafety("the Windows media-session list is unavailable");
                    return;
                case FirefoxMediaSessionChangeStatus.NoFirefoxCandidate:
                    _log.Write("FIREFOX GSMTC CANDIDATES: ZERO exact firefox.exe sessions. Staying silent.");
                    PublishTargetStatus(FirefoxTargetState.Missing, "Firefox media session is missing");
                    InvalidatePendingForSessionSafety("zero Firefox media-session candidates are available");
                    return;
                case FirefoxMediaSessionChangeStatus.AmbiguousFirefoxCandidates:
                    _log.Write(
                        $"FIREFOX GSMTC CANDIDATES: MULTIPLE/AMBIGUOUS " +
                        $"({change.MatchingFirefoxSessionCount} exact firefox.exe sessions). Staying silent.");
                    PublishTargetStatus(FirefoxTargetState.Ambiguous, "Firefox media-session targeting is ambiguous");
                    InvalidatePendingForSessionSafety("multiple Firefox media-session candidates are ambiguous");
                    return;
                case FirefoxMediaSessionChangeStatus.SingleFirefoxCandidate:
                    await HandleSingleFirefoxCandidateAsync(change.Candidate!);
                    return;
                default:
                    throw new InvalidOperationException("Unknown Firefox media-session lifecycle decision.");
            }
        }
        finally
        {
            _mediaEventGate.Release();
        }
    }

    private async Task HandleSingleFirefoxCandidateAsync(
        GlobalSystemMediaTransportControlsSession candidate)
    {
        GlobalSystemMediaTransportControlsSession selected = _selectedSession!;
        bool sameWrapper = ReferenceEquals(selected, candidate);
        RadioSessionDetails selectedDetails = await _sessionAccess.ReadSessionDetailsAsync(selected);
        FirefoxSessionContinuityEvidence selectedEvidence =
            FirefoxSessionContinuityEvidence.FromDetails(selectedDetails);
        RadioSessionDetails candidateDetails = sameWrapper
            ? selectedDetails
            : await _sessionAccess.ReadSessionDetailsAsync(candidate);
        FirefoxSessionContinuityEvidence candidateEvidence =
            FirefoxSessionContinuityEvidence.FromDetails(candidateDetails);
        FirefoxSessionContinuityDecision decision = FirefoxSessionContinuity.Evaluate(
            sameWrapper,
            selectedEvidence,
            candidateEvidence);

        if (decision.PreservesPendingAndActiveWork)
        {
            switch (decision.Status)
            {
                case FirefoxSessionContinuityStatus.SelectedWrapperStillValid:
                    _log.Write(
                        "GLOBAL GSMTC COLLECTION CHANGED: selected Firefox remains readable, Playing, " +
                        "eligible, and unambiguous. Unrelated application change ignored; pending, " +
                        "prepared, and active work plus the current interval were preserved.");
                    return;
                case FirefoxSessionContinuityStatus.SelectedWrapperRetainedBecauseCandidateIsIneligible:
                    _log.Write(
                        "FIREFOX GSMTC WRAPPER REFRESH NOT USED: the selected Firefox session remains " +
                        $"readable and Playing, but the newly enumerated wrapper is " +
                        $"{DescribeSessionIneligibility(candidateDetails)}. Current binding, pending, " +
                        "prepared, and active work plus the current interval were preserved.");
                    return;
                case FirefoxSessionContinuityStatus.RefreshedWrapperSameLogicalSession:
                    RefreshSelectedFirefoxWrapper(candidate, candidateDetails);
                    return;
                default:
                    throw new InvalidOperationException(
                        "A preserving Firefox continuity decision was not recognized.");
            }
        }

        string reacquisitionReason;
        if (!selectedEvidence.IsEligible)
        {
            reacquisitionReason =
                $"the selected Firefox session became {DescribeSessionIneligibility(selectedDetails)}";
            _log.Write(
                $"FIREFOX GSMTC SELECTED SESSION INVALID: {reacquisitionReason}. " +
                "Protected cancellation must finish before validated reacquisition.");
        }
        else
        {
            reacquisitionReason =
                "the sole eligible Firefox candidate no longer matched the selected title/artist";
            _log.Write(
                "FIREFOX GSMTC LOGICAL CONTINUITY NOT ESTABLISHED: the selected and newly enumerated " +
                "Firefox wrappers are both eligible, but their normalized title/artist metadata differ. " +
                "Treating this as an actual target change and using protected reacquisition.");
        }

        await TryReacquireFirefoxSessionAsync(reacquisitionReason);
    }

    private void RefreshSelectedFirefoxWrapper(
        GlobalSystemMediaTransportControlsSession refreshedWrapper,
        RadioSessionDetails refreshedDetails)
    {
        GlobalSystemMediaTransportControlsSession previous = _selectedSession!;

        try
        {
            refreshedWrapper.MediaPropertiesChanged += OnMediaPropertiesChanged;
        }
        catch (Exception exception)
        {
            _log.Warning(
                "Could not subscribe to the refreshed wrapper; the still-valid selected Firefox " +
                "wrapper and all pending/active work were retained",
                exception);
            return;
        }

        _selectedSession = refreshedWrapper;

        try
        {
            previous.MediaPropertiesChanged -= OnMediaPropertiesChanged;
        }
        catch (Exception exception)
        {
            _log.Warning("Could not remove the superseded Firefox wrapper observer", exception);
        }

        _log.Write(
            "FIREFOX GSMTC WRAPPER REFRESHED: SAME LOGICAL FIREFOX SESSION. Exactly one eligible " +
            "firefox.exe candidate remained Playing with the same normalized title/artist. " +
            "Observer binding moved without cancellation or interval reset.");
        _log.Write(
            $"Continuous track: {MediaSnapshot.Display(refreshedDetails.Media.Title)} - " +
            $"{MediaSnapshot.Display(refreshedDetails.Media.Artist)}");
    }

    private async Task TryReacquireFirefoxSessionAsync(string reason)
    {
        _log.Write(
            $"FIREFOX GSMTC REACQUISITION REQUIRED: {reason}. " +
            "Cancelling and discarding pending/active material before rebinding.");
        await FirefoxSessionRebindSafety.DiscardPendingBeforeRebindingAsync(
            () => InvalidatePendingForSessionSafety(reason),
            WaitForPipelineAsync);
        await WaitForLinerPreparationAsync();
        InvalidateLinerInventory();

        if (!_sessionAccess.TryReadExactFirefoxMatches(
                out IReadOnlyList<GlobalSystemMediaTransportControlsSession> refreshedMatches))
        {
            _log.Write("FIREFOX GSMTC REACQUISITION REFUSED: the refreshed session list was unavailable.");
            PublishTargetStatus(FirefoxTargetState.Unavailable, "Firefox session list unavailable during recovery");
            return;
        }

        if (refreshedMatches.Count == 0)
        {
            _log.Write("FIREFOX GSMTC REACQUISITION REFUSED: ZERO Firefox candidates remain.");
            PublishTargetStatus(FirefoxTargetState.Missing, "Firefox media session is missing");
            return;
        }

        if (refreshedMatches.Count > 1)
        {
            _log.Write(
                $"FIREFOX GSMTC REACQUISITION REFUSED: MULTIPLE/AMBIGUOUS candidates " +
                $"({refreshedMatches.Count}) remain.");
            PublishTargetStatus(FirefoxTargetState.Ambiguous, "Firefox media-session targeting is ambiguous");
            return;
        }

        GlobalSystemMediaTransportControlsSession refreshedCandidate = refreshedMatches[0];
        RadioSessionDetails replacement = await _sessionAccess.ReadSessionDetailsAsync(refreshedCandidate);
        FirefoxSessionContinuityEvidence replacementEvidence =
            FirefoxSessionContinuityEvidence.FromDetails(replacement);
        if (!replacementEvidence.IsEligible)
        {
            _log.Write(
                $"FIREFOX GSMTC REACQUISITION REFUSED: the sole candidate is " +
                $"{DescribeSessionIneligibility(replacement)}.");
            PublishTargetStatus(FirefoxTargetState.Unavailable, "The Firefox candidate is not safe to use");
            return;
        }

        GlobalSystemMediaTransportControlsSession previous = _selectedSession!;

        if (!ReferenceEquals(previous, refreshedCandidate))
        {
            try
            {
                refreshedCandidate.MediaPropertiesChanged += OnMediaPropertiesChanged;
            }
            catch (Exception exception)
            {
                _log.Warning("Could not subscribe to the replacement Firefox media session", exception);
                return;
            }

            _selectedSession = refreshedCandidate;

            try
            {
                previous.MediaPropertiesChanged -= OnMediaPropertiesChanged;
            }
            catch (Exception exception)
            {
                _log.Warning("Could not remove the invalid Firefox media-property observer", exception);
            }
        }

        lock (_stateLock)
        {
            _state.RebindCurrentTrack(replacement.Media, _clock.Now);
        }

        RearmLinerInventory();

        _log.Write(
            "FIREFOX GSMTC SESSION REACQUIRED: PASS - exactly one validated, eligible, Playing " +
            "firefox.exe session is selected.");
        _log.Write(
            $"Reacquired track: {MediaSnapshot.Display(replacement.Media.Title)} - " +
            $"{MediaSnapshot.Display(replacement.Media.Artist)}");
        _log.Write("Pending material was discarded. A fresh transition interval begins now.");
        WriteNextSchedule();
        PublishTargetStatus(
            FirefoxTargetState.Safe,
            "Firefox session safely reacquired; fresh interval started",
            CreateObservingActivity());
    }

    private static string DescribeSessionIneligibility(RadioSessionDetails details)
    {
        if (!details.SourceReadSucceeded)
        {
            return "unreadable at its source identifier";
        }

        if (!string.Equals(
                details.SourceId,
                FirefoxSessionSelector.VerifiedSourceId,
                StringComparison.OrdinalIgnoreCase))
        {
            return $"the wrong source ({MediaSnapshot.Display(details.SourceId)})";
        }

        if (!details.MediaReadSucceeded)
        {
            return "unreadable at its media properties";
        }

        if (!AnnouncementText.TryBuild(details.Media, out _))
        {
            return "missing a usable title or artist";
        }

        if (!details.PlaybackReadSucceeded)
        {
            return "unreadable at its playback state";
        }

        if (!PlaybackControlRules.CanStart(details.PlaybackStatus))
        {
            return $"{RadioSessionAccess.DisplayStatus(details.PlaybackStatus)}, not Playing";
        }

        return "eligible";
    }

    internal void InvalidatePendingForSessionSafety(string reason)
    {
        CancelLinerPreparation("Firefox session safety changed");
        bool invalidated;

        _cancellationGatesForTesting?.StateContenderBeforeLock?.Invoke();
        lock (_stateLock)
        {
            _cancellationGatesForTesting?.StateLockAcquired?.Invoke();
            invalidated = _state.CancelPendingOpportunityAndReschedule();
        }

        CancelPendingPipeline(reason);

        if (invalidated)
        {
            _log.Write("PENDING OR ACTIVE MATERIAL DISCARDED FOR FIREFOX SESSION SAFETY.");
            WriteNextSchedule();
        }
    }

    private void StartPreparation(RadioPreparationOpportunity opportunity)
    {
        CancelLinerPreparation("an expiring comment preparation has priority");
        bool capacityAvailable;
        lock (_pipelineLock)
        {
            capacityAvailable = (_pipelineTask is null || _pipelineTask.IsCompleted)
                && _preparedOpportunity is null;
        }

        EditorialDecision editorialDecision;
        lock (_stateLock)
        {
            editorialDecision = _state.EvaluatePreparation(opportunity, capacityAvailable);
        }

        WriteEditorialDecision(editorialDecision);
        if (editorialDecision.Outcome != EditorialDecisionOutcome.Prepare)
        {
            lock (_stateLock)
            {
                _state.ResolveOpportunity(opportunity.Id, false, null, _clock.Now);
            }

            _log.Write("PREPARATION SKIPPED: the Editorial Arbiter selected direct music continuation.");
            _log.Write("No missed comment was queued.");
            WriteNextSchedule();
            ReportActivity(new RadioActivitySnapshot(RadioActivityState.OpportunityAbandoned));
            return;
        }

        var lifetime = new OperationLifetime(_shutdownToken);
        bool skipped;

        lock (_pipelineLock)
        {
            skipped = (_pipelineTask is not null && !_pipelineTask.IsCompleted)
                || _preparedOpportunity is not null;

            if (!skipped)
            {
                _pendingLifetime = lifetime;
                _pipelinePhase = PipelineOperationPhase.Preparing;
                _pipelineTask = Task.Run(
                    () => RunPreparationAndStoreAsync(opportunity, lifetime, lifetime.Token));
            }
        }

        if (!skipped)
        {
            return;
        }

        lifetime.RequestTerminalDisposal();
        lock (_stateLock)
        {
            _state.ResolveOpportunity(opportunity.Id, false, null, _clock.Now);
        }

        _log.Write("PREPARATION SKIPPED: another automatic pipeline already exists.");
        _log.Write("No missed comment was queued.");
        WriteNextSchedule();
        ReportActivity(new RadioActivitySnapshot(RadioActivityState.OpportunityAbandoned));
    }

    private async Task RunPreparationAndStoreAsync(
        RadioPreparationOpportunity opportunity,
        OperationLifetime lifetime,
        CancellationToken cancellationToken)
    {
        bool retainedForDelivery = false;

        try
        {
            TransitionPreparationResult result = await _pipeline.PrepareAsync(
                opportunity,
                _aiEnabled,
                cancellationToken);

            if (result.Status == TransitionPreparationStatus.Ready && result.Prepared is not null)
            {
                retainedForDelivery = TryMarkAndPublishPreparedOpportunity(
                    opportunity.Id,
                    lifetime,
                    cancellationToken,
                    result.Prepared);

                if (!retainedForDelivery)
                {
                    result.Prepared.Dispose();
                }
            }

            if (retainedForDelivery && result.Prepared is not null)
            {
                _m9bCommentProvider?.RecordPrepared(result.Prepared.Opportunity, result.Prepared.Selection);
            }
            else
            {
                _m9bCommentProvider?.Discard(opportunity, result.Reason);
            }

            _log.Write(
                $"AUTOMATIC PREPARATION RESULT: {result.Status.ToString().ToUpperInvariant()} - {result.Reason}.");

            if (!retainedForDelivery)
            {
                bool resolved;
                lock (_stateLock)
                {
                    resolved = _state.ResolveOpportunity(opportunity.Id, false, null, _clock.Now);
                }

                if (resolved)
                {
                    WriteNextSchedule();
                    ReportActivity(new RadioActivitySnapshot(RadioActivityState.OpportunityAbandoned));
                }
            }
        }
        catch (Exception exception)
        {
            _log.Warning("Unexpected transition-preparation cleanup failure", exception);
            ReportActivity(new RadioActivitySnapshot(RadioActivityState.OpportunityAbandoned));
            lock (_stateLock)
            {
                _state.ResolveOpportunity(opportunity.Id, false, null, _clock.Now);
            }
        }
        finally
        {
            FinalizePreparationLifetime(lifetime);
        }
    }

    private void StartDelivery(RadioTransitionDelivery transition)
    {
        long transitionSequence;
        PreparedMaterialSessionId sessionId;
        bool silentMode;
        lock (_stateLock)
        {
            transitionSequence = _state.TotalObservedTransitions;
            sessionId = _state.CurrentSessionId;
            silentMode = _state.SilentMode;
        }

        var linerTransition = new ReusableLinerTransition(
            transitionSequence,
            transition.NewTrack,
            transition.TransitionObservedAt,
            sessionId);
        EditorialAirtimeCandidate linerCandidate;
        lock (_linerLock)
        {
            linerCandidate = _linerInventory.CreateCandidate(
                linerTransition,
                silentMode,
                protectedPreparationBoundaryClear: false,
                missedCommentBoundaryClear: false,
                treatmentAvailable: true);
        }

        M11BTransitionSelection selection;
        lock (_stateLock)
        {
            selection = _state.TryReserveTransition(transition, linerCandidate, _clock.Now);
        }

        M10CDeliveryReservation? reservation = selection.CommentReservation;
        EditorialDecision editorialDecision = selection.Decision;

        if (editorialDecision is not null)
        {
            WriteEditorialDecision(editorialDecision);
        }

        if (reservation is null)
        {
            _log.Write("TRANSITION DELIVERY ABANDONED: prepared material was no longer safely reservable. Silence is correct.");
            CancelPendingPipeline("prepared material could not be reserved for delivery");
            lock (_stateLock)
            {
                _state.ResolveOpportunity(transition.OpportunityId, false, null, _clock.Now);
            }

            WriteNextSchedule();
            ReportActivity(new RadioActivitySnapshot(RadioActivityState.OpportunityAbandoned));
            return;
        }

        if (TryTransferPreparedOpportunity(
                transition.OpportunityId,
                (prepared, lifetime, simulateFailure) =>
                {
                    _pipelineTask = Task.Run(
                        () => RunDeliveryAndFinalizeAsync(
                            prepared,
                            transition,
                            lifetime,
                            lifetime.Token,
                            simulateFailure,
                            reservation));
                }))
        {
            return;
        }

        _log.Write(
            "TRANSITION DELIVERY ABANDONED: Track-A speech was not fully ready when Track B arrived. Silence is correct.");
        CancelPendingPipeline("preparation did not finish before the delivery transition");
        lock (_stateLock)
        {
            _state.ResolveOpportunity(transition.OpportunityId, false, null, _clock.Now, reservation);
        }

        WriteNextSchedule();
        ReportActivity(new RadioActivitySnapshot(RadioActivityState.OpportunityAbandoned));
    }
    private async Task RunDeliveryAndFinalizeAsync(
        PreparedTransitionOpportunity prepared,
        RadioTransitionDelivery transition,
        OperationLifetime lifetime,
        CancellationToken cancellationToken,
        bool simulateFailure,
        M10CDeliveryReservation reservation)
    {
        try
        {
            TransitionDeliveryResult result = await _pipeline.DeliverAsync(
                prepared,
                transition,
                simulateFailure,
                cancellationToken);

            string? acceptedAiComment = result.Status == TransitionDeliveryStatus.Completed
                && (prepared.Selection.Source == CommentTextSource.Ai
                    || prepared.Selection.Source == CommentTextSource.GroundedAi)
                    ? prepared.Selection.Text
                    : null;
            if (result.Status == TransitionDeliveryStatus.Completed)
            {
                _m9bCommentProvider?.RecordCompleted(prepared.Opportunity, prepared.Selection);
            }
            if (result.Status == TransitionDeliveryStatus.Completed)
            {
                long completedSpeechTransition;
                lock (_stateLock)
                {
                    completedSpeechTransition = _state.TotalObservedTransitions;
                }

                lock (_linerLock)
                {
                    _linerInventory.RecordHostSpeechCompleted(completedSpeechTransition);
                }
            }
            else
            {
                _m9bCommentProvider?.Discard(prepared.Opportunity, result.Reason);
            }

            bool resolved;

            lock (_stateLock)
            {
                resolved = _state.ResolveOpportunity(
                    transition.OpportunityId,
                    result.Status == TransitionDeliveryStatus.Completed,
                    acceptedAiComment,
                    _clock.Now,
                    reservation);
            }

            _log.Write(
                $"AUTOMATIC TRANSITION RESULT: {result.Status.ToString().ToUpperInvariant()} - {result.Reason}.");

            if (resolved)
            {
                WriteHistorySizes();
                WriteNextSchedule();
                ReportActivity(result.Status == TransitionDeliveryStatus.Completed
                    ? CreateObservingActivity()
                    : new RadioActivitySnapshot(RadioActivityState.OpportunityAbandoned));
            }
        }
        catch (Exception exception)
        {
            _log.Warning("Unexpected transition-delivery cleanup failure", exception);
            ReportActivity(new RadioActivitySnapshot(RadioActivityState.OpportunityAbandoned));
            lock (_stateLock)
            {
                _state.ResolveOpportunity(transition.OpportunityId, false, null, _clock.Now);
            }
        }
        finally
        {
            CompleteDeliveryOwnership(prepared, lifetime);
        }
    }

    private bool _aiEnabled;

    internal void CancelPendingPipeline(string reason)
    {
        PreparedTransitionOpportunity? prepared;
        OperationLifetime? terminalLifetime = null;
        OperationLifetime.CancellationLease? lease = null;
        _cancellationGatesForTesting?.ContenderBeforeLock?.Invoke();
        lock (_pipelineLock)
        {
            _cancellationGatesForTesting?.LockAcquired?.Invoke();
            OperationLifetime? lifetime = _pendingLifetime;
            prepared = _preparedOpportunity;
            lease = lifetime?.TryAcquireCancellationLease();
            if (lease is not null)
            {
                _cancellationGatesForTesting?.LeaseAcquired?.Invoke();
            }
            if (lifetime is not null && lease is not null && _pipelinePhase == PipelineOperationPhase.Ready)
            {
                _pendingLifetime = null;
                _preparedOpportunity = null;
                _pipelinePhase = PipelineOperationPhase.None;
                terminalLifetime = lifetime;
                _cancellationGatesForTesting?.StateTransitioned?.Invoke();
            }
            else
            {
                prepared = null;
            }
        }
        Exception? failure = null;
        if (lease is not null)
        {
            try { WriteCancellationLog($"Cancelling pending automatic preparation/delivery because {reason}."); }
            catch (Exception exception) { failure ??= exception; }
            try
            {
                _cancellationGatesForTesting?.BeforeCancellation?.Invoke();
                lease.Cancel();
            }
            catch (Exception exception) { failure ??= exception; }
        }
        try
        {
            if (prepared is not null)
            {
                try { DiscardPrepared(prepared, reason); } catch (Exception exception) { failure ??= exception; }
                try { prepared.Dispose(); } catch (Exception exception) { failure ??= exception; }
            }
        }
        finally
        {
            try { terminalLifetime?.RequestTerminalDisposal(); } catch (Exception exception) { failure ??= exception; }
            try { lease?.Dispose(); } catch (Exception exception) { failure ??= exception; }
        }
        if (failure is not null) throw failure;
    }

    internal void FinalizePreparationLifetime(OperationLifetime lifetime)
    {
        bool terminal = false;
        lock (_pipelineLock)
        {
            if (ReferenceEquals(_pendingLifetime, lifetime) && _pipelinePhase == PipelineOperationPhase.Preparing)
            {
                _pendingLifetime = null;
                _pipelineTask = null;
                _pipelinePhase = PipelineOperationPhase.None;
                terminal = true;
            }
            else if (!ReferenceEquals(_pendingLifetime, lifetime))
            {
                terminal = true;
            }
        }
        if (terminal) lifetime.RequestTerminalDisposal();
    }

    internal void FinalizeDeliveryLifetime(OperationLifetime lifetime)
    {
        lock (_pipelineLock)
        {
            if (ReferenceEquals(_pendingLifetime, lifetime) && _pipelinePhase == PipelineOperationPhase.Delivering)
            {
                _pendingLifetime = null;
                _pipelineTask = null;
                _pipelinePhase = PipelineOperationPhase.None;
            }
        }
        lifetime.RequestTerminalDisposal();
    }

    internal void CompleteDeliveryOwnership(
        PreparedTransitionOpportunity prepared,
        OperationLifetime lifetime)
    {
        try
        {
            prepared.Dispose();
        }
        finally
        {
            FinalizeDeliveryLifetime(lifetime);
        }
    }

    private async Task WaitForPipelineAsync()
    {
        Task? task;

        lock (_pipelineLock)
        {
            task = _pipelineTask;
        }

        if (task is not null)
        {
            await task;
        }
    }

    internal void DisposePreparedOpportunity()
    {
        PreparedTransitionOpportunity? prepared;
        OperationLifetime? lifetime;

        lock (_pipelineLock)
        {
            prepared = _preparedOpportunity;
            _preparedOpportunity = null;
            lifetime = _pendingLifetime;
            _pendingLifetime = null;
            _pipelineTask = null;
            _pipelinePhase = PipelineOperationPhase.None;
        }

        Exception? failure = null;
        if (prepared is not null)
        {
            try
            {
                DiscardPrepared(prepared, "the radio stopped before delivery");
            }
            catch (Exception exception)
            {
                failure ??= exception;
            }

            try
            {
                prepared.Dispose();
            }
            catch (Exception exception)
            {
                failure ??= exception;
            }
        }

        try
        {
            lifetime?.RequestTerminalDisposal();
        }
        catch (Exception exception)
        {
            failure ??= exception;
        }

        if (failure is not null)
        {
            throw failure;
        }
    }

    internal bool TryMarkAndPublishPreparedOpportunity(
        long opportunityId,
        OperationLifetime lifetime,
        CancellationToken cancellationToken,
        PreparedTransitionOpportunity prepared)
    {
        lock (_stateLock)
        {
            return _state.MarkPreparationReady(opportunityId, prepared)
                && TryPublishPreparedOpportunity(lifetime, cancellationToken, prepared);
        }
    }

    internal bool TryPublishPreparedOpportunity(
        OperationLifetime lifetime,
        CancellationToken cancellationToken,
        PreparedTransitionOpportunity prepared)
    {
        _cancellationGatesForTesting?.ContenderBeforeLock?.Invoke();
        lock (_pipelineLock)
        {
            _cancellationGatesForTesting?.LockAcquired?.Invoke();
            if (!ReferenceEquals(_pendingLifetime, lifetime)
                || _pipelinePhase != PipelineOperationPhase.Preparing
                || cancellationToken.IsCancellationRequested)
            {
                return false;
            }

            _preparedOpportunity = prepared;
            _pipelinePhase = PipelineOperationPhase.Ready;
            _cancellationGatesForTesting?.StateTransitioned?.Invoke();
            return true;
        }
    }

    internal bool TryTransferPreparedOpportunity(
        long opportunityId,
        Action<PreparedTransitionOpportunity, OperationLifetime, bool> beginDelivery)
    {
        _cancellationGatesForTesting?.ContenderBeforeLock?.Invoke();
        lock (_pipelineLock)
        {
            _cancellationGatesForTesting?.LockAcquired?.Invoke();
            PreparedTransitionOpportunity? prepared = _preparedOpportunity;
            OperationLifetime? lifetime = _pendingLifetime;
            if (prepared is null
                || prepared.Opportunity.Id != opportunityId
                || lifetime is null
                || _pipelinePhase != PipelineOperationPhase.Ready
                || lifetime.IsCancellationRequested)
            {
                return false;
            }

            _preparedOpportunity = null;
            _pipelinePhase = PipelineOperationPhase.Delivering;
            bool simulateFailure = _simulateNextTransitionFailure;
            _simulateNextTransitionFailure = false;
            _cancellationGatesForTesting?.StateTransitioned?.Invoke();
            beginDelivery(prepared, lifetime, simulateFailure);
            return true;
        }
    }

    private void WriteCancellationLog(string message)
    {
        if (_cancellationLogForTesting is not null) _cancellationLogForTesting(message);
        else _log.Write(message);
    }

    private void DiscardPrepared(PreparedTransitionOpportunity prepared, string reason)
    {
        if (_discardPreparedForTesting is not null) _discardPreparedForTesting(prepared, reason);
        else _m9bCommentProvider?.Discard(prepared.Opportunity, reason);
    }

    internal void ConfigureCancellationTesting(
        CancellationCoordinatorTestGates? gates = null,
        Action<string>? writeLog = null,
        Action<PreparedTransitionOpportunity, string>? discardPrepared = null)
    {
        _cancellationGatesForTesting = gates;
        _cancellationLogForTesting = writeLog;
        _discardPreparedForTesting = discardPrepared;
    }

    internal void InstallPipelineOperationForTesting(
        OperationLifetime lifetime,
        PipelineOperationPhase phase,
        PreparedTransitionOpportunity? prepared = null,
        Task? task = null)
    {
        lock (_pipelineLock)
        {
            _pendingLifetime = lifetime;
            _pipelinePhase = phase;
            _preparedOpportunity = prepared;
            _pipelineTask = task;
        }
    }

    internal RadioPreparationOpportunity ArmAndCreatePreparationForTesting(
        MediaSnapshot initialTrack,
        MediaSnapshot subjectTrack,
        DateTimeOffset observedAt)
    {
        lock (_stateLock)
        {
            _state.Arm(initialTrack, observedAt);
            return _state.ObserveTrack(subjectTrack, observedAt.AddSeconds(1)).PreparationOpportunity
                ?? throw new InvalidOperationException("The test state did not create a preparation opportunity.");
        }
    }

    internal (OperationLifetime? Lifetime, PipelineOperationPhase Phase, PreparedTransitionOpportunity? Prepared, Task? Task)
        PipelineOperationForTesting()
    {
        lock (_pipelineLock)
        {
            return (_pendingLifetime, _pipelinePhase, _preparedOpportunity, _pipelineTask);
        }
    }

    internal void RunWithPipelineLockForTesting(Action action)
    {
        lock (_pipelineLock) action();
    }

    private bool TrySubscribe()
    {
        if (_selectedSession is null)
        {
            return false;
        }

        try
        {
            _selectedSession.MediaPropertiesChanged += OnMediaPropertiesChanged;
            _mediaSubscribed = true;
            _manager.SessionsChanged += OnSessionsChanged;
            _managerSubscribed = true;
            return true;
        }
        catch (Exception exception)
        {
            _log.Warning("Could not subscribe to the required automatic radio observers", exception);
            Unsubscribe();
            return false;
        }
    }

    private void Unsubscribe()
    {
        if (_mediaSubscribed && _selectedSession is not null)
        {
            try
            {
                _selectedSession.MediaPropertiesChanged -= OnMediaPropertiesChanged;
            }
            catch (Exception exception)
            {
                _log.Warning("Could not remove the automatic media-property observer", exception);
            }

            _mediaSubscribed = false;
        }

        if (_managerSubscribed)
        {
            try
            {
                _manager.SessionsChanged -= OnSessionsChanged;
            }
            catch (Exception exception)
            {
                _log.Warning("Could not remove the automatic session-list observer", exception);
            }

            _managerSubscribed = false;
        }
    }

    private RadioRuntimeState GetRuntimeState()
    {
        lock (_stateLock)
        {
            return _state.GetRuntimeState();
        }
    }

    private void WriteConfiguration(bool aiEnabled)
    {
        _aiEnabled = aiEnabled;
        _log.Write("RADIO BEHAVIOR SETTINGS");
        _log.Write(
            $"  Transitions between comments: {_radioConfiguration.MinimumTransitionsBetweenComments}-" +
            $"{_radioConfiguration.MaximumTransitionsBetweenComments} (random and inclusive)");
        _log.Write(
            $"  Minimum Track-A age before preparation: {_radioConfiguration.MinimumTrackAgeBeforeSpeech.TotalSeconds:0} seconds");
        _log.Write(
            $"  Transition delivery window: {_radioConfiguration.TransitionDeliveryWindow.TotalSeconds:0.0} seconds after Track B is observed");
        _log.Write($"  Maximum automatic speech duration: {_radioConfiguration.MaximumSpeechDuration.TotalSeconds:0} seconds");
        _log.Write($"  Recent in-memory tracks: {_radioConfiguration.RecentTrackLimit}");
        _log.Write($"  Recent accepted AI comments: {_radioConfiguration.RecentCommentLimit}");
        _log.Write($"  AI mode: {(aiEnabled ? "local Ollama when scheduled" : "OFF - fixed fallback only")}");
        _log.Write("  Delivery: Firefox Core Audio ducking at 25% of the captured current scalar; no GSMTC pause/resume");
        _log.Write(
            $"  Simulated post-duck failure: {(_simulateNextTransitionFailure ? "ON for the first delivery" : "OFF")}");
        _log.Write("Recent behavioral history is memory-only and is discarded when this process exits.");
    }

    private void WriteTrackTransition(RadioTrackObservation observation)
    {
        _log.Write("TRACK TRANSITION OBSERVED");
        _log.Write($"  Previous title: {MediaSnapshot.Display(observation.PreviousTrack?.Title)}");
        _log.Write($"  Previous artist: {MediaSnapshot.Display(observation.PreviousTrack?.Artist)}");
        _log.Write($"  Current title: {MediaSnapshot.Display(observation.CurrentTrack?.Title)}");
        _log.Write($"  Current artist: {MediaSnapshot.Display(observation.CurrentTrack?.Artist)}");
        _log.Write($"  Total transitions during this run: {observation.TotalObservedTransitions}");

        lock (_stateLock)
        {
            if (_state.SilentMode)
            {
                _log.Write("  Silent Mode is ON: this transition is not counted toward a comment.");
            }
            else
            {
                _log.Write(
                    $"  Scheduled transition counter: {observation.TransitionCounter} of {observation.ScheduledThreshold}");
            }
        }

        foreach (string trustedObservation in observation.TrustedListeningObservations)
        {
            _log.Write($"  TRUSTED LISTENING OBSERVATION: {trustedObservation}");
        }

        WriteHistorySizes();

        if (observation.PreparationOpportunity is not null)
        {
            _log.Write("OPPORTUNITY SCHEDULED: one transition remains before delivery.");
            _log.Write("Track A preparation will begin when its stability age is satisfied, while music keeps Playing.");
        }

        if (observation.Delivery is not null)
        {
            _log.Write("DELIVERY TRANSITION REACHED: prepared Track-A material may be delivered over Track B now.");
        }
    }

    private void WriteHistorySizes()
    {
        lock (_stateLock)
        {
            _log.Write(
                $"Bounded in-memory history: {_state.RecentTracks.Count}/{_radioConfiguration.RecentTrackLimit} track(s), " +
                $"{_state.RecentAcceptedAiComments.Count}/{_radioConfiguration.RecentCommentLimit} accepted AI comment(s).");
        }
    }

    private void WriteNextSchedule()
    {
        lock (_stateLock)
        {
            _log.Write(
                $"Next comment target: delivery on transition {_state.ScheduledThreshold}; " +
                $"preparation starts one transition earlier.");
        }
    }

    private void WriteEditorialDecision(EditorialDecision decision)
    {
        string selected = decision.SelectedCandidateId ?? "direct-music-continuation";
        string reasons = string.Join(", ", decision.Trace.Select(item =>
            item.CandidateId is null ? item.Reason.ToString() : $"{item.Reason}:{item.CandidateId}"));
        _log.Write(
            $"EDITORIAL {decision.Stage.ToString().ToUpperInvariant()}: " +
            $"{decision.Outcome} selected {selected}; reasons [{reasons}].");
    }

    private sealed class QuickReactionTransitionCommentProvider : ITransitionCommentProvider
    {
        private readonly AiCommentService _service;
        public QuickReactionTransitionCommentProvider(AiCommentService service) => _service = service;
        public Task<CommentSelection> SelectAsync(RadioPreparationContext context, bool aiEnabled, CancellationToken cancellationToken) => _service.SelectQuickReactionAsync(context, aiEnabled, cancellationToken);
    }

    private sealed class LocalTransitionCommentProvider : ITransitionCommentProvider
    {
        private readonly AiCommentService _service;

        public LocalTransitionCommentProvider(AiCommentService service)
        {
            _service = service;
        }

        public Task<CommentSelection> SelectAsync(
            RadioPreparationContext context,
            bool aiEnabled,
            CancellationToken cancellationToken)
        {
            return _service.SelectPreparedTransitionCommentAsync(context, aiEnabled, cancellationToken);
        }
    }
}
