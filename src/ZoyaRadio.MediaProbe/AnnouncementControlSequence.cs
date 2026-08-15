using System.Diagnostics;
using Windows.Media.Control;

namespace ZoyaRadio.MediaProbe;

internal interface IAnnouncementLog
{
    void Write(string message);

    void Warning(string context, Exception exception);
}

internal interface IAnnouncementPlaybackTarget
{
    string SourceId { get; }

    Task<bool> RequestPauseAsync();

    Task<bool> RequestPlayAsync();

    PlaybackStatusRead ReadPlaybackStatus();
}

internal interface IPreparedAnnouncementSpeech : IDisposable
{
    TimeSpan MeasuredDuration { get; }

    Task<SpeechPlaybackResult> SpeakAsync(TimeSpan timeout, CancellationToken cancellationToken);
}

internal enum SpeechPlaybackStatus
{
    Completed,
    Failed,
    Cancelled,
    TimedOut,
}

internal sealed record SpeechPlaybackResult(SpeechPlaybackStatus Status, string? ErrorMessage = null);

internal sealed record PlaybackStatusRead(
    bool Success,
    GlobalSystemMediaTransportControlsSessionPlaybackStatus? Status);

internal sealed record AnnouncementSequenceResult(
    bool TestPassed,
    bool PauseVerified,
    bool PlaybackRestored,
    bool RecoveryNeeded,
    bool SimulatedFailure);

internal sealed record AnnouncementSequenceTimings(
    TimeSpan PauseVerificationTimeout,
    TimeSpan SpeechTimeout,
    TimeSpan ResumeVerificationTimeout,
    TimeSpan PollInterval,
    TimeSpan ResumeRetryDelay,
    int MaximumResumeAttempts)
{
    public static AnnouncementSequenceTimings Production { get; } = new(
        TimeSpan.FromSeconds(3),
        TimeSpan.FromSeconds(20),
        TimeSpan.FromSeconds(2),
        TimeSpan.FromMilliseconds(150),
        TimeSpan.FromMilliseconds(300),
        3);
}

internal sealed class AnnouncementControlSequence
{
    private readonly IAnnouncementLog _log;
    private readonly AnnouncementSequenceTimings _timings;

    public AnnouncementControlSequence(
        IAnnouncementLog log,
        AnnouncementSequenceTimings? timings = null)
    {
        _log = log;
        _timings = timings ?? AnnouncementSequenceTimings.Production;
    }

