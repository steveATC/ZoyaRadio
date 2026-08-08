namespace ZoyaRadio.MediaProbe;

internal enum SpeechProviderPreference
{
    PreferNeural,
    WindowsOnly,
}

internal interface IAnnouncementSpeechService : IDisposable
{
    string ProviderName { get; }

    Task<IPreparedAnnouncementSpeech> PrepareAsync(
        string text,
        CancellationToken cancellationToken);
}

internal static class SpeechServiceSelection
{
    public static IAnnouncementSpeechService? Select(
        SpeechProviderPreference preference,
        IAnnouncementSpeechService? neural,
        IAnnouncementSpeechService? windows,
        IAnnouncementLog log)
    {
        if (preference == SpeechProviderPreference.WindowsOnly)
        {
            neural?.Dispose();

            if (windows is null)
            {
                log.Write("EXPLICIT WINDOWS TTS MODE FAILED: no usable Windows voice is available.");
                return null;
            }

            log.Write($"Speech provider mode: explicit Windows TTS ({windows.ProviderName})");
            return windows;
        }

        if (neural is null)
        {
            if (windows is null)
            {
                log.Write("NO USABLE SPEECH PROVIDER: neural TTS and Windows TTS are both unavailable.");
                return null;
            }

            log.Write($"AUTOMATIC SPEECH FALLBACK ACTIVE: using {windows.ProviderName}.");
            return windows;
        }

        log.Write($"Preferred speech provider: {neural.ProviderName}");
        log.Write(
            windows is null
                ? "Windows TTS fallback: unavailable; a neural preparation failure will safely abandon speech before pause."
                : $"Automatic fallback provider: {windows.ProviderName}");
        return new FallbackSpeechService(neural, windows, log);
    }
}

internal sealed class FallbackSpeechService : IAnnouncementSpeechService
{
    private readonly IAnnouncementSpeechService _preferred;
    private readonly IAnnouncementSpeechService? _fallback;
    private readonly IAnnouncementLog _log;
    private bool _disposed;

    public FallbackSpeechService(
        IAnnouncementSpeechService preferred,
        IAnnouncementSpeechService? fallback,
        IAnnouncementLog log)
    {
        _preferred = preferred;
        _fallback = fallback;
        _log = log;
    }

    public string ProviderName => _fallback is null
        ? _preferred.ProviderName
        : $"{_preferred.ProviderName} with {_fallback.ProviderName} fallback";

    public async Task<IPreparedAnnouncementSpeech> PrepareAsync(
        string text,
        CancellationToken cancellationToken)
    {
        ObjectDisposedException.ThrowIf(_disposed, this);

        try
        {
            IPreparedAnnouncementSpeech prepared = await _preferred.PrepareAsync(text, cancellationToken);
            _log.Write($"Speech prepared successfully with preferred provider: {_preferred.ProviderName}");
            return prepared;
        }
        catch (OperationCanceledException)
        {
            throw;
        }
        catch (Exception exception)
        {
            _log.Warning($"Preferred speech provider failed before playback pause ({_preferred.ProviderName})", exception);
        }

        if (_fallback is null)
        {
            throw new InvalidOperationException(
                "Neural speech preparation failed and no Windows TTS fallback is available.");
        }

        _log.Write($"AUTOMATIC WINDOWS TTS FALLBACK: preparing the same text with {_fallback.ProviderName}.");

        try
        {
            IPreparedAnnouncementSpeech prepared = await _fallback.PrepareAsync(text, cancellationToken);
            _log.Write("AUTOMATIC WINDOWS TTS FALLBACK PREPARED before playback pause.");
            return prepared;
        }
        catch (OperationCanceledException)
        {
            throw;
        }
        catch (Exception exception)
        {
            _log.Warning("Windows TTS fallback also failed before playback pause", exception);
            throw new InvalidOperationException(
                "Both neural speech and Windows TTS fallback failed before playback pause.",
                exception);
        }
    }

    public void Dispose()
    {
        if (_disposed)
        {
            return;
        }

        _disposed = true;
        _preferred.Dispose();

        if (_fallback is not null && !ReferenceEquals(_fallback, _preferred))
        {
            _fallback.Dispose();
        }
    }
}
