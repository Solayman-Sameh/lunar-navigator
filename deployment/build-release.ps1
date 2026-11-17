# LunarNavigator Windows Deployment Build Script
# Assembles an application-private CPython 3.11 runtime, compiles the C# GUI launcher,
# stages the fresh Unity Player, and builds the Inno Setup standalone installer.

[CmdletBinding()]
param(
    [string]$BuildId = ("build-" + (Get-Date).ToString("yyyyMMdd-HHmmss")),
    [switch]$SkipTests = $false,
    [string]$FrameworkReferenceDir = "",
    [string]$Wheelhouse = ""
)

$ErrorActionPreference = "Stop"
$RepoRoot = Split-Path -Parent $PSScriptRoot

Write-Host "========================================================" -ForegroundColor Cyan
Write-Host "  LunarNavigator Windows Deployment Build ($BuildId)   " -ForegroundColor Cyan
Write-Host "========================================================" -ForegroundColor Cyan

# 1. Locate and validate build prerequisites
$RoslynCsc = "C:\Program Files\Microsoft Visual Studio\2022\Community\MSBuild\Current\Bin\Roslyn\csc.exe"
if (-not (Test-Path $RoslynCsc)) {
    throw "Roslyn csc.exe compiler not found at: $RoslynCsc"
}

$IsccPaths = @(
    "$env:LOCALAPPDATA\Programs\Inno Setup 6\ISCC.exe",
    "C:\Program Files (x86)\Inno Setup 6\ISCC.exe",
    "C:\Program Files\Inno Setup 6\ISCC.exe"
)
$IsccExe = $IsccPaths | Where-Object { Test-Path $_ } | Select-Object -First 1
if (-not $IsccExe) {
    throw "Inno Setup compiler (ISCC.exe) not found. Please install Inno Setup 6."
}

$HostPython = Join-Path $RepoRoot "backend\.venv\Scripts\python.exe"
if (-not (Test-Path $HostPython)) {
    throw "Host Python virtual environment not found at: $HostPython"
}

Write-Host "[1/8] Prerequisites validated:" -ForegroundColor Green
Write-Host "  Roslyn CSC: $RoslynCsc"
Write-Host "  Inno ISCC:  $IsccExe"
Write-Host "  Host Python:$HostPython"

$UnityBuildSource = Join-Path $RepoRoot "Builds\Win64"
$SourceManifest = Join-Path $UnityBuildSource "source-hashes.txt"
if (-not (Test-Path $SourceManifest)) { throw "No Unity source manifest. Run Lunar Navigator/Build Windows 64-bit in the open Editor first." }
if ((Get-Content (Join-Path $UnityBuildSource "release-version.txt") -Raw).Trim() -ne "1.1.0") { throw "Unity release version is not 1.1.0" }
$Entries = @(Get-Content $SourceManifest)
$CurrentFiles = @(foreach ($dir in @("Assets", "Packages", "ProjectSettings")) { Get-ChildItem (Join-Path $RepoRoot $dir) -Recurse -File })
if ($Entries.Count -ne $CurrentFiles.Count) { throw "Unity source file set changed; rebuild player." }
foreach ($line in $Entries) {
    $parts = $line -split "\|", 2
    if ($parts.Count -ne 2 -or $parts[1] -match "(^|/)\.\.(/|$)" -or [IO.Path]::IsPathRooted($parts[1])) { throw "Invalid source manifest entry" }
    $path = Join-Path $RepoRoot $parts[1]
    if (-not (Test-Path $path) -or (Get-FileHash $path -Algorithm SHA256).Hash -ne $parts[0]) { throw "Unity source changed: $($parts[1]); rebuild player." }
}

# 2. Run backend test suite
if (-not $SkipTests) {
    Write-Host "[2/8] Running backend unit test suite..." -ForegroundColor Cyan
    & $HostPython -m pytest (Join-Path $RepoRoot "backend\tests") (Join-Path $RepoRoot "deployment\test_backend_lifecycle.py") -q -p no:cacheprovider
    if ($LASTEXITCODE -ne 0) {
        throw "Backend tests failed with exit code $LASTEXITCODE. Aborting build."
    }
    Write-Host "  Backend tests passed cleanly." -ForegroundColor Green
} else {
    Write-Host "[2/8] Skipping tests as requested." -ForegroundColor Yellow
}

