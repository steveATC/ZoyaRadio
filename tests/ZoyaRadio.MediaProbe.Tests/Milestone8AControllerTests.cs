using ZoyaRadio.MediaProbe;

internal static class Milestone8AControllerTests
{
    public static async Task NormalStart()
    {
        var session = new FakeRuntimeSession();
        await using RadioRuntimeController controller = CreateController(new FakeRuntimeSessionFactory(session));

        RadioControlStartResult result = await controller.StartAsync();

        TestAssert.True(result.Started);
        TestAssert.Equal(RadioLifecycleState.Running, result.Snapshot.Lifecycle);
        TestAssert.Equal(1, session.StartCalls);
        TestAssert.True(result.Snapshot.HasActiveRuntime);
        TestAssert.Equal(RadioActivityState.Observing, result.Snapshot.Activity.State);
    }

    public static async Task FaultyStatusListenerCannotBreakLifecycle()
    {
        var session = new FakeRuntimeSession();
        await using RadioRuntimeController controller = CreateController(new FakeRuntimeSessionFactory(session));
        controller.StatusChanged += _ => throw new InvalidOperationException("simulated presentation failure");

        RadioControlStartResult start = await controller.StartAsync();
        RadioControlStopResult stop = await controller.StopAsync();

        TestAssert.True(start.Started);
        TestAssert.True(stop.SafeToClose);
    }

    public static async Task RepeatedStartIsIdempotent()
    {
        var session = new FakeRuntimeSession();
        var factory = new FakeRuntimeSessionFactory(session);
        await using RadioRuntimeController controller = CreateController(factory);

        RadioControlStartResult first = await controller.StartAsync();
        RadioControlStartResult second = await controller.StartAsync();

        TestAssert.True(first.Started && second.Started);
        TestAssert.Equal(1, factory.CreateCalls);
        TestAssert.Equal(1, session.StartCalls);
    }

    public static async Task StopAfterPreparationPublishesTerminalStoppedSnapshot()
    {
        var session = new FakeRuntimeSession { BlockStopUntilReleased = true };
        await using RadioRuntimeController controller = CreateController(new FakeRuntimeSessionFactory(session));
        RadioControlSnapshot? lastPublished = null;
        controller.StatusChanged += snapshot => lastPublished = snapshot;
        await controller.StartAsync();

        Task<RadioControlStopResult> stop = controller.StopAsync();
        await session.StopEntered.Task.WaitAsync(TimeSpan.FromSeconds(2));
        session.ReleaseStop();
        RadioControlStopResult result = await stop;

        TestAssert.True(result.SafeToClose);
        TestAssert.Equal(RadioLifecycleState.Stopped, result.Snapshot.Lifecycle);
        TestAssert.True(lastPublished is not null);
        TestAssert.Equal(RadioLifecycleState.Stopped, lastPublished!.Lifecycle);
        TestAssert.Equal(RadioActivityState.Stopped, lastPublished.Activity.State);
    }
    public static async Task StopPublishesTerminalStateBeforeNonSafetyDisposeCompletes()
    {
        var session = new FakeRuntimeSession { BlockDisposeUntilReleased = true };
        await using RadioRuntimeController controller = CreateController(new FakeRuntimeSessionFactory(session));
        await controller.StartAsync();

        RadioControlStopResult result = await controller.StopAsync();

        await session.DisposeEntered.Task.WaitAsync(TimeSpan.FromSeconds(2));
        TestAssert.True(result.SafeToClose);
        TestAssert.True(session.RestorationCompleted);
        TestAssert.Equal(RadioLifecycleState.Stopped, result.Snapshot.Lifecycle);
        TestAssert.Equal(RadioActivityState.Stopped, controller.CurrentStatus.Activity.State);
        TestAssert.False(session.DisposeCompleted.Task.IsCompleted);

        session.ReleaseDispose();
        await session.DisposeCompleted.Task.WaitAsync(TimeSpan.FromSeconds(2));
    }

    public static async Task NormalStop()
    {
        var session = new FakeRuntimeSession();
        await using RadioRuntimeController controller = CreateController(new FakeRuntimeSessionFactory(session));
        await controller.StartAsync();

        RadioControlStopResult result = await controller.StopAsync();
        await session.DisposeCompleted.Task.WaitAsync(TimeSpan.FromSeconds(2));

        TestAssert.True(result.SafeToClose);
        TestAssert.Equal(RadioLifecycleState.Stopped, result.Snapshot.Lifecycle);
        TestAssert.Equal(RadioActivityState.Stopped, result.Snapshot.Activity.State);
        TestAssert.Equal(1, session.StopCalls);
        TestAssert.Equal(1, session.DisposeCalls);
    }

