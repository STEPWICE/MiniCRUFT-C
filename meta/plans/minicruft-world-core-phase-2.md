---
SECTION_ID: plans.minicruft-world-core-phase-2
TYPE: plan
STATUS: completed
PRIORITY: high
---

# MiniCRUFT World Core Phase 2

GOAL: Implement the smallest useful world-core slice in `MiniCRUFT.World` that establishes deterministic chunk data, subchunk storage, coordinate helpers, and chunk retrieval from `WorldHost`.
TIMELINE: current session

## Task Checklist

### Phase 1: Model boundaries
- [x] Confirm exact minimal world-core file set
- [x] Confirm safe interfaces for `Game -> World`

### Phase 2: World data model
- [x] Add `BlockId` baseline
- [x] Add chunk coordinate and local block coordinate helpers
- [x] Add dense `Subchunk` storage
- [x] Add `Chunk` with 8 subchunks
- [x] Add `ChunkState` enum

### Phase 3: World behavior
- [x] Add deterministic simple generator
- [x] Add `WorldHost.GetOrCreateChunk(x, z)`
- [x] Add basic world counters/debug-friendly state

### Phase 4: Validation
- [x] Validate same seed => same chunk contents
- [x] Validate negative-coordinate mapping and world-space block helpers
- [x] Validate chunk dimensions/constants usage stays correct
- [x] Update next-step backlog

## Decisions
- Keep the slice inside `MiniCRUFT.World` only unless a tiny `Game` touch is required
- Prefer flat/simple deterministic terrain first; avoid caves/features for now
- Dense storage first, no palette compression yet
- Preserve `16x16x128` chunk and `16x16x16` subchunk boundaries

## Owners
- Cody: implementation
- Archy: boundary review

## Success Criteria
- [x] World data model exists and compiles
- [x] `WorldHost` can return deterministic chunk instances by coordinate
- [x] No renderer/persistence coupling is introduced
- [x] The result is a clean base for later meshing/streaming/save-load work

## Current Checkpoint
- Added minimal world-core types in `MiniCRUFT.World`: `BlockId`, `ChunkCoordinate`, `LocalBlockCoordinate`, `Subchunk`, `Chunk`, `ChunkState`, `WorldGenerator`
- Completed this slice: added a real `WorldMeshingScheduler` loop over claimed dirty snapshots, wired it through `GameHost.Update()`, migrated the lightweight runner into an xUnit test project, and added scheduler-driven renderer pickup validations while preserving one-way pickup via `WorldHost.TryDrainNextBuiltSubchunkMeshingOutput(...)` and stable `chunkX -> chunkZ -> subchunkIndex` drain semantics
- Added tiny renderer-side pickup state in `MiniCRUFT.Rendering.RendererHost`: `PickupBuiltSubchunkMeshingOutputs(WorldHost world)` drains ready outputs in one pass and caches the latest uploaded output per `(chunk, subchunk)` without reaching into world internals beyond the drain contract
- App wiring now calls the renderer pickup loop after `game.Update()` via `renderer.PickupBuiltSubchunkMeshingOutputs(game.World)`; the `GameHost.World` exposure is intentionally tiny and used only as the app-level bridge
- `WorldHost` now owns chunk cache, seed-aware initialization, deterministic `GetOrCreateChunk(x, z)`, world-space `GetBlock`/`SetBlock` helpers, `TryGetBlock`, neighbor/block-face helpers, and simple counters
- Added explicit per-subchunk mesh state via `SubchunkMeshState` and dirty propagation across chunk/subchunk seams after world edits
- Added meshing-base types/APIs: `BlockFace`, `BlockNeighbor`, `VisibleBlockFace`, `SubchunkMeshing`
- Added minimal rebuild-queue plumbing in `WorldHost`: stable dirty-subchunk discovery, next-dirty claim, and build completion marking without renderer coupling
- Added renderer-agnostic meshing snapshot contract in `MiniCRUFT.World`: `SubchunkMeshingSnapshot` + `SubchunkMeshingOutput`, immutable target/border block capture for one subchunk rebuild, and per-subchunk `Revision`/`BuildVersion` tagging for stale-result rejection
- `WorldHost` now builds meshing snapshots for dirty subchunks without mutating chunk residency and validates snapshot completion against revision/build-version state
- `WorldHost` now also supports one-step `TryClaimNextDirtySubchunkSnapshot(out snapshot)` so the next stable dirty claim can hand back a ready meshing snapshot immediately while preserving queued state and stale-result safety
- Added scheduler-facing meshing helpers in `WorldHost` for claimed snapshots: build/validate `SubchunkMeshingOutput` against target + revision + buildVersion and complete or reject without renderer coupling
- Accepted `SubchunkMeshingOutput` is now cached per subchunk, exposed via `WorldHost.TryGetBuiltSubchunkMeshingOutput(...)`, invalidated immediately when later edits dirty that subchunk again, and can be enumerated in stable chunk/subchunk order via `WorldHost.EnumerateBuiltSubchunkMeshingOutputs()`
- Added one-shot scheduler-facing drain pickup via `WorldHost.TryDrainNextBuiltSubchunkMeshingOutput(out output)`, preserving stable `chunkX -> chunkZ -> subchunkIndex` order while ensuring each accepted output is drained at most once until that subchunk is rebuilt
- Migrated the runner into real xUnit source layout at `tests/MiniCRUFT.World.Tests/WorldHostTests.cs`, preserving coverage for same-seed chunk equality, negative coordinate mapping, world-space block reads/writes, cross-chunk neighbor resolution, subchunk dirty/mesh-state transitions, dirty-subchunk claim flow, one-step claim+snapshot flow, claimed-snapshot meshing happy path, meshing scheduler budgeting, built-output retrieval/enumeration/drain/invalidation, renderer pickup one-pass consumption, stale rejection, wrong-target rejection, meshing snapshot seam correctness, stale-result protection, meshing face visibility, and chunk/subchunk constants
- `dotnet test tests/MiniCRUFT.World.Tests/MiniCRUFT.World.Tests.csproj` passes with 24 focused xUnit tests