    public async Task<AnnouncementSequenceResult> RunAsync(
        IAnnouncementPlaybackTarget target,
        IPreparedAnnouncementSpeech? preparedSpeech,
        bool simulateSpeechFailure,
        CancellationToken cancellationToken)
    {
        bool pauseRequestAccepted = false;
        bool pauseVerified = false;
        bool restorationArmed = false;
        bool speechCompleted = false;
        bool resumeRequestAccepted = false;
        bool playbackRestored = false;
        bool recoveryNeeded = false;
        string? failureReason = null;

        try
        {
            cancellationToken.ThrowIfCancellationRequested();
            restorationArmed = true;

            _log.Write($"PAUSE REQUESTED: {target.SourceId}");

            try
            {
                pauseRequestAccepted = await target.RequestPauseAsync();
            }
            catch (OperationCanceledException)
            {
                throw;
            }
            catch (Exception exception)
            {
                _log.Warning("TryPauseAsync failed after resume protection was armed", exception);
                throw new AnnouncementSequenceException("The pause request threw an exception.");
            }

            _log.Write($"Pause request boolean result: {pauseRequestAccepted}");

            if (!pauseRequestAccepted)
            {
                _log.Write("PAUSE FAILED: Firefox rejected the targeted pause request.");
                throw new AnnouncementSequenceException("Firefox rejected the pause request.");
            }

            PlaybackWaitResult paused = await WaitForStateAsync(
                target,
                GlobalSystemMediaTransportControlsSessionPlaybackStatus.Paused,
                _timings.PauseVerificationTimeout,
                cancellationToken);

            if (!paused.ReachedExpectedState)
            {
                _log.Write("PAUSE FAILED: Paused was not observed within the bounded wait.");
                throw new AnnouncementSequenceException(
                    $"Pause was accepted, but Paused was not observed. Last state: {DisplayStatus(paused.LastStatus)}.");
            }

            pauseVerified = true;
            _log.Write("CONTROL: Playing -> Paused");
            _log.Write("PAUSE VERIFIED");

            if (simulateSpeechFailure)
            {
                recoveryNeeded = true;
                failureReason = "Speech failure was intentionally simulated before audible speech.";
                _log.Write("SIMULATED SPEECH FAILURE: intentionally triggering the protected recovery path.");
                _log.Write($"SPEECH FAILED: {failureReason}");
            }
            else
            {
                if (preparedSpeech is null)
                {
                    throw new AnnouncementSequenceException("Prepared speech was unexpectedly unavailable.");
                }

                _log.Write("SPEECH STARTED");
                SpeechPlaybackResult speech = await preparedSpeech.SpeakAsync(
                    _timings.SpeechTimeout,
                    cancellationToken);

                switch (speech.Status)
                {
                    case SpeechPlaybackStatus.Completed:
                        speechCompleted = true;
                        _log.Write("SPEECH COMPLETED");
                        break;
                    case SpeechPlaybackStatus.TimedOut:
                        recoveryNeeded = true;
                        failureReason = speech.ErrorMessage ?? "Speech exceeded its time limit.";
                        _log.Write("SPEECH TIMEOUT: speech time limit reached; protected playback recovery will run.");
                        break;
                    case SpeechPlaybackStatus.Cancelled:
                        recoveryNeeded = true;
                        failureReason = speech.ErrorMessage ?? "Speech was cancelled.";
                        _log.Write("SPEECH CANCELLED");
                        break;
                    default:
                        recoveryNeeded = true;
                        failureReason = speech.ErrorMessage ?? "Windows speech playback failed.";
                        _log.Write($"SPEECH FAILED: {failureReason}");
                        break;
                }
            }
        }
        catch (OperationCanceledException)
        {
            recoveryNeeded = restorationArmed;
            failureReason = "Ctrl+C or cancellation occurred after the control sequence began.";
            _log.Write("SPEECH CANCELLED: protected same-session resume cleanup will run before exit.");
        }
        catch (AnnouncementSequenceException exception)
        {
            recoveryNeeded = restorationArmed;
            failureReason = exception.Message;
            _log.Write($"ANNOUNCEMENT TEST FAILURE: {failureReason}");
        }
        catch (Exception exception)
        {
            recoveryNeeded = restorationArmed;
            failureReason = "An unexpected error occurred after the control sequence began.";
            _log.Warning(failureReason, exception);
        }
        finally
        {
            if (restorationArmed)
            {
                try
                {
                    ResumeOutcome resume = await TryResumeSameSessionAsync(target);
                    resumeRequestAccepted = resume.RequestAccepted;
                    playbackRestored = resume.PlaybackRestored;
                }
                catch (Exception exception)
                {
                    recoveryNeeded = true;
                    failureReason ??= "Protected resume cleanup encountered an unexpected error.";
                    _log.Warning(failureReason, exception);
                    WriteManualResumeWarning();
                }
            }
        }

        if (restorationArmed && !playbackRestored)
        {
            recoveryNeeded = true;
        }

        _log.Write($"Protected recovery needed: {(recoveryNeeded ? "YES" : "NO")}");

        if (recoveryNeeded)
        {
            _log.Write($"PLAYBACK RECOVERY: {(playbackRestored ? "PASS" : "FAIL")}");
        }

        if (simulateSpeechFailure)
        {
            bool simulatedTestPassed = pauseRequestAccepted
                && pauseVerified
                && resumeRequestAccepted
                && playbackRestored;

            _log.Write(
                $"SIMULATED SPEECH-FAILURE TEST RESULT: {(simulatedTestPassed ? "PASS" : "FAIL")}");
            _log.Write("ANNOUNCEMENT TEST RESULT: NOT RUN - audible speech was intentionally skipped.");
            return new AnnouncementSequenceResult(
                simulatedTestPassed,
                pauseVerified,
                playbackRestored,
                recoveryNeeded,
                true);
        }

        bool passed = pauseRequestAccepted
            && pauseVerified
            && speechCompleted
            && resumeRequestAccepted
            && playbackRestored
            && failureReason is null;

        if (failureReason is not null)
        {
            _log.Write($"Failure reason: {failureReason}");
        }

        _log.Write($"ANNOUNCEMENT TEST RESULT: {(passed ? "PASS" : "FAIL")}");
        return new AnnouncementSequenceResult(
            passed,
            pauseVerified,
            playbackRestored,
            recoveryNeeded,
            false);
    }

