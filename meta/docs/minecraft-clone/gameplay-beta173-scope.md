---
SECTION_ID: docs.minecraft-clone.gameplay-beta173-scope
TYPE: note
---

# Gameplay Beta 1.7.3 Scope

## Goal
Recreate the **feel and gameplay loop** of Minecraft Beta 1.7.3, while using a modern technical foundation and keeping room for later co-op.

## Product Principles
- **Beta spirit first**: simple, readable, sandbox-heavy gameplay.
- **Frametime first**: stable frame pacing is more important than maximum feature count.
- **Singleplayer first**: all core systems must work offline before networking.
- **Modern UX, old-school gameplay**: preserve mechanics, improve clarity/accessibility where it does not change the game’s identity.

## In Scope for v1
### Core Loop
- Spawn into procedural world
- Gather blocks and basic resources
- Craft essential tools and blocks
- Build freely
- Survive day/night cycle
- Explore terrain and caves

### World
- Infinite-feeling chunked voxel world
- Biomes inspired by Beta era
- Basic terrain features:
    - hills
    - plains
    - forests
    - caves
    - water bodies
- Deterministic seed-based generation

### Blocks
- Standard solid blocks
- Transparent blocks
- Fluids at simple gameplay level
- Light-emitting blocks
- Gravity blocks if included in Beta-style target set

### Player
- Walking
- Sprinting only if intentionally approved later
- Jumping
- Camera look
- Block interaction
- Inventory
- Health
- Fall damage
- Basic survival rules

### Inventory and Crafting
- Hotbar
- Main inventory
- Stack rules
- Crafting grid
- Furnace-like progression can be phased if needed

### Survival
- Health system
- Damage sources:
    - fall
    - drowning
    - lava/fire
    - hostile mobs
- Respawn flow
- Simple death penalty

### Mobs
- Initial target:
    - pig
    - sheep
    - cow
    - zombie
    - skeleton
    - creeper
    - spider
- Basic pathing and combat
- Despawn/spawn rules tuned for playability, not full simulation complexity

### Time and Environment
- Day/night cycle
- Ambient fog/sky changes
- Weather is optional phase-2 scope
- Basic world lighting sufficient for gameplay readability

## Explicit Non-Goals for v1
- Full redstone parity
- Pistons
- Enchanting
- Nether/End
- Villages
- Complex farming simulation
- Command system
- Dedicated server
- Marketplace / live-service features
- Large mod API at launch

## Quality Bar
- World loads fast and streams smoothly
- Block placement/break feels instant and reliable
- Camera/input feels responsive
- Save/load is robust
- No frame spikes during normal traversal on target hardware

## Acceptance Criteria
- Player can start a new world and spawn reliably
- Player can place and break blocks
- Chunks stream in without obvious gameplay-breaking stalls
- Inventory and crafting support at least one complete survival loop
- Death and respawn work correctly
- A world can be saved and reopened with no corruption

## Nice-to-Have After Core
- Weather
- Beds
- More biome variety
- More mob behaviors
- Better ambient world polish
