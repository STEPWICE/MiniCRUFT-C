---
SECTION_ID: docs.minecraft-clone.world-chunks-generation
TYPE: note
---

# World, Chunks, Generation

## World Structure

### Logical Chunk
- size: `16 x 16 x 128`

### Subchunk
- size: `16 x 16 x 16`
- subchunks per chunk: `8`

This split is mandatory for:
- localized dirty tracking
- smaller meshing jobs
- bounded rebuild cost
- better streaming and memory behavior

## Coordinate Model

- X/Z define horizontal chunk space
- Y is vertical inside chunk
- world positions map to:
    - chunk coordinate
    - subchunk index
    - local block coordinate

Keep conversion helpers centralized and tested.

## Block Storage

Recommended initial model:
- fixed-size dense storage per subchunk
- palette compression can come later if profiling justifies it

Reason:
- simpler implementation
- fast random access
- easier meshing and block updates
- fewer failure modes early

Each subchunk should track:
- block IDs
- dirty flags
- mesh state
- optional light state later

## Chunk Lifecycle

1. required by streaming radius
2. load from disk if present
3. otherwise generate from seed
4. mark generated subchunks dirty
5. queue meshing jobs
6. upload meshes
7. display
8. unload or serialize when far enough away

## Generation Pipeline

Keep generation deterministic and stage-based.

### Stage 1: Base Terrain
Use seed-driven noise to determine:
- terrain height
- stone/dirt distribution
- water level
- basic biome flavor if included at v1

### Stage 2: Surface Pass
Apply:
- grass/sand/gravel top layers
- exposed material adjustments
- shoreline shaping

### Stage 3: Cave Pass
Carve caves after primary terrain fill.
Cave generation must be bounded and consistent across chunk borders.

### Stage 4: Feature Pass
Optional by milestone:
- trees
- ores
- flowers
- simple structures

Features that cross chunk borders should either:
- use neighbor-aware deterministic rules, or
- be resolved through a small border-safe post process

## Border Rules

Chunk seams must not produce:
- visible terrain cracks
- mismatched cave edges
- feature duplication
- face generation errors

Generation rules should be deterministic from world coordinates, not local chunk randomness alone.

## Meshing Rules

Meshing runs per subchunk.

### Opaque
Emit faces only when adjacent block does not occlude.

### Cutout
Emit in a separate material bucket.

### Transparent
Emit separately and keep rules minimal.

Neighbor access must support:
- same subchunk
- adjacent subchunk
- adjacent chunk

If neighbors are missing, use conservative rebuild logic and remesh when neighbor data arrives.

## Dirtying Strategy

Subchunk becomes dirty when:
- a block changes inside it
- a bordering block in neighbor subchunk changes visibility
- light/fluids later affect visible result

Dirty propagation should be explicit and minimal.

## Streaming Policy

Use player-centric radius with at least three concepts:
- **simulation radius**
- **mesh/visibility radius**
- **retention radius**

This allows smoother movement and less thrash.

Prioritize work by:
1. chunks closest to camera/player
2. chunks inside frustum bias
3. chunks affecting currently visible holes
4. background fill farther out

## Persistence

Save at chunk granularity.
Recommended contents:
- chunk version
- chunk coordinate
- modified block data
- generated feature state if needed
- future-proof section headers

Prefer region-style grouping later if chunk file count becomes a problem.

## Existing Assets

`/assets` should define visual mapping from block IDs to texture/material data.
The world format should not depend on raw texture file names at runtime; resolve through block definitions.

## Debug Requirements

Need debug views for:
- chunk states
- generation queue depth
- meshing queue depth
- dirty subchunks
- load/generate source
- player streaming radius rings

## Done Criteria

World pipeline is solid when:
- traversal continuously streams chunks without major hitching
- block edits rebuild only the affected local area
- chunk borders are visually correct
- save/load reproduces world state reliably
