using Windows.Media.Control;

namespace ZoyaRadio.MediaProbe;

internal sealed class MediaSessionWatcher : IAsyncDisposable
{
    private readonly GlobalSystemMediaTransportControlsSessionManager _manager;
    private readonly LocalLog _log;
    private readonly SemaphoreSlim _refreshGate = new(1, 1);
    private readonly Dictionary<GlobalSystemMediaTransportControlsSession, SessionObserver> _observers =
        new(ReferenceEqualityComparer.Instance);
    private bool _managerSubscribed;
    private bool _observeChanges;
    private volatile bool _disposed;

    public MediaSessionWatcher(GlobalSystemMediaTransportControlsSessionManager manager, LocalLog log)
    {
        _manager = manager;
        _log = log;
    }

    public async Task<bool> StartAsync(bool observeChanges)
    {
        _observeChanges = observeChanges;

        if (observeChanges)
        {
            try
            {
                _manager.SessionsChanged += OnSessionsChanged;
                _managerSubscribed = true;
            }
            catch (Exception exception)
            {
                _log.Warning("Could not observe Windows media-session list changes", exception);
                return false;
            }
        }

        return await RefreshSessionsAsync(initialSnapshot: true);
    }

    public async ValueTask DisposeAsync()
    {
        if (_disposed)
        {
            return;
        }

        _disposed = true;

        if (_managerSubscribed)
        {
            try
            {
                _manager.SessionsChanged -= OnSessionsChanged;
            }
            catch (Exception exception)
            {
                _log.Warning("Could not remove the Windows session-list observer", exception);
            }
        }

        await _refreshGate.WaitAsync();

        try
        {
            foreach (SessionObserver observer in _observers.Values)
            {
                await observer.DisposeAsync();
            }

            _observers.Clear();
        }
        finally
        {
            _refreshGate.Release();
        }
    }

    private async void OnSessionsChanged(
        GlobalSystemMediaTransportControlsSessionManager sender,
        SessionsChangedEventArgs args)
    {
        try
        {
            await RefreshSessionsAsync(initialSnapshot: false);
        }
        catch (Exception exception)
        {
            _log.Warning("Unexpected session-list event failure", exception);
        }
    }

    private async Task<bool> RefreshSessionsAsync(bool initialSnapshot)
    {
        await _refreshGate.WaitAsync();

        try
        {
            if (_disposed)
            {
                return false;
            }

            GlobalSystemMediaTransportControlsSession[] sessions;

            try
            {
                sessions = _manager.GetSessions().ToArray();
            }
            catch (Exception exception)
            {
                _log.Warning("Could not enumerate Windows media sessions", exception);
                return false;
            }

            var currentSessions = new HashSet<GlobalSystemMediaTransportControlsSession>(
                sessions,
                ReferenceEqualityComparer.Instance);

            if (initialSnapshot)
            {
                _log.Write($"Windows reports {sessions.Length} available media session(s).");
                if (sessions.Length == 0)
                {
                    _log.Write("No media sessions are currently available. This is not an error.");
                }
            }
            else
            {
                _log.Write($"SESSION LIST CHANGED: Windows now reports {sessions.Length} session(s).");
            }

            foreach ((GlobalSystemMediaTransportControlsSession session, SessionObserver observer) in
                _observers.Where(pair => !currentSessions.Contains(pair.Key)).ToArray())
            {
                _observers.Remove(session);
                await observer.DisposeAsync();
                _log.Write($"SESSION DISAPPEARED: {observer.SourceId}");
            }

            foreach (GlobalSystemMediaTransportControlsSession session in sessions)
            {
                if (_observers.ContainsKey(session))
                {
                    continue;
                }

                SessionObserver observer;

                try
                {
                    observer = new SessionObserver(session, _log);
                    _observers.Add(session, observer);
                }
                catch (Exception exception)
                {
                    _log.Warning("Could not prepare an observer for one Windows media session", exception);
                    continue;
                }

                if (!initialSnapshot)
                {
                    _log.Write($"SESSION APPEARED: {observer.SourceId}");
                }

                try
                {
                    await observer.StartAsync(_observeChanges);
                }
                catch (Exception exception)
                {
                    _log.Warning($"Could not initialize media session {observer.SourceId}", exception);
                }
            }

            return true;
        }
        finally
        {
            _refreshGate.Release();
        }
    }
}
