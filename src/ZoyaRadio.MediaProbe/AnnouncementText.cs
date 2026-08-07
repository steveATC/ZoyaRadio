using System.Diagnostics.CodeAnalysis;

namespace ZoyaRadio.MediaProbe;

internal static class AnnouncementText
{
    public static bool TryBuild(MediaSnapshot media, [NotNullWhen(true)] out string? sentence)
    {
        if (media.Title is null || media.Artist is null)
        {
            sentence = null;
            return false;
        }

        sentence = $"That was {media.Title} by {media.Artist}.";
        return true;
    }

    public static bool HasSameTitleAndArtist(MediaSnapshot expected, MediaSnapshot actual)
    {
        return string.Equals(expected.Title, actual.Title, StringComparison.Ordinal)
            && string.Equals(expected.Artist, actual.Artist, StringComparison.Ordinal);
    }
}
