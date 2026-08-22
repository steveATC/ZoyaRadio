namespace ZoyaRadio.MediaProbe;

internal sealed record OpportunityKernelConfiguration(
    int MinimumTransitionsBetweenComments,
    int MaximumTransitionsBetweenComments,
    int RecentTrackLimit,
    int RecentCommentLimit)
{
    public static OpportunityKernelConfiguration FromRadioBehavior(
        RadioBehaviorConfiguration configuration)
    {
        ArgumentNullException.ThrowIfNull(configuration);
        configuration.Validate();

        return new OpportunityKernelConfiguration(
            configuration.MinimumTransitionsBetweenComments,
            configuration.MaximumTransitionsBetweenComments,
            configuration.RecentTrackLimit,
            configuration.RecentCommentLimit);
    }

    public void Validate()
    {
        if (MinimumTransitionsBetweenComments is < 2 or > 50)
        {
            throw new InvalidOperationException(
                "MinimumTransitionsBetweenComments must be between 2 and 50 so preparation can begin one transition ahead.");
        }

        if (MaximumTransitionsBetweenComments < MinimumTransitionsBetweenComments
            || MaximumTransitionsBetweenComments > 50)
        {
            throw new InvalidOperationException(
                "MaximumTransitionsBetweenComments must be at least the minimum and no greater than 50.");
        }

        if (RecentTrackLimit is < 1 or > 100)
        {
            throw new InvalidOperationException("RecentTrackLimit must be between 1 and 100.");
        }

        if (RecentCommentLimit is < 1 or > 50)
        {
            throw new InvalidOperationException("RecentCommentLimit must be between 1 and 50.");
        }
    }
}

internal sealed record OpportunityKernelPreparation(
    long Id,
    MediaSnapshot SubjectTrack,
    DateTimeOffset SubjectTrackStartedAt,
    IReadOnlyList<string> TrustedListeningObservations,
    IReadOnlyList<string> RecentAcceptedAiComments);

internal sealed record OpportunityKernelDelivery(
    long OpportunityId,
    MediaSnapshot CommentedTrack,
    MediaSnapshot NewTrack,
    DateTimeOffset TransitionObservedAt);

internal sealed record OpportunityKernelState(
    bool Armed,
    bool SilentMode,
    MediaSnapshot CurrentTrack,
    DateTimeOffset CurrentTrackStartedAt,
    int TransitionCounter,
    int ScheduledThreshold,
    int TotalObservedTransitions,
    long NextOpportunityId,
    OpportunityKernelPreparation? PendingOpportunity,
    bool PreparationReady,
    bool DeliveryTransitionObserved,
    IReadOnlyList<MediaSnapshot> RecentTracks,
    IReadOnlyList<string> RecentAcceptedAiComments)
{
    public static OpportunityKernelState Initial { get; } = new(
        Armed: false,
        SilentMode: false,
        CurrentTrack: MediaSnapshot.Empty,
        CurrentTrackStartedAt: default,
        TransitionCounter: 0,
        ScheduledThreshold: 0,
        TotalObservedTransitions: 0,
        NextOpportunityId: 0,
        PendingOpportunity: null,
        PreparationReady: false,
        DeliveryTransitionObserved: false,
        RecentTracks: Array.Empty<MediaSnapshot>(),
        RecentAcceptedAiComments: Array.Empty<string>());
}

internal enum OpportunityKernelResolutionStatus
{
    Completed,
    Abandoned,
    Failed,
}

internal abstract record OpportunityKernelInput;

internal sealed record OpportunityKernelArm(
    MediaSnapshot InitialTrack,
    DateTimeOffset ObservedAt,
    int FreshTransitionTarget) : OpportunityKernelInput;

internal sealed record OpportunityKernelObserveTrack(
    MediaSnapshot Track,
    DateTimeOffset ObservedAt,
    int? FreshTransitionTarget = null) : OpportunityKernelInput;

