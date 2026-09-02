using System.Collections.ObjectModel;
using System.Text;

namespace ZoyaRadio.MediaProbe;

// M13A-R2-I1 is an isolated provider-neutral foundation. It performs no I/O and
// is deliberately not composed into the production runtime.
internal enum KnowledgeEntityScope
{
    Artist,
    Track,
    Recording,
    Release,
}

internal readonly record struct KnowledgeEntityId
{
    internal KnowledgeEntityId(Guid value)
    {
        if (value == Guid.Empty) throw new ArgumentException("A knowledge entity ID must be application-owned and non-empty.", nameof(value));
        Value = value;
    }

    internal Guid Value { get; }
    internal bool IsValid => Value != Guid.Empty;

    public override string ToString() => Value.ToString("D");
}

internal readonly record struct KnowledgeClaimId
{
    internal KnowledgeClaimId(Guid value)
    {
        if (value == Guid.Empty) throw new ArgumentException("A claim ID must be non-empty.", nameof(value));
        Value = value;
    }

    internal Guid Value { get; }
    internal bool IsValid => Value != Guid.Empty;
}

internal enum IdentityBasisKind
{
    ReviewedEvidence,
    ProviderEvidence,
}

internal sealed record IdentityBasis
{
    internal IdentityBasis(IdentityBasisKind kind, string evidenceId, KnowledgeEntityScope establishedScope)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(evidenceId);
        if (establishedScope is not KnowledgeEntityScope.Artist and not KnowledgeEntityScope.Track)
        {
            throw new ArgumentOutOfRangeException(nameof(establishedScope), "R2-I1 establishes only Artist and Track subjects.");
        }

        Kind = kind;
        EvidenceId = evidenceId;
        EstablishedScope = establishedScope;
    }

    internal IdentityBasisKind Kind { get; }
    internal string EvidenceId { get; }
    internal KnowledgeEntityScope EstablishedScope { get; }
}

internal sealed record ObservedLabel
{
    internal ObservedLabel(string rawValue)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(rawValue);
        RawValue = rawValue;
        NormalizedValue = EntityKnowledgeText.Normalize(rawValue);
    }

    internal string RawValue { get; }
    internal string NormalizedValue { get; }
}

internal sealed record ObservedSubjectKey
{
    private ObservedSubjectKey(KnowledgeEntityScope scope, string normalizedPrimary, string? normalizedCredit)
    {
        Scope = scope;
        NormalizedPrimary = normalizedPrimary;
        NormalizedCredit = normalizedCredit;
    }

    internal KnowledgeEntityScope Scope { get; }
    internal string NormalizedPrimary { get; }
    internal string? NormalizedCredit { get; }

    internal static ObservedSubjectKey Artist(string artist) =>
        new(KnowledgeEntityScope.Artist, EntityKnowledgeText.NormalizeRequired(artist), null);

    internal static ObservedSubjectKey Track(string title, string rawArtistCredit) =>
        new(KnowledgeEntityScope.Track, EntityKnowledgeText.NormalizeRequired(title), EntityKnowledgeText.NormalizeRequired(rawArtistCredit));
}

internal sealed record ObservedTrackDescriptor
{
    internal ObservedTrackDescriptor(string title, string rawArtistCredit)
    {
        Title = new ObservedLabel(title);
        ArgumentException.ThrowIfNullOrWhiteSpace(rawArtistCredit);
        RawArtistCredit = rawArtistCredit;
        SubjectKey = ObservedSubjectKey.Track(title, rawArtistCredit);
    }

    internal ObservedLabel Title { get; }
    internal string RawArtistCredit { get; }
    internal ObservedSubjectKey SubjectKey { get; }
}

internal enum ObservedSubjectResolutionState
{
    Unresolved,
    UniqueEstablished,
    Ambiguous,
    Conflicting,
}

internal sealed record ObservedSubjectResolution(
    ObservedSubjectResolutionState State,
    IReadOnlyList<KnowledgeEntityId> CandidateEntityIds);

