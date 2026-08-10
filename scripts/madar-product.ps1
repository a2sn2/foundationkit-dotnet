[CmdletBinding()]
param(
    [Parameter(Mandatory = $true, Position = 0)]
    [ValidateSet(
        'start',
        'status',
        'logs',
        'stop',
        'open',
        'credentials',
        'publish',
        'share-microsoft',
        'share-cloudflare')]
    [string]$Action,

    [ValidateSet('Auto', 'Native', 'Docker')]
    [string]$Mode = 'Auto',

    [switch]$Reset,

    [string]$BaseUrl = 'http://localhost:8100',

    [string]$NativeConnectionString = 'Server=.;Database=MadarDb;Trusted_Connection=True;TrustServerCertificate=True;MultipleActiveResultSets=True'
)

$ErrorActionPreference = 'Stop'
Set-StrictMode -Version Latest

$RepoRoot = Split-Path -Parent $PSScriptRoot
$ComposeFile = Join-Path $RepoRoot 'deploy\madar-compose.yml'
$ComposeProject = 'madar-product'
$LocalRoot = Join-Path $RepoRoot '.local'
$ConfigPath = Join-Path $LocalRoot 'madar-product.env'
$ModePath = Join-Path $LocalRoot 'madar-product.mode'
$NativeRoot = Join-Path $LocalRoot 'madar-native'
$NativePublishRoot = Join-Path $NativeRoot 'app'
$NativeAttachmentsRoot = Join-Path $NativeRoot 'attachments'
$NativePidPath = Join-Path $LocalRoot 'madar-native.pid'
$NativeOutputLog = Join-Path $LocalRoot 'logs\madar-native.out.log'
$NativeErrorLog = Join-Path $LocalRoot 'logs\madar-native.err.log'
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

    if (-not (Test-Path $Path)) {
        return
    }

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
        '# Bootstrap passwords are generated for local UAT only.'
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

function Assert-DotNet10 {
    if (-not (Get-Command dotnet -ErrorAction SilentlyContinue)) {
        throw '.NET 10 SDK is required for Madar Native mode.'
    }

    $sdkVersion = (& dotnet --version).Trim()
    if ($sdkVersion -notmatch '^10\.') {
        throw "Madar requires the .NET 10 SDK selected by global.json. Active SDK: $sdkVersion"
    }
}

function Save-Mode {
    param([Parameter(Mandatory = $true)][ValidateSet('Native', 'Docker')][string]$ExecutionMode)

    New-Item -ItemType Directory -Path $LocalRoot -Force | Out-Null
    [System.IO.File]::WriteAllText($ModePath, $ExecutionMode, [System.Text.Encoding]::ASCII)
}

function Get-StoredMode {
    if (-not (Test-Path $ModePath)) {
        return $null
    }

    $stored = (Get-Content -LiteralPath $ModePath -Raw).Trim()
    if ($stored -in @('Native', 'Docker')) {
        return $stored
    }

    return $null
}

function Resolve-MadarMode {
    param([switch]$PreferStored)

    if ($Mode -eq 'Native') {
        if ($env:OS -ne 'Windows_NT') {
            throw 'Madar Native UAT mode currently requires Windows and a local SQL Server instance.'
        }
        Assert-DotNet10
        return 'Native'
    }

    if ($Mode -eq 'Docker') {
        if (-not (Test-DockerReady)) {
            throw 'Madar Docker mode was requested, but Docker Desktop/Engine is not ready.'
        }
        return 'Docker'
    }

    if ($PreferStored) {
        $stored = Get-StoredMode
        if ($stored -eq 'Native') {
            if ($env:OS -eq 'Windows_NT' -and (Get-Command dotnet -ErrorAction SilentlyContinue)) {
                Assert-DotNet10
                return 'Native'
            }
        }
        elseif ($stored -eq 'Docker' -and (Test-DockerReady)) {
            return 'Docker'
        }
    }

    if ($env:OS -eq 'Windows_NT' -and (Get-Command dotnet -ErrorAction SilentlyContinue)) {
        Assert-DotNet10
        return 'Native'
    }

    if (Test-DockerReady) {
        return 'Docker'
    }

    throw 'No supported Madar runtime is available. On Windows install .NET 10 and SQL Server for Native mode, or use Docker mode.'
}