internal sealed record OpportunityKernelMarkPreparationReady(long OpportunityId)
    : OpportunityKernelInput;

internal sealed record OpportunityKernelResolveOpportunity(
    long OpportunityId,
    OpportunityKernelResolutionStatus Status,
    string? AcceptedAiComment,
    int? FreshTransitionTarget = null) : OpportunityKernelInput;

internal sealed record OpportunityKernelSetSilentMode(
    bool Silent,
    int? FreshTransitionTarget = null) : OpportunityKernelInput;

internal sealed record OpportunityKernelClearRecentHistory(int FreshTransitionTarget)
    : OpportunityKernelInput;

internal sealed record OpportunityKernelCancelPendingOpportunity(int? FreshTransitionTarget = null)
    : OpportunityKernelInput;

internal sealed record OpportunityKernelRebindCurrentTrack(
    MediaSnapshot Track,
    DateTimeOffset ObservedAt,
    int FreshTransitionTarget) : OpportunityKernelInput;

internal sealed record OpportunityKernelDisarm : OpportunityKernelInput;

internal enum OpportunityKernelDecisionReason
{
    Armed,
    Disarmed,
    UnusableTrackIgnored,
    DuplicateTrackIgnored,
    TransitionObserved,
    TransitionCounted,
    PreparationRequested,
    PreparationMarkedReady,
    PreparationMissedTransition,
    DeliveryDue,
    RapidTransitionInvalidatedDelivery,
    PendingOpportunityInvalidated,
    OpportunityResolvedCompleted,
    OpportunityResolvedAbandoned,
    OpportunityResolvedFailed,
    StaleOpportunityRefused,
    SilentModeEnabled,
    SilentModeDisabled,
    SilentModeUnchanged,
    RecentHistoryCleared,
    PendingOpportunityCancelled,
    NoPendingOpportunityToCancel,
    CurrentTrackRebound,
    FreshScheduleSelected,
}

internal sealed record OpportunityKernelDecision(
    OpportunityKernelDecisionReason Reason,
    long? OpportunityId = null,
    int? ScheduledThreshold = null,
    OpportunityKernelPreparation? Preparation = null,
    OpportunityKernelDelivery? Delivery = null,
    IReadOnlyList<string>? TrustedListeningObservations = null);

internal sealed record OpportunityKernelResult(
    OpportunityKernelState State,
    IReadOnlyList<OpportunityKernelDecision> Decisions);

internal sealed class DeterministicOpportunityKernel
{
    private readonly OpportunityKernelConfiguration _configuration;

    public DeterministicOpportunityKernel(OpportunityKernelConfiguration configuration)
    {
        ArgumentNullException.ThrowIfNull(configuration);
        configuration.Validate();
        _configuration = configuration;
    }

    public OpportunityKernelResult Apply(
        OpportunityKernelState state,
        OpportunityKernelInput input)
    {
        ArgumentNullException.ThrowIfNull(state);
        ArgumentNullException.ThrowIfNull(input);

        return input switch
        {
            OpportunityKernelArm arm => ApplyArm(state, arm),
            OpportunityKernelObserveTrack observation => ApplyObservation(state, observation),
            OpportunityKernelMarkPreparationReady ready => ApplyPreparationReady(state, ready),
            OpportunityKernelResolveOpportunity resolution => ApplyResolution(state, resolution),
            OpportunityKernelSetSilentMode silentMode => ApplySilentMode(state, silentMode),
            OpportunityKernelClearRecentHistory clear => ApplyClearRecentHistory(state, clear),
            OpportunityKernelCancelPendingOpportunity cancel => ApplyCancellation(state, cancel),
            OpportunityKernelRebindCurrentTrack rebind => ApplyRebind(state, rebind),
            OpportunityKernelDisarm => ApplyDisarm(state),
            _ => throw new ArgumentOutOfRangeException(nameof(input)),
        };
    }

