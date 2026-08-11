using System.Diagnostics;

namespace ZoyaRadio.MediaProbe;

public enum RadioLifecycleState
{
    Stopped,
    Starting,
    Running,
    Stopping,
    Faulted,
}

public enum FirefoxTargetState
{
    NotChecked,
    Safe,
    Missing,
    Ambiguous,
    Unavailable,
}
public enum RadioActivityState
{
    Stopped,
    Observing,
    WaitingForTrackStability,
    GeneratingComment,
    PreparingSpeech,
    PreparedForTransition,
    ValidatingFirefoxTarget,
    ValidatingFirefoxAudio,
    LoweringFirefoxVolume,
    Speaking,
    RestoringFirefoxVolume,
    SilentMode,
    OpportunityAbandoned,
    NoSafeFirefoxTarget,
    Stopping,
    Faulted,
}

public sealed record RadioActivitySnapshot(
    RadioActivityState State,
    int? TransitionsRemaining = null,
    string? Detail = null);

public sealed record RadioControlSnapshot(
    long Revision,
    RadioLifecycleState Lifecycle,
    string? Title,
    string? Artist,
    FirefoxTargetState FirefoxTarget,
    bool SilentMode,
    RadioActivitySnapshot Activity,
    string StatusText,
    string LogFolderPath,
    bool HasActiveRuntime)
{
    public bool CleanupInProgress => Lifecycle == RadioLifecycleState.Stopping;
}

public sealed record RadioControlStartResult(
    bool Started,
    int SuggestedExitCode,
    RadioControlSnapshot Snapshot);

public sealed record RadioControlStopResult(
    bool SafeToClose,
    string Message,
    RadioControlSnapshot Snapshot);

public sealed record RadioControlViewState(
    bool StartEnabled,
    bool StopEnabled,
    bool SilentModeEnabled,
    bool ClearHistoryEnabled,
    bool OpenLogsEnabled,
    bool SilentModeChecked,
    bool CleanupVisible,
    string TitleText,
    string ArtistText,
    string ActivityText,
    string StatusText,
    string FirefoxStatusText)
{
    public static RadioControlViewState FromSnapshot(RadioControlSnapshot snapshot)
    {
        bool running = snapshot.Lifecycle == RadioLifecycleState.Running;
        bool starting = snapshot.Lifecycle == RadioLifecycleState.Starting;
        bool stopping = snapshot.Lifecycle == RadioLifecycleState.Stopping;
        bool canStop = snapshot.HasActiveRuntime || starting || running || stopping;

        return new RadioControlViewState(
            StartEnabled: !snapshot.HasActiveRuntime
                && snapshot.Lifecycle is RadioLifecycleState.Stopped or RadioLifecycleState.Faulted,
            StopEnabled: canStop && !stopping,
            SilentModeEnabled: running,
            ClearHistoryEnabled: running,
            OpenLogsEnabled: !stopping,
            SilentModeChecked: snapshot.SilentMode,
            CleanupVisible: stopping,
            TitleText: Display(snapshot.Title),
            ArtistText: Display(snapshot.Artist),
            ActivityText: DescribeActivity(snapshot.Activity),
            StatusText: snapshot.StatusText,
            FirefoxStatusText: DescribeFirefox(snapshot.FirefoxTarget));
    }

    private static string Display(string? value)
    {
        return string.IsNullOrWhiteSpace(value) ? "<not provided>" : value;
    }

    private static string DescribeFirefox(FirefoxTargetState target)
    {
        return target switch
        {
            FirefoxTargetState.Safe => "Ready: exactly one eligible Playing Firefox session",
            FirefoxTargetState.Missing => "Not found: start YouTube Music playing in Firefox",
            FirefoxTargetState.Ambiguous => "Ambiguous: more than one Firefox media session",
            FirefoxTargetState.Unavailable => "Unavailable: Windows could not validate Firefox",
            _ => "Not checked",
        };
    }

    private static string DescribeActivity(RadioActivitySnapshot activity)
    {
        return activity.State switch
        {
            RadioActivityState.Stopped => "Stopped",
            RadioActivityState.Observing when activity.TransitionsRemaining == 2 =>
                "Observing - preparation begins on the next transition",
            RadioActivityState.Observing when activity.TransitionsRemaining == 1 =>
                "Observing - next comment on the next transition",
            RadioActivityState.Observing when activity.TransitionsRemaining > 0 =>
                $"Observing - next comment in {activity.TransitionsRemaining} transitions",
            RadioActivityState.Observing => "Observing normally",
            RadioActivityState.WaitingForTrackStability => "Waiting for the current track to become stable",
            RadioActivityState.GeneratingComment => "Generating a local comment",
            RadioActivityState.PreparingSpeech => "Preparing Nicole speech",
            RadioActivityState.PreparedForTransition => "Ready for the next track transition",
            RadioActivityState.ValidatingFirefoxTarget => "Validating the Firefox media target",
            RadioActivityState.ValidatingFirefoxAudio => "Validating Firefox audio",
            RadioActivityState.LoweringFirefoxVolume => "Lowering Firefox volume",
            RadioActivityState.Speaking => "Speaking",
            RadioActivityState.RestoringFirefoxVolume => "Restoring Firefox volume",
            RadioActivityState.SilentMode => "Silent Mode - observing without announcements",
            RadioActivityState.OpportunityAbandoned =>
                "Opportunity abandoned safely - observing for a fresh opportunity",
            RadioActivityState.NoSafeFirefoxTarget => "No safe Firefox media target",
            RadioActivityState.Stopping => "Stopping and restoring audio",
            RadioActivityState.Faulted => string.IsNullOrWhiteSpace(activity.Detail)
                ? "Faulted - check the status message"
                : $"Faulted - {activity.Detail}",
            _ => "Activity unavailable",
        };
    }
}

