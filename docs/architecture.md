# ARCHITECTURE.md

## Overview
MiniCRUFT is a modular voxel engine written in C# (.NET 10) with Veldrid rendering (D3D11 by default). The codebase is split into small projects so gameplay, rendering, IO and data handling stay separated.

## Solution Layout
- `MiniCRUFT.Core` - shared config, logging, asset paths, noise helpers, math types
- `MiniCRUFT.World` - blocks, biomes, chunk data, world generation, lighting, fluids and liquid helpers
- `MiniCRUFT.Renderer` - GPU rendering, chunk meshing, sky, clouds, HUD, text
- `MiniCRUFT.Game` - main loop, player, input, inventory, world session, runtime systems
- `MiniCRUFT.IO` - chunk persistence and world save data
- `MiniCRUFT.UI` - shared HUD state and localization strings
- `MiniCRUFT.Audio` - platform audio backends
- `MiniCRUFT.Tests` - automated checks for saves, generation and gameplay helpers

## Core
- `GameConfig` - root configuration object, normalization and JSON load/save
- `RenderConfig`, `FaceShadingConfig`, `PaletteConfig`, `FogConfig`, `FoliageConfig`, `LodConfig` - renderer tuning and visual subsettings
- `AtmosphereConfig`, `UiConfig`, `WorldGenConfig`, `AudioConfig`, `ParticleConfig`, `PhysicsConfig`, `SaveConfig`, `SpawnConfig`, `FluidConfig`, `FallingConfig`, `FireConfig`, `MobConfig` - subsystem settings
- `ToolConfig` - tool durability, mining speed, break-time scaling and repair values
- `SpatialAudio` - shared spatial attenuation and stereo pan helper for world sounds
- `AssetStore` - canonical asset lookup and caching
- `Log` - shared logging API
- `ColorSpace` - `sRGB` and `Linear` conversion helpers
- `Vector3i`, `ChunkCoord`, `BlockCoord` - integer coordinate types
- `NoiseService` - generator noise wrapper

## World
- `Chunk` - 16x16x256 storage for blocks and light data
- `BlockId` - compact block identifiers, including water and lava levels
- `BlockRegistry` / `BlockDefinition` - block properties, textures and render flags
- `WaterBlocks` - helper for source/level water handling
- `LiquidKind` / `LiquidBlocks` - shared helpers for water and lava IDs and levels
- `BiomeId` / `BiomeRegistry` - biome identifiers and climate metadata
- `BiomeColorMap` - foliage and grass colormap lookup
- `WorldHeightSampler` - terrain sampling and biome-aware height shaping
- `WorldGenerator` - terrain, rivers, caves, surface blocks, aquifers and feature placement
- `WorldLiquidSeeder` - underground liquid source placement
- `TreeGenerator` - tree shapes and biome-specific foliage
- `SurfaceFeatureGenerator` - ponds, boulders, logs, gravel, flowers and similar details
- `WorldLighting` - sunlight and torchlight propagation
- `WorldEditor` - single mutation entry point for block changes
- `WorldChangeQueue` - queued world mutations for downstream systems
- `FluidSystem` - water and lava spread, source handling and lava hardening
- `FallingBlockSystem` - sand and gravel simulation
- `FireSystem` - fire spread, burn-out, explosion ignition and rain extinguish behavior
- `World` - chunk access and block queries

## Renderer
- `RenderDevice` - window, swapchain and command submission
- `WorldRenderer` - chunk rendering and visibility control
- `ChunkMeshBuilder` - greedy meshing, lighting and special block geometry
- `ParticleSystem` - world-space particles for block break/place, player feedback and mob attack/hurt/death bursts
- `IParticleEmitter` - render-side particle emission contract for gameplay systems
- `TextureAtlas` - block texture atlas
- `TextureAtlasAnimator` - runtime updater for animated atlas regions parsed from `.mcmeta`
- `ItemIconAtlas` - icon atlas for hotbar and inventory
- `ChunkMesh` - GPU mesh containers for solid and transparent geometry
- `SkyRenderer` - sky gradient and celestial bodies
- `CloudRenderer` - 3D clouds as world geometry
- `SpriteAtlas` / `SpriteBatch` - 2D UI sprites
- `UiRenderer` / `UiTextRenderer` - HUD, inventory and all screen text
- `MobRenderer` - mob instance rendering from gameplay state

