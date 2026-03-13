---
SECTION_ID: docs.minecraft-clone.audio-system
TYPE: note
---

# Audio System

## Goal
Provide a lightweight, reliable audio system that supports the core sandbox experience without hurting frametime.

## Audio Pillars
- **Responsiveness**: actions must sound immediate
- **Clarity**: gameplay-relevant sounds must be easy to parse
- **Mood**: ambience supports exploration without overwhelming it
- **Performance**: no heavy runtime audio processing on critical path

## Categories
### UI
- button hover/click
- open/close inventory
- error/invalid action feedback

### Player / World Interaction
- block break
- block place
- footsteps
- jump/land
- pickup/drop

### Environment
- wind/ambient beds if used
- cave ambience
- water/lava proximity
- weather later

### Creatures / Combat
- passive mob vocalizations
- hostile mob cues
- hit/hurt
- death
- explosion

### Music
- sparse ambient/background tracks
- avoid constant looping fatigue
- music should feel occasional, not intrusive

## Functional Requirements
- one-shot playback
- looping playback
- per-category volume controls
- positional audio for world sounds
- listener tied to player/camera
- concurrency limits to avoid spam

## Mix Controls
At minimum:
- master volume
- music volume
- SFX volume
- UI volume
- ambient volume

## Playback Rules
- high-priority sounds should not be dropped easily
- repetitive sounds need cooldown/variation handling
- same-sound spam should be capped
- distant sounds should attenuate cleanly

## Data-Driven Sound Events
Use event ids mapped to assets/settings.

Example:
```json
{
  "block.place.stone": {
    "clips": ["stone_place_01.ogg", "stone_place_02.ogg"],
    "volume": 0.9,
    "pitchMin": 0.98,
    "pitchMax": 1.02
    }
}
```

## Technical Notes
- Asset decoding/loading should avoid gameplay hitches
- Preload common UI and block interaction sounds
- Stream or lazily load large music assets
- Keep API simple: play, stop, set volume, update listener

## Future Extensions
- biome-based ambience
- underwater filtering
- reverb zones
- subtitle integration
- network-aware audio events for co-op

## Acceptance Criteria
- core actions have immediate audio feedback
- audio settings persist
- no audible stutter during normal traversal
- positional cues help gameplay instead of creating noise
