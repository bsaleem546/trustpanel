<#
.SYNOPSIS
    Seeds or updates the billing plans in the TrustPanel database.

.DESCRIPTION
    Connects directly to PostgreSQL and upserts the canonical plan rows that
    IPlanResolver expects. Run this after migrate.ps1 on a fresh deployment or
    when plan definitions change.

.PARAMETER ConnectionString
    PostgreSQL connection string. Falls back to DATABASE_URL env var.

.EXAMPLE
    $env:DATABASE_URL = "Host=localhost;Database=trustpanel;Username=tp;Password=secret"
    .\scripts\seed-plans.ps1
#>
param(
    [string]$ConnectionString
)

Set-StrictMode -Version Latest
$ErrorActionPreference = "Stop"

$cs = if ($ConnectionString) { $ConnectionString } else { $env:DATABASE_URL }
if (-not $cs) {
    Write-Error "Provide -ConnectionString or set DATABASE_URL."
    exit 1
}

# Requires psql on PATH (ships with PostgreSQL client tools).
if (-not (Get-Command psql -ErrorAction SilentlyContinue)) {
    Write-Error "psql not found on PATH. Install the PostgreSQL client tools."
    exit 1
}

$sql = @'
INSERT INTO "Plans" ("Id","Name","Slug","MonthlyPriceCents","AnnualPriceCents",
    "TestimonialLimit","WidgetLimit","TeamMemberLimit","HasVideoTestimonials",
    "HasCustomDomain","HasAiFeatures","HasApiAccess","HasWhiteLabel","IsActive")
VALUES
  (gen_random_uuid(), 'Free',       'free',       0,      0,      50,  2,   1, false, false, false, false, false, true),
  (gen_random_uuid(), 'Starter',    'starter',    2900,   29000,  500, 5,   3, false, false, false, false, false, true),
  (gen_random_uuid(), 'Growth',     'growth',     7900,   79000,  5000,20,  10, true,  false, true,  true,  false, true),
  (gen_random_uuid(), 'Agency',     'agency',     19900,  199000, null,null,null,true,  true,  true,  true,  true,  true)
ON CONFLICT ("Slug") DO UPDATE SET
  "MonthlyPriceCents"  = EXCLUDED."MonthlyPriceCents",
  "AnnualPriceCents"   = EXCLUDED."AnnualPriceCents",
  "TestimonialLimit"   = EXCLUDED."TestimonialLimit",
  "WidgetLimit"        = EXCLUDED."WidgetLimit",
  "TeamMemberLimit"    = EXCLUDED."TeamMemberLimit",
  "HasVideoTestimonials"= EXCLUDED."HasVideoTestimonials",
  "HasCustomDomain"    = EXCLUDED."HasCustomDomain",
  "HasAiFeatures"      = EXCLUDED."HasAiFeatures",
  "HasApiAccess"       = EXCLUDED."HasApiAccess",
  "HasWhiteLabel"      = EXCLUDED."HasWhiteLabel",
  "IsActive"           = EXCLUDED."IsActive";
'@

$tmpFile = [System.IO.Path]::GetTempFileName() + ".sql"
$sql | Set-Content -Path $tmpFile -Encoding utf8

Write-Host "Seeding plans..." -ForegroundColor Cyan
$env:PGPASSWORD = ($cs -match 'Password=([^;]+)' ? $Matches[1] : "")
psql $cs -f $tmpFile

if ($LASTEXITCODE -ne 0) {
    Write-Error "Seed failed (exit $LASTEXITCODE)."
    Remove-Item $tmpFile -ErrorAction SilentlyContinue
    exit $LASTEXITCODE
}

Remove-Item $tmpFile -ErrorAction SilentlyContinue
Write-Host "Plans seeded successfully." -ForegroundColor Green
