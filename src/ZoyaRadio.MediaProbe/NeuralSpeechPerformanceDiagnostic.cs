using System.Diagnostics;

namespace ZoyaRadio.MediaProbe;

internal sealed class NeuralSpeechPerformanceDiagnostic
{
    private static readonly string[] Comments =
    [
        "Steve, that title has arrived with enough confidence to rearrange the furniture, and frankly I admire the commitment.",
        "A wonderfully modest objective: continue existing. Efficient, clear, and considerably less paperwork than most grand ambitions.",
        "That name does not enter the room quietly; it kicks the door open, checks the lighting, and judges the curtains.",
    ];

    private readonly IAnnouncementSpeechService _speechService;
    private readonly IAnnouncementLog _log;

    public NeuralSpeechPerformanceDiagnostic(
        IAnnouncementSpeechService speechService,
        IAnnouncementLog log)
    {
        _speechService = speechService;
        _log = log;
    }

    public async Task<bool> RunAsync(CancellationToken cancellationToken)
    {
        _log.Write("NEURAL TTS PERFORMANCE DIAGNOSTIC");
        _log.Write("This mode prepares speech in memory only. It does not play audio or control Firefox.");
        _log.Write($"Provider: {_speechService.ProviderName}");

        for (int index = 0; index < Comments.Length; index++)
        {
            cancellationToken.ThrowIfCancellationRequested();
            var timer = Stopwatch.StartNew();

            using IPreparedAnnouncementSpeech prepared = await _speechService.PrepareAsync(
                Comments[index],
                cancellationToken);

            timer.Stop();
            _log.Write(
                $"REPEATED PREPARATION {index + 1} OF {Comments.Length}: PASS in " +
                $"{timer.Elapsed.TotalSeconds:N3} seconds; prepared buffer disposed without playback.");
        }

        _log.Write("NEURAL TTS PERFORMANCE RESULT: PASS - 3 prepared, 0 played, 0 playback controls.");
        return true;
    }
}
