---
SECTION_ID: docs.minecraft-clone.open-questions
TYPE: note
---

# Open Questions

## Purpose
Track unresolved product/technical decisions that can materially affect scope, architecture, or production order.

## Gameplay
1. **Exact fidelity target**  
    Are we aiming for:
    - “Beta 1.7.3 feel”
    - “Beta 1.7.3 with selective modern fixes”
    - “Beta-inspired but not parity-driven”

2. **Survival scope at first playable**  
    Must first public/internal milestone include:
    - crafting
    - furnace/smelting
    - hostile mobs
    - death/respawn
    - day/night

3. **Redstone-like systems**  
    Is any logic/wiring planned at all, or fully post-v1?

## World / Tech
4. **World persistence layout**  
    Final choice:
    - per-chunk files
    - region files

5. **Lighting scope**  
    At launch:
    - basic block + sky light
    - simplified model
    - more advanced propagation later

6. **Fluid complexity**  
    How close to classic behavior do water/lava need to be in v1?

7. **Worldgen target**  
    Strict Beta-style terrain or Beta-inspired terrain with modern tuning?

## UX / Product
8. **UI identity**  
    How far can we modernize menus/HUD before it stops feeling right?

9. **Localization depth**  
    RU/EN for:
    - full UI only
    - UI + item/block names
    - all future content from day one

10. **Accessibility baseline**  
    Which of these are required in first shipping version:
    - UI scale
    - rebinding
    - subtitle support hooks
    - color-safe indicators

## Audio
11. **Audio asset strategy**  
    Use existing assets only, or plan for replacement/expansion pass?

12. **Music philosophy**  
    Sparse ambient tracks, fully dynamic system, or minimal launch music?

## Networking
13. **Co-op target shape**  
    First networking release should be:
    - LAN-like listen server
    - internet co-op via host
    - dedicated server deferred entirely

14. **Max player count goal**  
    Real target:
    - 2
    - 4
    - 8+
    This changes architecture and QA cost.

15. **Authority strictness**  
    Are we designing for trusted friends only, or eventual hostile/public environments?

## Modding
16. **Modding ambition**
    Long-term target:
    - resource/data packs only
    - light scripting
    - full mod API

17. **Save compatibility promise**
    What level of backward compatibility do we want between milestones?

## Production
18. **Vertical slice definition**
    What exact combination marks “core game exists”:
    - worldgen
    - movement
    - block place/break
    - inventory
    - save/load
    - one hostile mob
    - one complete gameplay loop

19. **Performance target hardware**
    What is the reference machine for frametime-first decisions?

20. **Release format**
    Internal prototype only, closed alpha, or early public build?

## Recommended Next Decisions
To unblock implementation fastest, decide these first:
1. fidelity target
2. first playable feature set
3. save layout
4. lighting/fluid scope
5. co-op target shape
6. modding ambition ceiling
