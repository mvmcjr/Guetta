<#
.SYNOPSIS
    Downloads libdave native binaries for Windows x64 and Linux x64 from GitHub releases.
.PARAMETER Force
    Re-download even if files already exist.
.PARAMETER Version
    The libdave version to download. Defaults to v1.1.1.
#>
param(
    [switch]$Force,
    [string]$Version = "v1.1.1"
)

$ErrorActionPreference = "Stop"

$scriptDir = $PSScriptRoot
$baseUrl = "https://github.com/discord/libdave/releases/download/$Version/cpp"

$targets = @(
    @{
        Name    = "Windows-X64"
        ZipName = "libdave-Windows-X64-boringssl.zip"
        OutDir  = Join-Path $scriptDir "runtimes\win-x64\native"
    },
    @{
        Name    = "Linux-X64"
        ZipName = "libdave-Linux-X64-boringssl.zip"
        OutDir  = Join-Path $scriptDir "runtimes\linux-x64\native"
    }
)

foreach ($target in $targets) {
    $outDir = $target.OutDir
    $zipName = $target.ZipName
    $url = "$baseUrl/$zipName"
    $tempZip = Join-Path $scriptDir $zipName
    $extractDir = Join-Path $scriptDir "temp_$($target.Name)"

    # Skip if already downloaded (unless -Force)
    if ((Test-Path $outDir) -and -not $Force) {
        $existingFiles = Get-ChildItem $outDir -File -ErrorAction SilentlyContinue
        if ($existingFiles.Count -gt 0) {
            Write-Host "[SKIP] $($target.Name) - native binaries already exist. Use -Force to re-download." -ForegroundColor Yellow
            continue
        }
    }

    Write-Host "[DOWNLOAD] $($target.Name) from $url..." -ForegroundColor Cyan

    # Download
    Invoke-WebRequest -Uri $url -OutFile $tempZip -UseBasicParsing

    # Extract to temp directory
    if (Test-Path $extractDir) {
        Remove-Item $extractDir -Recurse -Force
    }
    Expand-Archive -Path $tempZip -DestinationPath $extractDir -Force

    # Create output directory
    if (-not (Test-Path $outDir)) {
        New-Item -ItemType Directory -Path $outDir -Force | Out-Null
    }

    # Copy native binaries (dll, so, lib, a files) from extracted content
    $nativeExtensions = @("*.dll", "*.so", "*.so.*", "*.lib", "*.a", "*.dylib")
    $found = $false
    foreach ($ext in $nativeExtensions) {
        $files = Get-ChildItem -Path $extractDir -Filter $ext -Recurse -File
        foreach ($file in $files) {
            Copy-Item $file.FullName -Destination $outDir -Force
            Write-Host "  -> Copied $($file.Name)" -ForegroundColor Green
            $found = $true
        }
    }

    if (-not $found) {
        Write-Host "  [WARN] No native binary files found in archive. Copying all files..." -ForegroundColor Yellow
        $allFiles = Get-ChildItem -Path $extractDir -Recurse -File
        foreach ($file in $allFiles) {
            Copy-Item $file.FullName -Destination $outDir -Force
            Write-Host "  -> Copied $($file.Name)" -ForegroundColor Green
        }
    }

    # Cleanup
    Remove-Item $tempZip -Force -ErrorAction SilentlyContinue
    Remove-Item $extractDir -Recurse -Force -ErrorAction SilentlyContinue

    Write-Host "[DONE] $($target.Name) binaries placed in $outDir" -ForegroundColor Green
}

Write-Host ""
Write-Host "Fetch complete! Run 'dotnet build' to include the native binaries." -ForegroundColor Cyan
