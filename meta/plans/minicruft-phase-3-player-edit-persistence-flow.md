---
SECTION_ID: plans.minicruft-phase-3-player-edit-persistence-flow
TYPE: plan
STATUS: completed
PRIORITY: high
---

# MiniCRUFT Phase 3 Player Edit Persistence Flow

GOAL: Make real block edits survive save/load and still flow back through existing dirty-subchunk/meshing behavior without leaking renderer state into persistence.
TIMELINE: current session

## Decision
- Pick **minimal player-edit persistence flow** next.
- Reason: the save/load seam is already hardened, so this is the next small vertical slice that proves real gameplay edits survive reload.

## Ownership Split
- `MiniCRUFT.World`: own voxel edit/runtime dirty-state behavior and snapshot export/import of edited voxel state
- `MiniCRUFT.Persistence`: own serialized storage/load only
- `MiniCRUFT.Game`: tiny orchestration only if required
- `MiniCRUFT.Rendering`: no changes

## Active Session Progress
- [x] Define smallest edit->save->load->rebuild path and file targets
- [x] Implement minimal runtime/persistence support
- [x] Add focused edit roundtrip tests
- [x] Run focused tests and update checkpoint

## Current Checkpoint
- `WorldHost` now tracks the smallest persistence-worthy chunk set from player edits: the edited chunk plus required X/Z seam neighbors that were pulled into dirty rebuild propagation.
- `ExportSnapshot()` now saves only that edit-affected chunk set instead of every generated/loaded chunk, while `ImportSnapshot(...)` restores the same set and keeps imported subchunks dirty/`NeedsBuild`.
- Focused tests now prove untouched generated chunks are skipped, edited chunks survive persistence JSON roundtrip, and restored chunks can run back through the existing meshing scheduler/build-output flow.
- `dotnet test tests/MiniCRUFT.World.Tests/MiniCRUFT.World.Tests.csproj` passes with 33 tests.

## Likely File Targets
- `src/MiniCRUFT.World/WorldHost.cs`
- `src/MiniCRUFT.Persistence/PersistenceHost.cs`
- `src/MiniCRUFT.Game/GameHost.cs` if tiny wiring is needed
- `tests/MiniCRUFT.World.Tests/WorldHostTests.cs`

## Success Criteria
- block edits survive save/load
- restored edited chunks come back dirty/ready for rebuild through existing flow
- no renderer/upload state enters persistence
- focused tests cover edit -> save -> load -> rebuild path
