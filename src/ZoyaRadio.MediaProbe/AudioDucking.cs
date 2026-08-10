namespace ZoyaRadio.MediaProbe;

internal enum AudioSessionActivity
{
    Unknown = -1,
    Inactive = 0,
    Active = 1,
    Expired = 2,
}

internal sealed record AudioSessionVolumeState(
    bool Success,
    AudioSessionActivity Activity,
    float VolumeScalar,
    bool Muted,
    string? ErrorMessage = null);

internal interface IAudioSessionVolumeTarget : IDisposable
{
    string StableSessionTag { get; }

    int? ProcessId { get; }

    string? ProcessName { get; }

    AudioSessionVolumeState ReadState();

    void SetVolume(float volumeScalar, Guid eventContext);

    void SetMute(bool muted, Guid eventContext);
}

internal sealed record AudioSessionDescriptor(
    IAudioSessionVolumeTarget Target,
    AudioSessionVolumeState InitialState);

internal enum AudioSessionSelectionStatus
{
    NoFirefoxCandidate,
    Selected,
    Ambiguous,
}

internal sealed record AudioSessionSelection(
    AudioSessionSelectionStatus Status,
    IAudioSessionVolumeTarget? Target,
    IReadOnlyList<AudioSessionDescriptor> FirefoxCandidates,
    IReadOnlyList<AudioSessionDescriptor> AllFirefoxSessions);

internal static class AudioSessionSelector
{
    public static AudioSessionSelection SelectFirefox(
        IEnumerable<AudioSessionDescriptor> sessions)
    {
        AudioSessionDescriptor[] firefoxSessions = sessions
            .Where(session => IsFirefoxProcessName(session.Target.ProcessName))
            .ToArray();
        AudioSessionDescriptor[] candidates = firefoxSessions
            .Where(session =>
                session.InitialState.Activity == AudioSessionActivity.Active)
            .ToArray();

        return candidates.Length switch
        {
            0 => new AudioSessionSelection(
                AudioSessionSelectionStatus.NoFirefoxCandidate,
                null,
                candidates,
                firefoxSessions),
            1 => new AudioSessionSelection(
                AudioSessionSelectionStatus.Selected,
                candidates[0].Target,
                candidates,
                firefoxSessions),
            _ => new AudioSessionSelection(
                AudioSessionSelectionStatus.Ambiguous,
                null,
                candidates,
                firefoxSessions),
        };
    }

    public static bool IsFirefoxProcessName(string? processName)
    {
        string normalized = processName?.Trim() ?? string.Empty;
        return string.Equals(normalized, "firefox", StringComparison.OrdinalIgnoreCase)
            || string.Equals(normalized, "firefox.exe", StringComparison.OrdinalIgnoreCase);
    }
}

internal sealed record AudioDuckingConfiguration(
    float DuckRatio,
    float MinimumUsableStartingVolume,
    TimeSpan RampDuration,
    TimeSpan HoldDuration,
    int RampSteps,
    float VerificationTolerance)
{
    public static AudioDuckingConfiguration Diagnostic { get; } = new(
        DuckRatio: 0.25f,
        MinimumUsableStartingVolume: 0.05f,
        RampDuration: TimeSpan.FromMilliseconds(600),
        HoldDuration: TimeSpan.FromSeconds(3),
        RampSteps: 10,
        VerificationTolerance: 0.01f);

    public void Validate()
    {
        if (DuckRatio <= 0f || DuckRatio >= 1f)
        {
            throw new InvalidOperationException("The duck ratio must be greater than zero and less than one.");
        }

        if (MinimumUsableStartingVolume < 0f || MinimumUsableStartingVolume > 1f)
        {
            throw new InvalidOperationException("The minimum usable volume must be between zero and one.");
        }

        if (RampDuration < TimeSpan.Zero || HoldDuration < TimeSpan.Zero)
        {
            throw new InvalidOperationException("Ducking durations cannot be negative.");
        }

        if (RampSteps < 1)
        {
            throw new InvalidOperationException("At least one ramp step is required.");
        }

        if (VerificationTolerance < 0f || VerificationTolerance > 1f)
        {
            throw new InvalidOperationException("The verification tolerance must be between zero and one.");
        }
    }

    public float CalculateDuckVolume(float capturedStartingVolume)
    {
        return Math.Clamp(capturedStartingVolume * DuckRatio, 0f, 1f);
    }
}

internal static class AudioDuckingSafety
{
    public static string? GetUnsuitableReason(
        AudioSessionVolumeState state,
        AudioDuckingConfiguration configuration)
    {
        if (!state.Success)
        {
            return state.ErrorMessage ?? "Windows did not provide readable session-volume state.";
        }

        if (state.Activity != AudioSessionActivity.Active)
        {
            return $"the audio session is {state.Activity}, not Active";
        }

        if (state.Muted)
        {
            return "the Firefox audio session is already muted";
        }

        if (state.VolumeScalar < configuration.MinimumUsableStartingVolume)
        {
            return $"the Firefox per-session volume is only {state.VolumeScalar:P0}";
        }

        return null;
    }
}