# 3. Setup isolated staging directory
$PackagingDir = Join-Path $RepoRoot "Builds\Packaging"
$StagingDir = Join-Path $PackagingDir $BuildId
$DistDir = Join-Path $RepoRoot "Builds\Dist"
$DownloadsDir = Join-Path $PackagingDir "Downloads"

if ($BuildId -notmatch "^[a-zA-Z0-9_-]+$") { throw "Invalid BuildId" }
if (Test-Path $StagingDir) { throw "Build directory already exists: $StagingDir" }
New-Item -ItemType Directory -Path $StagingDir -Force | Out-Null
New-Item -ItemType Directory -Path (Join-Path $StagingDir "bin") -Force | Out-Null
New-Item -ItemType Directory -Path (Join-Path $StagingDir "backend") -Force | Out-Null
New-Item -ItemType Directory -Path $DistDir -Force | Out-Null
New-Item -ItemType Directory -Path $DownloadsDir -Force | Out-Null

Write-Host "[3/8] Staging directory initialized: $StagingDir" -ForegroundColor Green

# 4. Prepare private CPython 3.11 embeddable runtime
Write-Host "[4/8] Assembling private CPython 3.11 embeddable runtime..." -ForegroundColor Cyan
$EmbedZip = Join-Path $DownloadsDir "python-3.11.9-embed-amd64.zip"
$ExpectedSha256 = "009D6BF7E3B2DDCA3D784FA09F90FE54336D5B60F0E0F305C37F400BF83CFD3B"

if (-not (Test-Path $EmbedZip)) {
    Write-Host "  Downloading official Python 3.11.9 embeddable zip..."
    [Net.ServicePointManager]::SecurityProtocol = [Net.SecurityProtocolType]::Tls12
    Invoke-WebRequest -Uri "https://www.python.org/ftp/python/3.11.9/python-3.11.9-embed-amd64.zip" -OutFile $EmbedZip
}

$ActualSha256 = (Get-FileHash -Path $EmbedZip -Algorithm SHA256).Hash
if ($ActualSha256 -ne $ExpectedSha256) {
    throw "SHA256 mismatch for Python embeddable zip. Expected: $ExpectedSha256, Actual: $ActualSha256"
}
Write-Host "  Verified Python 3.11.9 embeddable zip SHA-256." -ForegroundColor Green

# Extract embeddable zip into staging/backend
Expand-Archive -Path $EmbedZip -DestinationPath (Join-Path $StagingDir "backend") -Force

# Configure python311._pth to import vendor directory and application root hermetically
$PthContent = @"
python311.zip
.
vendor
"@
Set-Content -Path (Join-Path $StagingDir "backend\python311._pth") -Value $PthContent -Encoding ASCII

# Copy backend application source and data
Copy-Item (Join-Path $RepoRoot "backend\run_backend.py") (Join-Path $StagingDir "backend\run_backend.py")
$ScienceStaging = Join-Path $StagingDir "backend\science"
New-Item -ItemType Directory -Path $ScienceStaging -Force | Out-Null
Copy-Item (Join-Path $RepoRoot "backend\science\*.py") $ScienceStaging
New-Item -ItemType Directory -Path (Join-Path $StagingDir "backend\data") -Force | Out-Null
Copy-Item (Join-Path $RepoRoot "backend\data\sites.json") (Join-Path $StagingDir "backend\data\sites.json")

# Vendor runtime dependencies
Write-Host "  Vendoring pinned runtime dependencies into backend\vendor..." -ForegroundColor Cyan
$VendorDir = Join-Path $StagingDir "backend\vendor"
New-Item -ItemType Directory -Path $VendorDir -Force | Out-Null
$ReqRuntime = Join-Path $RepoRoot "backend\requirements-runtime.txt"
if ($Wheelhouse) {
    & $HostPython (Join-Path $PSScriptRoot "assemble_vendor.py") $ReqRuntime (Resolve-Path $Wheelhouse).Path $VendorDir
} else {
    & $HostPython -m pip install --target $VendorDir -r $ReqRuntime --only-binary=:all: --no-compile --quiet
}
if ($LASTEXITCODE -ne 0) { throw "Runtime dependency assembly failed." }