    private OpportunityKernelResult ApplyArm(
        OpportunityKernelState state,
        OpportunityKernelArm input)
    {
        EnsureUsableTrack(input.InitialTrack);

        OpportunityKernelState armed = state with
        {
            Armed = true,
            SilentMode = false,
            CurrentTrack = input.InitialTrack,
            CurrentTrackStartedAt = input.ObservedAt,
            TransitionCounter = 0,
            TotalObservedTransitions = 0,
            PendingOpportunity = null,
            PreparationReady = false,
            DeliveryTransitionObserved = false,
            RecentTracks = [HistoryTrack(input.InitialTrack)],
            RecentAcceptedAiComments = Array.Empty<string>(),
        };

        var decisions = new List<OpportunityKernelDecision>
        {
            new(OpportunityKernelDecisionReason.Armed),
        };

        armed = SelectFreshSchedule(armed, input.FreshTransitionTarget, decisions);
        return new OpportunityKernelResult(armed, decisions);
    }

    private OpportunityKernelResult ApplyDisarm(OpportunityKernelState state)
    {
        var decisions = new List<OpportunityKernelDecision>();
        if (state.PendingOpportunity is not null)
        {
            decisions.Add(new OpportunityKernelDecision(
                OpportunityKernelDecisionReason.PendingOpportunityInvalidated,
                state.PendingOpportunity.Id));
        }

        decisions.Add(new OpportunityKernelDecision(OpportunityKernelDecisionReason.Disarmed));
        return new OpportunityKernelResult(
            ClearPending(state) with { Armed = false },
            decisions);
    }

    private OpportunityKernelResult ApplyObservation(
        OpportunityKernelState state,
        OpportunityKernelObserveTrack input)
    {
        if (!HasUsableTitleAndArtist(input.Track))
        {
            return SingleDecision(state, OpportunityKernelDecisionReason.UnusableTrackIgnored);
        }

        if (HasSameNormalizedTrackIdentity(state.CurrentTrack, input.Track))
        {
            return SingleDecision(state, OpportunityKernelDecisionReason.DuplicateTrackIgnored);
        }

        OpportunityKernelState transitioned = state with
        {
            CurrentTrack = input.Track,
            CurrentTrackStartedAt = input.ObservedAt,
            TotalObservedTransitions = state.TotalObservedTransitions + 1,
            RecentTracks = AddBounded(
                state.RecentTracks,
                HistoryTrack(input.Track),
                _configuration.RecentTrackLimit),
        };

        IReadOnlyList<string> trustedObservations = BuildTrustedObservations(
            transitioned.RecentTracks,
            input.Track);
        var decisions = new List<OpportunityKernelDecision>
        {
            new(
                OpportunityKernelDecisionReason.TransitionObserved,
                TrustedListeningObservations: trustedObservations),
        };

        if (transitioned.PendingOpportunity is not null)
        {
            return ApplyPendingObservation(
                transitioned,
                input,
                decisions);
        }

        if (!transitioned.Armed || transitioned.SilentMode)
        {
            return new OpportunityKernelResult(transitioned, decisions);
        }

        transitioned = transitioned with
        {
            TransitionCounter = transitioned.TransitionCounter + 1,
        };
        decisions.Add(new OpportunityKernelDecision(
            OpportunityKernelDecisionReason.TransitionCounted));

        if (transitioned.TransitionCounter == transitioned.ScheduledThreshold - 1)
        {
            long opportunityId = transitioned.NextOpportunityId + 1;
            var preparation = new OpportunityKernelPreparation(
                opportunityId,
                input.Track,
                input.ObservedAt,
                trustedObservations.ToArray(),
                transitioned.RecentAcceptedAiComments.ToArray());
            transitioned = transitioned with
            {
                NextOpportunityId = opportunityId,
                PendingOpportunity = preparation,
                PreparationReady = false,
                DeliveryTransitionObserved = false,
            };
            decisions.Add(new OpportunityKernelDecision(
                OpportunityKernelDecisionReason.PreparationRequested,
                opportunityId,
                Preparation: preparation));
        }

        return new OpportunityKernelResult(transitioned, decisions);
    }

