[CmdletBinding()]
param(
    [Parameter(Mandatory = $true, Position = 0)]
    [ValidateSet('start', 'status', 'logs', 'stop', 'credentials', 'publish')]
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
$MadarProject = Join-Path $RepoRoot 'apps\Madar\Madar.Api\Madar.Api.csproj'
$PublishRoot = Join-Path $RepoRoot 'artifacts\madar\publish'
$PublishZip = Join-Path $RepoRoot 'artifacts\madar\Madar-net10.0-Release.zip'
$PublishHash = "$PublishZip.sha256"

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

function Show-Credentials {
    $config = Get-LocalConfig
    if ($null -eq $config) {
        throw "Madar local credentials do not exist yet. Run 'start' once to create the local development configuration."
    }

    Protect-LocalFile -Path $ConfigPath
    Write-Host 'Madar local development credentials' -ForegroundColor Cyan
    Write-Host ("  Administrator email:    {0}" -f $config['MADAR_ADMIN_EMAIL'])
    Write-Host ("  Administrator password: {0}" -f $config['MADAR_ADMIN_PASSWORD'])
    Write-Host ("  Operator email:         {0}" -f $config['MADAR_OPERATOR_EMAIL'])
    Write-Host ("  Operator password:      {0}" -f $config['MADAR_OPERATOR_PASSWORD'])
    Write-Host ''
    Write-Host "Stored locally at $ConfigPath. Do not commit or share these development credentials." -ForegroundColor Yellow
}

function Publish-Madar {
    if (-not (Get-Command dotnet -ErrorAction SilentlyContinue)) {
        throw '.NET 10 SDK is required to publish Madar.'
    }

    if (-not (Test-Path $MadarProject)) {
        throw "Madar project was not found: $MadarProject"
    }

    $sdkVersion = (& dotnet --version).Trim()
    if ($sdkVersion -notmatch '^10\.') {
        throw "Madar publish requires the .NET 10 SDK selected by global.json. Active SDK: $sdkVersion"
    }

    $artifactRoot = Split-Path -Parent $PublishRoot
    New-Item -ItemType Directory -Path $artifactRoot -Force | Out-Null
    Remove-Item -LiteralPath $PublishRoot -Recurse -Force -ErrorAction SilentlyContinue
    Remove-Item -LiteralPath $PublishZip -Force -ErrorAction SilentlyContinue
    Remove-Item -LiteralPath $PublishHash -Force -ErrorAction SilentlyContinue

    & dotnet publish $MadarProject `
        --configuration Release `
        --framework net10.0 `
        --no-self-contained `
        --output $PublishRoot
    if ($LASTEXITCODE -ne 0) {
        throw "Madar Release publish failed with exit code $LASTEXITCODE."
    }

    Compress-Archive -Path (Join-Path $PublishRoot '*') -DestinationPath $PublishZip -CompressionLevel Optimal
    $hash = (Get-FileHash -LiteralPath $PublishZip -Algorithm SHA256).Hash.ToLowerInvariant()
    [System.IO.File]::WriteAllText(
        $PublishHash,
        "$hash  $([System.IO.Path]::GetFileName($PublishZip))`n",
        [System.Text.UTF8Encoding]::new($false))

    Write-Host 'Madar Release publish completed.' -ForegroundColor Green
    Write-Host "  Folder: $PublishRoot"
    Write-Host "  ZIP:    $PublishZip"
    Write-Host "  SHA256: $PublishHash"
    Write-Host 'The package contains no production database credentials or deployment-specific secrets.' -ForegroundColor Yellow
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
        Write-Host "Run '.\foundationkit.ps1 credentials -Target Madar' to display the generated passwords."
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

    'credentials' {
        Show-Credentials
    }

    'publish' {
        Publish-Madar
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
