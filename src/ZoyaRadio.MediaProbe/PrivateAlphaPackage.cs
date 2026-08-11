using System.Reflection;
using System.Security.Cryptography;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace ZoyaRadio.MediaProbe;

public static class PrivateAlphaIdentity
{
    public const string ProductName = "Zoya Radio";
    public const string CandidateVersion = "0.8.0-alpha.1";
    public const string RuntimeIdentifier = "win-x64";
    public const string Platform = "Windows x64";
    public const string SupportedWindows = "Windows 10 22H2 (build 19045) or newer";
    public const string EntryPoint = "ZoyaRadio.ControlApp.exe";
    public const string ManifestFileName = "package-manifest.json";
    public const string PackagedModelRelativePath = "assets/kokoro/kokoro.onnx";
    public const string NicoleVoiceRelativePath = "voices/af_nicole.npy";
    public const string SpeechRoute =
        "Kokoro FP32 with af_nicole, with local Windows TTS fallback";
    public const string PackageFileName = "ZoyaRadio-0.8.0-alpha.1-win-x64.zip";

    public static readonly string[] RequiredCriticalFiles =
    [
        EntryPoint,
        "ZoyaRadio.ControlApp.dll",
        "ZoyaRadio.ControlApp.deps.json",
        "ZoyaRadio.ControlApp.runtimeconfig.json",
        "ZoyaRadio.MediaProbe.dll",
        "KokoroSharp.dll",
        "MisakiSharp.dll",
        "Microsoft.ML.OnnxRuntime.dll",
        "onnxruntime.dll",
        "onnxruntime_providers_shared.dll",
        PackagedModelRelativePath,
        NicoleVoiceRelativePath,
    ];
}

internal static class PrivateAlphaBuildIdentity
{
    private const string MetadataKey = "ZoyaRadioPrivateAlphaCandidate";

    public static bool IsPackagedCandidate =>
        IsPackagedCandidateAssembly(typeof(PrivateAlphaBuildIdentity).Assembly);

    internal static bool IsPackagedCandidateAssembly(Assembly assembly)
    {
        return assembly
            .GetCustomAttributes<AssemblyMetadataAttribute>()
            .Any(attribute =>
                string.Equals(attribute.Key, MetadataKey, StringComparison.Ordinal)
                && string.Equals(
                    attribute.Value,
                    PrivateAlphaIdentity.CandidateVersion,
                    StringComparison.Ordinal));
    }
}

public sealed record PrivateAlphaManifest(
    [property: JsonPropertyName("productName")] string ProductName,
    [property: JsonPropertyName("candidateVersion")] string CandidateVersion,
    [property: JsonPropertyName("runtimeIdentifier")] string RuntimeIdentifier,
    [property: JsonPropertyName("platform")] string Platform,
    [property: JsonPropertyName("supportedWindows")] string SupportedWindows,
    [property: JsonPropertyName("entryPoint")] string EntryPoint,
    [property: JsonPropertyName("speechRoute")] string SpeechRoute,
    [property: JsonPropertyName("excludedComponents")] IReadOnlyList<string> ExcludedComponents,
    [property: JsonPropertyName("sourceCommit")] string SourceCommit,
    [property: JsonPropertyName("sourceDirty")] bool SourceDirty,
    [property: JsonPropertyName("files")] IReadOnlyList<PrivateAlphaManifestFile> Files);

public sealed record PrivateAlphaManifestFile(
    [property: JsonPropertyName("path")] string Path,
    [property: JsonPropertyName("size")] long Size,
    [property: JsonPropertyName("sha256")] string Sha256,
    [property: JsonPropertyName("critical")] bool Critical);

public enum PackageIntegrityStatus
{
    Valid,
    Missing,
    Corrupt,
}

public sealed record PackageIntegrityResult(
    PackageIntegrityStatus Status,
    string Summary,
    IReadOnlyList<string> Issues,
    int VerifiedFileCount,
    long ManifestPackageBytes)
{
    public bool IsValid => Status == PackageIntegrityStatus.Valid;
}

