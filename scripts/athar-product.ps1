#requires -Version 5.1

[CmdletBinding()]
param(
    [ValidateSet("Start", "Stop", "Status", "Open", "Lan", "Backup", "Reset")]
    [string]$Action = "Start",

    [ValidateSet("Auto", "Docker", "Native")]
    [string]$Mode = "Auto",

    [switch]$Force
)

$ErrorActionPreference = "Stop"
Set-StrictMode -Version Latest

$RepositoryRoot = Split-Path -Parent $PSScriptRoot
$ComposeFile = Join-Path $RepositoryRoot "deploy/athar-compose.yml"
$NativeProject = Join-Path $RepositoryRoot "examples/Athar/Athar.Api/Athar.Api.csproj"
$LocalDirectory = Join-Path $RepositoryRoot ".local"
$EnvironmentFile = Join-Path $LocalDirectory "athar-product.env"
$BackupDirectory = Join-Path $LocalDirectory "backups"
$LogDirectory = Join-Path $LocalDirectory "logs"
$NativeAppDirectory = Join-Path $LocalDirectory "athar-native/app"
$NativePidFile = Join-Path $LocalDirectory "athar-native.pid"
$ModeFile = Join-Path $LocalDirectory "athar-product.mode"
$NativeOutputLog = Join-Path $LogDirectory "athar-native.out.log"
$NativeErrorLog = Join-Path $LogDirectory "athar-native.err.log"
$ProjectName = "athar-product"
$BaseUrl = "http://localhost:8090"
$ListenUrl = "http://0.0.0.0:8090"
$DefaultNativeConnectionString = "Server=.;Database=Athar;Trusted_Connection=True;TrustServerCertificate=True;MultipleActiveResultSets=True"

function Test-Command {
    param([Parameter(Mandatory)][string]$Name)

    return $null -ne (Get-Command $Name -ErrorAction SilentlyContinue)
}

function Assert-Command {
    param([Parameter(Mandatory)][string]$Name)

    if (-not (Test-Command $Name)) {
        throw "Required command '$Name' is not installed or is not available in PATH."
    }
}

