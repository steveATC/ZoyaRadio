namespace ZoyaRadio.MediaProbe;

internal enum AnnouncementContext
{
    PreviousTrack,
    CurrentTrackIntroduction,
}

internal static class DeterministicAnnouncementRenderer
{
    public static string Render(CommentaryTrack track, AnnouncementContext context)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(track.Title);
        ArgumentException.ThrowIfNullOrWhiteSpace(track.Artist);

        string sentence = context switch
        {
            AnnouncementContext.PreviousTrack => $"That was {track.Title} by {track.Artist}",
            AnnouncementContext.CurrentTrackIntroduction => $"This is {track.Title} by {track.Artist}",
            _ => throw new ArgumentOutOfRangeException(nameof(context)),
        };

        char finalCharacter = track.Artist[^1];
        return finalCharacter is '.' or '!' or '?'
            ? sentence
            : sentence + ".";
    }
}