internal sealed record AudioDuckingResult(
    bool DuckVerified,
    bool RestorationAttempted,
    bool RestorationVerified,
    bool VolumeCommandMayHaveBeenIssued,
    bool Cancelled,
    bool SimulatedFailure,
    string? FailureReason)
{
    public bool TestPassed => DuckVerified
        && RestorationVerified
        && !Cancelled
        && !SimulatedFailure
        && FailureReason is null;

    public bool ExpectedFailureRecoveryPassed => SimulatedFailure && RestorationVerified;
}

internal sealed class SimulatedAudioDuckingException : Exception
{
    public SimulatedAudioDuckingException()
        : base("The requested diagnostic failure was triggered after ducking was verified.")
    {
    }
}

internal sealed class AudioDuckingSequence
{
    private readonly IAnnouncementLog _log;
    private readonly AudioDuckingConfiguration _configuration;

    public AudioDuckingSequence(
        IAnnouncementLog log,
        AudioDuckingConfiguration? configuration = null)
    {
        _log = log;
        _configuration = configuration ?? AudioDuckingConfiguration.Diagnostic;
        _configuration.Validate();
    }

    public async Task<AudioDuckingResult> RunAsync(
        IAudioSessionVolumeTarget target,
        bool simulateFailure,
        CancellationToken cancellationToken)
    {
        return await RunCoreAsync(
            target,
            simulateFailure,
            async actionCancellationToken =>
            {
                _log.Write(
                    $"DUCK HOLD: approximately {_configuration.HoldDuration.TotalSeconds:0.#} seconds. Firefox remains Playing.");
                await Task.Delay(_configuration.HoldDuration, actionCancellationToken);
            },
            cancellationToken);
    }

    public async Task<AudioDuckingResult> RunWithProtectedActionAsync(
        IAudioSessionVolumeTarget target,
        Func<CancellationToken, Task> protectedAction,
        bool simulateFailure,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(protectedAction);
        return await RunCoreAsync(target, simulateFailure, protectedAction, cancellationToken);
    }

    private async Task<AudioDuckingResult> RunCoreAsync(
        IAudioSessionVolumeTarget target,
        bool simulateFailure,
        Func<CancellationToken, Task> protectedAction,
        CancellationToken cancellationToken)
    {
        AudioSessionVolumeState captured = target.ReadState();
        string? unsuitableReason = AudioDuckingSafety.GetUnsuitableReason(captured, _configuration);

        if (unsuitableReason is not null)
        {
            _log.Write($"AUDIO DUCKING REFUSED: {unsuitableReason}.");
            _log.Write("VOLUME COMMAND ISSUED: NO");
            return new AudioDuckingResult(false, false, false, false, false, false, unsuitableReason);
        }

        float duckVolume = _configuration.CalculateDuckVolume(captured.VolumeScalar);
        Guid eventContext = Guid.NewGuid();
        bool restorationArmed = false;
        bool volumeCommandMayHaveBeenIssued = false;
        bool duckVerified = false;
        bool restorationAttempted = false;
        bool restorationVerified = false;
        bool cancelled = false;
        bool simulatedFailureObserved = false;
        string? failureReason = null;

        _log.Write(
            $"ORIGINAL STATE CAPTURED: volume {captured.VolumeScalar:P1}; muted {DisplayBool(captured.Muted)}; " +
            $"session {target.StableSessionTag}.");
        _log.Write(
            $"DUCK TARGET: {duckVolume:P1} ({_configuration.DuckRatio:P0} of the captured starting volume).");

        try
        {
            cancellationToken.ThrowIfCancellationRequested();
            restorationArmed = true;

            _log.Write("DUCK STARTED: smoothly lowering only the selected Firefox audio session.");
            await RampVolumeAsync(
                target,
                captured.VolumeScalar,
                duckVolume,
                eventContext,
                cancellationToken,
                () => volumeCommandMayHaveBeenIssued = true);

            AudioSessionVolumeState ducked = target.ReadState();
            if (!StateMatches(ducked, duckVolume, captured.Muted))
            {
                throw new InvalidOperationException(
                    $"Windows did not verify the requested ducked state. Last state: {DescribeState(ducked)}.");
            }

            duckVerified = true;
            _log.Write($"DUCK VERIFIED: volume {ducked.VolumeScalar:P1}; muted {DisplayBool(ducked.Muted)}.");

            if (simulateFailure)
            {
                simulatedFailureObserved = true;
                _log.Write("SIMULATED FAILURE: deliberately triggering protected restoration now.");
                throw new SimulatedAudioDuckingException();
            }

            await protectedAction(cancellationToken);
        }
        catch (OperationCanceledException)
        {
            cancelled = true;
            failureReason = "Cancellation was requested.";
            _log.Write(
                restorationArmed
                    ? "AUDIO DUCKING CANCELLED: protected restoration will run before exit."
                    : "AUDIO DUCKING CANCELLED BEFORE ANY VOLUME COMMAND.");
        }
        catch (SimulatedAudioDuckingException exception)
        {
            failureReason = exception.Message;
            _log.Write("EXPECTED SIMULATED FAILURE: protected restoration is running.");
        }
        catch (Exception exception)
        {
            failureReason = exception.Message;
            _log.Warning("Audio ducking failed after restoration protection was armed", exception);
        }
        finally
        {
            if (restorationArmed)
            {
                restorationAttempted = true;
                restorationVerified = await RestoreCapturedStateAsync(
                    target,
                    captured,
                    eventContext);
            }
        }

        return new AudioDuckingResult(
            duckVerified,
            restorationAttempted,
            restorationVerified,
            volumeCommandMayHaveBeenIssued,
            cancelled,
            simulatedFailureObserved,
            failureReason);
    }

