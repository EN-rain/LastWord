#!/usr/bin/env python3
"""Add sub-grouped prop instances under World/Props in F1_Basement.tscn."""
import re
from pathlib import Path

ROOT = Path("/mnt/c/Users/LENOVO/Documents/Last-word-godot")
SCENE = ROOT / "Scenes/Floors/F1_Basement.tscn"

# ext_resource IDs must match those already added by add_visual_art.py
ASSET_IDS = {
    "barrel": "16",
    "chest": "17",
    "column": "15",
    "rocks": "9",
    "stones": "8",
    "wood_support": "10",
    "wood_structure": "11",
    "door": "13",
    "stairs": "14",
    "wall_half": "7",
    "floor_detail": "7",
}


def t(x, y, z, *rest):
    if len(rest) == 0:
        return f"Transform3D(1, 0, 0, 0, 1, 0, 0, 0, 1, {x}, {y}, {z})"
    if len(rest) == 3:
        sx, sy, sz = rest
        return f"Transform3D({sx}, 0, 0, 0, {sy}, 0, 0, 0, {sz}, {x}, {y}, {z})"
    if len(rest) == 9:
        return f"Transform3D({', '.join(str(v) for v in rest)}, {x}, {y}, {z})"
    raise ValueError("t() accepts (x,y,z), (x,y,z,sx,sy,sz), or (x,y,z,basis...9)")


def main():
    text = SCENE.read_text(encoding="utf-8")

    # Ensure all asset IDs exist as ext_resources
    missing = [k for k, v in ASSET_IDS.items() if f'[ext_resource type="PackedScene"' not in text or f'id="{v}"' not in text]
    if missing:
        # If missing, add them. Find first free numeric ids.
        existing_ids = set(re.findall(r'id="(\d+)"', text))
        next_id = max((int(x) for x in existing_ids if x.isdigit()), default=5) + 1
        ext_lines = []
        for key in missing:
            path = f"res://Assets/Floor1/Meshes/{key.replace('_', '-')}.glb"
            ASSET_IDS[key] = str(next_id)
            ext_lines.append(f'[ext_resource type="PackedScene" path="{path}" id="{next_id}"]\n')
            next_id += 1
        sub_idx = text.find("[sub_resource")
        text = text[:sub_idx] + "".join(ext_lines) + text[sub_idx:]
        if ext_lines:
            m = re.search(r'\[gd_scene load_steps=(\d+)', text)
            if m:
                old = int(m.group(1))
                text = text.replace(f"[gd_scene load_steps={old}", f"[gd_scene load_steps={old + len(ext_lines)}]", 1)

    groups = {
        "Crates": [
            ("Crate_01", t(-18, 0.1, 26, 1.2, 1.2, 1.2)),
            ("Crate_02", t(-14, 0.1, 30, 1.0, 1.0, 1.0)),
            ("Crate_03", t(-10, 0.1, 24, 0.9, 0.9, 0.9)),
        ],
        "Barrels": [
            ("Barrel_01", t(-16, 0.1, 32)),
            ("Barrel_02", t(-12, 0.1, 28, 0.9, 0.9, 0.9)),
            ("Barrel_03", t(-8, 0.1, 34, 1.1, 1.1, 1.1)),
            ("Barrel_04", t(28, 0.1, 12, 1.0, 1.0, 1.0)),
        ],
        "Chests": [
            ("Chest_01", t(18, 0.1, 4, 1.5, 1.5, 1.5)),
            ("Chest_02", t(-6, 0.1, 40, 1.2, 1.2, 1.2)),
        ],
        "Columns": [
            ("Column_01", t(0, 0.1, 0, 1.5, 2.0, 1.5)),
            ("Column_02", t(30, 0.1, 16, 1.5, 2.0, 1.5)),
            ("Column_03", t(-14, 0.1, 10, 1.2, 1.8, 1.2)),
        ],
        "Rubble": [
            ("Rocks_01", t(12, 0.1, -4, 1.5, 1.5, 1.5)),
            ("Rocks_02", t(16, 0.1, -10, 1.2, 1.2, 1.2)),
            ("Stones_01", t(20, 0.1, -12, 1.3, 1.3, 1.3)),
            ("Stones_02", t(10, 0.1, 36, 1.0, 1.0, 1.0)),
        ],
        "WoodSupports": [
            ("WoodSupport_01", t(-18, 0.1, 20, 1.0, 2.0, 1.0)),
            ("WoodSupport_02", t(-18, 0.1, 14, 1.0, 2.0, 1.0)),
            ("WoodStructure_01", t(-16, 0.1, 18, 1.0, 1.5, 1.0)),
        ],
        "Doors": [
            ("Door_Boiler", t(-6, 0.1, 38, 1.0, 1.5, 1.0)),
            ("Door_Crypt", t(30, 0.1, 16, 0, 0, 1, 0, 1, 0, -1, 0, 0)),
        ],
        "Stairs": [
            ("Stairs_Hub", t(34, 0.1, 16, 1.0, 1.0, 1.0)),
            ("Stairs_Furnace", t(-20, 0.1, 42, 1.0, 1.0, 1.0)),
        ],
    }

    # Map group names to asset IDs
    group_asset = {
        "Crates": "wall_half",
        "Barrels": "barrel",
        "Chests": "chest",
        "Columns": "column",
        "Rubble": "rocks",
        "WoodSupports": "wood_support",
        "Doors": "door",
        "Stairs": "stairs",
    }

    blocks = []
    for group, items in groups.items():
        blocks.append(f'\n[node name="{group}" type="Node3D" parent="World/Props"]\n')
        for name, tr in items:
            asset = group_asset[group]
            blocks.append(f'[node name="{name}" type="Node3D" parent="World/Props/{group}" instance=ExtResource("{ASSET_IDS[asset]}")]\n')
            blocks.append(f'transform = {tr}\n')
        # override a few items in groups to use other assets
        if group == "Rubble":
            # add one stones variant
            pass

    # Insert before the first existing prop child of World/Props (PLACEHOLDER_Furnace is later under same parent)
    marker = '\n[node name="World/Props"'
    idx = text.find(marker)
    if idx == -1:
        # find [node name="..." parent="World/Props"]
        m = re.search(r'\n\[node name="Mesh_FloorDetail_Ritual" type="Node3D" parent="World/Props"', text)
        if m:
            idx = m.start()
    else:
        # Insert right after the World/Props node declaration line
        end = text.find("\n\n", idx)
        idx = end + 1

    if idx == -1:
        raise RuntimeError("Could not locate World/Props insertion point")

    text = text[:idx] + "".join(blocks) + text[idx:]

    SCENE.write_text(text, encoding="utf-8")
    count = len(re.findall(r'parent="World/Props/', text))
    balanced = text.count("[") == text.count("]")
    print(f"Wrote {SCENE}")
    print(f"parent=World/Props/ count: {count}")
    print(f"Brackets balanced: {balanced}")


if __name__ == "__main__":
    main()