public sealed class RadioWindowCloseCoordinator
{
    private int _state;

    public bool CloseAllowed => Volatile.Read(ref _state) == 2;

    public bool ShutdownInProgress => Volatile.Read(ref _state) == 1;

    public async Task<bool> RequestCloseAsync(Func<Task<RadioControlStopResult>> stopAsync)
    {
        ArgumentNullException.ThrowIfNull(stopAsync);

        if (CloseAllowed)
        {
            return true;
        }

        if (Interlocked.CompareExchange(ref _state, 1, 0) != 0)
        {
            return false;
        }

        try
        {
            RadioControlStopResult result = await stopAsync();
            if (!result.SafeToClose)
            {
                Volatile.Write(ref _state, 0);
                return false;
            }

            Volatile.Write(ref _state, 2);
            return true;
        }
        catch
        {
            Volatile.Write(ref _state, 0);
            throw;
        }
    }
}

internal sealed record RadioRuntimeTarget(string SourceId, string Title, string Artist);

internal sealed record RadioRuntimeStartOptions(
    bool AiEnabled,
    bool SimulateTransitionFailure,
    Func<RadioRuntimeTarget, CancellationToken, Task<bool>>? ConfirmAsync);

internal sealed record RadioRuntimeSessionStartResult(
    bool Started,
    int SuggestedExitCode,
    string StatusText);

internal sealed record RadioRuntimeStatusUpdate(
    string? Title,
    string? Artist,
    FirefoxTargetState FirefoxTarget,
    bool SilentMode,
    RadioActivitySnapshot Activity,
    string StatusText);

internal interface IRadioRuntimeSession : IAsyncDisposable
{
    event Action<RadioRuntimeStatusUpdate>? StatusChanged;

    Task<RadioRuntimeSessionStartResult> StartAsync(
        RadioRuntimeStartOptions options,
        CancellationToken cancellationToken);

    Task StopAsync();

    void SetSilentMode(bool silent);

    void ClearRecentHistory();
}

internal interface IRadioRuntimeSessionFactory
{
    Task<IRadioRuntimeSession> CreateAsync(CancellationToken cancellationToken);
}

internal interface ILogFolderProvider
{
    string GetLogFolderPath();

    void EnsureLogFolderExists();
}

internal interface IExternalFolderOpener
{
    void OpenFolder(string folderPath);
}

internal sealed class LocalAppDataLogFolderProvider : ILogFolderProvider
{
    public const string ApplicationFolderName = "Zoya Radio";
    public const string LogsFolderName = "logs";

    public string GetLogFolderPath()
    {
        string localAppData = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
        return Resolve(localAppData);
    }

    public void EnsureLogFolderExists()
    {
        Directory.CreateDirectory(GetLogFolderPath());
    }

