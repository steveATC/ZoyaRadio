using System.Collections.ObjectModel;
using System.Globalization;

namespace ZoyaRadio.MediaProbe;

internal enum CreativeContextFactState
{
    Known,
    Unknown,
}

internal enum CreativeContextCollectionState
{
    Unknown,
    KnownEmpty,
    KnownComplete,
    KnownWithOmissions,
}

internal enum CreativeContextProvenance
{
    ObservedMediaState,
    DeterministicSessionState,
    AcceptedCommentHistory,
    EditorialPolicyState,
}

internal sealed record CreativeContextText(
    CreativeContextFactState State,
    string? Value,
    CreativeContextProvenance Provenance,
    int OriginalTextElementCount,
    int OmittedTextElementCount)
{
    public bool WasTruncated => OmittedTextElementCount > 0;
}

internal sealed record CreativeContextTrackInput(string? Title, string? Artist);

internal sealed record CreativeContextTrack(
    CreativeContextText Title,
    CreativeContextText Artist);

internal sealed record CreativeContextOpportunityInput(
    long? OpportunityId,
    string? OpportunityClass,
    string? DecisionReason);

internal sealed record CreativeContextOpportunity(
    long? OpportunityId,
    CreativeContextText OpportunityClass,
    CreativeContextText DecisionReason,
    CreativeContextProvenance Provenance);

internal sealed record CreativeContextSessionInput(
    long? TotalObservedTransitions,
    int? CurrentTransitionCounter,
    int? ScheduledThreshold,
    bool? SilentMode);

internal sealed record CreativeContextSession(
    long? TotalObservedTransitions,
    int? CurrentTransitionCounter,
    int? ScheduledThreshold,
    bool? SilentMode,
    CreativeContextProvenance Provenance);

internal sealed record CreativeContextRecentUseInput(
    long? LastCompletedHostSpeechTransition,
    long? LastCompletedCommentTransition,
    long? LastCompletedLinerTransition,
    DateTimeOffset? LastCompletedLinerAt,
    int? ExperimentalLinerCooldownTransitions,
    TimeSpan? ExperimentalLinerCooldownElapsed,
    int? ExperimentalHostSpeechSpacingTransitions);

internal sealed record CreativeContextRecentUse(
    long? LastCompletedHostSpeechTransition,
    long? LastCompletedCommentTransition,
    long? LastCompletedLinerTransition,
    DateTimeOffset? LastCompletedLinerAt,
    int? ExperimentalLinerCooldownTransitions,
    TimeSpan? ExperimentalLinerCooldownElapsed,
    int? ExperimentalHostSpeechSpacingTransitions,
    CreativeContextProvenance RecentUseProvenance,
    CreativeContextProvenance PolicyProvenance);

internal sealed record CreativeContextCollection<T>(
    CreativeContextCollectionState State,
    IReadOnlyList<T> Items,
    int SourceItemCount,
    int OmittedItemCount);

internal sealed record CreativeContextInput(
    string? CorrelationId,
    CreativeContextTrackInput CurrentTrack,
    CreativeContextOpportunityInput Opportunity,
    CreativeContextSessionInput Session,
    IReadOnlyList<CreativeContextTrackInput>? RecentTracks,
    IReadOnlyList<string>? RecentAcceptedComments,
    CreativeContextRecentUseInput RecentUse);

internal sealed record CreativeContextPacket(
    int SchemaVersion,
    CreativeContextText CorrelationId,
    CreativeContextTrack CurrentTrack,
    CreativeContextOpportunity Opportunity,
    CreativeContextSession Session,
    CreativeContextCollection<CreativeContextTrack> RecentTracks,
    CreativeContextCollection<CreativeContextText> RecentAcceptedComments,
    CreativeContextRecentUse RecentUse);

internal sealed record CreativeContextLimitPolicy(
    int CorrelationIdTextElements,
    int MediaTextElements,
    int OpportunityTextElements,
    int CommentTextElements,
    int RecentTrackLimit,
    int RecentCommentLimit)
{
    public static CreativeContextLimitPolicy Default { get; } = new(
        CorrelationIdTextElements: 128,
        MediaTextElements: 256,
        OpportunityTextElements: 128,
        CommentTextElements: 1000,
        RecentTrackLimit: 10,
        RecentCommentLimit: 6);

    public void Validate()
    {
        RequirePositive(CorrelationIdTextElements, nameof(CorrelationIdTextElements));
        RequirePositive(MediaTextElements, nameof(MediaTextElements));
        RequirePositive(OpportunityTextElements, nameof(OpportunityTextElements));
        RequirePositive(CommentTextElements, nameof(CommentTextElements));

        if (RecentTrackLimit is < 1 or > 10)
        {
            throw new ArgumentOutOfRangeException(
                nameof(RecentTrackLimit),
                "M12A recent-track limits must remain between 1 and the accepted cap of 10.");
        }

        if (RecentCommentLimit is < 1 or > 6)
        {
            throw new ArgumentOutOfRangeException(
                nameof(RecentCommentLimit),
                "M12A recent-comment limits must remain between 1 and the accepted cap of 6.");
        }
    }

    private static void RequirePositive(int value, string parameterName)
    {
        if (value < 1)
        {
            throw new ArgumentOutOfRangeException(
                parameterName,
                "M12A string bounds must be positive.");
        }
    }
}

internal sealed class CreativeContextAssembler
{
    public const int CurrentSchemaVersion = 1;

    private readonly CreativeContextLimitPolicy _limits;

