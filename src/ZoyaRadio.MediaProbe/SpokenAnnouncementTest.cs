using Windows.Media.Control;

namespace ZoyaRadio.MediaProbe;

internal sealed class SpokenAnnouncementTest
{
    private readonly GlobalSystemMediaTransportControlsSessionManager _manager;
    private readonly LocalLog _log;
    private readonly IAnnouncementSpeechService _speechService;

    public SpokenAnnouncementTest(
        GlobalSystemMediaTransportControlsSessionManager manager,
        LocalLog log,
        IAnnouncementSpeechService speechService)
    {
        _manager = manager;
        _log = log;
        _speechService = speechService;
    }

    public async Task<int> RunAsync(
        bool simulateSpeechFailure,
        CancellationToken cancellationToken)
    {
        try
        {
            cancellationToken.ThrowIfCancellationRequested();

            GlobalSystemMediaTransportControlsSession[] sessions;

            try
            {
                sessions = _manager.GetSessions().ToArray();
            }
            catch (Exception exception)
            {
                _log.Warning("Could not enumerate Windows media sessions for the announcement test", exception);
                _log.Write("ANNOUNCEMENT TEST RESULT: FAIL - no playback or speech commands were issued.");
                return 1;
            }

            _log.Write($"AVAILABLE WINDOWS MEDIA SESSIONS: {sessions.Length}");

            var details = new List<AnnouncementTargetDetails>(sessions.Length);
            for (int index = 0; index < sessions.Length; index++)
            {
                AnnouncementTargetDetails current = await ReadDetailsAsync(sessions[index]);
                details.Add(current);
                WriteDetails($"SESSION {index + 1}", current);
            }

            IReadOnlyList<AnnouncementTargetDetails> matches = FirefoxSessionSelector.FindExactMatches(
                details,
                candidate => candidate.SourceId);

            if (matches.Count == 0)
            {
                _log.Write("NO FIREFOX TARGET FOUND: no playback or speech commands were issued.");
                _log.Write("Start YouTube Music playing in Firefox, then run this test again.");
                _log.Write("ANNOUNCEMENT TEST RESULT: NOT RUN");
                return 1;
            }

            if (matches.Count > 1)
            {
                _log.Write("!!! AMBIGUOUS TARGET !!!");
                _log.Write(
                    $"Found {matches.Count} sessions matching firefox.exe. " +
                    "No playback or speech commands were issued.");

                for (int index = 0; index < matches.Count; index++)
                {
                    WriteDetails($"FIREFOX CANDIDATE {index + 1}", matches[index]);
                }

                _log.Write("Close the extra Firefox media session(s) and try again.");
                _log.Write("ANNOUNCEMENT TEST RESULT: NOT RUN");
                return 1;
            }

            GlobalSystemMediaTransportControlsSession selectedSession = matches[0].Session;
            AnnouncementTargetDetails target = await ReadDetailsAsync(selectedSession);
            WriteDetails("TARGET", target);

            if (!target.MediaReadSucceeded
                || !AnnouncementText.TryBuild(target.Media, out string? sentence))
            {
                _log.Write("METADATA SAFETY: Firefox did not provide both a usable title and artist.");
                _log.Write("No playback or speech commands were issued. Try again while a song is playing.");
                _log.Write("ANNOUNCEMENT TEST RESULT: NOT RUN");
                return 1;
            }

            if (!target.PlaybackReadSucceeded || !PlaybackControlRules.CanStart(target.PlaybackStatus))
            {
                _log.Write(
                    $"STARTING-STATE SAFETY: Firefox is {DisplayStatus(target.PlaybackStatus)}, not Playing.");
                _log.Write("No playback or speech commands were issued. Start YouTube Music playing and try again.");
                _log.Write("ANNOUNCEMENT TEST RESULT: NOT RUN");
                return 1;
            }

            _log.Write($"Proposed spoken sentence: {sentence}");
            _log.Write("Speak this announcement and temporarily pause Firefox? [y/N]");

            string? response = await Console.In.ReadLineAsync(cancellationToken);
            if (!PlaybackControlRules.IsAffirmative(response))
            {
                _log.Write("User confirmation: NO");
                _log.Write("ANNOUNCEMENT TEST CANCELLED: no playback or speech commands were issued.");
                _log.Write("ANNOUNCEMENT TEST RESULT: NOT RUN");
                return 0;
            }

            _log.Write("User confirmation: YES");
            cancellationToken.ThrowIfCancellationRequested();

            AnnouncementTargetDetails rechecked = await ReadDetailsAsync(selectedSession);
            if (!IsSafeToContinue(target, rechecked))
            {
                _log.Write("STALE-METADATA SAFETY: the same Firefox session changed while confirmation was pending.");
                _log.Write("No playback or speech commands were issued. Check the current song and run the test again.");
                _log.Write("ANNOUNCEMENT TEST RESULT: NOT RUN");
                return 1;
            }

            IPreparedAnnouncementSpeech? preparedSpeech = null;

            if (!simulateSpeechFailure)
            {
                try
                {
                    _log.Write("Preparing the confirmed Unicode announcement with the selected local voice...");
                    preparedSpeech = await _speechService.PrepareAsync(sentence, cancellationToken);
                    cancellationToken.ThrowIfCancellationRequested();
                }
                catch (OperationCanceledException)
                {
                    _log.Write("ANNOUNCEMENT TEST CANCELLED before pause: no playback commands were issued.");
                    _log.Write("ANNOUNCEMENT TEST RESULT: NOT RUN");
                    return 0;
                }
                catch (Exception exception)
                {
                    _log.Warning("Windows could not prepare the spoken announcement", exception);
                    _log.Write("SPEECH FAILED before playback was paused.");
                    _log.Write("No playback or speech commands were issued.");
                    _log.Write("ANNOUNCEMENT TEST RESULT: FAIL");
                    return 1;
                }
            }

            using (preparedSpeech)
            {
                var targetControl = new WindowsAnnouncementPlaybackTarget(selectedSession, _log);
                var sequence = new AnnouncementControlSequence(_log);
                AnnouncementSequenceResult result = await sequence.RunAsync(
                    targetControl,
                    preparedSpeech,
                    simulateSpeechFailure,
                    cancellationToken);
                return result.TestPassed ? 0 : 1;
            }
        }
        catch (OperationCanceledException)
        {
            _log.Write("ANNOUNCEMENT TEST CANCELLED before pause: no playback or speech commands were issued.");
            _log.Write("ANNOUNCEMENT TEST RESULT: NOT RUN");
            return 0;
        }
        catch (Exception exception)
        {
            _log.Warning("Unexpected failure before the announcement control sequence", exception);
            _log.Write("ANNOUNCEMENT TEST RESULT: FAIL - no playback or speech commands were issued.");
            return 1;
        }
    }