`RenderDevice` tries D3D11 first and can fall back to Vulkan or OpenGL.

## Game
- `GameApp` - application bootstrap and frame loop
- `WorldSession` - binds game state, world systems and render-side state
- `Player`, `Camera`, `InputState`, `InputHandler` - movement and look control
- `PlayerFeedbackSystem` - jump, step, run sound and particle feedback for player motion
- `CraftingSystem` - survival recipes and ingredient consumption for progression
- `SmeltingSystem` - furnace-style conversion rules for ores and basic materials
- `FoodCatalog` / `HungerSystem` / `SleepSystem` - hunger drain, eating and rest rules
- `HarvestSystem` - tool-aware block drops, break timing and resource gathering rules
- `BlockBreakSystem` - progressive block mining state and completion tracking
- `ToolRepairSystem` - repair cost lookup and durability restoration flow
- `LootTable` / `DropTable` helpers - block, mob, elite and structure reward tables
- `StructureGenerator` - rare points of interest and loot-bearing world features
- `ToolProfile` / `ToolCatalog` - harvest tier, damage bonus and utility metadata
- `MobType` / `MobDefinition` / `MobCatalog` - mob metadata, type lookup and spawn tuning
- `MobState` / `MobSystem` - mob simulation, combat, elite variants, rare threat profiles, physics, damage reactions and save-state management
- `MobNavigation` - shared steering helper for obstacle-aware movement choices
- `CharacterLiquidSampler` / `LiquidCurrentSampler` - liquid overlap sampling and water current push for player and mob physics
- `MobEvent` / `MobHitResult` / `MobAttackMode` - mob interaction plumbing
- `MobSoundSystem` - event-driven mob audio playback
- `TntSystem` / `TntEvent` - TNT priming, explosion resolution and save-state management
- `ExplosionSystem` - shared ray-sampled block destruction helper used by TNT and creeper explosions
- `Inventory` - hotbar, stack counts, tool durability and selected block state
- `WorldRaycaster` - block targeting and selection
- `ChunkManager` - chunk request lifecycle, chunk loading, unloading, neighbor refresh, mesh rebuilds and queued changes
- `DayNightCycle` - time-of-day progression
- `WeatherSystem` - weather state and atmosphere switching
- `SoundSystem` - block and fire SFX playback, including step/run/jump and block break/place
- `AmbientSoundSystem` - ambient, music and liquid audio events
- `SoundRegistry` - sound group lookup and naming
- `SpawnLocator` - spawn search and biome filtering
- `AssetAudit` - startup validation of required asset coverage

## IO
- `FileChunkStorage` - chunk files on disk
- `MobSaveData` - compact mob persistence records with elite flags and elite variants
- `WorldSave` - seed, player and mob save data, including legacy fallback formats and mob save version 3
- `HungerSaveData` - hunger persistence for the survival loop
- `ChunkSaveQueue` - background chunk save queue

## UI
- `HudState` - HUD values shared between gameplay and renderer
- `Localization` - RU/EN string lookup

## Audio
- `IAudioBackend` - audio backend abstraction
- `NaudioBackend` - Windows implementation
- `NoAudioBackend` - fallback implementation for non-audio runs

## Runtime Data Flow
1. `GameApp` reads config and creates `WorldSession`.
2. `WorldSession` creates world systems and render-facing helpers.
3. `ChunkManager` drains completed generation results, loads chunks, refreshes neighbors, queues visible meshes and consumes queued changes.
4. `MobSystem`, `HungerSystem` and `SleepSystem` update spawn logic, survival pressure, physics and combat on the main thread, then emit render instances and audio events.
5. `WorldEditor` is used by gameplay and physics systems for all block mutations.
6. `WorldLighting`, `FluidSystem`, `FallingBlockSystem` and `FireSystem` react to changes through the same mutation path, and `GameApp` drains fire events into particles and audio after queued block changes are processed.
7. `WorldRenderer` draws chunks, sky, clouds and HUD every frame, while `MobRenderer` draws mob instances.

