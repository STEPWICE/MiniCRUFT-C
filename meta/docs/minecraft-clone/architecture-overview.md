---
SECTION_ID: docs.minecraft-clone.architecture-overview
TYPE: note
---

# Architecture Overview

## High-Level Architecture

The game is split into a small set of explicit runtime domains:

1. **Platform Layer**
    - window creation
    - input
    - audio bootstrap
    - file system paths
    - localization and config bootstrap

2. **Game Layer**
    - game state machine
    - player state
    - inventory and gameplay rules
    - world interaction
    - save/load coordination

3. **World Layer**
    - world metadata and seed
    - chunk registry
    - subchunk voxel storage
    - procedural generation
    - block updates
    - meshing requests

4. **Rendering Layer**
    - camera
    - frame graph / ordered render passes
    - chunk mesh upload and draw
    - texture/material binding
    - UI composition

5. **Background Job Layer**
    - chunk generation jobs
    - lighting/build propagation jobs if enabled later
    - mesh build jobs
    - streaming prioritization

6. **Persistence Layer**
    - world saves
    - chunk serialization
    - settings
    - localization data
    - asset manifests if needed

## Runtime Principles

- main thread owns the authoritative gameplay state for the current frame
- expensive world work happens off-thread as jobs
- GPU resource creation/upload is funneled through a controlled renderer boundary
- no system is allowed to perform unbounded work in a single frame

## Main Data Flow

### Frame Loop
1. poll OS/window/input
2. advance game state
3. determine required world streaming actions
4. consume completed background jobs within budget
5. build render lists
6. execute DX12 passes
7. present
8. schedule next background work

### World Streaming Flow
1. player position determines active chunk radius
2. chunk manager computes desired set
3. missing chunks are queued for generation/load
4. generated voxel data is split into subchunks
5. dirty subchunks are queued for mesh build
6. completed meshes are uploaded to GPU
7. renderer swaps visible chunk instances safely

## Main Modules

## App/Core
Responsible for startup, shutdown, service wiring, logging, configuration, localization bootstrap, and scene/state transitions.

## World
Owns:
- world seed
- chunk coordinates
- chunk lifecycle
- voxel/block storage
- block queries and writes
- save integration

Should not know about DX12 details.

## Generation
Responsible for deterministic terrain generation from seed.
Should expose pure-ish generation stages:
- biome/noise sampling
- density/height decisions
- surface pass
- cave pass
- feature placement pass

## Meshing
Converts subchunk voxel data into renderable geometry.
Owns:
- visibility checks
- face emission
- transparent/opaque split
- rebuild reasons and dirty tracking

## Renderer
Owns all GPU concerns:
- device and swapchain
- descriptor heaps
- pipeline state objects
- upload buffers
- chunk vertex/index buffers
- texture arrays/atlases from `/assets`
- UI and debug overlays

## Save System
Responsible for:
- world metadata
- region/chunk file mapping
- versioning
- migration compatibility
- safe write strategy

## UI
Modern 2026 UX layer, but visually compatible with the game identity:
- title screen
- world create/load
- pause/settings
- keybinds
- language selection
- loading progress and error states

## Threading Model

### Main Thread
- gameplay simulation
- player actions
- chunk visibility decisions
- render submission
- swap of completed job outputs

### Worker Threads
- chunk generation
- chunk load/decode
- meshing
- optional future lighting propagation

Rule: workers produce immutable job results; main thread commits them.

## Chunk Lifecycle

A chunk moves through states similar to:
- `Unloaded`
- `Requested`
- `LoadingOrGenerating`
- `ReadyVoxelData`
- `MeshingQueued`
- `ReadyMesh`
- `Visible`
- `Dormant`
- `Evicting`

This state machine should be explicit and observable in debug UI.

## Asset Strategy

Use existing `/assets` as the source of:
- block textures
- UI textures where applicable
- metadata-derived animation information where applicable

Asset loading should normalize paths and build renderer-friendly representations during startup or lazy-load stages.

## Operational Notes

- every subsystem should expose counters for debug HUD
- all async queues need hard caps and priorities
- save writes should be crash-safe
- logs must identify chunk coordinates, job type, and timings for world pipeline events

## Future Co-op Boundary

Singleplayer is first-class. Co-op later should plug in at:
- session/transport layer
- replication/state sync layer
- authority model layer

Do not leak networking assumptions into the renderer or chunk storage layout now.
