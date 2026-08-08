namespace ZoyaRadio.MediaProbe;

internal sealed class AudioDuckingTest
{
    private readonly LocalLog _log;
    private readonly AudioDuckingConfiguration _configuration;

    public AudioDuckingTest(
        LocalLog log,
        AudioDuckingConfiguration? configuration = null)
    {
        _log = log;
        _configuration = configuration ?? AudioDuckingConfiguration.Diagnostic;
    }

    public async Task<int> RunAsync(
        bool enumerateOnly,
        bool simulateFailure,
        CancellationToken cancellationToken)
    {
        AudioSessionEnumeration enumeration;

        try
        {
            _log.Write("Enumerating audio sessions on the default Windows multimedia output device...");
            enumeration = WindowsAudioSessionCatalog.Enumerate(_log);
        }
        catch (Exception exception)
        {
            _log.Warning("Windows audio-session enumeration failed", exception);
            _log.Write("VOLUME COMMAND ISSUED: NO");
            _log.Write("AUDIO DUCKING TEST RESULT: NOT RUN");
            return 1;
        }

        try
        {
            _log.Write($"Default output endpoint tag: {enumeration.EndpointTag}");
            _log.Write($"Windows audio sessions found: {enumeration.Sessions.Count}");

            for (int index = 0; index < enumeration.Sessions.Count; index++)
            {
                LogSession(index + 1, enumeration.Sessions[index]);
            }

            AudioSessionSelection selection = AudioSessionSelector.SelectFirefox(enumeration.Sessions);

            if (selection.Status == AudioSessionSelectionStatus.NoFirefoxCandidate)
            {
                _log.Write("NO ACTIVE FIREFOX AUDIO SESSION: no volume command will be issued.");
                _log.Write("Start YouTube Music playing in Firefox, then run this test again.");
                _log.Write("VOLUME COMMAND ISSUED: NO");
                _log.Write("AUDIO DUCKING TEST RESULT: NOT RUN");
                return 0;
            }

            if (selection.Status == AudioSessionSelectionStatus.Ambiguous)
            {
                _log.Write("!!! AMBIGUOUS FIREFOX AUDIO TARGET !!!");
                _log.Write($"Active Firefox candidates: {selection.FirefoxCandidates.Count}");

                foreach (AudioSessionDescriptor candidate in selection.FirefoxCandidates)
                {
                    _log.Write(
                        $"Candidate session {candidate.Target.StableSessionTag}; PID {DisplayProcessId(candidate.Target.ProcessId)}; " +
                        $"volume {DisplayVolume(candidate.InitialState)}; muted {DisplayMute(candidate.InitialState)}.");
                }

                _log.Write("Zoya Radio will not guess which Firefox audio session contains YouTube Music.");
                _log.Write("VOLUME COMMAND ISSUED: NO");
                _log.Write("AUDIO DUCKING TEST RESULT: REFUSED SAFELY");
                return 0;
            }

            IAudioSessionVolumeTarget target = selection.Target!;
            AudioSessionVolumeState current = target.ReadState();
            string? unsuitableReason = AudioDuckingSafety.GetUnsuitableReason(current, _configuration);

            _log.Write("EXACTLY ONE ACTIVE FIREFOX AUDIO CANDIDATE SELECTED");
            _log.Write(
                $"TARGET: process {DisplayProcessName(target.ProcessName)}; PID {DisplayProcessId(target.ProcessId)}; " +
                $"session {target.StableSessionTag}; state {current.Activity}; volume {DisplayVolume(current)}; " +
                $"muted {DisplayMute(current)}.");

            if (unsuitableReason is not null)
            {
                _log.Write($"AUDIO DUCKING REFUSED: {unsuitableReason}.");
                _log.Write("The probe will not unmute Firefox or increase a near-silent starting volume.");
                _log.Write("VOLUME COMMAND ISSUED: NO");
                _log.Write("AUDIO DUCKING TEST RESULT: NOT RUN");
                return 0;
            }

            if (enumerateOnly)
            {
                _log.Write("READ-ONLY ENUMERATION COMPLETE: no volume command was issued.");
                _log.Write("VOLUME COMMAND ISSUED: NO");
                return 0;
            }

            float targetVolume = _configuration.CalculateDuckVolume(current.VolumeScalar);
            _log.Write("!!! THIS TEST WILL TEMPORARILY CHANGE ONLY FIREFOX'S PER-SESSION VOLUME !!!");
            _log.Write(
                $"Proposed change: smoothly lower Firefox from {current.VolumeScalar:P1} to about {targetVolume:P1}, " +
                $"hold for about {_configuration.HoldDuration.TotalSeconds:0.#} seconds, then restore exactly {current.VolumeScalar:P1}.");
            Console.Write("Temporarily duck this exact Firefox audio session? [y/N] ");
            string? response = Console.ReadLine();

            if (!PlaybackControlRules.IsAffirmative(response))
            {
                _log.Write("USER CONFIRMATION: NO - test cancelled safely.");
                _log.Write("VOLUME COMMAND ISSUED: NO");
                _log.Write("AUDIO DUCKING TEST RESULT: CANCELLED BEFORE CONTROL");
                return 0;
            }

            _log.Write("USER CONFIRMATION: YES");
            cancellationToken.ThrowIfCancellationRequested();

            var sequence = new AudioDuckingSequence(_log, _configuration);
            AudioDuckingResult result = await sequence.RunAsync(
                target,
                simulateFailure,
                cancellationToken);

            if (result.TestPassed)
            {
                _log.Write("AUDIO DUCKING TEST RESULT: PASS");
                return 0;
            }

            if (result.ExpectedFailureRecoveryPassed)
            {
                _log.Write("SIMULATED-FAILURE RECOVERY: PASS");
                _log.Write("AUDIO DUCKING TEST RESULT: EXPECTED FAILURE; RESTORATION PASS");
                return 0;
            }

            if (result.Cancelled && result.RestorationVerified)
            {
                _log.Write("CANCELLATION RECOVERY: PASS");
                _log.Write("AUDIO DUCKING TEST RESULT: CANCELLED; RESTORATION PASS");
                return 0;
            }

            if (result.Cancelled && !result.VolumeCommandMayHaveBeenIssued)
            {
                _log.Write("VOLUME COMMAND ISSUED: NO");
                _log.Write("AUDIO DUCKING TEST RESULT: CANCELLED BEFORE CONTROL");
                return 0;
            }

            _log.Write(
                $"AUDIO DUCKING TEST RESULT: FAIL - {result.FailureReason ?? "duck or restoration was not verified"}.");
            return 1;
        }
        catch (OperationCanceledException)
        {
            _log.Write("AUDIO DUCKING CANCELLED BEFORE ANY VOLUME COMMAND.");
            _log.Write("VOLUME COMMAND ISSUED: NO");
            return 0;
        }
        finally
        {
            foreach (AudioSessionDescriptor session in enumeration.Sessions)
            {
                session.Target.Dispose();
            }
        }
    }

