#requires -Version 5.1

[CmdletBinding()]
param(
    [Parameter(Position = 0)]
    [ValidateSet(
        "help",
        "doctor",
        "start",
        "stop",
        "restart",
        "status",
        "open",
        "logs",
        "lan",
        "expose",
        "credentials",
        "backup",
        "reset",
        "restore",
        "build",
        "test",
        "verify",
        "pack",
        "production-check")]
    [string]$Action = "help",

    [ValidateSet("Athar", "Workbench", "Madar", "All", "Repository")]
    [string]$Target = "Athar",

    [ValidateSet("Auto", "Native", "Docker")]
    [string]$Mode = "Auto",

    [ValidateSet("Debug", "Release")]
    [string]$Configuration = "Release",

    [switch]$Force
)

$ErrorActionPreference = "Stop"
Set-StrictMode -Version Latest

$RepositoryRoot = $PSScriptRoot
$SolutionFile = Join-Path $RepositoryRoot "FoundationKit.sln"
$AtharManager = Join-Path $RepositoryRoot "scripts/athar-product.ps1"
$AtharTunnel = Join-Path $RepositoryRoot "scripts/expose-athar-tunnel.ps1"
$MadarManager = Join-Path $RepositoryRoot "scripts/madar-product.ps1"
$MadarUrl = "http://localhost:8100"
$LocalDirectory = Join-Path $RepositoryRoot ".local"
$LogDirectory = Join-Path $LocalDirectory "logs"
$ArtifactsDirectory = Join-Path $RepositoryRoot "artifacts"

$WorkbenchProject = Join-Path $RepositoryRoot "samples/FoundationKit.Workbench/FoundationKit.Workbench.Api.csproj"
$WorkbenchComposeFile = Join-Path $RepositoryRoot "deploy/docker-compose.yml"
$WorkbenchEnvironmentFile = Join-Path $LocalDirectory "workbench-product.env"
$WorkbenchModeFile = Join-Path $LocalDirectory "workbench-product.mode"
$WorkbenchNativeDirectory = Join-Path $LocalDirectory "workbench-native/app"
$WorkbenchPidFile = Join-Path $LocalDirectory "workbench-native.pid"
$WorkbenchOutputLog = Join-Path $LogDirectory "workbench-native.out.log"
$WorkbenchErrorLog = Join-Path $LogDirectory "workbench-native.err.log"
$WorkbenchDockerProject = "foundationkit-workbench"
$WorkbenchNativeUrl = "http://localhost:5057"
$WorkbenchDockerUrl = "http://localhost:8080"
$WorkbenchNativeListenUrl = "http://0.0.0.0:5057"
$WorkbenchDefaultConnectionString = "Server=.;Database=FoundationKitWorkbench;Trusted_Connection=True;TrustServerCertificate=True;MultipleActiveResultSets=True"

function Write-Section {
    param([Parameter(Mandatory)][string]$Title)

    Write-Host ""
    Write-Host ("=" * 76) -ForegroundColor DarkGray
    Write-Host $Title -ForegroundColor Cyan
    Write-Host ("=" * 76) -ForegroundColor DarkGray
}

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