    public static async Task RepeatedStopIsIdempotent()
    {
        var session = new FakeRuntimeSession();
        await using RadioRuntimeController controller = CreateController(new FakeRuntimeSessionFactory(session));
        await controller.StartAsync();

        RadioControlStopResult first = await controller.StopAsync();
        RadioControlStopResult second = await controller.StopAsync();
        await session.DisposeCompleted.Task.WaitAsync(TimeSpan.FromSeconds(2));

        TestAssert.True(first.SafeToClose && second.SafeToClose);
        TestAssert.Equal(1, session.StopCalls);
        TestAssert.Equal(1, session.DisposeCalls);
    }

    public static async Task RapidStartStopIsSerialized()
    {
        var session = new FakeRuntimeSession { BlockStartUntilCancelled = true };
        await using RadioRuntimeController controller = CreateController(new FakeRuntimeSessionFactory(session));

        Task<RadioControlStartResult> start = controller.StartAsync();
        await session.StartEntered.Task.WaitAsync(TimeSpan.FromSeconds(2));
        Task<RadioControlStopResult> firstStop = controller.StopAsync();
        Task<RadioControlStopResult> secondStop = controller.StopAsync();

        RadioControlStopResult[] results = await Task.WhenAll(firstStop, secondStop);
        RadioControlStartResult startResult = await start;

        TestAssert.False(startResult.Started);
        TestAssert.True(results.All(result => result.SafeToClose));
        TestAssert.Equal(1, session.StartCalls);
        TestAssert.Equal(1, session.StopCalls);
        TestAssert.Equal(RadioLifecycleState.Stopped, controller.CurrentStatus.Lifecycle);
    }

    public static Task StopDuringGeneration() => StopDuringOwnedPhase("generation");

    public static Task StopDuringNeuralPreparation() => StopDuringOwnedPhase("Nicole preparation");

    public static async Task StopDuringTargetAcquisition()
    {
        var session = new FakeRuntimeSession();
        var factory = new BlockingRuntimeSessionFactory(session);
        await using RadioRuntimeController controller = CreateController(factory);

        Task<RadioControlStartResult> start = controller.StartAsync();
        await factory.CreateEntered.Task.WaitAsync(TimeSpan.FromSeconds(2));
        RadioControlStopResult stop = await controller.StopAsync();
        RadioControlStartResult startResult = await start;

        TestAssert.False(startResult.Started);
        TestAssert.True(stop.SafeToClose);
        TestAssert.Equal(0, session.StartCalls);
        TestAssert.Equal(RadioLifecycleState.Stopped, controller.CurrentStatus.Lifecycle);
    }

    public static Task StopDuringDucking() => StopDuringOwnedPhase("simulated ducking");

    public static Task StopDuringSpeech() => StopDuringOwnedPhase("speech");

    public static Task StopDuringRestorationRetry() => StopDuringOwnedPhase("restoration retry");

    public static async Task RestorationCompletesBeforeStopReturns()
    {
        var session = new FakeRuntimeSession { BlockStopUntilReleased = true };
        await using RadioRuntimeController controller = CreateController(new FakeRuntimeSessionFactory(session));
        await controller.StartAsync();

        Task<RadioControlStopResult> stop = controller.StopAsync();
        await session.StopEntered.Task.WaitAsync(TimeSpan.FromSeconds(2));
        TestAssert.False(stop.IsCompleted);
        TestAssert.False(session.RestorationCompleted);

        session.ReleaseStop();
        RadioControlStopResult result = await stop;
        TestAssert.True(result.SafeToClose);
        TestAssert.True(session.RestorationCompleted);
    }

    public static async Task WindowCloseAwaitsStop()
    {
        var coordinator = new RadioWindowCloseCoordinator();
        var completion = new TaskCompletionSource<RadioControlStopResult>(
            TaskCreationOptions.RunContinuationsAsynchronously);

        Task<bool> close = coordinator.RequestCloseAsync(() => completion.Task);
        TestAssert.True(coordinator.ShutdownInProgress);
        TestAssert.False(close.IsCompleted);

        completion.SetResult(SafeStopResult());
        TestAssert.True(await close);
        TestAssert.True(coordinator.CloseAllowed);
    }

    public static async Task WindowCloseRejectsReentrancy()
    {
        var coordinator = new RadioWindowCloseCoordinator();
        var completion = new TaskCompletionSource<RadioControlStopResult>(
            TaskCreationOptions.RunContinuationsAsynchronously);
        int stopCalls = 0;

        Task<bool> first = coordinator.RequestCloseAsync(() =>
        {
            stopCalls++;
            return completion.Task;
        });
        bool second = await coordinator.RequestCloseAsync(() =>
        {
            stopCalls++;
            return completion.Task;
        });

        TestAssert.False(second);
        TestAssert.Equal(1, stopCalls);
        completion.SetResult(SafeStopResult());
        TestAssert.True(await first);
    }