internal enum ExternalIdentityCorrespondence
{
    IdentifiesEntity,
    NarrowsEntity,
    RelatedOnly,
}

internal sealed record ExternalIdentityReference
{
    internal ExternalIdentityReference(
        string provider,
        string providerEntityType,
        string providerRecordId,
        ExternalIdentityCorrespondence correspondence,
        KnowledgeEntityScope applicableScope,
        string evidenceId)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(provider);
        ArgumentException.ThrowIfNullOrWhiteSpace(providerEntityType);
        ArgumentException.ThrowIfNullOrWhiteSpace(providerRecordId);
        ArgumentException.ThrowIfNullOrWhiteSpace(evidenceId);
        Provider = provider;
        ProviderEntityType = providerEntityType;
        ProviderRecordId = providerRecordId;
        Correspondence = correspondence;
        ApplicableScope = applicableScope;
        EvidenceId = evidenceId;
    }

    internal string Provider { get; }
    internal string ProviderEntityType { get; }
    internal string ProviderRecordId { get; }
    internal ExternalIdentityCorrespondence Correspondence { get; }
    internal KnowledgeEntityScope ApplicableScope { get; }
    internal string EvidenceId { get; }
}

internal enum ClaimEvidenceRole
{
    Supports,
    Corroborates,
    Disputes,
}

internal sealed record ClaimEvidence
{
    internal ClaimEvidence(
        string source,
        string sourceLocator,
        DateTimeOffset retrievedAtUtc,
        KnowledgeEntityScope supportedScope,
        ClaimEvidenceRole role,
        Uri? sourceUrl = null)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(source);
        ArgumentException.ThrowIfNullOrWhiteSpace(sourceLocator);
        Source = source;
        SourceLocator = sourceLocator;
        RetrievedAtUtc = retrievedAtUtc;
        SupportedScope = supportedScope;
        Role = role;
        SourceUrl = sourceUrl;
    }

    internal string Source { get; }
    internal string SourceLocator { get; }
    internal DateTimeOffset RetrievedAtUtc { get; }
    internal KnowledgeEntityScope SupportedScope { get; }
    internal ClaimEvidenceRole Role { get; }
    internal Uri? SourceUrl { get; }
}

internal enum KnowledgeClaimState
{
    SourceBacked,
    Corroborated,
    Conflicting,
    Ambiguous,
    Withdrawn,
    Stale,
}

internal sealed record KnowledgeClaim
{
    internal KnowledgeClaim(
        KnowledgeClaimId id,
        KnowledgeEntityId subjectId,
        string factType,
        string value,
        KnowledgeEntityScope applicableScope,
        KnowledgeClaimState state,
        IEnumerable<ClaimEvidence> evidence,
        DateTimeOffset observedAtUtc,
        KnowledgeClaimId? conflictsWith = null,
        KnowledgeClaimId? replaces = null)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(factType);
        ArgumentException.ThrowIfNullOrWhiteSpace(value);
        ArgumentNullException.ThrowIfNull(evidence);
        ClaimEvidence[] evidenceArray = evidence.ToArray();
        if (evidenceArray.Length == 0) throw new ArgumentException("A durable claim requires provenance.", nameof(evidence));
        Id = id;
        SubjectId = subjectId;
        FactType = factType;
        Value = value;
        ApplicableScope = applicableScope;
        State = state;
        Evidence = Array.AsReadOnly(evidenceArray);
        ObservedAtUtc = observedAtUtc;
        ConflictsWith = conflictsWith;
        Replaces = replaces;
    }

    internal KnowledgeClaimId Id { get; }
    internal KnowledgeEntityId SubjectId { get; }
    internal string FactType { get; }
    internal string Value { get; }
    internal KnowledgeEntityScope ApplicableScope { get; }
    internal KnowledgeClaimState State { get; init; }
    internal IReadOnlyList<ClaimEvidence> Evidence { get; }
    internal DateTimeOffset ObservedAtUtc { get; }
    internal KnowledgeClaimId? ConflictsWith { get; }
    internal KnowledgeClaimId? Replaces { get; }
    internal KnowledgeClaimId? ReplacedBy { get; init; }
}