    private async Task<bool> RestoreCapturedStateAsync(
        IAudioSessionVolumeTarget target,
        AudioSessionVolumeState captured,
        Guid eventContext)
    {
        _log.Write(
            $"RESTORATION REQUESTED: exact captured volume {captured.VolumeScalar:P1}; " +
            $"muted {DisplayBool(captured.Muted)}; same session {target.StableSessionTag}.");

        try
        {
            AudioSessionVolumeState current = target.ReadState();
            float rampStart = current.Success ? current.VolumeScalar : captured.VolumeScalar;

            await RampVolumeAsync(
                target,
                rampStart,
                captured.VolumeScalar,
                eventContext,
                CancellationToken.None,
                commandIssued: null);

            // Make the exact captured scalar the final request even after floating-point interpolation.
            target.SetVolume(captured.VolumeScalar, eventContext);
            target.SetMute(captured.Muted, eventContext);

            AudioSessionVolumeState restored = target.ReadState();
            if (!StateMatches(restored, captured.VolumeScalar, captured.Muted)
                || restored.Activity is AudioSessionActivity.Expired or AudioSessionActivity.Unknown)
            {
                _log.Write($"RESTORATION FAILED VERIFICATION: {DescribeState(restored)}.");
                WriteManualRestoreWarning();
                return false;
            }

            _log.Write(
                $"RESTORATION VERIFIED: exact volume {restored.VolumeScalar:P1}; " +
                $"muted {DisplayBool(restored.Muted)}.");
            return true;
        }
        catch (Exception exception)
        {
            _log.Warning(
                "The original Firefox audio session disappeared or rejected exact volume restoration; no replacement session was selected",
                exception);
            WriteManualRestoreWarning();
            return false;
        }
    }

    private async Task RampVolumeAsync(
        IAudioSessionVolumeTarget target,
        float from,
        float to,
        Guid eventContext,
        CancellationToken cancellationToken,
        Action? commandIssued)
    {
        TimeSpan stepDelay = TimeSpan.FromTicks(_configuration.RampDuration.Ticks / _configuration.RampSteps);

        for (int step = 1; step <= _configuration.RampSteps; step++)
        {
            cancellationToken.ThrowIfCancellationRequested();
            float progress = step / (float)_configuration.RampSteps;
            float volume = from + ((to - from) * progress);
            commandIssued?.Invoke();
            target.SetVolume(Math.Clamp(volume, 0f, 1f), eventContext);

            if (stepDelay > TimeSpan.Zero && step < _configuration.RampSteps)
            {
                await Task.Delay(stepDelay, cancellationToken);
            }
        }
    }

    private bool StateMatches(
        AudioSessionVolumeState state,
        float expectedVolume,
        bool expectedMuted)
    {
        return state.Success
            && Math.Abs(state.VolumeScalar - expectedVolume) <= _configuration.VerificationTolerance
            && state.Muted == expectedMuted;
    }

    private static string DescribeState(AudioSessionVolumeState state)
    {
        return state.Success
            ? $"state {state.Activity}; volume {state.VolumeScalar:P1}; muted {DisplayBool(state.Muted)}"
            : $"unavailable ({state.ErrorMessage ?? "unknown error"})";
    }

    private static string DisplayBool(bool value) => value ? "Yes" : "No";

    private void WriteManualRestoreWarning()
    {
        _log.Write("!!! WARNING !!!");
        _log.Write("ZOYA RADIO COULD NOT VERIFY FIREFOX'S ORIGINAL PER-SESSION VOLUME.");
        _log.Write("PLEASE RESTORE FIREFOX IN WINDOWS VOLUME MIXER MANUALLY.");
        _log.Write("!!! WARNING !!!");
    }
}
