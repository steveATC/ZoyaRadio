using System.Diagnostics;
using Windows.Media.Control;

namespace ZoyaRadio.MediaProbe;

internal sealed class PlaybackControlTest
{
    private static readonly TimeSpan PauseVerificationTimeout = TimeSpan.FromSeconds(3);
    private static readonly TimeSpan TestPauseDuration = TimeSpan.FromSeconds(3);
    private static readonly TimeSpan ResumeVerificationTimeout = TimeSpan.FromSeconds(2);
    private static readonly TimeSpan PollInterval = TimeSpan.FromMilliseconds(150);
    private static readonly TimeSpan ResumeRetryDelay = TimeSpan.FromMilliseconds(300);
    private const int MaximumResumeAttempts = 3;

    private readonly GlobalSystemMediaTransportControlsSessionManager _manager;
    private readonly LocalLog _log;

    public PlaybackControlTest(
        GlobalSystemMediaTransportControlsSessionManager manager,
        LocalLog log)
    {
        _manager = manager;
        _log = log;
    }

    public async Task<int> RunAsync(CancellationToken cancellationToken)
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
                _log.Warning("Could not enumerate Windows media sessions for the control test", exception);
                _log.Write("CONTROL TEST RESULT: FAIL - no playback commands were issued.");
                return 1;
            }

            _log.Write($"AVAILABLE WINDOWS MEDIA SESSIONS: {sessions.Length}");

            var details = new List<ControlTargetDetails>(sessions.Length);
            for (int index = 0; index < sessions.Length; index++)
            {
                ControlTargetDetails current = await ReadDetailsAsync(sessions[index]);
                details.Add(current);
                WriteDetails($"SESSION {index + 1}", current);
            }

            IReadOnlyList<ControlTargetDetails> matches = FirefoxSessionSelector.FindExactMatches(
                details,
                candidate => candidate.SourceId);

            if (matches.Count == 0)
            {
                _log.Write("NO FIREFOX TARGET FOUND: no playback commands were issued.");
                _log.Write("Start YouTube Music playing in Firefox, then run this test again.");
                _log.Write("CONTROL TEST RESULT: NOT RUN");
                return 1;
            }

            if (matches.Count > 1)
            {
                _log.Write("!!! AMBIGUOUS TARGET !!!");
                _log.Write($"Found {matches.Count} sessions matching firefox.exe. No playback commands were issued.");

                for (int index = 0; index < matches.Count; index++)
                {
                    WriteDetails($"FIREFOX CANDIDATE {index + 1}", matches[index]);
                }

                _log.Write("Close the extra Firefox media session(s) and try again.");
                _log.Write("CONTROL TEST RESULT: NOT RUN");
                return 1;
            }

            GlobalSystemMediaTransportControlsSession selectedSession = matches[0].Session;
            ControlTargetDetails target = await ReadDetailsAsync(selectedSession);
            WriteDetails("TARGET", target);

            if (!PlaybackControlRules.CanStart(target.PlaybackStatus))
            {
                _log.Write(
                    $"STARTING-STATE SAFETY: Firefox is {DisplayStatus(target.PlaybackStatus)}, not Playing.");
                _log.Write("No playback commands were issued. Start YouTube Music playing and run the test again.");
                _log.Write("CONTROL TEST RESULT: NOT RUN");
                return 1;
            }

            _log.Write("Pause and resume this Firefox session for the Milestone 1 test? [y/N]");

            string? response = await Console.In.ReadLineAsync(cancellationToken);
            if (!PlaybackControlRules.IsAffirmative(response))
            {
                _log.Write("User confirmation: NO");
                _log.Write("CONTROL TEST CANCELLED: no playback commands were issued.");
                _log.Write("CONTROL TEST RESULT: NOT RUN");
                return 0;
            }

            _log.Write("User confirmation: YES");
            cancellationToken.ThrowIfCancellationRequested();

            PlaybackReadResult startingState = TryReadPlaybackStatus(selectedSession, "before pause");
            if (!startingState.Success || !PlaybackControlRules.CanStart(startingState.Status))
            {
                _log.Write(
                    $"STARTING-STATE SAFETY: Firefox changed to {DisplayStatus(startingState.Status)} before control.");
                _log.Write("No playback commands were issued. Ensure YouTube Music is playing and try again.");
                _log.Write("CONTROL TEST RESULT: NOT RUN");
                return 1;
            }

            return await ExecuteControlSequenceAsync(selectedSession, cancellationToken);
        }
        catch (OperationCanceledException)
        {
            _log.Write("CONTROL TEST CANCELLED before pause: no playback commands were issued.");
            _log.Write("CONTROL TEST RESULT: NOT RUN");
            return 0;
        }
        catch (Exception exception)
        {
            _log.Warning("Unexpected failure before the playback-control sequence", exception);
            _log.Write("CONTROL TEST RESULT: FAIL - no playback commands were issued.");
            return 1;
        }
    }

    private async Task<int> ExecuteControlSequenceAsync(
        GlobalSystemMediaTransportControlsSession selectedSession,
        CancellationToken cancellationToken)
    {
        bool pauseRequestAccepted = false;
        bool pauseVerified = false;
        bool restorationArmed = false;
        bool pauseIntervalCompleted = false;
        bool resumeRequestAccepted = false;
        bool playbackRestored = false;
        bool recoveryNeeded = false;
        string? failureReason = null;

        try
        {
            cancellationToken.ThrowIfCancellationRequested();
            restorationArmed = true;

            _log.Write($"CONTROL TEST: requesting pause for {FirefoxSessionSelector.VerifiedSourceId}");

            try
            {
                pauseRequestAccepted = await selectedSession.TryPauseAsync();
                _log.Write($"Pause request boolean result: {pauseRequestAccepted}");
            }
            catch (Exception exception)
            {
                _log.Warning("TryPauseAsync failed after resume protection was armed", exception);
                throw new ControlTestFailureException("The pause request threw an exception.", exception);
            }

            if (!pauseRequestAccepted)
            {
                PlaybackReadResult afterFailure = TryReadPlaybackStatus(selectedSession, "after rejected pause");
                _log.Write($"Playback after rejected pause request: {DisplayStatus(afterFailure.Status)}");
                throw new ControlTestFailureException("Firefox rejected the pause request.");
            }

            PlaybackWaitResult paused = await WaitForStateAsync(
                selectedSession,
                GlobalSystemMediaTransportControlsSessionPlaybackStatus.Paused,
                PauseVerificationTimeout,
                cancellationToken);

            if (!paused.ReachedExpectedState)
            {
                throw new ControlTestFailureException(
                    $"Pause was accepted, but Paused was not observed. Last state: {DisplayStatus(paused.LastStatus)}.");
            }

            pauseVerified = true;
            _log.Write("CONTROL TEST: Playing -> Paused");
            _log.Write("PAUSE VERIFIED");
            _log.Write("Holding the test pause for approximately 3 seconds...");

            await Task.Delay(TestPauseDuration, cancellationToken);
            cancellationToken.ThrowIfCancellationRequested();
            pauseIntervalCompleted = true;
        }
        catch (OperationCanceledException)
        {
            failureReason = "Ctrl+C or cancellation occurred after the control sequence began.";
            recoveryNeeded = restorationArmed;
            _log.Write("Cancellation received. Protected same-session resume cleanup will run before exit.");
        }
        catch (ControlTestFailureException exception)
        {
            failureReason = exception.Message;
            recoveryNeeded = restorationArmed;
            _log.Write($"CONTROL TEST FAILURE: {failureReason}");
        }
        catch (Exception exception)
        {
            failureReason = "An unexpected error occurred after the control sequence began.";
            recoveryNeeded = restorationArmed;
            _log.Warning(failureReason, exception);
        }
        finally
        {
            if (restorationArmed)
            {
                try
                {
                    ResumeOutcome resume = await TryResumeSameSessionAsync(selectedSession);
                    resumeRequestAccepted = resume.RequestAccepted;
                    playbackRestored = resume.PlaybackRestored;
                }
                catch (Exception exception)
                {
                    failureReason ??= "Protected resume cleanup encountered an unexpected error.";
                    recoveryNeeded = true;
                    _log.Warning(failureReason, exception);
                    _log.Write("!!! WARNING !!!");
                    _log.Write("ZOYA RADIO COULD NOT CONFIRM THAT FIREFOX RESUMED.");
                    _log.Write("PLEASE RESUME YOUTUBE MUSIC MANUALLY.");
                    _log.Write("!!! WARNING !!!");
                }
            }
        }

        bool passed = pauseRequestAccepted
            && pauseVerified
            && pauseIntervalCompleted
            && resumeRequestAccepted
            && playbackRestored
            && failureReason is null;

        _log.Write($"Recovery needed: {(recoveryNeeded ? "YES" : "NO")}");

        if (passed)
        {
            _log.Write("CONTROL TEST RESULT: PASS");
            return 0;
        }

        if (failureReason is not null)
        {
            _log.Write($"Failure reason: {failureReason}");
        }

        _log.Write("CONTROL TEST RESULT: FAIL");
        return 1;
    }

    private async Task<ResumeOutcome> TryResumeSameSessionAsync(
        GlobalSystemMediaTransportControlsSession selectedSession)
    {
        bool anyRequestAccepted = false;
        bool playingObserved = false;

        for (int attempt = 1; attempt <= MaximumResumeAttempts; attempt++)
        {
            bool requestAccepted = false;

            try
            {
                _log.Write(
                    $"CONTROL TEST: requesting resume for {FirefoxSessionSelector.VerifiedSourceId} " +
                    $"(attempt {attempt} of {MaximumResumeAttempts})");
                requestAccepted = await selectedSession.TryPlayAsync();
                anyRequestAccepted |= requestAccepted;
                _log.Write($"Resume request attempt {attempt} boolean result: {requestAccepted}");
            }
            catch (Exception exception)
            {
                _log.Warning($"Resume request attempt {attempt} threw an exception", exception);
            }

            PlaybackWaitResult playing = await WaitForStateAsync(
                selectedSession,
                GlobalSystemMediaTransportControlsSessionPlaybackStatus.Playing,
                ResumeVerificationTimeout,
                CancellationToken.None);

            if (playing.ReachedExpectedState)
            {
                playingObserved = true;

                if (anyRequestAccepted)
                {
                    _log.Write("CONTROL TEST: Paused -> Playing");
                    _log.Write("RESUME VERIFIED");
                    return new ResumeOutcome(true, true);
                }

                _log.Write(
                    "Firefox is Playing, but this resume request returned false; " +
                    "retrying so the control result is not falsely credited.");
            }
            else
            {
                _log.Write(
                    $"Resume attempt {attempt} was not verified. " +
                    $"Last state: {DisplayStatus(playing.LastStatus)}.");
            }

            if (attempt < MaximumResumeAttempts)
            {
                await Task.Delay(ResumeRetryDelay);
            }
        }

        if (playingObserved)
        {
            _log.Write("Firefox is Playing, but no resume request reported success.");
            _log.Write("Resume safety was achieved, but the Milestone 1 control proof failed.");
            return new ResumeOutcome(false, true);
        }

        _log.Write("!!! WARNING !!!");
        _log.Write("ZOYA RADIO COULD NOT CONFIRM THAT FIREFOX RESUMED.");
        _log.Write("PLEASE RESUME YOUTUBE MUSIC MANUALLY.");
        _log.Write("!!! WARNING !!!");
        return new ResumeOutcome(anyRequestAccepted, false);
    }

    private async Task<PlaybackWaitResult> WaitForStateAsync(
        GlobalSystemMediaTransportControlsSession session,
        GlobalSystemMediaTransportControlsSessionPlaybackStatus expected,
        TimeSpan timeout,
        CancellationToken cancellationToken)
    {
        var stopwatch = Stopwatch.StartNew();
        GlobalSystemMediaTransportControlsSessionPlaybackStatus? lastStatus = null;

        while (true)
        {
            cancellationToken.ThrowIfCancellationRequested();

            PlaybackReadResult current = TryReadPlaybackStatus(session, $"while waiting for {expected}");
            if (current.Success)
            {
                lastStatus = current.Status;
                if (current.Status == expected)
                {
                    return new PlaybackWaitResult(true, lastStatus);
                }
            }

            if (stopwatch.Elapsed >= timeout)
            {
                return new PlaybackWaitResult(false, lastStatus);
            }

            await Task.Delay(PollInterval, cancellationToken);
        }
    }

    private async Task<ControlTargetDetails> ReadDetailsAsync(
        GlobalSystemMediaTransportControlsSession session)
    {
        string? sourceId = null;
        MediaSnapshot media = MediaSnapshot.Empty;

        try
        {
            sourceId = session.SourceAppUserModelId;
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
            }
        }
        catch (Exception exception)
        {
            _log.Warning($"Could not read media properties for {MediaSnapshot.Display(sourceId)}", exception);
        }

        PlaybackReadResult playback = TryReadPlaybackStatus(session, "while listing sessions");
        return new ControlTargetDetails(session, sourceId, media, playback.Status);
    }

    private PlaybackReadResult TryReadPlaybackStatus(
        GlobalSystemMediaTransportControlsSession session,
        string context)
    {
        try
        {
            GlobalSystemMediaTransportControlsSessionPlaybackInfo? info = session.GetPlaybackInfo();
            return new PlaybackReadResult(info is not null, info?.PlaybackStatus);
        }
        catch (Exception exception)
        {
            _log.Warning($"Could not read playback state {context}", exception);
            return new PlaybackReadResult(false, null);
        }
    }

    private void WriteDetails(string heading, ControlTargetDetails details)
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

    private sealed record ControlTargetDetails(
        GlobalSystemMediaTransportControlsSession Session,
        string? SourceId,
        MediaSnapshot Media,
        GlobalSystemMediaTransportControlsSessionPlaybackStatus? PlaybackStatus);

    private sealed record PlaybackReadResult(
        bool Success,
        GlobalSystemMediaTransportControlsSessionPlaybackStatus? Status);

    private sealed record PlaybackWaitResult(
        bool ReachedExpectedState,
        GlobalSystemMediaTransportControlsSessionPlaybackStatus? LastStatus);

    private sealed record ResumeOutcome(bool RequestAccepted, bool PlaybackRestored);

    private sealed class ControlTestFailureException : Exception
    {
        public ControlTestFailureException(string message)
            : base(message)
        {
        }

        public ControlTestFailureException(string message, Exception innerException)
            : base(message, innerException)
        {
        }
    }
}
