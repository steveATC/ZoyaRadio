using Windows.Media.SpeechSynthesis;

namespace ZoyaRadio.MediaProbe;

internal sealed class WindowsSpeechService : IAnnouncementSpeechService
{
    private readonly SpeechSynthesizer _synthesizer;
    private bool _disposed;

    private WindowsSpeechService(SpeechSynthesizer synthesizer, VoiceInformation selectedVoice)
    {
        _synthesizer = synthesizer;
        SelectedVoice = selectedVoice;
    }

    public VoiceInformation SelectedVoice { get; }

    public string ProviderName => $"Windows TTS ({Display(SelectedVoice.DisplayName)})";

    public static WindowsSpeechService? TryCreate(IAnnouncementLog log)
    {
        try
        {
            log.Write("TTS fallback mechanism: Microsoft Windows.Media.SpeechSynthesis (local Windows voice)");

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
                log.Write("NO USABLE WINDOWS VOICE: Windows speech fallback is unavailable.");
                return null;
            }

            VoiceInformation? defaultVoice = SpeechSynthesizer.DefaultVoice;
            if (defaultVoice is null)
            {
                log.Write("NO DEFAULT WINDOWS VOICE: Windows speech fallback is unavailable.");
                return null;
            }

            var synthesizer = new SpeechSynthesizer
            {
                Voice = defaultVoice,
            };

            log.Write($"Windows fallback voice: {Display(defaultVoice.DisplayName)}");
            log.Write($"Windows fallback culture: {Display(defaultVoice.Language)}");
            log.Write("Speech output: normal/default Windows audio device");
            return new WindowsSpeechService(synthesizer, defaultVoice);
        }
        catch (Exception exception)
        {
            log.Warning("Could not initialize or validate local Windows speech fallback", exception);
            return null;
        }
    }

    public async Task<IPreparedAnnouncementSpeech> PrepareAsync(
        string text,
        CancellationToken cancellationToken)
    {
        ObjectDisposedException.ThrowIf(_disposed, this);
        cancellationToken.ThrowIfCancellationRequested();

        SpeechSynthesisStream stream = await _synthesizer.SynthesizeTextToStreamAsync(text);

        if (cancellationToken.IsCancellationRequested)
        {
            stream.Dispose();
            cancellationToken.ThrowIfCancellationRequested();
        }

        return new MediaPlayerPreparedSpeech(stream, stream.ContentType);
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
}
