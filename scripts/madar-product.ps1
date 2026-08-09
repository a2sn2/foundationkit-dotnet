[CmdletBinding()]
param(
    [Parameter(Mandatory = $true, Position = 0)]
    [ValidateSet('start', 'status', 'logs', 'stop')]
    [string]$Action,

    [switch]$Reset,

    [string]$BaseUrl = 'http://localhost:8100'
)

$ErrorActionPreference = 'Stop'
Set-StrictMode -Version Latest

$RepoRoot = Split-Path -Parent $PSScriptRoot
$ComposeFile = Join-Path $RepoRoot 'deploy\madar-compose.yml'
$ComposeProject = 'madar-product'
$LocalRoot = Join-Path $RepoRoot '.local'
$ConfigPath = Join-Path $LocalRoot 'madar-product.env'

function New-RandomSecret {
    param([int]$Bytes = 24)

    $buffer = New-Object byte[] $Bytes
    $rng = [System.Security.Cryptography.RandomNumberGenerator]::Create()
    try {
        $rng.GetBytes($buffer)
    }
    finally {
        $rng.Dispose()
    }

    return ([Convert]::ToBase64String($buffer) -replace '[+/=]', 'x') + '!Aa1'
}

function Protect-LocalFile {
    param([Parameter(Mandatory = $true)][string]$Path)

    if ($env:OS -ne 'Windows_NT') {
        return
    }

    try {
        $identity = [System.Security.Principal.WindowsIdentity]::GetCurrent()
        $sid = $identity.User.Value
        & icacls $Path /inheritance:r /grant:r "*$sid`:(F)" *> $null
        if ($LASTEXITCODE -ne 0) {
            throw "icacls returned exit code $LASTEXITCODE."
        }
    }
    catch {
        throw "Unable to restrict Madar local credential file ACL: $Path. $($_.Exception.Message)"
    }
}

function Get-LocalConfig {
    if (-not (Test-Path $ConfigPath)) {
        return $null
    }

    $values = @{}
    Get-Content -LiteralPath $ConfigPath -Encoding UTF8 | ForEach-Object {
        $line = $_.Trim()
        if (-not $line -or $line.StartsWith('#')) {
            return
        }

        $separator = $line.IndexOf('=')
        if ($separator -le 0) {
            return
        }

        $values[$line.Substring(0, $separator)] = $line.Substring($separator + 1)
    }

    return $values
}

function Initialize-LocalConfig {
    if ($Reset -and (Test-Path $ConfigPath)) {
        Remove-Item -LiteralPath $ConfigPath -Force
    }

    $existing = Get-LocalConfig
    if ($null -ne $existing) {
        Protect-LocalFile -Path $ConfigPath
        return $existing
    }

    New-Item -ItemType Directory -Path $LocalRoot -Force | Out-Null
    $values = @{
        MADAR_SQL_PASSWORD      = New-RandomSecret
        MADAR_ADMIN_EMAIL       = 'admin@madar.local'
        MADAR_ADMIN_PASSWORD    = New-RandomSecret
        MADAR_OPERATOR_EMAIL    = 'operator@madar.local'
        MADAR_OPERATOR_PASSWORD = New-RandomSecret
        MADAR_SLA_ENABLED       = 'false'
        MADAR_SLA_LOW           = '01:00:00'
        MADAR_SLA_MEDIUM        = '01:00:00'
        MADAR_SLA_HIGH          = '01:00:00'
        MADAR_SLA_CRITICAL      = '01:00:00'
    }

    $content = @(
        '# Local Madar development settings. Do not commit this file.'
        '# SLA is disabled by default. Duration values are development placeholders only.'
        ($values.GetEnumerator() | Sort-Object Key | ForEach-Object { '{0}={1}' -f $_.Key, $_.Value })
    )
    [System.IO.File]::WriteAllLines($ConfigPath, $content, [System.Text.UTF8Encoding]::new($false))
    Protect-LocalFile -Path $ConfigPath
    return $values
}

function Get-ComposeEnvironment {
    $config = Get-LocalConfig
    if ($null -ne $config) {
        return $config
    }

    return @{
        MADAR_SQL_PASSWORD = 'unused'
        MADAR_ADMIN_EMAIL = 'unused@madar.local'
        MADAR_ADMIN_PASSWORD = 'unused'
        MADAR_OPERATOR_EMAIL = 'unused@madar.local'
        MADAR_OPERATOR_PASSWORD = 'unused'
        MADAR_SLA_ENABLED = 'false'
        MADAR_SLA_LOW = '01:00:00'
        MADAR_SLA_MEDIUM = '01:00:00'
        MADAR_SLA_HIGH = '01:00:00'
        MADAR_SLA_CRITICAL = '01:00:00'
    }
}

