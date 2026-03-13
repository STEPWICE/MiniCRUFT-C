---
SECTION_ID: plans.minicruft-phase-3-minimal-slice
TYPE: plan
STATUS: completed
PRIORITY: high
---

# MiniCRUFT Phase 3 Minimal Slice

GOAL: Pick the next smallest vertical slice on top of the completed Phase 2 world/meshing foundation without breaking one-way renderer pickup, stable built-output drain ordering, or introducing broader `WorldHost` coupling.
TIMELINE: current session

## Task Checklist

### Phase 1: Option review
- [x] Review completed Phase 2 boundaries and tests
- [x] Compare streaming/residency vs renderer upload lifecycle vs save/load seam
- [x] Pick the smallest safe vertical slice

### Phase 2: Proposed implementation slice
- [x] Define the exact scope for the next slice
- [x] List likely file targets
- [x] Define success criteria and non-goals

## Decisions
- Keep `WorldHost` as world authority only; do not add renderer residency or upload bookkeeping there
- Preserve renderer pull-only integration via `WorldHost.TryDrainNextBuiltSubchunkMeshingOutput(...)`
- Preserve stable built-output drain ordering from world: `chunkX -> chunkZ -> subchunkIndex`
- Prefer a slice that can be validated mostly through `Rendering` + integration tests instead of broad new cross-project contracts

## Recommendation
- Pick **renderer upload lifecycle** as the next minimal Phase 3 slice

## Why This Slice
- It is the smallest real vertical on top of the current pipeline: `World` builds -> `Renderer` drains -> `Renderer` tracks upload state
- It keeps ownership clean:
  - `World` stays authoritative for meshing outputs
  - `Renderer` owns upload/residency state
  - `App/Game` remain tiny wiring only
- It avoids premature chunk streaming policy and avoids introducing persistence schema decisions too early
- It directly supports the roadmap requirement that upload path stays stable under movement and bounded work stays visible

## Defer For Later
- Streaming/residency policy in world/game space
- Chunk eviction and near/far prioritization
- Save/load chunk serialization contract
- Player-edit persistence flow

## Proposed Slice
- Add a minimal renderer-side **upload lifecycle state** for each drained subchunk mesh:
  - `PendingUpload`
  - `Uploaded`
  - optional `Superseded`/replacement handling through overwrite by `(chunk, subchunk)` key
- Add a capped renderer-side upload pump so pickup and upload are no longer the same instant step
- Keep `PickupBuiltSubchunkMeshingOutputs(WorldHost world)` one-way; it should only drain and enqueue renderer-owned pending uploads in stable incoming order
- Add `ProcessPendingUploads(int maxUploads = ...)` in `RendererHost` to commit a bounded number of pending uploads per frame
- Keep overwrite semantics simple: newest drained output for a `(chunk, subchunk)` target replaces older pending/uploaded renderer state

## Likely File Targets
- `src/MiniCRUFT.Rendering/RendererHost.cs`
  - split drain/pickup from upload commit
  - add pending upload queue + uploaded cache
  - expose tiny debug counters like `PendingSubchunkUploadCount` and `UploadedSubchunkMeshCount`
- `src/MiniCRUFT.Rendering/` new tiny type if needed:
  - `SubchunkUploadState.cs` or `RendererSubchunkUploadEntry.cs`
- `src/MiniCRUFT.App/AppHost.cs`
  - tiny frame wiring change only if needed: `pickup -> process pending uploads -> render`
- `tests/MiniCRUFT.World.Tests/WorldHostTests.cs`
  - add integration-style tests for pickup ordering, bounded upload processing, and replacement behavior without changing world ownership

## Success Criteria
- Renderer still consumes world outputs only through `WorldHost.TryDrainNextBuiltSubchunkMeshingOutput(...)`
- Stable world drain ordering remains intact end-to-end for pickup
- Upload work becomes bounded per frame on renderer side
- Rebuilt output for the same `(chunk, subchunk)` cleanly replaces older renderer upload state
- `WorldHost` gains no renderer upload/residency fields or APIs
- Focused tests cover pickup queueing, bounded upload processing, and replacement behavior

## Non-Goals
- No GPU resource implementation yet
- No frustum culling or visible-set logic yet
- No chunk retention/eviction policy yet
- No save/load format yet

## Active Session Progress
- [x] Implement renderer pickup split: drain+enqueue in `PickupBuiltSubchunkMeshingOutputs(...)`
- [x] Add bounded `ProcessPendingUploads(...)` in `RendererHost`
- [x] Add renderer-side pending/uploaded replacement behavior by `(chunk, subchunk)`
- [x] Keep `AppHost` wiring tiny: `game.Update() -> renderer.Pickup... -> renderer.ProcessPendingUploads(...) -> renderer.RenderFrame()`
- [x] Add focused integration tests for pickup ordering and bounded upload processing
- [x] Run focused tests and update plan checkpoint

## Current Checkpoint
- `RendererHost` now keeps renderer-owned per-subchunk upload lifecycle state and splits world drain/pickup from bounded upload processing.
- `PickupBuiltSubchunkMeshingOutputs(WorldHost world)` only drains world outputs and queues pending uploads in stable incoming order.
- `ProcessPendingUploads(int maxUploads = ...)` now commits a bounded number of pending uploads per call and skips superseded queue entries safely.
- Replacement by `(chunk, subchunk)` now overwrites older pending/uploaded renderer state without adding upload bookkeeping to `WorldHost`.
- `AppHost` frame wiring now does `game.Update() -> renderer.PickupBuiltSubchunkMeshingOutputs(game.World) -> renderer.ProcessPendingUploads() -> renderer.RenderFrame()`.
- Focused xUnit coverage now checks pickup ordering, bounded upload processing, pending/uploaded replacement behavior, and game-to-renderer upload pump flow.

## Next Tasks
- [x] Document recommended next slice and rationale
- [x] Name concrete file targets
- [x] Define testable success criteria for implementation handoff