    private async Task<ResumeOutcome> TryResumeSameSessionAsync(IAnnouncementPlaybackTarget target)
    {
        bool anyRequestAccepted = false;
        bool playingObserved = false;

        for (int attempt = 1; attempt <= _timings.MaximumResumeAttempts; attempt++)
        {
            bool requestAccepted = false;

            try
            {
                _log.Write(
                    $"RESUME REQUESTED: {target.SourceId} " +
                    $"(attempt {attempt} of {_timings.MaximumResumeAttempts})");
                requestAccepted = await target.RequestPlayAsync();
                anyRequestAccepted |= requestAccepted;
                _log.Write($"Resume request attempt {attempt} boolean result: {requestAccepted}");
            }
            catch (Exception exception)
            {
                _log.Warning($"Resume request attempt {attempt} threw an exception", exception);
            }

            PlaybackWaitResult playing = await WaitForStateAsync(
                target,
                GlobalSystemMediaTransportControlsSessionPlaybackStatus.Playing,
                _timings.ResumeVerificationTimeout,
                CancellationToken.None);

            if (playing.ReachedExpectedState)
            {
                playingObserved = true;

                if (anyRequestAccepted)
                {
                    _log.Write("CONTROL: Paused -> Playing");
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

            if (attempt < _timings.MaximumResumeAttempts)
            {
                await Task.Delay(_timings.ResumeRetryDelay);
            }
        }

        if (playingObserved)
        {
            _log.Write("Firefox is Playing, but no resume request reported success.");
            return new ResumeOutcome(false, true);
        }

        WriteManualResumeWarning();
        return new ResumeOutcome(anyRequestAccepted, false);
    }

    private async Task<PlaybackWaitResult> WaitForStateAsync(
        IAnnouncementPlaybackTarget target,
        GlobalSystemMediaTransportControlsSessionPlaybackStatus expected,
        TimeSpan timeout,
        CancellationToken cancellationToken)
    {
        var stopwatch = Stopwatch.StartNew();
        GlobalSystemMediaTransportControlsSessionPlaybackStatus? lastStatus = null;

        while (true)
        {
            cancellationToken.ThrowIfCancellationRequested();

            try
            {
                PlaybackStatusRead current = target.ReadPlaybackStatus();
                if (current.Success)
                {
                    lastStatus = current.Status;
                    if (current.Status == expected)
                    {
                        return new PlaybackWaitResult(true, lastStatus);
                    }
                }
            }
            catch (Exception exception)
            {
                _log.Warning($"Could not read playback while waiting for {expected}", exception);
            }

            if (stopwatch.Elapsed >= timeout)
            {
                return new PlaybackWaitResult(false, lastStatus);
            }

            await Task.Delay(_timings.PollInterval, cancellationToken);
        }
    }

    private void WriteManualResumeWarning()
    {
        _log.Write("!!! WARNING !!!");
        _log.Write("ZOYA RADIO COULD NOT CONFIRM THAT FIREFOX RESUMED.");
        _log.Write("PLEASE RESUME YOUTUBE MUSIC MANUALLY.");
        _log.Write("!!! WARNING !!!");
    }

    private static string DisplayStatus(
        GlobalSystemMediaTransportControlsSessionPlaybackStatus? status)
    {
        return status?.ToString() ?? "<not provided>";
    }

    private sealed record PlaybackWaitResult(
        bool ReachedExpectedState,
        GlobalSystemMediaTransportControlsSessionPlaybackStatus? LastStatus);

    private sealed record ResumeOutcome(bool RequestAccepted, bool PlaybackRestored);

    private sealed class AnnouncementSequenceException : Exception
    {
        public AnnouncementSequenceException(string message)
            : base(message)
        {
        }
    }
}