    private void LogSession(int number, AudioSessionDescriptor session)
    {
        _log.Write($"AUDIO SESSION {number}");
        _log.Write($"  Session tag: {session.Target.StableSessionTag}");
        _log.Write($"  State: {session.InitialState.Activity}");
        _log.Write($"  Process ID: {DisplayProcessId(session.Target.ProcessId)}");
        _log.Write($"  Process: {DisplayProcessName(session.Target.ProcessName)}");
        _log.Write($"  Per-session volume: {DisplayVolume(session.InitialState)}");
        _log.Write($"  Muted: {DisplayMute(session.InitialState)}");

        if (!session.InitialState.Success)
        {
            _log.Write($"  Read warning: {session.InitialState.ErrorMessage ?? "unavailable"}");
        }
    }

    private static string DisplayProcessId(int? processId) => processId?.ToString() ?? "<not available>";

    private static string DisplayProcessName(string? processName)
    {
        if (string.IsNullOrWhiteSpace(processName))
        {
            return "<not available>";
        }

        return processName.EndsWith(".exe", StringComparison.OrdinalIgnoreCase)
            ? processName
            : $"{processName}.exe";
    }

    private static string DisplayVolume(AudioSessionVolumeState state) =>
        state.Success ? state.VolumeScalar.ToString("P1") : "<not available>";

    private static string DisplayMute(AudioSessionVolumeState state) =>
        state.Success ? (state.Muted ? "Yes" : "No") : "<not available>";
}
