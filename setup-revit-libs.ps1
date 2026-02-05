#!/usr/bin/env pwsh
<#
.SYNOPSIS
Setup script to copy Revit API DLLs from installations to local lib folder

.DESCRIPTION
This script copies the required Revit API DLLs from your Revit installations
to the local lib folder for building the plugin without NuGet dependencies.

.EXAMPLE
.\setup-revit-libs.ps1
Copies DLLs from all installed Revit versions

.EXAMPLE
.\setup-revit-libs.ps1 -Version 2024
Copies DLLs only for Revit 2024
#>

param(
    [Parameter(Position = 0)]
    [ValidateSet('2023', '2024', '2025', '2026', 'All')]
    [string]$Version = 'All'
)

$ErrorActionPreference = 'Continue'

# Define Revit versions and their installation paths
$revitVersions = @{
    '2023' = 'C:\Program Files\Autodesk\Revit 2023'
    '2024' = 'C:\Program Files\Autodesk\Revit 2024'
    '2025' = 'C:\Program Files\Autodesk\Revit 2025'
    '2026' = 'C:\Program Files\Autodesk\Revit 2026'
}

# Required DLLs for each Revit version
$requiredDlls = @('RevitAPI.dll', 'RevitAPIUI.dll', 'UIFramework.dll')

# Get script directory
$scriptDir = Split-Path -Parent $MyInvocation.MyCommand.Path
$libDir = Join-Path $scriptDir 'lib'

# Ensure lib directory exists
if (-not (Test-Path $libDir)) {
    New-Item -ItemType Directory -Path $libDir -Force | Out-Null
}

function Copy-RevitDlls {
    param(
        [string]$RevitVersion,
        [string]$InstallPath
    )

    Write-Host "`nProcessing Revit $RevitVersion..." -ForegroundColor Cyan

    # Check if Revit is installed
    if (-not (Test-Path $InstallPath)) {
        Write-Host "  [WARN] Revit $RevitVersion not found at: $InstallPath" -ForegroundColor Yellow
        Write-Host "  Skipping..." -ForegroundColor Gray
        return $false
    }

    # Create target directory
    $targetDir = Join-Path $libDir "Revit$RevitVersion"
    if (-not (Test-Path $targetDir)) {
        New-Item -ItemType Directory -Path $targetDir -Force | Out-Null
    }

    $copiedCount = 0
    $skippedCount = 0

    # Copy each required DLL
    foreach ($dll in $requiredDlls) {
        $sourcePath = Join-Path $InstallPath $dll
        $targetPath = Join-Path $targetDir $dll

        if (Test-Path $sourcePath) {
            try {
                Copy-Item -Path $sourcePath -Destination $targetPath -Force
                $fileInfo = Get-Item $targetPath
                $version = [System.Diagnostics.FileVersionInfo]::GetVersionInfo($targetPath).FileVersion
                Write-Host "  [OK] Copied $dll (v$version)" -ForegroundColor Green
                $copiedCount++
            }
            catch {
                Write-Host "  [ERROR] Failed to copy $dll : $_" -ForegroundColor Red
            }
        }
        else {
            Write-Host "  [WARN] $dll not found in installation" -ForegroundColor Yellow
            $skippedCount++
        }
    }

    Write-Host "  Summary: $copiedCount copied, $skippedCount skipped" -ForegroundColor Gray
    return ($copiedCount -gt 0)
}

# Main execution
Write-Host "═══════════════════════════════════════════════════════" -ForegroundColor Cyan
Write-Host "  Revit API DLLs Setup" -ForegroundColor Cyan
Write-Host "═══════════════════════════════════════════════════════" -ForegroundColor Cyan

$successCount = 0
$totalCount = 0

if ($Version -eq 'All') {
    foreach ($ver in $revitVersions.Keys | Sort-Object) {
        $totalCount++
        if (Copy-RevitDlls -RevitVersion $ver -InstallPath $revitVersions[$ver]) {
            $successCount++
        }
    }
}
else {
    $totalCount = 1
    if (Copy-RevitDlls -RevitVersion $Version -InstallPath $revitVersions[$Version]) {
        $successCount = 1
    }
}

# Final summary
Write-Host "`n═══════════════════════════════════════════════════════" -ForegroundColor Cyan
if ($successCount -eq $totalCount) {
    Write-Host "[SUCCESS] Setup completed successfully!" -ForegroundColor Green
    Write-Host "  $successCount of $totalCount Revit version(s) configured" -ForegroundColor Green
}
elseif ($successCount -gt 0) {
    Write-Host "[PARTIAL] Setup partially completed" -ForegroundColor Yellow
    Write-Host "  $successCount of $totalCount Revit version(s) configured" -ForegroundColor Yellow
}
else {
    Write-Host "[FAILED] Setup failed - no Revit installations found" -ForegroundColor Red
    Write-Host "  Please install Revit or check installation paths" -ForegroundColor Red
    exit 1
}

Write-Host "`nNext steps:" -ForegroundColor Cyan
Write-Host "  1. Build the project: .\build.ps1" -ForegroundColor White
Write-Host "  2. Or build specific version: .\build.ps1 -RevitVersion 2024" -ForegroundColor White
Write-Host "═══════════════════════════════════════════════════════" -ForegroundColor Cyan
