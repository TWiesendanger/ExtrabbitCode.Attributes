<#
.SYNOPSIS
    Builds the add-in (which deploys it via Buildscript.cmd) and starts Inventor to test it.

.PARAMETER Version
    Inventor version to start. Defaults to 2026.

.PARAMETER Configuration
    Build configuration. Defaults to Debug.
#>
param(
    [ValidateSet('2025', '2026', '2027')]
    [string]$Version = '2026',

    [string]$Configuration = 'Debug'
)

$ErrorActionPreference = 'Stop'

$repoRoot = Split-Path -Parent $PSScriptRoot
$project = Join-Path $repoRoot 'ExtrabbitCode.Attributes\ExtrabbitCode.Attributes.csproj'
$devAddin = 'C:\ProgramData\Autodesk\Inventor Addins\ExtrabbitCode.Attributes.addin'
$inventorExe = "C:\Program Files\Autodesk\Inventor $Version\Bin\Inventor.exe"

if (-not (Test-Path $inventorExe)) {
    throw "Inventor $Version not found at '$inventorExe'."
}

# The deployed DLL is locked while Inventor runs, so the build could not replace it.
if (Get-Process -Name 'Inventor' -ErrorAction SilentlyContinue) {
    throw 'Inventor is already running. Close it first so the add-in can be redeployed.'
}

Write-Host "Building $Configuration..." -ForegroundColor Cyan
dotnet build $project -c $Configuration
if ($LASTEXITCODE -ne 0) {
    throw "Build failed with exit code $LASTEXITCODE."
}

# Other registrations with the same add-in id (installed bundle, old dev builds) can win over
# the dev build, in which case Inventor silently runs an old version.
$clientId = ([xml](Get-Content $devAddin)).Addin.ClientId
$searchPaths = @(
    'C:\ProgramData\Autodesk\Inventor Addins',
    "C:\ProgramData\Autodesk\Inventor $Version\Addins",
    "$env:APPDATA\Autodesk\Inventor $Version\Addins",
    "$env:APPDATA\Autodesk\ApplicationPlugins",
    'C:\ProgramData\Autodesk\ApplicationPlugins'
)
# Inventor also loads renamed manifests such as "*.addin.disabled", so match those too.
$duplicates = Get-ChildItem $searchPaths -Filter *.addin* -Recurse -ErrorAction SilentlyContinue |
    Where-Object { $_.FullName -ne $devAddin -and (Select-String -Path $_.FullName -Pattern $clientId -SimpleMatch -Quiet) } |
    Where-Object { ([xml](Get-Content $_.FullName)).Addin.Assembly }
foreach ($duplicate in $duplicates) {
    Write-Warning "Another add-in registration uses the same id and may load instead of the dev build: $($duplicate.FullName)"
}

Write-Host "Starting Inventor $Version..." -ForegroundColor Cyan
Start-Process -FilePath $inventorExe
