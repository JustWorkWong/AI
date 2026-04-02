param(
    [string]$OutputPath = ".\artifacts\k8s",
    [string]$Environment = "Production",
    [string]$AppHost = ".\src\Wms.AppHost\Wms.AppHost.csproj"
)

Set-StrictMode -Version Latest
$ErrorActionPreference = "Stop"

$repoRoot = Split-Path -Parent $PSScriptRoot
$resolvedOutput = [System.IO.Path]::GetFullPath((Join-Path $repoRoot $OutputPath))
$resolvedAppHost = [System.IO.Path]::GetFullPath((Join-Path $repoRoot $AppHost))

if (-not (Test-Path $resolvedAppHost)) {
    throw "AppHost project not found: $resolvedAppHost"
}

$aspireCommand = Get-Command aspire -ErrorAction SilentlyContinue
$fallbackAspirePath = Join-Path $env:USERPROFILE ".aspire\bin\aspire.exe"

if (-not $aspireCommand -and (Test-Path $fallbackAspirePath)) {
    $aspireCommand = $fallbackAspirePath
}

if (-not $aspireCommand) {
    throw "Aspire CLI not found. Install it with: irm https://aspire.dev/install.ps1 | iex"
}

New-Item -ItemType Directory -Force -Path $resolvedOutput | Out-Null

Push-Location $repoRoot
try {
    & $aspireCommand publish --non-interactive --apphost $resolvedAppHost --output-path $resolvedOutput --environment $Environment
}
finally {
    Pop-Location
}
