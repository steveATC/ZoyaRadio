namespace ZoyaRadio.MediaProbe;

internal sealed partial class RadioBehaviorTest
{
    public event Action<RadioRuntimeStatusUpdate>? StatusChanged;

    public async Task<int> RunAsync(
        bool aiEnabled,
        bool simulateTransitionFailure,
        CancellationToken cancellationToken)
    {
        string logDirectory = Path.GetDirectoryName(_log.FilePath)
            ?? new LocalAppDataLogFolderProvider().GetLogFolderPath();
        var startOptions = new RadioRuntimeStartOptions(
            aiEnabled,
            simulateTransitionFailure,
            ConfirmConsoleStartAsync);

        await using var controller = new RadioRuntimeController(
            new SingleRadioRuntimeSessionFactory(this),
            startOptions,
            new FixedLogFolderProvider(logDirectory),
            new WindowsExternalFolderOpener());

        RadioControlStartResult start = await controller.StartAsync(cancellationToken);
        if (!start.Started)
        {
            return start.SuggestedExitCode;
        }

        try
        {
            await MonitorControllerConsoleAsync(controller, cancellationToken);
        }
        catch (OperationCanceledException)
        {
            _log.Write(
                "Shutdown requested. Cancelling prepared delivery and preserving exact Firefox volume restoration.");
        }

        RadioControlStopResult stop = await controller.StopAsync();
        if (!stop.SafeToClose)
        {
            _log.Write("!!! WARNING !!! Zoya could not confirm protected cleanup. Check Firefox volume manually.");
            return 1;
        }

        return 0;
    }

