<#
.SYNOPSIS
    Applies pending EF Core migrations against the target database.

.DESCRIPTION
    Runs `dotnet ef database update` for TrustPanel.Api using the connection string
    provided via the DATABASE_URL environment variable or the -ConnectionString parameter.

.PARAMETER ConnectionString
    Optional. Overrides DATABASE_URL env var.

.PARAMETER Project
    Path to the API project that owns the DbContext. Defaults to
    backend/src/TrustPanel.Api.

.EXAMPLE
    $env:DATABASE_URL = "Host=localhost;Database=trustpanel;Username=tp;Password=secret"
    .\scripts\migrate.ps1

.EXAMPLE
    .\scripts\migrate.ps1 -ConnectionString "Host=prod-db;Database=trustpanel;..."
#>
param(
    [string]$ConnectionString,
    [string]$Project = "backend/src/TrustPanel.Api"
)

Set-StrictMode -Version Latest
$ErrorActionPreference = "Stop"

$cs = if ($ConnectionString) { $ConnectionString } else { $env:DATABASE_URL }

if (-not $cs) {
    Write-Error "Provide a connection string via -ConnectionString or DATABASE_URL env var."
    exit 1
}

Write-Host "Running EF Core migrations..." -ForegroundColor Cyan
Write-Host "  Project: $Project"

$env:ConnectionStrings__Default = $cs

dotnet ef database update `
    --project $Project `
    --startup-project $Project `
    --no-build

if ($LASTEXITCODE -ne 0) {
    Write-Error "Migration failed (exit $LASTEXITCODE)."
    exit $LASTEXITCODE
}

Write-Host "Migrations applied successfully." -ForegroundColor Green
