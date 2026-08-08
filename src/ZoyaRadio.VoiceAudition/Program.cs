using KokoroSharp;
using KokoroSharp.Core;
using KokoroSharp.Processing;
using System.Diagnostics;
using System.Globalization;
using System.Reflection;
using System.Security.Cryptography;
using System.Text;

const int sampleRate = 24_000;
const string auditionText = "Steve, that was ‘Cochise’ by Audioslave. It certainly didn't tiptoe into the room; subtle as a damn marching band, wasn't it?";
const string unicodeText = "Steve, that was ‘Группа крови’ by Кино. The Cyrillic metadata must remain exactly as supplied—even when this English voice cannot pronounce it well.";

var repositoryRoot = FindRepositoryRoot();
var modelPath = Path.Combine(repositoryRoot, ".tools", "kokoro", "kokoro.onnx");
var outputDirectory = Path.Combine(
    repositoryRoot,
    "artifacts",
    "voice-audition",
    DateTime.Now.ToString("yyyyMMdd-HHmmss-fff", CultureInfo.InvariantCulture));

Directory.CreateDirectory(outputDirectory);
using var log = new AuditionLog(Path.Combine(outputDirectory, "voice-audition-report.txt"));

log.Write("ZOYA RADIO MILESTONE 7 - KOKORO VOICE AUDITION");
log.Write("This diagnostic generates WAV files only. It does not play audio, inspect Firefox, or control playback.");
log.Write($"Output folder: {outputDirectory}");
log.Write($"Runtime: .NET {Environment.Version} on {Environment.OSVersion}");
log.Write($"KokoroSharp assembly: {GetPackageVersion(typeof(KokoroTTS).Assembly)}");
log.Write("NuGet package: KokoroSharp.CPU 0.8.0");
log.Write("Inference provider: CPU only (Microsoft.ML.OnnxRuntime; no CUDA provider requested)");
log.Write($"Main audition text: {auditionText}");
log.Write($"Unicode robustness text: {unicodeText}");
log.Write("Russian/Cyrillic is not a documented Kokoro language. The Unicode sample checks preservation and failure behavior, not pronunciation quality.");

if (!File.Exists(modelPath))
{
    log.Write($"ERROR: Model not found at {modelPath}");
    Environment.ExitCode = 1;
    return;
}

var modelInfo = new FileInfo(modelPath);
log.Write($"Model: {modelPath}");
log.Write($"Model size: {modelInfo.Length:N0} bytes ({ToMiB(modelInfo.Length):N1} MiB)");
using (var modelStream = File.OpenRead(modelPath))
{
    log.Write($"Model SHA-256: {Convert.ToHexString(SHA256.HashData(modelStream))}");
}

var baseline = ResourceSnapshot.Capture();
log.Write($"Before model load: {baseline}");

KokoroTTS? tts = null;
var successfulSamples = 0;
var failedSamples = 0;

