#!/usr/bin/env pwsh
<#
.SYNOPSIS
Build script for COBIe Manager project

.DESCRIPTION
Builds the COBIe Manager plugin for specified Revit versions and configurations.

.PARAMETER Configuration
The build configuration: Debug or Release (default: Release)

.PARAMETER RevitVersion
The target Revit version: 2023 or 2024 (default: 2024)

.PARAMETER All
Build all configurations for all supported Revit versions

.PARAMETER Clean
Clean build output before building

.EXAMPLE
.\build.ps1 -Configuration Release -RevitVersion 2024
.\build.ps1 -All
.\build.ps1 -Clean
#>

param(
    [Parameter(Position = 0)]
    [ValidateSet('Debug', 'Release')]
    [string]$Configuration = 'Release',

    [Parameter(Position = 1)]
    [ValidateSet('2023', '2024', '2025', '2026')]
    [string]$RevitVersion = '2024',

    [switch]$All,
    [switch]$Clean
)

$ErrorActionPreference = 'Stop'

# Get script directory
$ScriptDir = Split-Path -Parent $MyInvocation.MyCommand.Path
$ProjectPath = Join-Path $ScriptDir 'COBIeManager.csproj'
$SolutionPath = Join-Path $ScriptDir 'COBIe Manager.sln'

# Validate project exists
if (-not (Test-Path $ProjectPath)) {
    Write-Error "Project file not found: $ProjectPath"
    exit 1
}

function Get-MsbuildPath {
    $vsPath = & "C:\Program Files (x86)\Microsoft Visual Studio\Installer\vswhere.exe" -latest -property installationPath
    if ($LASTEXITCODE -ne 0) {
        Write-Error "Could not find Visual Studio installation"
        exit 1
    }
    $msbuildPath = Join-Path $vsPath "MSBuild\Current\Bin\MSBuild.exe"
    return $msbuildPath
}

function Build-Configuration {
    param(
        [string]$Config,
        [string]$Version
    )

    $fullConfig = "${Config}${Version}"
    Write-Host "Building $fullConfig..." -ForegroundColor Cyan

    $msbuild = Get-MsbuildPath
    $args = @(
        $SolutionPath,
        "/p:Configuration=$fullConfig",
        "/p:Platform=x64",
        "/v:minimal",
        "/m"
    )

    if ($Clean) {
        Write-Host "Cleaning $fullConfig..." -ForegroundColor Yellow
        & $msbuild $args "/t:Clean"
    }

    & $msbuild $args
    if ($LASTEXITCODE -ne 0) {
        Write-Error "Build failed for $fullConfig"
        exit 1
    }

    Write-Host "Build completed successfully for $fullConfig" -ForegroundColor Green

    $outputDir = Join-Path $ScriptDir "bin\$Config$Version"
    Write-Host "Output: $outputDir" -ForegroundColor Gray
}

# Main build logic
if ($All) {
    Write-Host "Building all configurations..." -ForegroundColor Cyan
    $versions = @('2023', '2024', '2025', '2026')
    foreach ($version in $versions) {
        Build-Configuration 'Debug' $version
        Build-Configuration 'Release' $version
    }
    Write-Host "All builds completed!" -ForegroundColor Green
}
else {
    Build-Configuration $Configuration $RevitVersion
}

Write-Host "`nBuild Summary:" -ForegroundColor Cyan
Write-Host "- Configuration: $Configuration"
Write-Host "- Revit Version: $RevitVersion"
Write-Host "- Output Directory: bin\$Configuration$RevitVersion"
Write-Host "`nNext steps:" -ForegroundColor Yellow
Write-Host "1. Copy COBIeManager.dll to Revit addins folder"
Write-Host "2. Verify COBIeManager.addin manifest is in the same directory"
Write-Host "3. Restart Revit to load the plugin"
