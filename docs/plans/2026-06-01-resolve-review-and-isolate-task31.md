# Resolve Review & Isolate Task 31 Implementation Plan

> **For Antigravity:** REQUIRED WORKFLOW: Use `.agent/workflows/execute-plan.md` to execute this plan in single-flow mode.

**Goal:** Isolate Task 31 UI/UX Juice & Animations changes to a backup folder and restore the working tree to a completely clean state, enabling a safe and clean review of previous tasks (such as Task 21) without scope pollution.

**Architecture:**
1. Create a safe backup folder `docs/plans/backups/task31/`.
2. Move all Task 31 new files (`CozyButtonJuice.cs`, `CozyJuiceUtility.cs`, `ProgressionHudWidget.cs` and their `.meta` files) to this backup folder.
3. Move a copy of all Task 31 modified files into the backup folder for safekeeping.
4. Restore all modified files in `Assets/` to their clean HEAD commit state using `git restore`.
5. Temporarily reset the tracker files (`task.md` and `current-handoff.md`) to show Task 30 as completed and Task 31 as pending review/execution.
6. Provide a simple script/command instructions to easily restore Task 31 when the reviewer is ready.

**Tech Stack:** Git, PowerShell/Command Line

---

### Task 1: Back Up Task 31 Files

**Files:**
- Create: `docs/plans/backups/task31/` (Backup directory)
- Move to Backup:
  - `Assets/CozyLifeSim/Scripts/UI/CozyButtonJuice.cs`
  - `Assets/CozyLifeSim/Scripts/UI/CozyButtonJuice.cs.meta`
  - `Assets/CozyLifeSim/Scripts/UI/CozyJuiceUtility.cs`
  - `Assets/CozyLifeSim/Scripts/UI/CozyJuiceUtility.cs.meta`
  - `Assets/CozyLifeSim/Scripts/UI/ProgressionHudWidget.cs`
  - `Assets/CozyLifeSim/Scripts/UI/ProgressionHudWidget.cs.meta`
- Copy to Backup (for modified files):
  - `Assets/CozyLifeSim/Scripts/Core/IProgressionService.cs`
  - `Assets/CozyLifeSim/Scripts/Editor/CozySceneSetupWindow.cs`
  - `Assets/CozyLifeSim/Scripts/UI/AnimalWidget.cs`
  - `Assets/CozyLifeSim/Scripts/UI/CropWidget.cs`
  - `Assets/CozyLifeSim/Scripts/UI/GameLifetimeScope.cs`
  - `Assets/CozyLifeSim/Scripts/UI/InventoryHudWidget.cs`
  - `Assets/CozyLifeSim/Scripts/UI/Presenters/FarmPresenter.cs`
  - `Assets/CozyLifeSim/Scripts/UI/Services/ProgressionService.cs`
  - `Assets/CozyLifeSim/Scripts/UI/ShopPopup.cs`

**Step 1: Create backup directories**
Create directories `docs/plans/backups/task31/new/` and `docs/plans/backups/task31/modified/`.

**Step 2: Move new untracked files into `docs/plans/backups/task31/new/`**
Execute file system commands to move the 6 new files (3 `.cs` + 3 `.meta`).

**Step 3: Copy modified files into `docs/plans/backups/task31/modified/`**
Execute file system commands to copy the 9 modified files.

**Step 4: Verify backup completeness**
Confirm that all 15 files are safely backed up in `docs/plans/backups/task31/`.

---

### Task 2: Restore Working Directory to Clean HEAD State

**Files:**
- Modify: Restore `Assets/` to clean HEAD state.

**Step 1: Restore Assets directory**
Run `git restore Assets` to discard all Task 31 changes in `Assets/`.

**Step 2: Run Git Status**
Run `git status` to verify that `Assets/` is completely clean of any modifications or untracked files.
Expected: `Assets/` does not appear in `git status` or contains 0 modified/untracked files.

---

### Task 3: Update Tracker Files to "Done Task 30" State

**Files:**
- Modify: `docs/plans/task.md`
- Modify: `docs/plans/current-handoff.md`

**Step 1: Revert task.md to show Task 31 as Pending**
Set Task 31 status to `[ ]` and update notes to mention that the work is completed locally and backed up at `docs/plans/backups/task31/` to keep the workspace clean for the Task 21 review.

**Step 2: Revert current-handoff.md to show Task 30 as completed**
Set Current Phase to "Phase 3.0", Current Task to "Task 30", and Recommended Next Task to "Task 31". Update Current Uncommitted Scope to be empty or only show modified docs.

**Step 3: Verify with validation tests**
Run the editor tests to confirm that the clean HEAD state compiles perfectly and all 25 tests pass.
Run: `python .agent/scratch/run_unity_mcp_command.py run_tests` (or check compilation status).

---

### Task 4: Provide Restore Script and Instructions

**Files:**
- Create: `docs/plans/backups/task31/restore_task31.py`

**Step 1: Write restore script**
Write a Python script `docs/plans/backups/task31/restore_task31.py` that will automatically copy the backed-up files back into their original locations when executed, restoring Task 31 implementation 100% cleanly.

**Step 2: Verify restore script**
Dry-run test the restore script path mappings to make sure they are exact.