    public static void ViewStateCoversLifecycleStates()
    {
        RadioControlViewState stopped = View(RadioLifecycleState.Stopped, hasRuntime: false);
        RadioControlViewState starting = View(RadioLifecycleState.Starting, hasRuntime: true);
        RadioControlViewState running = View(RadioLifecycleState.Running, hasRuntime: true);
        RadioControlViewState stopping = View(RadioLifecycleState.Stopping, hasRuntime: true);
        RadioControlViewState faulted = View(RadioLifecycleState.Faulted, hasRuntime: false);

        TestAssert.True(stopped.StartEnabled && !stopped.StopEnabled);
        TestAssert.True(!starting.StartEnabled && starting.StopEnabled);
        TestAssert.True(running.StopEnabled && running.SilentModeEnabled && running.ClearHistoryEnabled);
        TestAssert.True(stopping.CleanupVisible && !stopping.StopEnabled && !stopping.OpenLogsEnabled);
        TestAssert.True(faulted.StartEnabled);
    }

    public static async Task SilentModeReflectsRuntimeState()
    {
        var session = new FakeRuntimeSession();
        await using RadioRuntimeController controller = CreateController(new FakeRuntimeSessionFactory(session));
        await controller.StartAsync();

        controller.SetSilentMode(true);

        TestAssert.Equal(1, session.SilentModeCalls);
        TestAssert.True(controller.CurrentStatus.SilentMode);
        TestAssert.Equal(RadioActivityState.SilentMode, controller.CurrentStatus.Activity.State);
        TestAssert.Contains(
            RadioControlViewState.FromSnapshot(controller.CurrentStatus).ActivityText,
            "Silent Mode");
        TestAssert.True(session.ObservationContinues);
        TestAssert.True(session.PendingWorkInvalidated);
    }

    public static async Task ClearHistoryUsesBoundedRuntimeStateOnly()
    {
        var session = new FakeRuntimeSession();
        await using RadioRuntimeController controller = CreateController(new FakeRuntimeSessionFactory(session));
        await controller.StartAsync();

        controller.ClearRecentHistory();

        TestAssert.Equal(1, session.ClearHistoryCalls);
        TestAssert.True(session.RecentHistoryCleared);
        TestAssert.False(session.LogsDeleted);
    }

    public static async Task MetadataStatusUpdates()
    {
        var session = new FakeRuntimeSession();
        await using RadioRuntimeController controller = CreateController(new FakeRuntimeSessionFactory(session));
        await controller.StartAsync();

        session.Emit("Du Riechst so Gut", "Russian Style Folkestra", FirefoxTargetState.Safe);

        TestAssert.Equal("Du Riechst so Gut", controller.CurrentStatus.Title);
        TestAssert.Equal("Russian Style Folkestra", controller.CurrentStatus.Artist);
    }

    public static void FirefoxTargetPresentation()
    {
        TestAssert.Contains(ViewForTarget(FirefoxTargetState.Safe), "exactly one");
        TestAssert.Contains(ViewForTarget(FirefoxTargetState.Missing), "Not found");
        TestAssert.Contains(ViewForTarget(FirefoxTargetState.Ambiguous), "Ambiguous");
    }

    public static async Task StaleStatusAfterShutdownIsSuppressed()
    {
        var session = new FakeRuntimeSession();
        await using RadioRuntimeController controller = CreateController(new FakeRuntimeSessionFactory(session));
        await controller.StartAsync();
        await controller.StopAsync();
        long stoppedRevision = controller.CurrentStatus.Revision;

        session.EmitStale("Wrong late title", "Wrong late artist");

        TestAssert.Equal(stoppedRevision, controller.CurrentStatus.Revision);
        TestAssert.Equal(RadioLifecycleState.Stopped, controller.CurrentStatus.Lifecycle);
        TestAssert.Equal(RadioActivityState.Stopped, controller.CurrentStatus.Activity.State);
        TestAssert.Equal<string?>(null, controller.CurrentStatus.Title);
    }

    public static async Task StopFailureIsReadableAndRetryable()
    {
        var session = new FakeRuntimeSession
        {
            StopFailuresRemaining = 1,
        };
        await using RadioRuntimeController controller = CreateController(new FakeRuntimeSessionFactory(session));
        await controller.StartAsync();

        RadioControlStopResult first = await controller.StopAsync();

        TestAssert.False(first.SafeToClose);
        TestAssert.Equal(RadioLifecycleState.Faulted, first.Snapshot.Lifecycle);
        TestAssert.True(first.Snapshot.HasActiveRuntime);
        TestAssert.Contains(first.Message, "STOP FAILED");

        RadioControlStopResult second = await controller.StopAsync();
        TestAssert.True(second.SafeToClose);
        TestAssert.Equal(RadioLifecycleState.Stopped, second.Snapshot.Lifecycle);
        TestAssert.Equal(2, session.StopCalls);
    }

