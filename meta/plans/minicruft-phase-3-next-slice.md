---
SECTION_ID: plans.minicruft-phase-3-next-slice
TYPE: plan
STATUS: completed
PRIORITY: high
---

# MiniCRUFT Phase 3 Next Slice

GOAL: Implement the next smallest useful slice after renderer upload lifecycle while keeping boundaries clean and avoiding rewrite risk.
TIMELINE: current session

## Decision
- Pick **minimal save/load seam** next.
- Reason: it closes an earlier roadmap gap (`save/load scaffold`) with a contained cross-project slice and does not bloat renderer/world upload ownership.

## Ownership Split
- `MiniCRUFT.World`: expose the smallest serializable world snapshot/export-import seam
- `MiniCRUFT.Persistence`: own save/load formatting and file/path concerns
- `MiniCRUFT.App` or `MiniCRUFT.Game`: tiny wiring only if required
- `MiniCRUFT.Rendering`: no changes unless a test dependency requires existing flow only

## Active Session Progress
- [x] Define the minimal save/load seam and file targets
- [x] Implement world export/import seam with smallest safe surface
- [x] Implement persistence-side roundtrip scaffold
- [x] Add focused roundtrip tests
- [x] Run focused tests and update checkpoint

## Current Checkpoint
- `WorldHost` now exposes a minimal snapshot seam: `ExportSnapshot()` + `ImportSnapshot(...)` for seed plus loaded chunk voxel/state data only.
- Imported subchunks intentionally come back dirty/`NeedsBuild` so persistence restores runtime voxel state without pulling renderer upload state into the contract.
- `PersistenceHost` now owns the JSON/file scaffold via `SaveWorldToJson(...)`, `SaveWorld(...)`, `LoadWorldFromJson(...)`, and `LoadWorld(...)`.
- Focused xUnit coverage now proves direct snapshot roundtrip and persistence JSON/file roundtrip for seed + chunk/block state.
- `dotnet test tests/MiniCRUFT.World.Tests/MiniCRUFT.World.Tests.csproj` passes with 27 tests.

## Likely File Targets
- `src/MiniCRUFT.World/WorldHost.cs`
- `src/MiniCRUFT.Persistence/PersistenceHost.cs`
- maybe one tiny new persistence/world DTO file if needed
- `tests/MiniCRUFT.World.Tests/WorldHostTests.cs`

## Success Criteria
- world seed + changed/generated chunk state can roundtrip through persistence scaffold
- boundaries stay clean: persistence owns storage/format, world owns runtime state
- no renderer coupling is introduced
- focused tests prove roundtrip correctness
