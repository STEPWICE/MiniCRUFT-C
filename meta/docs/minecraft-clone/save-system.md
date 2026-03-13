---
SECTION_ID: docs.minecraft-clone.save-system
TYPE: note
---

# Save System

## Goal
Provide a **robust, deterministic, crash-resistant** save system for singleplayer worlds, designed so multiplayer persistence can be layered on later.

## Design Principles
- **Correctness first**
- **Incremental writes**
- **Chunk-oriented persistence**
- **Versioned format**
- **Atomic updates where possible**
- **Fast load of nearby gameplay-critical data**

## Save Structure
Each world is stored in its own folder.

```text
/worlds/{world_id}/
    world.json
    player.json
    chunks/
    r.{region_x}.{region_z}.bin
    backups/
    logs/
```

## Core Data
### `world.json`
Stores:
- world id
- display name
- seed
- world format version
- created_at / updated_at
- gameplay flags
- generator parameters

### `player.json`
Stores:
- position
- rotation
- velocity if needed
- health
- inventory
- selected hotbar slot
- spawn point
- current dimension if dimensions exist later

### Chunk Data
Each chunk stores:
- chunk coordinates
- block ids / states
- height-related compact data if used
- light data if persisted
- fluid state if needed
- block entities
- dirty/version metadata

## Persistence Model
### Chunk-Based Saving
- Save chunks independently
- Mark chunk dirty when gameplay changes it
- Queue dirty chunks for background serialization
- Flush on:
    - autosave interval
    - manual save/quit
    - critical state transition

### Region Files
Recommended:
- group chunks into region files
- reduce filesystem overhead
- keep simple lookup table per region

## Serialization Strategy
- Use a **custom binary format** for chunk payloads
- Use **JSON** for small human-readable metadata
- Version every top-level format explicitly

Example metadata:
```json
{
  "formatVersion": 1,
  "seed": 123456789,
  "name": "World 1"
}
```

## Safety
- Write temp file first, then replace
- Keep rolling backups for metadata files
- Validate chunk headers before accepting load
- Never overwrite good data with partially written buffers
- Log save failures with enough detail to recover/debug

## Autosave Policy
- Default autosave every 15-30 seconds for dirty data
- Save budget per frame must be capped
- Disk IO must run off the main gameplay path
- On quit: do a final blocking flush with progress indicator if needed

## Versioning and Migration
Every world includes:
- `formatVersion`
- optional `generatorVersion`

Rules:
- newer game versions may migrate old saves
- unsupported future versions fail with clear message
- migration should be explicit and one-way unless rollback tooling exists

## Performance Rules
- No full-world scans during save
- No blocking serialization on render-critical thread
- Chunk compression optional; only if it improves IO without hurting frametime
- Prioritize nearby chunk load latency over maximum compression ratio

## Failure Handling
If save operation fails:
- keep previous valid data
- surface warning to player
- continue running if safe
- avoid silent corruption

## Open Technical Choice
Need final decision between:
1. one-file-per-chunk for simplicity
2. region files for scale and IO efficiency

Current recommendation:
- start with **region files**
- keep serialization layer abstract so storage backend can evolve

## Acceptance Criteria
- World survives normal quit/reload
- Dirty chunks are saved without freezing gameplay
- Crash during save does not destroy previous good world
- Version mismatch is detected cleanly
- Save format supports future migration
