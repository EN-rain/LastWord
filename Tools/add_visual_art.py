#!/usr/bin/env python3
"""Add PackedScene visual instances to F1_Basement.tscn."""
import re
from pathlib import Path

ROOT = Path("/mnt/c/Users/LENOVO/Documents/Last-word-godot")
SCENE = ROOT / "Scenes/Floors/F1_Basement.tscn"

MESH_ASSETS = [
    ("floor", "res://Assets/Floor1/Meshes/floor.glb"),
    ("floor_detail", "res://Assets/Floor1/Meshes/floor-detail.glb"),
    ("wall", "res://Assets/Floor1/Meshes/wall.glb"),
    ("wall_half", "res://Assets/Floor1/Meshes/wall-half.glb"),
    ("wall_narrow", "res://Assets/Floor1/Meshes/wall-narrow.glb"),
    ("stones", "res://Assets/Floor1/Meshes/stones.glb"),
    ("rocks", "res://Assets/Floor1/Meshes/rocks.glb"),
    ("wood_support", "res://Assets/Floor1/Meshes/wood-support.glb"),
    ("wood_structure", "res://Assets/Floor1/Meshes/wood-structure.glb"),
    ("column", "res://Assets/Floor1/Meshes/column.glb"),
    ("barrel", "res://Assets/Floor1/Meshes/barrel.glb"),
    ("chest", "res://Assets/Floor1/Meshes/chest.glb"),
]


def transform_str(x, y, z, sx=1.0, sy=1.0, sz=1.0):
    return f"Transform3D({sx}, 0, 0, 0, {sy}, 0, 0, 0, {sz}, {x}, {y}, {z})"


def tile_floor(room_name, sx, sz, y_offset):
    out = []
    tile = 2.0
    count_x = max(1, int(round(sx / tile)))
    count_z = max(1, int(round(sz / tile)))
    half_x = (count_x - 1) * tile / 2
    half_z = (count_z - 1) * tile / 2
    for ix in range(count_x):
        for iz in range(count_z):
            px = -half_x + ix * tile
            pz = -half_z + iz * tile
            out.append(f'\n[node name="Mesh_Floor_{ix}_{iz}" type="Node3D" parent="World/Rooms/{room_name}" instance=ExtResource("6")]\n')
            out.append(f'transform = {transform_str(px, y_offset, pz)}\n')
    return out


def wall_segments(room_name, sx, sz, sy, doors):
    out = []
    seg_len = 2.0
    for side, z_base in [("N", sz/2 - 0.1), ("S", -sz/2 + 0.1)]:
        if side in doors:
            continue
        count = max(1, int(round(sx / seg_len)))
        half = (count - 1) * seg_len / 2
        for i in range(count):
            px = -half + i * seg_len
            out.append(f'\n[node name="Mesh_Wall_{side}_{i}" type="Node3D" parent="World/Rooms/{room_name}" instance=ExtResource("7")]\n')
            if side == "N":
                out.append(f'transform = {transform_str(px, sy/2, z_base)}\n')
            else:
                out.append(f'transform = Transform3D(-1, 0, 0, 0, 1, 0, 0, 0, -1, {px}, {sy/2}, {z_base})\n')
    for side, x_base in [("E", sx/2 - 0.1), ("W", -sx/2 + 0.1)]:
        if side in doors:
            continue
        count = max(1, int(round(sz / seg_len)))
        half = (count - 1) * seg_len / 2
        for i in range(count):
            pz = -half + i * seg_len
            out.append(f'\n[node name="Mesh_Wall_{side}_{i}" type="Node3D" parent="World/Rooms/{room_name}" instance=ExtResource("7")]\n')
            if side == "E":
                out.append(f'transform = Transform3D(0, 0, -1, 0, 1, 0, 1, 0, 0, {x_base}, {sy/2}, {pz})\n')
            else:
                out.append(f'transform = Transform3D(0, 0, 1, 0, 1, 0, -1, 0, 0, {x_base}, {sy/2}, {pz})\n')
    return out


