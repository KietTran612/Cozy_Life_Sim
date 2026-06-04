# Runtime Playtest Final Polish Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Validate the current vertical slice in Play Mode, capture the smallest useful polish backlog, fix only verified runtime issues, and leave the project with clean targeted verification.

**Architecture:** This is a validation-first plan. Start from the existing `Main.unity` scene and runtime playtest checklist, use Unity MCP where available for editor state, Play Mode, Console logs, validation runners, and screenshots, then apply scoped fixes only for defects reproduced during the walkthrough. Handoff files stay concise and point back to this plan.

**Tech Stack:** Unity 6000.3.11f1, C#, VContainer, DOTween, UniTask, Unity Editor validation menu routes, Unity MCP when available.

---

## File Structure

- Modify: `docs/plans/task.md`
  - Add or update the concise Task 42 tracker row.
- Modify: `docs/plans/current-handoff.md`
  - Record latest playtest status, verification results, known warnings, current uncommitted scope, and recommended next task.
- Modify: `docs/plans/index.md`
  - Link this detailed plan under Scene And UI and update Suggested Next Plan.
- Read-only reference: `docs/plans/2026-06-02-runtime-playtest-checklist.md`
  - Runtime walkthrough checklist.
- Potentially modify after confirmed defects only: `Assets/CozyLifeSim/Scripts/**/*.cs`
  - Runtime widgets, presenters, services, or scene setup scripts touched only when a reproduced playtest issue requires it.
- Potentially modify after confirmed defects only: `Assets/CozyLifeSim/Scenes/Main.unity`
  - Scene changes only when a reproduced scene wiring or layout issue requires regeneration or serialized adjustment.

---

### Task 42.1: Baseline Editor And Scene Health

**Files:**
- Modify: `docs/plans/current-handoff.md`

- [ ] **Step 1: Open Unity editor state through Unity MCP when available**

Use Unity MCP to confirm the editor is open on `Assets/CozyLifeSim/Scenes/Main.unity`, compilation/import is idle, and Play Mode is stopped.

Expected: Unity reports no active compile/import work and the Main scene is loaded.

- [ ] **Step 2: Fall back to local Editor log inspection if Unity MCP is unavailable**

Run:

```powershell
Get-Content -Tail 200 -LiteralPath "$env:LOCALAPPDATA\Unity\Editor\Editor.log"
```

Expected: Recent log tail is readable. Record Unity MCP as unavailable in `current-handoff.md` if this fallback is used.

- [ ] **Step 3: Check Console or Editor log for compile errors**

Use Unity MCP Console/log tools when available.

Expected: 0 compiler errors. Existing obsolete `TextureImporter.spritesheet` warnings may remain and should be documented as expected warnings.

- [ ] **Step 4: Run the smallest pre-playtest scene verification**

Use Unity MCP or Unity menu route:

```text
Tools/CozySim/Run Scene Gameplay Loop Validation
```

Expected: PASS with 0 failed checks. Do not run the full validation suite unless the user explicitly approves it.

- [ ] **Step 5: Record baseline results**

Update `docs/plans/current-handoff.md` with the baseline compile/log status and scene validation result. Keep this concise; do not paste logs.

---

### Task 42.2: Runtime Playtest Walkthrough

**Files:**
- Modify: `docs/plans/current-handoff.md`
- Read-only reference: `docs/plans/2026-06-02-runtime-playtest-checklist.md`

- [ ] **Step 1: Start Play Mode from Main scene**

Use Unity MCP to enter Play Mode from:

```text
Assets/CozyLifeSim/Scenes/Main.unity
```

Expected: Game starts in landscape orientation with the startup/home experience visible.

- [ ] **Step 2: Verify HUD startup state**

Check that HUD shows:

```text
Coins
Seeds
Crops
Level
Autosave indicator
```

Expected: Values render without overlap, missing sprites, or blocked interaction.

- [ ] **Step 3: Verify shop feedback**

Open the shop and inspect locked or unaffordable entries.

Expected: Locked/unaffordable items explain why, item icons render, tabs switch correctly, and no popup layout regression appears.

- [ ] **Step 4: Verify purchase flow**

Buy one seed and one sticker when affordable.

Expected: Coins decrement once per purchase, inventory updates immediately, and no duplicate reward or save error appears.

- [ ] **Step 5: Verify blocked farm action feedback**

Trigger a blocked farm action, such as planting without a valid seed or interacting while the action is not available.

Expected: Feedback toast appears, explains the blocked action, and does not stack indefinitely.

- [ ] **Step 6: Verify crop loop**

Plant, water through all stages, harvest, and sell a crop.

Expected: Crop stage visuals update, reward economy is coherent, inventory updates, and no stuck state remains after harvest/sale.

- [ ] **Step 7: Verify quest and dialogue loop**

Complete at least one quest and trigger the related dialogue.

Expected: Quest completion grants reward once, dialogue popup opens, typewriter/skip behavior works, and popup close returns to the prior interaction state.

- [ ] **Step 8: Verify scrapbook and diary loop**

Open scrapbook, place a sticker, cycle page style, add a diary note, drag it, and delete it.

Expected: Sticker count/placement persists in runtime state, page style changes cleanly, diary note drag/drop/delete works, and no UI overlap appears.

- [ ] **Step 9: Verify persistence lifecycle**

Stop and restart Play Mode.

Expected: Saved state is coherent: economy, crops, stickers, scrapbook, quests, diary notes, and level progression should not duplicate or disappear unexpectedly.

- [ ] **Step 10: Capture evidence**

Use Unity MCP screenshot tools when available for at least:

```text
Home HUD
Shop popup
Scrapbook/diary state
```

Expected: Screenshots show the tested state and can be referenced in the handoff summary without committing temporary files unless the user requests it.

