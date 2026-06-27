# Step 1 Implementation Note: F1_Basement.tscn

**Ferment:** 019eff87-38d2-703a-aa09-4c100f2d1f45 — Floor 1 Greybox Phase 1
**Step:** 1 of N — Author F1_Basement.tscn greybox static geometry
**Date:** 2026-06-26

## Route Chosen

**Direct write (route B).** No godot-mcp tools were exposed in this builder agent's toolset (only bash / edit / find / grep / ls / read / write), so I authored the .tscn file by hand as text. This is reliable and the file is small enough (~180 lines) to be reviewable in one read.

## Files Created

- `Scenes/Floors/F1_Basement.tscn` — greybox geometry scene (179 lines, ~6.7 KB)
- `Scenes/Floors/` directory (new)
- `.kimchi/ferments/019eff87-38d2-703a-aa09-4c100f2d1f45/docs/step-1-implementation.md` (this file)

## Scene Structure

Root: `Node3D` named `F1_Basement`.

| Node path                                | Type           | Transform origin    | Mesh size            | Notes                                                    |
| ---------------------------------------- | -------------- | ------------------- | -------------------- | -------------------------------------------------------- |
| `Floor`                                  | StaticBody3D   | (0, 0, 0)           | PlaneMesh 20x20      | Floor collision is a thin BoxShape3D 20x0.2x20 at y=-0.1 |
| `LeftWall`                               | StaticBody3D   | (-10, 1.75, 0)      | BoxMesh 0.5x3.5x20   | BoxMesh shared with RightWall via sub_resource id       |
| `RightWall`                              | StaticBody3D   | (10, 1.75, 0)       | BoxMesh 0.5x3.5x20   | Shares BoxMesh_side_wall + BoxShape3D_side_wall         |
| `BackWall`                               | StaticBody3D   | (0, 1.75, -10)      | BoxMesh 20x3.5x0.5   | Carries two BreakableWindow child StaticBody3Ds         |
| `BackWall/BreakableWindowLeft`           | StaticBody3D   | (-3, 2, -10.05)     | BoxMesh 1.5x1.5x0.1  | On back wall surface                                    |
| `BackWall/BreakableWindowRight`          | StaticBody3D   | (3, 2, -10.05)      | BoxMesh 1.5x1.5x0.1  | Shares BoxMesh_window + BoxShape3D_window                |
| `CentralPillar`                          | StaticBody3D   | (0, 1.75, 0)        | BoxMesh 2x3.5x2      | Origin obstacle                                          |
| `Ceiling`                                | StaticBody3D   | (0, 3.75, 0)        | BoxMesh 20x0.5x20    | Sits flush on top of walls (y=3.5..4.0)                 |
| `Staircase/Step0`..`Step4`               | StaticBody3D   | (0, 0.15+0.3*i, 8+i)| BoxMesh 2x0.3x1      | i in 0..4; ascending out the open front                  |

## Resource Inventory

- **6 BoxMesh sub_resources:** `side_wall`, `back_wall`, `pillar`, `ceiling`, `step`, `window` — `side_wall`, `step`, and `window` are reused across multiple nodes (left/right walls share; 5 steps share; 2 windows share).
- **1 PlaneMesh sub_resource:** `floor`.
- **7 BoxShape3D sub_resources:** matching every distinct collision footprint.

## Layout Decisions

1. **Floor collision vs visual split.** PlaneMesh sits exactly at y=0; the BoxShape3D collision is centered at y=-0.1 with size 20x0.2x20 so the walkable surface is at y=0 (matches the visual top). This is the pattern NavigationRegion3D will look for when baking.
2. **Ceiling y=3.75, not y=3.5.** The spec narrative said "at y=3.5" but the detailed layout table specified center y=3.75 with thickness 0.5 — that puts the ceiling bottom flush with the wall tops (y=3.5). Followed the layout table.
3. **Staircase extends past z=10.** Front of the U-shape is intentionally open (z=+10 is the threshold). Steps run z=8..12 so the player climbs out of the basement. Step 4 tops out at y=1.5 (not the full wall height) — Step 2 will add NavigationRegion3D and any necessary landing/walk-off geometry if needed.
4. **BreakableWindow parent.** Each window is a child `StaticBody3D` of `BackWall` rather than a free-floating `MeshInstance3D`. This groups the visual+collision under one logical entity — when the future `BreakableWindow` script runs, it has a single node to disable, freeing the embedded glass. The slight wall-collision overlap (window at z=-10.05 vs wall span z=[-10.25,-9.75]) is harmless for NavMesh baking.
5. **Resource sharing.** Walls/steps/windows that share identical sizes share their BoxMesh and BoxShape3D sub_resources (different `id` strings per node are not needed; Godot's SubResource lookup handles reuse).

## Out of Scope (Step 1)

Per the plan: no NavigationRegion3D, no NoteSpawn markers, no ListenerSpawn, no RegistrationBoardArea, no scripts. Those are Step 2.

## Verification

```bash
test -f /mnt/c/Users/LENOVO/Documents/Last-word-godot/Scenes/Floors/F1_Basement.tscn && \
  grep -q 'BoxMesh' /mnt/c/Users/LENOVO/Documents/Last-word-godot/Scenes/Floors/F1_Basement.tscn && \
  grep -q 'PlaneMesh' /mnt/c/Users/LENOVO/Documents/Last-word-godot/Scenes/Floors/F1_Basement.tscn && \
  echo "VERIFIED"
```

Result: **VERIFIED**

Structural sanity counts (all paired): 13 StaticBody3D ↔ 13 MeshInstance3D ↔ 13 CollisionShape3D; 6 BoxMesh + 1 PlaneMesh + 7 BoxShape3D sub_resource defs; 5 staircase steps.

The scene `uid="uid://bf1basement0001"` is a placeholder — Godot will rewrite it on the first editor save/open.

## Deviations from the Plan

None of substance. Two micro-decisions to flag:

- **Staircase parent node.** Added a `Node3D` named `Staircase` as the parent of the 5 steps (rather than flattening them under root). This keeps the scene tree tidy and gives Step 2 a single transform handle to attach the staircase's NavMesh baked geometry under if desired.
- **BreakableWindow as StaticBody3D, not MeshInstance3D.** The plan said "Two BoxMesh 'BreakableWindow' rectangles" without specifying a body type. I chose `StaticBody3D` so each window carries its own collision, isolating the future "break the glass" logic to a single node that can disable its own `CollisionShape3D` without touching the wall.

Both deviations are minimal, additive, and reversible in Step 2 if the downstream code prefers a flatter structure.
