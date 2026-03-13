---
SECTION_ID: plans.minecraft-clone-implementation-phase-1
TYPE: plan
STATUS: completed
PRIORITY: high
---

# Minecraft Clone Implementation Phase 1

GOAL: Create the initial .NET solution structure and starter code skeleton for the Minecraft clone.
TIMELINE: current session

## Task Checklist

### Phase 1: Solution map and boundaries
- [x] Confirm phase 1 scope and constraints
- [x] Get recommended project map and dependency boundaries

### Phase 2: Scaffold plan
- [x] Define exact initial project set and starter files
- [x] Create root solution/config files
- [x] Create src project structure and starter code
- [x] Create missing project shell files for `App/Game/World/Rendering/Persistence` (`.csproj`)
- [x] Wire project references across solution
- [x] Execute technical rename plan (`MinecraftClone` -> `MiniCRUFT`)

### Phase 3: Validation
- [x] Review resulting file tree for current checkpoint
- [*] Update next-step implementation backlog
- [x] Apply product naming/version metadata (`MiniCRUFT`, `0.1 Alpha`) into solution scaffold

## Decisions
- Project product name is `MiniCRUFT`
- Start versioning at `0.1 Alpha`
- Technical project/module prefix is now `MiniCRUFT` after shell/reference rename pass
- Keep runtime boundaries explicit: App / Platform / Game / World / Rendering / Persistence
- Keep networking/modding/audio out of phase 1
- Chunk model fixed at `16x16x128`, subchunk `16x16x16`
- Frametime-first boundaries from docs stay mandatory

## Owners
- Archy: solution map and dependency boundaries
- Cody: starter skeleton and minimal file set

## Success Criteria
- [x] Root solution/config files exist
- [x] Starter projects exist under `src/`
- [x] Project references follow agreed boundaries
- [x] Initial code skeleton is ready for next implementation step

## Current Checkpoint
- Created: `MiniCRUFT.sln`, `global.json`, `Directory.Build.props`, `Directory.Build.targets`
- Product/version metadata remains fixed as `MiniCRUFT` / `0.1 Alpha`
- Runtime metadata source remains `src/MiniCRUFT.App/AppInfo.cs`
- Renamed project folders, `.csproj` files, assembly/root namespace values, and solution entries from `MinecraftClone.*` to `MiniCRUFT.*`
- Wired project references remain as agreed: `App -> Platform, Game, Rendering, Persistence`; `Game -> World`; `Rendering -> World`; `Persistence -> World`
- Final code sweep removed remaining `MinecraftClone` namespace/using references from `src/`
- Build validation passes on `MiniCRUFT.sln`; added missing renamed platform stub at `src/MiniCRUFT.Platform/PlatformServices.cs`

## Blockers
- None for the current shell/materialization step.
