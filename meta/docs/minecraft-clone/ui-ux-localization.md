---
SECTION_ID: docs.minecraft-clone.ui-ux-localization
TYPE: note
---

# UI / UX / Localization

## Goal
Deliver a UI that feels **clean, readable, low-friction**, with support for **RU/EN** from the start.

## UX Principles
- **Gameplay first**: UI must never fight the player
- **Low cognitive load**
- **Controller support optional later; keyboard/mouse first**
- **Fast access to common actions**
- **Readable at multiple resolutions**
- **Preserve Beta simplicity, not Beta rough edges**

## Visual Direction
- Inspired by classic voxel sandbox UI
- Crisp typography
- Clear panel hierarchy
- Minimal animation
- Strong contrast
- Pixel-art-friendly presentation without forcing poor usability

## Required Screens
### Main Menu
- Continue
- Singleplayer / Worlds
- Settings
- Language
- Exit

### World Select
- create world
- rename/delete later if needed
- seed input
- basic world options
- last played / metadata display

### In-Game HUD
- crosshair
- hotbar
- selected item highlight
- health
- debug overlay optional
- interaction feedback if needed

### Inventory / Crafting
- drag/drop or click behavior
- stack split rules
- recipe output clarity
- tooltip support

### Pause Menu
- resume
- settings
- save/quit
- language should ideally be reachable from settings

### Settings
- graphics
- audio
- controls
- gameplay
- language
- accessibility basics

## UX Requirements
- Menus must be usable at 1080p and scale cleanly upward
- All interactive elements need visible focused/hover/pressed states
- Input rebinding should be planned even if not fully shipped in earliest build
- Error messages must be actionable, not technical dumps

## Localization Strategy
Support languages:
- English (`en`)
- Russian (`ru`)

Rules:
- no hardcoded player-facing strings in gameplay/UI code
- use stable localization keys
- fallback chain: requested language -> English
- keep strings context-aware and short enough for UI constraints

Example:
```json
{
  "menu.continue": "Continue",
  "menu.settings": "Settings"
}
```

## Font and Text Rules
- Font must support Latin + Cyrillic
- Avoid overly narrow UI layouts
- Text expansion must be tolerated
- Dynamic text should support pluralization strategy later if needed

## Content Guidelines
Tone:
- simple
- direct
- readable
- no joke text in critical UX paths

Use:
- short button labels
- consistent terminology
- same item/block names across HUD, inventory, settings, saves

## Accessibility Minimum
- adjustable UI scale
- adjustable mouse sensitivity
- subtitle hook support for later
- master brightness/gamma controls if rendering path allows
- avoid color-only state indicators when practical

## Technical Requirements
- UI state separated from gameplay state
- localization loaded from data, not code constants
- hot-reload for localization files is nice-to-have
- all settings persist per user profile

## Acceptance Criteria
- Full basic flow playable in RU and EN
- No clipped core UI text in supported languages
- HUD remains readable during gameplay
- Main menu -> world creation -> gameplay -> pause -> quit is smooth