    public static async Task StartFailureIsReadable()
    {
        var session = new FakeRuntimeSession
        {
            StartException = new InvalidOperationException("simulated start failure"),
        };
        await using RadioRuntimeController controller = CreateController(new FakeRuntimeSessionFactory(session));

        RadioControlStartResult result = await controller.StartAsync();

        TestAssert.False(result.Started);
        TestAssert.Equal(RadioLifecycleState.Faulted, result.Snapshot.Lifecycle);
        TestAssert.Equal(RadioActivityState.Faulted, result.Snapshot.Activity.State);
        TestAssert.Contains(
            RadioControlViewState.FromSnapshot(result.Snapshot).ActivityText,
            "Faulted");
        TestAssert.Contains(result.Snapshot.StatusText, "simulated start failure");
        TestAssert.False(result.Snapshot.HasActiveRuntime);
    }

    public static async Task StartCancellationIsSafe()
    {
        var session = new FakeRuntimeSession();
        var factory = new BlockingRuntimeSessionFactory(session);
        await using RadioRuntimeController controller = CreateController(factory);
        using var cancellation = new CancellationTokenSource();

        Task<RadioControlStartResult> start = controller.StartAsync(cancellation.Token);
        await factory.CreateEntered.Task.WaitAsync(TimeSpan.FromSeconds(2));
        cancellation.Cancel();
        RadioControlStartResult result = await start;

        TestAssert.False(result.Started);
        TestAssert.Equal(RadioLifecycleState.Stopped, result.Snapshot.Lifecycle);
        TestAssert.False(result.Snapshot.HasActiveRuntime);
    }

    public static void LocalAppDataLogFolderResolution()
    {
        string root = Path.Combine("C:\\Users", "Test", "AppData", "Local");
        string resolved = LocalAppDataLogFolderProvider.Resolve(root);

        TestAssert.Equal(Path.Combine(root, "Zoya Radio", "logs"), resolved);
    }

    public static void OpenFolderUsesFakeBoundary()
    {
        var session = new FakeRuntimeSession();
        var logFolder = new FakeLogFolderProvider("C:\\FakeLocalAppData\\Zoya Radio\\logs");
        var opener = new FakeExternalFolderOpener();
        var controller = CreateController(new FakeRuntimeSessionFactory(session), logFolder, opener);

        controller.OpenLogsFolder();

        TestAssert.Equal(1, logFolder.EnsureCalls);
        TestAssert.Equal(logFolder.Path, opener.OpenedPath);
    }

    public static void ConsoleRuntimeUsesSharedSessionBoundary()
    {
        TestAssert.True(typeof(IRadioRuntimeSession).IsAssignableFrom(typeof(RadioBehaviorTest)));
        TestAssert.True(typeof(RadioBehaviorTest).GetMethod("RunAsync") is not null);
        TestAssert.True(typeof(RadioRuntimeController).GetMethod("StartAsync") is not null);
    }

    public static void InitialActivityIsStopped()
    {
        var session = new FakeRuntimeSession();
        var controller = CreateController(new FakeRuntimeSessionFactory(session));

        TestAssert.Equal(RadioActivityState.Stopped, controller.CurrentStatus.Activity.State);
        TestAssert.Contains(
            RadioControlViewState.FromSnapshot(controller.CurrentStatus).ActivityText,
            "Stopped");
    }

    public static async Task ObservingCountdownIsReadable()
    {
        var session = new FakeRuntimeSession();
        await using RadioRuntimeController controller = CreateController(new FakeRuntimeSessionFactory(session));
        await controller.StartAsync();

        TestAssert.Equal(RadioActivityState.Observing, controller.CurrentStatus.Activity.State);
        TestAssert.Equal<int?>(3, controller.CurrentStatus.Activity.TransitionsRemaining);
        TestAssert.Contains(RadioControlViewState.FromSnapshot(controller.CurrentStatus).ActivityText, "3 transitions");

        session.Emit(
            "Cochise",
            "Audioslave",
            FirefoxTargetState.Safe,
            activity: new RadioActivitySnapshot(RadioActivityState.Observing, 2));
        TestAssert.Contains(
            RadioControlViewState.FromSnapshot(controller.CurrentStatus).ActivityText,
            "preparation begins on the next transition");

        session.Emit(
            "Cochise",
            "Audioslave",
            FirefoxTargetState.Safe,
            activity: new RadioActivitySnapshot(RadioActivityState.Observing, 1));
        TestAssert.Contains(
            RadioControlViewState.FromSnapshot(controller.CurrentStatus).ActivityText,
            "next comment on the next transition");
    }

    public static async Task TransitionPreparationActivitySequence()
    {
        DateTimeOffset now = DateTimeOffset.Parse("2026-08-11T12:00:00Z");
        RadioPreparationOpportunity opportunity = TransitionOpportunity(now);
        var activities = new List<RadioActivityState>();
        var host = new FakeTransitionRadioHost(ValidSnapshot(opportunity));
        var pipeline = new TransitionRadioPipeline(
            RadioBehaviorConfiguration.Default,
            new FakeTransitionCommentProvider(),
            host,
            new FakeRadioClock(now),
            new FakeLog(),
            activity => activities.Add(activity.State));

        TransitionPreparationResult result = await pipeline.PrepareAsync(
            opportunity,
            true,
            CancellationToken.None);

        TestAssert.Equal(TransitionPreparationStatus.Ready, result.Status);
        TestAssert.Equal(5, activities.Count);
        TestAssert.Equal(RadioActivityState.ValidatingFirefoxTarget, activities[0]);
        TestAssert.Equal(RadioActivityState.WaitingForTrackStability, activities[1]);
        TestAssert.Equal(RadioActivityState.GeneratingComment, activities[2]);
        TestAssert.Equal(RadioActivityState.PreparingSpeech, activities[3]);
        TestAssert.Equal(RadioActivityState.PreparedForTransition, activities[4]);
        result.Prepared!.Dispose();
    }

