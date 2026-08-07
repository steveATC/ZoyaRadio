using Windows.Media.Control;

namespace ZoyaRadio.MediaProbe;

internal sealed class SessionObserver : IAsyncDisposable
{
    private readonly GlobalSystemMediaTransportControlsSession _session;
    private readonly LocalLog _log;
    private readonly SemaphoreSlim _mediaGate = new(1, 1);
    private readonly SemaphoreSlim _playbackGate = new(1, 1);
    private MediaSnapshot? _lastMedia;
    private string? _lastPlayback;
    private bool _mediaSubscribed;
    private bool _playbackSubscribed;
    private volatile bool _disposed;

    public SessionObserver(GlobalSystemMediaTransportControlsSession session, LocalLog log)
    {
        _session = session;
        _log = log;
        SourceId = ReadSourceId();
    }

    public string SourceId { get; }

    public async Task StartAsync(bool observeChanges)
    {
        if (observeChanges)
        {
            SubscribeToChanges();
        }

        MediaSnapshot media = await ReadInitialMediaAsync();
        string? playback = ReadInitialPlayback();
        WriteMetadata("SESSION SNAPSHOT", media, playback);
    }

    public async ValueTask DisposeAsync()
    {
        if (_disposed)
        {
            return;
        }

        _disposed = true;

        if (_mediaSubscribed)
        {
            try
            {
                _session.MediaPropertiesChanged -= OnMediaPropertiesChanged;
            }
            catch (Exception exception)
            {
                _log.Warning($"Could not remove the media-property observer for {SourceId}", exception);
            }
        }

        if (_playbackSubscribed)
        {
            try
            {
                _session.PlaybackInfoChanged -= OnPlaybackInfoChanged;
            }
            catch (Exception exception)
            {
                _log.Warning($"Could not remove the playback observer for {SourceId}", exception);
            }
        }

        await _mediaGate.WaitAsync();
        _mediaGate.Release();
        await _playbackGate.WaitAsync();
        _playbackGate.Release();
    }

    private string ReadSourceId()
    {
        try
        {
            return string.IsNullOrWhiteSpace(_session.SourceAppUserModelId)
                ? "<not provided>"
                : _session.SourceAppUserModelId;
        }
        catch (Exception exception)
        {
            _log.Warning("Could not read a media session's source application identifier", exception);
            return "<not provided>";
        }
    }

    private void SubscribeToChanges()
    {
        try
        {
            _session.MediaPropertiesChanged += OnMediaPropertiesChanged;
            _mediaSubscribed = true;
        }
        catch (Exception exception)
        {
            _log.Warning($"Could not observe media-property changes for {SourceId}", exception);
        }

        try
        {
            _session.PlaybackInfoChanged += OnPlaybackInfoChanged;
            _playbackSubscribed = true;
        }
        catch (Exception exception)
        {
            _log.Warning($"Could not observe playback changes for {SourceId}", exception);
        }
    }

    private async Task<MediaSnapshot> ReadInitialMediaAsync()
    {
        await _mediaGate.WaitAsync();

        try
        {
            MediaSnapshot? current = await TryReadMediaAsync();
            _lastMedia = current;
            return current ?? MediaSnapshot.Empty;
        }
        finally
        {
            _mediaGate.Release();
        }
    }

    private string? ReadInitialPlayback()
    {
        _playbackGate.Wait();

        try
        {
            (bool success, string? current) = TryReadPlayback();
            if (success)
            {
                _lastPlayback = current;
            }

            return current;
        }
        finally
        {
            _playbackGate.Release();
        }
    }

    private async void OnMediaPropertiesChanged(
        GlobalSystemMediaTransportControlsSession sender,
        MediaPropertiesChangedEventArgs args)
    {
        try
        {
            await RefreshMediaAsync();
        }
        catch (Exception exception)
        {
            _log.Warning($"Unexpected media-property event failure for {SourceId}", exception);
        }
    }

    private async Task RefreshMediaAsync()
    {
        await _mediaGate.WaitAsync();

        try
        {
            if (_disposed)
            {
                return;
            }

            MediaSnapshot? current = await TryReadMediaAsync();
            if (current is null)
            {
                return;
            }

            MediaSnapshot? previous = _lastMedia;
            _lastMedia = current;

            if (previous is null)
            {
                WriteMetadata("MEDIA PROPERTIES NOW AVAILABLE", current, _lastPlayback);
            }
            else if (current != previous)
            {
                WriteMetadata("TRACK CHANGED", current, _lastPlayback);
            }
            else
            {
                _log.Write($"MEDIA PROPERTIES EVENT: metadata unchanged for {SourceId}");
            }
        }
        finally
        {
            _mediaGate.Release();
        }
    }

    private async Task<MediaSnapshot?> TryReadMediaAsync()
    {
        try
        {
            GlobalSystemMediaTransportControlsSessionMediaProperties? properties =
                await _session.TryGetMediaPropertiesAsync();

            if (properties is null)
            {
                _log.Write($"WARNING: Windows returned no media properties for {SourceId}.");
                return MediaSnapshot.Empty;
            }

            return MediaSnapshot.FromWindows(properties.Title, properties.Artist, properties.AlbumTitle);
        }
        catch (Exception exception)
        {
            _log.Warning($"Could not read media properties for {SourceId}", exception);
            return null;
        }
    }

    private async void OnPlaybackInfoChanged(
        GlobalSystemMediaTransportControlsSession sender,
        PlaybackInfoChangedEventArgs args)
    {
        try
        {
            await RefreshPlaybackAsync();
        }
        catch (Exception exception)
        {
            _log.Warning($"Unexpected playback event failure for {SourceId}", exception);
        }
    }

    private async Task RefreshPlaybackAsync()
    {
        await _playbackGate.WaitAsync();

        try
        {
            if (_disposed)
            {
                return;
            }

            (bool success, string? current) = TryReadPlayback();
            if (!success)
            {
                return;
            }

            string? previous = _lastPlayback;
            _lastPlayback = current;

            if (!string.Equals(previous, current, StringComparison.Ordinal))
            {
                _log.Write(
                    $"PLAYBACK: {MediaSnapshot.Display(previous)} -> {MediaSnapshot.Display(current)} " +
                    $"(source: {SourceId})");
            }
        }
        finally
        {
            _playbackGate.Release();
        }
    }

    private (bool Success, string? Status) TryReadPlayback()
    {
        try
        {
            GlobalSystemMediaTransportControlsSessionPlaybackInfo? playbackInfo = _session.GetPlaybackInfo();
            return (true, playbackInfo?.PlaybackStatus.ToString());
        }
        catch (Exception exception)
        {
            _log.Warning($"Could not read playback state for {SourceId}", exception);
            return (false, null);
        }
    }

    private void WriteMetadata(string heading, MediaSnapshot media, string? playback)
    {
        _log.Write(heading);
        _log.Write($"  SourceAppUserModelId: {SourceId}");

        if (LooksLikeFirefox(SourceId))
        {
            _log.Write("  Application hint: Firefox (inferred from the source identifier)");
        }

        _log.Write($"  Title: {MediaSnapshot.Display(media.Title)}");
        _log.Write($"  Artist: {MediaSnapshot.Display(media.Artist)}");
        _log.Write($"  Album: {MediaSnapshot.Display(media.Album)}");
        _log.Write($"  Playback: {MediaSnapshot.Display(playback)}");
    }

    private static bool LooksLikeFirefox(string sourceId)
    {
        return sourceId.Contains("firefox", StringComparison.OrdinalIgnoreCase)
            || sourceId.Contains("mozilla", StringComparison.OrdinalIgnoreCase);
    }
}