function Invoke-WithMadarEnvironment {
    param(
        [Parameter(Mandatory = $true)][System.Collections.IDictionary]$Values,
        [Parameter(Mandatory = $true)][scriptblock]$Script
    )

    $original = @{}
    foreach ($name in $Values.Keys) {
        $original[$name] = [Environment]::GetEnvironmentVariable([string]$name, 'Process')
        [Environment]::SetEnvironmentVariable([string]$name, [string]$Values[$name], 'Process')
    }

    try {
        & $Script
    }
    finally {
        foreach ($name in $Values.Keys) {
            [Environment]::SetEnvironmentVariable([string]$name, $original[$name], 'Process')
        }
    }
}

function Test-DockerReady {
    if (-not (Get-Command docker -ErrorAction SilentlyContinue)) {
        return $false
    }

    $previousErrorActionPreference = $ErrorActionPreference
    try {
        $ErrorActionPreference = 'SilentlyContinue'

        & docker info --format '{{.ServerVersion}}' *> $null
        if ($LASTEXITCODE -ne 0) {
            return $false
        }

        & docker compose version *> $null
        return $LASTEXITCODE -eq 0
    }
    catch {
        return $false
    }
    finally {
        $ErrorActionPreference = $previousErrorActionPreference
    }
}

function Invoke-MadarCompose {
    param(
        [Parameter(Mandatory = $true)][System.Collections.IDictionary]$Values,
        [Parameter(Mandatory = $true)][string[]]$Arguments
    )

    Invoke-WithMadarEnvironment -Values $Values -Script {
        & docker compose --project-name $ComposeProject -f $ComposeFile @Arguments
        if ($LASTEXITCODE -ne 0) {
            throw "Madar Docker Compose command failed with exit code $LASTEXITCODE."
        }
    }
}

function Get-Health {
    param([string]$Path)

    try {
        return Invoke-RestMethod -Uri ($BaseUrl.TrimEnd('/') + $Path) -TimeoutSec 5
    }
    catch {
        return $null
    }
}

function Wait-MadarReady {
    param([int]$Attempts = 90)

    for ($attempt = 1; $attempt -le $Attempts; $attempt++) {
        $ready = Get-Health -Path '/health/ready'
        if ($null -ne $ready -and $ready.status -eq 'ready') {
            return
        }

        Start-Sleep -Seconds 2
    }

    throw "Madar did not become ready at $BaseUrl before the timeout."
}

function Show-Status {
    $live = Get-Health -Path '/health/live'
    $ready = Get-Health -Path '/health/ready'

    if ($null -eq $live) {
        Write-Host 'Madar: STOPPED or unreachable' -ForegroundColor Yellow
        return
    }

    if ($null -ne $ready -and $ready.status -eq 'ready') {
        Write-Host 'Madar: READY' -ForegroundColor Green
    }
    else {
        Write-Host 'Madar: LIVE but NOT READY' -ForegroundColor Yellow
    }

    Write-Host "URL: $BaseUrl"
}

if (-not (Test-Path $ComposeFile)) {
    throw "Madar compose file not found: $ComposeFile"
}

switch ($Action) {
    'start' {
        if (-not (Test-DockerReady)) {
            throw 'Docker Desktop/Engine with Docker Compose is required and must be ready.'
        }

        $config = Initialize-LocalConfig
        Invoke-MadarCompose -Values $config -Arguments @('up', '--build', '-d')

        try {
            Wait-MadarReady
        }
        catch {
            Invoke-MadarCompose -Values $config -Arguments @('logs', '--no-color', '--tail=250')
            throw
        }

        Show-Status
        Write-Host ''
        Write-Host 'Local development accounts:' -ForegroundColor Cyan
        Write-Host ("  Administrator: {0}" -f $config['MADAR_ADMIN_EMAIL'])
        Write-Host ("  Operator:      {0}" -f $config['MADAR_OPERATOR_EMAIL'])
        Write-Host ("  SLA enabled:   {0}" -f $config['MADAR_SLA_ENABLED'])
        Write-Host "Settings are stored in $ConfigPath with local-only ACLs on Windows."
    }

    'status' {
        Show-Status
    }

    'logs' {
        if (-not (Test-DockerReady)) {
            throw 'Docker Desktop/Engine with Docker Compose is required to read Madar logs.'
        }

        $config = Get-ComposeEnvironment
        Invoke-MadarCompose -Values $config -Arguments @('logs', '--no-color', '--tail=250')
    }

    'stop' {
        if (-not (Test-DockerReady)) {
            throw 'Docker Desktop/Engine with Docker Compose is required to stop Madar.'
        }

        $config = Get-ComposeEnvironment
        Invoke-MadarCompose -Values $config -Arguments @('down', '--remove-orphans')
        Write-Host 'Madar stopped. SQL data was preserved.' -ForegroundColor Green
    }
}
