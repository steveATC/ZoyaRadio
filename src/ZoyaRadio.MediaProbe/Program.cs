using Windows.Media.Control;

namespace ZoyaRadio.MediaProbe;

internal static class Program
{
    private static async Task<int> Main(string[] args)
    {
        bool once = false;
        bool controlTest = false;
        bool announcementTest = false;
        bool simulateSpeechFailure = false;
        bool aiCommentTest = false;
        bool generateOnly = false;
        bool measureAi = false;
        bool noAi = false;
        bool simulateAiFailure = false;
        bool radioBehaviorTest = false;
        bool simulateTransitionFailure = false;
        bool windowsTts = false;
        bool simulateNeuralTtsFailure = false;
        bool measureNeuralTts = false;
        bool audioDuckingTest = false;
        bool enumerateAudioOnly = false;
        bool simulateDuckFailure = false;
        bool commentaryBenchmark = false;
        bool sourceBackedCommentaryBenchmark = false;
        bool ownerCuratedCommentaryBenchmark = false;
        bool clearEnrichmentCache = false;
        bool deterministicContextBenchmark = false;
        bool m9c5Comparison = false;
        bool m9c6Benchmark = false;

        foreach (string argument in args)
        {
            if (string.Equals(argument, "--once", StringComparison.OrdinalIgnoreCase))
            {
                once = true;
                continue;
            }

            if (string.Equals(argument, "--control-test", StringComparison.OrdinalIgnoreCase))
            {
                controlTest = true;
                continue;
            }

            if (string.Equals(argument, "--announcement-test", StringComparison.OrdinalIgnoreCase))
            {
                announcementTest = true;
                continue;
            }

            if (string.Equals(argument, "--simulate-speech-failure", StringComparison.OrdinalIgnoreCase))
            {
                simulateSpeechFailure = true;
                continue;
            }

            if (string.Equals(argument, "--ai-comment-test", StringComparison.OrdinalIgnoreCase))
            {
                aiCommentTest = true;
                continue;
            }

            if (string.Equals(argument, "--generate-only", StringComparison.OrdinalIgnoreCase))
            {
                generateOnly = true;
                continue;
            }

            if (string.Equals(argument, "--measure-ai", StringComparison.OrdinalIgnoreCase))
            {
                measureAi = true;
                continue;
            }

            if (string.Equals(argument, "--no-ai", StringComparison.OrdinalIgnoreCase))
            {
                noAi = true;
                continue;
            }

            if (string.Equals(argument, "--simulate-ai-failure", StringComparison.OrdinalIgnoreCase))
            {
                simulateAiFailure = true;
                continue;
            }

            if (string.Equals(argument, "--radio-behavior-test", StringComparison.OrdinalIgnoreCase))
            {
                radioBehaviorTest = true;
                continue;
            }

            if (string.Equals(argument, "--simulate-transition-failure", StringComparison.OrdinalIgnoreCase))
            {
                simulateTransitionFailure = true;
                continue;
            }

            if (string.Equals(argument, "--windows-tts", StringComparison.OrdinalIgnoreCase))
            {
                windowsTts = true;
                continue;
            }

            if (string.Equals(argument, "--simulate-neural-tts-failure", StringComparison.OrdinalIgnoreCase))
            {
                simulateNeuralTtsFailure = true;
                continue;
            }

            if (string.Equals(argument, "--measure-neural-tts", StringComparison.OrdinalIgnoreCase))
            {
                measureNeuralTts = true;
                continue;
            }

            if (string.Equals(argument, "--audio-ducking-test", StringComparison.OrdinalIgnoreCase))
            {
                audioDuckingTest = true;
                continue;
            }

            if (string.Equals(argument, "--enumerate-only", StringComparison.OrdinalIgnoreCase))
            {
                enumerateAudioOnly = true;
                continue;
            }

            if (string.Equals(argument, "--simulate-duck-failure", StringComparison.OrdinalIgnoreCase))
            {
                simulateDuckFailure = true;
                continue;
            }

            if (string.Equals(argument, "--commentary-benchmark", StringComparison.OrdinalIgnoreCase))
            {
                commentaryBenchmark = true;
                continue;
            }

            if (string.Equals(argument, "--source-backed-commentary-benchmark", StringComparison.OrdinalIgnoreCase))
            {
                sourceBackedCommentaryBenchmark = true;
                continue;
            }

            if (string.Equals(argument, "--owner-curated-commentary-benchmark", StringComparison.OrdinalIgnoreCase))
            {
                ownerCuratedCommentaryBenchmark = true;
                continue;
            }

            if (string.Equals(argument, "--clear-enrichment-cache", StringComparison.OrdinalIgnoreCase))
            {
                clearEnrichmentCache = true;
                continue;
            }

            if (string.Equals(argument, "--deterministic-context-benchmark", StringComparison.OrdinalIgnoreCase))
            {
                deterministicContextBenchmark = true;
                continue;
            }
            if (string.Equals(argument, "--m9c5-model-comparison", StringComparison.OrdinalIgnoreCase))
            {
                m9c5Comparison = true;
                continue;
            }

            if (string.Equals(argument, "--m9c6-whole-comment-benchmark", StringComparison.OrdinalIgnoreCase))
            {
                m9c6Benchmark = true;
                continue;
            }

            Console.Error.WriteLine($"Unknown option: {argument}");
            WriteUsage();
            return 2;
        }

        int selectedModes = (once ? 1 : 0)
            + (controlTest ? 1 : 0)
            + (announcementTest ? 1 : 0)
            + (aiCommentTest ? 1 : 0)
            + (radioBehaviorTest ? 1 : 0)
            + (measureNeuralTts ? 1 : 0)
            + (audioDuckingTest ? 1 : 0)
            + (commentaryBenchmark ? 1 : 0)
            + (sourceBackedCommentaryBenchmark ? 1 : 0)
            + (ownerCuratedCommentaryBenchmark ? 1 : 0)
            + (deterministicContextBenchmark ? 1 : 0)
            + (m9c5Comparison ? 1 : 0)
            + (m9c6Benchmark ? 1 : 0);
        if (selectedModes > 1)
        {
            Console.Error.WriteLine(
                "Snapshot, playback-control, announcement, AI-comment, radio-behavior, neural-TTS measurement, audio-ducking, commentary-benchmark, source-backed-commentary-benchmark, owner-curated-commentary-benchmark, deterministic-context-benchmark, M9C.5 comparison, and M9C.6 whole-comment modes cannot be combined.");
            WriteUsage();
            return 2;
        }

        if ((enumerateAudioOnly || simulateDuckFailure) && !audioDuckingTest)
        {
            Console.Error.WriteLine(
                "--enumerate-only and --simulate-duck-failure require --audio-ducking-test (normally supplied by its launcher).");
            WriteUsage();
            return 2;
        }

        if (simulateTransitionFailure && !radioBehaviorTest)
        {
            Console.Error.WriteLine(
                "--simulate-transition-failure requires --radio-behavior-test (normally supplied by its launcher).");
            WriteUsage();
            return 2;
        }

        if (enumerateAudioOnly && simulateDuckFailure)
        {
            Console.Error.WriteLine("--enumerate-only cannot be combined with --simulate-duck-failure.");
            WriteUsage();
            return 2;
        }

        if (simulateSpeechFailure && !announcementTest)
        {
            Console.Error.WriteLine("--simulate-speech-failure requires --announcement-test.");
            WriteUsage();
            return 2;
        }

        if ((generateOnly || measureAi || simulateAiFailure) && !aiCommentTest)
        {
            Console.Error.WriteLine("These AI diagnostic options require --ai-comment-test (normally supplied by its launcher).");
            WriteUsage();
            return 2;
        }

        if (noAi && !aiCommentTest && !radioBehaviorTest)
        {
            Console.Error.WriteLine("--no-ai requires the AI-comment or radio-behavior launcher.");
            WriteUsage();
            return 2;
        }

        if (generateOnly && measureAi)
        {
            Console.Error.WriteLine("--generate-only and --measure-ai cannot be combined.");
            WriteUsage();
            return 2;
        }

        if (noAi && (generateOnly || measureAi || simulateAiFailure))
        {
            Console.Error.WriteLine("--no-ai cannot be combined with AI generation, measurement, or simulated AI failure.");
            WriteUsage();
            return 2;
        }

        if (simulateAiFailure && (generateOnly || measureAi))
        {
            Console.Error.WriteLine("--simulate-ai-failure is only for the spoken fallback test.");
            WriteUsage();
            return 2;
        }

        bool spokenMode = radioBehaviorTest
            || announcementTest
            || (aiCommentTest && !generateOnly && !measureAi);

        if ((windowsTts || simulateNeuralTtsFailure) && !spokenMode)
        {
            Console.Error.WriteLine(
                "--windows-tts and --simulate-neural-tts-failure require a spoken announcement, AI-comment, or radio-behavior mode.");
            WriteUsage();
            return 2;
        }

        if (windowsTts && simulateNeuralTtsFailure)
        {
            Console.Error.WriteLine("--windows-tts cannot be combined with --simulate-neural-tts-failure.");
            WriteUsage();
            return 2;
        }

        LocalLog log;
        if (m9c6Benchmark)
        {
            return await M9C6Command.RunAsync();
        }
        if (m9c5Comparison)
        {
            return await M9C5ComparisonCommand.RunAsync();
        }

        if (deterministicContextBenchmark)
        {
            return await DeterministicContextBenchmarkCommand.RunAsync();
        }

        if (commentaryBenchmark)
        {
            return await CommentaryBenchmarkCommand.RunAsync();
        }

        if (sourceBackedCommentaryBenchmark || ownerCuratedCommentaryBenchmark || clearEnrichmentCache)
        {
            if (clearEnrichmentCache && !sourceBackedCommentaryBenchmark && !ownerCuratedCommentaryBenchmark)
            {
                return await MusicContextBenchmarkCommand.RunAsync(clearCacheOnly: true);
            }

            return await MusicContextBenchmarkCommand.RunAsync(
                clearCacheOnly: false,
                ownerCuratedCommentaryBenchmark ? MusicContextBenchmarkCorpus.M9C3OwnerCurated : MusicContextBenchmarkCorpus.M9C2SafetyAndFactCards);
        }

        try
        {
            string logPrefix = measureNeuralTts
                ? "neural-tts-diagnostic"
                : audioDuckingTest
                ? "audio-ducking-test"
                : radioBehaviorTest
                ? "radio-behavior-test"
                : aiCommentTest
                ? "ai-comment-test"
                : announcementTest
                ? "spoken-announcement-test"
                : controlTest
                    ? "playback-control-test"
                    : "media-probe";
            string? logDirectory = radioBehaviorTest
                ? new LocalAppDataLogFolderProvider().GetLogFolderPath()
                : null;
            log = LocalLog.Create(logPrefix, logDirectory);
        }
        catch (Exception exception)
        {
            Console.Error.WriteLine($"Could not create the local log file: {exception.Message}");
            return 1;
        }

        using (log)
        {
            if (measureNeuralTts)
            {
                log.Write("ZOYA RADIO - MILESTONE 7 NEURAL TTS PERFORMANCE DIAGNOSTIC");
                log.Write("READ ONLY WITH RESPECT TO MEDIA: no audio playback or Firefox control is used.");
            }
            else if (audioDuckingTest)
            {
                log.Write("ZOYA RADIO - MILESTONE 7.5A FIREFOX AUDIO-DUCKING DIAGNOSTIC");
                log.Write(
                    enumerateAudioOnly
                        ? "READ ONLY: this mode enumerates Windows audio sessions and will not change volume or playback."
                        : "This mode can temporarily change exactly one confirmed Firefox audio session's volume; it never pauses playback.");
                log.Write(
                    $"Mode: {(enumerateAudioOnly ? "read-only enumeration" : simulateDuckFailure ? "simulated failure with real protected volume restoration" : "one confirmed duck-and-restore cycle")}");
            }
            else if (radioBehaviorTest)
            {
                log.Write("ZOYA RADIO - MILESTONE 7.5B TRANSITION-AWARE AUTOMATIC RADIO TEST");
                log.Write(
                    "After arming, this mode may automatically duck exactly one safe Firefox Core Audio session and speak; normal delivery never pauses playback.");
                log.Write("Ollama requests remain loopback-only; recent behavioral history remains bounded and in memory.");
                log.Write(
                    $"Mode: {(noAi ? "automatic fixed fallback; AI disabled" : "automatic local-AI comments")}" +
                    $"{(simulateTransitionFailure ? "; first delivery simulates a post-duck failure" : string.Empty)}");
            }
            else if (aiCommentTest)
            {
                string aiMode = measureAi
                    ? "performance measurement (no playback or speech)"
                    : generateOnly
                        ? "generate only (no playback or speech)"
                        : noAi
                            ? "AI disabled; fixed fallback spoken test"
                            : simulateAiFailure
                                ? "simulated AI failure; fixed fallback spoken test"
                                : "one local-AI spoken comment";
                log.Write("ZOYA RADIO - MILESTONE 4 HOST-PERSONA TEST");
                log.Write("Ollama requests are restricted to this computer. No cloud AI service or API key is used.");
                log.Write($"Mode: {aiMode}");
            }
            else if (announcementTest)
            {
                log.Write("ZOYA RADIO - MILESTONE 2 EXPLICIT SPOKEN-ANNOUNCEMENT TEST");
                log.Write(
                    "This test can pause and resume exactly one firefox.exe session and play prepared local speech, " +
                    "but only after confirmation.");
                log.Write(
                    $"Mode: {(simulateSpeechFailure ? "simulated speech failure and real playback recovery" : "one spoken announcement")}");
            }
            else if (controlTest)
            {
                log.Write("ZOYA RADIO - MILESTONE 1 EXPLICIT PLAYBACK-CONTROL TEST");
                log.Write("This test can pause and resume exactly one firefox.exe session, but only after confirmation.");
            }
            else
            {
                log.Write("ZOYA RADIO - MILESTONE 0 MEDIA-SESSION PROBE");
                log.Write("READ ONLY: this probe observes Windows media sessions and will not control playback.");
                log.Write($"Mode: {(once ? "one-time snapshot" : "watch for changes")}");
            }

            log.Write($"Local log: {log.FilePath}");

            if (measureNeuralTts)
            {
                using KokoroSpeechService? neuralDiagnostic = KokoroSpeechService.TryCreate(log);
                if (neuralDiagnostic is null)
                {
                    log.Write("NEURAL TTS PERFORMANCE RESULT: FAIL - Nicole could not be initialized.");
                    return 1;
                }

                using var diagnosticCancellation = new CancellationTokenSource();
                ConsoleCancelEventHandler diagnosticCancelHandler = (_, eventArgs) =>
                {
                    eventArgs.Cancel = true;
                    diagnosticCancellation.Cancel();
                };

                Console.CancelKeyPress += diagnosticCancelHandler;

                try
                {
                    var diagnostic = new NeuralSpeechPerformanceDiagnostic(neuralDiagnostic, log);
                    return await diagnostic.RunAsync(diagnosticCancellation.Token) ? 0 : 1;
                }
                catch (OperationCanceledException)
                {
                    log.Write("NEURAL TTS PERFORMANCE RESULT: CANCELLED - no audio was played.");
                    return 0;
                }
                finally
                {
                    Console.CancelKeyPress -= diagnosticCancelHandler;
                }
            }

            if (audioDuckingTest)
            {
                using var audioDuckingCancellation = new CancellationTokenSource();
                ConsoleCancelEventHandler audioDuckingCancelHandler = (_, eventArgs) =>
                {
                    eventArgs.Cancel = true;
                    audioDuckingCancellation.Cancel();
                };

                Console.CancelKeyPress += audioDuckingCancelHandler;

                try
                {
                    var test = new AudioDuckingTest(log);
                    return await test.RunAsync(
                        enumerateAudioOnly,
                        simulateDuckFailure,
                        audioDuckingCancellation.Token);
                }
                finally
                {
                    Console.CancelKeyPress -= audioDuckingCancelHandler;
                }
            }

            IAnnouncementSpeechService? neuralSpeech = null;
            WindowsSpeechService? windowsSpeech = null;

            if (spokenMode)
            {
                if (!windowsTts)
                {
                    if (simulateNeuralTtsFailure)
                    {
                        log.Write("SIMULATED NEURAL TTS INITIALIZATION FAILURE: automatic Windows fallback will be tested.");
                    }
                    else
                    {
                        neuralSpeech = KokoroSpeechService.TryCreate(log);
                    }
                }

                windowsSpeech = WindowsSpeechService.TryCreate(log);
            }

            using IAnnouncementSpeechService? speechService = spokenMode
                ? SpeechServiceSelection.Select(
                    windowsTts ? SpeechProviderPreference.WindowsOnly : SpeechProviderPreference.PreferNeural,
                    neuralSpeech,
                    windowsSpeech,
                    log)
                : null;

            if (spokenMode && speechService is null)
            {
                log.Write(
                    radioBehaviorTest
                        ? "RADIO BEHAVIOR RESULT: NOT ARMED - no usable local speech provider was available."
                        : "SPOKEN TEST RESULT: NOT RUN - no usable local speech provider was available.");
                return 1;
            }

            if (speechService is not null)
            {
                log.Write($"Active speech service: {speechService.ProviderName}");
            }

            GlobalSystemMediaTransportControlsSessionManager manager;

            try
            {
                log.Write("Requesting the Windows global media-session manager...");
                manager = await GlobalSystemMediaTransportControlsSessionManager.RequestAsync();
                log.Write("Windows media-session manager is available.");
            }
            catch (Exception exception)
            {
                log.Warning("Windows did not provide the global media-session manager", exception);

                if (exception is UnauthorizedAccessException)
                {
                    log.Write(
                        "Windows denied media-session access to this account. " +
                        "Run the probe from Steve's normal signed-in desktop account; administrator rights should not be needed.");
                }

                if (announcementTest)
                {
                    log.Write("ANNOUNCEMENT TEST RESULT: NOT RUN - no playback or speech commands were issued.");
                }

                if (aiCommentTest)
                {
                    log.Write("AI COMMENT TEST RESULT: NOT RUN - no AI, playback, or speech commands were issued.");
                }

                if (radioBehaviorTest)
                {
                    log.Write("RADIO BEHAVIOR RESULT: NOT ARMED - no AI, playback, or speech commands were issued.");
                }

                return 1;
            }

            if (radioBehaviorTest)
            {
                AiCommentConfiguration aiConfiguration = AiCommentConfiguration.Default;
                HostPersonaConfiguration persona = HostPersonaConfiguration.Default;
                RadioBehaviorConfiguration radioConfiguration = RadioBehaviorConfiguration.Default;

                try
                {
                    aiConfiguration.Validate();
                    persona.Validate();
                    radioConfiguration.Validate();
                }
                catch (Exception exception)
                {
                    log.Warning("Milestone 5 configuration is invalid", exception);
                    log.Write("RADIO BEHAVIOR RESULT: NOT ARMED");
                    return 1;
                }

                using var radioCancellation = new CancellationTokenSource();
                ConsoleCancelEventHandler radioCancelHandler = (_, eventArgs) =>
                {
                    eventArgs.Cancel = true;
                    radioCancellation.Cancel();
                };

                Console.CancelKeyPress += radioCancelHandler;

                try
                {
                    using var ollama = new OllamaClient(aiConfiguration);
                    var test = new RadioBehaviorTest(
                        manager,
                        log,
                        speechService!,
                        ollama,
                        aiConfiguration,
                        persona,
                        radioConfiguration);
                    return await test.RunAsync(
                        aiEnabled: !noAi,
                        simulateTransitionFailure,
                        radioCancellation.Token);
                }
                finally
                {
                    Console.CancelKeyPress -= radioCancelHandler;
                }
            }

            if (aiCommentTest)
            {
                AiCommentConfiguration configuration = AiCommentConfiguration.Default;
                HostPersonaConfiguration persona = HostPersonaConfiguration.Default;

                try
                {
                    configuration.Validate();
                    persona.Validate();
                }
                catch (Exception exception)
                {
                    log.Warning("Milestone 4 local AI configuration is invalid", exception);
                    log.Write("AI COMMENT TEST RESULT: NOT RUN");
                    return 1;
                }

                using var aiCancellation = new CancellationTokenSource();
                ConsoleCancelEventHandler aiCancelHandler = (_, eventArgs) =>
                {
                    eventArgs.Cancel = true;
                    aiCancellation.Cancel();
                };

                Console.CancelKeyPress += aiCancelHandler;

                try
                {
                    using var ollama = new OllamaClient(configuration);
                    var test = new AiCommentTest(
                        manager,
                        log,
                        speechService,
                        ollama,
                        configuration,
                        persona);
                    AiCommentRunMode mode = measureAi
                        ? AiCommentRunMode.Measure
                        : generateOnly
                            ? AiCommentRunMode.GenerateOnly
                            : AiCommentRunMode.Spoken;
                    return await test.RunAsync(
                        mode,
                        aiEnabled: !noAi,
                        simulateAiFailure,
                        aiCancellation.Token);
                }
                finally
                {
                    Console.CancelKeyPress -= aiCancelHandler;
                }
            }

            if (announcementTest)
            {
                using var announcementCancellation = new CancellationTokenSource();
                ConsoleCancelEventHandler announcementCancelHandler = (_, eventArgs) =>
                {
                    eventArgs.Cancel = true;
                    announcementCancellation.Cancel();
                };

                Console.CancelKeyPress += announcementCancelHandler;

                try
                {
                    var test = new SpokenAnnouncementTest(manager, log, speechService!);
                    return await test.RunAsync(simulateSpeechFailure, announcementCancellation.Token);
                }
                finally
                {
                    Console.CancelKeyPress -= announcementCancelHandler;
                }
            }

            if (controlTest)
            {
                using var controlCancellation = new CancellationTokenSource();
                ConsoleCancelEventHandler controlCancelHandler = (_, eventArgs) =>
                {
                    eventArgs.Cancel = true;
                    controlCancellation.Cancel();
                };

                Console.CancelKeyPress += controlCancelHandler;

                try
                {
                    var test = new PlaybackControlTest(manager, log);
                    return await test.RunAsync(controlCancellation.Token);
                }
                finally
                {
                    Console.CancelKeyPress -= controlCancelHandler;
                }
            }

            await using var watcher = new MediaSessionWatcher(manager, log);

            if (!await watcher.StartAsync(observeChanges: !once))
            {
                return 1;
            }

            if (once)
            {
                log.Write("Snapshot complete. No playback controls were used.");
                return 0;
            }

            using var cancellation = new CancellationTokenSource();
            ConsoleCancelEventHandler cancelHandler = (_, eventArgs) =>
            {
                eventArgs.Cancel = true;
                cancellation.Cancel();
            };

            Console.CancelKeyPress += cancelHandler;

            try
            {
                log.Write("Watching for session, track, and playback changes. Press Ctrl+C to stop.");
                await Task.Delay(Timeout.Infinite, cancellation.Token);
            }
            catch (OperationCanceledException)
            {
                log.Write("Shutdown requested. Removing media-session observers...");
            }
            finally
            {
                Console.CancelKeyPress -= cancelHandler;
            }

            await watcher.DisposeAsync();
            log.Write("Stopped cleanly. No playback controls were used.");
            return 0;
        }
    }

