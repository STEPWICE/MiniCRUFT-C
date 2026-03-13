---
SECTION_ID: plans.minicruft-cleanup-and-next-slice
TYPE: plan
STATUS: in_progress
PRIORITY: high
---

# MiniCRUFT Cleanup And Next Slice

GOAL: First remove tracked generated build artifacts cleanly, then move into the next thin product slice: simple world create/load/save UI over the existing persistence flow.
TIMELINE: current session

## Task Checklist

### Phase 1: Repo cleanup
- [x] Finalize smallest safe cleanup batch for tracked generated outputs
- [x] Delegate cleanup implementation and verify repo state
- [x] Commit cleanup-only batch

### Phase 2: Next product slice
- [x] Lock the next thin product slice and touched files
- [x] Delegate implementation of simple world create/load/save UI flow after cleanup batch is isolated
- [x] Review tests/evidence for the slice
- [x] Commit slice

## Phase 2 Implementation Plan

### Scope lock
- Keep the slice strictly console-driven and single-session: create new world, save current world, load saved world, then let the existing `GameHost -> WorldMeshingScheduler -> RendererHost` flow resume normally.
- Do not expand into slot management, in-game editing UX, autosave, or persistent world lists yet.
- Keep ownership clean: `PersistenceHost` stays file/json owner, `GameHost` stays runtime-world owner, and `AppHost` owns the small UI/orchestration loop.

### Touched files
- `src/MiniCRUFT.App/AppHost.cs` — add the minimal console menu / prompt loop and wire create-save-load actions into existing hosts.
- `src/MiniCRUFT.App/Program.cs` — only if startup wiring must pass args through; otherwise leave untouched.
- `src/MiniCRUFT.Game/GameHost.cs` — tiny helper(s) only if needed to swap in a fresh/restored `WorldHost` without leaking app/persistence concerns.
- `src/MiniCRUFT.Persistence/PersistenceHost.cs` — tiny helper(s) only if needed for default save path or save-file existence checks; no UI strings.
- `tests/MiniCRUFT.World.Tests/WorldHostTests.cs` — add focused integration-style tests for app/game/persistence orchestration seams that stay console-free where possible.

### Smallest Definition of Done
- App exposes a minimal user-facing flow to:
  - create a new world with optional seed input,
  - save the current world to one known file path,
  - load that same file path back into `GameHost`.
- Loading a saved world replaces the current runtime world and the next update still rebuilds/restores through the existing meshing/upload pipeline.
- Missing save file and invalid seed input are handled with simple non-crashing console feedback.
- Product slice stays isolated from cleanup work and does not broaden persistence schema or renderer ownership.

### Test plan
- Add a focused integration test proving save -> load into `GameHost` -> `Update()` -> renderer pickup/upload still works from the restored world.
- Add a focused integration test proving create-new-world swaps the runtime world/seed cleanly before update.
- If `AppHost` gets extractable non-interactive command handlers, add narrow tests around those handlers instead of brittle console I/O tests.
- Manual smoke after cleanup isolation: run app, create world, save, load, confirm no crash and meshing/upload pump still runs.

## Owners
- Cody: cleanup implementation and product slice implementation
- Archy: slice guidance
- Many: coordination and plan tracking

## Success Criteria
- [x] tracked `bin/` and `obj/` noise is removed from git index
- [x] cleanup is isolated from product code changes
- [x] next slice is user-facing and small
- [x] focused tests prove create/load/save/resume flow

## Current Checkpoint
- Follow-up cleanup commit `36b3bdd` finished the remaining tracked generated outputs under `src/MiniCRUFT.App/bin|obj` and `src/MiniCRUFT.Platform/bin|obj`; requested `src/**/bin`, `src/**/obj`, `tests/**/bin`, and `tests/**/obj` patterns are now fully untracked from git.
- `AppHost` now exposes a minimal console loop plus non-interactive handlers for create/save/load/update over one default save path.
- `GameHost` now supports clean runtime world replacement via `CreateWorld(...)` and `LoadWorld(...)`, while `PersistenceHost` exposes default save path + existence checks without taking UI ownership.
- Focused tests cover create-world swap/invalid seed handling and save -> load -> `GameHost.Update()` -> renderer pickup/upload resume flow.
- `dotnet test tests/MiniCRUFT.World.Tests/MiniCRUFT.World.Tests.csproj` passes with 36 tests.
