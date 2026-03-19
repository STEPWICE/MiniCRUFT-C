# Project State

This file is a compact snapshot for future maintenance and AI-assisted work.

## Current Module Map
- `MiniCRUFT.Core` - configuration objects, logging, asset paths, noise, shared math types
- `MiniCRUFT.World` - blocks, biomes, chunk storage, worldgen, lighting, fluids and falling blocks
- `MiniCRUFT.Renderer` - chunk meshing, texture atlases, sky, clouds, HUD and text
- `MiniCRUFT.Game` - application bootstrap, player, input, inventory, world session, mobs, progression guidance, milestone routing and runtime systems
- `MiniCRUFT.IO` - world, mob and chunk save/load helpers
- `MiniCRUFT.UI` - HUD state, progression text, milestone text and localization
- `MiniCRUFT.Audio` - audio backend abstraction and platform implementations

## Important Runtime Invariants
- Block mutations should go through `WorldEditor` when lighting or mesh rebuilds must happen.
- `WaterBlocks.IsWater` is the correct way to match water levels, not `BlockId.Water` alone.
- `Ui.FontFile` is the single font source for all UI text.
- `WorldGenConfig.ForcedBiome` is a string override parsed at runtime; `Any` means "no override".
- Water physics, sand/gravel physics and lighting updates are coupled through queued block changes.

## Current High-Signal Systems
- Water now has source and level variants.
- Sand and gravel fall through non-solid blocks and water.
- Torch rendering uses a narrowed shaft and a small flame sprite.
- Hotbar and inventory draw block icons from an icon atlas.
- Ambient audio is split into block SFX, ambient, music, random and liquid groups.
- Mobs now spawn as zombies, creepers, cows, sheep and chickens with save/load, combat, physics, event-driven spatial audio, passive flee behavior, hostile pursuit memory, damage reactions, elite variants, rare elite threats, obstacle steering and blocky 3D rendering.
- Mob attack, hurt and death events now emit configurable particle bursts, with staggered damage reactions and elite deaths adding trophy feedback on top of the loot drop.
- TNT now uses a ray-sampled Minecraft Beta-style blast pass with capped state/event queues and a frame-budgeted block-change drain.
- World generation uses biome-aware height shaping plus smaller surface feature masks.
- Survival-loop now includes hunger, food consumption, rest, tool-aware harvesting, tier-based mining speed, tool durability, tool repair, crafting/smelting, resource drops, mob trophies from elite kills, rare elite threat drops, rare world points of interest like ruins and mine shafts, POI-specific chest loot, and storm weather pressure with lightning/thunder cues.
- Progression guidance now surfaces the next procedure in the HUD, while a milestone panel shows the longer survival route as a series of goals instead of a loose set of systems.
- Crafting recipe selection is now priority-driven so the HUD and inventory flow point toward the intended progression path instead of whichever recipe happens to appear first in the catalog.
- The current progression loop is: harvest resources, repair and upgrade tools, mine faster with better tiers, then use the stronger loadout against elite mobs, rare elite variants and other threats.
- Automated tests now cover config roundtrips, legacy config fallback behavior, deterministic world generation, save/load smoke paths, survival systems, and legacy player/chunk/seed compatibility.

## Files To Check First When Extending
- `src\MiniCRUFT.Core\GameConfig.cs`
- `src\MiniCRUFT.World\WorldGenerator.cs`
- `src\MiniCRUFT.World\SurfaceFeatureGenerator.cs`
- `src\MiniCRUFT.World\WorldLighting.cs`
- `src\MiniCRUFT.World\FluidSystem.cs`
- `src\MiniCRUFT.World\FallingBlockSystem.cs`
- `src\MiniCRUFT.Renderer\ChunkMeshBuilder.cs`
- `src\MiniCRUFT.Renderer\UiRenderer.cs`
- `src\MiniCRUFT.Renderer\ItemIconAtlas.cs`
- `src\MiniCRUFT.Game\MobSystem.cs`
- `src\MiniCRUFT.Game\MobSoundSystem.cs`
- `src\MiniCRUFT.Renderer\MobRenderer.cs`
- `src\MiniCRUFT.Game\GameApp.cs`
- `src\MiniCRUFT.Game\Inventory.cs`
- `src\MiniCRUFT.Game\CraftingSystem.cs`
- `src\MiniCRUFT.Game\SmeltingSystem.cs`
- `src\MiniCRUFT.Game\HungerSystem.cs`
- `src\MiniCRUFT.Game\SleepSystem.cs`
- `src\MiniCRUFT.Game\FoodCatalog.cs`
- `src\MiniCRUFT.Game\HarvestSystem.cs`
- `src\MiniCRUFT.Game\BlockBreakSystem.cs`
- `src\MiniCRUFT.Game\ToolRepairSystem.cs`
- `src\MiniCRUFT.Game\ProgressionMilestoneSystem.cs`
- `src\MiniCRUFT.Game\ProgressionGuideSystem.cs`
- `src\MiniCRUFT.Game\RecipeCatalog.cs`
- `src\MiniCRUFT.Core\ToolConfig.cs`
- `src\MiniCRUFT.Game\LootTable.cs`
- `src\MiniCRUFT.Game\WorldSession.cs`
- `src\MiniCRUFT.Core\MobConfig.cs`
- `src\MiniCRUFT.IO\WorldSave.cs`
- `src\MiniCRUFT.IO\HungerSaveData.cs`

## Known Cautions
- Avoid adding block-specific rules outside the registry or helper types when a generic helper already exists.
- Keep docs aligned with `config.json`; the config surface changes more often than the public API.
- Keep mob spawn, combat and movement tuning aligned between `MobConfig`, `MobSystem` and the save format. Spawn policy now also depends on sky exposure, and passive mobs will flee nearby hostile mobs.
- If new text disappears, check the font atlas setup before assuming the renderer is broken.
- If new fluid behavior is added, ensure it still triggers lighting and mesh rebuilds through the same mutation path.