    private static void WriteUsage()
    {
        Console.Error.WriteLine("Usage: run-media-probe.cmd [--once]");
        Console.Error.WriteLine("   or: run-playback-control-test.cmd");
        Console.Error.WriteLine("   or: run-spoken-announcement-test.cmd");
        Console.Error.WriteLine("   or: run-simulated-speech-failure-test.cmd");
        Console.Error.WriteLine("   or: run-ai-comment-test.cmd [--generate-only|--measure-ai|--no-ai|--simulate-ai-failure|--windows-tts]");
        Console.Error.WriteLine(
            "   or: run-radio-behavior-test.cmd [--no-ai|--windows-tts|--simulate-transition-failure]");
        Console.Error.WriteLine("   or: run-neural-tts-fallback-test.cmd");
        Console.Error.WriteLine("   or: run-neural-tts-diagnostic.cmd");
        Console.Error.WriteLine("   or: run-audio-ducking-test.cmd [--enumerate-only|--simulate-duck-failure]");
        Console.Error.WriteLine("   or: run-commentary-benchmark.cmd");
        Console.Error.WriteLine("   or: run-source-backed-commentary-benchmark.cmd [--clear-enrichment-cache]");
        Console.Error.WriteLine("   or: run-owner-curated-commentary-benchmark.cmd");
        Console.Error.WriteLine("   or: run-deterministic-context-benchmark.cmd");
        Console.Error.WriteLine("   or: run-m9c5-model-comparison.cmd");
        Console.Error.WriteLine("   or: run-m9c6-whole-comment-benchmark.cmd");
    }
}
