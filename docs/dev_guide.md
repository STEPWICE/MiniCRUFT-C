# DEV_GUIDE.md

## Build
```bat
dotnet build src\MiniCRUFT.sln -c Release
```

## Run
```bat
run.bat
```

## Tests
```bat
dotnet test src\MiniCRUFT.Tests\MiniCRUFT.Tests.csproj -c Release
```

## Logs
- `logs\run.log` - console output
- `logs\engine.log` - engine events and errors

## Add A Block
1. Add or update the `BlockId` entry.
2. Register the block in `BlockRegistry`.
3. Add textures under `assets\minecraft\textures\block`.
4. Keep source textures at 16x16 when possible.
5. If the block needs special geometry, update `ChunkMeshBuilder`.

## Add A Biome
1. Add the biome to `BiomeId` and `BiomeRegistry`.
2. Set climate values and colors in `BiomeRegistry`.
3. Wire it into `WorldGenerator` and `WorldHeightSampler`.
4. Add any biome-specific surface features in `SurfaceFeatureGenerator` or `TreeGenerator`.

## Add A Tree Or Feature
1. Add the shape in `TreeGenerator` or `SurfaceFeatureGenerator`.
2. Call it from `WorldGenerator` using biome checks and noise masks.
3. Keep rare features noise-driven so they remain spatially coherent.

## Add Localization
1. Update `assets\lang_ru.json` and `assets\lang_en.json`.
2. Use keys through `Localization` only.

## Add Audio
- Base block sounds come from `assets\minecraft\sounds\dig` and `assets\minecraft\sounds\step`.
- Ambient and music tracks are managed by `AmbientSoundSystem`.
- Mob sounds are loaded by `MobSoundSystem` from `assets\minecraft\sounds\mob`.
- If a new block needs sounds, add it to `SoundRegistry` and verify the asset naming convention.

## Add A Mob
1. Add or update `MobType`, `MobDefinition` and `MobCatalog` in `MiniCRUFT.Game`.
2. Keep mob AI, movement, combat and physics in `MobSystem`; use `MobNavigation` for steering and add tunables to `MobConfig`.
3. Use `AlertRadius` and `FleeSpeed` in `MobDefinition` for passive flee behavior.
4. Update `MobRenderer` if the mob needs a new render treatment or tint.
5. Add textures under `assets\minecraft\textures\entity` and sounds under `assets\minecraft\sounds\mob`.
6. Persist new mob state through `MobSaveData` / `WorldSave` and add a save-load roundtrip test.
7. Extend `AssetAudit` if the mob requires mandatory assets.

## Add World Generation Tuning
The main knobs live in `WorldGenConfig` and `config.json`:
- `SeaLevel`, `BaseHeight`, biome height scales and biases
- river width, depth, warp strength and influence
- trail, tree cluster and surface feature noise
- `ForcedBiome` for deterministic test runs

When checking worldgen behavior, remember these controls:
- `F3` toggles the debug HUD
- `F5` opens the biome menu
- `F6` opens the seed menu
- `Enter` applies the selected biome or seed
- `F11` toggles fullscreen

## Add UI Or HUD Work
- `UiConfig` owns scaling, inventory layout, item name placement and font settings.
- `Ui.FontFile` should point to a single TTF that supports Cyrillic and Latin.
- If the font atlas renders incorrectly, test `Ui.FontInvertMask`.
- Keep the selected item label above the hotbar, not on top of slot icons.

## Add Fluid Or Falling-Block Behavior
- Water uses `FluidConfig` and `FluidSystem`.
- Any code that must treat all water variants equally should use `WaterBlocks.IsWater`.
- Sand and gravel updates go through `FallingBlockSystem`.
- Mutations should go through `WorldEditor` so lighting and mesh rebuilds are triggered.

## Add Save Or Load Behavior
- World metadata lives in `WorldSave`.
- Chunk data is written through `ChunkSaveQueue`.
- Keep save-compatible changes versioned or backward-safe.
- `seed.dat` must keep text and legacy binary `Int32` compatibility.
- `player.dat` must keep the current `v2` format and the legacy position-only reader.
- `world\mobs.dat` stores mob state through `MobSaveData` and should stay versioned.
- `chunk_X_Z.dat` currently supports versions `1` and `2`; newer save formats should be versioned explicitly.

## Current Configuration Surface
`config.json` currently covers:
- windowing, seed and paths
- player movement
- day/night and weather
- audio groups and intervals
- save queue behavior
- spawn search
- mob spawning, combat, movement tuning and weights
- falling blocks and water fluids
- renderer fog, palette and foliage tuning
- atmosphere and clouds
- HUD, inventory and text sizing
- world generation and biome shaping
- partial legacy JSON inputs are accepted, then normalized to fill missing sections

## Profiling
Watch:
- chunk generation time
- mesh rebuild time
- save queue backlog
- number of loaded chunks
- repeated lighting rebuilds

## Color Pipeline
- All lighting and color math is handled in linear space.
- Output is converted back to sRGB for presentation.

## Config Safety
- `GameConfig.LoadOrCreate` should not throw on invalid config files; it falls back to defaults and logs the problem.
- Keep new config sections nullable-safe and add normalization rules for any new nested settings.
- If you add a new persisted field, add a test that covers both save/load roundtrip and fallback behavior.

## Code Style
- `PascalCase` for types and public methods
- `camelCase` for locals
- one class per file
- keep the responsibilities separated
