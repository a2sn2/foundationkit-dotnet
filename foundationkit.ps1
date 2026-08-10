param(
    [Parameter(Position = 0)]
    [ValidateSet("start", "stop", "status", "logs", "test", "pack", "help")]
    [string]$Action = "help",

    [ValidateSet("Workbench")]
    [string]$Target = "Workbench"
)

$ErrorActionPreference = "Stop"
$Root = Split-Path -Parent $MyInvocation.MyCommand.Path

function Show-Help {
    Write-Host "FoundationKit Core manager"
    Write-Host "  .\foundationkit.ps1 start  -Target Workbench"
    Write-Host "  .\foundationkit.ps1 stop   -Target Workbench"
    Write-Host "  .\foundationkit.ps1 status -Target Workbench"
    Write-Host "  .\foundationkit.ps1 logs   -Target Workbench"
    Write-Host "  .\foundationkit.ps1 test"
    Write-Host "  .\foundationkit.ps1 pack"
}

switch ($Action) {
    "start"  { & (Join-Path $Root "scripts/run-workbench.ps1") }
    "stop"   { & (Join-Path $Root "scripts/stop-workbench.ps1") }
    "status" { docker compose -f (Join-Path $Root "deploy/docker-compose.yml") ps }
    "logs"   { docker compose -f (Join-Path $Root "deploy/docker-compose.yml") logs --tail 200 workbench }
    "test"   { dotnet test (Join-Path $Root "FoundationKit.sln") --configuration Release }
    "pack"   { & (Join-Path $Root "scripts/pack.ps1") -Configuration Release -OutputDirectory (Join-Path $Root "artifacts/packages") }
    "help"   { Show-Help }
}
