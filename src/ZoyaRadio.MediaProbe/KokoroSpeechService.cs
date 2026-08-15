using KokoroSharp;
using KokoroSharp.Core;
using KokoroSharp.Processing;
using System.Diagnostics;
using System.Globalization;
using System.Text;
using Windows.Storage.Streams;

namespace ZoyaRadio.MediaProbe;

internal sealed record NeuralSpeechConfiguration(
    string VoiceName,
    string ModelRelativePath,
    TimeSpan SynthesisTimeout,
    int SampleRate,
    float Speed)
{
    public static NeuralSpeechConfiguration Default { get; } = new(
        VoiceName: "af_nicole",
        ModelRelativePath: Path.Combine(".tools", "kokoro", "kokoro.onnx"),
        SynthesisTimeout: TimeSpan.FromSeconds(20),
        SampleRate: 24_000,
        Speed: 1f);

    public void Validate()
    {
        if (string.IsNullOrWhiteSpace(VoiceName))
        {
            throw new InvalidOperationException("The neural speech voice name is required.");
        }

        if (Path.IsPathRooted(ModelRelativePath) || string.IsNullOrWhiteSpace(ModelRelativePath))
        {
            throw new InvalidOperationException("The neural model path must be a repository-relative path.");
        }

        if (SynthesisTimeout <= TimeSpan.Zero || SynthesisTimeout > TimeSpan.FromMinutes(2))
        {
            throw new InvalidOperationException("The neural synthesis timeout must be between zero and two minutes.");
        }

        if (SampleRate is < 8_000 or > 96_000)
        {
            throw new InvalidOperationException("The neural sample rate is outside the supported diagnostic range.");
        }

        if (Speed is < 0.5f or > 2f)
        {
            throw new InvalidOperationException("The neural speech speed must be between 0.5 and 2.0.");
        }
    }
}

internal sealed class KokoroSpeechService : IAnnouncementSpeechService
{
    private readonly NeuralSpeechConfiguration _configuration;
    private readonly string _modelPath;
    private readonly KokoroVoice _voice;
    private readonly IAnnouncementLog _log;
    private bool _disposed;

    private KokoroSpeechService(
        NeuralSpeechConfiguration configuration,
        string modelPath,
        KokoroVoice voice,
        IAnnouncementLog log)
    {
        _configuration = configuration;
        _modelPath = modelPath;
        _voice = voice;
        _log = log;
    }

    public string ProviderName => $"Kokoro CPU ({_configuration.VoiceName})";

    public static KokoroSpeechService? TryCreate(
        IAnnouncementLog log,
        NeuralSpeechConfiguration? configuration = null)
    {
        NeuralSpeechConfiguration selected = configuration ?? NeuralSpeechConfiguration.Default;

        try
        {
            selected.Validate();
            string modelPath = NeuralModelPathResolver.Resolve(
                selected,
                AppContext.BaseDirectory,
                PrivateAlphaBuildIdentity.IsPackagedCandidate);

            if (!File.Exists(modelPath))
            {
                log.Write($"NEURAL TTS UNAVAILABLE: Kokoro model is missing at {modelPath}");
                return null;
            }

            KokoroVoice voice = KokoroVoiceManager.GetVoice(selected.VoiceName);
            if (!string.Equals(voice.Name, selected.VoiceName, StringComparison.OrdinalIgnoreCase))
            {
                log.Write($"NEURAL TTS UNAVAILABLE: voice {selected.VoiceName} could not be loaded.");
                return null;
            }

            var model = new FileInfo(modelPath);
            log.Write("Neural TTS mechanism: KokoroSharp.CPU 0.8.0 with Microsoft ONNX Runtime 1.22.0");
            log.Write($"Selected neural voice: {voice.Name} ({voice.Language}, {voice.Gender})");
            log.Write($"Neural model: {modelPath}");
            log.Write($"Neural model size: {model.Length:N0} bytes ({model.Length / 1024d / 1024d:N1} MiB)");
            log.Write("Neural execution provider: CPU only; no CUDA provider and no GPU model load");
            log.Write("Neural model lifecycle: load for preparation, dispose before Firefox pause");
            log.Write("Prepared neural audio: in-memory WAV; no temporary audio file is created");
            return new KokoroSpeechService(selected, modelPath, voice, log);
        }
        catch (Exception exception)
        {
            log.Warning("Could not initialize the local Kokoro neural voice", exception);
            return null;
        }
    }