---

### Task 42.3: Triage Reproduced Runtime Issues

**Files:**
- Modify: `docs/plans/current-handoff.md`
- Potentially modify: `Assets/CozyLifeSim/Scripts/**/*.cs`
- Potentially modify: `Assets/CozyLifeSim/Scenes/Main.unity`

- [ ] **Step 1: Create a concise issue list**

For each reproduced issue, record:

```text
Symptom:
Steps:
Expected:
Actual:
Likely scope:
Verification route:
```

Expected: Each issue has reproduction steps and a targeted verification route before any fix is attempted.

- [ ] **Step 2: Classify each issue**

Use these categories:

```text
P1: Blocks core loop or corrupts state.
P2: Breaks visible runtime behavior but has a workaround.
P3: Cosmetic polish or minor feedback issue.
```

Expected: Fix P1/P2 items first. Defer P3 items unless they are trivial and isolated.

- [ ] **Step 3: Choose the smallest fix scope**

Map each issue to one likely owner:

```text
Presenter/service logic -> Assets/CozyLifeSim/Scripts/Core or UI presenter files
Widget interaction/animation -> Assets/CozyLifeSim/Scripts/UI widget files
Scene wiring/layout -> Assets/CozyLifeSim/Scripts/Editor/CozySceneSetupWindow*.cs or Main.unity
Validation coverage -> Assets/CozyLifeSim/Scripts/Editor/CozyLifeSim*Validation*.cs
```

Expected: No broad refactor is planned unless the reproduced issue requires it.

- [ ] **Step 4: Ask before broad validation**

If a fix would require running multiple broad validation routes together, ask the user before running the full validation suite.

Expected: Without explicit approval, run only the smallest targeted validation required by the changed scope.

---

### Task 42.4: Fix Confirmed Issues Only

**Files:**
- Potentially modify: `Assets/CozyLifeSim/Scripts/**/*.cs`
- Potentially modify: `Assets/CozyLifeSim/Scenes/Main.unity`
- Modify: `docs/plans/current-handoff.md`

- [ ] **Step 1: Write or identify a failing targeted validation**

For logic or scene issues, add or identify the smallest validation route that fails for the reproduced behavior.

Expected: The failure proves the issue. If an automated validation is not practical, document the exact manual Play Mode repro.

- [ ] **Step 2: Implement the minimal fix**

Change only the owner file(s) identified in Task 42.3.

Expected: The fix addresses the reproduction without changing unrelated gameplay, content, or branding.

- [ ] **Step 3: Wait for Unity compile/import**

Use Unity MCP where available.

Expected: Unity finishes compiling/importing with 0 compiler errors.

- [ ] **Step 4: Run targeted verification for the fix**

Choose the relevant route:

```text
Core service, data model, persistence, inventory, economy, quest, or presenter logic:
Tools/CozySim/Run Logic Verification Tests

Scene setup, serialized wiring, texture importer, UI hierarchy, or layout:
Tools/CozySim/Run Scene Gameplay Loop Validation

Runtime gameplay, Play Mode interaction, persistence lifecycle, DI lifecycle, animation/tween behavior, or widget behavior:
Relevant Play Mode runtime validation or manual Play Mode repro
```

Expected: Targeted verification passes and the original issue no longer reproduces.

- [ ] **Step 5: Repeat for remaining P1/P2 issues**

Expected: All P1/P2 issues found during the walkthrough are fixed or explicitly documented as blocked with concrete reason.

---

### Task 42.5: Final Targeted Verification And Handoff

**Files:**
- Modify: `docs/plans/task.md`
- Modify: `docs/plans/current-handoff.md`
- Modify: `docs/plans/index.md`

- [ ] **Step 1: Run final compile/log check**

Use Unity MCP Console/log tools when available.

Expected: 0 compiler errors. Known expected warnings are documented.

- [ ] **Step 2: Run post-playtest scene validation**

Use Unity MCP or Unity menu route:

```text
Tools/CozySim/Run Scene Gameplay Loop Validation
```

Expected: PASS with 0 failed checks.

- [ ] **Step 3: Re-run the runtime checklist items affected by any fixes**

Use `docs/plans/2026-06-02-runtime-playtest-checklist.md`.

Expected: Affected flows pass in Play Mode.

- [ ] **Step 4: Check git status**

Run:

```powershell
git status --short
```

Expected: Only intended task files and any expected out-of-scope `.agent/scratch/` files are listed.

- [ ] **Step 5: Update task tracker**

Update `docs/plans/task.md` with Task 42 status and a one-line verification note.

Expected: The tracker stays concise and does not include logs.

- [ ] **Step 6: Update current handoff**

Update `docs/plans/current-handoff.md` with:

```text
Latest completed work
Latest verification
Known warnings or blockers
Current uncommitted scope
Recommended next task
```

Expected: Handoff is concise and points to this plan for details.

- [ ] **Step 7: Do not commit unless the user asks**

Expected: No `git commit` or `git push` is run without explicit user approval.

---

## Verification Policy

- Do not run the full validation suite by default.
- Use Unity MCP for editor state, Play Mode, Console logs, validation runners, and screenshots when available.
- For docs-only updates, Unity validation is not relevant.
- For Unity script changes, wait for Unity compile/import and check Console/Editor log.
- For runtime/widget/persistence changes, run the relevant Play Mode validation or manual Play Mode reproduction.
- Ask the user before running broad multi-route validation.

## Execution Options

Plan complete and saved to `docs/plans/2026-06-04-runtime-playtest-final-polish.md`.

1. **Subagent-Driven (recommended)** - Use `superpowers:subagent-driven-development`, execute one task at a time, and review between tasks.
2. **Inline Execution** - Use `superpowers:executing-plans`, execute in this session with checkpoints.