internal abstract class MusicKnowledgeEntity
{
    private readonly List<ObservedLabel> _labels;
    private readonly List<ExternalIdentityReference> _externalIdentities = [];
    private readonly Dictionary<KnowledgeClaimId, KnowledgeClaim> _claims = [];

    protected MusicKnowledgeEntity(KnowledgeEntityId id, KnowledgeEntityScope scope, IdentityBasis identityBasis, IEnumerable<ObservedLabel> labels)
    {
        ArgumentNullException.ThrowIfNull(identityBasis);
        ArgumentNullException.ThrowIfNull(labels);
        if (identityBasis.EstablishedScope != scope) throw new ArgumentException("Identity evidence must establish the entity at its modeled scope.", nameof(identityBasis));
        ObservedLabel[] labelArray = labels.ToArray();
        if (labelArray.Length == 0) throw new ArgumentException("An established entity requires at least one retained label.", nameof(labels));
        Id = id;
        Scope = scope;
        IdentityBasis = identityBasis;
        _labels = [.. labelArray];
    }

    internal KnowledgeEntityId Id { get; }
    internal KnowledgeEntityScope Scope { get; }
    internal IdentityBasis IdentityBasis { get; }
    internal IReadOnlyList<ObservedLabel> Labels => _labels.AsReadOnly();
    internal IReadOnlyList<ExternalIdentityReference> ExternalIdentities => _externalIdentities.AsReadOnly();
    internal IReadOnlyList<KnowledgeClaim> Claims => _claims.Values.ToArray();

    internal void AddExternalIdentity(ExternalIdentityReference reference) => _externalIdentities.Add(reference);

    internal void AddClaim(KnowledgeClaim claim) => _claims.Add(claim.Id, claim);

    internal bool TryGetClaim(KnowledgeClaimId id, out KnowledgeClaim? claim) => _claims.TryGetValue(id, out claim);

    internal void ReplaceClaim(KnowledgeClaim claim) => _claims[claim.Id] = claim;
}

internal sealed class ArtistEntity : MusicKnowledgeEntity
{
    internal ArtistEntity(KnowledgeEntityId id, IdentityBasis identityBasis, IEnumerable<ObservedLabel> labels)
        : base(id, KnowledgeEntityScope.Artist, identityBasis, labels)
    {
    }
}

internal sealed class TrackEntity : MusicKnowledgeEntity
{
    private readonly List<TrackArtistCredit> _artistCredits = [];

    internal TrackEntity(KnowledgeEntityId id, IdentityBasis identityBasis, ObservedTrackDescriptor observation)
        : base(id, KnowledgeEntityScope.Track, identityBasis, [observation.Title])
    {
        Observation = observation;
    }

    internal ObservedTrackDescriptor Observation { get; }
    internal IReadOnlyList<TrackArtistCredit> ArtistCredits => _artistCredits.OrderBy(credit => credit.Order).ToArray();

    internal void AddArtistCredit(TrackArtistCredit credit) => _artistCredits.Add(credit);
}

internal sealed record TrackArtistCredit
{
    internal TrackArtistCredit(
        KnowledgeEntityId trackId,
        KnowledgeEntityId artistId,
        string creditedName,
        int order,
        string? relationship,
        string evidenceId)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(creditedName);
        ArgumentException.ThrowIfNullOrWhiteSpace(evidenceId);
        if (order < 0) throw new ArgumentOutOfRangeException(nameof(order));
        TrackId = trackId;
        ArtistId = artistId;
        CreditedName = creditedName;
        Order = order;
        Relationship = relationship;
        EvidenceId = evidenceId;
    }

    internal KnowledgeEntityId TrackId { get; }
    internal KnowledgeEntityId ArtistId { get; }
    internal string CreditedName { get; }
    internal int Order { get; }
    internal string? Relationship { get; }
    internal string EvidenceId { get; }
}