function Invoke-MadarCompose {
    param(
        [Parameter(Mandatory = $true)][System.Collections.IDictionary]$Values,
        [Parameter(Mandatory = $true)][string[]]$Arguments
    )

    if (-not (Test-Path $ComposeFile)) {
        throw "Madar compose file not found: $ComposeFile"
    }

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

function Get-NativeProcess {
    if (-not (Test-Path $NativePidPath)) {
        return $null
    }

    $pidText = (Get-Content -LiteralPath $NativePidPath -Raw).Trim()
    $processId = 0
    if (-not [int]::TryParse($pidText, [ref]$processId)) {
        Remove-Item -LiteralPath $NativePidPath -Force -ErrorAction SilentlyContinue
        return $null
    }

    try {
        return Get-Process -Id $processId -ErrorAction Stop
    }
    catch {
        Remove-Item -LiteralPath $NativePidPath -Force -ErrorAction SilentlyContinue
        return $null
    }
}

function Show-NativeLogs {
    $found = $false
    foreach ($path in @($NativeOutputLog, $NativeErrorLog)) {
        if (Test-Path $path) {
            $found = $true
            Write-Host "--- $path ---" -ForegroundColor Yellow
            Get-Content -LiteralPath $path -Tail 250
        }
    }

    if (-not $found) {
        Write-Host 'No Madar Native logs were found.' -ForegroundColor Yellow
    }
}

function Publish-NativeRuntime {
    Assert-DotNet10

    if (-not (Test-Path $MadarProject)) {
        throw "Madar project was not found: $MadarProject"
    }

    New-Item -ItemType Directory -Path $NativeRoot -Force | Out-Null
    Remove-Item -LiteralPath $NativePublishRoot -Recurse -Force -ErrorAction SilentlyContinue

    & dotnet publish $MadarProject `
        --configuration Release `
        --framework net10.0 `
        --no-self-contained `
        --output $NativePublishRoot `
        --nologo
    if ($LASTEXITCODE -ne 0) {
        throw "Madar Native publish failed with exit code $LASTEXITCODE."
    }
}

function Get-NativeEnvironment {
    param([Parameter(Mandatory = $true)][System.Collections.IDictionary]$Config)

    New-Item -ItemType Directory -Path $NativeAttachmentsRoot -Force | Out-Null

    return @{
        ASPNETCORE_ENVIRONMENT = 'Development'
        DOTNET_ENVIRONMENT = 'Development'
        ASPNETCORE_URLS = $BaseUrl
        ConnectionStrings__Madar = $NativeConnectionString
        Madar__Bootstrap__Enabled = 'true'
        Madar__Bootstrap__AdministratorEmail = $Config['MADAR_ADMIN_EMAIL']
        Madar__Bootstrap__AdministratorPassword = $Config['MADAR_ADMIN_PASSWORD']
        Madar__Bootstrap__AdministratorDisplayName = 'Madar Administrator'
        Madar__Bootstrap__OperatorEmail = $Config['MADAR_OPERATOR_EMAIL']
        Madar__Bootstrap__OperatorPassword = $Config['MADAR_OPERATOR_PASSWORD']
        Madar__Bootstrap__OperatorDisplayName = 'Madar Operator'
        Madar__DatabaseStartup__MigrationAttempts = '30'
        Madar__DatabaseStartup__DelaySeconds = '2'
        Madar__Sla__Enabled = $Config['MADAR_SLA_ENABLED']
        Madar__Sla__Low = $Config['MADAR_SLA_LOW']
        Madar__Sla__Medium = $Config['MADAR_SLA_MEDIUM']
        Madar__Sla__High = $Config['MADAR_SLA_HIGH']
        Madar__Sla__Critical = $Config['MADAR_SLA_CRITICAL']
        Madar__Attachments__StorageRoot = $NativeAttachmentsRoot
    }
}

function Start-Native {
    if ($env:OS -ne 'Windows_NT') {
        throw 'Madar Native UAT mode currently requires Windows and a local SQL Server instance.'
    }

    Assert-DotNet10

    $existing = Get-NativeProcess
    if ($null -ne $existing) {
        Save-Mode -ExecutionMode 'Native'
        Write-Host "Madar Native is already running with PID $($existing.Id)." -ForegroundColor Yellow
        Wait-MadarReady -Attempts 10
        Show-Status
        return
    }

    $config = Initialize-LocalConfig
    Publish-NativeRuntime

    New-Item -ItemType Directory -Path (Split-Path -Parent $NativeOutputLog) -Force | Out-Null
    Remove-Item -LiteralPath $NativeOutputLog -Force -ErrorAction SilentlyContinue
    Remove-Item -LiteralPath $NativeErrorLog -Force -ErrorAction SilentlyContinue

    $applicationDll = Join-Path $NativePublishRoot 'Madar.Api.dll'
    if (-not (Test-Path $applicationDll)) {
        throw "Published Madar runtime was not found: $applicationDll"
    }

    $environment = Get-NativeEnvironment -Config $config
    $process = Invoke-WithMadarEnvironment -Values $environment -Script {
        Start-Process `
            -FilePath 'dotnet' `
            -ArgumentList @('Madar.Api.dll') `
            -WorkingDirectory $NativePublishRoot `
            -RedirectStandardOutput $NativeOutputLog `
            -RedirectStandardError $NativeErrorLog `
            -PassThru
    }

    if ($null -eq $process) {
        throw 'Madar Native process could not be started.'
    }

    [System.IO.File]::WriteAllText($NativePidPath, $process.Id.ToString(), [System.Text.Encoding]::ASCII)
    Save-Mode -ExecutionMode 'Native'

    try {
        Wait-MadarReady
    }
    catch {
        Show-NativeLogs
        Stop-Process -Id $process.Id -Force -ErrorAction SilentlyContinue
        Remove-Item -LiteralPath $NativePidPath -Force -ErrorAction SilentlyContinue
        throw
    }

    Show-Status
    Write-Host ''
    Write-Host 'Madar Native UAT runtime is ready.' -ForegroundColor Green
    Write-Host "  URL: $BaseUrl"
    Write-Host "  SQL: $NativeConnectionString"
    Write-Host "  PID: $($process.Id)"
    Write-Host "  Settings: $ConfigPath"
    Write-Host "Run the credentials action to display the generated Development accounts." -ForegroundColor Yellow
}

function Stop-Native {
    $process = Get-NativeProcess
    if ($null -eq $process) {
        Write-Host 'Madar Native process is not running.' -ForegroundColor Yellow
        Remove-Item -LiteralPath $NativePidPath -Force -ErrorAction SilentlyContinue
        return
    }

    Stop-Process -Id $process.Id -Force
    try {
        Wait-Process -Id $process.Id -Timeout 15 -ErrorAction SilentlyContinue
    }
    catch {
    }

    Remove-Item -LiteralPath $NativePidPath -Force -ErrorAction SilentlyContinue
    Write-Host 'Madar Native process stopped. Local SQL data was preserved.' -ForegroundColor Green
}

function Start-Docker {
    if (-not (Test-DockerReady)) {
        throw 'Docker Desktop/Engine with Docker Compose is required and must be ready.'
    }

    $config = Initialize-LocalConfig
    Invoke-MadarCompose -Values $config -Arguments @('up', '--build', '-d')
    Save-Mode -ExecutionMode 'Docker'

    try {
        Wait-MadarReady
    }
    catch {
        Invoke-MadarCompose -Values $config -Arguments @('logs', '--no-color', '--tail=250')
        throw
    }

    Show-Status
}

function Stop-Docker {
    if (-not (Test-DockerReady)) {
        Write-Host 'Docker is unavailable; Madar Docker resources were not changed.' -ForegroundColor Yellow
        return
    }

    $config = Get-ComposeEnvironment
    Invoke-MadarCompose -Values $config -Arguments @('down', '--remove-orphans')
    Write-Host 'Madar Docker stack stopped. SQL data was preserved.' -ForegroundColor Green
}

function Show-Status {
    $stored = Get-StoredMode
    if ($null -ne $stored) {
        Write-Host "Madar mode: $stored" -ForegroundColor Cyan
    }

    $live = Get-Health -Path '/health/live'
    $ready = Get-Health -Path '/health/ready'

    if ($null -eq $live) {
        $nativeProcess = Get-NativeProcess
        if ($null -ne $nativeProcess) {
            Write-Host "Madar: PROCESS RUNNING but health is unreachable (PID $($nativeProcess.Id))." -ForegroundColor Yellow
            Write-Host "URL: $BaseUrl"
            return
        }

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
        throw "Madar local credentials do not exist yet. Run 'start' once to create the local Development configuration."
    }

    Protect-LocalFile -Path $ConfigPath
    Write-Host 'Madar local Development credentials' -ForegroundColor Cyan
    Write-Host ("  Administrator email:    {0}" -f $config['MADAR_ADMIN_EMAIL'])
    Write-Host ("  Administrator password: {0}" -f $config['MADAR_ADMIN_PASSWORD'])
    Write-Host ("  Operator email:         {0}" -f $config['MADAR_OPERATOR_EMAIL'])
    Write-Host ("  Operator password:      {0}" -f $config['MADAR_OPERATOR_PASSWORD'])
    Write-Host ''
    Write-Host "Stored locally at $ConfigPath. Do not commit or share these Development credentials." -ForegroundColor Yellow
    Write-Host 'Bootstrap is idempotent and does not overwrite passwords for users that already existed before this local config was created.' -ForegroundColor Yellow
}

function Publish-Madar {
    Assert-DotNet10

    if (-not (Test-Path $MadarProject)) {
        throw "Madar project was not found: $MadarProject"
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
        --output $PublishRoot `
        --nologo
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

function Assert-MadarReadyForSharing {
    $ready = Get-Health -Path '/health/ready'
    if ($null -eq $ready -or $ready.status -ne 'ready') {
        throw "Madar must be READY at $BaseUrl before creating a temporary UAT tunnel."
    }
}

function Get-BasePort {
    try {
        $uri = [Uri]$BaseUrl
        return $uri.Port
    }
    catch {
        throw "BaseUrl is invalid: $BaseUrl"
    }
}

function Share-Microsoft {
    Assert-MadarReadyForSharing
    if (-not (Get-Command devtunnel -ErrorAction SilentlyContinue)) {
        throw "Microsoft Dev Tunnels CLI is not installed. Install 'Microsoft.devtunnel' and log in before sharing."
    }

    $port = Get-BasePort
    Write-Host 'Starting temporary Microsoft Dev Tunnel for Madar UAT.' -ForegroundColor Cyan
    Write-Host 'This endpoint is intentionally anonymous while this command is running. Stop it with Ctrl+C.' -ForegroundColor Yellow
    & devtunnel host -p $port --allow-anonymous
    if ($LASTEXITCODE -ne 0) {
        throw "devtunnel exited with code $LASTEXITCODE."
    }
}

function Share-Cloudflare {
    Assert-MadarReadyForSharing
    if (-not (Get-Command cloudflared -ErrorAction SilentlyContinue)) {
        throw 'cloudflared is not installed or is not available in PATH.'
    }

    Write-Host 'Starting temporary Cloudflare Quick Tunnel for Madar UAT.' -ForegroundColor Cyan
    Write-Host 'The generated trycloudflare.com URL is temporary and is not Production hosting. Stop it with Ctrl+C.' -ForegroundColor Yellow
    & cloudflared tunnel --url $BaseUrl
    if ($LASTEXITCODE -ne 0) {
        throw "cloudflared exited with code $LASTEXITCODE."
    }
}

switch ($Action) {
    'start' {
        $executionMode = Resolve-MadarMode
        if ($executionMode -eq 'Native') {
            Start-Native
        }
        else {
            Start-Docker
        }
    }

    'status' {
        Show-Status
    }

    'logs' {
        $executionMode = Resolve-MadarMode -PreferStored
        if ($executionMode -eq 'Native') {
            Show-NativeLogs
        }
        else {
            if (-not (Test-DockerReady)) {
                throw 'Docker Desktop/Engine with Docker Compose is required to read Madar Docker logs.'
            }
            $config = Get-ComposeEnvironment
            Invoke-MadarCompose -Values $config -Arguments @('logs', '--no-color', '--tail=250')
        }
    }

    'open' {
        Start-Process $BaseUrl
    }

    'credentials' {
        Show-Credentials
    }

    'publish' {
        Publish-Madar
    }

    'share-microsoft' {
        Share-Microsoft
    }

    'share-cloudflare' {
        Share-Cloudflare
    }

    'stop' {
        $executionMode = Resolve-MadarMode -PreferStored
        if ($executionMode -eq 'Native') {
            Stop-Native
        }
        else {
            Stop-Docker
        }
    }
}
