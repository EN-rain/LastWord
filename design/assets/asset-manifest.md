# Asset Manifest

> Last updated: 2026-06-27

## Progress Summary

| Total | Needed | In Progress | Done | Approved |
|-------|--------|-------------|------|----------|
| 1 | 0 | 0 | 1 | 1 |

## Asset Packs

| Pack | Source | License | Location | Status |
|------|--------|---------|----------|--------|
| KayKit Dungeon Remastered 1.0 | [GitHub](https://github.com/KayKit-Game-Assets/KayKit-Dungeon-Remastered-1.0) | CC0 | `res://Assets/KayKitDungeon/` | Imported |
| KayKit Furniture Bits 1.0 | [GitHub](https://github.com/KayKit-Game-Assets/KayKit-Furniture-Bits-1.0) | CC0 | `res://Assets/KayKitFurniture/` | Downloaded (.gltf/.bin format, not yet imported as PackedScenes) |

## Assets by Context

### Level: F1 Basement

| Asset ID | Name | Category | Source File | Status |
|----------|------|----------|-------------|--------|
| ASSET-001 | Floor wood dark tile | Environment | `Assets/KayKitDungeon/gltf/floor_wood_large_dark.gltf.glb` | Done |
| ASSET-002 | Foundation floor all sides | Environment | `Assets/KayKitDungeon/gltf/floor_foundation_allsides.gltf.glb` | Done |
| ASSET-003 | Foundation floor front | Environment | `Assets/KayKitDungeon/gltf/floor_foundation_front.gltf.glb` | Done |
| ASSET-004 | Foundation floor corner | Environment | `Assets/KayKitDungeon/gltf/floor_foundation_corner.gltf.glb` | Done |
| ASSET-005 | Stone wall segment | Environment | `Assets/KayKitDungeon/gltf/wall.gltf.glb` | Done |
| ASSET-006 | Stone wall corner | Environment | `Assets/KayKitDungeon/gltf/wall_corner.gltf.glb` | Done |
| ASSET-007 | Broken stone wall | Environment | `Assets/KayKitDungeon/gltf/wall_broken.gltf.glb` | Done |
| ASSET-008 | Wooden pillar | Environment | `Assets/KayKitDungeon/gltf/pillar.gltf.glb` | Done |
| ASSET-009 | Large barrel | Prop | `Assets/KayKitDungeon/gltf/barrel_large.gltf.glb` | Done |
| ASSET-010 | Stacked small barrels | Prop | `Assets/KayKitDungeon/gltf/barrel_small_stack.gltf.glb` | Done |
| ASSET-011 | Stacked crates | Prop | `Assets/KayKitDungeon/gltf/crates_stacked.gltf.glb` | Done |
| ASSET-012 | Wooden chest | Prop | `Assets/KayKitDungeon/gltf/chest.glb` | Done |
| ASSET-013 | Lit candle | Prop/Light | `Assets/KayKitDungeon/gltf/candle_lit.gltf.glb` | Done |
| ASSET-014 | Triple candle | Prop/Light | `Assets/KayKitDungeon/gltf/candle_triple.gltf.glb` | Done |
| ASSET-015 | Dungeon texture atlas | Texture | `Assets/KayKitDungeon/texture/dungeon_texture.png` | Done |

## Notes

- All KayKit assets are modular low-poly pieces sized to a 4 m tile grid.
- **F1–F4 floor scenes** now use *actual instanced PackedScene* nodes saved directly in the `.tscn` files (replacing the previous runtime-builder approach). This means the assets are visible in the Godot editor, not only at runtime.
- Each floor scene includes:
  - `WorldEnvironment` with dark ProceduralSky, ambient lighting, volumetric fog, glow/bloom post-processing
  - `DirectionalLight3D` for soft fill
  - 5 warm `OmniLight3D` point lights placed near candles
  - Modular walls (4 segments per side), corner pillars, and a rich prop layout (barrels, crates, chest, candles, furniture)
  - Static collision boxes matching wall positions
- Scene composition uses a 5 m tile grid for walls, with Y-axis rotation only (correct Transform3D math).
- The KayKit Furniture Bits pack is downloaded but not yet used — it uses `.gltf/.bin` format which requires Godot to import before referencing as PackedScene. Current scenes use furniture from the Dungeon pack (table_long, chair, bed_frame) as substitutes.
