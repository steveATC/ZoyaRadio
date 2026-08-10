namespace ZoyaRadio.MediaProbe;

internal enum TransitionDuckingStatus
{
    Completed,
    Refused,
    Cancelled,
    Failed,
}

internal sealed record TransitionDuckingResult(
    TransitionDuckingStatus Status,
    string Reason,
    AudioDuckingResult? AudioResult);

internal interface ITransitionAudioDucker
{
    Task<TransitionDuckingResult> RunAsync(
        Func<CancellationToken, Task> protectedAction,
        bool simulatePostDuckFailure,
        CancellationToken cancellationToken,
        DateTimeOffset? targetAcquisitionDeadline = null);
}

internal interface IAudioSessionEnumerationProvider
{
    AudioSessionEnumeration Enumerate();
}

internal sealed class WindowsAudioSessionEnumerationProvider : IAudioSessionEnumerationProvider
{
    private readonly IAnnouncementLog _log;

    public WindowsAudioSessionEnumerationProvider(IAnnouncementLog log)
    {
        _log = log;
    }

    public AudioSessionEnumeration Enumerate()
    {
        return WindowsAudioSessionCatalog.Enumerate(_log);
    }
}

internal sealed class WindowsTransitionAudioDucker : ITransitionAudioDucker
{
    private static readonly TimeSpan RetryInterval = TimeSpan.FromMilliseconds(100);
    private static readonly TimeSpan SpeechStartSafetyMargin = TimeSpan.FromMilliseconds(100);
    private readonly IAnnouncementLog _log;
    private readonly AudioDuckingConfiguration _configuration;
    private readonly IAudioSessionEnumerationProvider _enumerationProvider;
    private readonly IRadioClock _clock;

    public WindowsTransitionAudioDucker(
        IAnnouncementLog log,
        AudioDuckingConfiguration? configuration = null,
        IAudioSessionEnumerationProvider? enumerationProvider = null,
        IRadioClock? clock = null)
    {
        _log = log;
        _configuration = configuration ?? AudioDuckingConfiguration.Diagnostic;
        _configuration.Validate();
        _enumerationProvider = enumerationProvider ?? new WindowsAudioSessionEnumerationProvider(log);
        _clock = clock ?? new SystemRadioClock();
    }

    public async Task<TransitionDuckingResult> RunAsync(
        Func<CancellationToken, Task> protectedAction,
        bool simulatePostDuckFailure,
        CancellationToken cancellationToken,
        DateTimeOffset? targetAcquisitionDeadline = null)
    {
        cancellationToken.ThrowIfCancellationRequested();
        DateTimeOffset? finalSelectionTime = targetAcquisitionDeadline
            - _configuration.RampDuration
            - SpeechStartSafetyMargin;
        int attempt = 0;

        while (true)
        {
            cancellationToken.ThrowIfCancellationRequested();
            attempt++;
            AudioSessionEnumeration enumeration;

            try
            {
                _log.Write(
                    $"DELIVERY AUDIO TARGET: enumerating current Core Audio sessions (attempt {attempt}).");
                enumeration = _enumerationProvider.Enumerate();
            }
            catch (Exception exception)
            {
                _log.Warning("The Firefox audio target could not be enumerated; staying silent", exception);
                return new TransitionDuckingResult(
                    TransitionDuckingStatus.Refused,
                    "the Windows audio-session list was unavailable",
                    null);
            }

            AudioSessionSelection selection = AudioSessionSelector.SelectFirefox(enumeration.Sessions);

            if (selection.Status == AudioSessionSelectionStatus.Selected)
            {
                return await RunSelectedAsync(
                    selection.Target!,
                    enumeration,
                    protectedAction,
                    simulatePostDuckFailure,
                    cancellationToken);
            }

            if (selection.Status == AudioSessionSelectionStatus.Ambiguous)
            {
                _log.Write(
                    $"DELIVERY ABANDONED: MULTIPLE/AMBIGUOUS FIREFOX CORE AUDIO CANDIDATES " +
                    $"({selection.FirefoxCandidates.Count} active exact Firefox sessions). No duck or speech began.");
                DisposeEnumeration(enumeration);
                return new TransitionDuckingResult(
                    TransitionDuckingStatus.Refused,
                    "the active Firefox Core Audio target was ambiguous",
                    null);
            }

            string unavailableDescription = selection.AllFirefoxSessions.Count == 0
                ? "ZERO FIREFOX CORE AUDIO SESSIONS were present"
                : $"ZERO ACTIVE FIREFOX CORE AUDIO CANDIDATES were present; " +
                  $"{selection.AllFirefoxSessions.Count} exact Firefox session(s) were inactive or unavailable";
            _log.Write($"DELIVERY AUDIO TARGET NOT READY: {unavailableDescription} on attempt {attempt}.");
            DisposeEnumeration(enumeration);

            if (finalSelectionTime is null || _clock.Now >= finalSelectionTime.Value)
            {
                _log.Write(
                    "DELIVERY ABANDONED: no active Firefox Core Audio candidate became available " +
                    "inside the existing transition window. No duck or speech began.");
                return new TransitionDuckingResult(
                    TransitionDuckingStatus.Refused,
                    "no active Firefox Core Audio candidate became available inside the transition window",
                    null);
            }

            TimeSpan remaining = finalSelectionTime.Value - _clock.Now;
            TimeSpan delay = remaining < RetryInterval ? remaining : RetryInterval;
            _log.Write(
                $"DELIVERY AUDIO RETRY: waiting {delay.TotalMilliseconds:0} ms within the existing " +
                "two-second transition window; Chrome and unrelated sessions remain ineligible.");
            await _clock.DelayAsync(delay, cancellationToken);
        }
    }

