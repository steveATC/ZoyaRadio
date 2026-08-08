using Windows.Media.Core;
using Windows.Media.Playback;
using Windows.Storage.Streams;

namespace ZoyaRadio.MediaProbe;

internal sealed class MediaPlayerPreparedSpeech : IPreparedAnnouncementSpeech
{
    private readonly IRandomAccessStream _stream;
    private readonly string _contentType;
    private bool _disposed;

    public MediaPlayerPreparedSpeech(IRandomAccessStream stream, string contentType)
    {
        _stream = stream;
        _contentType = contentType;
    }

    public async Task<SpeechPlaybackResult> SpeakAsync(
        TimeSpan timeout,
        CancellationToken cancellationToken)
    {
        ObjectDisposedException.ThrowIf(_disposed, this);

        if (cancellationToken.IsCancellationRequested)
        {
            return new SpeechPlaybackResult(SpeechPlaybackStatus.Cancelled);
        }

        using var player = new MediaPlayer
        {
            AudioCategory = MediaPlayerAudioCategory.Speech,
            AutoPlay = false,
        };

        player.CommandManager.IsEnabled = false;
        _stream.Seek(0);

        using MediaSource source = MediaSource.CreateFromStream(_stream, _contentType);
        var completion = new TaskCompletionSource<SpeechPlaybackResult>(
            TaskCreationOptions.RunContinuationsAsynchronously);

        void MediaEnded(MediaPlayer sender, object args)
        {
            completion.TrySetResult(new SpeechPlaybackResult(SpeechPlaybackStatus.Completed));
        }

        void MediaFailed(MediaPlayer sender, MediaPlayerFailedEventArgs args)
        {
            completion.TrySetResult(new SpeechPlaybackResult(
                SpeechPlaybackStatus.Failed,
                args.ErrorMessage));
        }

        player.MediaEnded += MediaEnded;
        player.MediaFailed += MediaFailed;

        using var timeoutCancellation = new CancellationTokenSource(timeout);
        using CancellationTokenRegistration userCancellationRegistration = cancellationToken.Register(
            () => completion.TrySetResult(new SpeechPlaybackResult(SpeechPlaybackStatus.Cancelled)));
        using CancellationTokenRegistration timeoutRegistration = timeoutCancellation.Token.Register(
            () => completion.TrySetResult(new SpeechPlaybackResult(
                SpeechPlaybackStatus.TimedOut,
                $"Speech did not complete within {timeout.TotalSeconds:0} seconds.")));

        try
        {
            player.Source = source;

            if (cancellationToken.IsCancellationRequested)
            {
                return new SpeechPlaybackResult(SpeechPlaybackStatus.Cancelled);
            }

            player.Play();
            return await completion.Task;
        }
        catch (Exception exception)
        {
            return new SpeechPlaybackResult(SpeechPlaybackStatus.Failed, exception.Message);
        }
        finally
        {
            player.MediaEnded -= MediaEnded;
            player.MediaFailed -= MediaFailed;

            try
            {
                player.Pause();
                player.Source = null;
            }
            catch
            {
                // Disposal below is the final best-effort stop for cancelled or failed speech.
            }
        }
    }

    public void Dispose()
    {
        if (_disposed)
        {
            return;
        }

        _disposed = true;

        try
        {
            _stream.Dispose();
        }
        catch
        {
            // Playback recovery and its final result must not be hidden by stream cleanup.
        }
    }
}