internal enum EntityGraphOperationOutcome
{
    Added,
    Updated,
    EntityNotFound,
    DuplicateEntity,
    DuplicateClaim,
    SubjectMismatch,
    WrongEntityScope,
    WrongEvidenceScope,
    UnsupportedEntityScope,
    ClaimNotFound,
    InvalidEntityId,
    InvalidClaimId,
    InvalidIdentityBasis,
}

internal sealed record EntityGraphOperationResult(EntityGraphOperationOutcome Outcome, string Detail)
{
    internal bool Succeeded => Outcome is EntityGraphOperationOutcome.Added or EntityGraphOperationOutcome.Updated;
}

internal sealed class MusicKnowledgeEntityGraph
{
    private readonly Dictionary<KnowledgeEntityId, MusicKnowledgeEntity> _entities = [];
    private readonly Dictionary<KnowledgeClaimId, KnowledgeEntityId> _claimOwners = [];
    private readonly Dictionary<ObservedSubjectKey, HashSet<KnowledgeEntityId>> _observedMappings = [];
    private readonly HashSet<ObservedSubjectKey> _conflictingMappings = [];

    internal IReadOnlyCollection<MusicKnowledgeEntity> Entities => _entities.Values.ToArray();

    internal EntityGraphOperationResult EstablishArtist(KnowledgeEntityId id, IEnumerable<ObservedLabel> labels, IdentityBasis? basis)
    {
        if (!id.IsValid) return new(EntityGraphOperationOutcome.InvalidEntityId, "A default or empty local entity ID is not valid.");
        if (basis is null || basis.EstablishedScope != KnowledgeEntityScope.Artist)
        {
            return new(EntityGraphOperationOutcome.InvalidIdentityBasis, "Observed labels alone cannot establish a durable Artist subject.");
        }

        return AddEntity(new ArtistEntity(id, basis, labels));
    }

    internal EntityGraphOperationResult EstablishTrack(KnowledgeEntityId id, ObservedTrackDescriptor observation, IdentityBasis? basis)
    {
        if (!id.IsValid) return new(EntityGraphOperationOutcome.InvalidEntityId, "A default or empty local entity ID is not valid.");
        if (basis is null || basis.EstablishedScope != KnowledgeEntityScope.Track)
        {
            return new(EntityGraphOperationOutcome.InvalidIdentityBasis, "Observed labels alone cannot establish a durable Track subject.");
        }

        return AddEntity(new TrackEntity(id, basis, observation));
    }

    internal ArtistEntity GetArtist(KnowledgeEntityId id) => _entities.TryGetValue(id, out MusicKnowledgeEntity? entity) && entity is ArtistEntity artist
        ? artist
        : throw new KeyNotFoundException("The requested Artist entity does not exist.");

    internal TrackEntity GetTrack(KnowledgeEntityId id) => _entities.TryGetValue(id, out MusicKnowledgeEntity? entity) && entity is TrackEntity track
        ? track
        : throw new KeyNotFoundException("The requested Track entity does not exist.");

    internal EntityGraphOperationResult LinkArtistCredit(TrackArtistCredit credit)
    {
        if (!credit.TrackId.IsValid || !credit.ArtistId.IsValid)
        {
            return new(EntityGraphOperationOutcome.InvalidEntityId, "Default or empty IDs cannot form an Artist credit relationship.");
        }

        if (!_entities.TryGetValue(credit.TrackId, out MusicKnowledgeEntity? trackEntity)
            || !_entities.TryGetValue(credit.ArtistId, out MusicKnowledgeEntity? artistEntity))
        {
            return new(EntityGraphOperationOutcome.EntityNotFound, "Both established Track and Artist subjects are required.");
        }

        if (trackEntity is not TrackEntity track || artistEntity is not ArtistEntity)
        {
            return new(EntityGraphOperationOutcome.WrongEntityScope, "Artist credits require a Track-to-Artist relationship.");
        }

        track.AddArtistCredit(credit);
        return new(EntityGraphOperationOutcome.Added, "The reviewed Track-to-Artist credit was added.");
    }

