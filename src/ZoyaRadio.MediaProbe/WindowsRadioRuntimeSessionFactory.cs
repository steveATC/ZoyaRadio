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
        OllamaClient? m9bOllama = null;
        MusicBrainzWikidataEnricher? m9bLiveResolver = null;

        try
        {
            log = LocalLog.Create("radio-behavior", _logFolderProvider.GetLogFolderPath());
            log.Write("ZOYA RADIO - MILESTONE 8B PRIVATE-ALPHA CANDIDATE");
            log.Write("This frontend hosts the accepted transition-aware runtime in the same process.");
            log.Write("Normal automatic delivery never pauses playback and targets only validated Firefox audio.");
            log.Write($"Local log: {log.FilePath}");
            PrivateAlphaPackageIntegrity.EnsureSafeStartup(AppContext.BaseDirectory, log);

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
            AiCommentConfiguration m9bConfiguration = aiConfiguration with { Model = M9BConstants.Model };
            m9bOllama = new OllamaClient(m9bConfiguration);
            MusicContextEnrichmentConfiguration m9bContextConfiguration = MusicContextEnrichmentConfiguration.Default with
            {
                UserAgent = "ZoyaRadio/0.8 (private-local-M9B-grounded-context)",
            };
            var m9bCache = new M9BContextCache(M9BContextCachePaths.DefaultDirectory, m9bContextConfiguration);
            var liveIdentityCache = new MusicContextCache(
                Path.Combine(M9BContextCachePaths.DefaultDirectory, "live-identity"),
                m9bContextConfiguration);
            m9bLiveResolver = new MusicBrainzWikidataEnricher(m9bContextConfiguration, liveIdentityCache);
            var m9bContextProvider = new M9BContextProvider(
                M9BContextCatalog.Load(),
                m9bLiveResolver,
                m9bCache,
                log);
            log.Write("M9B READINESS: 14B candidate will be checked per preparation; no model download or startup load is requested.");
            log.Write("M9B MODEL: qwen3:14b-q4_K_M, 14.8B Q4_K_M, about 9.3 GB local artifact, Apache-2.0; higher RAM/VRAM and cold-load cost than the 2.5 GB 4B model.");

            var runtime = new RadioBehaviorTest(
                manager,
                log,
                speechService,
                ollama,
                aiConfiguration,
                persona,
                radioConfiguration,
                m9bGenerator: m9bOllama,
                m9bConfiguration: m9bConfiguration,
                m9bContextProvider: m9bContextProvider,
                quickReactionPilotEnabled: string.Equals(Environment.GetEnvironmentVariable("ZOYA_QUICK_REACTION_PILOT"), "1", StringComparison.Ordinal));

            return new OwnedWindowsRadioRuntimeSession(runtime, speechService, ollama, m9bOllama, m9bLiveResolver, log);
        }
        catch
        {
            m9bLiveResolver?.Dispose();
            m9bOllama?.Dispose();
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
    private readonly IDisposable _m9bGenerator;
    private readonly IDisposable _m9bLiveResolver;
    private readonly LocalLog _log;
    private bool _disposed;

    public OwnedWindowsRadioRuntimeSession(
        RadioBehaviorTest runtime,
        IAnnouncementSpeechService speechService,
        IDisposable commentGenerator,
        IDisposable m9bGenerator,
        IDisposable m9bLiveResolver,
        LocalLog log)
    {
        _runtime = runtime;
        _speechService = speechService;
        _commentGenerator = commentGenerator;
        _m9bGenerator = m9bGenerator;
        _m9bLiveResolver = m9bLiveResolver;
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

    public async Task StopAsync()
    {
        await _runtime.StopAsync();
        _log.Write("AUTOMATIC RADIO SESSION: protected stop returned to the controller.");
    }

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
        _m9bLiveResolver.Dispose();
        _m9bGenerator.Dispose();
        _commentGenerator.Dispose();
        _speechService.Dispose();
        _log.Dispose();
    }
}