## Threading
- Main thread: input, gameplay, simulation orchestration and rendering
- Chunk generation workers: terrain generation and completed-chunk handoff
- Mesh workers: greedy meshing
- Save queue workers: chunk persistence

## Invariants
- `GameConfig.LoadOrCreate` tolerates partial or invalid JSON and normalizes defaults before use.
- `GameConfig.Normalize` clamps audio spatial settings, TNT limits, mob event backlog, elite mob tuning and block-change budgets before runtime use.
- Item-like entries should stay out of world placement paths; use `BlockDefinition.IsPlaceable` to separate block items from utility items.
- Crafting and smelting should stay data-driven; recipe tables belong in dedicated helpers, not in the main loop.
- Resource gathering should flow through inventory stack helpers, not direct hotbar mutation.
- Use `WaterBlocks.IsWater` instead of comparing only `BlockId.Water` when code must match any water level.
- Use `WorldEditor` for block changes that must trigger lighting and mesh updates.
- Keep gameplay feedback isolated in `PlayerFeedbackSystem` instead of mixing it into the main loop.
- `SoundSystem` loads block sounds from the asset manifests and keeps block-specific audio pools separate from ambient audio.
- `FireSystem` emits fire events for ignition, crackle, consumption and extinguish so gameplay can keep audio/particles outside the simulation code.
- `SoundSystem` and `MobSoundSystem` are listener-based and use `SpatialAudio` for attenuation and stereo pan.
- `ChunkManager` owns requested/in-flight/loaded chunk state on the main thread; generation workers only enqueue completed chunks.
- `ChunkManager.ProcessChanges` enforces a per-frame block-change budget to avoid long stalls from mass explosions.
- Use `Ui.FontFile` for the single UI font; `Ui.FontInvertMask` is a fallback for atlas polarity issues.
- `WorldGenConfig.ForcedBiome` is parsed from a string, and `Any` means "no override".
- `WorldSave.LoadSeed` accepts text seeds and legacy binary `Int32` seeds.
- `MobConfig` is normalized before use so mob movement, spawn, elite, rare-elite and combat tuning stays within safe bounds.
- `MobSystem` owns mob AI, movement physics, combat resolution, sunlight burn, staggered damage reactions and render-instance generation.
- Creeper fuse behavior follows the Beta-style pattern: start within 3 blocks, require line of sight, and cancel when the player leaves the extended 7-block fuse range.
- `MobNavigation` keeps the steering math separate from world queries and sampling.
- `MobSoundSystem` only consumes `MobEvent` entries and does not mutate gameplay state.
- `TextureAtlasAnimator` keeps animated block textures in sync with atlas metadata instead of using ad hoc UV scrolling.
- `LiquidCurrentSampler` provides the shared water current vector used by player and mob movement so flowing water nudges entities consistently.
- `FireSystem` owns fire spread, burn-out, rain extinguish and explosion ignition so beta-style burning trees stay out of the main loop.
- `HarvestSystem` uses `ExplosionResistance` plus tier-aware tool multipliers to derive block break time, while `BlockBreakSystem` keeps mining progress on the gameplay thread.
- `ToolRepairSystem` owns tool-material repair mapping so `GameApp` only orchestrates inventory consumption and durability updates.
- `TntSystem` uses a ray-sampled explosion model with chain reactions and capped event/state queues.
- `ExplosionSystem` is the single block-destruction path for TNT-like explosions; mob explosions reuse it so creepers and TNT stay consistent.
- `WorldSave.SaveMobs` / `LoadMobs` serialize `world\mobs.dat` with legacy fallback formats, elite flags and elite variants.
- `AssetAudit` validates required mob textures and sounds at startup.
- `FileChunkStorage` currently supports chunk save versions `1` and `2`; older versions are rejected safely.

## Optimization Notes
- greedy meshing
- frustum culling
- texture atlases
- asynchronous world generation
- queued mesh rebuilds
- queued save writes