## Next Backlog
- [x] Implement a real meshing scheduler loop over claimed dirty subchunk snapshots
- [x] Preserve one-way tiny renderer pickup via `WorldHost.TryDrainNextBuiltSubchunkMeshingOutput(...)`
- [x] Preserve stable `chunkX -> chunkZ -> subchunkIndex` drain semantics end-to-end
- [x] Migrate the lightweight validation runner into a real xUnit test project
- [x] Add integration-style renderer pickup validations on top of scheduler-driven meshing

## Active Session Progress
- [x] Add the smallest real meshing scheduler loop and wire it through the existing app/game boundary
- [x] Convert the lightweight runner into xUnit facts without broadening renderer/world coupling
- [x] Run focused tests and finalize the phase-2 plan checkpoint
- [x] Hook a minimal renderer-facing pickup loop to the built-output drain API
- [x] Keep renderer/world coupling one-way and tiny
- [x] Verify drained outputs are consumed once by the pickup loop
- [x] Add focused integration-style validation for pickup flow
- [x] Add one-shot claim/drain API for built subchunk meshing outputs
- [x] Preserve stable chunk/subchunk order during drain
- [x] Ensure drained outputs are not returned twice unless rebuilt
- [x] Add focused validations for drain happy path and repeat-drain behavior
- [x] Add stable built-output enumeration API for scheduler/renderer pickup
- [x] Validate built-output enumeration ordering and filtering
- [x] Cache last accepted `SubchunkMeshingOutput` per subchunk and expose retrieval API
- [x] Invalidate cached meshing output immediately on dirtying edits
- [x] Store accepted output only on successful completion with revision/buildVersion match
- [x] Add focused validations for retrieval happy path and dirty invalidation
- [x] Add scheduler-facing helper for `snapshot -> meshing output -> complete` flow
- [x] Accept claimed `SubchunkMeshingSnapshot` and validate output target/revision/buildVersion on completion
- [x] Preserve stable dirty ordering and queued semantics through the helper path
- [x] Add focused validations for happy path, stale rejection, and wrong-target rejection
- [x] Add one-step `claim dirty subchunk -> ready snapshot` API while preserving stable ordering and revision/build-version safety
- [x] Add meshing snapshot contract for one subchunk rebuild, including revision/version tagging
- [x] Validate snapshot correctness across chunk/subchunk seams and stale-result protection
- [x] Keep rebuild-queue plumbing aligned to the snapshot contract
- [x] Current work: add one-subchunk meshing snapshot input/output records, per-subchunk revision tracking, `WorldHost` snapshot/build-complete APIs, and focused validations
- [x] Add minimal dirty-subchunk rebuild discovery/claim APIs for the first mesh scheduler slice
- [x] Validate dirty-subchunk discovery, stable ordering, and queue/build transitions
- [x] Prepare explicit dirty/mesh-state expansion per subchunk when meshing work starts
- [x] Add world-space neighbor/block-face helper APIs for future meshing and edit propagation work
- [x] Add minimal meshing base types/APIs in `MiniCRUFT.World` without renderer coupling
- [x] Validate cross-chunk neighbor mapping and `WorldHost` API boundaries
