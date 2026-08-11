using System.Diagnostics;
using System.Runtime.InteropServices;

namespace ZoyaRadio.MediaProbe;

public enum DiskReadinessState
{
    Unknown,
    Sufficient,
    Insufficient,
}

public enum FirefoxPrerequisiteState
{
    Absent,
    InstalledIdle,
    Active,
}

public enum VisualCppRuntimeState
{
    Available,
    Missing,
    Unknown,
}

public sealed record PrivateAlphaReadinessSnapshot(
    PackageIntegrityResult Package,
    bool IsSupportedWindows,
    string WindowsDescription,
    DiskReadinessState Disk,
    long? AvailableDiskBytes,
    FirefoxPrerequisiteState Firefox,
    OllamaReadinessResult Ollama,
    VisualCppRuntimeState VisualCppRuntime,
    string LogFolderPath)
{
    public bool ReadyForNormalOperation =>
        Package.IsValid
        && IsSupportedWindows
        && Firefox != FirefoxPrerequisiteState.Absent
        && Ollama.Ollama == OllamaReadinessState.RunningCompatible
        && Ollama.Qwen == QwenReadinessState.Present;

    public bool CanStartSafely => Package.IsValid && IsSupportedWindows;

    public bool CanRequestModel =>
        Disk == DiskReadinessState.Sufficient
        && Ollama.Ollama == OllamaReadinessState.RunningCompatible
        && Ollama.Qwen == QwenReadinessState.Missing;
}

public sealed record PrivateAlphaReadinessPresentation(
    string Summary,
    IReadOnlyList<string> Details,
    bool CanStartSafely,
    bool CanRequestModel);

public static class PrivateAlphaReadinessPresenter
{
    public static PrivateAlphaReadinessPresentation Present(
        PrivateAlphaReadinessSnapshot snapshot)
    {
        var details = new List<string>
        {
            $"Package: {snapshot.Package.Summary}",
            $"Windows: {snapshot.WindowsDescription}",
            $"Disk for optional model setup: {DescribeDisk(snapshot)}",
            $"Firefox: {DescribeFirefox(snapshot.Firefox)}",
            $"Ollama/Qwen: {snapshot.Ollama.Message}",
            $"Visual C++ x64 runtime: {DescribeVisualCpp(snapshot.VisualCppRuntime)}",
            $"Logs: {snapshot.LogFolderPath}",
        };

        string summary = snapshot.ReadyForNormalOperation
            ? "Ready for normal private-alpha operation."
            : snapshot.CanStartSafely
                ? "Zoya Radio can start safely; review the prerequisite notes below."
                : "Zoya Radio is not ready to start safely.";

        return new PrivateAlphaReadinessPresentation(
            summary,
            details,
            snapshot.CanStartSafely,
            snapshot.CanRequestModel);
    }

    private static string DescribeDisk(PrivateAlphaReadinessSnapshot snapshot)
    {
        string available = snapshot.AvailableDiskBytes is long bytes
            ? $"{bytes / 1024d / 1024d / 1024d:N1} GiB available"
            : "available space could not be measured";

        return snapshot.Disk switch
        {
            DiskReadinessState.Sufficient => $"sufficient ({available})",
            DiskReadinessState.Insufficient =>
                $"insufficient ({available}); no model download will begin",
            _ => $"unknown ({available}); no model download will begin",
        };
    }

    private static string DescribeFirefox(FirefoxPrerequisiteState state)
    {
        return state switch
        {
            FirefoxPrerequisiteState.Active =>
                "running (the radio will still require exactly one eligible Playing media session)",
            FirefoxPrerequisiteState.InstalledIdle =>
                "installed but idle; this is not an installation failure",
            _ => "not found; install Firefox separately before normal use",
        };
    }

    private static string DescribeVisualCpp(VisualCppRuntimeState state)
    {
        return state switch
        {
            VisualCppRuntimeState.Available => "detected",
            VisualCppRuntimeState.Missing =>
                "not detected; Windows TTS fallback may still work, but Nicole requires the official Microsoft x64 redistributable",
            _ => "could not be confirmed",
        };
    }
}

internal interface IPrivateAlphaEnvironment
{
    long? GetAvailableDiskBytes();

    FirefoxPrerequisiteState GetFirefoxState();

    bool IsOllamaInstalled();

    bool IsOllamaRunning();

    (bool Supported, string Description) GetWindowsState();

    VisualCppRuntimeState GetVisualCppRuntimeState();
}

internal interface IExternalUriOpener
{
    void Open(Uri uri);
}

internal sealed class WindowsPrivateAlphaEnvironment : IPrivateAlphaEnvironment
{
    internal static IReadOnlyList<string> RequiredVisualCppLibraries { get; } =
        ["vcruntime140.dll", "vcruntime140_1.dll", "msvcp140.dll", "msvcp140_1.dll"];

    public long? GetAvailableDiskBytes()
    {
        try
        {
            string localAppData = Environment.GetFolderPath(
                Environment.SpecialFolder.LocalApplicationData);
            string? root = Path.GetPathRoot(localAppData);
            return string.IsNullOrWhiteSpace(root) ? null : new DriveInfo(root).AvailableFreeSpace;
        }
        catch
        {
            return null;
        }
    }