    private async Task<TransitionDuckingResult> RunSelectedAsync(
        IAudioSessionVolumeTarget target,
        AudioSessionEnumeration enumeration,
        Func<CancellationToken, Task> protectedAction,
        bool simulatePostDuckFailure,
        CancellationToken cancellationToken)
    {
        try
        {
            AudioSessionVolumeState deliveryState = target.ReadState();
            string? unsuitableReason = AudioDuckingSafety.GetUnsuitableReason(deliveryState, _configuration);
            if (unsuitableReason is not null)
            {
                _log.Write($"DELIVERY ABANDONED: Firefox audio was unsafe to duck because {unsuitableReason}. No speech began.");
                return new TransitionDuckingResult(
                    TransitionDuckingStatus.Refused,
                    unsuitableReason,
                    null);
            }

            _log.Write(
                $"DELIVERY AUDIO TARGET REVALIDATED: firefox.exe; PID {target.ProcessId?.ToString() ?? "<not available>"}; " +
                $"session {target.StableSessionTag}; current volume {deliveryState.VolumeScalar:P1}; muted {(deliveryState.Muted ? "Yes" : "No")}.");

            var sequence = new AudioDuckingSequence(_log, _configuration);
            AudioDuckingResult result = await sequence.RunWithProtectedActionAsync(
                target,
                protectedAction,
                simulatePostDuckFailure,
                cancellationToken);

            if (result.TestPassed)
            {
                return new TransitionDuckingResult(
                    TransitionDuckingStatus.Completed,
                    "speech completed and the exact Firefox audio state was restored",
                    result);
            }

            if (result.Cancelled)
            {
                return new TransitionDuckingResult(
                    TransitionDuckingStatus.Cancelled,
                    result.RestorationVerified
                        ? "delivery was cancelled and exact restoration passed"
                        : "delivery was cancelled but exact restoration was not verified",
                    result);
            }

            if (!result.VolumeCommandMayHaveBeenIssued)
            {
                return new TransitionDuckingResult(
                    TransitionDuckingStatus.Refused,
                    result.FailureReason ?? "the selected Firefox audio state was unsuitable",
                    result);
            }

            return new TransitionDuckingResult(
                TransitionDuckingStatus.Failed,
                result.FailureReason ?? "ducking, speech, or restoration did not fully pass",
                result);
        }
        finally
        {
            DisposeEnumeration(enumeration);
        }
    }

    private static void DisposeEnumeration(AudioSessionEnumeration enumeration)
    {
        foreach (AudioSessionDescriptor session in enumeration.Sessions)
        {
            session.Target.Dispose();
        }
    }
}
