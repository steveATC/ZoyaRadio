using System.Text.RegularExpressions;

namespace ZoyaRadio.MediaProbe;

internal enum CommentaryReason
{
    OrdinaryTrackReaction,
    ExactTrackRepeat,
    RepeatedArtist,
    SameArtistTransition,
    OrdinaryContinuity,
    NoNotableSessionPattern,
}

internal sealed record CommentaryTrack(string Title, string Artist);

internal sealed record CommentarySessionFacts(
    bool ExactTrackPreviouslyAppeared,
    int ExactTrackPriorCount,
    int? TransitionsSinceExactTrack,
    bool ArtistPreviouslyAppeared,
    int ArtistPriorCount,
    int? TransitionsSinceArtist,
    bool PreviousTrackHasSameArtist,
    bool NoNotableRetainedHistoryPattern);

internal sealed record CommentaryStyleFingerprint(
    string NormalizedOpeningPhrase,
    bool ListenerNameUsed,
    bool ContainsQuestion,
    bool ContainsComparisonMarker,
    int ApproximateWordCount);

internal sealed record CommentaryRecentComment(
    string Text,
    CommentaryStyleFingerprint Fingerprint);

internal sealed record CommentaryBrief(
    CommentaryTrack SubjectTrack,
    CommentaryTrack? PreviousTrack,
    CommentarySessionFacts SessionFacts,
    CommentaryReason PrimaryReason,
    IReadOnlyList<CommentaryReason> SecondaryReasons,
    IReadOnlyList<CommentaryRecentComment> RecentAcceptedComments);

internal static partial class CommentaryBriefFactory
{
    public static CommentaryBrief Create(
        MediaSnapshot subjectTrack,
        IReadOnlyList<MediaSnapshot> priorTracks,
        IReadOnlyList<string> recentAcceptedComments,
        CommentaryReason ordinaryReason,
        string listenerName = "Steve")
    {
        EnsureUsable(subjectTrack);

        if (ordinaryReason is not CommentaryReason.OrdinaryTrackReaction
            and not CommentaryReason.OrdinaryContinuity
            and not CommentaryReason.NoNotableSessionPattern)
        {
            throw new ArgumentOutOfRangeException(
                nameof(ordinaryReason),
                "The default reason must be an ordinary, continuity, or no-pattern reason.");
        }

        int trackLimit = RadioBehaviorConfiguration.Default.RecentTrackLimit;
        int commentLimit = RadioBehaviorConfiguration.Default.RecentCommentLimit;
        MediaSnapshot[] boundedTracks = priorTracks
            .Where(HasUsableTitleAndArtist)
            .TakeLast(trackLimit)
            .ToArray();
        string[] boundedComments = recentAcceptedComments
            .Where(comment => !string.IsNullOrWhiteSpace(comment))
            .TakeLast(commentLimit)
            .Select(CommentValidator.NormalizeWhitespace)
            .ToArray();

        int exactTrackCount = boundedTracks.Count(track => SameTrack(track, subjectTrack));
        int artistCount = boundedTracks.Count(
            track => RadioHistory.SameNormalizedText(track.Artist, subjectTrack.Artist));
        int? transitionsSinceExact = DistanceSinceLast(
            boundedTracks,
            track => SameTrack(track, subjectTrack));
        int? transitionsSinceArtist = DistanceSinceLast(
            boundedTracks,
            track => RadioHistory.SameNormalizedText(track.Artist, subjectTrack.Artist));

        MediaSnapshot? previousSnapshot = boundedTracks.LastOrDefault();
        bool sameArtistTransition = previousSnapshot is not null
            && RadioHistory.SameNormalizedText(previousSnapshot.Artist, subjectTrack.Artist);
        bool noPattern = exactTrackCount == 0 && artistCount == 0 && !sameArtistTransition;

        var applicableReasons = new List<CommentaryReason>();
        if (sameArtistTransition)
        {
            applicableReasons.Add(CommentaryReason.SameArtistTransition);
        }

        if (exactTrackCount > 0)
        {
            applicableReasons.Add(CommentaryReason.ExactTrackRepeat);
        }

        if (artistCount > 0)
        {
            applicableReasons.Add(CommentaryReason.RepeatedArtist);
        }

        CommentaryReason effectiveOrdinaryReason = ordinaryReason;
        if (ordinaryReason == CommentaryReason.OrdinaryContinuity && previousSnapshot is null)
        {
            effectiveOrdinaryReason = CommentaryReason.NoNotableSessionPattern;
        }

        CommentaryReason primaryReason = applicableReasons.Count > 0
            ? applicableReasons[0]
            : effectiveOrdinaryReason;
        IReadOnlyList<CommentaryReason> secondaryReasons = applicableReasons.Count > 1
            ? applicableReasons.Skip(1).ToArray()
            : [];

        var facts = new CommentarySessionFacts(
            exactTrackCount > 0,
            exactTrackCount,
            transitionsSinceExact,
            artistCount > 0,
            artistCount,
            transitionsSinceArtist,
            sameArtistTransition,
            noPattern);

        return new CommentaryBrief(
            new CommentaryTrack(subjectTrack.Title!, subjectTrack.Artist!),
            previousSnapshot is null
                ? null
                : new CommentaryTrack(previousSnapshot.Title!, previousSnapshot.Artist!),
            facts,
            primaryReason,
            secondaryReasons,
            boundedComments
                .Select(comment => new CommentaryRecentComment(
                    comment,
                    CreateFingerprint(comment, listenerName)))
                .ToArray());
    }

