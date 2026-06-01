import os
import shutil

# Paths configuration
project_root = os.path.abspath(os.path.join(os.path.dirname(__file__), "..", "..", "..", ".."))
backup_dir = os.path.dirname(os.path.abspath(__file__))

new_files_map = {
    "CozyButtonJuice.cs": "Assets/CozyLifeSim/Scripts/UI/CozyButtonJuice.cs",
    "CozyButtonJuice.cs.meta": "Assets/CozyLifeSim/Scripts/UI/CozyButtonJuice.cs.meta",
    "CozyJuiceUtility.cs": "Assets/CozyLifeSim/Scripts/UI/CozyJuiceUtility.cs",
    "CozyJuiceUtility.cs.meta": "Assets/CozyLifeSim/Scripts/UI/CozyJuiceUtility.cs.meta",
    "ProgressionHudWidget.cs": "Assets/CozyLifeSim/Scripts/UI/ProgressionHudWidget.cs",
    "ProgressionHudWidget.cs.meta": "Assets/CozyLifeSim/Scripts/UI/ProgressionHudWidget.cs.meta"
}

modified_files_map = {
    "IProgressionService.cs": "Assets/CozyLifeSim/Scripts/Core/IProgressionService.cs",
    "CozySceneSetupWindow.cs": "Assets/CozyLifeSim/Scripts/Editor/CozySceneSetupWindow.cs",
    "AnimalWidget.cs": "Assets/CozyLifeSim/Scripts/UI/AnimalWidget.cs",
    "CropWidget.cs": "Assets/CozyLifeSim/Scripts/UI/CropWidget.cs",
    "GameLifetimeScope.cs": "Assets/CozyLifeSim/Scripts/UI/GameLifetimeScope.cs",
    "InventoryHudWidget.cs": "Assets/CozyLifeSim/Scripts/UI/InventoryHudWidget.cs",
    "FarmPresenter.cs": "Assets/CozyLifeSim/Scripts/UI/Presenters/FarmPresenter.cs",
    "ProgressionService.cs": "Assets/CozyLifeSim/Scripts/UI/Services/ProgressionService.cs",
    "ShopPopup.cs": "Assets/CozyLifeSim/Scripts/UI/ShopPopup.cs"
}

print("=== Starting Cozy Life Sim Task 31 Restore Process ===")

# 1. Restore new files
print("\n[1/3] Restoring new untracked files...")
new_src_dir = os.path.join(backup_dir, "new")
for name, relative_path in new_files_map.items():
    src = os.path.join(new_src_dir, name)
    dest = os.path.join(project_root, relative_path)

    # Ensure destination directory exists
    os.makedirs(os.path.dirname(dest), exist_ok=True)

    shutil.copy2(src, dest)
    print(f"  Restored: {relative_path}")

# 2. Restore modified files
print("\n[2/3] Restoring modified files...")
mod_src_dir = os.path.join(backup_dir, "modified")
for name, relative_path in modified_files_map.items():
    src = os.path.join(mod_src_dir, name)
    dest = os.path.join(project_root, relative_path)

    # Ensure destination directory exists
    os.makedirs(os.path.dirname(dest), exist_ok=True)

    shutil.copy2(src, dest)
    print(f"  Restored: {relative_path}")

# 3. Update task.md tracker to show Task 31 in progress or done
print("\n[3/3] Task 31 files restored successfully!")
print("Please run Unity Editor to compile the restored files.")
print("=====================================================")
