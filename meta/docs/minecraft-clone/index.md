---
SECTION_ID: docs.minecraft-clone.index
TYPE: note
---

# Minecraft Clone Documentation Pack

## Scope
Documentation for a Windows-only Minecraft Beta 1.7.3-level clone using **C# / .NET 10** and **DirectX 12 via Vortice**, with **singleplayer first**, **co-op later**, and **frametime-first** engineering.

## Files
- `vision.md` — product vision and boundaries
- `architecture-overview.md` — runtime modules, threading, data flow
- `rendering-dx12.md` — DX12 renderer strategy
- `world-chunks-generation.md` — chunking, generation, streaming, meshing
- `performance-budget.md` — frametime budgets and degradation rules
- `roadmap-milestones.md` — phased delivery plan
- `gameplay-beta173-scope.md` — gameplay scope and non-goals
- `save-system.md` — save architecture and persistence rules
- `networking-roadmap.md` — co-op preparation and later networking phases
- `ui-ux-localization.md` — UI/UX and RU/EN localization rules
- `audio-system.md` — audio architecture and requirements
- `modding-roadmap.md` — staged modding strategy
- `open-questions.md` — unresolved decisions to close next

## Recommended Read Order
1. `vision.md`
2. `roadmap-milestones.md`
3. `architecture-overview.md`
4. `world-chunks-generation.md`
5. `rendering-dx12.md`
6. `performance-budget.md`
7. remaining subsystem docs
8. `open-questions.md`

## Current Key Decisions
- Windows 10+
- C# / .NET 10
- DX12 via Vortice
- logical chunk `16x16x128`
- subchunk `16x16x16`
- stable frametime over peak FPS
- singleplayer first, co-op later
- Beta 1.7.3 spirit with modern 2026 UX