    public CreativeContextAssembler(CreativeContextLimitPolicy limits)
    {
        ArgumentNullException.ThrowIfNull(limits);
        limits.Validate();
        _limits = limits;
    }

    public CreativeContextPacket Assemble(CreativeContextInput input)
    {
        ArgumentNullException.ThrowIfNull(input);
        ArgumentNullException.ThrowIfNull(input.CurrentTrack);
        ArgumentNullException.ThrowIfNull(input.Opportunity);
        ArgumentNullException.ThrowIfNull(input.Session);
        ArgumentNullException.ThrowIfNull(input.RecentUse);

        return new CreativeContextPacket(
            CurrentSchemaVersion,
            BoundText(
                input.CorrelationId,
                _limits.CorrelationIdTextElements,
                CreativeContextProvenance.DeterministicSessionState),
            CreateTrack(input.CurrentTrack),
            new CreativeContextOpportunity(
                input.Opportunity.OpportunityId,
                BoundText(
                    input.Opportunity.OpportunityClass,
                    _limits.OpportunityTextElements,
                    CreativeContextProvenance.DeterministicSessionState),
                BoundText(
                    input.Opportunity.DecisionReason,
                    _limits.OpportunityTextElements,
                    CreativeContextProvenance.EditorialPolicyState),
                CreativeContextProvenance.DeterministicSessionState),
            new CreativeContextSession(
                input.Session.TotalObservedTransitions,
                input.Session.CurrentTransitionCounter,
                input.Session.ScheduledThreshold,
                input.Session.SilentMode,
                CreativeContextProvenance.DeterministicSessionState),
            CreateRecentTracks(input.RecentTracks),
            CreateRecentComments(input.RecentAcceptedComments),
            new CreativeContextRecentUse(
                input.RecentUse.LastCompletedHostSpeechTransition,
                input.RecentUse.LastCompletedCommentTransition,
                input.RecentUse.LastCompletedLinerTransition,
                input.RecentUse.LastCompletedLinerAt,
                input.RecentUse.ExperimentalLinerCooldownTransitions,
                input.RecentUse.ExperimentalLinerCooldownElapsed,
                input.RecentUse.ExperimentalHostSpeechSpacingTransitions,
                CreativeContextProvenance.DeterministicSessionState,
                CreativeContextProvenance.EditorialPolicyState));
    }

    private CreativeContextTrack CreateTrack(CreativeContextTrackInput track)
    {
        ArgumentNullException.ThrowIfNull(track);

        return new CreativeContextTrack(
            BoundText(
                track.Title,
                _limits.MediaTextElements,
                CreativeContextProvenance.ObservedMediaState),
            BoundText(
                track.Artist,
                _limits.MediaTextElements,
                CreativeContextProvenance.ObservedMediaState));
    }

    private CreativeContextCollection<CreativeContextTrack> CreateRecentTracks(
        IReadOnlyList<CreativeContextTrackInput>? tracks)
    {
        if (tracks is null)
        {
            return UnknownCollection<CreativeContextTrack>();
        }

        int skip = Math.Max(0, tracks.Count - _limits.RecentTrackLimit);
        CreativeContextTrack[] bounded = tracks
            .Skip(skip)
            .Select(CreateTrack)
            .ToArray();
        return KnownCollection(bounded, tracks.Count, skip);
    }

    private CreativeContextCollection<CreativeContextText> CreateRecentComments(
        IReadOnlyList<string>? comments)
    {
        if (comments is null)
        {
            return UnknownCollection<CreativeContextText>();
        }

        int skip = Math.Max(0, comments.Count - _limits.RecentCommentLimit);
        CreativeContextText[] bounded = comments
            .Skip(skip)
            .Select(comment => BoundText(
                comment,
                _limits.CommentTextElements,
                CreativeContextProvenance.AcceptedCommentHistory))
            .ToArray();
        return KnownCollection(bounded, comments.Count, skip);
    }

    private static CreativeContextCollection<T> UnknownCollection<T>() =>
        new(
            CreativeContextCollectionState.Unknown,
            Array.Empty<T>(),
            SourceItemCount: 0,
            OmittedItemCount: 0);

    private static CreativeContextCollection<T> KnownCollection<T>(
        T[] items,
        int sourceCount,
        int omittedCount)
    {
        CreativeContextCollectionState state = sourceCount == 0
            ? CreativeContextCollectionState.KnownEmpty
            : omittedCount > 0
                ? CreativeContextCollectionState.KnownWithOmissions
                : CreativeContextCollectionState.KnownComplete;

        return new CreativeContextCollection<T>(
            state,
            new ReadOnlyCollection<T>(items),
            sourceCount,
            omittedCount);
    }

    private static CreativeContextText BoundText(
        string? value,
        int maximumTextElements,
        CreativeContextProvenance provenance)
    {
        if (value is null)
        {
            return new CreativeContextText(
                CreativeContextFactState.Unknown,
                null,
                provenance,
                OriginalTextElementCount: 0,
                OmittedTextElementCount: 0);
        }

        int[] textElementOffsets = StringInfo.ParseCombiningCharacters(value);
        int originalCount = textElementOffsets.Length;
        if (originalCount <= maximumTextElements)
        {
            return new CreativeContextText(
                CreativeContextFactState.Known,
                value,
                provenance,
                originalCount,
                OmittedTextElementCount: 0);
        }

        int endOffset = textElementOffsets[maximumTextElements];
        return new CreativeContextText(
            CreativeContextFactState.Known,
            value[..endOffset],
            provenance,
            originalCount,
            originalCount - maximumTextElements);
    }
}
