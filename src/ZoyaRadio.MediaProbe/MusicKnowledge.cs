using System.Collections.ObjectModel;

namespace ZoyaRadio.MediaProbe;

// M13A-I1 is a fixture-only foundation.  Nothing in this file opens a network
// connection, persists provider data, or participates in radio operation.
internal enum MusicKnowledgeOutcome
{
    Unknown,
    ConfirmedFact,
    ConfirmedAbsence,
    AmbiguousIdentity,
    UnsupportedRepresentation,
    StaleEvidence,
    ProviderOutage,
    Timeout,
    RateLimited,
    MalformedResponse,
    SchemaFailure,
    SourceConflict,
    CorruptCache,
}

internal enum MusicKnowledgeSource
{
    MusicBrainz,
    Wikidata,
    ReviewedLocal,
    MemoryCache,
}

internal sealed record MusicKnowledgeProvenance(
    MusicKnowledgeSource Source,
    string SourceRecordId,
    DateTimeOffset RetrievedAtUtc,
    string FixtureName);

internal sealed record MusicKnowledgeAlias(string Name, string Type, MusicKnowledgeProvenance Provenance);

internal sealed record MusicKnowledgeArtist(
    string Id,
    string Name,
    IReadOnlyList<MusicKnowledgeAlias> Aliases,
    string? LinkedWikidataId,
    MusicKnowledgeProvenance Provenance);

internal sealed record MusicKnowledgeArtistCreditPart(
    string CreditedName,
    MusicKnowledgeArtist? Artist,
    string JoinPhrase);

internal sealed record MusicKnowledgeArtistCredit(IReadOnlyList<MusicKnowledgeArtistCreditPart> Parts)
{
    internal bool HasStableArtistIds => Parts.Count > 0 && Parts.All(part => !string.IsNullOrWhiteSpace(part.Artist?.Id));
}

internal sealed record MusicKnowledgeReleaseGroup(
    string Id,
    string Title,
    string? PrimaryType,
    MusicKnowledgeProvenance Provenance);

internal sealed record MusicKnowledgeRelease(
    string Id,
    string Title,
    string? Status,
    MusicKnowledgeReleaseGroup? ReleaseGroup,
    MusicKnowledgeProvenance Provenance);

internal sealed record MusicKnowledgeRecording(
    string Id,
    string Title,
    MusicKnowledgeArtistCredit ArtistCredit,
    IReadOnlyList<MusicKnowledgeRelease> Releases,
    MusicKnowledgeProvenance Provenance);

internal sealed record MusicKnowledgeRelationship(
    string Type,
    string SourceEntityId,
    string TargetEntityId,
    MusicKnowledgeProvenance Provenance);

internal sealed record MusicKnowledgeSnapshot(
    IReadOnlyList<MusicKnowledgeArtist> Artists,
    IReadOnlyList<MusicKnowledgeRecording> Recordings,
    IReadOnlyList<MusicKnowledgeRelationship> Relationships)
{
    internal static MusicKnowledgeSnapshot Empty { get; } = new([], [], []);
}

internal sealed record MusicKnowledgeIdentityRequest(string Artist, string? RecordingTitle);

internal sealed record MusicKnowledgeResolvedIdentity(
    MusicKnowledgeArtist Artist,
    MusicKnowledgeRecording? Recording,
    string? LinkedWikidataId);

internal sealed record MusicKnowledgeIdentityResolution(
    MusicKnowledgeOutcome Outcome,
    MusicKnowledgeResolvedIdentity? Identity,
    string Detail)
{
    internal bool IsAccepted => Outcome == MusicKnowledgeOutcome.ConfirmedFact && Identity is not null;
}

internal sealed record MusicKnowledgeFact(
    string FactType,
    string EntityId,
    string Value,
    MusicKnowledgeProvenance Provenance);

internal sealed record MusicKnowledgeFactResolution(
    string FactType,
    string EntityId,
    MusicKnowledgeOutcome Outcome,
    IReadOnlyList<MusicKnowledgeFact> Facts,
    IReadOnlyList<MusicKnowledgeProvenance> Provenance,
    string Detail)
{
    internal bool IsAvailable => Outcome == MusicKnowledgeOutcome.ConfirmedFact;

    internal static MusicKnowledgeFactResolution Unknown(string factType, string entityId) =>
        new(factType, entityId, MusicKnowledgeOutcome.Unknown, [], [], "No reviewed or provider fixture evidence is available.");
}

internal sealed record ReviewedMusicKnowledgeFact(string FactType, string EntityId, string Value, string ReviewId);

internal sealed class ReviewedMusicKnowledgeFactSource
{
    private readonly IReadOnlyDictionary<string, IReadOnlyList<ReviewedMusicKnowledgeFact>> _facts;