    internal static string Resolve(string localAppDataRoot)
    {
        if (string.IsNullOrWhiteSpace(localAppDataRoot))
        {
            throw new InvalidOperationException("Windows did not provide a usable LocalAppData folder.");
        }

        return Path.Combine(localAppDataRoot, ApplicationFolderName, LogsFolderName);
    }
}

internal sealed class FixedLogFolderProvider : ILogFolderProvider
{
    private readonly string _folderPath;

    public FixedLogFolderProvider(string folderPath)
    {
        _folderPath = Path.GetFullPath(folderPath);
    }

    public string GetLogFolderPath() => _folderPath;

    public void EnsureLogFolderExists()
    {
        Directory.CreateDirectory(_folderPath);
    }
}

internal sealed class WindowsExternalFolderOpener : IExternalFolderOpener
{
    public void OpenFolder(string folderPath)
    {
        Process.Start(new ProcessStartInfo
        {
            FileName = folderPath,
            UseShellExecute = true,
        });
    }
}

internal sealed class SingleRadioRuntimeSessionFactory : IRadioRuntimeSessionFactory
{
    private IRadioRuntimeSession? _session;

    public SingleRadioRuntimeSessionFactory(IRadioRuntimeSession session)
    {
        _session = session;
    }

    public Task<IRadioRuntimeSession> CreateAsync(CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        IRadioRuntimeSession session = Interlocked.Exchange(ref _session, null)
            ?? throw new InvalidOperationException("This radio runtime session has already been used.");
        return Task.FromResult(session);
    }
}

public sealed class RadioRuntimeController : IAsyncDisposable
{
    private readonly object _sync = new();
    private readonly IRadioRuntimeSessionFactory _sessionFactory;
    private readonly RadioRuntimeStartOptions _startOptions;
    private readonly ILogFolderProvider _logFolderProvider;
    private readonly IExternalFolderOpener _folderOpener;
    private RadioControlSnapshot _snapshot;
    private IRadioRuntimeSession? _session;
    private Action<RadioRuntimeStatusUpdate>? _sessionStatusHandler;
    private CancellationTokenSource? _lifetimeCancellation;
    private Task<RadioControlStartResult>? _startTask;
    private Task<RadioControlStopResult>? _stopTask;
    private long _generation;
    private bool _disposed;

    internal RadioRuntimeController(
        IRadioRuntimeSessionFactory sessionFactory,
        RadioRuntimeStartOptions startOptions,
        ILogFolderProvider logFolderProvider,
        IExternalFolderOpener folderOpener)
    {
        _sessionFactory = sessionFactory;
        _startOptions = startOptions;
        _logFolderProvider = logFolderProvider;
        _folderOpener = folderOpener;
        _snapshot = new RadioControlSnapshot(
            0,
            RadioLifecycleState.Stopped,
            null,
            null,
            FirefoxTargetState.NotChecked,
            false,
            new RadioActivitySnapshot(RadioActivityState.Stopped),
            "Stopped",
            logFolderProvider.GetLogFolderPath(),
            false);
    }

    public event Action<RadioControlSnapshot>? StatusChanged;

    public RadioControlSnapshot CurrentStatus
    {
        get
        {
            lock (_sync)
            {
                return _snapshot;
            }
        }
    }

    public string LogFolderPath => _logFolderProvider.GetLogFolderPath();

    public static RadioRuntimeController CreateWindowsDefault()
    {
        var logFolderProvider = new LocalAppDataLogFolderProvider();
        return new RadioRuntimeController(
            new WindowsRadioRuntimeSessionFactory(logFolderProvider),
            new RadioRuntimeStartOptions(
                AiEnabled: true,
                SimulateTransitionFailure: false,
                ConfirmAsync: null),
            logFolderProvider,
            new WindowsExternalFolderOpener());
    }

