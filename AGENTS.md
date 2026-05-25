# Codex Project Rules

## Superpowers Plan And Task Locations

For this project, Codex Superpowers must use the same plan and task locations as the existing project workflow:

- Implementation plans: `docs/plans/YYYY-MM-DD-<feature-name>.md`
- Live task tracker: `<project-root>/docs/plans/task.md`

Do not use `docs/superpowers/plans/` for this project unless the user explicitly asks for the Codex Superpowers default.

## Antigravity Profile Boundary

The `.agent/` directory belongs to the Antigravity Superpowers profile. Do not modify `.agent/` unless the user explicitly asks to change the Antigravity profile.

## Task Tracker Format

When updating `<project-root>/docs/plans/task.md`, keep it as a concise task tracker. Do not move long implementation details or session logs into this file.

## User Approval Boundaries

Do not create a new brand, rename the product, or introduce new branding unless the user explicitly requests it.

Do not push code to any remote repository unless the user explicitly requests a push.

## Active Context & Handover Guidelines

To minimize token usage, prevent context dilution, and maintain strict structural consistency across new sessions, any AI Agent starting a new chat thread MUST prioritize reading these three files before executing any tasks or modifying files:

1. **Live Task Tracker**: [task.md](file:///d:/soflware/Unity/Source/Cozy_Life_Sim/docs/plans/task.md) - Tells the agent exactly which tasks are completed `[x]` and which ones are remaining `[ ]`.
2. **Core Architectural Guidelines**: [2026-05-25-cozy-life-sim-core-architecture.md](file:///d:/soflware/Unity/Source/Cozy_Life_Sim/docs/plans/2026-05-25-cozy-life-sim-core-architecture.md) - Defines the VContainer (DI) rules, styling configurations, layout boundaries, and assembly definition setup.
3. **Gameplay Functional Specs**: [2026-05-25-cozy-life-sim-gameplay-prototype.md](file:///d:/soflware/Unity/Source/Cozy_Life_Sim/docs/plans/2026-05-25-cozy-life-sim-gameplay-prototype.md) - Contains precise specifications for stickers, notebook page flips, crop growth loops, and breathing chicken feedbacks.

Agents must NOT read unrelated large historical files or perform broad workspace scans unless explicitly instructed. This guarantees a lightweight, cost-effective, and highly focused coding workspace.

