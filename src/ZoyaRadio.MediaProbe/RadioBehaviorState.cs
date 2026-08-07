namespace ZoyaRadio.MediaProbe;

internal sealed record RadioCommentContext(
    MediaSnapshot PreviousTrack,
    MediaSnapshot CurrentTrack,
    DateTimeOffset CurrentTrackStartedAt,
    IReadOnlyList<string> TrustedListeningObservations,
    IReadOnlyList<string> RecentAcceptedAiComments);

internal sealed record RadioCommentOpportunity(long Id, RadioCommentContext Context);

internal enum RadioTrackObservationKind
{
    Duplicate,
    Unusable,
    Transition,
}

internal sealed record RadioTrackObservation(
    RadioTrackObservationKind Kind,
    MediaSnapshot? PreviousTrack,
    MediaSnapshot? CurrentTrack,
    int TransitionCounter,
    int ScheduledThreshold,
    int TotalObservedTransitions,
    bool PendingOpportunityInvalidated,
    RadioCommentOpportunity? Opportunity,
    IReadOnlyList<string> TrustedListeningObservations);

internal sealed record RadioModeChange(
    bool SilentMode,
    bool PendingOpportunityInvalidated,
    int ScheduledThreshold);

internal sealed record RadioRuntimeState(
    bool Armed,
    bool SilentMode,
    MediaSnapshot CurrentTrack,
    DateTimeOffset CurrentTrackStartedAt,
    long? PendingOpportunityId);

internal sealed class RadioBehaviorState
{
    private readonly RadioBehaviorConfiguration _configuration;
    private readonly ITransitionTargetSource _targetSource;
    private readonly RadioHistory _history;
    private MediaSnapshot _currentTrack = MediaSnapshot.Empty;
    private DateTimeOffset _currentTrackStartedAt;
    private long _nextOpportunityId;
    private RadioCommentOpportunity? _pendingOpportunity;

    public RadioBehaviorState(
        RadioBehaviorConfiguration configuration,
        ITransitionTargetSource targetSource)
    {
        configuration.Validate();
        _configuration = configuration;
        _targetSource = targetSource;
        _history = new RadioHistory(configuration.RecentTrackLimit, configuration.RecentCommentLimit);
    }

    public bool Armed { get; private set; }

    public bool SilentMode { get; private set; }

    public int TransitionCounter { get; private set; }

    public int ScheduledThreshold { get; private set; }

    public int TotalObservedTransitions { get; private set; }

    public IReadOnlyList<MediaSnapshot> RecentTracks => _history.Tracks;

    public IReadOnlyList<string> RecentAcceptedAiComments => _history.AcceptedAiComments;

    public void Arm(MediaSnapshot initialTrack, DateTimeOffset observedAt)
    {
        EnsureUsableTrack(initialTrack);
        Armed = true;
        SilentMode = false;
        TransitionCounter = 0;
        TotalObservedTransitions = 0;
        _pendingOpportunity = null;
        _currentTrack = initialTrack;
        _currentTrackStartedAt = observedAt;
        _history.Clear();
        _history.AddTrack(initialTrack);
        ChooseFreshSchedule();
    }

    public void Disarm()
    {
        Armed = false;
        _pendingOpportunity = null;
    }

    public RadioTrackObservation ObserveTrack(MediaSnapshot media, DateTimeOffset observedAt)
    {
        if (!HasUsableTitleAndArtist(media))
        {
            return CreateObservation(RadioTrackObservationKind.Unusable, null, null, false, null, []);
        }

        if (HasSameNormalizedTrackIdentity(_currentTrack, media))
        {
            return CreateObservation(RadioTrackObservationKind.Duplicate, null, _currentTrack, false, null, []);
        }

        MediaSnapshot previous = _currentTrack;
        _currentTrack = media;
        _currentTrackStartedAt = observedAt;
        TotalObservedTransitions++;
        _history.AddTrack(media);
        IReadOnlyList<string> observations = _history.BuildTrustedObservations(media);

        if (_pendingOpportunity is not null)
        {
            _pendingOpportunity = null;
            ChooseFreshSchedule();
            return CreateObservation(
                RadioTrackObservationKind.Transition,
                previous,
                media,
                true,
                null,
                observations);
        }

        if (!Armed || SilentMode)
        {
            return CreateObservation(
                RadioTrackObservationKind.Transition,
                previous,
                media,
                false,
                null,
                observations);
        }

        TransitionCounter++;
        RadioCommentOpportunity? opportunity = null;

        if (TransitionCounter >= ScheduledThreshold && HasUsableTitleAndArtist(previous))
        {
            var context = new RadioCommentContext(
                previous,
                media,
                observedAt,
                observations,
                _history.AcceptedAiComments);
            opportunity = new RadioCommentOpportunity(++_nextOpportunityId, context);
            _pendingOpportunity = opportunity;
        }

        return CreateObservation(
            RadioTrackObservationKind.Transition,
            previous,
            media,
            false,
            opportunity,
            observations);
    }