    public Task<RadioControlStartResult> StartAsync(CancellationToken cancellationToken = default)
    {
        Task<RadioControlStopResult>? stoppingTask;
        TaskCompletionSource<RadioControlStartResult>? completion = null;
        CancellationTokenSource? lifetime = null;
        RadioControlSnapshot? changed = null;
        long generation = 0;

        lock (_sync)
        {
            ObjectDisposedException.ThrowIf(_disposed, this);

            if (_snapshot.Lifecycle == RadioLifecycleState.Running)
            {
                return Task.FromResult(new RadioControlStartResult(true, 0, _snapshot));
            }

            if (_snapshot.Lifecycle == RadioLifecycleState.Starting && _startTask is not null)
            {
                return _startTask;
            }

            if (_snapshot.Lifecycle == RadioLifecycleState.Stopping && _stopTask is not null)
            {
                stoppingTask = _stopTask;
            }
            else
            {
                stoppingTask = null;
            }

            if (stoppingTask is null)
            {
                if (_snapshot.HasActiveRuntime)
                {
                    return Task.FromResult(new RadioControlStartResult(false, 1, _snapshot));
                }

                lifetime = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
                _lifetimeCancellation = lifetime;
                generation = ++_generation;
                completion = new TaskCompletionSource<RadioControlStartResult>(
                    TaskCreationOptions.RunContinuationsAsynchronously);
                _startTask = completion.Task;
                changed = SetSnapshotLocked(
                    RadioLifecycleState.Starting,
                    null,
                    null,
                    FirefoxTargetState.NotChecked,
                    false,
                    new RadioActivitySnapshot(RadioActivityState.ValidatingFirefoxTarget),
                    "Starting Zoya Radio...",
                    hasActiveRuntime: false);
            }
        }

        if (stoppingTask is not null)
        {
            return StartAfterStopAsync(stoppingTask, cancellationToken);
        }

        Publish(changed!);
        _ = CompleteStartAsync(generation, lifetime!, completion!);
        return completion!.Task;
    }

    public Task<RadioControlStopResult> StopAsync()
    {
        TaskCompletionSource<RadioControlStopResult>? completion = null;
        Task<RadioControlStartResult>? startTask = null;
        RadioControlSnapshot? changed = null;
        long generation = 0;

        lock (_sync)
        {
            if (_snapshot.Lifecycle == RadioLifecycleState.Stopping && _stopTask is not null)
            {
                return _stopTask;
            }

            if (!_snapshot.HasActiveRuntime
                && _snapshot.Lifecycle is RadioLifecycleState.Stopped or RadioLifecycleState.Faulted)
            {
                if (_snapshot.Lifecycle == RadioLifecycleState.Faulted)
                {
                    changed = SetSnapshotLocked(
                        RadioLifecycleState.Stopped,
                        null,
                        null,
                        FirefoxTargetState.NotChecked,
                        false,
                        new RadioActivitySnapshot(RadioActivityState.Stopped),
                        "Stopped",
                        hasActiveRuntime: false);
                }

                RadioControlSnapshot resultSnapshot = changed ?? _snapshot;
                if (changed is not null)
                {
                    PublishAfterLock(changed);
                }

                return Task.FromResult(new RadioControlStopResult(true, "Already stopped", resultSnapshot));
            }

            generation = _generation;
            _lifetimeCancellation?.Cancel();
            startTask = _startTask;
            completion = new TaskCompletionSource<RadioControlStopResult>(
                TaskCreationOptions.RunContinuationsAsynchronously);
            _stopTask = completion.Task;
            changed = SetSnapshotLocked(
                RadioLifecycleState.Stopping,
                _snapshot.Title,
                _snapshot.Artist,
                _snapshot.FirefoxTarget,
                _snapshot.SilentMode,
                new RadioActivitySnapshot(RadioActivityState.Stopping),
                "Stopping Zoya Radio and restoring protected audio state...",
                hasActiveRuntime: true);
        }

        Publish(changed);
        _ = CompleteStopAsync(generation, startTask, completion!);
        return completion!.Task;
    }

    public void SetSilentMode(bool silent)
    {
        IRadioRuntimeSession? session;

        lock (_sync)
        {
            session = _snapshot.Lifecycle == RadioLifecycleState.Running ? _session : null;
        }

        session?.SetSilentMode(silent);
    }

    public void ClearRecentHistory()
    {
        IRadioRuntimeSession? session;

        lock (_sync)
        {
            session = _snapshot.Lifecycle == RadioLifecycleState.Running ? _session : null;
        }

        session?.ClearRecentHistory();
    }

