---
SECTION_ID: docs.minecraft-clone.roadmap-milestones
TYPE: note
---

# Roadmap & Milestones

## Delivery Strategy

Ship a strong singleplayer vertical slice first, then expand carefully.
Every milestone must end in a playable state.

## Milestone 0 — Project Foundation

### Goal
Get the runtime skeleton working and remove startup risk.

### Deliverables
- app bootstrap on Windows 10+
- DX12 device + swapchain via Vortice
- input loop
- basic game state machine
- config and localization bootstrap
- asset loading path from `/assets`
- debug logging and basic counters

### Exit Criteria
- opens a window
- renders a clear frame
- can switch game states
- has dev-facing diagnostics

## Milestone 1 — World Core

### Goal
Get deterministic voxel world data working without final rendering quality concerns.

### Deliverables
- block registry
- world seed handling
- chunk `16x16x128`
- subchunk `16x16x16`
- chunk manager and lifecycle states
- deterministic terrain generation
- save/load scaffold

### Exit Criteria
- can create a world
- can request/generate nearby chunks
- chunk borders are consistent
- world state survives reload

## Milestone 2 — Playable Rendering

### Goal
Render the world reliably and stream it around the player.

### Deliverables
- CPU meshing per subchunk
- chunk mesh upload path
- opaque/cutout/transparent pass split
- camera and frustum culling
- texture mapping from `/assets`
- visible-set driven chunk draw

### Exit Criteria
- terrain is visible and navigable
- streaming near player works
- upload path is stable under movement
- no catastrophic hitching in normal traversal

## Milestone 3 — Core Interaction

### Goal
Make the sandbox actually playable.

### Deliverables
- first-person movement
- collision
- block break/place
- hotbar/inventory basics
- item/block selection
- local save after edits
- simple UI for pause/settings/world create/load

### Exit Criteria
- player can move, mine, and build
- edits persist after reload
- UI flow is usable without debug-only tools

## Milestone 4 — Beta Feel Pass

### Goal
Align gameplay and presentation with the Beta 1.7.3 spirit.

### Deliverables
- terrain tuning
- fog/atmosphere tuning
- day/night basics
- initial survival balancing
- simple world features like trees/ores as scoped
- visual and audio feedback polish if available in assets/scope

### Exit Criteria
- game feels recognizably Beta-inspired
- exploration and building are satisfying
- visuals are coherent, not placeholder-chaotic

## Milestone 5 — Frametime Hardening

### Goal
Stabilize the build technically.

### Deliverables
- performance HUD
- queue and upload budgeting
- streaming prioritization improvements
- memory caps and eviction tuning
- hitch investigations and fixes
- save-path hardening

### Exit Criteria
- frame pacing is meaningfully improved
- debug counters support profiling
- traversal and building remain stable under normal load

## Milestone 6 — Singleplayer Release Candidate

### Goal
Prepare the first serious playable release.

### Deliverables
- settings polish
- RU/EN localization pass
- world compatibility/versioning checks
- crash-safe save behavior
- onboarding and defaults polish
- bug fixing and content cleanup

### Exit Criteria
- new player can create, play, save, and reload a world cleanly
- key flows are understandable
- no critical data-loss or startup blockers remain

## Post-v1 — Co-op Foundation

### Goal
Extend the architecture without destabilizing singleplayer.

### Deliverables
- session layer
- replication boundaries
- authority model decision
- deterministic or server-owned world sync strategy

### Exit Criteria
- co-op prototype does not require rewriting chunk/render/save foundations

## Prioritization Rules

If schedule pressure appears:
1. protect world streaming
2. protect block interaction
3. protect save/load integrity
4. cut secondary features before core stability
5. delay co-op rather than weakening singleplayer foundation

## Definition of Done for Any Milestone

A milestone is done only if:
- it is playable
- debug counters exist for the new subsystem
- no obvious architectural contradiction was introduced
- the result can serve as a base for the next milestone without rewrite
