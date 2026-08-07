using Windows.Media.Control;

namespace ZoyaRadio.MediaProbe;

internal sealed class WindowsAnnouncementPlaybackTarget : IAnnouncementPlaybackTarget
{
    private readonly GlobalSystemMediaTransportControlsSession _session;
    private readonly IAnnouncementLog _log;

    public WindowsAnnouncementPlaybackTarget(
        GlobalSystemMediaTransportControlsSession session,
        IAnnouncementLog log)
    {
        _session = session;
        _log = log;
    }

    public string SourceId => FirefoxSessionSelector.VerifiedSourceId;

    public async Task<bool> RequestPauseAsync()
    {
        return await _session.TryPauseAsync();
    }

    public async Task<bool> RequestPlayAsync()
    {
        return await _session.TryPlayAsync();
    }

    public PlaybackStatusRead ReadPlaybackStatus()
    {
        try
        {
            GlobalSystemMediaTransportControlsSessionPlaybackInfo? info = _session.GetPlaybackInfo();
            return new PlaybackStatusRead(info is not null, info?.PlaybackStatus);
        }
        catch (Exception exception)
        {
            _log.Warning("Could not read playback state for the selected Firefox session", exception);
            return new PlaybackStatusRead(false, null);
        }
    }
}
