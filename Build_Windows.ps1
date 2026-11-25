#Requires -Version 5.1
<#
.SYNOPSIS
    ULTIMATE PENALTY CUP 3D - Full Build Pipeline
    Runs Unity 6 in headless mode to:
      1. Execute the complete project setup (materials, SOs, prefabs, scenes)
      2. Run all visual upgrades (characters, stadium, ball VFX)
      3. Build Windows x64 Standalone
#>

$UnityExe   = "D:\Program Files\Unity Hub\6000.6.4f1\Editor\Unity.exe"
$ProjectDir = "D:\Program Files\UltimatePenaltyCup3D\UltimatePenaltyCup3D"
$LogFile    = "$ProjectDir\Logs\BatchBuild.log"
$BuildDir   = "$ProjectDir\Builds\Windows"

if (!(Test-Path $UnityExe)) {
    Write-Error "Unity.exe not found at: $UnityExe"
    exit 1
}

# Ensure log directory exists
New-Item -ItemType Directory -Force -Path "$ProjectDir\Logs" | Out-Null
New-Item -ItemType Directory -Force -Path $BuildDir | Out-Null

Write-Host "======================================================" -ForegroundColor Cyan
Write-Host " ULTIMATE PENALTY CUP 3D — Unity Batch Build Pipeline" -ForegroundColor Cyan
Write-Host "======================================================" -ForegroundColor Cyan
Write-Host "Unity   : $UnityExe"
Write-Host "Project : $ProjectDir"
Write-Host "Log     : $LogFile"
Write-Host "Output  : $BuildDir"
Write-Host ""

# ---- Phase 1: Execute Complete Project Setup ----
Write-Host "[PHASE 1] Running ProjectSetupBuilder.ExecuteCompleteSetup ..." -ForegroundColor Yellow
$args1 = @(
    "-quit", "-batchmode", "-nographics",
    "-projectPath", "`"$ProjectDir`"",
    "-executeMethod", "UltimatePenaltyCup.Editor.ProjectSetupBuilder.ExecuteCompleteSetup",
    "-logFile", "`"$ProjectDir\Logs\Phase1_Setup.log`""
)
$p1 = Start-Process -FilePath $UnityExe -ArgumentList $args1 -Wait -PassThru -NoNewWindow
Write-Host "Phase 1 exit code: $($p1.ExitCode)"
if ($p1.ExitCode -ne 0) {
    Write-Warning "Phase 1 reported errors. Check Logs\Phase1_Setup.log. Continuing..."
}

# ---- Phase 2: Run All Visual Upgrades ----
Write-Host ""
Write-Host "[PHASE 2] Running CharacterAndStadiumUpgrader.RunAllUpgrades ..." -ForegroundColor Yellow
$args2 = @(
    "-quit", "-batchmode", "-nographics",
    "-projectPath", "`"$ProjectDir`"",
    "-executeMethod", "UltimatePenaltyCup.Editor.CharacterAndStadiumUpgrader.RunAllUpgrades",
    "-logFile", "`"$ProjectDir\Logs\Phase2_Upgrade.log`""
)
$p2 = Start-Process -FilePath $UnityExe -ArgumentList $args2 -Wait -PassThru -NoNewWindow
Write-Host "Phase 2 exit code: $($p2.ExitCode)"
if ($p2.ExitCode -ne 0) {
    Write-Warning "Phase 2 reported errors. Check Logs\Phase2_Upgrade.log. Continuing..."
}

# ---- Phase 3: Build Scene (MatchScene) ----
Write-Host ""
Write-Host "[PHASE 3] Building MatchScene ..." -ForegroundColor Yellow
$args3 = @(
    "-quit", "-batchmode", "-nographics",
    "-projectPath", "`"$ProjectDir`"",
    "-executeMethod", "UltimatePenaltyCup.Editor.ProjectSetupBuilder.BuildMatchScene",
    "-logFile", "`"$ProjectDir\Logs\Phase3_MatchScene.log`""
)
$p3 = Start-Process -FilePath $UnityExe -ArgumentList $args3 -Wait -PassThru -NoNewWindow
Write-Host "Phase 3 exit code: $($p3.ExitCode)"

# ---- Phase 4: Windows Standalone Build ----
Write-Host ""
Write-Host "[PHASE 4] Building Windows x64 Standalone ..." -ForegroundColor Yellow
$args4 = @(
    "-quit", "-batchmode", "-nographics",
    "-projectPath", "`"$ProjectDir`"",
    "-executeMethod", "UltimatePenaltyCup.Editor.GameBuildPipeline.BuildWindowsStandalone",
    "-logFile", "`"$ProjectDir\Logs\Phase4_Build.log`""
)
$p4 = Start-Process -FilePath $UnityExe -ArgumentList $args4 -Wait -PassThru -NoNewWindow
Write-Host "Phase 4 exit code: $($p4.ExitCode)"

# ---- Result ----
Write-Host ""
Write-Host "======================================================" -ForegroundColor Cyan
if (Test-Path "$BuildDir\UltimatePenaltyCup3D.exe") {
    Write-Host " BUILD SUCCESS: $BuildDir\UltimatePenaltyCup3D.exe" -ForegroundColor Green
} else {
    Write-Host " Build EXE not found — check Phase 4 log for errors." -ForegroundColor Red
    Write-Host " Log: $ProjectDir\Logs\Phase4_Build.log" -ForegroundColor Red
}
Write-Host "======================================================" -ForegroundColor Cyan
