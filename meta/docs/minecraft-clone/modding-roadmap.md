---
SECTION_ID: docs.minecraft-clone.modding-roadmap
TYPE: note
---

# Modding Roadmap

## Goal
Do **not** build full modding at launch, but avoid shutting the door on it. Prioritize a stable data-driven foundation first.

## Principle
Ship a solid game first. Add modding in layers:
1. data overrides
2. content packs
3. scripting/hooks
4. advanced API only if justified

## Phase 0 — Mod-Friendly Foundations
Do now:
- stable ids for blocks/items/entities
- data-driven registries where practical
- externalized localization
- externalized block/item definitions where feasible
- avoid gameplay constants scattered across code

## Phase 1 — Resource / Data Packs
Lowest-risk early mod support:
- textures
- audio replacements
- localization packs
- UI style tweaks if safe
- configurable gameplay data for non-code content

Good first targets:
- block definitions
- item definitions
- recipes
- loot tables later
- biome parameters later

## Phase 2 — Scripted Extension Layer
Only after core gameplay stabilizes.

Potential scope:
- simple scripted events
- custom items/blocks with limited behavior
- worldgen parameter mods
- UI additions in restricted zones

Requirements first:
- sandboxing
- versioned API
- error isolation
- mod load order rules

## Phase 3 — Full Mod API
Only if community demand justifies cost.

Would require:
- supported lifecycle hooks
- server/client authority rules
- serialization contract
- compatibility policy
- tooling/docs/examples

## Non-Goals for Early Versions
- arbitrary native code plugins
- unrestricted runtime patching
- guaranteed compatibility across fast-changing builds
- complex mod networking sync in first co-op releases

## Recommended Technical Direction
- keep engine systems internal
- expose data formats before exposing code hooks
- prefer declarative mod content over imperative scripts
- version registries and asset paths clearly

## Risks
- freezing internal architecture too early
- maintaining unstable public API
- save compatibility problems
- security/stability issues from unrestricted code mods

## Acceptance Criteria for Starting Real Mod Support
- ids and registries are stable
- save format is versioned
- content loading pipeline is data-driven
- localization/assets are already externalized
- internal gameplay systems are no longer being rewritten every week
