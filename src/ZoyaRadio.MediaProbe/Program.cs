using Windows.Media.Control;

namespace ZoyaRadio.MediaProbe;

internal static class Program
{
    private static async Task<int> Main(string[] args)
    {
        bool once = false;
        bool controlTest = false;

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

            Console.Error.WriteLine($"Unknown option: {argument}");
            WriteUsage();
            return 2;
        }

        if (once && controlTest)
        {
            Console.Error.WriteLine("The --once and --control-test options cannot be combined.");
            WriteUsage();
            return 2;
        }

        LocalLog log;

        try
        {
            log = LocalLog.Create(controlTest ? "playback-control-test" : "media-probe");
        }
        catch (Exception exception)
        {
            Console.Error.WriteLine($"Could not create the local log file: {exception.Message}");
            return 1;
        }

        using (log)
        {
            if (controlTest)
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

                return 1;
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
    }
}