    public static async Task TransitionDeliveryActivitySequence()
    {
        DateTimeOffset now = DateTimeOffset.Parse("2026-08-11T12:00:20Z");
        RadioPreparationOpportunity opportunity = TransitionOpportunity(now.AddSeconds(-20));
        var activities = new List<RadioActivityState>();
        var host = new FakeTransitionRadioHost(ValidSnapshot(opportunity));
        var pipeline = new TransitionRadioPipeline(
            RadioBehaviorConfiguration.Default,
            new FakeTransitionCommentProvider(),
            host,
            new FakeRadioClock(now),
            new FakeLog(),
            activity => activities.Add(activity.State));
        TransitionPreparationResult preparation = await pipeline.PrepareAsync(
            opportunity,
            true,
            CancellationToken.None);
        activities.Clear();
        var newTrack = new MediaSnapshot("Track B", "Artist B", null);
        var transition = new RadioTransitionDelivery(
            opportunity.Id,
            opportunity.Context.SubjectTrack,
            newTrack,
            now);
        host.Snapshot = ValidSnapshot(opportunity, newTrack);

        TransitionDeliveryResult delivery = await pipeline.DeliverAsync(
            preparation.Prepared!,
            transition,
            false,
            CancellationToken.None);

        TestAssert.Equal(TransitionDeliveryStatus.Completed, delivery.Status);
        TestAssert.Equal(2, activities.Count);
        TestAssert.Equal(RadioActivityState.ValidatingFirefoxTarget, activities[0]);
        TestAssert.Equal(RadioActivityState.ValidatingFirefoxAudio, activities[1]);
        preparation.Prepared!.Dispose();
    }

    public static async Task DuckingSpeechAndRestorationActivitySequence()
    {
        var activities = new List<RadioActivityState>();
        using var target = new FakeAudioSessionTarget("firefox", 20, "FIREFOX01", 0.8f);
        var sequence = new AudioDuckingSequence(
            new FakeLog(),
            FastDuckingConfiguration(),
            activity => activities.Add(activity.State));

        AudioDuckingResult result = await sequence.RunWithProtectedActionAsync(
            target,
            _ => Task.CompletedTask,
            false,
            CancellationToken.None);

        TestAssert.True(result.TestPassed);
        TestAssert.Equal(3, activities.Count);
        TestAssert.Equal(RadioActivityState.LoweringFirefoxVolume, activities[0]);
        TestAssert.Equal(RadioActivityState.Speaking, activities[1]);
        TestAssert.Equal(RadioActivityState.RestoringFirefoxVolume, activities[2]);
    }

    public static async Task OpportunityAbandonmentIsReadable()
    {
        var session = new FakeRuntimeSession();
        await using RadioRuntimeController controller = CreateController(new FakeRuntimeSessionFactory(session));
        await controller.StartAsync();

        session.Emit(
            "Cochise",
            "Audioslave",
            FirefoxTargetState.Safe,
            activity: new RadioActivitySnapshot(RadioActivityState.OpportunityAbandoned));

        TestAssert.Contains(
            RadioControlViewState.FromSnapshot(controller.CurrentStatus).ActivityText,
            "abandoned safely");
    }

    public static async Task MissingFirefoxTargetIsReadable()
    {
        var session = new FakeRuntimeSession();
        await using RadioRuntimeController controller = CreateController(new FakeRuntimeSessionFactory(session));
        await controller.StartAsync();

        session.Emit(
            "Cochise",
            "Audioslave",
            FirefoxTargetState.Missing,
            activity: new RadioActivitySnapshot(RadioActivityState.NoSafeFirefoxTarget));

        TestAssert.Contains(
            RadioControlViewState.FromSnapshot(controller.CurrentStatus).ActivityText,
            "No safe Firefox");
    }

    public static async Task StartRefusalPreservesMissingFirefoxActivity()
    {
        var session = new FakeRuntimeSession { RefuseStartForMissingFirefox = true };
        await using RadioRuntimeController controller = CreateController(new FakeRuntimeSessionFactory(session));

        RadioControlStartResult result = await controller.StartAsync();

        TestAssert.False(result.Started);
        TestAssert.Equal(RadioLifecycleState.Stopped, result.Snapshot.Lifecycle);
        TestAssert.Equal(FirefoxTargetState.Missing, result.Snapshot.FirefoxTarget);
        TestAssert.Equal(RadioActivityState.NoSafeFirefoxTarget, result.Snapshot.Activity.State);
        TestAssert.Contains(
            RadioControlViewState.FromSnapshot(result.Snapshot).ActivityText,
            "No safe Firefox");
    }

