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
        CancellationToken cancellationToken);
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
    private readonly IAnnouncementLog _log;
    private readonly AudioDuckingConfiguration _configuration;
    private readonly IAudioSessionEnumerationProvider _enumerationProvider;

    public WindowsTransitionAudioDucker(
        IAnnouncementLog log,
        AudioDuckingConfiguration? configuration = null,
        IAudioSessionEnumerationProvider? enumerationProvider = null)
    {
        _log = log;
        _configuration = configuration ?? AudioDuckingConfiguration.Diagnostic;
        _configuration.Validate();
        _enumerationProvider = enumerationProvider ?? new WindowsAudioSessionEnumerationProvider(log);
    }

    public async Task<TransitionDuckingResult> RunAsync(
        Func<CancellationToken, Task> protectedAction,
        bool simulatePostDuckFailure,
        CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        AudioSessionEnumeration enumeration;

        try
        {
            _log.Write("DELIVERY AUDIO TARGET: re-enumerating the current default-output Core Audio sessions.");
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

        try
        {
            AudioSessionSelection selection = AudioSessionSelector.SelectFirefox(enumeration.Sessions);

            if (selection.Status == AudioSessionSelectionStatus.NoFirefoxCandidate)
            {
                _log.Write("DELIVERY ABANDONED: no active Firefox Core Audio candidate exists. No duck or speech began.");
                return new TransitionDuckingResult(
                    TransitionDuckingStatus.Refused,
                    "no active Firefox Core Audio candidate existed",
                    null);
            }

            if (selection.Status == AudioSessionSelectionStatus.Ambiguous)
            {
                _log.Write(
                    $"DELIVERY ABANDONED: {selection.FirefoxCandidates.Count} active Firefox Core Audio candidates are ambiguous. No duck or speech began.");
                return new TransitionDuckingResult(
                    TransitionDuckingStatus.Refused,
                    "the active Firefox Core Audio target was ambiguous",
                    null);
            }

            IAudioSessionVolumeTarget target = selection.Target!;
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
            foreach (AudioSessionDescriptor session in enumeration.Sessions)
            {
                session.Target.Dispose();
            }
        }
    }
}
