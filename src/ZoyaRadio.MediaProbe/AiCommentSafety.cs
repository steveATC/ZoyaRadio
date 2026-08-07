using Windows.Media.Control;

namespace ZoyaRadio.MediaProbe;

internal static class AiCommentSafety
{
    public static bool CanSpeakPreparedComment(
        MediaSnapshot originalMedia,
        string? currentSourceId,
        bool currentSourceReadSucceeded,
        MediaSnapshot currentMedia,
        bool currentMediaReadSucceeded,
        GlobalSystemMediaTransportControlsSessionPlaybackStatus? currentPlaybackStatus,
        bool currentPlaybackReadSucceeded)
    {
        return currentSourceReadSucceeded
            && string.Equals(
                currentSourceId,
                FirefoxSessionSelector.VerifiedSourceId,
                StringComparison.OrdinalIgnoreCase)
            && currentMediaReadSucceeded
            && currentPlaybackReadSucceeded
            && PlaybackControlRules.CanStart(currentPlaybackStatus)
            && AnnouncementText.HasSameTitleAndArtist(originalMedia, currentMedia);
    }
}
