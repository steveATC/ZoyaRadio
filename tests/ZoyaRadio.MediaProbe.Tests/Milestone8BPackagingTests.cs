using System.Net;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using ZoyaRadio.MediaProbe;

internal static class Milestone8BPackagingTests
{
    public static void IdentityAndApplicationRelativeAssets()
    {
        Equal("0.8.0-alpha.1", PrivateAlphaIdentity.CandidateVersion);
        Equal("win-x64", PrivateAlphaIdentity.RuntimeIdentifier);
        Equal("ZoyaRadio-0.8.0-alpha.1-win-x64.zip", PrivateAlphaIdentity.PackageFileName);
        Contains(PrivateAlphaIdentity.SupportedWindows, "19045");

        string application = Path.GetFullPath(Path.Combine(Path.GetTempPath(), "zoya-app"));
        string unrelated = Path.GetFullPath(Path.Combine(Path.GetTempPath(), "other-cwd"));
        string original = Environment.CurrentDirectory;
        Directory.CreateDirectory(unrelated);

        try
        {
            Environment.CurrentDirectory = unrelated;
            string resolved = NeuralModelPathResolver.Resolve(
                NeuralSpeechConfiguration.Default,
                application,
                packagedCandidate: true);
            Equal(
                Path.GetFullPath(
                    Path.Combine(application, PrivateAlphaIdentity.PackagedModelRelativePath)),
                resolved);
            False(resolved.StartsWith(unrelated, StringComparison.OrdinalIgnoreCase));
        }
        finally
        {
            Environment.CurrentDirectory = original;
            Directory.Delete(unrelated, recursive: true);
        }
    }

    public static void ValidManifestAndHashes()
    {
        using var package = TestPackage.Create();
        PackageIntegrityResult result = PrivateAlphaPackageIntegrity.Verify(
            package.Root,
            verifyEveryFile: true);

        True(result.IsValid, string.Join(" | ", result.Issues));
        Equal(PrivateAlphaIdentity.RequiredCriticalFiles.Length, result.VerifiedFileCount);
        True(result.ManifestPackageBytes > 0);
    }

