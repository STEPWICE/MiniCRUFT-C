---
SECTION_ID: docs.minecraft-clone.rendering-dx12
TYPE: note
---

# Rendering DX12

## Rendering Goal

Deliver a clean, predictable DX12 renderer with stable frametime and low hitching during traversal, chunk streaming, and block interaction.

## Technology

- **API:** DirectX 12
- **Bindings:** Vortice
- **Language:** C# / .NET 10

## Renderer Responsibilities

- device and swapchain lifecycle
- descriptor heap management
- root signatures and PSOs
- upload scheduling
- texture/material binding
- chunk mesh buffer ownership
- per-frame constant data
- UI composition
- debug overlays and GPU timing hooks

## Frame Structure

Recommended frame order:

1. update frame constants
2. process completed uploads
3. depth prepass or skip if not justified
4. opaque world pass
5. alpha-tested pass if needed
6. transparent/world effects pass
7. screen-space UI pass
8. present

Keep the pass graph simple until proven insufficient.

## Rendering Model

### World Geometry
Chunk geometry is generated on CPU and uploaded as static or semi-static mesh data.

Use separate mesh groups:
- opaque blocks
- cutout blocks
- transparent blocks

This avoids material chaos and keeps draw submission predictable.

### Chunk Submission
Visible chunk instances should be gathered each frame from the world visibility set.
Renderer should consume immutable mesh handles, not raw world data.

## Resource Strategy

### Descriptor Heaps
Use stable global heaps for:
- SRV/UAV/CBV
- samplers

Avoid per-frame heap churn.

### Upload Strategy
Use a ring-buffer style upload path:
- CPU writes staging data
- GPU copies into default resources
- fence-based reuse
- budget bytes uploaded per frame

This is critical for frametime stability during chunk streaming.

### Buffer Ownership
Renderer owns:
- GPU vertex/index buffers
- upload buffers
- texture resources
- frame-local constant buffers

World/meshing code should not hold direct GPU resource references beyond renderer-issued handles.

## Texture Handling

Use `/assets` as source input.
Preferred runtime model:
- block texture atlas or array texture
- precomputed UV/material lookup per block face
- support for animated textures using metadata when needed

Keep texture binding strategy simple and deterministic. Avoid runtime material systems beyond what block rendering needs.

## Camera and Matrices

Minimum camera data per frame:
- view
- projection
- view-projection
- camera position
- fog/environment parameters

Use reversed-Z only if it materially improves depth precision without complicating the implementation too early.

## Culling Strategy

### Required
- CPU frustum culling at chunk or subchunk granularity

### Later if needed
- occlusion culling
- indirect draws
- meshlet/cluster experiments

Do not start with GPU-driven complexity. First ship a stable visible-set renderer.

## Transparency Rules

Transparent blocks should use a constrained strategy:
- separate pass
- limited material types
- stable ordering policy by chunk/subchunk distance if needed

Do not design around perfect per-pixel transparency. Favor predictable behavior.

## Debugging Hooks

Renderer debug mode should expose:
- visible chunk count
- draw call count
- uploaded bytes this frame
- GPU frame time
- descriptor usage
- mesh rebuild uploads in flight

## Failure Prevention Rules

- no synchronous GPU waits on the hot frame path
- no runtime shader recompilation during play unless explicitly in dev mode
- no unbounded buffer growth
- no resource creation spikes without budget control

## Recommended Initial PSOs

- opaque voxel
- cutout voxel
- transparent voxel
- UI sprite/text
- debug line/box

Keep PSO count intentionally low.

## Operational Concerns

- handle device lost/reset cleanly where practical
- isolate swapchain resize logic
- support windowed mode and resolution changes without corrupting renderer state
- log GPU memory-related failures with enough context to diagnose content spikes

## Done Criteria

Renderer foundation is ready when:
- it renders chunked voxel world correctly
- chunk upload does not hitch badly under normal movement
- UI overlays render on top reliably
- debug counters make streaming/render cost visible
