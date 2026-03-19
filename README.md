![MiniCRUFT banner](docs/github-banner.svg)

[![CI](https://github.com/STEPWICE/MiniCRUFT-C/actions/workflows/ci.yml/badge.svg)](https://github.com/STEPWICE/MiniCRUFT-C/actions/workflows/ci.yml)

# MiniCRUFT

MiniCRUFT is an experimental voxel sandbox game inspired by Minecraft Beta 1.7.3.
The project is written in C# (.NET 10) and uses Veldrid for rendering (D3D11 by default).

## Build
```bat
dotnet build src\MiniCRUFT.Game\MiniCRUFT.Game.csproj -c Release
```

## Run
```bat
run.bat
```

## Controls
- `W A S D` - movement
- `SPACE` - jump
- `Left click` - attack mobs or mine blocks; hold to mine with tier-based progress
- `C` - craft next available inventory recipe
- `V` - smelt next available inventory recipe
- `R` - rest until morning when it is safe and dark enough
- `R` in inventory - repair the selected tool if you have the matching material
- `E` - inventory
- `Right click` on food - eat and restore hunger
- `Right click` on a chest - open it and collect loot
- `F3` - debug HUD
- `F5` - biome menu
- `F6` - seed menu
- `Enter` - apply selection or regenerate
- `F11` - fullscreen
- `Esc` - close menus or exit

## Mobs
- `MobSystem` handles spawning, AI, movement physics, combat, sunlight burn, water and lava interaction, damage reactions, elite variants and save/load.
- Spawn rules now favor hostile mobs in shelter and passive mobs in the open, while passive mobs also flee nearby hostile mobs and hostile mobs keep chasing the last seen target for a short time.
- `MobRenderer` draws mob instances from gameplay state using blocky 3D mob models, per-part animation and sheep fur overlays.
- Mob attack, hurt and death events now spawn type-specific particle bursts, with staggered damage reactions and elite deaths adding a trophy burst on top of the normal loot feedback.
- `MobSoundSystem` plays ambient, step, hurt, death, attack and explosion sounds from `assets\minecraft\sounds\mob`.
- Creeper explosions now use the same ray-sampled block destruction path as TNT, including TNT chain reactions.
- Creeper fuse follows Beta 1.7.3-style rules: it starts only at close range, requires line of sight, and cancels if the player moves outside the extended fuse range.
- Water current nudges entities from flowing liquid levels instead of only applying slowdown and buoyancy.
- Explosion visuals now include a beta-style smoke cloud and flame burst using a synthetic smoke sprite plus animated fire atlas regions.
  - `Mob.*` in `config.json` controls spawn limits, movement tuning, combat timing, sky-exposure bias, pursuit memory, elite tuning and per-type weights.
  - Elite mobs and rare elite variants can drop `MobTrophy` and scaled bonus loot.
- Mob state is persisted in `world\mobs.dat`.

## Animated Textures
- `TextureAtlasAnimator` keeps animated block textures in sync with Minecraft-style `.mcmeta` metadata.
- Water, lava, fire and other animated atlas regions update at runtime instead of relying on UV scrolling tricks.

## Fire
- `FireSystem` handles flammable block ignition, spread, burn-out, rain extinguish and explosion seeding.
- Burning trees and leaf clusters now behave like the beta-era fire gameplay, and fire blocks render as tall animated flames.

## Survival Loop
- Hunger drains over time, food restores it, and starvation can damage the player.
- Crafting, smelting, harvesting and tool repair are data-driven and work through inventory stack helpers.
- Tools now matter for resource gathering and combat, wear down over time, and mining speed changes by tier so better tools feel faster as well as stronger.
- Rare structures, ruins, mine shafts and chests now use POI-specific loot profiles, so exploration rewards match the location instead of only depth and biome.
- Weather now feeds the survival loop too: rain shifts mob pressure, and storms can flash lightning and roll thunder.

## TNT And Audio
- `TntSystem` now uses a ray-sampled explosion pass with block resistance falloff, TNT chain reactions, and hard limits on primed TNT, event backlog, and affected blocks.
- `FireSystem` now emits beta-style smoke and spark bursts, plus ignite/crackle/extinguish audio cues, from the same fire events that drive burn spread.
- `Audio.*` in `config.json` controls voice count, spatial attenuation radius, stereo pan strength, and dedicated fire volume.
- `SoundSystem` and `MobSoundSystem` use listener-based 3D positioning so nearby sounds stay loud and distant sounds fade naturally instead of being cut off arbitrarily.
- `Save.MaxBlockChangesPerFrame` prevents large explosions from monopolizing a single frame.

## Project Layout
- `src\MiniCRUFT.Core` - config, shared types, logging, noise, assets
- `src\MiniCRUFT.World` - blocks, biomes, chunk data, worldgen, lighting, fluids
- `src\MiniCRUFT.Renderer` - chunk meshes, textures, HUD, text, sky and clouds
- `src\MiniCRUFT.Game` - game loop, input, player, inventory, world session, mobs and runtime systems
- `src\MiniCRUFT.IO` - chunk saves and world/mob save data
- `src\MiniCRUFT.UI` - shared HUD state and localization
- `src\MiniCRUFT.Audio` - audio backends
- `src\MiniCRUFT.Tests` - automated tests

## Docs
Project documentation lives in `docs`:
- `docs\readme.md` - gameplay overview and current feature summary
- `docs\architecture.md` - module architecture and runtime flow
- `docs\dev_guide.md` - development workflow and extension points
- `docs\world_generation.md` - current terrain and biome generation rules
- `docs\project_state.md` - compact state snapshot for future AI work

## Tests
Automated coverage currently includes:
- config load/save roundtrips and normalization
- partial legacy config JSON and invalid config fallback handling
- deterministic world generation for a fixed seed
- save/load smoke paths for seed, player and chunk data
- save/load smoke paths for mobs and mob state roundtrips
- mob steering and flee behavior
- legacy compatibility for position-only player saves and chunk files in versions 1 and 2
- legacy binary `seed.dat` support
