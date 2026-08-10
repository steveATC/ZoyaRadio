using Windows.Media.Control;

namespace ZoyaRadio.MediaProbe;

internal sealed record RadioSessionDetails(
    GlobalSystemMediaTransportControlsSession Session,
    string? SourceId,
    bool SourceReadSucceeded,
    MediaSnapshot Media,
    bool MediaReadSucceeded,
    GlobalSystemMediaTransportControlsSessionPlaybackStatus? PlaybackStatus,
    bool PlaybackReadSucceeded)
{
    public static RadioSessionDetails Unavailable { get; } = new(
        null!,
        null,
        false,
        MediaSnapshot.Empty,
        false,
        null,
        false);
}

internal sealed class RadioSessionAccess
{
    private readonly GlobalSystemMediaTransportControlsSessionManager _manager;
    private readonly IAnnouncementLog _log;

    public RadioSessionAccess(
        GlobalSystemMediaTransportControlsSessionManager manager,
        IAnnouncementLog log)
    {
        _manager = manager;
        _log = log;
    }

    public async Task<RadioSessionDetails[]> ReadAllSessionsAsync()
    {
        GlobalSystemMediaTransportControlsSession[] sessions = _manager.GetSessions().ToArray();
        _log.Write($"AVAILABLE WINDOWS MEDIA SESSIONS: {sessions.Length}");
        var details = new List<RadioSessionDetails>(sessions.Length);

        for (int index = 0; index < sessions.Length; index++)
        {
            RadioSessionDetails current = await ReadSessionDetailsAsync(sessions[index]);
            details.Add(current);
            WriteSessionDetails($"SESSION {index + 1}", current);
        }

        return details.ToArray();
    }

    public bool TryReadExactFirefoxMatches(
        out IReadOnlyList<GlobalSystemMediaTransportControlsSession> matches)
    {
        try
        {
            GlobalSystemMediaTransportControlsSession[] sessions = _manager.GetSessions().ToArray();
            matches = FirefoxSessionSelector.FindExactMatches(sessions, TryReadSourceIdWithoutLogging);
            return true;
        }
        catch (Exception exception)
        {
            _log.Warning("Could not read the Windows media-session list", exception);
            matches = [];
            return false;
        }
    }

    public async Task<RadioSessionDetails?> RecheckStartupTargetAsync(RadioSessionDetails original)
    {
        if (!TryReadExactFirefoxMatches(out IReadOnlyList<GlobalSystemMediaTransportControlsSession> matches)
            || matches.Count != 1)
        {
            return null;
        }

        RadioSessionDetails current = await ReadSessionDetailsAsync(matches[0]);
        return current.SourceReadSucceeded
            && string.Equals(
                current.SourceId,
                FirefoxSessionSelector.VerifiedSourceId,
                StringComparison.OrdinalIgnoreCase)
            && current.MediaReadSucceeded
            && current.PlaybackReadSucceeded
            && PlaybackControlRules.CanStart(current.PlaybackStatus)
            && AnnouncementText.HasSameTitleAndArtist(original.Media, current.Media)
                ? current
                : null;
    }

    public async Task<RadioSessionDetails> ReadSessionDetailsAsync(
        GlobalSystemMediaTransportControlsSession session)
    {
        string? sourceId = null;
        bool sourceReadSucceeded = false;
        MediaSnapshot media = MediaSnapshot.Empty;
        bool mediaReadSucceeded = false;

        try
        {
            sourceId = session.SourceAppUserModelId;
            sourceReadSucceeded = true;
        }
        catch (Exception exception)
        {
            _log.Warning("Could not read a session source identifier", exception);
        }

        MediaSnapshot? readMedia = await TryReadMediaAsync(session);
        if (readMedia is not null)
        {
            media = readMedia;
            mediaReadSucceeded = true;
        }

        PlaybackStatusRead playback = TryReadPlaybackStatus(session);
        return new RadioSessionDetails(
            session,
            sourceId,
            sourceReadSucceeded,
            media,
            mediaReadSucceeded,
            playback.Status,
            playback.Success);
    }

    public async Task<MediaSnapshot?> TryReadMediaAsync(
        GlobalSystemMediaTransportControlsSession session)
    {
        try
        {
            GlobalSystemMediaTransportControlsSessionMediaProperties? properties =
                await session.TryGetMediaPropertiesAsync();
            return properties is null
                ? null
                : MediaSnapshot.FromWindows(properties.Title, properties.Artist, properties.AlbumTitle);
        }
        catch (Exception exception)
        {
            _log.Warning("Could not read Firefox media properties", exception);
            return null;
        }
    }

    public void WriteSessionDetails(string heading, RadioSessionDetails details)
    {
        _log.Write(heading);
        _log.Write($"  SourceAppUserModelId: {MediaSnapshot.Display(details.SourceId)}");
        _log.Write($"  Title: {MediaSnapshot.Display(details.Media.Title)}");
        _log.Write($"  Artist: {MediaSnapshot.Display(details.Media.Artist)}");
        _log.Write($"  Album: {MediaSnapshot.Display(details.Media.Album)}");
        _log.Write($"  Playback: {DisplayStatus(details.PlaybackStatus)}");
    }

    public static string DisplayStatus(
        GlobalSystemMediaTransportControlsSessionPlaybackStatus? status)
    {
        return status?.ToString() ?? "<not provided>";
    }

    private PlaybackStatusRead TryReadPlaybackStatus(
        GlobalSystemMediaTransportControlsSession session)
    {
        try
        {
            GlobalSystemMediaTransportControlsSessionPlaybackInfo? info = session.GetPlaybackInfo();
            return new PlaybackStatusRead(info is not null, info?.PlaybackStatus);
        }
        catch (Exception exception)
        {
            _log.Warning("Could not read Firefox playback state", exception);
            return new PlaybackStatusRead(false, null);
        }
    }

    private static string? TryReadSourceIdWithoutLogging(
        GlobalSystemMediaTransportControlsSession session)
    {
        try
        {
            return session.SourceAppUserModelId;
        }
        catch
        {
            return null;
        }
    }
}