def main():
    text = SCENE.read_text(encoding="utf-8")

    # Add ext_resources for meshes using IDs 6..N
    ext_lines = []
    for idx, (key, path) in enumerate(MESH_ASSETS, start=6):
        ext_lines.append(f'[ext_resource type="PackedScene" path="{path}" id="{idx}"]\n')

    # Insert before first [sub_resource
    sub_idx = text.find("[sub_resource")
    text = text[:sub_idx] + "".join(ext_lines) + "\n" + text[sub_idx:]

    # Update load_steps
    m = re.search(r'\[gd_scene load_steps=(\d+)', text)
    if m:
        old = int(m.group(1))
        new = old + len(MESH_ASSETS)
        text = text.replace(f"[gd_scene load_steps={old}", f"[gd_scene load_steps={new}", 1)

    # Parse rooms: name -> (sx, sy, sz), doors
    rooms = {}
    lines = text.splitlines(keepends=True)
    i = 0
    while i < len(lines):
        line = lines[i]
        match = re.match(r'\[node name="(Room_\d+_[^"]+)" type="Node3D" parent="World/Rooms"\]', line)
        if match:
            name = match.group(1)
            sx = sy = sz = None
            doors = set()
            j = i + 1
            while j < len(lines) and not lines[j].startswith("[node name="):
                l = lines[j]
                if l.startswith("RoomSize = Vector3("):
                    vals = re.findall(r'[-+]?[0-9]*\.?[0-9]+', l)
                    if len(vals) >= 3:
                        sx, sy, sz = map(float, vals[:3])
                j += 1
            rooms[name] = {"sx": sx, "sy": sy, "sz": sz, "doors": doors, "floor_idx": i + 1}
        i += 1

    # Find doorways per room
    for name in rooms:
        pattern = rf'\[node name="Doorway_([NSEW])" type="Area3D" parent="World/Rooms/{re.escape(name)}"'
        rooms[name]["doors"] = set(re.findall(pattern, text))

    # Build insertions before each room's Floor child
    insertions = []
    for name, data in rooms.items():
        sx, sy, sz = data["sx"], data["sy"], data["sz"]
        if sx is None:
            continue
        block = []
        block.extend(tile_floor(name, sx, sz, -sy/2 + 0.1))
        block.extend(wall_segments(name, sx, sz, sy, data["doors"]))
        if sx * sz >= 64:
            block.append(f'\n[node name="Mesh_Column" type="Node3D" parent="World/Rooms/{name}" instance=ExtResource("15")]\n')
            block.append(f'transform = {transform_str(0, -sy/2 + 0.1, 0)}\n')
        if "Flooded" not in name:
            block.append(f'\n[node name="Mesh_Prop" type="Node3D" parent="World/Rooms/{name}" instance=ExtResource("12")]\n')
            block.append(f'transform = {transform_str(sx*0.25, -sy/2 + 0.1, sz*0.25, 0.8, 0.8, 0.8)}\n')
        insertions.append((name, "".join(block)))

    # Apply insertions in reverse order to preserve indices
    for name, block in reversed(insertions):
        marker = f'\n[node name="Floor" type="StaticBody3D" parent="World/Rooms/{name}"]\n'
        idx = text.find(marker)
        if idx != -1:
            text = text[:idx] + block + text[idx:]

    # Props under World/Props
    props = "".join([
        f'\n[node name="Mesh_FloorDetail_Ritual" type="Node3D" parent="World/Props" instance=ExtResource("7")]\n',
        f'transform = {transform_str(18, 0.1, 4)}\n',
        f'\n[node name="Mesh_Rocks_Sump" type="Node3D" parent="World/Props" instance=ExtResource("9")]\n',
        f'transform = {transform_str(18, 0.1, -8)}\n',
        f'\n[node name="Mesh_WoodSupport_Boiler" type="Node3D" parent="World/Props" instance=ExtResource("10")]\n',
        f'transform = {transform_str(-6, 0.1, 40)}\n',
        f'\n[node name="Mesh_WoodStructure_Cold" type="Node3D" parent="World/Props" instance=ExtResource("11")]\n',
        f'transform = {transform_str(-6, 0.1, 28)}\n',
        f'\n[node name="Mesh_Stones_Flooded" type="Node3D" parent="World/Props" instance=ExtResource("8")]\n',
        f'transform = {transform_str(6, 0.1, -8)}\n',
        f'\n[node name="Mesh_Column_Crypt" type="Node3D" parent="World/Props" instance=ExtResource("15")]\n',
        f'transform = {transform_str(30, 0.1, 16)}\n',
    ])
    marker = '\n[node name="PLACEHOLDER_Furnace"'
    idx = text.find(marker)
    if idx != -1:
        text = text[:idx] + props + text[idx:]

    SCENE.write_text(text, encoding="utf-8")
    count = text.count("instance=ExtResource(")
    balanced = text.count("[") == text.count("]")
    print(f"Wrote {SCENE}")
    print(f"instance=ExtResource count: {count}")
    print(f"Brackets balanced: {balanced}")


if __name__ == "__main__":
    main()
