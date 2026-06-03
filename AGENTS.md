# Codex Project Rules

## Superpowers Plan And Task Locations

For this project, Codex Superpowers must use the same plan and task locations as the existing project workflow:

- Implementation plans: `docs/plans/YYYY-MM-DD-<feature-name>.md`
- Live task tracker: `<project-root>/docs/plans/task.md`

Do not use `docs/superpowers/plans/` for this project unless the user explicitly asks for the Codex Superpowers default.

## Antigravity Profile Boundary

The `.agent/` directory belongs to the Antigravity Superpowers profile. Do not modify `.agent/` unless the user explicitly asks to change the Antigravity profile.

Exception: agents may create, update, and delete temporary helper files under `.agent/scratch/` when those files are used as disposable task scratch space, such as Python scripts for Unity validation or log inspection. Do not treat `.agent/scratch/` files as deliverable source files or include them in commits unless the user explicitly asks to promote them into the project workflow.

## Task Tracker Format

When updating `<project-root>/docs/plans/task.md`, keep it as a concise task tracker. Do not move long implementation details or session logs into this file.

## User Approval Boundaries

Do not create a new brand, rename the product, or introduce new branding unless the user explicitly requests it.

Do not commit changes unless the user explicitly requests a commit.

Do not push code to any remote repository unless the user explicitly requests a push.

For Unity asset/script changes, do not commit before Unity has finished compiling/importing and generated the required `.meta` files. Commits must include the corresponding `.meta` files for any new Unity-tracked files or folders.

Do not manually create Unity `.meta` files. If a new Unity-tracked file or folder does not have a `.meta` yet, wait for Unity to import/compile and generate it automatically.

Existing Unity `.meta` files may be edited when necessary, but do not change their `guid` or any serialized identifiers in a way that could break Unity references.

After completing any task that changes or adds scripts, wait for Unity to finish compiling, check the Console/Editor log for compiler errors, and fix any errors before marking the task complete.

## Verification Scope Policy

Do not run the full validation suite by default. Choose the smallest verification scope that proves the changed behavior.

- Docs-only changes: no Unity validation required.
- Non-Unity helper or scratch changes: no Unity validation unless they affect Unity execution.
- Unity script changes: wait for Unity compile/import, then check the Console/Editor log for compiler errors.
- Core service, data model, persistence, inventory, economy, quest, or presenter logic changes: run compile/log checks plus `Tools/CozySim/Run Logic Verification Tests`.
- Scene setup, serialized wiring, texture importer, asset import, UI hierarchy, or layout changes: run compile/log checks plus `Tools/CozySim/Run Scene Gameplay Loop Validation`.
- Run scene setup idempotency checks only when the task changes scene setup, importer configuration, serialized scene output, or layout code that can dirty `Main.unity`.
- Runtime gameplay, Play Mode interaction, persistence lifecycle, DI lifecycle, animation/tween behavior, or widget behavior changes: run compile/log checks plus the relevant Play Mode validation.
- Treat "full validation suite" as running multiple broad validation routes together, such as logic verification, scene gameplay validation, Play Mode runtime validation, and idempotency checks.
- Before running the full validation suite, ask the user for approval and wait for explicit acceptance. If the user does not approve full validation, run only the smallest targeted validation relevant to the changed scope.
- Full validation may be proposed for broad cross-system changes, release/final milestone validation, before a user-requested commit when the changed scope touches multiple systems, or when the user explicitly asks for full validation, but it still requires explicit user acceptance before running.
- If unsure, start with the narrowest relevant validation. Escalate to broader validation only when the targeted check fails in a way that suggests broader impact, the change crosses a listed boundary, or the user approves broader validation.

When a validation is intentionally skipped, record it as `not run - not relevant to this change` in the handoff instead of treating it as missing work.

## Active Context & Handover Guidelines

To minimize token usage, prevent context dilution, and maintain strict structural consistency across new sessions, any AI Agent starting a new chat thread MUST prioritize reading these lightweight files before executing any tasks or modifying files:

1. **Live Task Tracker**: [task.md](file:///d:/soflware/Unity/Source/Cozy_Life_Sim/docs/plans/task.md) - Concise completed/pending task state.
2. **Current Handoff**: [current-handoff.md](file:///d:/soflware/Unity/Source/Cozy_Life_Sim/docs/plans/current-handoff.md) - Latest completed work, verification status, known warnings, and recommended next task.
3. **Plan Index**: [index.md](file:///d:/soflware/Unity/Source/Cozy_Life_Sim/docs/plans/index.md) - Map of detailed plans to read only when relevant.

Only read larger historical plans when the current task specifically requires them:

- [2026-05-25-cozy-life-sim-core-architecture.md](file:///d:/soflware/Unity/Source/Cozy_Life_Sim/docs/plans/2026-05-25-cozy-life-sim-core-architecture.md) for DI, assembly, style, or architecture questions.
- [2026-05-25-cozy-life-sim-gameplay-prototype.md](file:///d:/soflware/Unity/Source/Cozy_Life_Sim/docs/plans/2026-05-25-cozy-life-sim-gameplay-prototype.md) for sticker drag/drop, notebook page flip, crop loop, or animal feedback behavior.

Agents must NOT read unrelated large historical files or perform broad workspace scans unless explicitly instructed. This guarantees a lightweight, cost-effective, and highly focused coding workspace.

## Handoff Update Protocol

After completing any new task, update:

1. [task.md](file:///d:/soflware/Unity/Source/Cozy_Life_Sim/docs/plans/task.md) with concise task status only.
2. [current-handoff.md](file:///d:/soflware/Unity/Source/Cozy_Life_Sim/docs/plans/current-handoff.md) with the latest completed work, verification results, known expected warnings or blockers, current uncommitted scope, and recommended next task.
3. [index.md](file:///d:/soflware/Unity/Source/Cozy_Life_Sim/docs/plans/index.md) only when adding a new detailed plan file.

Do not paste long logs or implementation details into the handoff files. Link to the detailed plan instead.
