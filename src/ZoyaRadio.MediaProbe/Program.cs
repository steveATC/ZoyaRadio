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

            Console.Error.WriteLine($"Unknown option: {argument}");
            WriteUsage();
            return 2;
        }

        int selectedModes = (once ? 1 : 0)
            + (controlTest ? 1 : 0)
            + (announcementTest ? 1 : 0)
            + (aiCommentTest ? 1 : 0);
        if (selectedModes > 1)
        {
            Console.Error.WriteLine("Snapshot, playback-control, announcement, and AI-comment modes cannot be combined.");
            WriteUsage();
            return 2;
        }

        if (simulateSpeechFailure && !announcementTest)
        {
            Console.Error.WriteLine("--simulate-speech-failure requires --announcement-test.");
            WriteUsage();
            return 2;
        }

        if ((generateOnly || measureAi || noAi || simulateAiFailure) && !aiCommentTest)
        {
            Console.Error.WriteLine("Milestone 3 options require --ai-comment-test (normally supplied by its launcher).");
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

        LocalLog log;

        try
        {
            string logPrefix = aiCommentTest
                ? "ai-comment-test"
                : announcementTest
                ? "spoken-announcement-test"
                : controlTest
                    ? "playback-control-test"
                    : "media-probe";
            log = LocalLog.Create(logPrefix);
        }
        catch (Exception exception)
        {
            Console.Error.WriteLine($"Could not create the local log file: {exception.Message}");
            return 1;
        }

        using (log)
        {
            if (aiCommentTest)
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
                log.Write("ZOYA RADIO - MILESTONE 3 LOCAL AI COMMENT TEST");
                log.Write("Ollama requests are restricted to this computer. No cloud AI service or API key is used.");
                log.Write($"Mode: {aiMode}");
            }
            else if (announcementTest)
            {
                log.Write("ZOYA RADIO - MILESTONE 2 EXPLICIT SPOKEN-ANNOUNCEMENT TEST");
                log.Write(
                    "This test can pause and resume exactly one firefox.exe session and play local Windows speech, " +
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

            bool spokenMode = announcementTest || (aiCommentTest && !generateOnly && !measureAi);
            using WindowsSpeechService? speechService = spokenMode
                ? WindowsSpeechService.TryCreate(log)
                : null;

            if (spokenMode && speechService is null)
            {
                log.Write("SPOKEN TEST RESULT: NOT RUN - no usable local Windows voice was available.");
                return 1;
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

                return 1;
            }

            if (aiCommentTest)
            {
                AiCommentConfiguration configuration = AiCommentConfiguration.Default;

                try
                {
                    configuration.Validate();
                }
                catch (Exception exception)
                {
                    log.Warning("Milestone 3 local Ollama configuration is invalid", exception);
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
                    var test = new AiCommentTest(manager, log, speechService, ollama, configuration);
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
        Console.Error.WriteLine("   or: run-ai-comment-test.cmd [--generate-only|--measure-ai|--no-ai|--simulate-ai-failure]");
    }
}