    private OpportunityKernelResult ApplyPendingObservation(
        OpportunityKernelState state,
        OpportunityKernelObserveTrack input,
        List<OpportunityKernelDecision> decisions)
    {
        OpportunityKernelPreparation pending = state.PendingOpportunity!;

        if (!state.DeliveryTransitionObserved)
        {
            if (!state.PreparationReady)
            {
                decisions.Add(new OpportunityKernelDecision(
                    OpportunityKernelDecisionReason.PreparationMissedTransition,
                    pending.Id));
                decisions.Add(new OpportunityKernelDecision(
                    OpportunityKernelDecisionReason.PendingOpportunityInvalidated,
                    pending.Id));
                OpportunityKernelState cleared = ClearPending(state);
                cleared = SelectFreshSchedule(
                    cleared,
                    RequireFreshTarget(input.FreshTransitionTarget),
                    decisions);
                return new OpportunityKernelResult(cleared, decisions);
            }

            var delivery = new OpportunityKernelDelivery(
                pending.Id,
                pending.SubjectTrack,
                input.Track,
                input.ObservedAt);
            OpportunityKernelState due = state with { DeliveryTransitionObserved = true };
            decisions.Add(new OpportunityKernelDecision(
                OpportunityKernelDecisionReason.DeliveryDue,
                pending.Id,
                Delivery: delivery));
            return new OpportunityKernelResult(due, decisions);
        }

        decisions.Add(new OpportunityKernelDecision(
            OpportunityKernelDecisionReason.RapidTransitionInvalidatedDelivery,
            pending.Id));
        decisions.Add(new OpportunityKernelDecision(
            OpportunityKernelDecisionReason.PendingOpportunityInvalidated,
            pending.Id));
        OpportunityKernelState invalidated = ClearPending(state);
        invalidated = SelectFreshSchedule(
            invalidated,
            RequireFreshTarget(input.FreshTransitionTarget),
            decisions);
        return new OpportunityKernelResult(invalidated, decisions);
    }

    private OpportunityKernelResult ApplyPreparationReady(
        OpportunityKernelState state,
        OpportunityKernelMarkPreparationReady input)
    {
        if (state.PendingOpportunity?.Id != input.OpportunityId
            || state.DeliveryTransitionObserved)
        {
            return SingleDecision(
                state,
                OpportunityKernelDecisionReason.StaleOpportunityRefused,
                input.OpportunityId);
        }

        return SingleDecision(
            state with { PreparationReady = true },
            OpportunityKernelDecisionReason.PreparationMarkedReady,
            input.OpportunityId);
    }

    private OpportunityKernelResult ApplyResolution(
        OpportunityKernelState state,
        OpportunityKernelResolveOpportunity input)
    {
        if (state.PendingOpportunity?.Id != input.OpportunityId)
        {
            return SingleDecision(
                state,
                OpportunityKernelDecisionReason.StaleOpportunityRefused,
                input.OpportunityId);
        }

        IReadOnlyList<string> comments = state.RecentAcceptedAiComments;
        if (input.Status == OpportunityKernelResolutionStatus.Completed
            && input.AcceptedAiComment is not null)
        {
            string normalized = CommentValidator.NormalizeWhitespace(input.AcceptedAiComment);
            if (normalized.Length > 0)
            {
                comments = AddBounded(
                    comments,
                    normalized,
                    _configuration.RecentCommentLimit);
            }
        }

        OpportunityKernelDecisionReason reason = input.Status switch
        {
            OpportunityKernelResolutionStatus.Completed =>
                OpportunityKernelDecisionReason.OpportunityResolvedCompleted,
            OpportunityKernelResolutionStatus.Abandoned =>
                OpportunityKernelDecisionReason.OpportunityResolvedAbandoned,
            OpportunityKernelResolutionStatus.Failed =>
                OpportunityKernelDecisionReason.OpportunityResolvedFailed,
            _ => throw new ArgumentOutOfRangeException(nameof(input)),
        };

        var decisions = new List<OpportunityKernelDecision>
        {
            new(reason, input.OpportunityId),
        };
        OpportunityKernelState resolved = ClearPending(state) with
        {
            RecentAcceptedAiComments = comments,
        };
        resolved = SelectFreshSchedule(
            resolved,
            RequireFreshTarget(input.FreshTransitionTarget),
            decisions);
        return new OpportunityKernelResult(resolved, decisions);
    }

