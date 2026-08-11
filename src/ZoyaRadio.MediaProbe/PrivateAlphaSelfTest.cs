using System.Diagnostics;

namespace ZoyaRadio.MediaProbe;

public static class PrivateAlphaSelfTestRunner
{
    public const string PackageArgument = "--package-self-test";
    public const string NeuralArgument = "--neural-package-self-test";

    public static async Task<int> RunAsync(bool verifyNeural, CancellationToken cancellationToken)
    {
        string logFolder = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
            LocalAppDataLogFolderProvider.ApplicationFolderName,
            LocalAppDataLogFolderProvider.LogsFolderName);
        Directory.CreateDirectory(logFolder);
        string reportPath = Path.Combine(
            logFolder,
            $"private-alpha-self-test-{DateTime.Now:yyyyMMdd-HHmmss-fff}.txt");
        using var log = new SelfTestLog(reportPath);

        log.Write("ZOYA RADIO PRIVATE-ALPHA SAFE PACKAGE SELF-TEST");
        log.Write("No Firefox media, playback, volume, audible speech, browser, or Ollama action is used.");
        log.Write($"Version: {PrivateAlphaIdentity.CandidateVersion}; target: {PrivateAlphaIdentity.RuntimeIdentifier}");
        log.Write($"Application base: {AppContext.BaseDirectory}");
        log.Write($"Current directory: {Environment.CurrentDirectory}");
        log.Write($"LocalAppData log folder: {logFolder}");

        try
        {
            if (!PrivateAlphaBuildIdentity.IsPackagedCandidate)
            {
                throw new InvalidOperationException(
                    "This executable was not built with the private-alpha candidate identity.");
            }

            PackageIntegrityResult integrity = PrivateAlphaPackageIntegrity.Verify(
                AppContext.BaseDirectory,
                verifyEveryFile: true);
            log.Write(integrity.Summary);
            foreach (string issue in integrity.Issues)
            {
                log.Write($"PACKAGE ISSUE: {issue}");
            }

            if (!integrity.IsValid)
            {
                log.Write("SELF-TEST RESULT: FAIL");
                return 1;
            }

            if (verifyNeural)
            {
                await VerifyNeuralPreparationAsync(log, cancellationToken);
            }

            log.Write("SELF-TEST RESULT: PASS");
            log.Write($"Report: {reportPath}");
            return 0;
        }
        catch (OperationCanceledException)
        {
            log.Write("SELF-TEST RESULT: CANCELLED");
            return 2;
        }
        catch (Exception exception)
        {
            log.Warning("SELF-TEST RESULT: FAIL", exception);
            return 1;
        }
    }

    private static async Task VerifyNeuralPreparationAsync(
        IAnnouncementLog log,
        CancellationToken cancellationToken)
    {
        EnsureNoEspeakModule(log);
        KokoroSpeechService neural = KokoroSpeechService.TryCreate(log)
            ?? throw new InvalidOperationException("Packaged Nicole/Kokoro could not be initialized.");
        WindowsSpeechService windows = WindowsSpeechService.TryCreate(log)
            ?? throw new InvalidOperationException("Windows TTS fallback is unavailable.");
        using IAnnouncementSpeechService packagedSpeech = SpeechServiceSelection.Select(
            SpeechProviderPreference.PreferNeural,
            neural,
            windows,
            log) ?? throw new InvalidOperationException("Packaged speech selection failed.");

        string[] samples =
        [
            "That was an ordinary English preparation test.",
            "Numbers and punctuation: 8, 29, 2026; ready? Yes!",
            "Unusual names: Xochitl, Siobhan, Qwynneth, and Zhyravex.",
            "Repeated preparation and disposal must remain silent and local.",
        ];

        foreach (string sample in samples)
        {
            cancellationToken.ThrowIfCancellationRequested();
            using IPreparedAnnouncementSpeech prepared =
                await packagedSpeech.PrepareAsync(sample, cancellationToken);
            log.Write($"OFFLINE PACKAGE SPEECH PREPARED (not played): {sample}");
        }

        using (var cancelled = new CancellationTokenSource())
        {
            cancelled.Cancel();
            try
            {
                using IPreparedAnnouncementSpeech ignored =
                    await packagedSpeech.PrepareAsync("Cancelled preparation must not play.", cancelled.Token);
                throw new InvalidOperationException("Pre-cancelled package speech preparation unexpectedly completed.");
            }
            catch (OperationCanceledException)
            {
                log.Write("OFFLINE PACKAGE SPEECH PRE-CANCELLATION VERIFIED.");
            }
        }

        WindowsSpeechService deliberateFallback = WindowsSpeechService.TryCreate(log)
            ?? throw new InvalidOperationException("Windows TTS fallback is unavailable.");
        using var failing = new DeliberatelyFailingSpeechService();
        using IAnnouncementSpeechService fallback = SpeechServiceSelection.Select(
            SpeechProviderPreference.PreferNeural,
            failing,
            deliberateFallback,
            log) ?? throw new InvalidOperationException("Fallback selection failed.");
        using IPreparedAnnouncementSpeech fallbackPrepared = await fallback.PrepareAsync(
            "Windows fallback preparation remained in memory and was not played.",
            cancellationToken);
        log.Write("WINDOWS TTS FALLBACK PREPARATION VERIFIED (not played).");

        EnsureNoEspeakModule(log);
    }

    private static void EnsureNoEspeakModule(IAnnouncementLog log)
    {
        try
        {
            using Process process = Process.GetCurrentProcess();
            string[] modules = process.Modules
                .Cast<ProcessModule>()
                .Select(module => module.ModuleName)
                .Where(name => name.Contains("espeak", StringComparison.OrdinalIgnoreCase))
                .ToArray();
            if (modules.Length > 0)
            {
                throw new InvalidOperationException(
                    $"eSpeak module unexpectedly loaded: {string.Join(", ", modules)}");
            }

            log.Write("eSpeak loaded-module check: none found.");
        }
        catch (InvalidOperationException)
        {
            throw;
        }
        catch (Exception exception)
        {
            throw new InvalidOperationException(
                "The self-test could not verify loaded process modules.",
                exception);
        }
    }

    private sealed class DeliberatelyFailingSpeechService : IAnnouncementSpeechService
    {
        public string ProviderName => "deliberate offline failure";

        public Task<IPreparedAnnouncementSpeech> PrepareAsync(
            string text,
            CancellationToken cancellationToken)
        {
            throw new InvalidOperationException("Intentional neural failure for fallback verification.");
        }

        public void Dispose()
        {
        }
    }

    private sealed class SelfTestLog : IAnnouncementLog, IDisposable
    {
        private readonly StreamWriter _writer;

        public SelfTestLog(string path)
        {
            _writer = new StreamWriter(
                path,
                append: false,
                new System.Text.UTF8Encoding(false))
            {
                AutoFlush = true,
            };
        }

        public void Write(string message)
        {
            string line = $"[{DateTimeOffset.Now:O}] {message}";
            Console.WriteLine(line);
            _writer.WriteLine(line);
        }

        public void Warning(string context, Exception exception)
        {
            Write($"WARNING: {context}: {exception.Message}");
        }

        public void Dispose() => _writer.Dispose();
    }
}