try
{
    var loadTimer = Stopwatch.StartNew();
    tts = KokoroTTS.LoadModel(modelPath);
    loadTimer.Stop();
    log.Write($"MODEL LOADED in {loadTimer.Elapsed.TotalSeconds:N3} seconds");
    log.Write($"After model load: {ResourceSnapshot.Capture()}");

    var samples = new[]
    {
        new AuditionSample("01-af-heart", "af_heart", KokoroLanguage.AmericanEnglish, auditionText, false),
        new AuditionSample("02-af-bella", "af_bella", KokoroLanguage.AmericanEnglish, auditionText, false),
        new AuditionSample("03-af-nicole", "af_nicole", KokoroLanguage.AmericanEnglish, auditionText, false),
        new AuditionSample("04-bf-emma", "bf_emma", KokoroLanguage.BritishEnglish, auditionText, false),
        new AuditionSample("05-af-sarah", "af_sarah", KokoroLanguage.AmericanEnglish, auditionText, false),
        new AuditionSample("06-unicode-af-heart", "af_heart", KokoroLanguage.AmericanEnglish, unicodeText, true),
    };

    foreach (var sample in samples)
    {
        log.Write(string.Empty);
        log.Write($"SAMPLE STARTED: {sample.FileStem}.wav");
        log.Write($"Voice: {sample.VoiceName} ({sample.Language})");
        log.Write($"Text: {sample.Text}");

        try
        {
            var timer = Stopwatch.StartNew();
            var audio = await SynthesizeAsync(tts, sample, TimeSpan.FromSeconds(60));
            timer.Stop();

            if (audio.Length == 0)
            {
                throw new InvalidOperationException("Kokoro returned no audio samples.");
            }

            var outputPath = Path.Combine(outputDirectory, $"{sample.FileStem}.wav");
            WavFileWriter.WritePcm16(outputPath, audio, sampleRate);
            var duration = TimeSpan.FromSeconds(audio.Length / (double)sampleRate);
            var realTimeFactor = timer.Elapsed.TotalSeconds / duration.TotalSeconds;

            successfulSamples++;
            log.Write($"SAMPLE COMPLETED: synthesis {timer.Elapsed.TotalSeconds:N3} s; audio {duration.TotalSeconds:N3} s; real-time factor {realTimeFactor:N3}x");
            log.Write($"Saved: {outputPath}");
            if (sample.IsUnicodeRobustnessTest)
            {
                log.Write("Unicode result: source text remained intact through tokenization and file/report handling. Listen manually to assess omissions or pronunciation.");
            }
        }
        catch (Exception exception)
        {
            failedSamples++;
            log.Write($"SAMPLE FAILED: {exception.GetType().Name}: {exception.Message}");
            if (sample.IsUnicodeRobustnessTest)
            {
                log.Write("Unicode result: unsupported-language synthesis failed safely; the original Cyrillic text remains intact in this UTF-8 report.");
            }
        }
    }
}
catch (Exception exception)
{
    log.Write($"FATAL AUDITION ERROR: {exception.GetType().Name}: {exception.Message}");
    Environment.ExitCode = 1;
}
finally
{
    if (tts is not null)
    {
        tts.Dispose();
        log.Write("MODEL DISPOSE REQUESTED: yes");
    }

    GC.Collect();
    GC.WaitForPendingFinalizers();
    GC.Collect();
    log.Write($"After disposal: {ResourceSnapshot.Capture()}");
    log.Write("Model intentionally left loaded after audition: no");
    log.Write($"Samples completed: {successfulSamples}; failed: {failedSamples}");
    log.Write($"FINAL RESULT: {(successfulSamples >= 5 && Environment.ExitCode == 0 ? "AUDITION FILES READY FOR MANUAL LISTENING" : "AUDITION INCOMPLETE - REVIEW ERRORS")}");
}

static async Task<float[]> SynthesizeAsync(KokoroTTS tts, AuditionSample sample, TimeSpan timeout)
{
    var languageCode = sample.Language == KokoroLanguage.BritishEnglish ? "en-gb" : "en-us";
    var tokens = Tokenizer.Tokenize(sample.Text, languageCode, preprocess: true);
    if (tokens.Length == 0)
    {
        throw new InvalidOperationException("Phonemization produced no tokens.");
    }

    var voice = KokoroVoiceManager.GetVoice(sample.VoiceName);
    var completion = new TaskCompletionSource<float[]>(TaskCreationOptions.RunContinuationsAsynchronously);
    var job = KokoroJob.Create(tokens, voice, speed: 1f, samples => completion.TrySetResult(samples));
    tts.EnqueueJob(job);

    try
    {
        return await completion.Task.WaitAsync(timeout);
    }
    catch (TimeoutException)
    {
        job.Cancel();
        throw new TimeoutException($"Synthesis exceeded the {timeout.TotalSeconds:N0}-second audition limit.");
    }
}

static string FindRepositoryRoot()
{
    foreach (var startingPath in new[] { Environment.CurrentDirectory, AppContext.BaseDirectory })
    {
        for (var directory = new DirectoryInfo(startingPath); directory is not null; directory = directory.Parent)
        {
            if (Directory.Exists(Path.Combine(directory.FullName, ".git")) &&
                File.Exists(Path.Combine(directory.FullName, "README.md")))
            {
                return directory.FullName;
            }
        }
    }

    throw new DirectoryNotFoundException("Could not locate the ZoyaRadio repository root.");
}

