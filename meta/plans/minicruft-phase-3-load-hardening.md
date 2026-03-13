---
SECTION_ID: plans.minicruft-phase-3-load-hardening
TYPE: plan
STATUS: completed
---

# MiniCRUFT Phase 3 Load Hardening

GOAL: Harden the new save/load seam with input validation, corrupt snapshot handling, and explicit snapshot format version checks without polluting `WorldHost` / `PersistenceHost` ownership.
TIMELINE: current session

## Decision
- Pick **load validation hardening + snapshot format versioning** as the next minimal slice.
- Reason: it makes the existing save/load seam safer without expanding storage scope or dragging renderer state into persistence.

## Ownership Split
- `MiniCRUFT.World`: validate imported snapshot shape/invariants that are world-domain
- `MiniCRUFT.Persistence`: validate JSON payloads, own format version field, and reject unsupported/corrupt serialized data cleanly
- `MiniCRUFT.Rendering`: no changes
- `MiniCRUFT.Game` / `MiniCRUFT.App`: no changes unless tests reveal tiny wiring need

## Active Session Progress
- [x] Define scope and file targets
- [x] Implement snapshot version field and compatibility checks
- [x] Add load validation for malformed/corrupt snapshot payloads
- [x] Keep world/persistence boundaries clean during validation
- [x] Add focused failure-path tests
- [x] Run focused tests and update checkpoint

## Current Checkpoint
- `PersistenceHost` now writes explicit `FormatVersion` into saved JSON and normalizes legacy unversioned payloads (`FormatVersion = 0`) to the current format on load.
- `PersistenceHost.LoadWorldFromJson(...)` now rejects malformed JSON and unsupported/invalid snapshot versions with clear failures before import.
- `WorldHost.ImportSnapshot(...)` now validates chunk/subchunk payload shape before mutating runtime state, so corrupt snapshots fail fast instead of partially importing.
- World-domain validation stays in `WorldHost` (`ChunkState`, subchunk count, block payload length, valid `BlockId`s, duplicate chunk coordinates); serialized format/version concerns stay in `PersistenceHost`.
- Focused tests now cover happy-path persistence roundtrip, legacy version migration, malformed JSON rejection, unsupported version rejection, and corrupt subchunk payload rejection.

## Likely File Targets
- `src/MiniCRUFT.Persistence/PersistenceHost.cs`
- `src/MiniCRUFT.World/WorldHost.cs`
- maybe one tiny snapshot DTO/constant file if truly needed
- `tests/MiniCRUFT.World.Tests/WorldHostTests.cs`

## Success Criteria
- saved JSON includes an explicit snapshot format version
- load rejects unsupported future/invalid versions with clear failure
- load rejects malformed/corrupt snapshot JSON instead of partially importing
- world-domain validation stays in world import path; persistence owns serialized format concerns
- focused tests prove happy path still works and failure paths are covered