function Invoke-CheckedCommand {
    param(
        [Parameter(Mandatory)][string]$FilePath,
        [Parameter(Mandatory)][string[]]$Arguments,
        [string]$FailureMessage = "Command failed."
    )

    & $FilePath @Arguments
    if ($LASTEXITCODE -ne 0) {
        throw "$FailureMessage Exit code: $LASTEXITCODE"
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

function New-StrongPassword {
    param([Parameter(Mandatory)][string]$Prefix)

    return $Prefix + [Guid]::NewGuid().ToString("N") + "Aa1!"
}

function Protect-LocalFile {
    param([Parameter(Mandatory)][string]$Path)

    if (-not (Test-Path $Path)) {
        return
    }

    if ($env:OS -ne "Windows_NT") {
        throw "Local credential-file protection requires Windows ACL support."
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

function Invoke-ChildPowerShell {
    param(
        [Parameter(Mandatory)][string]$ScriptPath,
        [Parameter(Mandatory)][string[]]$Arguments
    )

    if (-not (Test-Path $ScriptPath)) {
        throw "Required script was not found: $ScriptPath"
    }

    & powershell.exe -NoProfile -ExecutionPolicy Bypass -File $ScriptPath @Arguments
    if ($LASTEXITCODE -ne 0) {
        throw "Child script failed with exit code ${LASTEXITCODE}: $ScriptPath"
    }
}

function Invoke-AtharAction {
    param(
        [Parameter(Mandatory)]
        [ValidateSet("Start", "Stop", "Status", "Open", "Lan", "Backup", "Reset")]
        [string]$AtharAction
    )

    $arguments = @("-Action", $AtharAction, "-Mode", $Mode)
    if ($Force) {
        $arguments += "-Force"
    }

    Invoke-ChildPowerShell -ScriptPath $AtharManager -Arguments $arguments
}

function Invoke-MadarAction {
    param(
        [Parameter(Mandatory)]
        [ValidateSet("start", "stop", "status", "logs")]
        [string]$MadarAction
    )

    if ($MadarAction -eq "start" -and $Mode -eq "Native") {
        throw "Madar currently supports the unified manager through its Docker operational path only. Use -Mode Auto or -Mode Docker."
    }

    Invoke-ChildPowerShell -ScriptPath $MadarManager -Arguments @($MadarAction)
}

function Invoke-AtharExpose {
    Invoke-ChildPowerShell -ScriptPath $AtharTunnel -Arguments @()
}

function Show-AtharLogs {
    Write-Section "Athar logs"

    $found = $false
    $paths = @(
        (Join-Path $LogDirectory "athar-native.out.log"),
        (Join-Path $LogDirectory "athar-native.err.log")
    )

    foreach ($path in $paths) {
        if (Test-Path $path) {
            $found = $true
            Write-Host "--- $path ---" -ForegroundColor Yellow
            Get-Content $path -Tail 160
        }
    }

    if (Test-DockerReady) {
        $atharCompose = Join-Path $RepositoryRoot "deploy/athar-compose.yml"
        $atharEnv = Join-Path $LocalDirectory "athar-product.env"
        if ((Test-Path $atharCompose) -and (Test-Path $atharEnv)) {
            $found = $true
            & docker compose --project-name athar-product --env-file $atharEnv -f $atharCompose logs --tail 160
        }
    }

    if (-not $found) {
        Write-Host "No Athar logs were found." -ForegroundColor Yellow
    }
}

function Show-AtharCredentials {
    $environmentFile = Join-Path $LocalDirectory "athar-product.env"
    if (-not (Test-Path $environmentFile)) {
        throw "Athar local settings do not exist yet. Run the start action first."
    }

    $values = @{}
    foreach ($line in Get-Content $environmentFile) {
        if ([string]::IsNullOrWhiteSpace($line) -or $line.TrimStart().StartsWith("#")) {
            continue
        }

        $parts = $line -split "=", 2
        if ($parts.Count -eq 2) {
            $values[$parts[0].Trim()] = $parts[1]
        }
    }

    Write-Section "Athar local administrator"
    Write-Host "Email:    $($values['ATHAR_ADMIN_EMAIL'])"
    Write-Host "Password: $($values['ATHAR_ADMIN_PASSWORD'])"
    Write-Host "Do not share the administrator password with demo users." -ForegroundColor Yellow
}

function Initialize-WorkbenchEnvironment {
    New-Item -ItemType Directory -Force -Path $LocalDirectory | Out-Null

    if (Test-Path $WorkbenchEnvironmentFile) {
        Protect-LocalFile $WorkbenchEnvironmentFile
        return
    }

    $lines = @(
        "FOUNDATIONKIT_SQL_PASSWORD=$(New-StrongPassword 'FkitSql!')",
        "WORKBENCH_NATIVE_CONNECTION_STRING=$WorkbenchDefaultConnectionString"
    )

    [System.IO.File]::WriteAllLines(
        $WorkbenchEnvironmentFile,
        $lines,
        [System.Text.UTF8Encoding]::new($false))
    Protect-LocalFile $WorkbenchEnvironmentFile

    Write-Host "Created protected local Workbench settings at .local/workbench-product.env" -ForegroundColor Green
    Write-Host "This file is ignored by Git and restricted to the current Windows account." -ForegroundColor DarkYellow
}

function Get-WorkbenchEnvironment {
    Initialize-WorkbenchEnvironment
    $values = @{}

    foreach ($line in Get-Content $WorkbenchEnvironmentFile) {
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

function Save-WorkbenchMode {
    param([Parameter(Mandatory)][ValidateSet("Native", "Docker")][string]$ExecutionMode)

    New-Item -ItemType Directory -Force -Path $LocalDirectory | Out-Null
    [System.IO.File]::WriteAllText(
        $WorkbenchModeFile,
        $ExecutionMode,
        [System.Text.Encoding]::ASCII)
}

function Get-StoredWorkbenchMode {
    if (-not (Test-Path $WorkbenchModeFile)) {
        return $null
    }

    $stored = (Get-Content $WorkbenchModeFile -Raw).Trim()
    if ($stored -in @("Native", "Docker")) {
        return $stored
    }

    return $null
}

function Resolve-WorkbenchMode {
    param([switch]$PreferStored)

    if ($Mode -eq "Native") {
        Assert-Command "dotnet"
        return "Native"
    }

    if ($Mode -eq "Docker") {
        if (-not (Test-DockerReady)) {
            throw "Docker mode was requested, but Docker Desktop is not ready."
        }
        return "Docker"
    }

    if ($PreferStored) {
        $stored = Get-StoredWorkbenchMode
        if ($stored -eq "Docker" -and (Test-DockerReady)) {
            return "Docker"
        }
        if ($stored -eq "Native") {
            Assert-Command "dotnet"
            return "Native"
        }
    }

    if (Test-DockerReady) {
        return "Docker"
    }

    Assert-Command "dotnet"
    return "Native"
}

function Invoke-WorkbenchCompose {
    param([Parameter(Mandatory)][string[]]$ComposeArguments)

    Initialize-WorkbenchEnvironment

    & docker compose `
        --project-name $WorkbenchDockerProject `
        --env-file $WorkbenchEnvironmentFile `
        -f $WorkbenchComposeFile `
        @ComposeArguments

    if ($LASTEXITCODE -ne 0) {
        throw "Workbench Docker Compose command failed with exit code $LASTEXITCODE."
    }
}

function Get-WorkbenchProcess {
    if (-not (Test-Path $WorkbenchPidFile)) {
        return $null
    }

    $pidText = (Get-Content $WorkbenchPidFile -Raw).Trim()
    $processId = 0
    if (-not [int]::TryParse($pidText, [ref]$processId)) {
        Remove-Item $WorkbenchPidFile -Force -ErrorAction SilentlyContinue
        return $null
    }

    try {
        return Get-Process -Id $processId -ErrorAction Stop
    }
    catch {
        Remove-Item $WorkbenchPidFile -Force -ErrorAction SilentlyContinue
        return $null
    }
}

function Wait-WorkbenchReady {
    param(
        [Parameter(Mandatory)][string]$BaseUrl,
        [int]$Attempts = 120
    )

    Write-Host "Waiting for Workbench at $BaseUrl ..." -ForegroundColor Cyan

    for ($attempt = 1; $attempt -le $Attempts; $attempt++) {
        try {
            $response = Invoke-RestMethod -Uri "$BaseUrl/api/health" -TimeoutSec 3
            if ($null -ne $response) {
                Write-Host "Workbench is ready." -ForegroundColor Green
                return
            }
        }
        catch {
            Start-Sleep -Seconds 2
        }
    }

    throw "Workbench did not become ready before the timeout."
}

function Start-WorkbenchNative {
    Assert-Command "dotnet"
    Initialize-WorkbenchEnvironment

    $existing = Get-WorkbenchProcess
    if ($null -ne $existing) {
        Write-Host "Workbench is already running in Native mode with PID $($existing.Id)." -ForegroundColor Yellow
        Save-WorkbenchMode "Native"
        Wait-WorkbenchReady -BaseUrl $WorkbenchNativeUrl -Attempts 10
        Start-Process $WorkbenchNativeUrl
        return
    }

    Write-Section "Publishing Workbench"
    New-Item -ItemType Directory -Force -Path $WorkbenchNativeDirectory | Out-Null

    Invoke-CheckedCommand `
        -FilePath "dotnet" `
        -Arguments @(
            "publish",
            $WorkbenchProject,
            "--configuration", "Release",
            "--output", $WorkbenchNativeDirectory,
            "--nologo") `
        -FailureMessage "Workbench publish failed."

    $values = Get-WorkbenchEnvironment
    $connectionString = $WorkbenchDefaultConnectionString
    if ($values.ContainsKey("WORKBENCH_NATIVE_CONNECTION_STRING") -and
        -not [string]::IsNullOrWhiteSpace($values["WORKBENCH_NATIVE_CONNECTION_STRING"])) {
        $connectionString = $values["WORKBENCH_NATIVE_CONNECTION_STRING"]
    }

    New-Item -ItemType Directory -Force -Path $LogDirectory | Out-Null
    Remove-Item $WorkbenchOutputLog -Force -ErrorAction SilentlyContinue
    Remove-Item $WorkbenchErrorLog -Force -ErrorAction SilentlyContinue

    $environmentNames = @(
        "ASPNETCORE_ENVIRONMENT",
        "DOTNET_ENVIRONMENT",
        "ASPNETCORE_URLS",
        "ConnectionStrings__Workbench",
        "Database__MigrationAttempts",
        "Database__MigrationDelaySeconds")

    $oldEnvironment = @{}
    foreach ($name in $environmentNames) {
        $oldEnvironment[$name] = [Environment]::GetEnvironmentVariable($name, "Process")
    }

    try {
        [Environment]::SetEnvironmentVariable("ASPNETCORE_ENVIRONMENT", "Development", "Process")
        [Environment]::SetEnvironmentVariable("DOTNET_ENVIRONMENT", "Development", "Process")
        [Environment]::SetEnvironmentVariable("ASPNETCORE_URLS", $WorkbenchNativeListenUrl, "Process")
        [Environment]::SetEnvironmentVariable("ConnectionStrings__Workbench", $connectionString, "Process")
        [Environment]::SetEnvironmentVariable("Database__MigrationAttempts", "30", "Process")
        [Environment]::SetEnvironmentVariable("Database__MigrationDelaySeconds", "2", "Process")

        $executable = Join-Path $WorkbenchNativeDirectory "FoundationKit.Workbench.Api.exe"
        $applicationDll = Join-Path $WorkbenchNativeDirectory "FoundationKit.Workbench.Api.dll"

        if (Test-Path $executable) {
            $process = Start-Process `
                -FilePath $executable `
                -WorkingDirectory $WorkbenchNativeDirectory `
                -RedirectStandardOutput $WorkbenchOutputLog `
                -RedirectStandardError $WorkbenchErrorLog `
                -PassThru
        }
        elseif (Test-Path $applicationDll) {
            $process = Start-Process `
                -FilePath "dotnet" `
                -ArgumentList @($applicationDll) `
                -WorkingDirectory $WorkbenchNativeDirectory `
                -RedirectStandardOutput $WorkbenchOutputLog `
                -RedirectStandardError $WorkbenchErrorLog `
                -PassThru
        }
        else {
            throw "Published Workbench executable was not found."
        }
    }
    finally {
        foreach ($name in $environmentNames) {
            [Environment]::SetEnvironmentVariable($name, $oldEnvironment[$name], "Process")
        }
    }

    [System.IO.File]::WriteAllText(
        $WorkbenchPidFile,
        $process.Id.ToString(),
        [System.Text.Encoding]::ASCII)
    Save-WorkbenchMode "Native"

    try {
        Wait-WorkbenchReady -BaseUrl $WorkbenchNativeUrl
    }
    catch {
        Show-WorkbenchLogs
        throw
    }

    Start-Process $WorkbenchNativeUrl
}

function Start-WorkbenchDocker {
    if (-not (Test-DockerReady)) {
        throw "Docker Desktop is not ready."
    }

    Write-Section "Starting Workbench with Docker"
    Invoke-WorkbenchCompose -ComposeArguments @("up", "--build", "-d")
    Save-WorkbenchMode "Docker"
    Wait-WorkbenchReady -BaseUrl $WorkbenchDockerUrl
    Start-Process $WorkbenchDockerUrl
}

function Start-Workbench {
    $executionMode = Resolve-WorkbenchMode
    if ($executionMode -eq "Docker") {
        Start-WorkbenchDocker
    }
    else {
        Start-WorkbenchNative
    }
}

function Stop-WorkbenchNative {
    $process = Get-WorkbenchProcess
    if ($null -eq $process) {
        Write-Host "Workbench Native process is not running." -ForegroundColor Yellow
        return
    }

    Stop-Process -Id $process.Id -Force
    try {
        Wait-Process -Id $process.Id -Timeout 15 -ErrorAction SilentlyContinue
    }
    catch {
    }

    Remove-Item $WorkbenchPidFile -Force -ErrorAction SilentlyContinue
    Write-Host "Workbench Native process stopped." -ForegroundColor Green
}

function Stop-WorkbenchDocker {
    if (-not (Test-DockerReady)) {
        Write-Host "Docker is unavailable; Workbench Docker resources were not changed." -ForegroundColor Yellow
        return
    }

    Invoke-WorkbenchCompose -ComposeArguments @("down", "--remove-orphans")
    Write-Host "Workbench Docker stack stopped. SQL data was preserved." -ForegroundColor Green
}

function Stop-Workbench {
    $executionMode = Resolve-WorkbenchMode -PreferStored
    if ($executionMode -eq "Docker") {
        Stop-WorkbenchDocker
    }
    else {
        Stop-WorkbenchNative
    }
}

function Show-WorkbenchStatus {
    $executionMode = Resolve-WorkbenchMode -PreferStored
    Write-Host "Workbench mode: $executionMode" -ForegroundColor Cyan

    if ($executionMode -eq "Docker") {
        Invoke-WorkbenchCompose -ComposeArguments @("ps")
        try {
            Invoke-RestMethod -Uri "$WorkbenchDockerUrl/api/health" -TimeoutSec 3 | ConvertTo-Json -Depth 5
        }
        catch {
            Write-Host "Workbench Docker readiness is unavailable." -ForegroundColor Yellow
        }
        return
    }

    $process = Get-WorkbenchProcess
    if ($null -eq $process) {
        Write-Host "Workbench Native process is not running." -ForegroundColor Yellow
        return
    }

    Write-Host "Workbench Native PID: $($process.Id)" -ForegroundColor Green
    try {
        Invoke-RestMethod -Uri "$WorkbenchNativeUrl/api/health" -TimeoutSec 3 | ConvertTo-Json -Depth 5
    }
    catch {
        Write-Host "Workbench process exists, but health is unavailable." -ForegroundColor Yellow
    }
}

function Open-Workbench {
    $executionMode = Resolve-WorkbenchMode -PreferStored
    $url = if ($executionMode -eq "Docker") { $WorkbenchDockerUrl } else { $WorkbenchNativeUrl }
    Start-Process $url
}

function Open-Madar {
    Start-Process $MadarUrl
}

function Show-WorkbenchLogs {
    Write-Section "Workbench logs"
    $executionMode = Resolve-WorkbenchMode -PreferStored

    if ($executionMode -eq "Docker") {
        if (Test-DockerReady) {
            Invoke-WorkbenchCompose -ComposeArguments @("logs", "--tail", "200")
        }
        return
    }

    $found = $false
    foreach ($path in @($WorkbenchOutputLog, $WorkbenchErrorLog)) {
        if (Test-Path $path) {
            $found = $true
            Write-Host "--- $path ---" -ForegroundColor Yellow
            Get-Content $path -Tail 200
        }
    }

    if (-not $found) {
        Write-Host "No Workbench logs were found." -ForegroundColor Yellow
    }
}

function Show-LanUrls {
    param(
        [Parameter(Mandatory)][string]$ProductName,
        [Parameter(Mandatory)][int]$Port
    )

    $addresses = Get-NetIPAddress -AddressFamily IPv4 -ErrorAction SilentlyContinue |
        Where-Object {
            $_.IPAddress -notlike "127.*" -and
            $_.IPAddress -notlike "169.254.*" -and
            $_.AddressState -eq "Preferred"
        } |
        Select-Object -ExpandProperty IPAddress -Unique

    Write-Section "$ProductName LAN URLs"
    if (-not $addresses) {
        Write-Host "No suitable IPv4 address was found." -ForegroundColor Yellow
        return
    }

    foreach ($address in $addresses) {
        Write-Host "http://${address}:$Port"
    }

    Write-Host "Allow TCP port $Port through Windows Firewall when required." -ForegroundColor Yellow
}

function Reset-Workbench {
    if (-not $Force) {
        throw "Reset requires -Force. It removes Workbench runtime files and Docker volumes when Docker mode is selected."
    }

    $executionMode = Resolve-WorkbenchMode -PreferStored
    if ($executionMode -eq "Docker") {
        Invoke-WorkbenchCompose -ComposeArguments @("down", "--volumes", "--remove-orphans")
    }
    else {
        Stop-WorkbenchNative
        Remove-Item (Join-Path $LocalDirectory "workbench-native") -Recurse -Force -ErrorAction SilentlyContinue
        Remove-Item $WorkbenchPidFile -Force -ErrorAction SilentlyContinue
        Remove-Item $WorkbenchOutputLog -Force -ErrorAction SilentlyContinue
        Remove-Item $WorkbenchErrorLog -Force -ErrorAction SilentlyContinue
        Write-Host "Native runtime files were removed. The local SQL database was preserved." -ForegroundColor Yellow
    }

    Remove-Item $WorkbenchModeFile -Force -ErrorAction SilentlyContinue
    Write-Host "Workbench reset completed." -ForegroundColor Green
}

function Invoke-Restore {
    Write-Section "Restore"
    Assert-Command "dotnet"
    Invoke-CheckedCommand `
        -FilePath "dotnet" `
        -Arguments @("restore", $SolutionFile) `
        -FailureMessage "Solution restore failed."
}

function Invoke-Build {
    Invoke-Restore
    Write-Section "Build $Configuration"
    Invoke-CheckedCommand `
        -FilePath "dotnet" `
        -Arguments @("build", $SolutionFile, "--configuration", $Configuration, "--no-restore") `
        -FailureMessage "Solution build failed."
}

function Invoke-Test {
    Invoke-Build
    Write-Section "Tests $Configuration"
    Invoke-CheckedCommand `
        -FilePath "dotnet" `
        -Arguments @(
            "test",
            $SolutionFile,
            "--configuration", $Configuration,
            "--no-build",
            "--logger", "console;verbosity=minimal") `
        -FailureMessage "Solution tests failed."
}

function Invoke-Verify {
    Invoke-Test

    Write-Section "Repository verification"
    Assert-Command "git"
    Invoke-CheckedCommand `
        -FilePath "git" `
        -Arguments @("diff", "--check") `
        -FailureMessage "Git whitespace validation failed."

    Invoke-CheckedCommand `
        -FilePath "dotnet" `
        -Arguments @(
            "run",
            "--project", (Join-Path $RepositoryRoot "tools/FoundationKit.CatalogGenerator"),
            "--configuration", $Configuration,
            "--no-build",
            "--",
            "--check") `
        -FailureMessage "Generated capability documentation check failed."

    if (Test-Command "python") {
        Invoke-CheckedCommand `
            -FilePath "python" `
            -Arguments @((Join-Path $RepositoryRoot "scripts/repository-hygiene.py")) `
            -FailureMessage "Tracked repository hygiene check failed."

        Invoke-CheckedCommand `
            -FilePath "python" `
            -Arguments @((Join-Path $RepositoryRoot "scripts/verify-pages.py")) `
            -FailureMessage "GitHub Pages manifest verification failed."
    }
    else {
        Write-Host "Python was not found; local repository-hygiene and Pages checks were skipped." -ForegroundColor Yellow
    }

    if (Test-Command "node") {
        foreach ($script in @(
            (Join-Path $RepositoryRoot "site/app.js"),
            (Join-Path $RepositoryRoot "site/athar-demo/app.js"))) {
            Invoke-CheckedCommand `
                -FilePath "node" `
                -Arguments @("--check", $script) `
                -FailureMessage "JavaScript syntax validation failed for $script."
        }
    }
    else {
        Write-Host "Node.js was not found; local JavaScript syntax checks were skipped." -ForegroundColor Yellow
    }

    Write-Host "Repository verification completed." -ForegroundColor Green
}

function Invoke-Pack {
    Write-Section "Pack reusable FoundationKit packages"

    $packScript = Join-Path $RepositoryRoot "scripts/pack.ps1"
    Invoke-ChildPowerShell `
        -ScriptPath $packScript `
        -Arguments @(
            "-Configuration", $Configuration,
            "-Output", "artifacts/packages")

    Write-Host "Packages: $(Join-Path $ArtifactsDirectory 'packages')" -ForegroundColor Green
}

function Test-LocalHealthEndpoint {
    param(
        [Parameter(Mandatory)][string]$Url,
        [int]$Attempts = 2,
        [int]$TimeoutSeconds = 3
    )

    for ($attempt = 1; $attempt -le $Attempts; $attempt++) {
        try {
            Invoke-WebRequest -Uri $Url -UseBasicParsing -TimeoutSec $TimeoutSeconds | Out-Null
            return $true
        }
        catch {
            if ($attempt -lt $Attempts) {
                Start-Sleep -Milliseconds 250
            }
        }
    }

    return $false
}

function Get-TrackedProcessId {
    param([string]$PidFile)

    if ([string]::IsNullOrWhiteSpace($PidFile) -or -not (Test-Path $PidFile)) {
        return $null
    }

    $pidText = (Get-Content $PidFile -Raw).Trim()
    $processId = 0
    if (-not [int]::TryParse($pidText, [ref]$processId)) {
        return $null
    }

    try {
        Get-Process -Id $processId -ErrorAction Stop | Out-Null
        return $processId
    }
    catch {
        return $null
    }
}

function Invoke-Doctor {
    Write-Section "FoundationKit doctor"

    $required = @("git", "dotnet", "powershell")
    $optional = @("docker", "cloudflared", "python", "node", "sqlcmd")
    $failed = $false
    $portOwners = @{}

    foreach ($name in $required) {
        if (Test-Command $name) {
            Write-Host ("[PASS] " + $name) -ForegroundColor Green
        }
        else {
            Write-Host ("[FAIL] " + $name) -ForegroundColor Red
            $failed = $true
        }
    }

    foreach ($name in $optional) {
        if (Test-Command $name) {
            Write-Host ("[PASS] " + $name + " (optional)") -ForegroundColor Green
        }
        else {
            Write-Host ("[INFO] " + $name + " is not installed") -ForegroundColor DarkYellow
        }
    }

    if (Test-Command "dotnet") {
        $activeSdk = (& dotnet --version).Trim()
        Write-Host "dotnet SDK: $activeSdk"

        $sdkLines = @(& dotnet --list-sdks)
        $hasDotNet10 = @($sdkLines | Where-Object { $_ -match '^10\.' }).Count -gt 0
        if ($hasDotNet10) {
            Write-Host "[PASS] .NET 10 SDK is installed for global.json." -ForegroundColor Green
        }
        else {
            Write-Host "[FAIL] .NET 10 SDK is required by global.json but was not found." -ForegroundColor Red
            $failed = $true
        }
    }

    Write-Host "Docker ready: $(Test-DockerReady)"
    Write-Host "Solution: $SolutionFile"
    Write-Host "Repository root: $RepositoryRoot"
    Write-Host "Local run guide: $(Join-Path $RepositoryRoot 'docs/LOCAL-RUN-WINDOWS-AR.md')"
    Write-Host "Madar operations guide: $(Join-Path $RepositoryRoot 'docs/MADAR-OPERATIONS-AR.md')"

    if ($env:OS -eq "Windows_NT") {
        $sqlServices = @(Get-Service -Name 'MSSQLSERVER', 'MSSQL$*' -ErrorAction SilentlyContinue | Sort-Object Name)
        if ($sqlServices.Count -eq 0) {
            Write-Host "[INFO] No local SQL Server service was detected; Docker mode can still be used." -ForegroundColor DarkYellow
        }
        else {
            foreach ($service in $sqlServices) {
                $color = if ($service.Status -eq "Running") { "Green" } else { "DarkYellow" }
                $label = if ($service.Status -eq "Running") { "PASS" } else { "INFO" }
                Write-Host "[$label] SQL service $($service.Name): $($service.Status)" -ForegroundColor $color
            }
        }

        if (Test-Command "Get-NetTCPConnection") {
            foreach ($port in @(5057, 5068, 8080, 8090, 8100, 14333, 14334, 14335)) {
                $listener = Get-NetTCPConnection -State Listen -LocalPort $port -ErrorAction SilentlyContinue |
                    Select-Object -First 1
                if ($null -eq $listener) {
                    Write-Host "[PASS] Port $port is available." -ForegroundColor Green
                }
                else {
                    $portOwners[$port] = [int]$listener.OwningProcess
                    Write-Host "[INFO] Port $port is already listening (PID $($listener.OwningProcess))." -ForegroundColor DarkYellow
                }
            }
        }
    }

    if (Test-Command "git") {
        $status = & git -C $RepositoryRoot status --porcelain
        if ([string]::IsNullOrWhiteSpace(($status -join ""))) {
            Write-Host "Git working tree: clean" -ForegroundColor Green
        }
        else {
            Write-Host "Git working tree: contains local changes" -ForegroundColor Yellow
            $status | ForEach-Object { Write-Host "  $_" }
        }
    }

    $atharPidFile = Join-Path $LocalDirectory "athar-native.pid"
    foreach ($health in @(
        @{ Name = "Athar"; Url = "http://127.0.0.1:8090/health/ready"; Port = 8090; PidFile = $atharPidFile },
        @{ Name = "Workbench Native"; Url = "http://127.0.0.1:5057/api/health"; Port = 5057; PidFile = $WorkbenchPidFile },
        @{ Name = "Workbench Docker"; Url = "http://127.0.0.1:8080/api/health"; Port = 8080; PidFile = $null },
        @{ Name = "Madar"; Url = "http://127.0.0.1:8100/health/ready"; Port = 8100; PidFile = $null })) {
        $isHealthy = Test-LocalHealthEndpoint -Url $health.Url
        if ($isHealthy) {
            Write-Host "[RUNNING] $($health.Name): $($health.Url)" -ForegroundColor Green
            continue
        }

        $trackedProcessId = Get-TrackedProcessId -PidFile $health.PidFile
        if ($null -ne $trackedProcessId) {
            Write-Host "[DEGRADED] $($health.Name): process PID $trackedProcessId is running, but health did not respond at $($health.Url)." -ForegroundColor Yellow
            continue
        }

        if ($portOwners.ContainsKey([int]$health.Port)) {
            $listenerPid = $portOwners[[int]$health.Port]
            Write-Host "[LISTENING] $($health.Name): port $($health.Port) is owned by PID $listenerPid, but health did not respond at $($health.Url)." -ForegroundColor Yellow
            continue
        }

        Write-Host "[STOPPED] $($health.Name)" -ForegroundColor DarkGray
    }

    if ($failed) {
        throw "One or more required local prerequisites are missing."
    }
}

function Invoke-ProductionCheck {
    Invoke-Verify

    Write-Section "Production baseline checks"
    $failed = $false

    $settingsPath = Join-Path $RepositoryRoot "examples/Athar/Athar.Api/appsettings.json"
    $settings = Get-Content $settingsPath -Raw | ConvertFrom-Json

    if ($settings.AdminSeed.Enabled -eq $false) {
        Write-Host "[PASS] Admin seed is disabled in committed appsettings.json." -ForegroundColor Green
    }
    else {
        Write-Host "[FAIL] Admin seed must be disabled in committed production settings." -ForegroundColor Red
        $failed = $true
    }

    if ([string]::IsNullOrWhiteSpace([string]$settings.ConnectionStrings.Athar)) {
        Write-Host "[PASS] No production database secret is committed." -ForegroundColor Green
    }
    else {
        Write-Host "[FAIL] A database connection string is committed in appsettings.json." -ForegroundColor Red
        $failed = $true
    }

    $gitStatus = & git -C $RepositoryRoot status --porcelain
    if ([string]::IsNullOrWhiteSpace(($gitStatus -join ""))) {
        Write-Host "[PASS] Git working tree is clean." -ForegroundColor Green
    }
    else {
        Write-Host "[WARN] Git working tree contains local changes." -ForegroundColor Yellow
    }

    Write-Host ""
    Write-Host "External production gates that this script cannot approve:" -ForegroundColor Yellow
    Write-Host "  - Trusted HTTPS certificate and production domain"
    Write-Host "  - Secret vault and least-privilege database account"
    Write-Host "  - Managed backups plus a tested restore exercise"
    Write-Host "  - Central logs, metrics, tracing, and alerts"
    Write-Host "  - Email confirmation, password reset, and administrative MFA"
    Write-Host "  - SAST, dependency, secret, penetration, and load testing"
    Write-Host "  - Privacy, terms, retention, accessibility, and domain compliance"
    Write-Host "  - Incident response, rollback, and product-owner acceptance"
    Write-Host ""
    Write-Host "See docs/PRODUCTION-READINESS-AR.md for the complete gate." -ForegroundColor Cyan

    if ($failed) {
        throw "Automated production baseline checks failed."
    }

    Write-Host "Automated baseline passed. External environment approval is still required." -ForegroundColor Green
}

function Show-Help {
    @"
FoundationKit unified repository manager

Usage:
  powershell -NoProfile -ExecutionPolicy Bypass -File .\foundationkit.ps1 <action> [options]

Product lifecycle actions:
  start              Start Athar, Workbench, Madar, or the supported All set
  stop               Stop while preserving data
  restart            Stop and start again
  status             Show process, containers, and health
  open               Open the selected application
  logs               Tail local or Docker logs
  lan                Show URLs for devices on the same network
  expose             Create a temporary public HTTPS URL for Athar
  credentials        Show the local Athar administrator account
  backup             Back up the Athar database
  reset              Remove supported runtime data; requires -Force

Repository actions:
  doctor             Check tools, .NET 10, SQL services, ports, Git state, and running applications
  restore            Restore NuGet dependencies
  build              Restore and build the full solution
  test               Restore, build, and test the full solution
  verify             Test plus hygiene, catalog, Pages, and JS checks available locally
  pack               Create all reusable NuGet and symbol packages
  production-check   Run the automated baseline and print external gates

Options:
  -Target Athar|Workbench|Madar|All|Repository
  -Mode Auto|Native|Docker
  -Configuration Debug|Release
  -Force

Examples:
  .\foundationkit.ps1 doctor
  .\foundationkit.ps1 start -Target Madar -Mode Docker
  .\foundationkit.ps1 status -Target Madar
  .\foundationkit.ps1 logs -Target Madar
  .\foundationkit.ps1 start -Target All -Mode Auto
  .\foundationkit.ps1 start -Target All -Mode Native
  .\foundationkit.ps1 status -Target All
  .\foundationkit.ps1 stop -Target All
  .\foundationkit.ps1 verify
  .\foundationkit.ps1 pack
  .\foundationkit.ps1 production-check

Auto mode uses Docker when Docker Desktop is ready; otherwise Athar and Workbench can use local .NET and SQL Server.
Madar currently uses its Docker operational path. When -Target All -Mode Native is selected, Madar is skipped so the existing Athar/Workbench native flow remains compatible.
See docs/LOCAL-RUN-WINDOWS-AR.md and docs/MADAR-OPERATIONS-AR.md for the canonical local-run paths.
"@ | Write-Host
}

function Invoke-StartTarget {
    switch ($Target) {
        "Athar" {
            Invoke-AtharAction "Start"
        }
        "Workbench" {
            Start-Workbench
        }
        "Madar" {
            Invoke-MadarAction "start"
        }
        "All" {
            Invoke-AtharAction "Start"
            Start-Workbench

            if ($Mode -eq "Native") {
                Write-Host "Madar is skipped for -Target All -Mode Native because its current operational path is Docker-only." -ForegroundColor Yellow
            }
            elseif (Test-DockerReady) {
                Invoke-MadarAction "start"
            }
            else {
                Write-Host "Madar was not started because Docker is unavailable. Athar and Workbench remain started through their supported path." -ForegroundColor Yellow
            }
        }
        default {
            throw "Start requires -Target Athar, Workbench, Madar, or All."
        }
    }
}

function Invoke-StopTarget {
    switch ($Target) {
        "Athar" {
            Invoke-AtharAction "Stop"
        }
        "Workbench" {
            Stop-Workbench
        }
        "Madar" {
            Invoke-MadarAction "stop"
        }
        "All" {
            if (Test-DockerReady) {
                Invoke-MadarAction "stop"
            }
            else {
                Write-Host "Docker is unavailable; Madar Docker resources were not changed." -ForegroundColor Yellow
            }
            Stop-Workbench
            Invoke-AtharAction "Stop"
        }
        default {
            throw "Stop requires -Target Athar, Workbench, Madar, or All."
        }
    }
}

function Invoke-StatusTarget {
    switch ($Target) {
        "Athar" {
            Invoke-AtharAction "Status"
        }
        "Workbench" {
            Show-WorkbenchStatus
        }
        "Madar" {
            Invoke-MadarAction "status"
        }
        "All" {
            Write-Section "Athar status"
            Invoke-AtharAction "Status"
            Write-Section "Workbench status"
            Show-WorkbenchStatus
            Write-Section "Madar status"
            Invoke-MadarAction "status"
        }
        default {
            throw "Status requires -Target Athar, Workbench, Madar, or All."
        }
    }
}

function Invoke-OpenTarget {
    switch ($Target) {
        "Athar" {
            Invoke-AtharAction "Open"
        }
        "Workbench" {
            Open-Workbench
        }
        "Madar" {
            Open-Madar
        }
        "All" {
            Invoke-AtharAction "Open"
            Open-Workbench
            Open-Madar
        }
        default {
            throw "Open requires -Target Athar, Workbench, Madar, or All."
        }
    }
}

function Invoke-LogsTarget {
    switch ($Target) {
        "Athar" {
            Show-AtharLogs
        }
        "Workbench" {
            Show-WorkbenchLogs
        }
        "Madar" {
            Invoke-MadarAction "logs"
        }
        "All" {
            Show-AtharLogs
            Show-WorkbenchLogs
            if (Test-DockerReady) {
                Write-Section "Madar logs"
                Invoke-MadarAction "logs"
            }
            else {
                Write-Host "Docker is unavailable; Madar container logs cannot be read." -ForegroundColor Yellow
            }
        }
        default {
            throw "Logs requires -Target Athar, Workbench, Madar, or All."
        }
    }
}

function Invoke-LanTarget {
    switch ($Target) {
        "Athar" {
            Invoke-AtharAction "Lan"
        }
        "Workbench" {
            $executionMode = Resolve-WorkbenchMode -PreferStored
            $port = if ($executionMode -eq "Docker") { 8080 } else { 5057 }
            Show-LanUrls -ProductName "Workbench" -Port $port
        }
        "Madar" {
            Show-LanUrls -ProductName "Madar" -Port 8100
        }
        "All" {
            Invoke-AtharAction "Lan"
            $executionMode = Resolve-WorkbenchMode -PreferStored
            $port = if ($executionMode -eq "Docker") { 8080 } else { 5057 }
            Show-LanUrls -ProductName "Workbench" -Port $port
            Show-LanUrls -ProductName "Madar" -Port 8100
        }
        default {
            throw "LAN discovery requires -Target Athar, Workbench, Madar, or All."
        }
    }
}

function Invoke-ResetTarget {
    if (-not $Force) {
        throw "Reset is destructive and requires -Force."
    }

    switch ($Target) {
        "Athar" {
            Invoke-AtharAction "Reset"
        }
        "Workbench" {
            Reset-Workbench
        }
        "Madar" {
            throw "Madar reset is intentionally not exposed by the unified manager yet. Use the documented Madar operational path and remove its Docker volume explicitly only when data destruction is intended."
        }
        "All" {
            Reset-Workbench
            Invoke-AtharAction "Reset"
            Write-Host "Madar data was preserved because unified destructive reset support is not enabled for Madar." -ForegroundColor Yellow
        }
        default {
            throw "Reset requires -Target Athar, Workbench, Madar, or All."
        }
    }
}

Set-Location $RepositoryRoot

switch ($Action.ToLowerInvariant()) {
    "help" {
        Show-Help
    }
    "doctor" {
        Invoke-Doctor
    }
    "start" {
        Invoke-StartTarget
    }
    "stop" {
        Invoke-StopTarget
    }
    "restart" {
        Invoke-StopTarget
        Invoke-StartTarget
    }
    "status" {
        Invoke-StatusTarget
    }
    "open" {
        Invoke-OpenTarget
    }
    "logs" {
        Invoke-LogsTarget
    }
    "lan" {
        Invoke-LanTarget
    }
    "expose" {
        if ($Target -notin @("Athar", "All")) {
            throw "The public tunnel is configured for Athar only."
        }
        Invoke-AtharExpose
    }
    "credentials" {
        if ($Target -notin @("Athar", "All")) {
            throw "Credentials are available for Athar only."
        }
        Show-AtharCredentials
    }
    "backup" {
        if ($Target -notin @("Athar", "All")) {
            throw "The unified backup action currently supports Athar only."
        }
        Invoke-AtharAction "Backup"
    }
    "reset" {
        Invoke-ResetTarget
    }
    "restore" {
        Invoke-Restore
    }
    "build" {
        Invoke-Build
    }
    "test" {
        Invoke-Test
    }
    "verify" {
        Invoke-Verify
    }
    "pack" {
        Invoke-Pack
    }
    "production-check" {
        Invoke-ProductionCheck
    }
}