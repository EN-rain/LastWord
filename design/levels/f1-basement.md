# Level Design — F1 Basement

> **Location**: Ashford Estate, servant cellar / storage basement
> **Scale**: 20 m × 20 m play area, 3.5 m ceiling
> **Mood**: Damp, dim, oppressive. Flickering candlelight, exposed stone foundation, forgotten storage.
> **In-Game Purpose**: Tutorial/intro floor. Players learn movement, stealth, note collection, and registration while the Listener patrols.

## Layout

Rectangular stone cellar with a heavy central wooden pillar.

```
   z=-10                         z=10
   +-----------------------------------+
   |  [Barrels]    [Pillar]   [Crates] |  x=-10
   |                                   |
   |  [Note]                           |  back wall with two
   |                                   |  narrow windows
   |              [Stairs up]          |
   |                                   |
   +-----------------------------------+
```

### Bounds
- Floor: y=0 plane, 20 m × 20 m
- Ceiling: y=3.5 m
- Walls: perimeter stone foundation walls (0.5 m thick)
- Central pillar: 2 m × 2 m at origin
- Staircase: front-center (z=+10) ascending out
- Windows: back wall (z=-10), x=±3 m, 1.5 m × 1.5 m

## Visual Theme

**Reference**: KayKit Dungeon Remastered (CC0), repurposed as a Victorian mansion cellar.

- **Floor**: `floor_wood_large_dark` planks for the main area, with `floor_foundation_*` pieces around the edges where the foundation meets dirt.
- **Walls**: `wall_*` stone foundation blocks along perimeter; windows cut out using smaller wall segments.
- **Pillar**: `pillar` or `pillar_decorated` at center.
- **Ceiling**: dark wooden beam plane (kept as simple collision-only ceiling).
- **Props**: barrels, crates, chests, candles/torches for pools of light.

## Lighting

- Ambient: very low (`#1a1a20`, 0.05 intensity)
- Warm point lights from candle clusters
- Two narrow light shafts from the back windows (cool moonlight)
- Stairwell slightly brighter to suggest exit

## Gameplay Elements

| Element | Position | Asset / Implementation |
|---------|----------|------------------------|
| RegistrationBoard | Front-center, near stairs | `Scenes/RegistrationBoard.tscn` |
| NoteSpawn_NE | NE corner near crates | marker |
| NoteSpawn_NW | NW corner near barrels | marker |
| NoteSpawn_SE | SE corner | marker |
| NoteSpawn_SW | SW corner | marker |
| ListenerSpawn | Center-back (z=-6) | marker |
| CreakZone_North | x=0, z=-6 | `CreakZone.cs` |
| CreakZone_East | x=+6, z=0 | `CreakZone.cs` |
| CreakZone_West | x=-6, z=0 | `CreakZone.cs` |

## Assets Used

All from `res://Assets/KayKitDungeon/gltf/`, CC0 (KayKit Dungeon Remastered).

- `floor_wood_large_dark`
- `floor_foundation_front`
- `floor_foundation_corner`
- `wall_corner` / `wall_*`
- `pillar`
- `barrel_large`, `barrel_small_stack`
- `crates_stacked`
- `chest`
- `candle_lit`, `candle_triple`

## Technical Notes

- Modular tiles are 4 m units where possible; floor tiles sized/rotated to fit 5 m grid.
- Existing `StaticBody3D` collision boxes remain for gameplay physics; visual meshes are KayKit instances.
- `FloorNavigationBaker` bakes navmesh from existing collision geometry.