    public async Task<RadioRuntimeSessionStartResult> StartAsync(
        RadioRuntimeStartOptions options,
        CancellationToken cancellationToken)
    {
        lock (_runtimeLifecycleLock)
        {
            if (_runtimeStarted)
            {
                return new RadioRuntimeSessionStartResult(true, 0, "Automatic radio is already running");
            }

            if (_runtimeStopTask is not null)
            {
                throw new InvalidOperationException("This stopped runtime session cannot be restarted.");
            }

            _runtimeCancellation = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
            _shutdownToken = _runtimeCancellation.Token;
        }

        _simulateNextTransitionFailure = options.SimulateTransitionFailure;
        WriteConfiguration(options.AiEnabled);
        PublishRuntimeStatus(
            null,
            null,
            FirefoxTargetState.NotChecked,
            silentMode: false,
            "Checking Windows media sessions...",
            new RadioActivitySnapshot(RadioActivityState.ValidatingFirefoxTarget));

        RadioSessionDetails[] sessions;

        try
        {
            sessions = await _sessionAccess.ReadAllSessionsAsync();
            _shutdownToken.ThrowIfCancellationRequested();
        }
        catch (OperationCanceledException)
        {
            throw;
        }
        catch (Exception exception)
        {
            _log.Warning("Could not enumerate Windows media sessions for automatic behavior", exception);
            _log.Write("RADIO BEHAVIOR RESULT: NOT ARMED - no AI, playback, or speech commands were issued.");
            PublishRuntimeStatus(
                null,
                null,
                FirefoxTargetState.Unavailable,
                silentMode: false,
                "Windows media sessions are unavailable",
                new RadioActivitySnapshot(RadioActivityState.NoSafeFirefoxTarget));
            return new RadioRuntimeSessionStartResult(false, 1, "Windows media sessions are unavailable");
        }

        IReadOnlyList<RadioSessionDetails> matches = FirefoxSessionSelector.FindExactMatches(
            sessions,
            details => details.SourceId);

        if (matches.Count == 0)
        {
            _log.Write("NO FIREFOX TARGET FOUND: automatic behavior was not armed.");
            _log.Write("Start YouTube Music playing in Firefox, then try Start again.");
            PublishRuntimeStatus(
                null,
                null,
                FirefoxTargetState.Missing,
                silentMode: false,
                "No eligible Firefox media session was found",
                new RadioActivitySnapshot(RadioActivityState.NoSafeFirefoxTarget));
            return new RadioRuntimeSessionStartResult(false, 1, "No eligible Firefox media session was found");
        }

        if (matches.Count > 1)
        {
            _log.Write("!!! AMBIGUOUS TARGET !!!");
            _log.Write($"Found {matches.Count} sessions matching firefox.exe. Automatic behavior was not armed.");
            PublishRuntimeStatus(
                null,
                null,
                FirefoxTargetState.Ambiguous,
                silentMode: false,
                "More than one Firefox media session is available",
                new RadioActivitySnapshot(RadioActivityState.NoSafeFirefoxTarget));
            return new RadioRuntimeSessionStartResult(false, 1, "Firefox targeting is ambiguous");
        }

        RadioSessionDetails initial = matches[0];
        _sessionAccess.WriteSessionDetails("AUTOMATIC RADIO TARGET", initial);
        string? initialTitle = initial.Media.Title;
        string? initialArtist = initial.Media.Artist;

        if (!initial.MediaReadSucceeded || !AnnouncementText.TryBuild(initial.Media, out _))
        {
            _log.Write("METADATA SAFETY: Firefox did not provide both a usable title and artist.");
            _log.Write("Automatic behavior was not armed and no playback or speech commands were issued.");
            PublishRuntimeStatus(
                initialTitle,
                initialArtist,
                FirefoxTargetState.Unavailable,
                silentMode: false,
                "Firefox title or artist metadata is unavailable",
                new RadioActivitySnapshot(RadioActivityState.NoSafeFirefoxTarget));
            return new RadioRuntimeSessionStartResult(false, 1, "Firefox metadata is unavailable");
        }

        if (!initial.PlaybackReadSucceeded || !PlaybackControlRules.CanStart(initial.PlaybackStatus))
        {
            _log.Write(
                $"STARTING-STATE SAFETY: Firefox is {RadioSessionAccess.DisplayStatus(initial.PlaybackStatus)}, not Playing.");
            _log.Write("Automatic behavior was not armed. Zoya will not start paused media.");
            PublishRuntimeStatus(
                initialTitle,
                initialArtist,
                FirefoxTargetState.Unavailable,
                silentMode: false,
                "Firefox is not Playing",
                new RadioActivitySnapshot(RadioActivityState.NoSafeFirefoxTarget));
            return new RadioRuntimeSessionStartResult(false, 1, "Firefox is not Playing");
        }

        PublishRuntimeStatus(
            initialTitle,
            initialArtist,
            FirefoxTargetState.Safe,
            silentMode: false,
            "Firefox target validated",
            new RadioActivitySnapshot(RadioActivityState.ValidatingFirefoxTarget));

        if (options.ConfirmAsync is not null)
        {
            _log.Write(
                "Enable automatic transition delivery? This may duck Firefox and speak, but will not pause playback. [y/N]");

            bool confirmed;
            try
            {
                confirmed = await options.ConfirmAsync(
                    new RadioRuntimeTarget(
                        initial.SourceId ?? FirefoxSessionSelector.VerifiedSourceId,
                        initialTitle!,
                        initialArtist!),
                    _shutdownToken);
            }
            catch (OperationCanceledException)
            {
                _log.Write(
                    "AUTOMATIC RADIO CANCELLED before arming. No AI, playback, or speech commands were issued.");
                return new RadioRuntimeSessionStartResult(false, 0, "Start cancelled before arming");
            }

            if (!confirmed)
            {
                _log.Write("User confirmation: NO");
                _log.Write("AUTOMATIC RADIO NOT ARMED: no AI, playback, or speech commands were issued.");
                return new RadioRuntimeSessionStartResult(false, 0, "Automatic radio was not armed");
            }

            _log.Write("User confirmation: YES");
        }

        RadioSessionDetails? rechecked = await _sessionAccess.RecheckStartupTargetAsync(initial);
        _shutdownToken.ThrowIfCancellationRequested();
        if (rechecked is null)
        {
            _log.Write("STARTUP REVALIDATION FAILED: session, track, or playback changed while starting.");
            _log.Write("Automatic behavior was not armed. Try Start again on the current track.");
            PublishRuntimeStatus(
                initialTitle,
                initialArtist,
                FirefoxTargetState.Unavailable,
                silentMode: false,
                "Firefox changed while Zoya was starting",
                new RadioActivitySnapshot(RadioActivityState.NoSafeFirefoxTarget));
            return new RadioRuntimeSessionStartResult(false, 1, "Firefox changed while Zoya was starting");
        }

        _selectedSession = rechecked.Session;
        await _mediaEventGate.WaitAsync(_shutdownToken);
        try
        {
            if (!TrySubscribe())
            {
                _log.Write(
                    "RADIO BEHAVIOR RESULT: NOT ARMED - required Windows change observers were unavailable.");
                PublishRuntimeStatus(
                    rechecked.Media.Title,
                    rechecked.Media.Artist,
                    FirefoxTargetState.Unavailable,
                    silentMode: false,
                    "Windows change observers are unavailable",
                    new RadioActivitySnapshot(RadioActivityState.NoSafeFirefoxTarget));
                return new RadioRuntimeSessionStartResult(false, 1, "Windows change observers are unavailable");
            }

            lock (_stateLock)
            {
                _state.Arm(rechecked.Media, _clock.Now);
            }

            lock (_linerLock)
            {
                _linerInventory.Arm(_state.CurrentSessionId, _clock.Now, _state.TotalObservedTransitions);
            }

            lock (_runtimeLifecycleLock)
            {
                _runtimeStarted = true;
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
        StartLinerPreparation();
        PublishRuntimeStatus(
            rechecked.Media.Title,
            rechecked.Media.Artist,
            FirefoxTargetState.Safe,
            silentMode: false,
            "Running and observing Firefox",
            CreateObservingActivity());

        return new RadioRuntimeSessionStartResult(true, 0, "Running and observing Firefox");
    }

    public Task StopAsync()
    {
        lock (_runtimeLifecycleLock)
        {
            _runtimeStopTask ??= StopCoreAsync();
            return _runtimeStopTask;
        }
    }

    public async ValueTask DisposeAsync()
    {
        await StopAsync();
        _m9bCommentProvider?.ClearEphemeralHistory();
    }

    public void SetSilentMode(bool silent)
    {
        RadioModeChange change;

        lock (_stateLock)
        {
            change = _state.SetSilentMode(silent);
        }

        if (change.PendingOpportunityInvalidated)
        {
            CancelPendingPipeline("Silent Mode changed");
        }

        if (silent)
        {
            CancelLinerPreparation("Silent Mode is ON");
        }
        else
        {
            StartLinerPreparation();
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

        RadioRuntimeState state = GetRuntimeState();
        PublishRuntimeStatus(
            state.CurrentTrack.Title,
            state.CurrentTrack.Artist,
            FirefoxTargetState.Safe,
            change.SilentMode,
            change.SilentMode ? "Running silently" : "Running and observing Firefox",
            change.SilentMode
                ? new RadioActivitySnapshot(RadioActivityState.SilentMode)
                : CreateObservingActivity());
    }

    public void ClearRecentHistory()
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

        _m9bCommentProvider?.ClearHistoryAndCache();
        _log.Write("RECENT HISTORY CLEARED: recent tracks and accepted AI comments were removed from memory.");
        _log.Write("The current diagnostic log was not deleted or rewritten.");
        _log.Write("Reusable liner inventory and cooldown are operational state and were left unchanged.");
        _log.Write(
            invalidated
                ? "A pending comment was cancelled; if Firefox was already ducked, protected exact-volume restoration still runs."
                : "No volume or speech command was issued by the clear-history command.");
        WriteHistorySizes();
        WriteNextSchedule();

        RadioRuntimeState state = GetRuntimeState();
        PublishRuntimeStatus(
            state.CurrentTrack.Title,
            state.CurrentTrack.Artist,
            FirefoxTargetState.Safe,
            state.SilentMode,
            "Recent in-memory history cleared",
            CreateObservingActivity());
    }

    private async Task StopCoreAsync()
    {
        bool wasStarted;
        lock (_runtimeLifecycleLock)
        {
            wasStarted = _runtimeStarted;
            _runtimeCancellation?.Cancel();
        }

        lock (_stateLock)
        {
            _state.Disarm();
        }

        CancelPendingPipeline("automatic behavior is shutting down");
        CancelLinerPreparation("automatic behavior is shutting down");
        Unsubscribe();
        await WaitForPipelineAsync();
        await WaitForLinerPreparationAsync();
        InvalidateLinerInventory();
        DisposePreparedOpportunity();
        await _mediaEventGate.WaitAsync();
        _mediaEventGate.Release();

        lock (_runtimeLifecycleLock)
        {
            _runtimeStarted = false;
            _runtimeCancellation?.Dispose();
            _runtimeCancellation = null;
        }

        if (wasStarted)
        {
            _log.Write("AUTOMATIC RADIO: STOPPED CLEANLY");
        }

        PublishRuntimeStatus(
            null,
            null,
            FirefoxTargetState.NotChecked,
            silentMode: false,
            "Stopped safely",
            new RadioActivitySnapshot(RadioActivityState.Stopped));
        _log.Write("AUTOMATIC RADIO: STOP STATUS PUBLISHED TO THE CONTROL LAYER");
    }

    private static async Task<bool> ConfirmConsoleStartAsync(
        RadioRuntimeTarget target,
        CancellationToken cancellationToken)
    {
        string? response = await Console.In.ReadLineAsync(cancellationToken);
        return PlaybackControlRules.IsAffirmative(response);
    }

    private async Task MonitorControllerConsoleAsync(
        RadioRuntimeController controller,
        CancellationToken cancellationToken)
    {
        _log.Write("Console controls: S = toggle Silent Mode, C = clear recent history, Ctrl+C = stop safely.");
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
                            controller.SetSilentMode(!controller.CurrentStatus.SilentMode);
                        }
                        else if (key.Key == ConsoleKey.C)
                        {
                            controller.ClearRecentHistory();
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

    private void ReportActivity(RadioActivitySnapshot activity)
    {
        RadioRuntimeState state = GetRuntimeState();
        RadioRuntimeStatusUpdate? lastStatus;

        lock (_statusLock)
        {
            lastStatus = _lastStatus;
        }

        PublishRuntimeStatus(
            state.CurrentTrack.Title,
            state.CurrentTrack.Artist,
            lastStatus?.FirefoxTarget ?? (state.Armed ? FirefoxTargetState.Safe : FirefoxTargetState.NotChecked),
            state.SilentMode,
            lastStatus?.StatusText ?? "Running and observing Firefox",
            activity);
    }

    private RadioActivitySnapshot CreateObservingActivity()
    {
        lock (_stateLock)
        {
            if (_state.SilentMode)
            {
                return new RadioActivitySnapshot(RadioActivityState.SilentMode);
            }

            int remaining = Math.Max(1, _state.ScheduledThreshold - _state.TransitionCounter);
            return new RadioActivitySnapshot(RadioActivityState.Observing, remaining);
        }
    }

    private static RadioActivitySnapshot CreateTransitionActivity(
        RadioTrackObservation observation,
        bool silentMode)
    {
        if (silentMode)
        {
            return new RadioActivitySnapshot(RadioActivityState.SilentMode);
        }

        if (observation.PendingOpportunityInvalidated)
        {
            return new RadioActivitySnapshot(RadioActivityState.OpportunityAbandoned);
        }

        if (observation.Delivery is not null)
        {
            return new RadioActivitySnapshot(RadioActivityState.ValidatingFirefoxTarget);
        }

        if (observation.PreparationOpportunity is not null)
        {
            return new RadioActivitySnapshot(RadioActivityState.WaitingForTrackStability);
        }

        int remaining = Math.Max(1, observation.ScheduledThreshold - observation.TransitionCounter);
        return new RadioActivitySnapshot(RadioActivityState.Observing, remaining);
    }

    private void PublishTargetStatus(
        FirefoxTargetState firefoxTarget,
        string statusText,
        RadioActivitySnapshot? activity = null)
    {
        RadioRuntimeState state = GetRuntimeState();
        RadioActivitySnapshot? effectiveActivity = activity ?? (firefoxTarget == FirefoxTargetState.Safe
            ? null
            : new RadioActivitySnapshot(RadioActivityState.NoSafeFirefoxTarget));
        PublishRuntimeStatus(
            state.CurrentTrack.Title,
            state.CurrentTrack.Artist,
            firefoxTarget,
            state.SilentMode,
            statusText,
            effectiveActivity);
    }

    private void PublishRuntimeStatus(
        string? title,
        string? artist,
        FirefoxTargetState firefoxTarget,
        bool silentMode,
        string statusText,
        RadioActivitySnapshot? activity = null)
    {
        RadioRuntimeStatusUpdate update;
        Delegate[] subscribers;

        lock (_statusLock)
        {
            RadioActivitySnapshot effectiveActivity = activity ?? _activity;
            update = new RadioRuntimeStatusUpdate(
                title,
                artist,
                firefoxTarget,
                silentMode,
                effectiveActivity,
                statusText);

            if (update == _lastStatus)
            {
                return;
            }

            _lastStatus = update;
            _activity = effectiveActivity;
            subscribers = StatusChanged?.GetInvocationList() ?? [];
        }

        foreach (Delegate subscriber in subscribers)
        {
            try
            {
                ((Action<RadioRuntimeStatusUpdate>)subscriber)(update);
            }
            catch (Exception exception)
            {
                _log.Warning("A runtime-status listener failed", exception);
            }
        }
    }
}
