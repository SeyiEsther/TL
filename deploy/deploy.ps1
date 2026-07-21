<#
.SYNOPSIS
    Safe one-click deploy for the TL Production Audit System.

.DESCRIPTION
    Publishing straight over a running IIS site fails because the live app holds
    a lock on TL.dll ("the process cannot access the file ... used by another
    process"). This script does the whole app_offline dance for you:

        1. Copies app_offline.htm to the site  -> IIS stops the app, lock released
        2. Waits for the app to shut down
        3. Publishes a fresh Release build to a local staging folder
        4. Copies the build onto the site (no lock now)
        5. Removes app_offline.htm             -> IIS restarts on the new build

    app_offline.htm is ALWAYS removed at the end, even if a step fails, so the
    site can never be left stuck in maintenance mode.

.PARAMETER Site
    The live site folder (UNC or local). Defaults to the csm-srv-16 share.

.EXAMPLE
    ./deploy/deploy.ps1
    ./deploy/deploy.ps1 -Site "\\csm-srv-16\c$\inetpub\wwwroot\TL portal"
#>

[CmdletBinding()]
param(
    [string]$Site    = "\\csm-srv-16\c$\inetpub\wwwroot\TL portal",
    [string]$Project = "",
    [int]$SettleSeconds = 5
)

$ErrorActionPreference = "Stop"
$scriptRoot = Split-Path -Parent $MyInvocation.MyCommand.Path
$repoRoot   = Split-Path -Parent $scriptRoot

if ([string]::IsNullOrWhiteSpace($Project)) {
    $csproj = Get-ChildItem -Path $repoRoot -Filter *.csproj | Select-Object -First 1
    if (-not $csproj) { throw "No .csproj found in $repoRoot" }
    $Project = $csproj.FullName
}

$offlineTemplate = Join-Path $scriptRoot "app_offline.template.htm"
$offlineTarget   = Join-Path $Site "app_offline.htm"
$stage           = Join-Path $env:TEMP ("TL-publish-" + [Guid]::NewGuid().ToString("N"))

function Write-Step($msg) { Write-Host "`n=== $msg ===" -ForegroundColor Cyan }

if (-not (Test-Path $Site)) {
    throw "Site path not reachable: $Site`nCheck the server share is available and you have write access."
}

$tookOffline = $false
try {
    # 1. Build & publish to a local staging folder FIRST, so a build failure
    #    never takes the live site offline.
    Write-Step "Publishing Release build to staging"
    dotnet publish $Project -c Release -o $stage --nologo
    if ($LASTEXITCODE -ne 0) { throw "dotnet publish failed (exit $LASTEXITCODE). Live site untouched." }

    # 2. Take the app offline (releases the TL.dll lock).
    Write-Step "Taking site offline"
    Copy-Item -Path $offlineTemplate -Destination $offlineTarget -Force
    $tookOffline = $true
    Write-Host "app_offline.htm placed. Waiting $SettleSeconds s for the app to release file locks..."
    Start-Sleep -Seconds $SettleSeconds

    # 3. Copy the new build over the live site.
    #    /XF app_offline.htm  -> never overwrite/remove the offline marker here
    #    (we remove it ourselves in the finally block once copying succeeds).
    Write-Step "Copying new build to site"
    $robolog = & robocopy $stage $Site /E /R:5 /W:2 /NFL /NDL /NP /XF app_offline.htm
    # robocopy exit codes < 8 are success (0-7). 8+ is a real failure.
    if ($LASTEXITCODE -ge 8) { throw "robocopy failed (exit $LASTEXITCODE):`n$robolog" }
    Write-Host "Files copied." -ForegroundColor Green
}
finally {
    # Always bring the site back, even on failure.
    if ($tookOffline -and (Test-Path $offlineTarget)) {
        Write-Step "Bringing site back online"
        Remove-Item -Path $offlineTarget -Force -ErrorAction SilentlyContinue
        if (Test-Path $offlineTarget) {
            Write-Warning "Could not remove app_offline.htm automatically. DELETE IT MANUALLY: $offlineTarget"
        } else {
            Write-Host "app_offline.htm removed — app restarting on the new build." -ForegroundColor Green
        }
    }
    if (Test-Path $stage) { Remove-Item -Path $stage -Recurse -Force -ErrorAction SilentlyContinue }
}

Write-Host "`nDeploy complete." -ForegroundColor Green