    internal EntityGraphOperationResult AddExternalIdentity(KnowledgeEntityId entityId, ExternalIdentityReference reference)
    {
        if (!entityId.IsValid) return new(EntityGraphOperationOutcome.InvalidEntityId, "A provider reference requires a valid local subject ID.");
        if (!_entities.TryGetValue(entityId, out MusicKnowledgeEntity? entity))
        {
            return new(EntityGraphOperationOutcome.EntityNotFound, "The identity reference subject was not found.");
        }

        if (reference.Correspondence == ExternalIdentityCorrespondence.IdentifiesEntity
            && reference.ApplicableScope != entity.Scope)
        {
            return new(EntityGraphOperationOutcome.WrongEntityScope, "An identifying provider reference must identify the modeled entity at its own scope.");
        }

        if (reference.Correspondence == ExternalIdentityCorrespondence.NarrowsEntity
            && (entity.Scope != KnowledgeEntityScope.Track
                || reference.ApplicableScope is not KnowledgeEntityScope.Recording and not KnowledgeEntityScope.Release))
        {
            return new(EntityGraphOperationOutcome.WrongEntityScope, "R2-I1 narrowing references may only narrow a Track to Recording or Release scope.");
        }

        entity.AddExternalIdentity(reference);
        return new(EntityGraphOperationOutcome.Added, "The external identity reference was attached without changing the local entity ID.");
    }

    internal EntityGraphOperationResult AddClaim(KnowledgeClaim claim)
    {
        if (!claim.Id.IsValid
            || claim.ConflictsWith is { } conflictId && !conflictId.IsValid
            || claim.Replaces is { } replacementId && !replacementId.IsValid)
        {
            return new(EntityGraphOperationOutcome.InvalidClaimId, "Default or empty claim IDs cannot enter the durable graph.");
        }

        if (!claim.SubjectId.IsValid) return new(EntityGraphOperationOutcome.InvalidEntityId, "A claim requires a valid local subject ID.");

        if (claim.ApplicableScope is KnowledgeEntityScope.Recording or KnowledgeEntityScope.Release)
        {
            return new(EntityGraphOperationOutcome.UnsupportedEntityScope, "Recording and Release subjects are not modeled in R2-I1.");
        }

        if (!_entities.TryGetValue(claim.SubjectId, out MusicKnowledgeEntity? entity))
        {
            return new(EntityGraphOperationOutcome.EntityNotFound, "The claim subject was not found.");
        }

        if (entity.Scope != claim.ApplicableScope)
        {
            return new(EntityGraphOperationOutcome.WrongEntityScope, "The claim must be owned by the entity it actually describes.");
        }

        if (claim.Evidence.Any(evidence => evidence.SupportedScope != claim.ApplicableScope))
        {
            return new(EntityGraphOperationOutcome.WrongEvidenceScope, "Narrow or unrelated evidence cannot promote a broader claim.");
        }

        if (_claimOwners.ContainsKey(claim.Id))
        {
            return new(EntityGraphOperationOutcome.DuplicateClaim, "Claim IDs are globally unique across the entity graph.");
        }

        entity.AddClaim(claim);
        _claimOwners.Add(claim.Id, entity.Id);
        return new(EntityGraphOperationOutcome.Added, "The provenance-bearing claim was attached to its subject.");
    }

    internal EntityGraphOperationResult SetClaimState(KnowledgeClaimId claimId, KnowledgeClaimState state, KnowledgeClaimId? replacedBy = null)
    {
        if (!claimId.IsValid || replacedBy is { } replacementId && !replacementId.IsValid)
        {
            return new(EntityGraphOperationOutcome.InvalidClaimId, "Default or empty claim IDs cannot mutate graph state.");
        }

        if (!_claimOwners.TryGetValue(claimId, out KnowledgeEntityId ownerId)
            || !_entities.TryGetValue(ownerId, out MusicKnowledgeEntity? entity)
            || !entity.TryGetClaim(claimId, out KnowledgeClaim? claim)
            || claim is null)
        {
            return new(EntityGraphOperationOutcome.ClaimNotFound, "The globally owned claim was not found.");
        }

        entity.ReplaceClaim(claim with { State = state, ReplacedBy = replacedBy });
        return new(EntityGraphOperationOutcome.Updated, "Only the globally owned selected claim state was updated.");
    }

