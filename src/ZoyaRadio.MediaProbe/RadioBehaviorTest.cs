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
    private readonly RadioOpportunityPipeline _pipeline;
    private readonly object _stateLock = new();
    private readonly object _pipelineLock = new();
    private readonly SemaphoreSlim _mediaEventGate = new(1, 1);
    private GlobalSystemMediaTransportControlsSession? _selectedSession;
    private CancellationToken _shutdownToken;
    private CancellationTokenSource? _pendingCancellation;
    private Task? _pipelineTask;
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
        var commentProvider = new LocalRadioCommentProvider(aiService);
        var opportunityHost = new WindowsRadioOpportunityHost(
            () => _selectedSession,
            GetRuntimeState,
            _sessionAccess,
            speechService,
            log);
        _pipeline = new RadioOpportunityPipeline(
            _radioConfiguration,
            commentProvider,
            opportunityHost,
            _clock,
            log);
    }

    public async Task<int> RunAsync(bool aiEnabled, CancellationToken cancellationToken)
    {
        _shutdownToken = cancellationToken;
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

        _log.Write("Enable automatic Zoya Radio behavior? [y/N]");
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
            _log.Write("Shutdown requested. Cancelling any pending comment and preserving playback recovery.");
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
            _log.Write("No new AI generation, automatic speech, or automatic pause will begin.");
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
                ? "A pending comment was cancelled; if Zoya had already paused Firefox, only protected resume recovery may run."
                : "No playback or speech command was issued by the clear-history command.");
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
        RadioCommentOpportunity? opportunity = null;

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
                _log.Write("PENDING COMMENT ABANDONED: the track changed during generation or waiting.");
                CancelPendingPipeline("the current track changed");
                WriteNextSchedule();
            }

            opportunity = observation.Opportunity;
        }
        finally
        {
            _mediaEventGate.Release();
        }

        if (opportunity is not null)
        {
            StartOpportunity(opportunity);
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

    private void StartOpportunity(RadioCommentOpportunity opportunity)
    {
        var linkedCancellation = CancellationTokenSource.CreateLinkedTokenSource(_shutdownToken);

        lock (_pipelineLock)
        {
            if (_pipelineTask is not null && !_pipelineTask.IsCompleted)
            {
                linkedCancellation.Dispose();
                lock (_stateLock)
                {
                    _state.ResolveOpportunity(opportunity.Id, false, null);
                }

                _log.Write("COMMENT OPPORTUNITY SKIPPED: another announcement pipeline is still cleaning up.");
                _log.Write("No missed comment was queued.");
                WriteNextSchedule();
                return;
            }

            _pendingCancellation = linkedCancellation;
            _pipelineTask = Task.Run(
                () => RunOpportunityAndFinalizeAsync(opportunity, linkedCancellation));
        }
    }

    private async Task RunOpportunityAndFinalizeAsync(
        RadioCommentOpportunity opportunity,
        CancellationTokenSource opportunityCancellation)
    {
        try
        {
            RadioOpportunityResult result = await _pipeline.RunAsync(
                opportunity,
                _aiEnabled,
                opportunityCancellation.Token);

            string? acceptedAiComment = result.Status == RadioOpportunityResultStatus.Completed
                && result.Selection?.Source == CommentTextSource.Ai
                    ? result.Selection.Text
                    : null;
            bool resolved;

            lock (_stateLock)
            {
                resolved = _state.ResolveOpportunity(
                    opportunity.Id,
                    result.Status == RadioOpportunityResultStatus.Completed,
                    acceptedAiComment);
            }

            _log.Write($"AUTOMATIC COMMENT RESULT: {result.Status.ToString().ToUpperInvariant()} - {result.Reason}.");

            if (resolved)
            {
                WriteHistorySizes();
                WriteNextSchedule();
            }
        }
        catch (Exception exception)
        {
            _log.Warning("Unexpected automatic pipeline cleanup failure", exception);
            lock (_stateLock)
            {
                _state.ResolveOpportunity(opportunity.Id, false, null);
            }
        }
        finally
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
    }

    private bool _aiEnabled;

    private void CancelPendingPipeline(string reason)
    {
        CancellationTokenSource? cancellation;

        lock (_pipelineLock)
        {
            cancellation = _pendingCancellation;
        }

        if (cancellation is null || cancellation.IsCancellationRequested)
        {
            return;
        }

        _log.Write($"Cancelling pending automatic comment because {reason}.");
        cancellation.Cancel();
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
            $"  Minimum current-track age before speech: {_radioConfiguration.MinimumTrackAgeBeforeSpeech.TotalSeconds:0} seconds");
        _log.Write($"  Maximum automatic speech duration: {_radioConfiguration.MaximumSpeechDuration.TotalSeconds:0} seconds");
        _log.Write($"  Recent in-memory tracks: {_radioConfiguration.RecentTrackLimit}");
        _log.Write($"  Recent accepted AI comments: {_radioConfiguration.RecentCommentLimit}");
        _log.Write($"  AI mode: {(aiEnabled ? "local Ollama when scheduled" : "OFF - fixed fallback only")}");
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

        if (observation.Opportunity is not null)
        {
            _log.Write("COMMENT OPPORTUNITY REACHED: one generation pipeline will start.");
            _log.Write("The primary comment subject is the previously playing track.");
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
            _log.Write($"Next comment target: after {_state.ScheduledThreshold} observed track transition(s).");
        }
    }

    private sealed class LocalRadioCommentProvider : IRadioCommentProvider
    {
        private readonly AiCommentService _service;

        public LocalRadioCommentProvider(AiCommentService service)
        {
            _service = service;
        }

        public Task<CommentSelection> SelectAsync(
            RadioCommentContext context,
            bool aiEnabled,
            CancellationToken cancellationToken)
        {
            return _service.SelectRadioCommentAsync(context, aiEnabled, cancellationToken);
        }
    }
}