    private OpportunityKernelResult ApplySilentMode(
        OpportunityKernelState state,
        OpportunityKernelSetSilentMode input)
    {
        if (state.SilentMode == input.Silent)
        {
            return SingleDecision(state, OpportunityKernelDecisionReason.SilentModeUnchanged);
        }

        var decisions = new List<OpportunityKernelDecision>();
        if (state.PendingOpportunity is not null)
        {
            decisions.Add(new OpportunityKernelDecision(
                OpportunityKernelDecisionReason.PendingOpportunityInvalidated,
                state.PendingOpportunity.Id));
        }

        OpportunityKernelState changed = ClearPending(state) with
        {
            SilentMode = input.Silent,
            TransitionCounter = 0,
        };

        if (input.Silent)
        {
            decisions.Add(new OpportunityKernelDecision(
                OpportunityKernelDecisionReason.SilentModeEnabled));
        }
        else
        {
            decisions.Add(new OpportunityKernelDecision(
                OpportunityKernelDecisionReason.SilentModeDisabled));
            changed = SelectFreshSchedule(
                changed,
                RequireFreshTarget(input.FreshTransitionTarget),
                decisions);
        }

        return new OpportunityKernelResult(changed, decisions);
    }

    private OpportunityKernelResult ApplyClearRecentHistory(
        OpportunityKernelState state,
        OpportunityKernelClearRecentHistory input)
    {
        var decisions = new List<OpportunityKernelDecision>();
        if (state.PendingOpportunity is not null)
        {
            decisions.Add(new OpportunityKernelDecision(
                OpportunityKernelDecisionReason.PendingOpportunityInvalidated,
                state.PendingOpportunity.Id));
        }

        OpportunityKernelState cleared = ClearPending(state) with
        {
            RecentTracks = Array.Empty<MediaSnapshot>(),
            RecentAcceptedAiComments = Array.Empty<string>(),
        };
        decisions.Add(new OpportunityKernelDecision(
            OpportunityKernelDecisionReason.RecentHistoryCleared));
        cleared = SelectFreshSchedule(cleared, input.FreshTransitionTarget, decisions);
        return new OpportunityKernelResult(cleared, decisions);
    }

    private OpportunityKernelResult ApplyCancellation(
        OpportunityKernelState state,
        OpportunityKernelCancelPendingOpportunity input)
    {
        if (state.PendingOpportunity is null)
        {
            return SingleDecision(
                state,
                OpportunityKernelDecisionReason.NoPendingOpportunityToCancel);
        }

        long opportunityId = state.PendingOpportunity.Id;
        var decisions = new List<OpportunityKernelDecision>
        {
            new(OpportunityKernelDecisionReason.PendingOpportunityCancelled, opportunityId),
        };
        OpportunityKernelState cancelled = ClearPending(state);
        cancelled = SelectFreshSchedule(
            cancelled,
            RequireFreshTarget(input.FreshTransitionTarget),
            decisions);
        return new OpportunityKernelResult(cancelled, decisions);
    }

