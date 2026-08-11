[CmdletBinding()]
param(
    [switch]$SkipNeuralVerification
)

$ErrorActionPreference = 'Stop'
Set-StrictMode -Version Latest

$candidateVersion = '0.8.0-alpha.1'
$runtimeIdentifier = 'win-x64'
$packageName = "ZoyaRadio-$candidateVersion-$runtimeIdentifier"
$repositoryRoot = (Resolve-Path (Join-Path $PSScriptRoot '..')).Path
$artifactsRoot = Join-Path $repositoryRoot 'artifacts\private-alpha'
$outputRoot = Join-Path $artifactsRoot $candidateVersion
$rawPublish = Join-Path $outputRoot 'publish-raw'
$stage = Join-Path $outputRoot 'staging'
$symbols = Join-Path $outputRoot 'private-symbols'
$extracted = Join-Path $outputRoot 'extracted'
$zipPath = Join-Path $outputRoot "$packageName.zip"
$digestPath = "$zipPath.sha256.txt"
$reportPath = Join-Path $outputRoot 'package-build-report.json'
$distributionRoot = Join-Path $repositoryRoot 'distribution\private-alpha'
$dotnet = Join-Path $repositoryRoot '.dotnet\dotnet.exe'
$packages = Join-Path $repositoryRoot '.packages'
$project = Join-Path $repositoryRoot 'src\ZoyaRadio.ControlApp\ZoyaRadio.ControlApp.csproj'
$modelSource = Join-Path $repositoryRoot '.tools\kokoro\kokoro.onnx'
$voiceSource = Join-Path $repositoryRoot 'src\ZoyaRadio.ControlApp\bin\Release\net8.0-windows10.0.19041.0\win-x64\voices\af_nicole.npy'

$expectedModelHash = '7D5DF8ECF7D4B1878015A32686053FD0EEBE2BC377234608764CC0EF3636A6C5'
$expectedVoiceHash = 'D0C96B799C867A9C03A210F5385353097B0F6EF12F7AB3D777D3227853B129C4'
$fixedZipTime = [DateTimeOffset]::new(2000, 1, 1, 0, 0, 0, [TimeSpan]::Zero)