public static class PrivateAlphaPackageIntegrity
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true,
    };

    public static PackageIntegrityResult Verify(string packageRoot, bool verifyEveryFile)
    {
        if (string.IsNullOrWhiteSpace(packageRoot))
        {
            return Missing("The package folder is unavailable.");
        }

        string root = Path.GetFullPath(packageRoot);
        string manifestPath = Path.Combine(root, PrivateAlphaIdentity.ManifestFileName);

        if (!File.Exists(manifestPath))
        {
            return Missing(
                $"Required package manifest '{PrivateAlphaIdentity.ManifestFileName}' is missing.");
        }

        PrivateAlphaManifest? manifest;

        try
        {
            manifest = JsonSerializer.Deserialize<PrivateAlphaManifest>(
                File.ReadAllText(manifestPath),
                JsonOptions);
        }
        catch (Exception exception) when (
            exception is JsonException or IOException or UnauthorizedAccessException)
        {
            return Corrupt($"The package manifest could not be read: {exception.Message}");
        }

        if (manifest is null)
        {
            return Corrupt("The package manifest is empty.");
        }

        var issues = new List<string>();
        ValidateManifestIdentity(manifest, issues);

        IReadOnlyList<PrivateAlphaManifestFile> files = manifest.Files ?? [];
        var paths = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        long packageBytes = 0;

        foreach (PrivateAlphaManifestFile entry in files)
        {
            string normalized = NormalizeRelativePath(entry.Path, issues);
            if (normalized.Length == 0)
            {
                continue;
            }

            if (!paths.Add(normalized))
            {
                issues.Add($"Manifest contains a duplicate path: {normalized}");
            }

            if (entry.Size < 0)
            {
                issues.Add($"Manifest contains a negative file size: {normalized}");
            }
            else if (packageBytes > long.MaxValue - entry.Size)
            {
                issues.Add("Manifest file sizes exceed the supported package-size range.");
            }
            else
            {
                packageBytes += entry.Size;
            }

            if (!IsSha256(entry.Sha256))
            {
                issues.Add($"Manifest contains an invalid SHA-256 value: {normalized}");
            }
        }

        foreach (string required in PrivateAlphaIdentity.RequiredCriticalFiles)
        {
            string normalized = required.Replace('\\', '/');
            PrivateAlphaManifestFile? entry = files.FirstOrDefault(
                file => string.Equals(
                    file.Path.Replace('\\', '/'),
                    normalized,
                    StringComparison.OrdinalIgnoreCase));

            if (entry is null)
            {
                issues.Add($"Manifest omits required critical file: {normalized}");
            }
            else if (!entry.Critical)
            {
                issues.Add($"Required startup file is not marked critical: {normalized}");
            }
        }

        AuditContentNames(root, paths, verifyEveryFile, issues);

        int verified = 0;
        foreach (PrivateAlphaManifestFile entry in files.Where(
                     entry => verifyEveryFile || entry.Critical))
        {
            string normalized = NormalizeRelativePath(entry.Path, issues);
            if (normalized.Length == 0)
            {
                continue;
            }

            string fullPath = ResolveInsideRoot(root, normalized);
            if (!File.Exists(fullPath))
            {
                issues.Add($"Package file is missing: {normalized}");
                continue;
            }

            var info = new FileInfo(fullPath);
            if (info.Length != entry.Size)
            {
                issues.Add(
                    $"Package file size does not match the manifest: {normalized} " +
                    $"(expected {entry.Size}, found {info.Length})");
                continue;
            }

            string hash;
            using (FileStream stream = File.OpenRead(fullPath))
            {
                hash = Convert.ToHexString(SHA256.HashData(stream));
            }

            if (!string.Equals(hash, entry.Sha256, StringComparison.OrdinalIgnoreCase))
            {
                issues.Add($"Package file hash does not match the manifest: {normalized}");
                continue;
            }

            verified++;
        }

        if (issues.Count > 0)
        {
            return new PackageIntegrityResult(
                PackageIntegrityStatus.Corrupt,
                $"Package readiness failed with {issues.Count} issue(s).",
                issues,
                verified,
                packageBytes);
        }

        return new PackageIntegrityResult(
            PackageIntegrityStatus.Valid,
            verifyEveryFile
                ? $"Full package integrity verified ({verified} files)."
                : $"Critical package integrity verified ({verified} files).",
            [],
            verified,
            packageBytes);
    }

    internal static void EnsureSafeStartup(string packageRoot, IAnnouncementLog log)
    {
        if (!PrivateAlphaBuildIdentity.IsPackagedCandidate)
        {
            return;
        }

        PackageIntegrityResult result = Verify(packageRoot, verifyEveryFile: false);
        log.Write(result.Summary);

        if (!result.IsValid)
        {
            foreach (string issue in result.Issues.Take(12))
            {
                log.Write($"PACKAGE READINESS: {issue}");
            }

            throw new InvalidOperationException(
                "Zoya Radio cannot start because required packaged files are missing or corrupt. " +
                "Extract a fresh copy of the private-alpha ZIP and run the readiness check again.");
        }
    }

    private static void ValidateManifestIdentity(
        PrivateAlphaManifest manifest,
        ICollection<string> issues)
    {
        Check(
            manifest.ProductName,
            PrivateAlphaIdentity.ProductName,
            "product name",
            issues);
        Check(
            manifest.CandidateVersion,
            PrivateAlphaIdentity.CandidateVersion,
            "candidate version",
            issues);
        Check(
            manifest.RuntimeIdentifier,
            PrivateAlphaIdentity.RuntimeIdentifier,
            "runtime identifier",
            issues);
        Check(manifest.Platform, PrivateAlphaIdentity.Platform, "platform", issues);
        Check(
            manifest.SupportedWindows,
            PrivateAlphaIdentity.SupportedWindows,
            "supported Windows baseline",
            issues);
        Check(manifest.EntryPoint, PrivateAlphaIdentity.EntryPoint, "entry point", issues);
        Check(manifest.SpeechRoute, PrivateAlphaIdentity.SpeechRoute, "speech route", issues);

        string[] exclusions = ["Firefox", "Ollama", "Qwen"];
        foreach (string exclusion in exclusions)
        {
            if (manifest.ExcludedComponents is null
                || !manifest.ExcludedComponents.Any(
                    item => string.Equals(item, exclusion, StringComparison.OrdinalIgnoreCase)))
            {
                issues.Add($"Manifest does not explicitly exclude {exclusion}.");
            }
        }

        if (string.IsNullOrWhiteSpace(manifest.SourceCommit))
        {
            issues.Add("Manifest build provenance is missing the source commit.");
        }
    }

    private static void AuditContentNames(
        string root,
        IReadOnlySet<string> manifestPaths,
        bool verifyEveryFile,
        ICollection<string> issues)
    {
        string[] actual = Directory
            .EnumerateFiles(root, "*", SearchOption.AllDirectories)
            .Select(path => Path.GetRelativePath(root, path).Replace('\\', '/'))
            .OrderBy(path => path, StringComparer.OrdinalIgnoreCase)
            .ToArray();

        foreach (string relative in actual)
        {
            string lower = relative.ToLowerInvariant();
            string name = Path.GetFileName(lower);

            if (lower.Contains("espeak", StringComparison.Ordinal))
            {
                issues.Add($"eSpeak content is forbidden: {relative}");
            }

            if (lower.EndsWith(".pdb", StringComparison.Ordinal)
                || lower.EndsWith(".lib", StringComparison.Ordinal))
            {
                issues.Add($"Private symbols/import libraries are forbidden in the tester package: {relative}");
            }

            if (name is "firefox.exe" or "ollama.exe" or "ollama app.exe")
            {
                issues.Add($"Separate third-party application is forbidden in the package: {relative}");
            }

            if (lower.EndsWith(".npy", StringComparison.Ordinal)
                && !string.Equals(
                    relative,
                    PrivateAlphaIdentity.NicoleVoiceRelativePath,
                    StringComparison.OrdinalIgnoreCase))
            {
                issues.Add($"Only Nicole voice data may be packaged: {relative}");
            }

            if (lower.Contains("linux-", StringComparison.Ordinal)
                || lower.Contains("osx-", StringComparison.Ordinal)
                || lower.Contains("win-x86", StringComparison.Ordinal)
                || lower.Contains("arm64", StringComparison.Ordinal))
            {
                issues.Add($"Non-Windows-x64 runtime asset is forbidden: {relative}");
            }

            if (lower.StartsWith("logs/", StringComparison.Ordinal)
                || lower.Contains("listening-history", StringComparison.Ordinal)
                || lower.EndsWith(".wav", StringComparison.Ordinal))
            {
                issues.Add($"Mutable or private runtime material is forbidden: {relative}");
            }

            if (verifyEveryFile
                && !string.Equals(
                    relative,
                    PrivateAlphaIdentity.ManifestFileName,
                    StringComparison.OrdinalIgnoreCase)
                && !manifestPaths.Contains(relative))
            {
                issues.Add($"Package contains an unmanifested file: {relative}");
            }
        }
    }

    private static string NormalizeRelativePath(string? value, ICollection<string> issues)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            issues.Add("Manifest contains an empty file path.");
            return string.Empty;
        }

        string normalized = value.Replace('\\', '/');
        if (Path.IsPathRooted(value)
            || normalized.StartsWith("/", StringComparison.Ordinal)
            || normalized.Split('/').Any(segment => segment is "." or ".."))
        {
            issues.Add($"Manifest path escapes the package root: {value}");
            return string.Empty;
        }

        return normalized;
    }

    private static string ResolveInsideRoot(string root, string relative)
    {
        string rootWithSeparator = root.TrimEnd(
            Path.DirectorySeparatorChar,
            Path.AltDirectorySeparatorChar) + Path.DirectorySeparatorChar;
        string fullPath = Path.GetFullPath(
            Path.Combine(root, relative.Replace('/', Path.DirectorySeparatorChar)));

        if (!fullPath.StartsWith(rootWithSeparator, StringComparison.OrdinalIgnoreCase))
        {
            throw new InvalidOperationException($"Package path escapes its root: {relative}");
        }

        return fullPath;
    }

    private static bool IsSha256(string? value)
    {
        return value is { Length: 64 }
            && value.All(character =>
                character is >= '0' and <= '9'
                || character is >= 'A' and <= 'F'
                || character is >= 'a' and <= 'f');
    }

    private static void Check(
        string? actual,
        string expected,
        string label,
        ICollection<string> issues)
    {
        if (!string.Equals(actual, expected, StringComparison.Ordinal))
        {
            issues.Add($"Manifest {label} is invalid (expected '{expected}').");
        }
    }

    private static PackageIntegrityResult Missing(string message)
    {
        return new PackageIntegrityResult(
            PackageIntegrityStatus.Missing,
            message,
            [message],
            0,
            0);
    }

    private static PackageIntegrityResult Corrupt(string message)
    {
        return new PackageIntegrityResult(
            PackageIntegrityStatus.Corrupt,
            message,
            [message],
            0,
            0);
    }
}

internal static class NeuralModelPathResolver
{
    public static string Resolve(
        NeuralSpeechConfiguration configuration,
        string applicationBaseDirectory,
        bool packagedCandidate,
        string? developmentRepositoryRoot = null)
    {
        configuration.Validate();

        if (packagedCandidate)
        {
            return Path.GetFullPath(
                Path.Combine(
                    applicationBaseDirectory,
                    PrivateAlphaIdentity.PackagedModelRelativePath));
        }

        string root = developmentRepositoryRoot ?? RepositoryPathLocator.FindRepositoryRoot();
        return Path.GetFullPath(Path.Combine(root, configuration.ModelRelativePath));
    }
}

public static class PrivateAlphaPackageDigest
{
    public static string ComputeSha256(string filePath)
    {
        using FileStream stream = File.OpenRead(filePath);
        return Convert.ToHexString(SHA256.HashData(stream));
    }

    public static bool VerifySha256(string filePath, string expected)
    {
        return expected is not null
            && string.Equals(
                ComputeSha256(filePath),
                expected.Trim(),
                StringComparison.OrdinalIgnoreCase);
    }
}
