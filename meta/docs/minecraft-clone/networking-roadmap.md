---
SECTION_ID: docs.minecraft-clone.networking-roadmap
TYPE: note
---

# Networking Roadmap

## Goal
Ship **singleplayer first**, but make core game logic compatible with later co-op without major rewrite.

## Strategy
- Build gameplay systems with **authority boundaries**
- Treat local singleplayer as **server + client in one process** conceptually
- Delay real networking until core simulation is stable

## Phase 0 — Singleplayer Foundation
Required before any networking work:
- deterministic-enough simulation rules
- chunk streaming ownership model
- serializable player/world state
- stable inventory logic
- event-driven gameplay actions

Technical rules:
- separate simulation state from presentation
- avoid UI directly mutating world state
- route player actions through command/use-action layer

## Phase 1 — Architecture Prep
Add networking-ready seams without shipping co-op yet:
- clear world authority model
- entity ids stable across serialization/runtime
- input commands represented as data
- game events replicated conceptually, even if local only
- spawn/despawn and inventory transactions go through server-style validation paths

## Phase 2 — Listen Server Prototype
Target:
- 2-4 player co-op
- host plays locally
- no dedicated server yet

Scope:
- player join/leave
- transform replication
- block place/break replication
- inventory sync
- chunk interest management
- basic hostile/passive mob sync

Out of scope:
- anti-cheat
- large player counts
- public internet hardening
- mod compatibility guarantees

## Phase 3 — Stability Pass
- lag compensation only if needed
- bandwidth reduction
- replication priority tuning
- disconnect/reconnect handling
- save/load with multiple players
- desync detection and correction

## Phase 4 — Optional Dedicated Server
Only after co-op is fun and stable.
Adds:
- headless runtime
- server config
- admin controls
- world persistence ownership on server side

## Network Model
Recommended model:
- **authoritative server**
- client sends intent/actions
- server validates and applies
- client predicts only where needed for responsiveness

## Replication Priorities
Highest:
- player state
- nearby block changes
- combat-relevant entity state

Medium:
- inventory deltas
- chunk presence/contents near player

Lower:
- distant entities
- cosmetic events
- ambience

## Key Risks
- retrofitting authority too late
- chunk streaming + replication complexity
- inventory desync
- mob AI divergence between peers
- save ownership conflicts in host migration scenarios

## Rules for Current Development
Even in singleplayer:
- never let UI mutate inventory directly
- never let renderer own gameplay truth
- use stable ids for entities/chunks/items
- keep serialization and runtime schemas aligned

## Exit Criteria for Starting Real Co-op
Do not start phase 2 until:
- singleplayer save/load is solid
- core survival loop is fun
- chunk system is stable under traversal
- entity/inventory systems are internally consistent
- major gameplay rewrites are unlikely