    internal EntityGraphOperationResult LinkObservedSubject(ObservedSubjectKey key, KnowledgeEntityId entityId, IdentityBasis? reviewedMappingBasis)
    {
        if (!entityId.IsValid) return new(EntityGraphOperationOutcome.InvalidEntityId, "An observed mapping requires a valid local entity ID.");
        if (!_entities.TryGetValue(entityId, out MusicKnowledgeEntity? entity))
        {
            return new(EntityGraphOperationOutcome.EntityNotFound, "The observed mapping target was not found.");
        }

        if (reviewedMappingBasis is null || reviewedMappingBasis.EstablishedScope != key.Scope || entity.Scope != key.Scope)
        {
            return new(EntityGraphOperationOutcome.InvalidIdentityBasis, "A name match alone cannot establish an observed-to-durable identity mapping.");
        }

        if (!_observedMappings.TryGetValue(key, out HashSet<KnowledgeEntityId>? candidates))
        {
            candidates = [];
            _observedMappings.Add(key, candidates);
        }

        candidates.Add(entityId);
        return new(EntityGraphOperationOutcome.Added, "The reviewed observation mapping was recorded.");
    }

    internal void MarkObservedSubjectConflicting(ObservedSubjectKey key) => _conflictingMappings.Add(key);

    internal ObservedSubjectResolution ResolveObservedSubject(ObservedSubjectKey key)
    {
        if (_conflictingMappings.Contains(key)) return new(ObservedSubjectResolutionState.Conflicting, Candidates(key));
        KnowledgeEntityId[] candidates = Candidates(key);
        return candidates.Length switch
        {
            0 => new(ObservedSubjectResolutionState.Unresolved, candidates),
            1 => new(ObservedSubjectResolutionState.UniqueEstablished, candidates),
            _ => new(ObservedSubjectResolutionState.Ambiguous, candidates),
        };
    }

    internal IReadOnlyList<KnowledgeClaim> RelatedClaimsForTrack(KnowledgeEntityId trackId)
    {
        TrackEntity track = GetTrack(trackId);
        IEnumerable<KnowledgeClaim> artistClaims = track.ArtistCredits
            .Select(credit => GetArtist(credit.ArtistId))
            .SelectMany(artist => artist.Claims);
        return track.Claims.Concat(artistClaims).ToArray();
    }

    private EntityGraphOperationResult AddEntity(MusicKnowledgeEntity entity)
    {
        if (!_entities.TryAdd(entity.Id, entity)) return new(EntityGraphOperationOutcome.DuplicateEntity, "The local entity ID already exists.");
        return new(EntityGraphOperationOutcome.Added, "The positively established durable subject was added.");
    }

    private KnowledgeEntityId[] Candidates(ObservedSubjectKey key) => _observedMappings.TryGetValue(key, out HashSet<KnowledgeEntityId>? candidates)
        ? candidates.OrderBy(candidate => candidate.Value).ToArray()
        : [];
}

internal enum KnowledgeLookupOutcome
{
    Found,
    Negative,
    Unavailable,
    Ambiguous,
}

internal sealed record KnowledgeLookupCacheKey(
    ObservedSubjectKey ObservedKey,
    string Provider,
    string FactFamily,
    KnowledgeEntityScope RequestedScope);

internal sealed record KnowledgeLookupCacheEntry(
    KnowledgeLookupOutcome Outcome,
    DateTimeOffset RetrievedAtUtc,
    DateTimeOffset ExpiresAtUtc);

internal sealed class MusicKnowledgeLookupCache
{
    private readonly int _capacity;
    private readonly Func<DateTimeOffset> _utcNow;
    private readonly Dictionary<KnowledgeLookupCacheKey, (KnowledgeLookupCacheEntry Entry, long Order)> _entries = [];
    private long _nextOrder;

