using System.Collections.ObjectModel;

namespace ZoyaRadio.MediaProbe;

internal readonly record struct PreparedMaterialId(string Value);
internal readonly record struct PreparedPayloadHandle(string Value);
internal readonly record struct PreparedMaterialReservationId(string Value);
internal readonly record struct PreparedMaterialSessionId(string Value);

internal enum PreparedMaterialReusePolicy { SingleOpportunity, ReusableWithinBinding }
internal enum PreparedMaterialLifecycleStatus { Preparing, Ready, Reserved, Terminal }
internal enum PreparedMaterialTerminalReason { Completed, Abandoned, Failed, Expired, Discarded, BindingInvalidated }
internal enum PreparedMaterialCompletionDisposition { RetireOrConsume, ReturnToInventory }
internal enum PreparedMaterialBindingKind { Opportunity, Track, Session }

internal sealed record PreparedMaterialTrackIdentity
{
    private PreparedMaterialTrackIdentity(string title, string artist)
    {
        Title = title;
        Artist = artist;
    }

    public string Title { get; }
    public string Artist { get; }

    public static PreparedMaterialTrackIdentity From(MediaSnapshot track)
    {
        ArgumentNullException.ThrowIfNull(track);
        if (track.Title is null || track.Artist is null)
        {
            throw new ArgumentException(
                "A usable title and artist are required for a prepared-material track binding.",
                nameof(track));
        }

        return new PreparedMaterialTrackIdentity(
            RadioHistory.NormalizeForComparison(track.Title),
            RadioHistory.NormalizeForComparison(track.Artist));
    }

    public bool Matches(MediaSnapshot track)
    {
        ArgumentNullException.ThrowIfNull(track);
        return RadioHistory.SameNormalizedText(Title, track.Title)
            && RadioHistory.SameNormalizedText(Artist, track.Artist);
    }
}

internal sealed record PreparedMaterialBinding(
    long? OpportunityId = null,
    PreparedMaterialTrackIdentity? SubjectTrack = null,
    PreparedMaterialSessionId? SessionId = null,
    DateTimeOffset? ExpiresAt = null);

internal sealed record PreparedMaterialUseContext(
    DateTimeOffset ObservedAt,
    long? OpportunityId = null,
    MediaSnapshot? Track = null,
    PreparedMaterialSessionId? SessionId = null);

internal sealed record PreparedMaterialReservation(
    PreparedMaterialReservationId Id,
    PreparedMaterialUseContext Context);

internal sealed record PreparedMaterialRecord(
    PreparedMaterialId Id,
    PreparedMaterialReusePolicy ReusePolicy,
    PreparedMaterialBinding Binding,
    DateTimeOffset RegisteredAt,
    PreparedMaterialLifecycleStatus Status,
    PreparedPayloadHandle? PayloadHandle,
    PreparedMaterialReservation? Reservation,
    PreparedMaterialTerminalReason? TerminalReason)
{
    public static PreparedMaterialRecord Preparing(
        PreparedMaterialId id,
        PreparedMaterialReusePolicy reusePolicy,
        PreparedMaterialBinding binding,
        DateTimeOffset registeredAt) =>
        new(
            id,
            reusePolicy,
            binding,
            registeredAt,
            PreparedMaterialLifecycleStatus.Preparing,
            PayloadHandle: null,
            Reservation: null,
            TerminalReason: null);

    public static PreparedMaterialRecord Ready(
        PreparedMaterialId id,
        PreparedMaterialReusePolicy reusePolicy,
        PreparedMaterialBinding binding,
        DateTimeOffset registeredAt,
        PreparedPayloadHandle payloadHandle) =>
        new(
            id,
            reusePolicy,
            binding,
            registeredAt,
            PreparedMaterialLifecycleStatus.Ready,
            payloadHandle,
            Reservation: null,
            TerminalReason: null);
}

internal sealed record PreparedMaterialLifecycleState(
    IReadOnlyDictionary<PreparedMaterialId, PreparedMaterialRecord> Materials)
{
    public static PreparedMaterialLifecycleState Initial { get; } = new(
        new ReadOnlyDictionary<PreparedMaterialId, PreparedMaterialRecord>(
            new Dictionary<PreparedMaterialId, PreparedMaterialRecord>()));
}

internal abstract record PreparedMaterialLifecycleInput;
internal sealed record PreparedMaterialRegister(
    PreparedMaterialId MaterialId,
    PreparedMaterialReusePolicy ReusePolicy,
    PreparedMaterialBinding Binding,
    DateTimeOffset RegisteredAt,
    PreparedPayloadHandle? PayloadHandle = null) : PreparedMaterialLifecycleInput;
internal sealed record PreparedMaterialAttachPayload(
    PreparedMaterialId MaterialId,
    PreparedPayloadHandle PayloadHandle) : PreparedMaterialLifecycleInput;
internal sealed record PreparedMaterialReserve(
    PreparedMaterialId MaterialId,
    PreparedMaterialReservationId ReservationId,
    PreparedMaterialUseContext Context) : PreparedMaterialLifecycleInput;
internal sealed record PreparedMaterialRelease(
    PreparedMaterialId MaterialId,
    PreparedMaterialReservationId ReservationId,
    PreparedMaterialUseContext CurrentContext) : PreparedMaterialLifecycleInput;
internal sealed record PreparedMaterialComplete(
    PreparedMaterialId MaterialId,
    PreparedMaterialReservationId ReservationId,
    PreparedMaterialCompletionDisposition Disposition,
    PreparedMaterialUseContext CurrentContext) : PreparedMaterialLifecycleInput;
internal sealed record PreparedMaterialTerminate(
    PreparedMaterialId MaterialId,
    PreparedMaterialTerminalReason Reason,
    PreparedMaterialReservationId? ReservationId = null) : PreparedMaterialLifecycleInput;
internal sealed record PreparedMaterialEvaluateExpiry(
    PreparedMaterialId MaterialId,
    DateTimeOffset ObservedAt) : PreparedMaterialLifecycleInput;
internal sealed record PreparedMaterialInvalidateBinding(
    PreparedMaterialId MaterialId,
    PreparedMaterialBindingKind BindingKind,
    long? OpportunityId = null,
    PreparedMaterialTrackIdentity? Track = null,
    PreparedMaterialSessionId? SessionId = null) : PreparedMaterialLifecycleInput;

internal enum PreparedMaterialDecisionReason
{
    RegisteredPreparing,
    RegisteredReady,
    RegistrationExpiredRefused,
    PayloadOwnershipAccepted,
    PayloadOwnershipRefused,
    PreparationReady,
    ReservationAcquired,
    ReservationConflict,
    BindingMismatch,
    ReservationReleased,
    SingleOpportunityReleaseDiscarded,
    CompletedReturnedToInventory,
    CompletedRetired,
    Abandoned,
    Failed,
    Expired,
    BindingInvalidated,
    Discarded,
    PayloadDisposalRequested,
    DuplicateRegistrationRefused,
    StaleMaterialRefused,
    StaleReservationRefused,
    InvalidTransitionRefused,
    AlreadyTerminalNoOp,
    ExpiryNotReached,
}

internal sealed record PreparedMaterialDecision(
    PreparedMaterialDecisionReason Reason,
    PreparedMaterialId MaterialId,
    PreparedPayloadHandle? PayloadHandle = null,
    PreparedMaterialReservationId? ReservationId = null);

internal sealed record PreparedMaterialLifecycleResult(
    PreparedMaterialLifecycleState State,
    IReadOnlyList<PreparedMaterialDecision> Decisions);