function Assert-UnderRoot {
    param([string]$Path, [string]$Root)

    $fullPath = [IO.Path]::GetFullPath($Path)
    $fullRoot = [IO.Path]::GetFullPath($Root).TrimEnd('\', '/') + [IO.Path]::DirectorySeparatorChar
    if (-not $fullPath.StartsWith($fullRoot, [StringComparison]::OrdinalIgnoreCase)) {
        throw "Refusing to modify a path outside the intended output root: $fullPath"
    }
}

function Remove-ValidatedDirectory {
    param([string]$Path)

    Assert-UnderRoot -Path $Path -Root $artifactsRoot
    if (Test-Path -LiteralPath $Path) {
        Remove-Item -LiteralPath $Path -Recurse -Force
    }
}

function Copy-RequiredFile {
    param([string]$Source, [string]$Destination)

    if (-not (Test-Path -LiteralPath $Source -PathType Leaf)) {
        throw "Required package input is missing: $Source"
    }

    $destinationFolder = Split-Path -Parent $Destination
    New-Item -ItemType Directory -Path $destinationFolder -Force | Out-Null
    Copy-Item -LiteralPath $Source -Destination $Destination -Force
}

function Get-RelativePath {
    param([string]$Root, [string]$FullPath)

    $rootPath = [IO.Path]::GetFullPath($Root).TrimEnd('\', '/') + [IO.Path]::DirectorySeparatorChar
    $rootUri = [Uri]$rootPath
    $fileUri = [Uri][IO.Path]::GetFullPath($FullPath)
    return [Uri]::UnescapeDataString(
        $rootUri.MakeRelativeUri($fileUri).ToString()).Replace('/', [IO.Path]::DirectorySeparatorChar)
}


function Get-RelativePackagePath {
    param([string]$FullPath)

    return (Get-RelativePath -Root $stage -FullPath $FullPath).Replace('\', '/')
}

function Invoke-SafeSelfTest {
    param([string]$PackageRoot, [string]$Argument)

    $executable = Join-Path $PackageRoot 'ZoyaRadio.ControlApp.exe'
    $process = Start-Process `
        -FilePath $executable `
        -ArgumentList $Argument `
        -WorkingDirectory $env:TEMP `
        -WindowStyle Hidden `
        -Wait `
        -PassThru
    if ($process.ExitCode -ne 0) {
        throw "Safe packaged self-test '$Argument' failed with exit code $($process.ExitCode)."
    }
}

function New-DeterministicZip {
    param([string]$SourceRoot, [string]$Destination)

    Add-Type -AssemblyName System.IO.Compression
    if (Test-Path -LiteralPath $Destination) {
        Remove-Item -LiteralPath $Destination -Force
    }

    $stream = [IO.File]::Open($Destination, [IO.FileMode]::CreateNew)
    try {
        $archive = [IO.Compression.ZipArchive]::new(
            $stream,
            [IO.Compression.ZipArchiveMode]::Create,
            $false)
        try {
            $files = Get-ChildItem -LiteralPath $SourceRoot -Recurse -File |
                Sort-Object { Get-RelativePath -Root $SourceRoot -FullPath $_.FullName }
            foreach ($file in $files) {
                $relative = (Get-RelativePath -Root $SourceRoot -FullPath $file.FullName).Replace('\', '/')
                $entry = $archive.CreateEntry($relative, [IO.Compression.CompressionLevel]::Optimal)
                $entry.LastWriteTime = $fixedZipTime
                $input = $file.OpenRead()
                $output = $entry.Open()
                try {
                    $input.CopyTo($output)
                }
                finally {
                    $output.Dispose()
                    $input.Dispose()
                }
            }
        }
        finally {
            $archive.Dispose()
        }
    }
    finally {
        $stream.Dispose()
    }
}

Set-Location $repositoryRoot
if (-not (Test-Path -LiteralPath $dotnet -PathType Leaf)) {
    throw "Repository-local .NET SDK is missing: $dotnet"
}

foreach ($required in @($modelSource, $distributionRoot)) {
    if (-not (Test-Path -LiteralPath $required)) {
        throw "Required candidate input is missing: $required"
    }
}

$modelHash = (Get-FileHash -Algorithm SHA256 -LiteralPath $modelSource).Hash
if ($modelHash -ne $expectedModelHash) {
    throw "The Kokoro model hash is not the accepted FP32 model hash. Found $modelHash"
}

New-Item -ItemType Directory -Path $artifactsRoot -Force | Out-Null
Assert-UnderRoot -Path $outputRoot -Root $artifactsRoot
Remove-ValidatedDirectory -Path $outputRoot
New-Item -ItemType Directory -Path $rawPublish, $stage, $symbols, $extracted -Force | Out-Null

$env:DOTNET_CLI_HOME = (Resolve-Path (Join-Path $repositoryRoot '.dotnet-cli-home')).Path
$env:NUGET_PACKAGES = (Resolve-Path $packages).Path

& $dotnet publish $project `
    -c Release `
    -r $runtimeIdentifier `
    --self-contained true `
    --no-restore `
    -p:PrivateAlphaBuild=true `
    -o $rawPublish `
    --nologo
if ($LASTEXITCODE -ne 0) {
    throw "Self-contained publish failed with exit code $LASTEXITCODE."
}

$publishedItems = Get-ChildItem -LiteralPath $rawPublish -Force
foreach ($item in $publishedItems) {
    Copy-Item -LiteralPath $item.FullName -Destination $stage -Recurse -Force
}

$pdbFiles = @(Get-ChildItem -LiteralPath $stage -Recurse -Filter '*.pdb' -File)
foreach ($pdb in $pdbFiles) {
    Copy-Item -LiteralPath $pdb.FullName -Destination (Join-Path $symbols $pdb.Name) -Force
    Remove-Item -LiteralPath $pdb.FullName -Force
}

Get-ChildItem -LiteralPath $stage -Recurse -Filter '*.lib' -File |
    ForEach-Object { Remove-Item -LiteralPath $_.FullName -Force }

Copy-RequiredFile `
    -Source $modelSource `
    -Destination (Join-Path $stage 'assets\kokoro\kokoro.onnx')

if (-not (Test-Path -LiteralPath $voiceSource -PathType Leaf)) {
    throw "Nicole was not copied to the expected Release win-x64 build output: $voiceSource"
}
$voiceHash = (Get-FileHash -Algorithm SHA256 -LiteralPath $voiceSource).Hash
if ($voiceHash -ne $expectedVoiceHash) {
    throw "The Nicole voice hash is unexpected. Found $voiceHash"
}
Copy-RequiredFile `
    -Source $voiceSource `
    -Destination (Join-Path $stage 'voices\af_nicole.npy')

Get-ChildItem -LiteralPath $distributionRoot -Force |
    ForEach-Object { Copy-Item -LiteralPath $_.FullName -Destination $stage -Recurse -Force }

$licenseCopies = [ordered]@{
    'microsoft.netcore.app.runtime.win-x64\8.0.29\LICENSE.TXT' = 'licenses\DotNet-Runtime-MIT.txt'
    'microsoft.netcore.app.runtime.win-x64\8.0.29\THIRD-PARTY-NOTICES.TXT' = 'licenses\DotNet-Runtime-Third-Party-Notices.txt'
    'microsoft.windowsdesktop.app.runtime.win-x64\8.0.29\LICENSE' = 'licenses\DotNet-WindowsDesktop-MIT.txt'
    'microsoft.ml.onnxruntime\1.22.0\LICENSE' = 'licenses\ONNX-Runtime-License.txt'
    'microsoft.ml.onnxruntime\1.22.0\ThirdPartyNotices.txt' = 'licenses\ONNX-Runtime-Third-Party-Notices.txt'
    'naudio\2.2.1\license.txt' = 'licenses\NAudio-MIT.txt'
    'numsharp\0.30.0\LICENSE' = 'licenses\NumSharp-Apache-2.0.txt'
    'kokorosharp\0.8.0\content\voices\LICENSE' = 'licenses\Apache-2.0-Misaki-Kokoro-Model-and-Voices.txt'
    'system.memory\4.5.5\LICENSE.TXT' = 'licenses\System.Memory-MIT.txt'
    'system.memory\4.5.5\THIRD-PARTY-NOTICES.TXT' = 'licenses\System.Memory-Third-Party-Notices.txt'
    'system.numerics.tensors\9.0.5\LICENSE.TXT' = 'licenses\System.Numerics.Tensors-MIT.txt'
    'system.numerics.tensors\9.0.5\THIRD-PARTY-NOTICES.TXT' = 'licenses\System.Numerics.Tensors-Third-Party-Notices.txt'
    'system.runtime.compilerservices.unsafe\5.0.0\LICENSE.TXT' = 'licenses\System.Runtime.CompilerServices.Unsafe-MIT.txt'
    'system.runtime.compilerservices.unsafe\5.0.0\THIRD-PARTY-NOTICES.TXT' = 'licenses\System.Runtime.CompilerServices.Unsafe-Third-Party-Notices.txt'
    'system.security.accesscontrol\4.7.0\LICENSE.TXT' = 'licenses\System.Security.AccessControl-MIT.txt'
    'system.security.accesscontrol\4.7.0\THIRD-PARTY-NOTICES.TXT' = 'licenses\System.Security.AccessControl-Third-Party-Notices.txt'
    'system.security.principal.windows\4.7.0\LICENSE.TXT' = 'licenses\System.Security.Principal.Windows-MIT.txt'
    'system.security.principal.windows\4.7.0\THIRD-PARTY-NOTICES.TXT' = 'licenses\System.Security.Principal.Windows-Third-Party-Notices.txt'
}
foreach ($relativeSource in $licenseCopies.Keys) {
    Copy-RequiredFile `
        -Source (Join-Path $packages $relativeSource) `
        -Destination (Join-Path $stage $licenseCopies[$relativeSource])
}

$requiredRuntimeFiles = @(
    'ZoyaRadio.ControlApp.exe',
    'ZoyaRadio.ControlApp.dll',
    'ZoyaRadio.ControlApp.deps.json',
    'ZoyaRadio.ControlApp.runtimeconfig.json',
    'ZoyaRadio.MediaProbe.dll',
    'System.Private.CoreLib.dll',
    'System.Windows.Forms.dll',
    'Microsoft.Windows.SDK.NET.dll',
    'WinRT.Runtime.dll',
    'hostfxr.dll',
    'coreclr.dll',
    'KokoroSharp.dll',
    'MisakiSharp.dll',
    'Microsoft.ML.OnnxRuntime.dll',
    'onnxruntime.dll',
    'onnxruntime_providers_shared.dll',
    'assets/kokoro/kokoro.onnx',
    'voices/af_nicole.npy',
    'TESTER-README.txt',
    'PRIVATE-ALPHA-TERMS.txt',
    'THIRD-PARTY-NOTICES.txt',
    'CLEAN-MACHINE-CHECKLIST.md',
    'EVIDENCE-TEMPLATE.md'
)
foreach ($relative in $requiredRuntimeFiles) {
    $fullPath = Join-Path $stage $relative.Replace('/', '\')
    if (-not (Test-Path -LiteralPath $fullPath -PathType Leaf)) {
        throw "Required staged file is missing: $relative"
    }
}

$allStageFiles = @(Get-ChildItem -LiteralPath $stage -Recurse -File)
$forbiddenNames = @($allStageFiles | Where-Object {
    $relative = Get-RelativePackagePath $_.FullName
    $lower = $relative.ToLowerInvariant()
    $name = $_.Name.ToLowerInvariant()
    $lower.Contains('espeak') -or
    $lower.EndsWith('.pdb') -or
    $lower.EndsWith('.lib') -or
    $name -in @('firefox.exe', 'ollama.exe', 'ollama app.exe') -or
    $lower.Contains('linux-') -or
    $lower.Contains('osx-') -or
    $lower.Contains('win-x86') -or
    $lower.Contains('arm64') -or
    $lower.StartsWith('logs/') -or
    $lower.Contains('listening-history') -or
    $lower.EndsWith('.wav') -or
    $lower.EndsWith('.gguf') -or
    $lower.EndsWith('.safetensors')
})
if ($forbiddenNames.Count -gt 0) {
    throw "Forbidden candidate content: $($forbiddenNames.FullName -join ', ')"
}

$voiceFiles = @(Get-ChildItem -LiteralPath $stage -Recurse -Filter '*.npy' -File)
if ($voiceFiles.Count -ne 1 -or $voiceFiles[0].Name -ne 'af_nicole.npy') {
    throw "The candidate must contain exactly one NPY voice file: af_nicole.npy."
}

$privatePdbs = @(Get-ChildItem -LiteralPath $symbols -Filter '*.pdb' -File)
if ($privatePdbs.Count -lt 2) {
    throw "Expected private PDB output was not retained."
}

$espeakRoot = Join-Path $packages 'kokorosharp\0.8.0\content\espeak'
if (-not (Test-Path -LiteralPath $espeakRoot -PathType Container)) {
    throw "Cannot perform the identified-component eSpeak hash audit: $espeakRoot is missing."
}
$espeakHashes = [Collections.Generic.HashSet[string]]::new([StringComparer]::OrdinalIgnoreCase)
Get-ChildItem -LiteralPath $espeakRoot -Recurse -File | ForEach-Object {
    [void]$espeakHashes.Add((Get-FileHash -Algorithm SHA256 -LiteralPath $_.FullName).Hash)
}

$criticalSet = [Collections.Generic.HashSet[string]]::new([StringComparer]::OrdinalIgnoreCase)
foreach ($relative in $requiredRuntimeFiles) {
    [void]$criticalSet.Add($relative.Replace('\', '/'))
}

$manifestFiles = @()
foreach ($file in ($allStageFiles | Sort-Object { Get-RelativePackagePath $_.FullName })) {
    $relative = Get-RelativePackagePath $file.FullName
    $hash = (Get-FileHash -Algorithm SHA256 -LiteralPath $file.FullName).Hash
    if ($espeakHashes.Contains($hash)) {
        throw "Candidate file matches an identified eSpeak component by SHA-256: $relative"
    }

    $manifestFiles += [ordered]@{
        path = $relative
        size = $file.Length
        sha256 = $hash
        critical = $criticalSet.Contains($relative)
    }
}

$sourceCommit = (& git -c safe.directory=C:/Users/Steve/Documents/ZoyaRadio rev-parse HEAD).Trim()
$sourceDirty = @(& git -c safe.directory=C:/Users/Steve/Documents/ZoyaRadio status --porcelain --untracked-files=all).Count -gt 0
$manifest = [ordered]@{
    productName = 'Zoya Radio'
    candidateVersion = $candidateVersion
    runtimeIdentifier = $runtimeIdentifier
    platform = 'Windows x64'
    supportedWindows = 'Windows 10 22H2 (build 19045) or newer'
    entryPoint = 'ZoyaRadio.ControlApp.exe'
    speechRoute = 'Kokoro FP32 with af_nicole, with local Windows TTS fallback'
    excludedComponents = @('Firefox', 'Ollama', 'Qwen')
    sourceCommit = $sourceCommit
    sourceDirty = $sourceDirty
    files = $manifestFiles
}
$manifestJson = $manifest | ConvertTo-Json -Depth 8
[IO.File]::WriteAllText(
    (Join-Path $stage 'package-manifest.json'),
    $manifestJson + [Environment]::NewLine,
    [Text.UTF8Encoding]::new($false))

$textExtensions = @('.txt', '.md', '.json', '.config')
foreach ($file in (Get-ChildItem -LiteralPath $stage -Recurse -File)) {
    if ($textExtensions -contains $file.Extension.ToLowerInvariant()) {
        $text = [IO.File]::ReadAllText($file.FullName)
        if ($text.IndexOf($repositoryRoot, [StringComparison]::OrdinalIgnoreCase) -ge 0 -or
            $text.IndexOf('C:\Users\Steve', [StringComparison]::OrdinalIgnoreCase) -ge 0) {
            throw "Candidate text contains a developer-specific absolute path: $($file.FullName)"
        }
    }
}

Invoke-SafeSelfTest -PackageRoot $stage -Argument '--package-self-test'

New-DeterministicZip -SourceRoot $stage -Destination $zipPath
$zipHash = (Get-FileHash -Algorithm SHA256 -LiteralPath $zipPath).Hash
[IO.File]::WriteAllText(
    $digestPath,
    "$zipHash  $packageName.zip" + [Environment]::NewLine,
    [Text.UTF8Encoding]::new($false))
if ((Get-FileHash -Algorithm SHA256 -LiteralPath $zipPath).Hash -ne $zipHash) {
    throw 'ZIP SHA-256 verification failed immediately after creation.'
}

Remove-ValidatedDirectory -Path $extracted
New-Item -ItemType Directory -Path $extracted -Force | Out-Null
Add-Type -AssemblyName System.IO.Compression.FileSystem
[IO.Compression.ZipFile]::ExtractToDirectory($zipPath, $extracted)
Invoke-SafeSelfTest -PackageRoot $extracted -Argument '--package-self-test'
if (-not $SkipNeuralVerification) {
    Invoke-SafeSelfTest -PackageRoot $extracted -Argument '--neural-package-self-test'
}

$packageFiles = @(Get-ChildItem -LiteralPath $stage -Recurse -File)
$packageBytes = ($packageFiles | Measure-Object -Property Length -Sum).Sum
$zipBytes = (Get-Item -LiteralPath $zipPath).Length
$report = [ordered]@{
    product = 'Zoya Radio'
    candidateVersion = $candidateVersion
    sourceCommit = $sourceCommit
    sourceDirty = $sourceDirty
    runtimeIdentifier = $runtimeIdentifier
    selfContained = $true
    packageFileCount = $packageFiles.Count
    extractedBytes = $packageBytes
    zipBytes = $zipBytes
    zipSha256 = $zipHash
    eSpeakNamedFiles = 0
    eSpeakHashMatches = 0
    voiceFiles = @('voices/af_nicole.npy')
    privatePdbCount = $privatePdbs.Count
    packageSelfTest = 'passed'
    neuralSelfTest = if ($SkipNeuralVerification) { 'skipped by explicit switch' } else { 'passed' }
    cleanMachineEvidence = 'pending'
}
[IO.File]::WriteAllText(
    $reportPath,
    ($report | ConvertTo-Json -Depth 5) + [Environment]::NewLine,
    [Text.UTF8Encoding]::new($false))

Write-Host "PRIVATE ALPHA CANDIDATE CREATED"
Write-Host "ZIP: $zipPath"
Write-Host "SHA-256: $zipHash"
Write-Host "Extracted bytes: $packageBytes"
Write-Host "ZIP bytes: $zipBytes"
Write-Host "Private symbols: $symbols"
Write-Host "Extracted verification: $extracted"
Write-Host "Clean-machine/manual acceptance: PENDING"