# Smoke test the assembled private runtime
Write-Host "  Testing assembled private backend runtime..." -ForegroundColor Cyan
$StagingPython = Join-Path $StagingDir "backend\python.exe"
& $StagingPython -c "import fastapi, uvicorn, pydantic, pydantic_core, certifi; from science.api import create_app; from science.sites import supported_catalog; assert supported_catalog(); create_app(); print('  [OK] Private runtime imports verified.')"
if ($LASTEXITCODE -ne 0) {
    throw "Assembled private Python runtime failed import verification."
}

# 5. Build C# GUI Launcher targeting .NET Framework 4.8
Write-Host "[5/8] Compiling C# Windows GUI Launcher (LunarNavigator.exe)..." -ForegroundColor Cyan
$LauncherOut = Join-Path $StagingDir "LunarNavigator.exe"
$NetFrameworkRefDir = $FrameworkReferenceDir
if (-not $NetFrameworkRefDir) {
    $NetFrameworkRefDir = Join-Path ([Environment]::GetFolderPath("ProgramFilesX86")) "Reference Assemblies\Microsoft\Framework\.NETFramework\v4.8"
}
if (-not (Test-Path $NetFrameworkRefDir)) { throw ".NET Framework 4.8 targeting pack required; runtime assemblies are not a targeting pack." }
$LauncherSources = @(
    (Join-Path $RepoRoot "launcher\NativeMethods.cs"),
    (Join-Path $RepoRoot "launcher\JobObject.cs"),
    (Join-Path $RepoRoot "launcher\BackendSession.cs"),
    (Join-Path $RepoRoot "launcher\Program.cs")
)
$References = @(
    (Join-Path $NetFrameworkRefDir "System.dll"),
    (Join-Path $NetFrameworkRefDir "System.Core.dll"),
    (Join-Path $NetFrameworkRefDir "System.Windows.Forms.dll"),
    (Join-Path $NetFrameworkRefDir "System.Drawing.dll"),
    (Join-Path $NetFrameworkRefDir "System.Web.Extensions.dll")
) -join ","

$AppIcon = Join-Path $RepoRoot "Assets\LunarNavigator\Branding\LunarNavigator.ico"
& $RoslynCsc /noconfig /nostdlib+ /r:"$NetFrameworkRefDir\mscorlib.dll" /target:winexe /platform:x64 /win32icon:"$AppIcon" /out:"$LauncherOut" /r:"$References" $LauncherSources
if ($LASTEXITCODE -ne 0 -or -not (Test-Path $LauncherOut)) {
    throw "Failed to compile Launcher with Roslyn csc.exe."
}
$LauncherTests = Join-Path $StagingDir "LauncherTests.exe"
& $RoslynCsc /noconfig /nostdlib+ /r:"$NetFrameworkRefDir\mscorlib.dll" /target:exe /main:LauncherTests /platform:x64 /out:"$LauncherTests" /r:"$References" $LauncherSources (Join-Path $PSScriptRoot "LauncherTests.cs")
if ($LASTEXITCODE -ne 0) { throw "Launcher test compilation failed" }
& $LauncherTests
if ($LASTEXITCODE -ne 0) { throw "Launcher regression tests failed" }
Write-Host "  Launcher compiled successfully ($((Get-Item $LauncherOut).Length) bytes)." -ForegroundColor Green

# 6. Stage Unity Player build
Write-Host "[6/8] Staging Unity 6 HDRP Standalone Player..." -ForegroundColor Cyan
if (-not (Test-Path (Join-Path $UnityBuildSource "LunarNavigator.exe"))) {
    throw "Unity player build not found in $UnityBuildSource. Please generate a fresh build first."
}

