using Windows.Media.Control;

namespace ZoyaRadio.MediaProbe;

internal sealed class WindowsRadioRuntimeSessionFactory : IRadioRuntimeSessionFactory
{
    private readonly ILogFolderProvider _logFolderProvider;

    public WindowsRadioRuntimeSessionFactory(ILogFolderProvider logFolderProvider)
    {
        _logFolderProvider = logFolderProvider;
    }

    public async Task<IRadioRuntimeSession> CreateAsync(CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        _logFolderProvider.EnsureLogFolderExists();

        LocalLog? log = null;
        IAnnouncementSpeechService? speechService = null;
        OllamaClient? ollama = null;

        try
        {
            log = LocalLog.Create("radio-behavior", _logFolderProvider.GetLogFolderPath());
            log.Write("ZOYA RADIO - MILESTONE 8A CONTROL APPLICATION CANDIDATE");
            log.Write("This frontend hosts the accepted transition-aware runtime in the same process.");
            log.Write("Normal automatic delivery never pauses playback and targets only validated Firefox audio.");
            log.Write($"Local log: {log.FilePath}");

            AiCommentConfiguration aiConfiguration = AiCommentConfiguration.Default;
            HostPersonaConfiguration persona = HostPersonaConfiguration.Default;
            RadioBehaviorConfiguration radioConfiguration = RadioBehaviorConfiguration.Default;
            aiConfiguration.Validate();
            persona.Validate();
            radioConfiguration.Validate();

            IAnnouncementSpeechService? neuralSpeech = KokoroSpeechService.TryCreate(log);
            WindowsSpeechService? windowsSpeech = WindowsSpeechService.TryCreate(log);
            speechService = SpeechServiceSelection.Select(
                SpeechProviderPreference.PreferNeural,
                neuralSpeech,
                windowsSpeech,
                log);

            if (speechService is null)
            {
                throw new InvalidOperationException("No usable local speech provider is available.");
            }

            log.Write($"Active speech service: {speechService.ProviderName}");
            log.Write("Requesting the Windows global media-session manager...");
            GlobalSystemMediaTransportControlsSessionManager manager =
                await GlobalSystemMediaTransportControlsSessionManager.RequestAsync();
            cancellationToken.ThrowIfCancellationRequested();
            log.Write("Windows media-session manager is available.");

            ollama = new OllamaClient(aiConfiguration);
            var runtime = new RadioBehaviorTest(
                manager,
                log,
                speechService,
                ollama,
                aiConfiguration,
                persona,
                radioConfiguration);

            return new OwnedWindowsRadioRuntimeSession(runtime, speechService, ollama, log);
        }
        catch
        {
            ollama?.Dispose();
            speechService?.Dispose();
            log?.Dispose();
            throw;
        }
    }
}

internal sealed class OwnedWindowsRadioRuntimeSession : IRadioRuntimeSession
{
    private readonly RadioBehaviorTest _runtime;
    private readonly IAnnouncementSpeechService _speechService;
    private readonly IDisposable _commentGenerator;
    private readonly LocalLog _log;
    private bool _disposed;

    public OwnedWindowsRadioRuntimeSession(
        RadioBehaviorTest runtime,
        IAnnouncementSpeechService speechService,
        IDisposable commentGenerator,
        LocalLog log)
    {
        _runtime = runtime;
        _speechService = speechService;
        _commentGenerator = commentGenerator;
        _log = log;
    }

    public event Action<RadioRuntimeStatusUpdate>? StatusChanged
    {
        add => _runtime.StatusChanged += value;
        remove => _runtime.StatusChanged -= value;
    }

    public Task<RadioRuntimeSessionStartResult> StartAsync(
        RadioRuntimeStartOptions options,
        CancellationToken cancellationToken)
    {
        return _runtime.StartAsync(options, cancellationToken);
    }

    public Task StopAsync() => _runtime.StopAsync();

    public void SetSilentMode(bool silent) => _runtime.SetSilentMode(silent);

    public void ClearRecentHistory() => _runtime.ClearRecentHistory();

    public async ValueTask DisposeAsync()
    {
        if (_disposed)
        {
            return;
        }

        await _runtime.DisposeAsync();
        _disposed = true;
        _commentGenerator.Dispose();
        _speechService.Dispose();
        _log.Dispose();
    }
}