    internal MusicKnowledgeLookupCache(int capacity, Func<DateTimeOffset> utcNow)
    {
        if (capacity < 1) throw new ArgumentOutOfRangeException(nameof(capacity));
        ArgumentNullException.ThrowIfNull(utcNow);
        _capacity = capacity;
        _utcNow = utcNow;
    }

    internal void Store(KnowledgeLookupCacheKey key, KnowledgeLookupOutcome outcome, TimeSpan timeToLive)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(key.Provider);
        ArgumentException.ThrowIfNullOrWhiteSpace(key.FactFamily);
        if (timeToLive <= TimeSpan.Zero) throw new ArgumentOutOfRangeException(nameof(timeToLive));
        DateTimeOffset now = _utcNow();
        _entries[key] = (new(outcome, now, now.Add(timeToLive)), ++_nextOrder);
        while (_entries.Count > _capacity)
        {
            KnowledgeLookupCacheKey oldest = _entries.OrderBy(item => item.Value.Order).First().Key;
            _entries.Remove(oldest);
        }
    }

    internal KnowledgeLookupCacheEntry? Read(KnowledgeLookupCacheKey key)
    {
        if (!_entries.TryGetValue(key, out (KnowledgeLookupCacheEntry Entry, long Order) stored)) return null;
        if (stored.Entry.ExpiresAtUtc <= _utcNow())
        {
            _entries.Remove(key);
            return null;
        }

        return stored.Entry;
    }
}

internal sealed record MusicKnowledgeContextClaimView(
    KnowledgeClaimId ClaimId,
    KnowledgeEntityId SubjectId,
    KnowledgeEntityScope SubjectScope,
    string FactType,
    string Value,
    KnowledgeClaimState State,
    IReadOnlyList<string> SourceLocators);

internal sealed record MusicKnowledgeContextProjection(
    KnowledgeEntityId TrackId,
    IReadOnlyList<MusicKnowledgeContextClaimView> Claims);

internal sealed class MusicKnowledgeContextProjectionBuilder
{
    internal MusicKnowledgeContextProjection Build(
        MusicKnowledgeEntityGraph graph,
        KnowledgeEntityId trackId,
        IEnumerable<KnowledgeClaimId> selectedClaimIds,
        int maximumClaims)
    {
        ArgumentNullException.ThrowIfNull(graph);
        ArgumentNullException.ThrowIfNull(selectedClaimIds);
        if (maximumClaims < 1) throw new ArgumentOutOfRangeException(nameof(maximumClaims));
        HashSet<KnowledgeClaimId> selected = [.. selectedClaimIds];
        MusicKnowledgeContextClaimView[] claims = graph.RelatedClaimsForTrack(trackId)
            .Where(claim => selected.Contains(claim.Id) && claim.State != KnowledgeClaimState.Withdrawn)
            .Take(maximumClaims)
            .Select(claim => new MusicKnowledgeContextClaimView(
                claim.Id,
                claim.SubjectId,
                claim.ApplicableScope,
                claim.FactType,
                claim.Value,
                claim.State,
                Array.AsReadOnly(claim.Evidence.Select(evidence => evidence.SourceLocator).ToArray())))
            .ToArray();
        return new(trackId, Array.AsReadOnly(claims));
    }
}

internal static class EntityKnowledgeText
{
    internal static string NormalizeRequired(string value)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(value);
        return Normalize(value);
    }

    internal static string Normalize(string value)
    {
        string normalized = value.Normalize(NormalizationForm.FormKC).Trim();
        var builder = new StringBuilder(normalized.Length);
        bool pendingSpace = false;
        foreach (char character in normalized)
        {
            if (char.IsWhiteSpace(character))
            {
                pendingSpace = builder.Length > 0;
                continue;
            }

            if (pendingSpace) builder.Append(' ');
            builder.Append(char.ToUpperInvariant(character));
            pendingSpace = false;
        }

        return builder.ToString();
    }
}
