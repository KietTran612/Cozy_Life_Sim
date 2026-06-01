$projectRoot = Resolve-Path (Join-Path $PSScriptRoot "..\..\..\..")
$backupDir = $PSScriptRoot

$newFilesMap = @{
    "CozyButtonJuice.cs" = "Assets/CozyLifeSim/Scripts/UI/CozyButtonJuice.cs"
    "CozyButtonJuice.cs.meta" = "Assets/CozyLifeSim/Scripts/UI/CozyButtonJuice.cs.meta"
    "CozyJuiceUtility.cs" = "Assets/CozyLifeSim/Scripts/UI/CozyJuiceUtility.cs"
    "CozyJuiceUtility.cs.meta" = "Assets/CozyLifeSim/Scripts/UI/CozyJuiceUtility.cs.meta"
    "ProgressionHudWidget.cs" = "Assets/CozyLifeSim/Scripts/UI/ProgressionHudWidget.cs"
    "ProgressionHudWidget.cs.meta" = "Assets/CozyLifeSim/Scripts/UI/ProgressionHudWidget.cs.meta"
}

$modifiedFilesMap = @{
    "IProgressionService.cs" = "Assets/CozyLifeSim/Scripts/Core/IProgressionService.cs"
    "CozySceneSetupWindow.cs" = "Assets/CozyLifeSim/Scripts/Editor/CozySceneSetupWindow.cs"
    "AnimalWidget.cs" = "Assets/CozyLifeSim/Scripts/UI/AnimalWidget.cs"
    "CropWidget.cs" = "Assets/CozyLifeSim/Scripts/UI/CropWidget.cs"
    "GameLifetimeScope.cs" = "Assets/CozyLifeSim/Scripts/UI/GameLifetimeScope.cs"
    "InventoryHudWidget.cs" = "Assets/CozyLifeSim/Scripts/UI/InventoryHudWidget.cs"
    "FarmPresenter.cs" = "Assets/CozyLifeSim/Scripts/UI/Presenters/FarmPresenter.cs"
    "ProgressionService.cs" = "Assets/CozyLifeSim/Scripts/UI/Services/ProgressionService.cs"
    "ShopPopup.cs" = "Assets/CozyLifeSim/Scripts/UI/ShopPopup.cs"
}

Write-Host "=== Starting Cozy Life Sim Task 31 Restore Process (PowerShell) ===" -ForegroundColor Green

# 1. Restore new files
Write-Host "`n[1/2] Restoring new untracked files..." -ForegroundColor Yellow
$newSrcDir = Join-Path $backupDir "new"
foreach ($item in $newFilesMap.GetEnumerator()) {
    $src = Join-Path $newSrcDir $item.Key
    $dest = Join-Path $projectRoot $item.Value

    $destParent = Split-Path $dest -Parent
    if (-not (Test-Path $destParent)) {
        New-Item -ItemType Directory -Path $destParent -Force | Out-Null
    }

    Copy-Item $src $dest -Force
    Write-Host "  Restored: $($item.Value)"
}

# 2. Restore modified files
Write-Host "`n[2/2] Restoring modified files..." -ForegroundColor Yellow
$modSrcDir = Join-Path $backupDir "modified"
foreach ($item in $modifiedFilesMap.GetEnumerator()) {
    $src = Join-Path $modSrcDir $item.Key
    $dest = Join-Path $projectRoot $item.Value

    $destParent = Split-Path $dest -Parent
    if (-not (Test-Path $destParent)) {
        New-Item -ItemType Directory -Path $destParent -Force | Out-Null
    }

    Copy-Item $src $dest -Force
    Write-Host "  Restored: $($item.Value)"
}

Write-Host "`nTask 31 files restored successfully!" -ForegroundColor Green
Write-Host "Please run/open Unity Editor to compile the restored files."
Write-Host "==========================================================" -ForegroundColor Green
