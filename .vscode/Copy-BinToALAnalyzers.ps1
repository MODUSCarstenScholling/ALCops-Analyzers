#!/usr/bin/env pwsh
<#
Copy-BinToALAnalyzers.ps1
Copies analyzer DLLs from ./src/**/bin/<Configuration>/<TargetFramework>/ into every installed
VS Code AL extension folder: ~/.vscode/extensions/ms-dynamics-smb.al-*/bin/Analyzers

Requires: PowerShell 7+ (for [System.IO.Path]::GetRelativePath)
#>

[CmdletBinding()]
param(
    [Parameter()]
    [ValidateSet('Debug', 'Release')]
    [string]$Configuration = 'Debug',

    [Parameter()]
    [ValidateSet('net10.0', 'net8.0', 'netstandard2.1')]
    [string]$TargetFramework = 'net10.0',

    # Add/remove projects here (folder name under ./src and dll name is assumed identical)
    [Parameter()]
    [string[]]$Cops = @(
        'ALCops.Common'
        'ALCops.ApplicationCop'
        'ALCops.DocumentationCop'
        'ALCops.FormattingCop'
        'ALCops.LinterCop'
        'ALCops.PlatformCop'
        'ALCops.TestAutomationCop'
    ),

    # Optional override if you want a non-default VS Code extensions location
    [Parameter()]
    [string]$ExtensionsRoot
)

Set-StrictMode -Version Latest
$ErrorActionPreference = 'Stop'

function New-DirectoryIfMissing {
    [CmdletBinding()]
    param(
        [Parameter(Mandatory)]
        [string]$Path
    )

    if (-not (Test-Path -LiteralPath $Path)) {
        New-Item -ItemType Directory -Path $Path -Force | Out-Null
    }
}

function Get-DefaultExtensionsRoot {
    $userProfile = [Environment]::GetFolderPath('UserProfile')
    if ([string]::IsNullOrWhiteSpace($userProfile)) {
        throw "Could not resolve UserProfile."
    }
    return (Join-Path $userProfile '.vscode/extensions')
}

$repoRoot = Resolve-Path (Join-Path $PSScriptRoot '..') | Select-Object -ExpandProperty Path

if ([string]::IsNullOrWhiteSpace($ExtensionsRoot)) {
    $ExtensionsRoot = Get-DefaultExtensionsRoot
}

$ExtensionsRoot = [System.IO.Path]::GetFullPath($ExtensionsRoot)

# Find all installed AL extension folders (can be multiple versions)
$alExtensionFolders =
Get-ChildItem -LiteralPath $ExtensionsRoot -Directory -ErrorAction SilentlyContinue |
Where-Object { $_.Name -like 'ms-dynamics-smb.al-*' }

if (-not $alExtensionFolders) {
    Write-Warning "No folders found under '$ExtensionsRoot' matching 'ms-dynamics-smb.al-*'."
    return
}

foreach ($ext in $alExtensionFolders) {
    $analyzersDir = Join-Path $ext.FullName 'bin/Analyzers'
    New-DirectoryIfMissing -Path $analyzersDir
}

foreach ($cop in $Cops) {
    $sourceDll = Join-Path $repoRoot "src/$cop/bin/$Configuration/$TargetFramework/$cop.dll"

    if (-not (Test-Path -LiteralPath $sourceDll)) {
        Write-Warning "Missing: $sourceDll"
        continue
    }

    foreach ($ext in $alExtensionFolders) {
        $destDll = Join-Path $ext.FullName "bin/Analyzers/$cop.dll"
        Copy-Item -LiteralPath $sourceDll -Destination $destDll -Force

        # Print path relative to the VS Code extensions root (no full C:\Users\... prefix)
        $relative = [System.IO.Path]::GetRelativePath($ExtensionsRoot, $destDll)
        Write-Host "Copied $cop -> $relative"
    }
}