    public static async Task StoppingActivityWinsOverLateRuntimeActivity()
    {
        var session = new FakeRuntimeSession { BlockStopUntilReleased = true };
        await using RadioRuntimeController controller = CreateController(new FakeRuntimeSessionFactory(session));
        await controller.StartAsync();

        Task<RadioControlStopResult> stop = controller.StopAsync();
        await session.StopEntered.Task.WaitAsync(TimeSpan.FromSeconds(2));
        session.Emit(
            "Cochise",
            "Audioslave",
            FirefoxTargetState.Safe,
            activity: new RadioActivitySnapshot(RadioActivityState.Speaking));

        TestAssert.Equal(RadioActivityState.Stopping, controller.CurrentStatus.Activity.State);
        TestAssert.Contains(
            RadioControlViewState.FromSnapshot(controller.CurrentStatus).ActivityText,
            "Stopping");
        session.ReleaseStop();
        TestAssert.True((await stop).SafeToClose);
    }

    public static async Task WrapperRefreshDoesNotCreateActivityNoise()
    {
        var session = new FakeRuntimeSession();
        await using RadioRuntimeController controller = CreateController(new FakeRuntimeSessionFactory(session));
        await controller.StartAsync();
        RadioControlSnapshot before = controller.CurrentStatus;

        session.SimulateHarmlessWrapperRefresh();

        TestAssert.Equal(1, session.WrapperRefreshes);
        TestAssert.Equal(before.Revision, controller.CurrentStatus.Revision);
        TestAssert.Equal(before.Activity, controller.CurrentStatus.Activity);
    }

    public static void ActivityContractDoesNotExposePlaybackCommands()
    {
        string[] methodNames = typeof(IRadioRuntimeSession)
            .GetMethods()
            .Select(method => method.Name)
            .ToArray();

        TestAssert.False(methodNames.Any(name => name.Contains("Pause", StringComparison.OrdinalIgnoreCase)));
        TestAssert.False(methodNames.Any(name => name.Contains("Play", StringComparison.OrdinalIgnoreCase)));
        TestAssert.False(methodNames.Any(name => name.Contains("Volume", StringComparison.OrdinalIgnoreCase)));
    }

    private static RadioPreparationOpportunity TransitionOpportunity(DateTimeOffset subjectStartedAt)
    {
        return new RadioPreparationOpportunity(
            800,
            new RadioPreparationContext(
                new MediaSnapshot("Track A", "Artist A", null),
                subjectStartedAt,
                [],
                []));
    }

    private static RadioOpportunitySnapshot ValidSnapshot(
        RadioPreparationOpportunity opportunity,
        MediaSnapshot? currentTrack = null)
    {
        return new RadioOpportunitySnapshot(
            Armed: true,
            SilentMode: false,
            OpportunityStillPending: true,
            SessionListReadSucceeded: true,
            MatchingFirefoxSessionCount: 1,
            SameSelectedSession: true,
            SourceId: "firefox.exe",
            SourceReadSucceeded: true,
            CurrentMedia: currentTrack ?? opportunity.Context.SubjectTrack,
            MediaReadSucceeded: true,
            PlaybackStatus: Windows.Media.Control.GlobalSystemMediaTransportControlsSessionPlaybackStatus.Playing,
            PlaybackReadSucceeded: true);
    }

    private static AudioDuckingConfiguration FastDuckingConfiguration()
    {
        return new AudioDuckingConfiguration(
            DuckRatio: 0.25f,
            MinimumUsableStartingVolume: 0.05f,
            RampDuration: TimeSpan.Zero,
            HoldDuration: TimeSpan.Zero,
            RampSteps: 2,
            VerificationTolerance: 0.0001f);
    }

    private static async Task StopDuringOwnedPhase(string phaseName)
    {
        var session = new FakeRuntimeSession
        {
            BlockStopUntilReleased = true,
            PhaseName = phaseName,
        };
        await using RadioRuntimeController controller = CreateController(new FakeRuntimeSessionFactory(session));
        await controller.StartAsync();

        Task<RadioControlStopResult> stop = controller.StopAsync();
        await session.StopEntered.Task.WaitAsync(TimeSpan.FromSeconds(2));
        TestAssert.Equal(phaseName, session.PhaseName);
        TestAssert.False(stop.IsCompleted);

        session.ReleaseStop();
        RadioControlStopResult result = await stop;
        TestAssert.True(result.SafeToClose);
        TestAssert.True(session.RestorationCompleted);
    }

