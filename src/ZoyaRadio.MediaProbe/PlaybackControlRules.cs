using Windows.Media.Control;

namespace ZoyaRadio.MediaProbe;

internal static class FirefoxSessionSelector
{
    public const string VerifiedSourceId = "firefox.exe";

    public static IReadOnlyList<T> FindExactMatches<T>(
        IEnumerable<T> sessions,
        Func<T, string?> sourceIdSelector)
    {
        return sessions
            .Where(session => string.Equals(
                sourceIdSelector(session),
                VerifiedSourceId,
                StringComparison.OrdinalIgnoreCase))
            .ToArray();
    }
}

internal static class PlaybackControlRules
{
    public static bool IsAffirmative(string? response)
    {
        string normalized = response?.Trim() ?? string.Empty;
        return string.Equals(normalized, "y", StringComparison.OrdinalIgnoreCase)
            || string.Equals(normalized, "yes", StringComparison.OrdinalIgnoreCase);
    }

    public static bool CanStart(GlobalSystemMediaTransportControlsSessionPlaybackStatus? status)
    {
        return status == GlobalSystemMediaTransportControlsSessionPlaybackStatus.Playing;
    }
}