    public FirefoxPrerequisiteState GetFirefoxState()
    {
        if (IsProcessRunning("firefox"))
        {
            return FirefoxPrerequisiteState.Active;
        }

        string[] candidates =
        [
            Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.ProgramFiles),
                "Mozilla Firefox",
                "firefox.exe"),
            Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.ProgramFilesX86),
                "Mozilla Firefox",
                "firefox.exe"),
        ];
        return candidates.Any(File.Exists)
            ? FirefoxPrerequisiteState.InstalledIdle
            : FirefoxPrerequisiteState.Absent;
    }

    public bool IsOllamaInstalled()
    {
        if (IsOllamaRunning())
        {
            return true;
        }

        string[] candidates =
        [
            Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
                "Programs",
                "Ollama",
                "ollama.exe"),
            Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.ProgramFiles),
                "Ollama",
                "ollama.exe"),
        ];
        return candidates.Any(File.Exists);
    }

    public bool IsOllamaRunning()
    {
        return IsProcessRunning("ollama") || IsProcessRunning("ollama app");
    }

    public (bool Supported, string Description) GetWindowsState()
    {
        if (!OperatingSystem.IsWindows())
        {
            return (false, "unsupported non-Windows operating system");
        }

        Version version = Environment.OSVersion.Version;
        bool supported = version.Major >= 10 && version.Build >= 19045;
        return (
            supported,
            supported
                ? $"supported Windows build {version.Build}"
                : $"Windows build {version.Build}; {PrivateAlphaIdentity.SupportedWindows} is required");
    }

    public VisualCppRuntimeState GetVisualCppRuntimeState()
    {
        try
        {
            foreach (string library in RequiredVisualCppLibraries)
            {
                if (!NativeLibrary.TryLoad(library, out nint handle))
                {
                    return VisualCppRuntimeState.Missing;
                }

                NativeLibrary.Free(handle);
            }

            return VisualCppRuntimeState.Available;
        }
        catch
        {
            return VisualCppRuntimeState.Unknown;
        }
    }

    private static bool IsProcessRunning(string processName)
    {
        try
        {
            Process[] processes = Process.GetProcessesByName(processName);
            foreach (Process process in processes)
            {
                process.Dispose();
            }

            return processes.Length > 0;
        }
        catch
        {
            return false;
        }
    }
}

internal sealed class WindowsExternalUriOpener : IExternalUriOpener
{
    public void Open(Uri uri)
    {
        Process.Start(new ProcessStartInfo(uri.AbsoluteUri) { UseShellExecute = true });
    }
}

public sealed class PrivateAlphaSetupController : IDisposable
{
    public static readonly Uri OfficialOllamaDownloadPage =
        new("https://ollama.com/download/windows", UriKind.Absolute);

    private readonly IPrivateAlphaEnvironment _environment;
    private readonly IOllamaSetupApi _ollama;
    private readonly IExternalUriOpener _uriOpener;
    private readonly string _applicationBaseDirectory;
    private readonly string _logFolderPath;
    private bool _disposed;

    internal PrivateAlphaSetupController(
        IPrivateAlphaEnvironment environment,
        IOllamaSetupApi ollama,
        IExternalUriOpener uriOpener,
        string applicationBaseDirectory,
        string logFolderPath)
    {
        _environment = environment;
        _ollama = ollama;
        _uriOpener = uriOpener;
        _applicationBaseDirectory = applicationBaseDirectory;
        _logFolderPath = logFolderPath;
    }

    public static PrivateAlphaSetupController CreateWindowsDefault()
    {
        string logFolder = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
            LocalAppDataLogFolderProvider.ApplicationFolderName,
            LocalAppDataLogFolderProvider.LogsFolderName);
        return new PrivateAlphaSetupController(
            new WindowsPrivateAlphaEnvironment(),
            new OllamaSetupClient(AiCommentConfiguration.Default),
            new WindowsExternalUriOpener(),
            AppContext.BaseDirectory,
            logFolder);
    }

    public async Task<PrivateAlphaReadinessSnapshot> CheckAsync(
        CancellationToken cancellationToken = default)
    {
        ObjectDisposedException.ThrowIf(_disposed, this);
        cancellationToken.ThrowIfCancellationRequested();

        PackageIntegrityResult package = PrivateAlphaBuildIdentity.IsPackagedCandidate
            ? PrivateAlphaPackageIntegrity.Verify(_applicationBaseDirectory, verifyEveryFile: false)
            : new PackageIntegrityResult(
                PackageIntegrityStatus.Valid,
                "Development build: packaged-file gate is not active.",
                [],
                0,
                0);
        long? available = _environment.GetAvailableDiskBytes();
        DiskReadinessState disk = available switch
        {
            null => DiskReadinessState.Unknown,
            >= OllamaSetupPolicy.MinimumFreeBytesBeforePull => DiskReadinessState.Sufficient,
            _ => DiskReadinessState.Insufficient,
        };
        (bool supported, string windowsDescription) = _environment.GetWindowsState();
        bool ollamaInstalled = _environment.IsOllamaInstalled();
        bool ollamaRunning = _environment.IsOllamaRunning();
        OllamaReadinessResult ollama = await _ollama.CheckAsync(
            ollamaInstalled,
            ollamaRunning,
            cancellationToken);

        return new PrivateAlphaReadinessSnapshot(
            package,
            supported,
            windowsDescription,
            disk,
            available,
            _environment.GetFirefoxState(),
            ollama,
            _environment.GetVisualCppRuntimeState(),
            _logFolderPath);
    }

    public Task<OllamaPullResult> RequestQwenAsync(
        bool explicitConsent,
        IProgress<OllamaPullProgress>? progress,
        CancellationToken cancellationToken = default)
    {
        ObjectDisposedException.ThrowIf(_disposed, this);
        return _ollama.PullModelAsync(
            explicitConsent,
            _environment.GetAvailableDiskBytes(),
            progress,
            cancellationToken);
    }

    public void OpenOfficialOllamaDownloadPage()
    {
        ObjectDisposedException.ThrowIf(_disposed, this);
        _uriOpener.Open(OfficialOllamaDownloadPage);
    }

    public void Dispose()
    {
        if (_disposed)
        {
            return;
        }

        _disposed = true;
        _ollama.Dispose();
    }
}
