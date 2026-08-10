using Windows.Media.Control;

namespace ZoyaRadio.MediaProbe;

internal sealed class WindowsRadioOpportunityHost : IRadioOpportunityHost, ITransitionRadioHost
{
    private readonly Func<GlobalSystemMediaTransportControlsSession?> _selectedSessionProvider;
    private readonly Func<RadioRuntimeState> _runtimeStateProvider;
    private readonly RadioSessionAccess _sessionAccess;
    private readonly IAnnouncementSpeechService _speechService;
    private readonly IRadioClock _clock;
    private readonly ITransitionAudioDucker _transitionDucker;
    private readonly IAnnouncementLog _log;

    public WindowsRadioOpportunityHost(
        Func<GlobalSystemMediaTransportControlsSession?> selectedSessionProvider,
        Func<RadioRuntimeState> runtimeStateProvider,
        RadioSessionAccess sessionAccess,
        IAnnouncementSpeechService speechService,
        IRadioClock clock,
        ITransitionAudioDucker transitionDucker,
        IAnnouncementLog log)
    {
        _selectedSessionProvider = selectedSessionProvider;
        _runtimeStateProvider = runtimeStateProvider;
        _sessionAccess = sessionAccess;
        _speechService = speechService;
        _clock = clock;
        _transitionDucker = transitionDucker;
        _log = log;
    }

    public async Task<RadioOpportunitySnapshot> ReadSnapshotAsync(
        long opportunityId,
        CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        RadioRuntimeState runtime = _runtimeStateProvider();
        bool sessionListReadSucceeded = _sessionAccess.TryReadExactFirefoxMatches(
            out IReadOnlyList<GlobalSystemMediaTransportControlsSession> matches);
        GlobalSystemMediaTransportControlsSession? selectedSession = _selectedSessionProvider();
        bool sameSelectedSession = matches.Count == 1
            && selectedSession is not null
            && ReferenceEquals(matches[0], selectedSession);

        RadioSessionDetails details = selectedSession is null
            ? RadioSessionDetails.Unavailable
            : await _sessionAccess.ReadSessionDetailsAsync(selectedSession);

        return new RadioOpportunitySnapshot(
            runtime.Armed,
            runtime.SilentMode,
            runtime.PendingOpportunityId == opportunityId,
            sessionListReadSucceeded,
            matches.Count,
            sameSelectedSession,
            details.SourceId,
            details.SourceReadSucceeded,
            details.Media,
            details.MediaReadSucceeded,
            details.PlaybackStatus,
            details.PlaybackReadSucceeded);
    }

    public async Task<IPreparedAnnouncementSpeech> PrepareSpeechAsync(
        string text,
        CancellationToken cancellationToken)
    {
        return await _speechService.PrepareAsync(text, cancellationToken);
    }

    public async Task<AnnouncementSequenceResult> RunAnnouncementAsync(
        IPreparedAnnouncementSpeech preparedSpeech,
        TimeSpan maximumSpeechDuration,
        CancellationToken cancellationToken)
    {
        GlobalSystemMediaTransportControlsSession? selectedSession = _selectedSessionProvider();
        if (selectedSession is null)
        {
            throw new InvalidOperationException("The selected Firefox session is unavailable.");
        }

        var target = new WindowsAnnouncementPlaybackTarget(selectedSession, _log);
        AnnouncementSequenceTimings timings = AnnouncementSequenceTimings.Production with
        {
            SpeechTimeout = maximumSpeechDuration,
        };
        var sequence = new AnnouncementControlSequence(_log, timings);
        return await sequence.RunAsync(
            target,
            preparedSpeech,
            simulateSpeechFailure: false,
            cancellationToken);
    }

    public async Task<TransitionDuckingResult> RunDuckedAnnouncementAsync(
        RadioTransitionDelivery transition,
        IPreparedAnnouncementSpeech preparedSpeech,
        TimeSpan maximumSpeechDuration,
        DateTimeOffset speechStartDeadline,
        bool simulatePostDuckFailure,
        CancellationToken cancellationToken)
    {
        if (_clock.Now > speechStartDeadline)
        {
            return new TransitionDuckingResult(
                TransitionDuckingStatus.Refused,
                "the transition delivery window expired before Core Audio targeting began",
                null);
        }

        return await _transitionDucker.RunAsync(
            async actionCancellationToken =>
            {
                RadioOpportunitySnapshot finalSnapshot = await ReadSnapshotAsync(
                    transition.OpportunityId,
                    actionCancellationToken);
                TimeSpan deliveryWindow = speechStartDeadline - transition.TransitionObservedAt;
                RadioSafetyDecision finalSafety = TransitionDeliverySafety.Evaluate(
                    transition,
                    finalSnapshot,
                    _clock.Now,
                    deliveryWindow);

                if (!finalSafety.CanProceed)
                {
                    throw new InvalidOperationException(
                        $"Prepared speech became stale after ducking: {finalSafety.Reason}.");
                }

                if (_clock.Now > speechStartDeadline)
                {
                    throw new InvalidOperationException(
                        "Prepared speech could not begin inside the transition delivery window.");
                }

                _log.Write($"SPEECH STARTED: {_clock.Now:O}");
                SpeechPlaybackResult speech = await preparedSpeech.SpeakAsync(
                    maximumSpeechDuration,
                    actionCancellationToken);

                switch (speech.Status)
                {
                    case SpeechPlaybackStatus.Completed:
                        _log.Write($"SPEECH COMPLETED: {_clock.Now:O}");
                        return;
                    case SpeechPlaybackStatus.Cancelled:
                        _log.Write("SPEECH CANCELLED: protected exact-volume restoration is running.");
                        throw new OperationCanceledException(actionCancellationToken);
                    case SpeechPlaybackStatus.TimedOut:
                        _log.Write("SPEECH TIMEOUT: protected exact-volume restoration is running.");
                        throw new InvalidOperationException("Prepared speech exceeded its bounded duration.");
                    default:
                        _log.Write("SPEECH FAILED: protected exact-volume restoration is running.");
                        throw new InvalidOperationException(
                            speech.ErrorMessage ?? "Prepared speech playback failed.");
                }
            },
            simulatePostDuckFailure,
            cancellationToken,
            speechStartDeadline);
    }
}