    internal ReviewedMusicKnowledgeFactSource(IEnumerable<ReviewedMusicKnowledgeFact> facts)
    {
        ArgumentNullException.ThrowIfNull(facts);
        _facts = new ReadOnlyDictionary<string, IReadOnlyList<ReviewedMusicKnowledgeFact>>(
            facts.GroupBy(fact => Key(fact.FactType, fact.EntityId), StringComparer.Ordinal)
                .ToDictionary(group => group.Key, group => (IReadOnlyList<ReviewedMusicKnowledgeFact>)group.ToArray(), StringComparer.Ordinal));
    }

    internal IReadOnlyList<MusicKnowledgeFact> Resolve(string factType, string entityId, DateTimeOffset retrievedAtUtc)
    {
        if (!_facts.TryGetValue(Key(factType, entityId), out IReadOnlyList<ReviewedMusicKnowledgeFact>? facts))
        {
            return [];
        }

        return facts.Select(fact => new MusicKnowledgeFact(
            fact.FactType,
            fact.EntityId,
            fact.Value,
            new MusicKnowledgeProvenance(MusicKnowledgeSource.ReviewedLocal, fact.ReviewId, retrievedAtUtc, "reviewed-local-fact"))).ToArray();
    }

    private static string Key(string factType, string entityId) => factType + "\u001F" + entityId;
}

internal sealed record MusicKnowledgeCacheReadResult(bool Found, MusicKnowledgeFactResolution? Resolution);

internal sealed class MusicKnowledgeMemoryCache
{
    private sealed record Entry(MusicKnowledgeFactResolution Resolution, DateTimeOffset ExpiresAtUtc, long StoredOrder);

    private readonly int _capacity;
    private readonly TimeSpan _confirmedTtl;
    private readonly TimeSpan _unavailableTtl;
    private readonly Dictionary<string, Entry?> _entries = new(StringComparer.Ordinal);
    private long _nextStoredOrder;

    internal MusicKnowledgeMemoryCache(int capacity, TimeSpan confirmedTtl, TimeSpan unavailableTtl)
    {
        if (capacity is < 1 or > 100 || confirmedTtl <= TimeSpan.Zero || unavailableTtl <= TimeSpan.Zero)
        {
            throw new ArgumentOutOfRangeException(nameof(capacity), "The in-memory cache must have bounded positive limits.");
        }

        _capacity = capacity;
        _confirmedTtl = confirmedTtl;
        _unavailableTtl = unavailableTtl;
    }

    internal int Count => _entries.Count;

    internal void Store(string key, MusicKnowledgeFactResolution resolution, DateTimeOffset now)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(key);
        ArgumentNullException.ThrowIfNull(resolution);
        DateTimeOffset expires = now.Add(resolution.Outcome == MusicKnowledgeOutcome.ConfirmedFact ? _confirmedTtl : _unavailableTtl);
        _entries[key] = new Entry(resolution, expires, ++_nextStoredOrder);
        while (_entries.Count > _capacity)
        {
            string oldestKey = _entries
                .Where(entry => entry.Value is not null)
                .OrderBy(entry => entry.Value!.StoredOrder)
                .ThenBy(entry => entry.Key, StringComparer.Ordinal)
                .First().Key;
            _entries.Remove(oldestKey);
        }
    }

    internal MusicKnowledgeCacheReadResult Read(string key, DateTimeOffset now)
    {
        if (!_entries.TryGetValue(key, out Entry? entry))
        {
            return new MusicKnowledgeCacheReadResult(false, null);
        }

        if (entry is null || entry.Resolution is null)
        {
            return new MusicKnowledgeCacheReadResult(true, new MusicKnowledgeFactResolution(
                "cache", key, MusicKnowledgeOutcome.CorruptCache, [], [], "The bounded in-memory cache entry was corrupt and was refused."));
        }

        if (entry.ExpiresAtUtc <= now)
        {
            _entries.Remove(key);
            return new MusicKnowledgeCacheReadResult(true, new MusicKnowledgeFactResolution(
                entry.Resolution.FactType,
                entry.Resolution.EntityId,
                MusicKnowledgeOutcome.StaleEvidence,
                [],
                entry.Resolution.Provenance,
                "The bounded in-memory cache entry expired and was refused as stale evidence."));
        }

        return new MusicKnowledgeCacheReadResult(true, entry.Resolution with
        {
            Provenance = entry.Resolution.Provenance.Append(new MusicKnowledgeProvenance(
                MusicKnowledgeSource.MemoryCache, key, now, "in-memory-cache")).ToArray(),
        });
    }

    internal void InjectCorruptEntryForTesting(string key) => _entries[key] = null;
}

internal sealed record MusicKnowledgeRetryPolicy(int MaximumAttempts)
{
    internal bool ShouldRetry(MusicKnowledgeOutcome outcome, int completedAttempts) =>
        completedAttempts < MaximumAttempts
        && outcome is MusicKnowledgeOutcome.ProviderOutage or MusicKnowledgeOutcome.Timeout or MusicKnowledgeOutcome.RateLimited;
}
