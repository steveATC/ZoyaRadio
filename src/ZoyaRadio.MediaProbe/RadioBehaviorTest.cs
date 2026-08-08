using Windows.Media.Control;

namespace ZoyaRadio.MediaProbe;

internal sealed class RadioBehaviorTest
{
    private readonly GlobalSystemMediaTransportControlsSessionManager _manager;
    private readonly LocalLog _log;
    private readonly RadioBehaviorConfiguration _radioConfiguration;
    private readonly IRadioClock _clock;
    private readonly RadioBehaviorState _state;
    private readonly RadioSessionAccess _sessionAccess;
    private readonly TransitionRadioPipeline _pipeline;
    private readonly object _stateLock = new();
    private readonly object _pipelineLock = new();
    private readonly SemaphoreSlim _mediaEventGate = new(1, 1);
    private GlobalSystemMediaTransportControlsSession? _selectedSession;
    private CancellationToken _shutdownToken;
    private CancellationTokenSource? _pendingCancellation;
    private Task? _pipelineTask;
    private PreparedTransitionOpportunity? _preparedOpportunity;
    private bool _simulateNextTransitionFailure;
    private bool _mediaSubscribed;
    private bool _managerSubscribed;

    public RadioBehaviorTest(
        GlobalSystemMediaTransportControlsSessionManager manager,
        LocalLog log,
        IAnnouncementSpeechService speechService,
        ILocalCommentGenerator generator,
        AiCommentConfiguration aiConfiguration,
        HostPersonaConfiguration persona,
        RadioBehaviorConfiguration? radioConfiguration = null,
        ITransitionTargetSource? targetSource = null,
        IRadioClock? clock = null)
    {
        _manager = manager;
        _log = log;
        _radioConfiguration = radioConfiguration ?? RadioBehaviorConfiguration.Default;
        _radioConfiguration.Validate();
        _clock = clock ?? new SystemRadioClock();
        _state = new RadioBehaviorState(
            _radioConfiguration,
            targetSource ?? new RandomTransitionTargetSource());
        _sessionAccess = new RadioSessionAccess(manager, log);

        var aiService = new AiCommentService(generator, aiConfiguration, log, persona);
        var commentProvider = new LocalTransitionCommentProvider(aiService);
        var transitionDucker = new WindowsTransitionAudioDucker(log);
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
            log);
    }

    public async Task<int> RunAsync(
        bool aiEnabled,
        bool simulateTransitionFailure,
        CancellationToken cancellationToken)
    {
        _shutdownToken = cancellationToken;
        _simulateNextTransitionFailure = simulateTransitionFailure;
        WriteConfiguration(aiEnabled);

        RadioSessionDetails[] sessions;

        try
        {
            sessions = await _sessionAccess.ReadAllSessionsAsync();
        }
        catch (Exception exception)
        {
            _log.Warning("Could not enumerate Windows media sessions for automatic behavior", exception);
            _log.Write("RADIO BEHAVIOR RESULT: NOT ARMED - no AI, playback, or speech commands were issued.");
            return 1;
        }

        IReadOnlyList<RadioSessionDetails> matches = FirefoxSessionSelector.FindExactMatches(
            sessions,
            details => details.SourceId);

        if (matches.Count == 0)
        {
            _log.Write("NO FIREFOX TARGET FOUND: automatic behavior was not armed.");
            _log.Write("Start YouTube Music playing in Firefox, then run this test again.");
            return 1;
        }

        if (matches.Count > 1)
        {
            _log.Write("!!! AMBIGUOUS TARGET !!!");
            _log.Write($"Found {matches.Count} sessions matching firefox.exe. Automatic behavior was not armed.");
            return 1;
        }

        RadioSessionDetails initial = matches[0];
        _sessionAccess.WriteSessionDetails("AUTOMATIC RADIO TARGET", initial);

        if (!initial.MediaReadSucceeded || !AnnouncementText.TryBuild(initial.Media, out _))
        {
            _log.Write("METADATA SAFETY: Firefox did not provide both a usable title and artist.");
            _log.Write("Automatic behavior was not armed and no playback or speech commands were issued.");
            return 1;
        }

        if (!initial.PlaybackReadSucceeded || !PlaybackControlRules.CanStart(initial.PlaybackStatus))
        {
            _log.Write(
                $"STARTING-STATE SAFETY: Firefox is {RadioSessionAccess.DisplayStatus(initial.PlaybackStatus)}, not Playing.");
            _log.Write("Automatic behavior was not armed. Zoya will not start paused media.");
            return 1;
        }

        _log.Write(
            "Enable automatic transition delivery? This may duck Firefox and speak, but will not pause playback. [y/N]");
        string? response;

        try
        {
            response = await Console.In.ReadLineAsync(cancellationToken);
        }
        catch (OperationCanceledException)
        {
            _log.Write("AUTOMATIC RADIO CANCELLED before arming. No AI, playback, or speech commands were issued.");
            return 0;
        }

        if (!PlaybackControlRules.IsAffirmative(response))
        {
            _log.Write("User confirmation: NO");
            _log.Write("AUTOMATIC RADIO NOT ARMED: no AI, playback, or speech commands were issued.");
            return 0;
        }

        _log.Write("User confirmation: YES");
        _selectedSession = initial.Session;

        RadioSessionDetails? rechecked = await _sessionAccess.RecheckStartupTargetAsync(initial);
        if (rechecked is null)
        {
            _log.Write("STARTUP REVALIDATION FAILED: session, track, or playback changed while awaiting confirmation.");
            _log.Write("Automatic behavior was not armed. Run the test again on the current track.");
            return 1;
        }

        await _mediaEventGate.WaitAsync();
        try
        {
            if (!TrySubscribe())
            {
                _log.Write("RADIO BEHAVIOR RESULT: NOT ARMED - required Windows change observers were unavailable.");
                return 1;
            }

            lock (_stateLock)
            {
                _state.Arm(rechecked.Media, _clock.Now);
            }
        }
        finally
        {
            _mediaEventGate.Release();
        }

        _log.Write("AUTOMATIC RADIO: ARMED");
        _log.Write($"Initial track: {rechecked.Media.Title} - {rechecked.Media.Artist}");
        _log.Write("The initial track establishes state and does not count as a transition.");
        WriteNextSchedule();
        _log.Write("Console controls: S = toggle Silent Mode, C = clear recent history, Ctrl+C = stop safely.");

        try
        {
            await MonitorConsoleAsync(cancellationToken);
        }
        catch (OperationCanceledException)
        {
            _log.Write("Shutdown requested. Cancelling prepared delivery and preserving exact Firefox volume restoration.");
        }
        finally
        {
            lock (_stateLock)
            {
                _state.Disarm();
            }

            CancelPendingPipeline("automatic behavior is shutting down");
            Unsubscribe();
            await WaitForPipelineAsync();
            DisposePreparedOpportunity();
            await _mediaEventGate.WaitAsync();
            _mediaEventGate.Release();
        }

        _log.Write("AUTOMATIC RADIO: STOPPED CLEANLY");
        return 0;
    }

    private async Task MonitorConsoleAsync(CancellationToken cancellationToken)
    {
        bool consoleKeysAvailable = true;

        while (true)
        {
            cancellationToken.ThrowIfCancellationRequested();

            if (consoleKeysAvailable)
            {
                try
                {
                    while (Console.KeyAvailable)
                    {
                        ConsoleKeyInfo key = Console.ReadKey(intercept: true);
                        if (key.Key == ConsoleKey.S)
                        {
                            ToggleSilentMode();
                        }
                        else if (key.Key == ConsoleKey.C)
                        {
                            ClearRecentHistory();
                        }
                    }
                }
                catch (InvalidOperationException exception)
                {
                    consoleKeysAvailable = false;
                    _log.Warning("S/C console controls are unavailable in this redirected console", exception);
                    _log.Write("Ctrl+C shutdown remains available.");
                }
            }

            await Task.Delay(100, cancellationToken);
        }
    }

    private void ToggleSilentMode()
    {
        RadioModeChange change;

        lock (_stateLock)
        {
            change = _state.SetSilentMode(!_state.SilentMode);
        }

        if (change.PendingOpportunityInvalidated)
        {
            CancelPendingPipeline("Silent Mode was enabled");
        }

        _log.Write($"SILENT MODE: {(change.SilentMode ? "ON" : "OFF")}");

        if (change.SilentMode)
        {
            _log.Write("No new AI generation, speech preparation, automatic duck, or speech will begin.");
        }
        else
        {
            _log.Write("Missed comments were discarded. A fresh transition interval begins now.");
            WriteNextSchedule();
        }
    }

    private void ClearRecentHistory()
    {
        bool invalidated;

        lock (_stateLock)
        {
            invalidated = _state.ClearRecentHistory();
        }

        if (invalidated)
        {
            CancelPendingPipeline("recent history was cleared");
        }

        _log.Write("RECENT HISTORY CLEARED: recent tracks and accepted AI comments were removed from memory.");
        _log.Write("The current diagnostic log was not deleted or rewritten.");
        _log.Write(
            invalidated
                ? "A pending comment was cancelled; if Firefox was already ducked, protected exact-volume restoration still runs."
                : "No volume or speech command was issued by the clear-history command.");
        WriteHistorySizes();
        WriteNextSchedule();
    }

    private async void OnMediaPropertiesChanged(
        GlobalSystemMediaTransportControlsSession sender,
        MediaPropertiesChangedEventArgs args)
    {
        try
        {
            await HandleMediaPropertiesChangedAsync();
        }
        catch (Exception exception)
        {
            _log.Warning("Unexpected automatic track-change observer failure", exception);
        }
    }

    private async Task HandleMediaPropertiesChangedAsync()
    {
        await _mediaEventGate.WaitAsync();
        RadioPreparationOpportunity? preparation = null;
        RadioTransitionDelivery? delivery = null;

        try
        {
            if (_selectedSession is null)
            {
                return;
            }

            MediaSnapshot? current = await _sessionAccess.TryReadMediaAsync(_selectedSession);
            if (current is null)
            {
                return;
            }

            RadioTrackObservation observation;

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
    }

    private void OnSessionsChanged(
        GlobalSystemMediaTransportControlsSessionManager sender,
        SessionsChangedEventArgs args)
    {
        try
        {
            bool sessionListReadSucceeded = _sessionAccess.TryReadExactFirefoxMatches(
                out IReadOnlyList<GlobalSystemMediaTransportControlsSession> matches);
            bool stillExactTarget = matches.Count == 1
                && _selectedSession is not null
                && ReferenceEquals(matches[0], _selectedSession);

            if (sessionListReadSucceeded && stillExactTarget)
            {
                return;
            }

            bool cancelled;
            lock (_stateLock)
            {
                cancelled = _state.CancelPendingOpportunityAndReschedule();
            }

            _log.Write(
                sessionListReadSucceeded
                    ? $"SESSION SAFETY: expected one original firefox.exe session; Windows now reports {matches.Count} exact match(es)."
                    : "SESSION SAFETY: the Windows media-session list could not be validated.");
            _log.Write("No automatic control will target a replacement or ambiguous session. Restart the test after fixing sessions.");

            if (cancelled)
            {
                CancelPendingPipeline("the selected Firefox session became missing or ambiguous");
                WriteNextSchedule();
            }
        }
        catch (Exception exception)
        {
            _log.Warning("Could not validate a Windows session-list change", exception);
            bool cancelled;
            lock (_stateLock)
            {
                cancelled = _state.CancelPendingOpportunityAndReschedule();
            }

            if (cancelled)
            {
                CancelPendingPipeline("the Windows media-session list could not be validated");
            }
        }

    }

    private void StartPreparation(RadioPreparationOpportunity opportunity)
    {
        var linkedCancellation = CancellationTokenSource.CreateLinkedTokenSource(_shutdownToken);
        bool skipped;

        lock (_pipelineLock)
        {
            skipped = (_pipelineTask is not null && !_pipelineTask.IsCompleted)
                || _preparedOpportunity is not null;

            if (!skipped)
            {
                _pendingCancellation = linkedCancellation;
                _pipelineTask = Task.Run(
                    () => RunPreparationAndStoreAsync(opportunity, linkedCancellation));
            }
        }

        if (!skipped)
        {
            return;
        }

        linkedCancellation.Dispose();
        lock (_stateLock)
        {
            _state.ResolveOpportunity(opportunity.Id, false, null);
        }

        _log.Write("PREPARATION SKIPPED: another automatic pipeline already exists.");
        _log.Write("No missed comment was queued.");
        WriteNextSchedule();
    }

    private async Task RunPreparationAndStoreAsync(
        RadioPreparationOpportunity opportunity,
        CancellationTokenSource opportunityCancellation)
    {
        bool retainedForDelivery = false;

        try
        {
            TransitionPreparationResult result = await _pipeline.PrepareAsync(
                opportunity,
                _aiEnabled,
                opportunityCancellation.Token);

            if (result.Status == TransitionPreparationStatus.Ready && result.Prepared is not null)
            {
                lock (_stateLock)
                {
                    if (_state.MarkPreparationReady(opportunity.Id))
                    {
                        lock (_pipelineLock)
                        {
                            if (ReferenceEquals(_pendingCancellation, opportunityCancellation)
                                && !opportunityCancellation.IsCancellationRequested)
                            {
                                _preparedOpportunity = result.Prepared;
                                retainedForDelivery = true;
                            }
                        }
                    }
                }

                if (!retainedForDelivery)
                {
                    result.Prepared.Dispose();
                }
            }

            _log.Write(
                $"AUTOMATIC PREPARATION RESULT: {result.Status.ToString().ToUpperInvariant()} - {result.Reason}.");

            if (!retainedForDelivery)
            {
                bool resolved;
                lock (_stateLock)
                {
                    resolved = _state.ResolveOpportunity(opportunity.Id, false, null);
                }

                if (resolved)
                {
                    WriteNextSchedule();
                }
            }
        }
        catch (Exception exception)
        {
            _log.Warning("Unexpected transition-preparation cleanup failure", exception);
            lock (_stateLock)
            {
                _state.ResolveOpportunity(opportunity.Id, false, null);
            }
        }
        finally
        {
            if (!retainedForDelivery)
            {
                lock (_pipelineLock)
                {
                    if (ReferenceEquals(_pendingCancellation, opportunityCancellation))
                    {
                        _pendingCancellation = null;
                    }
                }

                opportunityCancellation.Dispose();
            }
            else if (opportunityCancellation.IsCancellationRequested)
            {
                bool disposeCancellation = false;

                lock (_pipelineLock)
                {
                    if (ReferenceEquals(_pendingCancellation, opportunityCancellation)
                        && _preparedOpportunity is null)
                    {
                        _pendingCancellation = null;
                        disposeCancellation = true;
                    }
                }

                if (disposeCancellation)
                {
                    opportunityCancellation.Dispose();
                }
            }
        }
    }

    private void StartDelivery(RadioTransitionDelivery transition)
    {
        PreparedTransitionOpportunity? prepared;
        CancellationTokenSource? cancellation;
        bool simulateFailure;

        lock (_pipelineLock)
        {
            prepared = _preparedOpportunity;
            cancellation = _pendingCancellation;

            if (prepared is null
                || prepared.Opportunity.Id != transition.OpportunityId
                || cancellation is null
                || cancellation.IsCancellationRequested)
            {
                prepared = null;
            }
            else
            {
                _preparedOpportunity = null;
                simulateFailure = _simulateNextTransitionFailure;
                _simulateNextTransitionFailure = false;
                _pipelineTask = Task.Run(
                    () => RunDeliveryAndFinalizeAsync(
                        prepared,
                        transition,
                        cancellation,
                        simulateFailure));
                return;
            }
        }

        _log.Write(
            "TRANSITION DELIVERY ABANDONED: Track-A speech was not fully ready when Track B arrived. Silence is correct.");
        CancelPendingPipeline("preparation did not finish before the delivery transition");
        lock (_stateLock)
        {
            _state.ResolveOpportunity(transition.OpportunityId, false, null);
        }

        WriteNextSchedule();
    }

    private async Task RunDeliveryAndFinalizeAsync(
        PreparedTransitionOpportunity prepared,
        RadioTransitionDelivery transition,
        CancellationTokenSource opportunityCancellation,
        bool simulateFailure)
    {
        try
        {
            TransitionDeliveryResult result = await _pipeline.DeliverAsync(
                prepared,
                transition,
                simulateFailure,
                opportunityCancellation.Token);

            string? acceptedAiComment = result.Status == TransitionDeliveryStatus.Completed
                && prepared.Selection.Source == CommentTextSource.Ai
                    ? prepared.Selection.Text
                    : null;
            bool resolved;

            lock (_stateLock)
            {
                resolved = _state.ResolveOpportunity(
                    transition.OpportunityId,
                    result.Status == TransitionDeliveryStatus.Completed,
                    acceptedAiComment);
            }

            _log.Write(
                $"AUTOMATIC TRANSITION RESULT: {result.Status.ToString().ToUpperInvariant()} - {result.Reason}.");

            if (resolved)
            {
                WriteHistorySizes();
                WriteNextSchedule();
            }
        }
        catch (Exception exception)
        {
            _log.Warning("Unexpected transition-delivery cleanup failure", exception);
            lock (_stateLock)
            {
                _state.ResolveOpportunity(transition.OpportunityId, false, null);
            }
        }
        finally
        {
            prepared.Dispose();

            lock (_pipelineLock)
            {
                if (ReferenceEquals(_pendingCancellation, opportunityCancellation))
                {
                    _pendingCancellation = null;
                }
            }

            opportunityCancellation.Dispose();
        }
    }

    private bool _aiEnabled;

    private void CancelPendingPipeline(string reason)
    {
        CancellationTokenSource? cancellation;
        PreparedTransitionOpportunity? prepared;
        bool disposeCancellation = false;

        lock (_pipelineLock)
        {
            cancellation = _pendingCancellation;
            prepared = _preparedOpportunity;
            _preparedOpportunity = null;

            if (cancellation is not null
                && prepared is not null
                && (_pipelineTask is null || _pipelineTask.IsCompleted))
            {
                _pendingCancellation = null;
                disposeCancellation = true;
            }
        }

        prepared?.Dispose();

        if (cancellation is null)
        {
            return;
        }

        if (!cancellation.IsCancellationRequested)
        {
            _log.Write($"Cancelling pending automatic preparation/delivery because {reason}.");
            cancellation.Cancel();
        }

        if (disposeCancellation)
        {
            cancellation.Dispose();
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

    private void DisposePreparedOpportunity()
    {
        PreparedTransitionOpportunity? prepared;
        CancellationTokenSource? cancellation;

        lock (_pipelineLock)
        {
            prepared = _preparedOpportunity;
            _preparedOpportunity = null;
            cancellation = _pendingCancellation;
            _pendingCancellation = null;
        }

        prepared?.Dispose();
        cancellation?.Dispose();
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