    private async Task<AnnouncementTargetDetails> ReadDetailsAsync(
        GlobalSystemMediaTransportControlsSession session)
    {
        string? sourceId = null;
        MediaSnapshot media = MediaSnapshot.Empty;
        bool sourceReadSucceeded = false;
        bool mediaReadSucceeded = false;

        try
        {
            sourceId = session.SourceAppUserModelId;
            sourceReadSucceeded = true;
        }
        catch (Exception exception)
        {
            _log.Warning("Could not read one session's source application identifier", exception);
        }

        try
        {
            GlobalSystemMediaTransportControlsSessionMediaProperties? properties =
                await session.TryGetMediaPropertiesAsync();

            if (properties is not null)
            {
                media = MediaSnapshot.FromWindows(properties.Title, properties.Artist, properties.AlbumTitle);
                mediaReadSucceeded = true;
            }
        }
        catch (Exception exception)
        {
            _log.Warning($"Could not read media properties for {MediaSnapshot.Display(sourceId)}", exception);
        }

        PlaybackStatusRead playback = TryReadPlaybackStatus(session, "while reading session details");
        return new AnnouncementTargetDetails(
            session,
            sourceId,
            sourceReadSucceeded,
            media,
            mediaReadSucceeded,
            playback.Status,
            playback.Success);
    }

    private bool IsSafeToContinue(
        AnnouncementTargetDetails original,
        AnnouncementTargetDetails current)
    {
        if (!current.SourceReadSucceeded
            || !string.Equals(
                current.SourceId,
                FirefoxSessionSelector.VerifiedSourceId,
                StringComparison.OrdinalIgnoreCase))
        {
            return false;
        }

        return current.MediaReadSucceeded
            && current.PlaybackReadSucceeded
            && PlaybackControlRules.CanStart(current.PlaybackStatus)
            && AnnouncementText.HasSameTitleAndArtist(original.Media, current.Media);
    }

    private PlaybackStatusRead TryReadPlaybackStatus(
        GlobalSystemMediaTransportControlsSession session,
        string context)
    {
        try
        {
            GlobalSystemMediaTransportControlsSessionPlaybackInfo? info = session.GetPlaybackInfo();
            return new PlaybackStatusRead(info is not null, info?.PlaybackStatus);
        }
        catch (Exception exception)
        {
            _log.Warning($"Could not read playback state {context}", exception);
            return new PlaybackStatusRead(false, null);
        }
    }

    private void WriteDetails(string heading, AnnouncementTargetDetails details)
    {
        _log.Write(heading);
        _log.Write($"  SourceAppUserModelId: {MediaSnapshot.Display(details.SourceId)}");
        _log.Write($"  Title: {MediaSnapshot.Display(details.Media.Title)}");
        _log.Write($"  Artist: {MediaSnapshot.Display(details.Media.Artist)}");
        _log.Write($"  Album: {MediaSnapshot.Display(details.Media.Album)}");
        _log.Write($"  Playback: {DisplayStatus(details.PlaybackStatus)}");
    }

    private static string DisplayStatus(
        GlobalSystemMediaTransportControlsSessionPlaybackStatus? status)
    {
        return status?.ToString() ?? "<not provided>";
    }

    private sealed record AnnouncementTargetDetails(
        GlobalSystemMediaTransportControlsSession Session,
        string? SourceId,
        bool SourceReadSucceeded,
        MediaSnapshot Media,
        bool MediaReadSucceeded,
        GlobalSystemMediaTransportControlsSessionPlaybackStatus? PlaybackStatus,
        bool PlaybackReadSucceeded);

}