    public static CommentaryStyleFingerprint CreateFingerprint(
        string comment,
        string listenerName)
    {
        string normalized = CommentValidator.NormalizeWhitespace(comment);
        string opening = string.Join(
            ' ',
            normalized
                .Split(' ', StringSplitOptions.RemoveEmptyEntries)
                .Take(3))
            .ToLowerInvariant();
        bool listenerNameUsed = !string.IsNullOrWhiteSpace(listenerName)
            && Regex.IsMatch(
                normalized,
                $@"(?<![\p{{L}}\p{{N}}_]){Regex.Escape(listenerName.Trim())}(?![\p{{L}}\p{{N}}_])",
                RegexOptions.IgnoreCase | RegexOptions.CultureInvariant);
        string comparisonText = $" {normalized.ToLowerInvariant()} ";

        return new CommentaryStyleFingerprint(
            opening,
            listenerNameUsed,
            normalized.Contains('?'),
            comparisonText.Contains(" like ", StringComparison.Ordinal)
                || comparisonText.Contains(" as if ", StringComparison.Ordinal),
            CommentValidator.CountWords(normalized));
    }

    private static int? DistanceSinceLast(
        IReadOnlyList<MediaSnapshot> priorTracks,
        Func<MediaSnapshot, bool> predicate)
    {
        for (int index = priorTracks.Count - 1; index >= 0; index--)
        {
            if (predicate(priorTracks[index]))
            {
                // The current subject is excluded from priorTracks. An immediately previous
                // occurrence is therefore one genuine transition before the subject.
                return priorTracks.Count - index;
            }
        }

        return null;
    }

    private static bool SameTrack(MediaSnapshot left, MediaSnapshot right)
    {
        return RadioHistory.SameNormalizedText(left.Title, right.Title)
            && RadioHistory.SameNormalizedText(left.Artist, right.Artist);
    }

    private static bool HasUsableTitleAndArtist(MediaSnapshot media)
    {
        return media.Title is not null && media.Artist is not null;
    }

    private static void EnsureUsable(MediaSnapshot media)
    {
        if (!HasUsableTitleAndArtist(media))
        {
            throw new ArgumentException("A subject title and artist are required.", nameof(media));
        }
    }
}
