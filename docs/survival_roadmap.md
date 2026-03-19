# Survival Roadmap

This document tracks the next gameplay slice after the base voxel runtime.

## Current State
- Base survival loop is in place: hunger drains over time, food restores hunger, and `R` can skip to morning when it is safe and dark enough.
- Meat drops and smelting already support a simple food progression path.
- Tool wear is now part of the loop, so harvesting and combat consume equipment instead of leaving tools purely decorative.
- Chest and mob loot now feed the inventory loop instead of acting as dead-end rewards, and chest rewards vary by POI type.
- Weather now affects threat pacing too: rain changes spawn pressure, and storms can add lightning flashes plus thunder cues.
- Exploration now has more variety: surface ruins and underground mine shafts join camps, towers and cave caches.
- The HUD now points the player at the next useful procedure and shows an explicit milestone route, so the early-game loop reads as a sequence instead of separate systems.
- Crafting priorities now favor the survival path instead of whichever recipe appears first in the catalog.

## Goal
Turn the game from a sandbox of placeable blocks into a light survival loop:
- gather resources
- craft tools and utility blocks
- smelt ores and food
- explore structures and caves
- fight mobs for meaningful drops

## Module Boundaries

### `MiniCRUFT.Game`
Owns player-facing survival logic.
- inventory stack management
- crafting and smelting requests
- block harvest rules
- mob and structure drop handling
- tool damage and harvest bonuses
- next-step survival guidance
- milestone route text

### `MiniCRUFT.World`
Owns world generation and loot-bearing world features.
- ore distribution
- cave generation
- rare structures and points of interest
- surface feature placement

### `MiniCRUFT.Renderer`
Owns presentation only.
- inventory and hotbar rendering
- item icons
- block/tool visual feedback
- progression guidance and milestone overlay

### `MiniCRUFT.IO`
Owns persistence.
- player inventory counts
- future recipe unlocks or progression flags
- structure and loot persistence if needed later

## Early Gameplay Slice

### 1. Resource gathering
- Breaking stone, ore, wood and foliage should produce useful drops.
- Tools should improve harvest efficiency and unlock better drops.

### 2. Crafting
- Logs to planks.
- Planks to sticks, table and basic tools.
- Cobblestone to furnace.
- Food items and fuel should stay data-driven.

### 3. Smelting
- Raw ore to ingots.
- Basic food and utility conversions.
- Meat cooking should stay part of the same progression path as ore smelting.

### 4. Exploration rewards
- Rare surface structures.
- Small loot caches in caves or ruins.
- Better treasure stops in ruins, mine shafts and cave pockets.
- Better rewards from dangerous biomes and deeper terrain.

### 5. Enemy pressure
- Mob drops should support food and progression.
- Night and risky terrain should matter more than daytime wandering.
- Hostile mobs should create pressure around rest and exploration routes, while passive mobs should react to nearby threats instead of ignoring them.
- Storms should amplify that pressure instead of being visual-only.

## Implementation Notes
- Keep recipes data-driven.
- Keep item-like entries out of world placement unless a block is intentionally placeable.
- Prefer stack-based inventory changes over direct hotbar mutation.
- Avoid putting progression logic into renderer code.
