#!/usr/bin/env pwsh
# Regenerate the Commander Spellbook typed client from the live OpenAPI spec.
# Both Windows PowerShell 5.1 and pwsh (cross-platform) supported.
# Requires Kiota CLI 2.x+ (the runtime packages are pinned at 2.0.0 in Directory.Packages.props).

$ErrorActionPreference = "Stop"

$root        = Resolve-Path "$PSScriptRoot/.."
$schemaPath  = Join-Path $root "src/MysticForge.CommanderSpellbook/.openapi/schema.json"
$generatedDir= Join-Path $root "src/MysticForge.CommanderSpellbook/Generated"
$schemaUrl   = "https://backend.commanderspellbook.com/schema/"

# Pre-flight: kiota must be on PATH.
$kiota = Get-Command kiota -ErrorAction SilentlyContinue
if (-not $kiota) {
    Write-Error "kiota CLI not found. Install with: dotnet tool install --global Microsoft.OpenApi.Kiota (requires Kiota CLI 2.x+)"
    exit 1
}

Write-Host "Downloading OpenAPI spec from $schemaUrl ..."
Invoke-WebRequest -Uri $schemaUrl -OutFile $schemaPath -UseBasicParsing

Write-Host "Wiping previous generated output ..."
if (Test-Path $generatedDir) { Remove-Item -Recurse -Force $generatedDir }
New-Item -ItemType Directory -Path $generatedDir | Out-Null

Write-Host "Running kiota generate ..."
& kiota generate `
    --openapi $schemaPath `
    --language csharp `
    --output $generatedDir `
    --namespace-name "MysticForge.CommanderSpellbook.Generated" `
    --class-name "SpellbookApiClient" `
    --clean-output `
    --clear-cache

if ($LASTEXITCODE -ne 0) { Write-Error "kiota generate failed"; exit $LASTEXITCODE }

Write-Host "Codegen complete. Review the diff and commit if intended."
