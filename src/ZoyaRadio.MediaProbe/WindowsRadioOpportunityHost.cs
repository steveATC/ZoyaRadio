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

    public Task<RadioOpportunitySnapshot> ReadSnapshotAsync(
        long opportunityId,
        CancellationToken cancellationToken) =>
        ReadSnapshotCoreAsync(opportunityId, requirePendingOpportunity: true, cancellationToken);

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

    public Task<TransitionDuckingResult> RunDuckedAnnouncementAsync(
        RadioTransitionDelivery transition,
        IPreparedAnnouncementSpeech preparedSpeech,
        TimeSpan maximumSpeechDuration,
        DateTimeOffset speechStartDeadline,
        bool simulatePostDuckFailure,
        CancellationToken cancellationToken) =>
        RunDuckedSpeechAsync(
            preparedSpeech,
            maximumSpeechDuration,
            speechStartDeadline,
            simulatePostDuckFailure,
            async actionCancellationToken =>
            {
                RadioOpportunitySnapshot snapshot = await ReadSnapshotCoreAsync(
                    transition.OpportunityId,
                    requirePendingOpportunity: true,
                    actionCancellationToken);
                TimeSpan deliveryWindow = speechStartDeadline - transition.TransitionObservedAt;
                return TransitionDeliverySafety.Evaluate(
                    transition,
                    snapshot,
                    _clock.Now,
                    deliveryWindow);
            },
            cancellationToken);

    public Task<TransitionDuckingResult> RunDuckedLinerAsync(
        ReusableLinerTransition transition,
        IPreparedAnnouncementSpeech preparedSpeech,
        TimeSpan maximumSpeechDuration,
        DateTimeOffset speechStartDeadline,
        CancellationToken cancellationToken) =>
        RunDuckedSpeechAsync(
            preparedSpeech,
            maximumSpeechDuration,
            speechStartDeadline,
            simulatePostDuckFailure: false,
            async actionCancellationToken =>
            {
                RadioOpportunitySnapshot snapshot = await ReadSnapshotCoreAsync(
                    transition.TransitionSequence,
                    requirePendingOpportunity: false,
                    actionCancellationToken);
                TimeSpan deliveryWindow = speechStartDeadline - transition.ObservedAt;
                return ReusableLinerDeliverySafety.Evaluate(
                    transition,
                    snapshot,
                    _clock.Now,
                    deliveryWindow);
            },
            cancellationToken);

    private async Task<RadioOpportunitySnapshot> ReadSnapshotCoreAsync(
        long opportunityId,
        bool requirePendingOpportunity,
        CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        RadioRuntimeState runtime = _runtimeStateProvider();
        bool sessionListReadSucceeded = _sessionAccess.TryReadExactFirefoxMatches(
            out IReadOnlyList<GlobalSystemMediaTransportControlsSession> matches);
        GlobalSystemMediaTransportControlsSession? selectedSession = _selectedSessionProvider();
        RadioSessionDetails details = selectedSession is null
            ? RadioSessionDetails.Unavailable
            : await _sessionAccess.ReadSessionDetailsAsync(selectedSession);
        bool sameSelectedSession = false;

        if (sessionListReadSucceeded
            && matches.Count == 1
            && selectedSession is not null)
        {
            GlobalSystemMediaTransportControlsSession candidate = matches[0];
            bool sameWrapper = ReferenceEquals(candidate, selectedSession);
            RadioSessionDetails candidateDetails = sameWrapper
                ? details
                : await _sessionAccess.ReadSessionDetailsAsync(candidate);
            FirefoxSessionContinuityDecision continuity = FirefoxSessionContinuity.Evaluate(
                sameWrapper,
                FirefoxSessionContinuityEvidence.FromDetails(details),
                FirefoxSessionContinuityEvidence.FromDetails(candidateDetails));
            sameSelectedSession = continuity.PreservesPendingAndActiveWork;
        }

        bool opportunityMatches = !requirePendingOpportunity
            || runtime.PendingOpportunityId == opportunityId;
        return new RadioOpportunitySnapshot(
            runtime.Armed,
            runtime.SilentMode,
            opportunityMatches,
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

    private async Task<TransitionDuckingResult> RunDuckedSpeechAsync(
        IPreparedAnnouncementSpeech preparedSpeech,
        TimeSpan maximumSpeechDuration,
        DateTimeOffset speechStartDeadline,
        bool simulatePostDuckFailure,
        Func<CancellationToken, Task<RadioSafetyDecision>> finalSafety,
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
                RadioSafetyDecision safety = await finalSafety(actionCancellationToken);
                if (!safety.CanProceed)
                {
                    throw new InvalidOperationException(
                        $"Prepared speech became stale after ducking: {safety.Reason}.");
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
