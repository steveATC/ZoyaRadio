namespace ZoyaRadio.MediaProbe;

internal sealed record RadioCommentContext(
    MediaSnapshot PreviousTrack,
    MediaSnapshot CurrentTrack,
    DateTimeOffset CurrentTrackStartedAt,
    IReadOnlyList<string> TrustedListeningObservations,
    IReadOnlyList<string> RecentAcceptedAiComments);

internal sealed record RadioCommentOpportunity(long Id, RadioCommentContext Context);

internal sealed record RadioPreparationContext(
    MediaSnapshot SubjectTrack,
    DateTimeOffset SubjectTrackStartedAt,
    IReadOnlyList<string> TrustedListeningObservations,
    IReadOnlyList<string> RecentAcceptedAiComments);

internal sealed record RadioPreparationOpportunity(long Id, RadioPreparationContext Context);

internal sealed record RadioTransitionDelivery(
    long OpportunityId,
    MediaSnapshot CommentedTrack,
    MediaSnapshot NewTrack,
    DateTimeOffset TransitionObservedAt);

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
    bool PreparationMissedTransition,
    RadioPreparationOpportunity? PreparationOpportunity,
    RadioTransitionDelivery? Delivery,
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
    private RadioPreparationOpportunity? _pendingOpportunity;
    private bool _preparationReady;
    private bool _deliveryTransitionObserved;

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
        _preparationReady = false;
        _deliveryTransitionObserved = false;
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
        _preparationReady = false;
        _deliveryTransitionObserved = false;
    }

    public RadioTrackObservation ObserveTrack(MediaSnapshot media, DateTimeOffset observedAt)
    {
        if (!HasUsableTitleAndArtist(media))
        {
            return CreateObservation(RadioTrackObservationKind.Unusable, null, null, false, false, null, null, []);
        }

        if (HasSameNormalizedTrackIdentity(_currentTrack, media))
        {
            return CreateObservation(RadioTrackObservationKind.Duplicate, null, _currentTrack, false, false, null, null, []);
        }

        MediaSnapshot previous = _currentTrack;
        _currentTrack = media;
        _currentTrackStartedAt = observedAt;
        TotalObservedTransitions++;
        _history.AddTrack(media);
        IReadOnlyList<string> observations = _history.BuildTrustedObservations(media);

        if (_pendingOpportunity is not null)
        {
            if (!_deliveryTransitionObserved)
            {
                if (!_preparationReady)
                {
                    _pendingOpportunity = null;
                    _preparationReady = false;
                    ChooseFreshSchedule();
                    return CreateObservation(
                        RadioTrackObservationKind.Transition,
                        previous,
                        media,
                        true,
                        true,
                        null,
                        null,
                        observations);
                }

                _deliveryTransitionObserved = true;
                var delivery = new RadioTransitionDelivery(
                    _pendingOpportunity.Id,
                    _pendingOpportunity.Context.SubjectTrack,
                    media,
                    observedAt);
                return CreateObservation(
                    RadioTrackObservationKind.Transition,
                    previous,
                    media,
                    false,
                    false,
                    null,
                    delivery,
                    observations);
            }

            _pendingOpportunity = null;
            _preparationReady = false;
            _deliveryTransitionObserved = false;
            ChooseFreshSchedule();
            return CreateObservation(
                RadioTrackObservationKind.Transition,
                previous,
                media,
                true,
                false,
                null,
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
                false,
                null,
                null,
                observations);
        }

        TransitionCounter++;
        RadioPreparationOpportunity? preparation = null;

        if (TransitionCounter == ScheduledThreshold - 1 && HasUsableTitleAndArtist(media))
        {
            var context = new RadioPreparationContext(
                media,
                observedAt,
                observations,
                _history.AcceptedAiComments);
            preparation = new RadioPreparationOpportunity(++_nextOpportunityId, context);
            _pendingOpportunity = preparation;
            _preparationReady = false;
            _deliveryTransitionObserved = false;
        }

        return CreateObservation(
            RadioTrackObservationKind.Transition,
            previous,
            media,
            false,
            false,
            preparation,
            null,
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
        _preparationReady = false;
        _deliveryTransitionObserved = false;
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
        _preparationReady = false;
        _deliveryTransitionObserved = false;
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
        _preparationReady = false;
        _deliveryTransitionObserved = false;
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
        _preparationReady = false;
        _deliveryTransitionObserved = false;
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

    public bool MarkPreparationReady(long opportunityId)
    {
        if (_pendingOpportunity?.Id != opportunityId || _deliveryTransitionObserved)
        {
            return false;
        }

        _preparationReady = true;
        return true;
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
        bool preparationMissedTransition,
        RadioPreparationOpportunity? preparation,
        RadioTransitionDelivery? delivery,
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
            preparationMissedTransition,
            preparation,
            delivery,
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
