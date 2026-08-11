$ErrorActionPreference = "Stop"

$root = Split-Path -Parent $PSScriptRoot
Set-Location $root

if (-not (Get-Command docker -ErrorAction SilentlyContinue)) {
    throw "Docker Desktop is required."
}

$generatedRoot = Join-Path $root "generated"
if (-not (Test-Path $generatedRoot)) {
    New-Item -ItemType Directory -Path $generatedRoot | Out-Null
}

if ([string]::IsNullOrWhiteSpace($env:FOUNDATIONKIT_SQL_PASSWORD)) {
    $suffix = [Guid]::NewGuid().ToString("N").Substring(0, 18)
    $env:FOUNDATIONKIT_SQL_PASSWORD = "Fkit!${suffix}Aa1"
}

docker compose -f deploy/docker-compose.yml up --build -d
if ($LASTEXITCODE -ne 0) {
    throw "Docker Compose failed to start the Workbench."
}

$url = "http://localhost:8080"
for ($attempt = 1; $attempt -le 120; $attempt++) {
    try {
        Invoke-RestMethod -Uri "$url/api/health" -TimeoutSec 3 | Out-Null
        Write-Host "FoundationKit Workbench is ready: $url"
        Write-Host "Generated projects will be written under: $generatedRoot"
        Start-Process $url
        exit 0
    }
    catch {
        Start-Sleep -Seconds 2
    }
}

docker compose -f deploy/docker-compose.yml logs --tail=200
throw "Workbench did not become healthy. Review the logs above."