    private OpportunityKernelResult ApplyRebind(
        OpportunityKernelState state,
        OpportunityKernelRebindCurrentTrack input)
    {
        EnsureUsableTrack(input.Track);
        var decisions = new List<OpportunityKernelDecision>();
        if (state.PendingOpportunity is not null)
        {
            decisions.Add(new OpportunityKernelDecision(
                OpportunityKernelDecisionReason.PendingOpportunityInvalidated,
                state.PendingOpportunity.Id));
        }

        OpportunityKernelState rebound = ClearPending(state) with
        {
            CurrentTrack = input.Track,
            CurrentTrackStartedAt = input.ObservedAt,
            RecentTracks = AddBounded(
                state.RecentTracks,
                HistoryTrack(input.Track),
                _configuration.RecentTrackLimit),
        };
        decisions.Add(new OpportunityKernelDecision(
            OpportunityKernelDecisionReason.CurrentTrackRebound));
        rebound = SelectFreshSchedule(rebound, input.FreshTransitionTarget, decisions);
        return new OpportunityKernelResult(rebound, decisions);
    }

    private OpportunityKernelState SelectFreshSchedule(
        OpportunityKernelState state,
        int transitionTarget,
        List<OpportunityKernelDecision> decisions)
    {
        if (transitionTarget < _configuration.MinimumTransitionsBetweenComments
            || transitionTarget > _configuration.MaximumTransitionsBetweenComments)
        {
            throw new InvalidOperationException(
                "The transition target value was outside the configured inclusive range.");
        }

        decisions.Add(new OpportunityKernelDecision(
            OpportunityKernelDecisionReason.FreshScheduleSelected,
            ScheduledThreshold: transitionTarget));
        return state with
        {
            TransitionCounter = 0,
            ScheduledThreshold = transitionTarget,
        };
    }

    private static OpportunityKernelState ClearPending(OpportunityKernelState state)
    {
        return state with
        {
            PendingOpportunity = null,
            PreparationReady = false,
            DeliveryTransitionObserved = false,
        };
    }

    private static OpportunityKernelResult SingleDecision(
        OpportunityKernelState state,
        OpportunityKernelDecisionReason reason,
        long? opportunityId = null)
    {
        return new OpportunityKernelResult(
            state,
            [new OpportunityKernelDecision(reason, opportunityId)]);
    }

    private static int RequireFreshTarget(int? transitionTarget)
    {
        return transitionTarget
            ?? throw new InvalidOperationException(
                "This state transition requires an explicit fresh transition target.");
    }

    private static IReadOnlyList<T> AddBounded<T>(
        IReadOnlyList<T> existing,
        T item,
        int limit)
    {
        int skip = Math.Max(0, existing.Count + 1 - limit);
        return existing.Skip(skip).Append(item).ToArray();
    }

    private static IReadOnlyList<string> BuildTrustedObservations(
        IReadOnlyList<MediaSnapshot> tracks,
        MediaSnapshot focusTrack)
    {
        if (focusTrack.Artist is null)
        {
            return Array.Empty<string>();
        }

        int artistCount = tracks.Count(
            track => RadioHistory.SameNormalizedText(track.Artist, focusTrack.Artist));
        if (artistCount < 2)
        {
            return Array.Empty<string>();
        }

        return
        [
            $"The exact artist string '{focusTrack.Artist}' has appeared {artistCount} times " +
            $"in the last {tracks.Count} observed tracks.",
        ];
    }

    private static MediaSnapshot HistoryTrack(MediaSnapshot track)
    {
        return new MediaSnapshot(track.Title, track.Artist, null);
    }

    private static bool HasUsableTitleAndArtist(MediaSnapshot track)
    {
        return track.Title is not null && track.Artist is not null;
    }

    private static bool HasSameNormalizedTrackIdentity(
        MediaSnapshot left,
        MediaSnapshot right)
    {
        return RadioHistory.SameNormalizedText(left.Title, right.Title)
            && RadioHistory.SameNormalizedText(left.Artist, right.Artist);
    }

    private static void EnsureUsableTrack(MediaSnapshot track)
    {
        if (!HasUsableTitleAndArtist(track))
        {
            throw new ArgumentException("A usable title and artist are required.", nameof(track));
        }
    }
}