    public void OpenLogsFolder()
    {
        _logFolderProvider.EnsureLogFolderExists();
        _folderOpener.OpenFolder(_logFolderProvider.GetLogFolderPath());
    }

    public async ValueTask DisposeAsync()
    {
        RadioControlStopResult result = await StopAsync();
        if (!result.SafeToClose)
        {
            return;
        }

        lock (_sync)
        {
            _disposed = true;
            _lifetimeCancellation?.Dispose();
            _lifetimeCancellation = null;
        }
    }

    private async Task<RadioControlStartResult> StartAfterStopAsync(
        Task<RadioControlStopResult> stoppingTask,
        CancellationToken cancellationToken)
    {
        RadioControlStopResult stopped = await stoppingTask;
        if (!stopped.SafeToClose)
        {
            return new RadioControlStartResult(false, 1, stopped.Snapshot);
        }

        return await StartAsync(cancellationToken);
    }

    private async Task CompleteStartAsync(
        long generation,
        CancellationTokenSource lifetime,
        TaskCompletionSource<RadioControlStartResult> completion)
    {
        IRadioRuntimeSession? session = null;
        RadioRuntimeSessionStartResult? result = null;
        Exception? failure = null;

        try
        {
            session = await _sessionFactory.CreateAsync(lifetime.Token);
            Action<RadioRuntimeStatusUpdate> handler = update => HandleSessionStatus(generation, session, update);

            lock (_sync)
            {
                if (generation != _generation)
                {
                    throw new OperationCanceledException(lifetime.Token);
                }

                _session = session;
                _sessionStatusHandler = handler;
                session.StatusChanged += handler;
                _snapshot = _snapshot with { HasActiveRuntime = true };
            }

            result = await session.StartAsync(_startOptions, lifetime.Token);
        }
        catch (OperationCanceledException) when (lifetime.IsCancellationRequested)
        {
            result = new RadioRuntimeSessionStartResult(false, 0, "Start cancelled");
        }
        catch (Exception exception)
        {
            failure = exception;
            result = new RadioRuntimeSessionStartResult(false, 1, $"Start failed: {exception.Message}");
        }

        bool stopping;
        lock (_sync)
        {
            stopping = generation == _generation
                && _snapshot.Lifecycle == RadioLifecycleState.Stopping;
        }

        if (!result.Started && !stopping && session is not null)
        {
            Exception? cleanupFailure = await TryReleaseSessionAsync(generation, session);
            failure ??= cleanupFailure;
        }

        RadioControlSnapshot snapshot;
        lock (_sync)
        {
            if (generation != _generation || _snapshot.Lifecycle == RadioLifecycleState.Stopping)
            {
                snapshot = _snapshot;
            }
            else if (result.Started)
            {
                snapshot = SetSnapshotLocked(
                    RadioLifecycleState.Running,
                    _snapshot.Title,
                    _snapshot.Artist,
                    _snapshot.FirefoxTarget,
                    _snapshot.SilentMode,
                    _snapshot.Activity,
                    result.StatusText,
                    hasActiveRuntime: true);
            }
            else
            {
                bool active = session is not null && ReferenceEquals(_session, session);
                RadioActivitySnapshot failedActivity = failure is not null
                    ? new RadioActivitySnapshot(RadioActivityState.Faulted, Detail: result.StatusText)
                    : _snapshot.Activity.State == RadioActivityState.NoSafeFirefoxTarget
                        ? _snapshot.Activity
                        : new RadioActivitySnapshot(RadioActivityState.Stopped);
                snapshot = SetSnapshotLocked(
                    failure is null ? RadioLifecycleState.Stopped : RadioLifecycleState.Faulted,
                    _snapshot.Title,
                    _snapshot.Artist,
                    _snapshot.FirefoxTarget,
                    _snapshot.SilentMode,
                    failedActivity,
                    failure is null ? result.StatusText : $"{result.StatusText}. Cleanup requires attention.",
                    active);
            }

            _startTask = null;
        }

        Publish(snapshot);
        completion.TrySetResult(new RadioControlStartResult(result.Started, result.SuggestedExitCode, snapshot));
    }