    public async Task<IPreparedAnnouncementSpeech> PrepareAsync(
        string text,
        CancellationToken cancellationToken)
    {
        ObjectDisposedException.ThrowIf(_disposed, this);

        if (string.IsNullOrWhiteSpace(text))
        {
            throw new InvalidOperationException("Neural speech text is empty.");
        }

        cancellationToken.ThrowIfCancellationRequested();
        _log.Write("NEURAL TTS SYNTHESIS STARTED while Firefox remains Playing.");
        _log.Write($"Neural voice: {_configuration.VoiceName}; text length: {text.Length} characters");
        _log.Write($"Neural resources before load: {MeasureProcessMemory()}");

        var totalTimer = Stopwatch.StartNew();
        var loadTimer = Stopwatch.StartNew();
        KokoroTTS? tts = null;
        KokoroJob? job = null;

        try
        {
            tts = KokoroTTS.LoadModel(_modelPath);
            loadTimer.Stop();
            _log.Write($"Neural model loaded in {loadTimer.Elapsed.TotalSeconds:N3} seconds.");
            _log.Write($"Neural resources after load: {MeasureProcessMemory()}");

            int[] tokens = Tokenizer.Tokenize(text, "en-us", preprocess: true);
            if (tokens.Length == 0)
            {
                throw new InvalidOperationException("Neural phonemization produced no tokens.");
            }

            var completion = new TaskCompletionSource<float[]>(
                TaskCreationOptions.RunContinuationsAsynchronously);
            job = KokoroJob.Create(
                tokens,
                _voice,
                _configuration.Speed,
                samples => completion.TrySetResult(samples));
            tts.EnqueueJob(job);

            float[] samples;

            try
            {
                Task<float[]> synthesis = completion.Task.WaitAsync(_configuration.SynthesisTimeout);

                if (cancellationToken.CanBeCanceled)
                {
                    Task cancellation = Task.Delay(Timeout.InfiniteTimeSpan, cancellationToken);
                    Task first = await Task.WhenAny(synthesis, cancellation);

                    if (first == cancellation)
                    {
                        _log.Write(
                            "Neural preparation cancellation received; waiting for the in-flight CPU inference " +
                            "to finish safely, then discarding its audio before any playback pause.");

                        try
                        {
                            await synthesis;
                        }
                        catch
                        {
                            // Cancellation remains the public result; disposal still runs below.
                        }

                        throw new OperationCanceledException(cancellationToken);
                    }
                }

                samples = await synthesis;
            }
            catch (TimeoutException)
            {
                job.Cancel();
                throw new TimeoutException(
                    $"Neural synthesis exceeded {_configuration.SynthesisTimeout.TotalSeconds:N0} seconds.");
            }
            catch (OperationCanceledException)
            {
                throw;
            }

            ValidateSamples(samples, _configuration.SampleRate);
            byte[] wave = PcmWaveEncoder.Encode16BitMono(samples, _configuration.SampleRate);
            var stream = new InMemoryRandomAccessStream();

            try
            {
                using (var writer = new DataWriter(stream))
                {
                    writer.WriteBytes(wave);
                    await writer.StoreAsync();
                    await writer.FlushAsync();
                    writer.DetachStream();
                }

                cancellationToken.ThrowIfCancellationRequested();
                stream.Seek(0);
                TimeSpan audioDuration = TimeSpan.FromSeconds(
                    samples.Length / (double)_configuration.SampleRate);
                totalTimer.Stop();
                _log.Write(
                    $"NEURAL TTS SYNTHESIS COMPLETED: {totalTimer.Elapsed.TotalSeconds:N3} seconds; " +
                    $"prepared audio {audioDuration.TotalSeconds:N3} seconds; " +
                    $"real-time factor {totalTimer.Elapsed.TotalSeconds / audioDuration.TotalSeconds:N3}x.");
                _log.Write("Neural audio verified and held in memory; Firefox has not yet been paused.");
                return new MediaPlayerPreparedSpeech(stream, "audio/wav", audioDuration);
            }
            catch
            {
                stream.Dispose();
                throw;
            }
        }
        finally
        {
            if (job is not null && !job.isDone)
            {
                job.Cancel();
            }

            if (tts is not null)
            {
                tts.Dispose();
                _log.Write("Neural model dispose requested before playback pause.");
            }

            _log.Write($"Neural resources after disposal: {MeasureProcessMemory()}");
        }
    }

    public void Dispose()
    {
        _disposed = true;
    }

    private static void ValidateSamples(float[] samples, int sampleRate)
    {
        if (samples.Length < sampleRate / 4)
        {
            throw new InvalidOperationException("Neural synthesis returned too little audio to use safely.");
        }

        float peak = 0f;

        foreach (float sample in samples)
        {
            if (!float.IsFinite(sample))
            {
                throw new InvalidOperationException("Neural synthesis returned malformed audio samples.");
            }

            peak = Math.Max(peak, Math.Abs(sample));
        }

        if (peak < 0.0001f)
        {
            throw new InvalidOperationException("Neural synthesis returned silent audio.");
        }
    }

    private static string MeasureProcessMemory()
    {
        using Process process = Process.GetCurrentProcess();
        process.Refresh();
        return $"working set {process.WorkingSet64 / 1024d / 1024d:N1} MiB; " +
            $"private memory {process.PrivateMemorySize64 / 1024d / 1024d:N1} MiB; " +
            "VRAM 0 MiB requested (CPU provider)";
    }
}

internal static class RepositoryPathLocator
{
    public static string FindRepositoryRoot()
    {
        foreach (string startingPath in new[] { Environment.CurrentDirectory, AppContext.BaseDirectory })
        {
            for (var directory = new DirectoryInfo(startingPath); directory is not null; directory = directory.Parent)
            {
                if (Directory.Exists(Path.Combine(directory.FullName, ".git"))
                    && File.Exists(Path.Combine(directory.FullName, "README.md")))
                {
                    return directory.FullName;
                }
            }
        }

        throw new DirectoryNotFoundException("Could not locate the ZoyaRadio repository root.");
    }
}

internal static class PcmWaveEncoder
{
    public static byte[] Encode16BitMono(IReadOnlyList<float> samples, int sampleRate)
    {
        const short channelCount = 1;
        const short bitsPerSample = 16;
        var bytesPerSample = bitsPerSample / 8;
        var dataSize = checked(samples.Count * bytesPerSample);

        using var stream = new MemoryStream(capacity: checked(44 + dataSize));
        using var writer = new BinaryWriter(stream, Encoding.ASCII, leaveOpen: true);
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

        foreach (float sample in samples)
        {
            float clamped = Math.Clamp(sample, -1f, 1f);
            writer.Write((short)Math.Round(clamped * short.MaxValue));
        }

        writer.Flush();
        return stream.ToArray();
    }
}