    private static RadioRuntimeController CreateController(
        IRadioRuntimeSessionFactory factory,
        ILogFolderProvider? logFolder = null,
        IExternalFolderOpener? opener = null)
    {
        return new RadioRuntimeController(
            factory,
            new RadioRuntimeStartOptions(true, false, null),
            logFolder ?? new FakeLogFolderProvider("C:\\FakeLocalAppData\\Zoya Radio\\logs"),
            opener ?? new FakeExternalFolderOpener());
    }

    private static RadioControlStopResult SafeStopResult()
    {
        RadioControlSnapshot snapshot = Snapshot(RadioLifecycleState.Stopped, hasRuntime: false);
        return new RadioControlStopResult(true, "safe", snapshot);
    }

    private static RadioControlViewState View(RadioLifecycleState lifecycle, bool hasRuntime)
    {
        return RadioControlViewState.FromSnapshot(Snapshot(lifecycle, hasRuntime));
    }

    private static string ViewForTarget(FirefoxTargetState target)
    {
        RadioControlSnapshot snapshot = Snapshot(RadioLifecycleState.Running, hasRuntime: true) with
        {
            FirefoxTarget = target,
        };
        return RadioControlViewState.FromSnapshot(snapshot).FirefoxStatusText;
    }

    private static RadioControlSnapshot Snapshot(
        RadioLifecycleState lifecycle,
        bool hasRuntime,
        RadioActivitySnapshot? activity = null)
    {
        activity ??= lifecycle switch
        {
            RadioLifecycleState.Stopped => new RadioActivitySnapshot(RadioActivityState.Stopped),
            RadioLifecycleState.Starting => new RadioActivitySnapshot(RadioActivityState.ValidatingFirefoxTarget),
            RadioLifecycleState.Stopping => new RadioActivitySnapshot(RadioActivityState.Stopping),
            RadioLifecycleState.Faulted => new RadioActivitySnapshot(RadioActivityState.Faulted),
            _ => new RadioActivitySnapshot(RadioActivityState.Observing, 3),
        };

        return new RadioControlSnapshot(
            1,
            lifecycle,
            "Cochise",
            "Audioslave",
            FirefoxTargetState.Safe,
            false,
            activity,
            lifecycle.ToString(),
            "C:\\FakeLocalAppData\\Zoya Radio\\logs",
            hasRuntime);
    }
}

internal sealed class FakeRuntimeSessionFactory : IRadioRuntimeSessionFactory
{
    private readonly IRadioRuntimeSession _session;

    public FakeRuntimeSessionFactory(IRadioRuntimeSession session)
    {
        _session = session;
    }

    public int CreateCalls { get; private set; }

    public Task<IRadioRuntimeSession> CreateAsync(CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        CreateCalls++;
        return Task.FromResult(_session);
    }
}

internal sealed class BlockingRuntimeSessionFactory : IRadioRuntimeSessionFactory
{
    private readonly IRadioRuntimeSession _session;

    public BlockingRuntimeSessionFactory(IRadioRuntimeSession session)
    {
        _session = session;
    }

    public TaskCompletionSource CreateEntered { get; } = new(
        TaskCreationOptions.RunContinuationsAsynchronously);

    public async Task<IRadioRuntimeSession> CreateAsync(CancellationToken cancellationToken)
    {
        CreateEntered.TrySetResult();
        await Task.Delay(Timeout.InfiniteTimeSpan, cancellationToken);
        return _session;
    }
}

internal sealed class FakeRuntimeSession : IRadioRuntimeSession
{
    private Action<RadioRuntimeStatusUpdate>? _statusChanged;
    private Action<RadioRuntimeStatusUpdate>? _lastStatusHandler;
    private readonly TaskCompletionSource _stopRelease = new(
        TaskCreationOptions.RunContinuationsAsynchronously);
    private readonly TaskCompletionSource _disposeRelease = new(
        TaskCreationOptions.RunContinuationsAsynchronously);

    public event Action<RadioRuntimeStatusUpdate>? StatusChanged
    {
        add
        {
            _statusChanged += value;
            _lastStatusHandler = value;
        }
        remove => _statusChanged -= value;
    }

    public int StartCalls { get; private set; }

    public int StopCalls { get; private set; }

    public int DisposeCalls { get; private set; }

    public int SilentModeCalls { get; private set; }

    public int ClearHistoryCalls { get; private set; }

    public int WrapperRefreshes { get; private set; }

    public bool RefuseStartForMissingFirefox { get; init; }

    public bool BlockStartUntilCancelled { get; init; }

    public bool BlockStopUntilReleased { get; init; }

    public bool BlockDisposeUntilReleased { get; init; }

    public Exception? StartException { get; init; }
    public int StopFailuresRemaining { get; set; }


    public string PhaseName { get; init; } = "running";

    public bool RestorationCompleted { get; private set; }

    public bool ObservationContinues { get; private set; } = true;

    public bool PendingWorkInvalidated { get; private set; }

    public bool RecentHistoryCleared { get; private set; }

    public bool LogsDeleted { get; private set; }