function Test-DockerReady {
    if (-not (Test-Command "docker")) {
        return $false
    }

    $previousErrorActionPreference = $ErrorActionPreference
    try {
        $ErrorActionPreference = "SilentlyContinue"

        & docker info *> $null
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

function Assert-Docker {
    if (-not (Test-DockerReady)) {
        throw "Docker Desktop is not ready. Start Docker Desktop or run with -Mode Native."
    }
}

function Assert-NativeRequirements {
    Assert-Command "dotnet"

    & dotnet --version *> $null
    if ($LASTEXITCODE -ne 0) {
        throw ".NET SDK is not available. Install .NET 8 SDK and try again."
    }
}

function New-StrongPassword {
    param([string]$Prefix)

    return $Prefix + [Guid]::NewGuid().ToString("N") + "Aa1!"
}

function Protect-LocalFile {
    param([Parameter(Mandatory)][string]$Path)

    if (-not (Test-Path $Path)) {
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
        throw "Could not restrict ACLs on local secret file '$Path'. Refusing to continue with unprotected local credentials. $($_.Exception.Message)"
    }
}

function Initialize-EnvironmentFile {
    New-Item -ItemType Directory -Force -Path $LocalDirectory | Out-Null

    if (Test-Path $EnvironmentFile) {
        Protect-LocalFile $EnvironmentFile
        return
    }

    $sqlPassword = New-StrongPassword "AtharSql!"
    $adminPassword = New-StrongPassword "AtharAdmin!"
    $lines = @(
        "ATHAR_SQL_PASSWORD=$sqlPassword"
        "ATHAR_ADMIN_EMAIL=admin@athar.local"
        "ATHAR_ADMIN_PASSWORD=$adminPassword"
        "ATHAR_NATIVE_CONNECTION_STRING=$DefaultNativeConnectionString"
    )

    [System.IO.File]::WriteAllLines(
        $EnvironmentFile,
        $lines,
        [System.Text.UTF8Encoding]::new($false))
    Protect-LocalFile $EnvironmentFile

    Write-Host "Created protected local settings at .local/athar-product.env" -ForegroundColor Green
    Write-Host "This file is ignored by Git and restricted to the current Windows account." -ForegroundColor DarkYellow
}

function Get-EnvironmentValues {
    Initialize-EnvironmentFile
    $values = @{}

    foreach ($line in Get-Content $EnvironmentFile) {
        if ([string]::IsNullOrWhiteSpace($line) -or $line.TrimStart().StartsWith("#")) {
            continue
        }

        $parts = $line -split "=", 2
        if ($parts.Count -eq 2) {
            $values[$parts[0].Trim()] = $parts[1]
        }
    }

    return $values
}

function Save-ExecutionMode {
    param([Parameter(Mandatory)][ValidateSet("Docker", "Native")][string]$ExecutionMode)

    New-Item -ItemType Directory -Force -Path $LocalDirectory | Out-Null
    [System.IO.File]::WriteAllText(
        $ModeFile,
        $ExecutionMode,
        [System.Text.Encoding]::ASCII)
}

function Get-StoredExecutionMode {
    if (-not (Test-Path $ModeFile)) {
        return $null
    }

    $stored = (Get-Content $ModeFile -Raw).Trim()
    if ($stored -in @("Docker", "Native")) {
        return $stored
    }

    return $null
}

function Resolve-ExecutionMode {
    param(
        [Parameter(Mandatory)][string]$RequestedMode,
        [switch]$PreferStored
    )

    if ($RequestedMode -eq "Docker") {
        Assert-Docker
        return "Docker"
    }

    if ($RequestedMode -eq "Native") {
        Assert-NativeRequirements
        return "Native"
    }

    if ($PreferStored) {
        $stored = Get-StoredExecutionMode
        if ($stored -eq "Docker" -and (Test-DockerReady)) {
            return "Docker"
        }
        if ($stored -eq "Native") {
            Assert-NativeRequirements
            return "Native"
        }
    }

    if (Test-DockerReady) {
        return "Docker"
    }

    Assert-NativeRequirements
    return "Native"
}

function Invoke-Compose {
    param(
        [Parameter(Mandatory)]
        [string[]]$ComposeArguments
    )

    & docker compose `
        --project-name $ProjectName `
        --env-file $EnvironmentFile `
        -f $ComposeFile `
        @ComposeArguments

    if ($LASTEXITCODE -ne 0) {
        throw "Docker Compose failed. Review the messages above."
    }
}

function Get-NativeProcess {
    if (-not (Test-Path $NativePidFile)) {
        return $null
    }

    $pidText = (Get-Content $NativePidFile -Raw).Trim()
    $processId = 0
    if (-not [int]::TryParse($pidText, [ref]$processId)) {
        Remove-Item $NativePidFile -Force -ErrorAction SilentlyContinue
        return $null
    }

    try {
        return Get-Process -Id $processId -ErrorAction Stop
    }
    catch {
        Remove-Item $NativePidFile -Force -ErrorAction SilentlyContinue
        return $null
    }
}

function Publish-NativeProduct {
    Assert-NativeRequirements

    New-Item -ItemType Directory -Force -Path $NativeAppDirectory | Out-Null
    Write-Host "Publishing Athar for local Windows execution..." -ForegroundColor Cyan

    & dotnet publish `
        $NativeProject `
        --configuration Release `
        --output $NativeAppDirectory `
        --nologo

    if ($LASTEXITCODE -ne 0) {
        throw "dotnet publish failed. Review the messages above."
    }
}

function Show-NativeLogs {
    if (Test-Path $NativeOutputLog) {
        Write-Host "--- Native output log ---" -ForegroundColor Yellow
        Get-Content $NativeOutputLog -Tail 120
    }

    if (Test-Path $NativeErrorLog) {
        Write-Host "--- Native error log ---" -ForegroundColor Yellow
        Get-Content $NativeErrorLog -Tail 120
    }
}

function Wait-UntilReady {
    param([int]$Attempts = 120)

    Write-Host "Waiting for the API and SQL Server..." -ForegroundColor Cyan

    for ($attempt = 1; $attempt -le $Attempts; $attempt++) {
        try {
            $response = Invoke-RestMethod -Uri "$BaseUrl/health/ready" -TimeoutSec 3
            if ($null -ne $response) {
                Write-Host "Athar is ready." -ForegroundColor Green
                return
            }
        }
        catch {
            Start-Sleep -Seconds 2
        }
    }

    throw "Athar did not become ready before the timeout."
}

function Start-NativeProduct {
    Assert-NativeRequirements
    Initialize-EnvironmentFile

    $existing = Get-NativeProcess
    if ($null -ne $existing) {
        Write-Host "Athar is already running in Native mode with PID $($existing.Id)." -ForegroundColor Yellow
        Save-ExecutionMode "Native"
        Wait-UntilReady -Attempts 10
        return
    }

    Publish-NativeProduct

    $values = Get-EnvironmentValues
    $connectionString = $DefaultNativeConnectionString
    if ($values.ContainsKey("ATHAR_NATIVE_CONNECTION_STRING") -and
        -not [string]::IsNullOrWhiteSpace($values["ATHAR_NATIVE_CONNECTION_STRING"])) {
        $connectionString = $values["ATHAR_NATIVE_CONNECTION_STRING"]
    }

    New-Item -ItemType Directory -Force -Path $LogDirectory | Out-Null
    Remove-Item $NativeOutputLog -Force -ErrorAction SilentlyContinue
    Remove-Item $NativeErrorLog -Force -ErrorAction SilentlyContinue

    $environmentNames = @(
        "ASPNETCORE_ENVIRONMENT",
        "DOTNET_ENVIRONMENT",
        "ASPNETCORE_URLS",
        "ConnectionStrings__Athar",
        "AdminSeed__Enabled",
        "AdminSeed__Email",
        "AdminSeed__Password",
        "DatabaseStartup__ApplyMigrationsOnStartup",
        "DatabaseStartup__SeedRolesOnStartup",
        "DatabaseStartup__MigrationAttempts",
        "DatabaseStartup__DelaySeconds")

    $oldEnvironment = @{}
    foreach ($name in $environmentNames) {
        $oldEnvironment[$name] = [Environment]::GetEnvironmentVariable($name, "Process")
    }

    try {
        [Environment]::SetEnvironmentVariable("ASPNETCORE_ENVIRONMENT", "Development", "Process")
        [Environment]::SetEnvironmentVariable("DOTNET_ENVIRONMENT", "Development", "Process")
        [Environment]::SetEnvironmentVariable("ASPNETCORE_URLS", $ListenUrl, "Process")
        [Environment]::SetEnvironmentVariable("ConnectionStrings__Athar", $connectionString, "Process")
        [Environment]::SetEnvironmentVariable("AdminSeed__Enabled", "true", "Process")
        [Environment]::SetEnvironmentVariable("AdminSeed__Email", $values["ATHAR_ADMIN_EMAIL"], "Process")
        [Environment]::SetEnvironmentVariable("AdminSeed__Password", $values["ATHAR_ADMIN_PASSWORD"], "Process")
        [Environment]::SetEnvironmentVariable("DatabaseStartup__ApplyMigrationsOnStartup", "true", "Process")
        [Environment]::SetEnvironmentVariable("DatabaseStartup__SeedRolesOnStartup", "true", "Process")
        [Environment]::SetEnvironmentVariable("DatabaseStartup__MigrationAttempts", "30", "Process")
        [Environment]::SetEnvironmentVariable("DatabaseStartup__DelaySeconds", "2", "Process")

        $executable = Join-Path $NativeAppDirectory "Athar.Api.exe"
        $applicationDll = Join-Path $NativeAppDirectory "Athar.Api.dll"

        if (Test-Path $executable) {
            $process = Start-Process `
                -FilePath $executable `
                -WorkingDirectory $NativeAppDirectory `
                -RedirectStandardOutput $NativeOutputLog `
                -RedirectStandardError $NativeErrorLog `
                -PassThru
        }
        elseif (Test-Path $applicationDll) {
            $process = Start-Process `
                -FilePath "dotnet" `
                -ArgumentList @($applicationDll) `
                -WorkingDirectory $NativeAppDirectory `
                -RedirectStandardOutput $NativeOutputLog `
                -RedirectStandardError $NativeErrorLog `
                -PassThru
        }
        else {
            throw "Published Athar executable was not found."
        }
    }
    finally {
        foreach ($name in $environmentNames) {
            [Environment]::SetEnvironmentVariable($name, $oldEnvironment[$name], "Process")
        }
    }

    [System.IO.File]::WriteAllText(
        $NativePidFile,
        $process.Id.ToString(),
        [System.Text.Encoding]::ASCII)
    Save-ExecutionMode "Native"

    try {
        Wait-UntilReady
    }
    catch {
        Show-NativeLogs
        throw
    }
}

function Stop-NativeProduct {
    $process = Get-NativeProcess
    if ($null -eq $process) {
        Write-Host "Athar Native process is not running." -ForegroundColor Yellow
        return
    }

    Stop-Process -Id $process.Id -Force
    try {
        Wait-Process -Id $process.Id -Timeout 15 -ErrorAction SilentlyContinue
    }
    catch {
    }

    Remove-Item $NativePidFile -Force -ErrorAction SilentlyContinue
    Write-Host "Athar Native process stopped." -ForegroundColor Green
}

function Show-NativeStatus {
    $process = Get-NativeProcess
    if ($null -eq $process) {
        Write-Host "Athar Native process is not running." -ForegroundColor Yellow
        return
    }

    Write-Host "Athar Native process is running with PID $($process.Id)." -ForegroundColor Green
    try {
        Invoke-RestMethod -Uri "$BaseUrl/health/ready" -TimeoutSec 3 | ConvertTo-Json -Depth 5
    }
    catch {
        Write-Host "The process exists, but the readiness endpoint is not available." -ForegroundColor Yellow
        Show-NativeLogs
    }
}

function Show-AccessInformation {
    $values = Get-EnvironmentValues

    Write-Host ""
    Write-Host "Athar experimental product" -ForegroundColor Green
    Write-Host "  Home:          $BaseUrl"
    Write-Host "  Account:       $BaseUrl/account"
    Write-Host "  Initiatives:   $BaseUrl/initiatives"
    Write-Host "  Admin:         $BaseUrl/admin"
    Write-Host "  Swagger:       $BaseUrl/swagger (Development only)"
    Write-Host "  Readiness:     $BaseUrl/health/ready"
    Write-Host ""
    Write-Host "Local administrator account" -ForegroundColor Cyan
    Write-Host "  Email:         $($values['ATHAR_ADMIN_EMAIL'])"
    Write-Host "  Credential file: .local/athar-product.env (ACL restricted; never share or commit it)"
    Write-Host ""
}

function Show-LanUrls {
    $addresses = Get-NetIPAddress -AddressFamily IPv4 -ErrorAction SilentlyContinue |
        Where-Object {
            $_.IPAddress -notlike "127.*" -and
            $_.IPAddress -notlike "169.254.*" -and
            $_.AddressState -eq "Preferred"
        } |
        Select-Object -ExpandProperty IPAddress -Unique

    if (-not $addresses) {
        Write-Host "No suitable IPv4 address was found. Run ipconfig and check the active adapter." -ForegroundColor Yellow
        return
    }

    Write-Host "WARNING: Workbench/Athar LAN exposure is for controlled development only. Do not use real or sensitive data." -ForegroundColor Yellow
    Write-Host "Possible URLs for devices on the same Wi-Fi or LAN:" -ForegroundColor Cyan
    foreach ($address in $addresses) {
        Write-Host "  http://${address}:8090"
    }

    Write-Host ""
    Write-Host "If another device cannot connect, allow TCP port 8090 through Windows Firewall only for the trusted local network and remove the rule after testing." -ForegroundColor Yellow
}

function Backup-DockerDatabase {
    Assert-Docker
    Initialize-EnvironmentFile
    New-Item -ItemType Directory -Force -Path $BackupDirectory | Out-Null

    $stamp = Get-Date -Format "yyyyMMdd-HHmmss"
    $fileName = "AtharDb-$stamp.bak"
    $containerPath = "/var/opt/mssql/backup/$fileName"

    $backupCommand = @'
set -e
mkdir -p /var/opt/mssql/backup
if [ -x /opt/mssql-tools18/bin/sqlcmd ]; then
  SQLCMD=/opt/mssql-tools18/bin/sqlcmd
else
  SQLCMD=/opt/mssql-tools/bin/sqlcmd
fi
"$SQLCMD" -S localhost -U sa -P "$MSSQL_SA_PASSWORD" -C -Q "BACKUP DATABASE [AtharDb] TO DISK = N'__BACKUP_PATH__' WITH INIT, CHECKSUM"
'@.Replace("__BACKUP_PATH__", $containerPath)

    Invoke-Compose -ComposeArguments @("exec", "-T", "athar-sqlserver", "bash", "-lc", $backupCommand)
    $localBackupFile = Join-Path $BackupDirectory $fileName
    Invoke-Compose -ComposeArguments @("cp", "athar-sqlserver:$containerPath", $localBackupFile)
    Protect-LocalFile $localBackupFile

    Write-Host "Development database backup created with restricted ACL:" -ForegroundColor Green
    Write-Host "  $localBackupFile"
    Write-Host "This local backup is not production recovery evidence until an isolated restore test passes." -ForegroundColor Yellow
}

function Backup-NativeDatabase {
    Assert-Command "sqlcmd"
    Initialize-EnvironmentFile
    New-Item -ItemType Directory -Force -Path $BackupDirectory | Out-Null

    $values = Get-EnvironmentValues
    $connectionString = $DefaultNativeConnectionString
    if ($values.ContainsKey("ATHAR_NATIVE_CONNECTION_STRING") -and
        -not [string]::IsNullOrWhiteSpace($values["ATHAR_NATIVE_CONNECTION_STRING"])) {
        $connectionString = $values["ATHAR_NATIVE_CONNECTION_STRING"]
    }

    $server = "."
    $database = "Athar"
    if ($connectionString -match "(?i)(?:Server|Data Source)\s*=\s*([^;]+)") {
        $server = $Matches[1].Trim()
    }
    if ($connectionString -match "(?i)(?:Database|Initial Catalog)\s*=\s*([^;]+)") {
        $database = $Matches[1].Trim()
    }

    $defaultBackupPath = (& sqlcmd -S $server -E -C -b -h -1 -W -Q "SET NOCOUNT ON; SELECT CONVERT(nvarchar(4000), SERVERPROPERTY('InstanceDefaultBackupPath'));" | Select-Object -First 1).Trim()
    if ($LASTEXITCODE -ne 0 -or [string]::IsNullOrWhiteSpace($defaultBackupPath)) {
        throw "Could not read the SQL Server default backup path."
    }

    $stamp = Get-Date -Format "yyyyMMdd-HHmmss"
    $fileName = "$database-$stamp.bak"
    $serverBackupFile = Join-Path $defaultBackupPath $fileName
    $safeDatabase = $database.Replace("]", "]]" )
    $safePath = $serverBackupFile.Replace("'", "''")
    $query = "BACKUP DATABASE [$safeDatabase] TO DISK = N'$safePath' WITH INIT, CHECKSUM"

    & sqlcmd -S $server -E -C -b -Q $query
    if ($LASTEXITCODE -ne 0) {
        throw "Native SQL Server backup failed."
    }

    $localBackupFile = Join-Path $BackupDirectory $fileName
    try {
        Copy-Item $serverBackupFile $localBackupFile -Force
        Protect-LocalFile $localBackupFile
        Write-Host "Development database backup created with restricted ACL:" -ForegroundColor Green
        Write-Host "  $localBackupFile"
        Write-Host "This local backup is not production recovery evidence until an isolated restore test passes." -ForegroundColor Yellow
    }
    catch {
        Write-Host "SQL Server created the backup, but it could not be copied into the repository folder." -ForegroundColor Yellow
        Write-Host "  $serverBackupFile"
    }
}

switch ($Action) {
    "Start" {
        Initialize-EnvironmentFile
        $executionMode = Resolve-ExecutionMode -RequestedMode $Mode

        if ($executionMode -eq "Docker") {
            Write-Host "Starting Athar in Docker mode..." -ForegroundColor Cyan
            Invoke-Compose -ComposeArguments @("up", "--build", "-d")
            Save-ExecutionMode "Docker"
            Wait-UntilReady
        }
        else {
            Write-Host "Docker is unavailable. Starting Athar with local .NET and SQL Server..." -ForegroundColor Cyan
            Start-NativeProduct
        }

        Show-AccessInformation
        Start-Process $BaseUrl
    }
    "Stop" {
        $executionMode = Resolve-ExecutionMode -RequestedMode $Mode -PreferStored

        if ($executionMode -eq "Docker") {
            Initialize-EnvironmentFile
            Invoke-Compose -ComposeArguments @("down", "--remove-orphans")
            Write-Host "Athar stopped. Docker SQL Server data was preserved." -ForegroundColor Green
        }
        else {
            Stop-NativeProduct
        }
    }
    "Status" {
        $executionMode = Resolve-ExecutionMode -RequestedMode $Mode -PreferStored

        if ($executionMode -eq "Docker") {
            Initialize-EnvironmentFile
            Invoke-Compose -ComposeArguments @("ps")
            try {
                Invoke-RestMethod -Uri "$BaseUrl/health/ready" -TimeoutSec 3 | ConvertTo-Json -Depth 5
            }
            catch {
                Write-Host "The readiness endpoint is not available." -ForegroundColor Yellow
            }
        }
        else {
            Show-NativeStatus
        }
    }
    "Open" {
        Start-Process $BaseUrl
        Show-AccessInformation
    }
    "Lan" {
        Show-LanUrls
    }
    "Backup" {
        $executionMode = Resolve-ExecutionMode -RequestedMode $Mode -PreferStored
        if ($executionMode -eq "Docker") {
            Backup-DockerDatabase
        }
        else {
            Backup-NativeDatabase
        }
    }
    "Reset" {
        if (-not $Force) {
            throw "Reset removes local product files. Run the command again with -Force to confirm."
        }

        $executionMode = Resolve-ExecutionMode -RequestedMode $Mode -PreferStored
        if ($executionMode -eq "Docker") {
            Initialize-EnvironmentFile
            Invoke-Compose -ComposeArguments @("down", "--volumes", "--remove-orphans")
            Write-Host "Removed Docker containers and Docker SQL Server data." -ForegroundColor Green
        }
        else {
            Stop-NativeProduct
            Remove-Item (Join-Path $LocalDirectory "athar-native") -Recurse -Force -ErrorAction SilentlyContinue
            Remove-Item $NativeOutputLog -Force -ErrorAction SilentlyContinue
            Remove-Item $NativeErrorLog -Force -ErrorAction SilentlyContinue
            Write-Host "Removed Native launcher files. The local SQL Server database was preserved for safety." -ForegroundColor Green
        }

        Remove-Item $EnvironmentFile -Force -ErrorAction SilentlyContinue
        Remove-Item $ModeFile -Force -ErrorAction SilentlyContinue
        Remove-Item $NativePidFile -Force -ErrorAction SilentlyContinue
    }
}
