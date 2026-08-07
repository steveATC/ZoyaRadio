using Windows.Media.Core;
using Windows.Media.Playback;
using Windows.Media.SpeechSynthesis;

namespace ZoyaRadio.MediaProbe;

internal sealed class WindowsSpeechService : IDisposable
{
    private readonly SpeechSynthesizer _synthesizer;
    private bool _disposed;

    private WindowsSpeechService(SpeechSynthesizer synthesizer, VoiceInformation selectedVoice)
    {
        _synthesizer = synthesizer;
        SelectedVoice = selectedVoice;
    }

    public VoiceInformation SelectedVoice { get; }

    public static WindowsSpeechService? TryCreate(IAnnouncementLog log)
    {
        try
        {
            log.Write("TTS mechanism: Microsoft Windows.Media.SpeechSynthesis (local Windows voice)");

            VoiceInformation[] voices = SpeechSynthesizer.AllVoices.ToArray();
            log.Write($"AVAILABLE ENABLED WINDOWS VOICES: {voices.Length}");

            for (int index = 0; index < voices.Length; index++)
            {
                VoiceInformation voice = voices[index];
                log.Write(
                    $"  VOICE {index + 1}: {Display(voice.DisplayName)} | " +
                    $"Culture: {Display(voice.Language)} | Gender: {voice.Gender}");
            }

            if (voices.Length == 0)
            {
                log.Write("NO USABLE WINDOWS VOICE: no playback or speech commands were issued.");
                log.Write("Windows must have an enabled local speech voice before this test can run.");
                return null;
            }

            VoiceInformation? defaultVoice = SpeechSynthesizer.DefaultVoice;
            if (defaultVoice is null)
            {
                log.Write("NO DEFAULT WINDOWS VOICE: no playback or speech commands were issued.");
                return null;
            }

            var synthesizer = new SpeechSynthesizer
            {
                Voice = defaultVoice,
            };

            log.Write($"Selected voice: {Display(defaultVoice.DisplayName)}");
            log.Write($"Selected voice culture: {Display(defaultVoice.Language)}");
            log.Write("Speech output: normal/default Windows audio device");
            return new WindowsSpeechService(synthesizer, defaultVoice);
        }
        catch (Exception exception)
        {
            log.Warning("Could not initialize or validate local Windows speech", exception);
            log.Write("No playback or speech commands were issued.");
            return null;
        }
    }

    public async Task<IPreparedAnnouncementSpeech> PrepareAsync(string sentence)
    {
        ObjectDisposedException.ThrowIf(_disposed, this);

        SpeechSynthesisStream stream = await _synthesizer.SynthesizeTextToStreamAsync(sentence);
        return new WindowsPreparedSpeech(stream);
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
            _synthesizer.Dispose();
        }
        catch
        {
            // Playback recovery and the final result must not be hidden by speech cleanup.
        }
    }

    private static string Display(string? value)
    {
        return string.IsNullOrWhiteSpace(value) ? "<not provided>" : value;
    }

    private sealed class WindowsPreparedSpeech : IPreparedAnnouncementSpeech
    {
        private readonly SpeechSynthesisStream _stream;
        private bool _disposed;

        public WindowsPreparedSpeech(SpeechSynthesisStream stream)
        {
            _stream = stream;
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

            using MediaSource source = MediaSource.CreateFromStream(_stream, _stream.ContentType);
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
                // Playback recovery and the final result must not be hidden by stream cleanup.
            }
        }
    }
}
