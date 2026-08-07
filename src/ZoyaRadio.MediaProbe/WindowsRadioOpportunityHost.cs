using Windows.Media.Control;

namespace ZoyaRadio.MediaProbe;

internal sealed class WindowsRadioOpportunityHost : IRadioOpportunityHost
{
    private readonly Func<GlobalSystemMediaTransportControlsSession?> _selectedSessionProvider;
    private readonly Func<RadioRuntimeState> _runtimeStateProvider;
    private readonly RadioSessionAccess _sessionAccess;
    private readonly WindowsSpeechService _speechService;
    private readonly IAnnouncementLog _log;

    public WindowsRadioOpportunityHost(
        Func<GlobalSystemMediaTransportControlsSession?> selectedSessionProvider,
        Func<RadioRuntimeState> runtimeStateProvider,
        RadioSessionAccess sessionAccess,
        WindowsSpeechService speechService,
        IAnnouncementLog log)
    {
        _selectedSessionProvider = selectedSessionProvider;
        _runtimeStateProvider = runtimeStateProvider;
        _sessionAccess = sessionAccess;
        _speechService = speechService;
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
        IPreparedAnnouncementSpeech prepared = await _speechService.PrepareAsync(text);

        if (cancellationToken.IsCancellationRequested)
        {
            prepared.Dispose();
            cancellationToken.ThrowIfCancellationRequested();
        }

        return prepared;
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
}