    public static void MissingAndCorruptCriticalAssets()
    {
        using (var package = TestPackage.Create())
        {
            string missing = Path.Combine(package.Root, PrivateAlphaIdentity.EntryPoint);
            File.Delete(missing);
            PackageIntegrityResult result = PrivateAlphaPackageIntegrity.Verify(
                package.Root,
                verifyEveryFile: false);
            Equal(PackageIntegrityStatus.Corrupt, result.Status);
            True(result.Issues.Any(issue => issue.Contains("missing", StringComparison.OrdinalIgnoreCase)));
        }

        using (var package = TestPackage.Create())
        {
            string corrupt = Path.Combine(package.Root, PrivateAlphaIdentity.EntryPoint);
            File.AppendAllText(corrupt, "changed", Encoding.UTF8);
            PackageIntegrityResult result = PrivateAlphaPackageIntegrity.Verify(
                package.Root,
                verifyEveryFile: false);
            Equal(PackageIntegrityStatus.Corrupt, result.Status);
            True(result.Issues.Any(issue =>
                issue.Contains("manifest", StringComparison.OrdinalIgnoreCase)));
        }

        using (var package = TestPackage.Create())
        {
            string manifestPath = Path.Combine(package.Root, PrivateAlphaIdentity.ManifestFileName);
            PrivateAlphaManifest manifest = JsonSerializer.Deserialize<PrivateAlphaManifest>(
                File.ReadAllText(manifestPath))!;
            PrivateAlphaManifestFile[] entries = manifest.Files.ToArray();
            entries[0] = entries[0] with { Size = long.MaxValue };
            entries[1] = entries[1] with { Size = 1 };
            File.WriteAllText(
                manifestPath,
                JsonSerializer.Serialize(manifest with { Files = entries }),
                new UTF8Encoding(false));

            PackageIntegrityResult result = PrivateAlphaPackageIntegrity.Verify(
                package.Root,
                verifyEveryFile: false);
            Equal(PackageIntegrityStatus.Corrupt, result.Status);
            True(result.Issues.Any(issue =>
                issue.Contains("package-size", StringComparison.Ordinal)));
        }

        string absent = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString("N"));
        PackageIntegrityResult absentResult = PrivateAlphaPackageIntegrity.Verify(
            absent,
            verifyEveryFile: true);
        Equal(PackageIntegrityStatus.Missing, absentResult.Status);
    }

    public static void PackageExclusionAudit()
    {
        string[] forbidden =
        [
            "espeak-ng-data/en_dict",
            "voices/af_bella.npy",
            "private/app.pdb",
            "native/onnxruntime.lib",
            "runtimes/linux-x64/native/libonnxruntime.so",
            "tools/firefox.exe",
            "tools/ollama.exe",
            "logs/history.txt",
            "test-audio.wav",
        ];

        foreach (string relative in forbidden)
        {
            using var package = TestPackage.Create(relative);
            PackageIntegrityResult result = PrivateAlphaPackageIntegrity.Verify(
                package.Root,
                verifyEveryFile: true);
            False(result.IsValid, relative);
        }

        using var valid = TestPackage.Create();
        string[] npyFiles = Directory.GetFiles(valid.Root, "*.npy", SearchOption.AllDirectories);
        Equal(1, npyFiles.Length);
        True(npyFiles[0].EndsWith("af_nicole.npy", StringComparison.OrdinalIgnoreCase));
    }

    public static void UnicodeManifestContentIsPreserved()
    {
        using var package = TestPackage.Create("docs/Юникод.txt", "Привет, Зоя");
        PackageIntegrityResult result = PrivateAlphaPackageIntegrity.Verify(
            package.Root,
            verifyEveryFile: true);
        True(result.IsValid, string.Join(" | ", result.Issues));
        Equal(
            "Привет, Зоя",
            File.ReadAllText(Path.Combine(package.Root, "docs", "Юникод.txt"), Encoding.UTF8));
    }

    public static void ReadinessPresentationCoversSafeStates()
    {
        PrivateAlphaReadinessSnapshot ready = Snapshot(
            DiskReadinessState.Sufficient,
            FirefoxPrerequisiteState.Active,
            OllamaReadinessState.RunningCompatible,
            QwenReadinessState.Present);
        PrivateAlphaReadinessPresentation presentation =
            PrivateAlphaReadinessPresenter.Present(ready);
        True(ready.ReadyForNormalOperation);
        True(presentation.CanStartSafely);
        Contains(presentation.Summary, "Ready");
        Contains(string.Join("\n", presentation.Details), "exactly one eligible Playing");
        Contains(string.Join("\n", presentation.Details), "%LOCALAPPDATA%");
        Equal(4, WindowsPrivateAlphaEnvironment.RequiredVisualCppLibraries.Count);
        foreach (string library in new[]
                 {
                     "vcruntime140.dll", "vcruntime140_1.dll", "msvcp140.dll", "msvcp140_1.dll",
                 })
        {
            True(WindowsPrivateAlphaEnvironment.RequiredVisualCppLibraries.Contains(library));
        }

        PrivateAlphaReadinessSnapshot idle = ready with
        {
            Firefox = FirefoxPrerequisiteState.InstalledIdle,
        };
        True(idle.ReadyForNormalOperation);
        Contains(
            string.Join("\n", PrivateAlphaReadinessPresenter.Present(idle).Details),
            "not an installation failure");

        PrivateAlphaReadinessSnapshot insufficient = ready with
        {
            Disk = DiskReadinessState.Insufficient,
            AvailableDiskBytes = 1,
            Ollama = ready.Ollama with { Qwen = QwenReadinessState.Missing },
        };
        False(insufficient.CanRequestModel);
        Contains(
            string.Join("\n", PrivateAlphaReadinessPresenter.Present(insufficient).Details),
            "no model download will begin");

        PrivateAlphaReadinessSnapshot corrupt = ready with
        {
            Package = new PackageIntegrityResult(
                PackageIntegrityStatus.Corrupt,
                "Package corrupt",
                ["bad hash"],
                0,
                0),
        };
        False(corrupt.CanStartSafely);
    }

    public static async Task OllamaAbsentAndStoppedAreReadOnly()
    {
        var handler = new QueueHttpHandler();
        using var client = Client(handler);

        OllamaReadinessResult absent = await client.CheckAsync(false, false, CancellationToken.None);
        Equal(OllamaReadinessState.Absent, absent.Ollama);
        Equal(0, handler.Requests.Count);

        OllamaReadinessResult stopped = await client.CheckAsync(true, false, CancellationToken.None);
        Equal(OllamaReadinessState.InstalledStopped, stopped.Ollama);
        Equal(0, handler.Requests.Count);
    }

    public static async Task OllamaCompatibilityAndQwenIdentity()
    {
        var missingHandler = new QueueHttpHandler(
            Json(HttpStatusCode.OK, "{\"version\":\"0.32.6\"}"),
            Json(HttpStatusCode.OK, "{\"models\":[]}"));
        using (var missing = Client(missingHandler))
        {
            OllamaReadinessResult result = await missing.CheckAsync(true, true, CancellationToken.None);
            Equal(OllamaReadinessState.RunningCompatible, result.Ollama);
            Equal(QwenReadinessState.Missing, result.Qwen);
            Equal("0.32.6", result.Version!);
        }

        var presentHandler = new QueueHttpHandler(
            Json(HttpStatusCode.OK, "{\"version\":\"0.99.0\"}"),
            Json(
                HttpStatusCode.OK,
                "{\"models\":[{\"name\":\"qwen3:4b-instruct\",\"digest\":\"sha256:test\",\"size\":2900000000}]}"));
        using (var present = Client(presentHandler))
        {
            OllamaReadinessResult result = await present.CheckAsync(true, true, CancellationToken.None);
            Equal(QwenReadinessState.Present, result.Qwen);
            Equal("sha256:test", result.ModelDigest!);
            Equal(2_900_000_000L, result.ModelSize!.Value);
        }
    }

    public static async Task OllamaUnreachableAndIncompatible()
    {
        var unreachableHandler = new QueueHttpHandler(
            (_, _) => throw new HttpRequestException("offline"));
        using (var client = Client(unreachableHandler))
        {
            OllamaReadinessResult result = await client.CheckAsync(true, true, CancellationToken.None);
            Equal(OllamaReadinessState.Unreachable, result.Ollama);
        }

        var malformedHandler = new QueueHttpHandler(
            Json(HttpStatusCode.OK, "not-json"));
        using (var client = Client(malformedHandler))
        {
            OllamaReadinessResult result = await client.CheckAsync(true, true, CancellationToken.None);
            Equal(OllamaReadinessState.Incompatible, result.Ollama);
        }

        var missingCapability = new QueueHttpHandler(
            Json(HttpStatusCode.OK, "{\"version\":\"1.0\"}"),
            Json(HttpStatusCode.OK, "{}"));
        using (var client = Client(missingCapability))
        {
            OllamaReadinessResult result = await client.CheckAsync(true, true, CancellationToken.None);
            Equal(OllamaReadinessState.Incompatible, result.Ollama);
        }
    }

    public static async Task ModelPullRequiresConsentAndDiskSpace()
    {
        var handler = new QueueHttpHandler();
        using var client = Client(handler);

        OllamaPullResult noConsent = await client.PullModelAsync(
            false,
            long.MaxValue,
            null,
            CancellationToken.None);
        Equal(OllamaPullStatus.ConsentRequired, noConsent.Status);

        OllamaPullResult noSpace = await client.PullModelAsync(
            true,
            OllamaSetupPolicy.MinimumFreeBytesBeforePull - 1,
            null,
            CancellationToken.None);
        Equal(OllamaPullStatus.InsufficientSpace, noSpace.Status);
        Equal(0, handler.Requests.Count);
        Contains(OllamaSetupPolicy.DownloadDisclosure, "approximately 2.7 GiB");
        Contains(OllamaSetupPolicy.DownloadDisclosure, "own terms");
    }

    public static async Task ModelPullReportsProgressWithoutAcceptingTerms()
    {
        const string response =
            "{\"status\":\"pulling manifest\"}\n" +
            "{\"status\":\"downloading\",\"completed\":50,\"total\":100}\n" +
            "{\"status\":\"success\"}\n";
        var handler = new QueueHttpHandler(Json(HttpStatusCode.OK, response));
        using var client = Client(handler);
        var updates = new List<OllamaPullProgress>();

        OllamaPullResult result = await client.PullModelAsync(
            true,
            long.MaxValue,
            new InlineProgress<OllamaPullProgress>(updates.Add),
            CancellationToken.None);

        True(result.Succeeded);
        True(updates.Any(update => update.Percent == 50d));
        Equal(1, handler.Requests.Count);
        string body = handler.Requests[0].Body;
        Contains(body, "qwen3:4b-instruct");
        Contains(body, "\"stream\":true");
        False(body.Contains("accept", StringComparison.OrdinalIgnoreCase));
        False(body.Contains("terms", StringComparison.OrdinalIgnoreCase));
    }

    public static async Task ModelPullCancellationAndInterruptionAreSafe()
    {
        var cancelledHandler = new QueueHttpHandler(
            async (_, token) =>
            {
                await Task.Delay(Timeout.InfiniteTimeSpan, token);
                return new HttpResponseMessage(HttpStatusCode.OK)
                {
                    Content = new StringContent(string.Empty),
                };
            });
        using (var client = Client(cancelledHandler))
        using (var cancellation = new CancellationTokenSource())
        {
            cancellation.Cancel();
            OllamaPullResult result = await client.PullModelAsync(
                true,
                long.MaxValue,
                null,
                cancellation.Token);
            Equal(OllamaPullStatus.Cancelled, result.Status);
        }

        var interruptedHandler = new QueueHttpHandler(
            Json(HttpStatusCode.OK, "{\"status\":\"downloading\",\"completed\":1,\"total\":100}\n"));
        using (var client = Client(interruptedHandler))
        {
            OllamaPullResult result = await client.PullModelAsync(
                true,
                long.MaxValue,
                null,
                CancellationToken.None);
            Equal(OllamaPullStatus.Failed, result.Status);
            Contains(result.Message, "before success");
        }
    }

    public static async Task SetupControllerUsesExplicitExternalActionsOnly()
    {
        var environment = new FakeEnvironment
        {
            AvailableDiskBytes = long.MaxValue,
            Firefox = FirefoxPrerequisiteState.InstalledIdle,
            OllamaInstalled = true,
            OllamaRunning = true,
        };
        var ollama = new FakeOllamaSetupApi
        {
            Readiness = new OllamaReadinessResult(
                OllamaReadinessState.RunningCompatible,
                QwenReadinessState.Missing,
                "0.32.6",
                null,
                null,
                "Qwen missing"),
        };
        var opener = new FakeUriOpener();
        using var controller = new PrivateAlphaSetupController(
            environment,
            ollama,
            opener,
            Path.GetTempPath(),
            "%LOCALAPPDATA%\\Zoya Radio\\logs");

        PrivateAlphaReadinessSnapshot snapshot = await controller.CheckAsync();
        True(snapshot.CanRequestModel);
        Equal(0, opener.OpenCalls);
        Equal(0, ollama.PullCalls);
        Equal(1, environment.RunningChecks);

        OllamaPullResult refused = await controller.RequestQwenAsync(
            explicitConsent: false,
            progress: null);
        Equal(OllamaPullStatus.ConsentRequired, refused.Status);
        Equal(1, ollama.PullCalls);
        False(ollama.LastConsent);

        controller.OpenOfficialOllamaDownloadPage();
        Equal(1, opener.OpenCalls);
        Equal(PrivateAlphaSetupController.OfficialOllamaDownloadPage, opener.LastUri!);
    }

    public static void ZipDigestRoundTrips()
    {
        string path = Path.Combine(Path.GetTempPath(), $"zoya-digest-{Guid.NewGuid():N}.zip");
        try
        {
            File.WriteAllBytes(path, [0, 1, 2, 3, 255]);
            string expected = Convert.ToHexString(SHA256.HashData(File.ReadAllBytes(path)));
            Equal(expected, PrivateAlphaPackageDigest.ComputeSha256(path));
            True(PrivateAlphaPackageDigest.VerifySha256(path, expected.ToLowerInvariant()));
            False(PrivateAlphaPackageDigest.VerifySha256(path, new string('0', 64)));
        }
        finally
        {
            File.Delete(path);
        }
    }

    public static void PackagingScriptEncodesDistributionGates()
    {
        string root = RepositoryPathLocator.FindRepositoryRoot();
        string script = File.ReadAllText(
            Path.Combine(root, "scripts", "New-PrivateAlphaCandidate.ps1"));

        Contains(script, "--self-contained true");
        Contains(script, "-r $runtimeIdentifier");
        Contains(script, "-p:PrivateAlphaBuild=true");
        Contains(script, "--no-restore");
        Contains(script, "expectedModelHash");
        Contains(script, "expectedVoiceHash");
        Contains(script, "content\\espeak");
        Contains(script, "eSpeakHashMatches = 0");
        Contains(script, "private-symbols");
        Contains(script, "*.pdb");
        Contains(script, "--package-self-test");
        Contains(script, "--neural-package-self-test");
        Contains(script, "New-DeterministicZip");
        Contains(script, "Get-FileHash -Algorithm SHA256");
        Contains(script, "Apache-2.0-Misaki-Kokoro-Model-and-Voices.txt");
        Contains(script, "Firefox");
        Contains(script, "ollama.exe");
        Contains(script, "listening-history");
        False(script.Contains("dotnet restore", StringComparison.OrdinalIgnoreCase));
        False(script.Contains("ollama pull", StringComparison.OrdinalIgnoreCase));
    }

    public static void TesterDocumentationAndTermsArePresent()
    {
        string root = RepositoryPathLocator.FindRepositoryRoot();
        string distribution = Path.Combine(root, "distribution", "private-alpha");
        string[] required =
        [
            "TESTER-README.txt",
            "PRIVATE-ALPHA-TERMS.txt",
            "THIRD-PARTY-NOTICES.txt",
            "CLEAN-MACHINE-CHECKLIST.md",
            "EVIDENCE-TEMPLATE.md",
            Path.Combine("licenses", "KokoroSharp-MIT.txt"),
            Path.Combine("licenses", "OpenTK-MIT.txt"),
            Path.Combine("licenses", "OpenTK-THIRD-PARTIES.txt"),
            Path.Combine("licenses", "Microsoft-Windows-SDK-License.rtf"),
        ];
        foreach (string relative in required)
        {
            True(File.Exists(Path.Combine(distribution, relative)), relative);
        }

        string readme = File.ReadAllText(Path.Combine(distribution, "TESTER-README.txt"));
        Contains(readme, "Windows 10 22H2");
        Contains(readme, "SmartScreen");
        Contains(readme, "separate folder");
        Contains(readme, "rollback");
        Contains(readme, "%LOCALAPPDATA%\\Zoya Radio\\logs");
        Contains(readme, "safe silence");
        Contains(readme, "https://aka.ms/vs/17/release/vc_redist.x64.exe");

        string terms = File.ReadAllText(Path.Combine(distribution, "PRIVATE-ALPHA-TERMS.txt"));
        Contains(terms, "invitation-only");
        Contains(terms, "warranty");
        Contains(terms, "Do not redistribute");
        Contains(terms, "Firefox, Ollama, and qwen3:4b-instruct");
        Contains(CommentValidator.NormalizeWhitespace(terms), "Review logs before sharing");

        string notices = File.ReadAllText(Path.Combine(distribution, "THIRD-PARTY-NOTICES.txt"));
        foreach (string dependency in new[]
        {
            ".NET Runtime", "Windows SDK", "KokoroSharp", "MisakiSharp",
            "Kokoro 82M", "ONNX", "NAudio", "NumSharp", "OpenTK", "System.Numerics.Tensors",
        })
        {
            Contains(notices, dependency);
        }
        Contains(notices, "Apache-2.0-Misaki-Kokoro-Model-and-Voices.txt");

        byte[] windowsLicense = File.ReadAllBytes(
            Path.Combine(distribution, "licenses", "Microsoft-Windows-SDK-License.rtf"));
        True(windowsLicense.Length > 200_000);
        Equal("{\\rtf", Encoding.ASCII.GetString(windowsLicense, 0, 5));
    }

    private static PrivateAlphaReadinessSnapshot Snapshot(
        DiskReadinessState disk,
        FirefoxPrerequisiteState firefox,
        OllamaReadinessState ollama,
        QwenReadinessState qwen)
    {
        return new PrivateAlphaReadinessSnapshot(
            new PackageIntegrityResult(
                PackageIntegrityStatus.Valid,
                "Package valid",
                [],
                12,
                100),
            true,
            "supported Windows build 19045",
            disk,
            20L * 1024 * 1024 * 1024,
            firefox,
            new OllamaReadinessResult(
                ollama,
                qwen,
                "0.32.6",
                qwen == QwenReadinessState.Present ? "sha256:test" : null,
                qwen == QwenReadinessState.Present ? 2_900_000_000 : null,
                "readiness message"),
            VisualCppRuntimeState.Available,
            "%LOCALAPPDATA%\\Zoya Radio\\logs");
    }

    private static OllamaSetupClient Client(QueueHttpHandler handler)
    {
        return new OllamaSetupClient(
            AiCommentConfiguration.Default,
            new HttpClient(handler) { Timeout = Timeout.InfiniteTimeSpan });
    }

    private static Func<HttpRequestMessage, CancellationToken, Task<HttpResponseMessage>> Json(
        HttpStatusCode status,
        string body)
    {
        return (_, _) => Task.FromResult(
            new HttpResponseMessage(status)
            {
                Content = new StringContent(body, Encoding.UTF8, "application/json"),
            });
    }

    private static void True(bool condition, string? message = null)
    {
        if (!condition)
        {
            throw new InvalidOperationException(message ?? "Expected true.");
        }
    }

    private static void False(bool condition, string? message = null) =>
        True(!condition, message ?? "Expected false.");

    private static void Equal<T>(T expected, T actual)
    {
        if (!EqualityComparer<T>.Default.Equals(expected, actual))
        {
            throw new InvalidOperationException($"Expected '{expected}', found '{actual}'.");
        }
    }

    private static void Contains(string value, string expected)
    {
        if (!value.Contains(expected, StringComparison.OrdinalIgnoreCase))
        {
            throw new InvalidOperationException($"Expected text to contain '{expected}'.");
        }
    }

    private sealed class TestPackage : IDisposable
    {
        private TestPackage(string root)
        {
            Root = root;
        }

        public string Root { get; }

        public static TestPackage Create(string? extraPath = null, string extraText = "extra")
        {
            string root = Path.Combine(Path.GetTempPath(), $"zoya-package-{Guid.NewGuid():N}");
            Directory.CreateDirectory(root);
            var entries = new List<PrivateAlphaManifestFile>();

            foreach (string relative in PrivateAlphaIdentity.RequiredCriticalFiles)
            {
                Add(root, entries, relative, $"test:{relative}", critical: true);
            }

            if (extraPath is not null)
            {
                Add(root, entries, extraPath, extraText, critical: false);
            }

            var manifest = new PrivateAlphaManifest(
                PrivateAlphaIdentity.ProductName,
                PrivateAlphaIdentity.CandidateVersion,
                PrivateAlphaIdentity.RuntimeIdentifier,
                PrivateAlphaIdentity.Platform,
                PrivateAlphaIdentity.SupportedWindows,
                PrivateAlphaIdentity.EntryPoint,
                PrivateAlphaIdentity.SpeechRoute,
                ["Firefox", "Ollama", "Qwen"],
                "test-commit",
                false,
                entries.OrderBy(entry => entry.Path, StringComparer.Ordinal).ToArray());
            File.WriteAllText(
                Path.Combine(root, PrivateAlphaIdentity.ManifestFileName),
                JsonSerializer.Serialize(manifest, new JsonSerializerOptions { WriteIndented = true }),
                new UTF8Encoding(false));
            return new TestPackage(root);
        }

        public void Dispose()
        {
            if (Directory.Exists(Root))
            {
                Directory.Delete(Root, recursive: true);
            }
        }

        private static void Add(
            string root,
            ICollection<PrivateAlphaManifestFile> entries,
            string relative,
            string text,
            bool critical)
        {
            string normalized = relative.Replace('\\', '/');
            string fullPath = Path.Combine(root, normalized.Replace('/', Path.DirectorySeparatorChar));
            Directory.CreateDirectory(Path.GetDirectoryName(fullPath)!);
            File.WriteAllText(fullPath, text, new UTF8Encoding(false));
            byte[] bytes = File.ReadAllBytes(fullPath);
            entries.Add(
                new PrivateAlphaManifestFile(
                    normalized,
                    bytes.LongLength,
                    Convert.ToHexString(SHA256.HashData(bytes)),
                    critical));
        }
    }

    private sealed class QueueHttpHandler : HttpMessageHandler
    {
        private readonly Queue<Func<HttpRequestMessage, CancellationToken, Task<HttpResponseMessage>>> _responses;

        public QueueHttpHandler(
            params Func<HttpRequestMessage, CancellationToken, Task<HttpResponseMessage>>[] responses)
        {
            _responses = new Queue<Func<HttpRequestMessage, CancellationToken, Task<HttpResponseMessage>>>(
                responses);
        }

        public List<(HttpMethod Method, Uri Uri, string Body)> Requests { get; } = [];

        protected override async Task<HttpResponseMessage> SendAsync(
            HttpRequestMessage request,
            CancellationToken cancellationToken)
        {
            string body = request.Content is null
                ? string.Empty
                : await request.Content.ReadAsStringAsync(cancellationToken);
            Requests.Add((request.Method, request.RequestUri!, body));
            if (_responses.Count == 0)
            {
                throw new InvalidOperationException("Unexpected fake HTTP request.");
            }

            return await _responses.Dequeue()(request, cancellationToken);
        }
    }

    private sealed class InlineProgress<T> : IProgress<T>
    {
        private readonly Action<T> _report;

        public InlineProgress(Action<T> report)
        {
            _report = report;
        }

        public void Report(T value) => _report(value);
    }

    private sealed class FakeEnvironment : IPrivateAlphaEnvironment
    {
        public long? AvailableDiskBytes { get; set; }

        public FirefoxPrerequisiteState Firefox { get; set; }

        public bool OllamaInstalled { get; set; }

        public bool OllamaRunning { get; set; }

        public int RunningChecks { get; private set; }

        public long? GetAvailableDiskBytes() => AvailableDiskBytes;

        public FirefoxPrerequisiteState GetFirefoxState() => Firefox;

        public bool IsOllamaInstalled() => OllamaInstalled;

        public bool IsOllamaRunning()
        {
            RunningChecks++;
            return OllamaRunning;
        }

        public (bool Supported, string Description) GetWindowsState() =>
            (true, "supported Windows build 19045");

        public VisualCppRuntimeState GetVisualCppRuntimeState() =>
            VisualCppRuntimeState.Available;
    }

    private sealed class FakeOllamaSetupApi : IOllamaSetupApi
    {
        public required OllamaReadinessResult Readiness { get; init; }

        public int PullCalls { get; private set; }

        public bool LastConsent { get; private set; }

        public Task<OllamaReadinessResult> CheckAsync(
            bool installed,
            bool running,
            CancellationToken cancellationToken) => Task.FromResult(Readiness);

        public Task<OllamaPullResult> PullModelAsync(
            bool explicitConsent,
            long? availableBytes,
            IProgress<OllamaPullProgress>? progress,
            CancellationToken cancellationToken)
        {
            PullCalls++;
            LastConsent = explicitConsent;
            return Task.FromResult(
                explicitConsent
                    ? new OllamaPullResult(OllamaPullStatus.Completed, "done")
                    : new OllamaPullResult(OllamaPullStatus.ConsentRequired, "no request"));
        }

        public void Dispose()
        {
        }
    }

    private sealed class FakeUriOpener : IExternalUriOpener
    {
        public int OpenCalls { get; private set; }

        public Uri? LastUri { get; private set; }

        public void Open(Uri uri)
        {
            OpenCalls++;
            LastUri = uri;
        }
    }
}
