---
SECTION_ID: docs.minecraft-clone.performance-budget
TYPE: note
---

# Performance Budget

## Performance Philosophy

Primary target is **stable frametime**.
A lower but steady frame rate is better than a high average with spikes.

The engine should always prefer:
- bounded work
- visible prioritization
- graceful degradation
- instrumentation over guesswork

## Baseline Target

Initial target:
- smooth play on mid-range Windows gaming hardware typical for 2024-2026
- stable traversal in normal terrain
- chunk generation and upload without severe frame spikes

Internal engineering target:
- keep the main thread from accumulating unbounded world/renderer work
- track both CPU and GPU frame time separately

## Frame Budget Model

Treat frame time as a fixed budget to split across:

- input + game simulation
- world streaming coordination
- completed job integration
- renderer submission
- GPU execution
- UI/debug overhead

Do not let any single subsystem consume unlimited time just because work is queued.

## Budget Rules

### Main Thread
- must stay predictable
- should only integrate a capped number of completed jobs per frame
- should cap block update propagation and chunk state transitions per frame

### Worker Threads
- can build backlog
- must be priority-driven
- must not flood memory with unlimited completed outputs waiting for main-thread pickup

### GPU Uploads
- cap bytes uploaded per frame
- defer distant chunk uploads under pressure
- prioritize chunks near player/camera

## Budgeted Work Types

These operations need explicit per-frame limits:
- chunk state transitions
- chunk generation starts
- completed generation commits
- mesh build commits
- mesh upload bytes
- save writes kicked on gameplay path
- block update cascades

## Recommended Degradation Strategy

When under pressure:

1. reduce far-radius mesh generation priority
2. delay non-critical transparent rebuilds
3. lower terrain decoration generation priority
4. keep near-player holes filled first
5. preserve input responsiveness above visual completeness

Never degrade by making controls feel laggy.

## Memory Budget Rules

Track separately:
- CPU voxel memory
- CPU mesh staging memory
- GPU chunk mesh memory
- texture memory
- upload ring memory
- save/load buffers

Use retention radius and LRU-style eviction for old chunk data.
Do not keep both full voxel data and full mesh data for far chunks longer than necessary.

## Observability Requirements

Expose at runtime:
- CPU frame time
- GPU frame time
- active chunk count
- resident subchunk count
- dirty subchunk count
- generation queue size
- meshing queue size
- upload bytes this frame
- chunk load/generate timings
- save queue size

If it cannot be measured, it cannot be optimized safely.

## Hitch Prevention

Avoid:
- synchronous disk IO on hot frame path
- synchronous GPU waits
- giant one-frame mesh rebuild bursts
- full-world revalidation after local edits
- asset decode during gameplay if preload/lazy-cache is possible

## Acceptance Thresholds

The build is healthy when:
- walking forward through unexplored terrain does not produce constant major spikes
- placing/removing blocks only causes local, bounded rebuilds
- opening menus/settings does not destabilize frame pacing
- world save operations do not visibly freeze play

## Optimization Order

Always optimize in this order:
1. eliminate unbounded work
2. reduce spikes
3. improve locality and batching
4. reduce average cost
5. add complexity only if profiling proves the need

## Non-Goals

- chasing maximum benchmark FPS
- overengineering GPU-driven rendering too early
- premature micro-optimizations before instrumentation exists