static string GetPackageVersion(Assembly assembly)
{
    return assembly.GetCustomAttribute<AssemblyInformationalVersionAttribute>()?.InformationalVersion
        ?? assembly.GetName().Version?.ToString()
        ?? "<not provided>";
}

static double ToMiB(long bytes) => bytes / 1024d / 1024d;

internal sealed record AuditionSample(
    string FileStem,
    string VoiceName,
    KokoroLanguage Language,
    string Text,
    bool IsUnicodeRobustnessTest);

internal sealed class AuditionLog : IDisposable
{
    private readonly StreamWriter writer;

    public AuditionLog(string path)
    {
        writer = new StreamWriter(path, append: false, new UTF8Encoding(encoderShouldEmitUTF8Identifier: false))
        {
            AutoFlush = true,
        };
    }

    public void Write(string message)
    {
        var line = $"[{DateTimeOffset.Now:yyyy-MM-dd HH:mm:ss.fff zzz}] {message}";
        Console.WriteLine(line);
        writer.WriteLine(line);
    }

    public void Dispose() => writer.Dispose();
}

internal sealed record ResourceSnapshot(long WorkingSetBytes, long PrivateBytes, string ProcessGpuMemory)
{
    public static ResourceSnapshot Capture()
    {
        using var process = Process.GetCurrentProcess();
        process.Refresh();
        return new ResourceSnapshot(process.WorkingSet64, process.PrivateMemorySize64, QueryOwnGpuMemory(process.Id));
    }

    public override string ToString() =>
        $"working set {WorkingSetBytes / 1024d / 1024d:N1} MiB; private memory {PrivateBytes / 1024d / 1024d:N1} MiB; app VRAM {ProcessGpuMemory}";

    private static string QueryOwnGpuMemory(int processId)
    {
        try
        {
            using var process = Process.Start(new ProcessStartInfo
            {
                FileName = "nvidia-smi.exe",
                Arguments = "--query-compute-apps=pid,used_gpu_memory --format=csv,noheader,nounits",
                CreateNoWindow = true,
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                UseShellExecute = false,
            });

            if (process is null || !process.WaitForExit(5_000) || process.ExitCode != 0)
            {
                return "<measurement unavailable>";
            }

            foreach (var line in process.StandardOutput.ReadToEnd().Split('\n', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries))
            {
                var fields = line.Split(',', StringSplitOptions.TrimEntries);
                if (fields.Length >= 2 && int.TryParse(fields[0], CultureInfo.InvariantCulture, out var pid) && pid == processId)
                {
                    return int.TryParse(fields[1], CultureInfo.InvariantCulture, out var mebibytes)
                        ? $"{mebibytes:N0} MiB (measured by nvidia-smi)"
                        : $"{fields[1]} MiB (reported by nvidia-smi)";
                }
            }

            return "not listed by nvidia-smi (expected for CPU-only inference)";
        }
        catch
        {
            return "<measurement unavailable>";
        }
    }
}

internal static class WavFileWriter
{
    public static void WritePcm16(string path, IReadOnlyList<float> samples, int sampleRate)
    {
        const short channelCount = 1;
        const short bitsPerSample = 16;
        var bytesPerSample = bitsPerSample / 8;
        var dataSize = checked(samples.Count * bytesPerSample);

        using var stream = File.Create(path);
        using var writer = new BinaryWriter(stream, Encoding.ASCII, leaveOpen: false);
        writer.Write(Encoding.ASCII.GetBytes("RIFF"));
        writer.Write(36 + dataSize);
        writer.Write(Encoding.ASCII.GetBytes("WAVE"));
        writer.Write(Encoding.ASCII.GetBytes("fmt "));
        writer.Write(16);
        writer.Write((short)1);
        writer.Write(channelCount);
        writer.Write(sampleRate);
        writer.Write(sampleRate * channelCount * bytesPerSample);
        writer.Write((short)(channelCount * bytesPerSample));
        writer.Write(bitsPerSample);
        writer.Write(Encoding.ASCII.GetBytes("data"));
        writer.Write(dataSize);

        foreach (var sample in samples)
        {
            var clamped = Math.Clamp(sample, -1f, 1f);
            writer.Write((short)Math.Round(clamped * short.MaxValue));
        }
    }
}