    private async Task CompleteStopAsync(
        long generation,
        Task<RadioControlStartResult>? startTask,
        TaskCompletionSource<RadioControlStopResult> completion)
    {
        if (startTask is not null)
        {
            await startTask;
        }

        IRadioRuntimeSession? session;
        lock (_sync)
        {
            session = _session;
        }

        Exception? failure = session is null ? null : await TryReleaseSessionAsync(generation, session);
        RadioControlSnapshot snapshot;

        lock (_sync)
        {
            if (failure is null)
            {
                _generation++;
                _lifetimeCancellation?.Dispose();
                _lifetimeCancellation = null;
                snapshot = SetSnapshotLocked(
                    RadioLifecycleState.Stopped,
                    null,
                    null,
                    FirefoxTargetState.NotChecked,
                    false,
                    new RadioActivitySnapshot(RadioActivityState.Stopped),
                    "Stopped safely",
                    hasActiveRuntime: false);
            }
            else
            {
                snapshot = SetSnapshotLocked(
                    RadioLifecycleState.Faulted,
                    _snapshot.Title,
                    _snapshot.Artist,
                    _snapshot.FirefoxTarget,
                    _snapshot.SilentMode,
                    new RadioActivitySnapshot(RadioActivityState.Faulted, Detail: failure.Message),
                    $"STOP FAILED: protected cleanup could not be confirmed. {failure.Message}",
                    hasActiveRuntime: true);
            }

            _stopTask = null;
        }

        Publish(snapshot);
        completion.TrySetResult(new RadioControlStopResult(
            failure is null,
            failure is null ? "Protected cleanup completed" : snapshot.StatusText,
            snapshot));
    }

    private async Task<Exception?> TryReleaseSessionAsync(long generation, IRadioRuntimeSession session)
    {
        try
        {
            await session.StopAsync();
            await session.DisposeAsync();

            lock (_sync)
            {
                if (generation == _generation && ReferenceEquals(_session, session))
                {
                    if (_sessionStatusHandler is not null)
                    {
                        session.StatusChanged -= _sessionStatusHandler;
                    }

                    _sessionStatusHandler = null;
                    _session = null;
                }
            }

            return null;
        }
        catch (Exception exception)
        {
            return exception;
        }
    }

    private void HandleSessionStatus(
        long generation,
        IRadioRuntimeSession session,
        RadioRuntimeStatusUpdate update)
    {
        RadioControlSnapshot? changed = null;

        lock (_sync)
        {
            if (generation != _generation
                || !ReferenceEquals(session, _session)
                || _snapshot.Lifecycle is RadioLifecycleState.Stopped or RadioLifecycleState.Faulted)
            {
                return;
            }

            changed = SetSnapshotLocked(
                _snapshot.Lifecycle,
                update.Title,
                update.Artist,
                update.FirefoxTarget,
                update.SilentMode,
                _snapshot.Lifecycle == RadioLifecycleState.Stopping
                    ? new RadioActivitySnapshot(RadioActivityState.Stopping)
                    : update.Activity,
                _snapshot.Lifecycle == RadioLifecycleState.Stopping
                    ? "Stopping Zoya Radio and restoring protected audio state..."
                    : update.StatusText,
                hasActiveRuntime: true);
        }

        Publish(changed);
    }

    private RadioControlSnapshot SetSnapshotLocked(
        RadioLifecycleState lifecycle,
        string? title,
        string? artist,
        FirefoxTargetState firefoxTarget,
        bool silentMode,
        RadioActivitySnapshot activity,
        string statusText,
        bool hasActiveRuntime)
    {
        _snapshot = new RadioControlSnapshot(
            _snapshot.Revision + 1,
            lifecycle,
            title,
            artist,
            firefoxTarget,
            silentMode,
            activity,
            statusText,
            _logFolderProvider.GetLogFolderPath(),
            hasActiveRuntime);
        return _snapshot;
    }

    private void Publish(RadioControlSnapshot snapshot)
    {
        Delegate[] subscribers = StatusChanged?.GetInvocationList() ?? [];
        foreach (Delegate subscriber in subscribers)
        {
            try
            {
                ((Action<RadioControlSnapshot>)subscriber)(snapshot);
            }
            catch
            {
                // A stale or faulty presentation listener must not interrupt runtime cleanup.
            }
        }
    }

    private void PublishAfterLock(RadioControlSnapshot snapshot)
    {
        ThreadPool.QueueUserWorkItem(_ => Publish(snapshot));
    }
}