    public RadioModeChange SetSilentMode(bool silent)
    {
        if (SilentMode == silent)
        {
            return new RadioModeChange(SilentMode, false, ScheduledThreshold);
        }

        bool invalidated = _pendingOpportunity is not null;
        SilentMode = silent;
        _pendingOpportunity = null;
        TransitionCounter = 0;

        if (!silent)
        {
            ChooseFreshSchedule();
        }

        return new RadioModeChange(SilentMode, invalidated, ScheduledThreshold);
    }

    public bool ClearRecentHistory()
    {
        bool invalidated = _pendingOpportunity is not null;
        _history.Clear();
        _pendingOpportunity = null;
        ChooseFreshSchedule();
        return invalidated;
    }

    public bool CancelPendingOpportunityAndReschedule()
    {
        if (_pendingOpportunity is null)
        {
            return false;
        }

        _pendingOpportunity = null;
        ChooseFreshSchedule();
        return true;
    }

    public bool ResolveOpportunity(
        long opportunityId,
        bool announcementCompleted,
        string? acceptedAiComment)
    {
        if (_pendingOpportunity?.Id != opportunityId)
        {
            return false;
        }

        if (announcementCompleted && acceptedAiComment is not null)
        {
            _history.AddAcceptedAiComment(acceptedAiComment);
        }

        _pendingOpportunity = null;
        ChooseFreshSchedule();
        return true;
    }

    public RadioRuntimeState GetRuntimeState()
    {
        return new RadioRuntimeState(
            Armed,
            SilentMode,
            _currentTrack,
            _currentTrackStartedAt,
            _pendingOpportunity?.Id);
    }

    private void ChooseFreshSchedule()
    {
        TransitionCounter = 0;
        ScheduledThreshold = _targetSource.NextInclusive(
            _configuration.MinimumTransitionsBetweenComments,
            _configuration.MaximumTransitionsBetweenComments);

        if (ScheduledThreshold < _configuration.MinimumTransitionsBetweenComments
            || ScheduledThreshold > _configuration.MaximumTransitionsBetweenComments)
        {
            throw new InvalidOperationException("The transition target source returned an out-of-range value.");
        }
    }

    private RadioTrackObservation CreateObservation(
        RadioTrackObservationKind kind,
        MediaSnapshot? previous,
        MediaSnapshot? current,
        bool invalidated,
        RadioCommentOpportunity? opportunity,
        IReadOnlyList<string> observations)
    {
        return new RadioTrackObservation(
            kind,
            previous,
            current,
            TransitionCounter,
            ScheduledThreshold,
            TotalObservedTransitions,
            invalidated,
            opportunity,
            observations);
    }

    private static bool HasUsableTitleAndArtist(MediaSnapshot media)
    {
        return media.Title is not null && media.Artist is not null;
    }

    private static bool HasSameNormalizedTrackIdentity(MediaSnapshot left, MediaSnapshot right)
    {
        return RadioHistory.SameNormalizedText(left.Title, right.Title)
            && RadioHistory.SameNormalizedText(left.Artist, right.Artist);
    }

    private static void EnsureUsableTrack(MediaSnapshot media)
    {
        if (!HasUsableTitleAndArtist(media))
        {
            throw new ArgumentException("A usable title and artist are required.", nameof(media));
        }
    }
}
