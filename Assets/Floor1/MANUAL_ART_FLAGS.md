# Floor 1 — Manual Art Flags

All previously tagged CSGBox3D placeholders have been replaced with downloaded Kenney mini-dungeon (shifty-dungeon) mesh assets. Walls, floors, and ceilings remain CSGBox3D collision geometry.

## Replaced placeholder → mesh asset mapping

| Original placeholder | Replaced by | Asset file(s) | Scene path |
|---|---|---|---|
| `PLACEHOLDER_Furnace` | Furnace rock/column assembly | `rocks.glb`, `stones.glb`, `column.glb` | `World/Props/Furnace` |
| `PLACEHOLDER_Intercom` | Wall-mounted intercom box | `chest.glb` (scaled) | `World/Props/Intercom/Intercom_Box` |
| `PLACEHOLDER_Chains` | Hanging banner + trap detail | `banner.glb`, `trap.glb` | `World/Props/HangingChains` |
| `PLACEHOLDER_Puddle` | Muddy puddle surface | `dirt.glb` (flattened) | `World/Props/Puddle/Puddle_Dirt` |
| `PLACEHOLDER_Shelves` | Wooden shelves | `wood-structure.glb` | `World/Props/Shelves/Shelves_Wood` |
| `PLACEHOLDER_Workbench` | Workbench table/legs | `wood-structure.glb`, `wood-support.glb` | `World/Props/Workbench` |
| `PLACEHOLDER_Pipes` | Ceiling pipes | `column.glb` (scaled thin) | `World/Props/Pipes` |

## Prop categories using downloaded assets

- `Crates` — `chest.glb`
- `Barrels` — `barrel.glb`
- `Chests` — `chest.glb`
- `Columns` — `column.glb`
- `Rubble` — `rocks.glb`, `stones.glb`
- `WoodSupports` — `wood-support.glb`, `wood-structure.glb`
- `Doors` — `door.glb`
- `Stairs` — `stairs.glb`
- `Coins` — `coin.glb`
- `Traps` — `trap.glb`
- `Furnace`, `Intercom`, `HangingChains`, `Puddle`, `Shelves`, `Workbench`, `Pipes` — assembled from the assets above.

## Hand-authored scene elements

- `Scenes/Floors/F1_Basement.tscn` — hand-written/generated room layout, collision shapes, zones, waypoints, lighting, audio, and prop placement.
- `Tools/build_f1_basement.py` — custom generator that reproduces the canonical basement scene.
- `Tools/add_visual_art.py` — custom batch placement script for modular mesh instances.
- `Tools/add_props.py` — custom batch placement script for prop sub-groups.
- `Tools/fix_props.py` — replacement pass that swapped placeholders for downloaded mesh instances.
- `Scripts/World/StaircaseWinZone.cs` — custom win-trigger script for the staircase Area3D.
- `Assets/Floor1/Floor1_Layout.md` — ASCII top-down map.
- `Assets/Floor1/Floor1_RoomManifest.md` — room table, connectivity graph, and design notes.