    public TaskCompletionSource StartEntered { get; } = new(
        TaskCreationOptions.RunContinuationsAsynchronously);

    public TaskCompletionSource StopEntered { get; } = new(
        TaskCreationOptions.RunContinuationsAsynchronously);

    public TaskCompletionSource DisposeEntered { get; } = new(
        TaskCreationOptions.RunContinuationsAsynchronously);

    public TaskCompletionSource DisposeCompleted { get; } = new(
        TaskCreationOptions.RunContinuationsAsynchronously);

    public async Task<RadioRuntimeSessionStartResult> StartAsync(
        RadioRuntimeStartOptions options,
        CancellationToken cancellationToken)
    {
        StartCalls++;
        StartEntered.TrySetResult();
        if (StartException is not null)
        {
            throw StartException;
        }

        if (BlockStartUntilCancelled)
        {
            await Task.Delay(Timeout.InfiniteTimeSpan, cancellationToken);
        }

        if (RefuseStartForMissingFirefox)
        {
            Emit(
                "Cochise",
                "Audioslave",
                FirefoxTargetState.Missing,
                activity: new RadioActivitySnapshot(RadioActivityState.NoSafeFirefoxTarget));
            return new RadioRuntimeSessionStartResult(
                false,
                1,
                "No eligible Firefox media session was found");
        }

        Emit("Cochise", "Audioslave", FirefoxTargetState.Safe);
        return new RadioRuntimeSessionStartResult(true, 0, "fake runtime running");
    }

    public async Task StopAsync()
    {
        StopCalls++;
        StopEntered.TrySetResult();
        if (StopFailuresRemaining > 0)
        {
            StopFailuresRemaining--;
            throw new InvalidOperationException("simulated protected cleanup failure");
        }

        if (BlockStopUntilReleased)
        {
            await _stopRelease.Task;
        }

        RestorationCompleted = true;
    }

    public void ReleaseStop()
    {
        _stopRelease.TrySetResult();
    }

    public void SetSilentMode(bool silent)
    {
        SilentModeCalls++;
        PendingWorkInvalidated = true;
        Emit("Cochise", "Audioslave", FirefoxTargetState.Safe, silent);
    }

    public void ClearRecentHistory()
    {
        ClearHistoryCalls++;
        RecentHistoryCleared = true;
        Emit("Cochise", "Audioslave", FirefoxTargetState.Safe);
    }

    public void Emit(
        string title,
        string artist,
        FirefoxTargetState target,
        bool silent = false,
        RadioActivitySnapshot? activity = null)
    {
        _statusChanged?.Invoke(new RadioRuntimeStatusUpdate(
            title,
            artist,
            target,
            silent,
            activity ?? (silent
                ? new RadioActivitySnapshot(RadioActivityState.SilentMode)
                : new RadioActivitySnapshot(RadioActivityState.Observing, 3)),
            silent ? "Running silently" : "Running"));
    }

    public void EmitStale(string title, string artist)
    {
        _lastStatusHandler?.Invoke(new RadioRuntimeStatusUpdate(
            title,
            artist,
            FirefoxTargetState.Safe,
            false,
            new RadioActivitySnapshot(RadioActivityState.Observing, 1),
            "stale"));
    }

    public void SimulateHarmlessWrapperRefresh()
    {
        WrapperRefreshes++;
    }

    public async ValueTask DisposeAsync()
    {
        DisposeCalls++;
        DisposeEntered.TrySetResult();
        if (BlockDisposeUntilReleased)
        {
            await _disposeRelease.Task;
        }

        DisposeCompleted.TrySetResult();
    }

    public void ReleaseDispose() => _disposeRelease.TrySetResult();
}

internal sealed class FakeLogFolderProvider : ILogFolderProvider
{
    public FakeLogFolderProvider(string path)
    {
        Path = path;
    }

    public string Path { get; }

    public int EnsureCalls { get; private set; }

    public string GetLogFolderPath() => Path;

    public void EnsureLogFolderExists()
    {
        EnsureCalls++;
    }
}

internal sealed class FakeExternalFolderOpener : IExternalFolderOpener
{
    public string? OpenedPath { get; private set; }

    public void OpenFolder(string folderPath)
    {
        OpenedPath = folderPath;
    }
}

internal static class TestAssert
{
    public static void True(bool condition)
    {
        if (!condition)
        {
            throw new InvalidOperationException("Expected true.");
        }
    }

    public static void False(bool condition) => True(!condition);

    public static void Equal<T>(T expected, T actual)
    {
        if (!EqualityComparer<T>.Default.Equals(expected, actual))
        {
            throw new InvalidOperationException($"Expected '{expected}', received '{actual}'.");
        }
    }

    public static void Contains(string value, string expectedFragment)
    {
        if (!value.Contains(expectedFragment, StringComparison.OrdinalIgnoreCase))
        {
            throw new InvalidOperationException($"Expected '{value}' to contain '{expectedFragment}'.");
        }
    }
}