# Copy player files into staging/bin
$BinStaging = Join-Path $StagingDir "bin"
New-Item -ItemType Directory -Path $BinStaging -Force | Out-Null
Get-ChildItem $UnityBuildSource | Where-Object {
    $_.Name -notlike "*BackUpThisFolder_ButDontShipItWithYourGame*" -and
    $_.Extension -ne ".log" -and $_.Name -notin @("source-hashes.txt", "release-version.txt")
} | Copy-Item -Destination $BinStaging -Recurse -Force
# Also copy Third-Party notices
Copy-Item (Join-Path $RepoRoot "deployment\THIRD_PARTY_NOTICES.txt") (Join-Path $StagingDir "THIRD_PARTY_NOTICES.txt")

Write-Host "  Unity Player staged into bin/." -ForegroundColor Green

# 7. Compile Inno Setup Installer
Write-Host "[7/8] Compiling Inno Setup distribution package..." -ForegroundColor Cyan
$IssScript = Join-Path $RepoRoot "installer\LunarNavigator.iss"
$CandidateDir = Join-Path $StagingDir "installer"
New-Item -ItemType Directory -Path $CandidateDir -Force | Out-Null
& $IsccExe "/DStagingDir=$StagingDir" "/O$CandidateDir" "$IssScript"
if ($LASTEXITCODE -ne 0) {
    throw "Inno Setup compilation failed with exit code $LASTEXITCODE."
}

$InstallerPath = Join-Path $CandidateDir "LunarNavigator_Setup.exe"
if (-not (Test-Path $InstallerPath)) {
    throw "Expected installer output was not found at: $InstallerPath"
}

# 8. Compute checksums and write build manifest
Write-Host "[8/8] Generating SHA-256 and build manifest..." -ForegroundColor Cyan
$InstallerHash = (Get-FileHash -Path $InstallerPath -Algorithm SHA256).Hash
$Sha256File = "$InstallerPath.sha256"
"$InstallerHash *$((Split-Path $InstallerPath -Leaf))" | Set-Content -Path $Sha256File -Encoding ASCII

$InstallerSizeMB = [Math]::Round(((Get-Item $InstallerPath).Length / 1MB), 2)

$Manifest = [ordered]@{
    "build_id" = $BuildId
    "timestamp_utc" = (Get-Date).ToUniversalTime().ToString("yyyy-MM-ddTHH:mm:ssZ")
    "installer_file" = "LunarNavigator_Setup.exe"
    "installer_path" = $InstallerPath
    "installer_size_bytes" = (Get-Item $InstallerPath).Length
    "installer_size_mb" = $InstallerSizeMB
    "installer_sha256" = $InstallerHash
    "python_runtime" = "CPython 3.11.9 x64 embeddable"
    "python_embed_sha256" = $ActualSha256
    "launcher_target" = ".NET Framework 4.8 x64 (Roslyn)"
    "framework_reference_dir" = $NetFrameworkRefDir
    "unity_version" = "6000.6.3f1 HDRP"
    "inno_setup_version" = (Get-Item $IsccExe).VersionInfo.FileVersion
    "app_version" = "1.1.0"
    "git_revision" = (& git -C $RepoRoot rev-parse HEAD)
    "git_status" = @(& git -C $RepoRoot status --porcelain)
}

$ManifestPath = Join-Path $CandidateDir "build-manifest.json"
$Manifest | ConvertTo-Json -Depth 4 | Set-Content -Path $ManifestPath -Encoding UTF8

Write-Host ""
Write-Host "========================================================" -ForegroundColor Green
Write-Host "  BUILD SUCCESSFUL!                                     " -ForegroundColor Green
Write-Host "========================================================" -ForegroundColor Green
Write-Host "  Installer: $InstallerPath" -ForegroundColor White
Write-Host "  Size:      $InstallerSizeMB MB" -ForegroundColor White
Write-Host "  SHA-256:   $InstallerHash" -ForegroundColor White

Write-Host "  Manifest:  $ManifestPath" -ForegroundColor White
Write-Host "========================================================" -ForegroundColor Green
