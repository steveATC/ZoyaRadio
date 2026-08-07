namespace ZoyaRadio.MediaProbe;

internal sealed class RadioHistory
{
    private readonly int _trackLimit;
    private readonly int _commentLimit;
    private readonly List<MediaSnapshot> _tracks = [];
    private readonly List<string> _acceptedAiComments = [];

    public RadioHistory(int trackLimit, int commentLimit)
    {
        _trackLimit = trackLimit;
        _commentLimit = commentLimit;
    }

    public IReadOnlyList<MediaSnapshot> Tracks => _tracks.ToArray();

    public IReadOnlyList<string> AcceptedAiComments => _acceptedAiComments.ToArray();

    public void AddTrack(MediaSnapshot media)
    {
        if (media.Title is null || media.Artist is null)
        {
            return;
        }

        _tracks.Add(new MediaSnapshot(media.Title, media.Artist, null));
        TrimOldest(_tracks, _trackLimit);
    }

    public void AddAcceptedAiComment(string comment)
    {
        string normalized = CommentValidator.NormalizeWhitespace(comment);
        if (normalized.Length == 0)
        {
            return;
        }

        _acceptedAiComments.Add(normalized);
        TrimOldest(_acceptedAiComments, _commentLimit);
    }

    public bool IsExactRecentComment(string comment)
    {
        string normalized = NormalizeForComparison(comment);
        return _acceptedAiComments.Any(
            recent => string.Equals(
                NormalizeForComparison(recent),
                normalized,
                StringComparison.OrdinalIgnoreCase));
    }

    public IReadOnlyList<string> BuildTrustedObservations(MediaSnapshot focusTrack)
    {
        if (focusTrack.Artist is null)
        {
            return [];
        }

        int artistCount = _tracks.Count(
            track => SameNormalizedText(track.Artist, focusTrack.Artist));

        if (artistCount < 2)
        {
            return [];
        }

        return
        [
            $"The exact artist string '{focusTrack.Artist}' has appeared {artistCount} times " +
            $"in the last {_tracks.Count} observed tracks.",
        ];
    }

    public void Clear()
    {
        _tracks.Clear();
        _acceptedAiComments.Clear();
    }

    internal static bool SameNormalizedText(string? left, string? right)
    {
        if (left is null || right is null)
        {
            return false;
        }

        return string.Equals(
            NormalizeForComparison(left),
            NormalizeForComparison(right),
            StringComparison.OrdinalIgnoreCase);
    }

    internal static string NormalizeForComparison(string value)
    {
        return CommentValidator.NormalizeWhitespace(value);
    }

    private static void TrimOldest<T>(List<T> items, int limit)
    {
        while (items.Count > limit)
        {
            items.RemoveAt(0);
        }
    }
}
