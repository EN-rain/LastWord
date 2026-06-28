#!/usr/bin/env python3
"""Generate Scenes/Floors/F1_Basement.tscn — 20-room asymmetric basement."""
from pathlib import Path

ROOT = Path("/mnt/c/Users/LENOVO/Documents/Last-word-godot")
SCENE = ROOT / "Scenes/Floors/F1_Basement.tscn"

ROOMS = [
    ("Room_01_Furnace",        (-16, 0, 40), (10, 5, 10), "Furnace",   "HUMID",    False, False, 2, True,  False, {"N": (0, 2, 5), "E": (5, 2, 0)}),
    ("Room_02_BoilerAnte",     ( -6, 0, 40), ( 8, 4,  8), "Utility",   "HUMID",    False, False, 3, False, True,  {"S": (0, 2, -4), "N": (0, 2, 4), "E": (4, 2, 0)}),
    ("Room_03_CoalBin",        (-16, 0, 28), ( 8, 4,  8), "Storage",   "DEAD",     False, False, 2, True,  True,  {"S": (0, 2, -4), "E": (4, 2, 0)}),
    ("Room_04_ColdStorage",    ( -6, 0, 28), ( 8, 4,  8), "ColdStorage","COLD",    False, False, 4, True,  True,  {"W": (-4, 2, 0), "E": (4, 2, 0), "N": (0, 2, 4), "S": (0, 2, -4)}),
    ("Room_05_WineCellar",     (  6, 0, 40), (10, 4,  8), "Storage",   "DEAD",     False, False, 3, True,  True,  {"W": (-5, 2, 0), "N": (0, 2, 4), "E": (5, 2, 0)}),
    ("Room_06_Laundry",        (  6, 0, 28), ( 8, 4,  8), "Utility",   "WET",      False, False, 3, False, True,  {"S": (0, 2, -4), "W": (-4, 2, 0), "N": (0, 2, 4)}),
    ("Room_07_ServantHall",    ( -6, 0, 16), (10, 4, 10), "Corridor",  "NEUTRAL",  True,  True,  4, False, True,  {"S": (0, 2, -5), "N": (0, 2, 5), "W": (-5, 2, 0), "E": (5, 2, 0)}),
    ("Room_08_Pantry",         ( 18, 0, 40), ( 6, 4,  6), "Storage",   "DEAD",     True,  False, 2, True,  True,  {"W": (-3, 2, 0), "N": (0, 2, 3)}),
    ("Room_09_KitchenPrep",    ( 18, 0, 28), ( 8, 4,  8), "Utility",   "WET",      True,  False, 2, False, True,  {"S": (0, 2, -4), "W": (-4, 2, 0)}),
    ("Room_10_CentralHall",    (  6, 0, 16), (10, 5, 10), "Hall",      "REVERB",   False, True,  4, False, True,  {"W": (-5, 2, 0), "E": (5, 2, 0), "N": (0, 2, 5), "S": (0, 2, -5)}),
    ("Room_11_EastCorridor",   ( 18, 0, 16), ( 8, 4,  8), "Corridor",  "NEUTRAL",  False, True,  3, False, True,  {"W": (-4, 2, 0), "N": (0, 2, 4), "S": (0, 2, -4)}),
    ("Room_12_Crypt",          ( 30, 0, 16), ( 8, 5,  8), "Crypt",     "DEAD",     False, False, 2, True,  True,  {"W": (-4, 2, 0), "N": (0, 2, 4)}),
    ("Room_13_RitualChamber",  ( 18, 0,  4), (10, 5,  8), "Ritual",    "DEAD",     False, False, 3, True,  True,  {"S": (0, 2, -4), "E": (5, 2, 0), "W": (-5, 2, 0)}),
    ("Room_14_NorthStore",     (  6, 0,  4), ( 8, 4,  8), "Storage",   "DEAD",     False, False, 3, True,  True,  {"N": (0, 2, 4), "E": (4, 2, 0), "S": (0, 2, -4)}),
    ("Room_15_WestCorridor",   ( -6, 0,  4), ( 8, 4,  8), "Corridor",  "NEUTRAL",  False, True,  3, False, True,  {"E": (4, 2, 0), "N": (0, 2, 4), "S": (0, 2, -4)}),
    ("Room_16_StorageNiche",   (-18, 0,  4), ( 6, 4,  6), "Storage",   "DEAD",     False, False, 2, True,  True,  {"E": (3, 2, 0), "N": (0, 2, 3)}),
    ("Room_17_SecretNiche",    (-18, 0, -8), ( 6, 4,  6), "Secret",    "DEAD",     False, False, 2, False, True,  {"S": (0, 2, -3), "E": (3, 2, 0)}),
    ("Room_18_FloodedTunnel",  (  6, 0, -8), (10, 4,  8), "Flooded",   "WET",      False, False, 2, False, False, {"N": (0, 2, 4), "S": (0, 2, -4)}),
    ("Room_19_SumpPit",        ( 18, 0, -8), ( 6, 4,  6), "Flooded",   "WET",      False, False, 2, False, False, {"W": (-3, 2, 0), "S": (0, 2, -3)}),
    ("Room_20_StaircaseHub",   ( 30, 0,  4), (10, 5, 10), "Staircase", "REVERB",   False, True,  3, False, True,  {"W": (-5, 2, 0), "S": (0, 2, -5), "N": (0, 2, 5)}),
]

ROOM_META = {}
for name, pos, size, rtype, acous, creak, patrol, exits, cover, investigate, doors in ROOMS:
    ROOM_META[name] = {
        "pos": pos, "size": size, "type": rtype, "acous": acous,
        "creak": creak, "patrol": patrol, "exits": exits,
        "cover": cover, "investigate": investigate, "doors": doors
    }


def transform_str(x, y, z):
    return f"Transform3D(1, 0, 0, 0, 1, 0, 0, 0, 1, {x}, {y}, {z})"


def box_shape(sid, size):
    return f'[sub_resource type="BoxShape3D" id="{sid}"]\nsize = Vector3({size[0]}, {size[1]}, {size[2]})\n'


def static_body_with_box(parent_path, node_name, transform, box_shape_id):
    return (
        f'\n[node name="{node_name}" type="StaticBody3D" parent="{parent_path}"]\n'
        f'transform = {transform_str(*transform)}\n'
        f'\n[node name="CollisionShape3D" type="CollisionShape3D" parent="{parent_path}/{node_name}"]\n'
        f'shape = SubResource("{box_shape_id}")\n'
    )


def doorway_area(parent_path, name, transform):
    return (
        f'\n[node name="{name}" type="Area3D" parent="{parent_path}" groups=["Doorway"]]\n'
        f'transform = {transform_str(*transform)}\n'
        f'\n[node name="CollisionShape3D" type="CollisionShape3D" parent="{parent_path}/{name}"]\n'
        f'shape = SubResource("BoxShape_doorway")\n'
    )


def build_room(room_name, meta):
    p = f"World/Rooms/{room_name}"
    sx, sy, sz = meta["size"]
    tx, ty, tz = meta["pos"]
    walls = meta["doors"]
    out = []
    out.append(f'\n[node name="{room_name}" type="Node3D" parent="World/Rooms"]\n')
    out.append(f'transform = {transform_str(tx, ty, tz)}\n')
    out.append('script = ExtResource("1_abc123")\n')
    out.append(f'RoomName = "{room_name.replace("_", " ")}"\n')
    out.append(f'RoomType = "{meta["type"]}"\n')
    out.append(f'AcousticType = "{meta["acous"]}"\n')
    out.append(f'IsCreakZone = {str(meta["creak"]).lower()}\n')
    out.append(f'HasPatrolWaypoint = {str(meta["patrol"]).lower()}\n')
    out.append(f'ExitCount = {meta["exits"]}\n')
    out.append(f'HasCover = {str(meta["cover"]).lower()}\n')
    out.append(f'ListenerCanInvestigate = {str(meta["investigate"]).lower()}\n')
    out.append(f'RoomSize = Vector3({sx}, {sy}, {sz})\n')

    rid = room_name.replace("Room_", "").replace("_", "")
    floor_id = f"Shape_{rid}_floor"
    ceil_id = f"Shape_{rid}_ceiling"
    wall_ns_id = f"Shape_{rid}_wall_ns"
    wall_ew_id = f"Shape_{rid}_wall_ew"

    out.append(static_body_with_box(p, "Floor", (0, -sy/2 + 0.1, 0), floor_id))
    out.append(static_body_with_box(p, "Ceiling", (0, sy/2 - 0.1, 0), ceil_id))

    wall_thick = 0.2
    if "N" not in walls:
        out.append(static_body_with_box(p, "Wall_North", (0, 0, sz/2 - wall_thick/2), wall_ns_id))
    if "S" not in walls:
        out.append(static_body_with_box(p, "Wall_South", (0, 0, -sz/2 + wall_thick/2), wall_ns_id))
    if "E" not in walls:
        out.append(static_body_with_box(p, "Wall_East", (sx/2 - wall_thick/2, 0, 0), wall_ew_id))
    if "W" not in walls:
        out.append(static_body_with_box(p, "Wall_West", (-sx/2 + wall_thick/2, 0, 0), wall_ew_id))

    for dname, dpos in walls.items():
        out.append(doorway_area(p, f"Doorway_{dname}", dpos))

    return "".join(out), {rid: (sx, sy, sz)}


def main():
    lines = []
    lines.append('[gd_scene load_steps=17 format=3]\n')
    lines.append('[ext_resource type="Script" path="res://Scripts/World/Floor1Room.cs" id="1_abc123"]\n')
    lines.append('[ext_resource type="AudioStream" path="res://Assets/Floor1/Audio/basement_drone.wav" id="2_audio_drone"]\n')
    lines.append('[ext_resource type="AudioStream" path="res://Assets/Floor1/Audio/furnace_machine.wav" id="3_audio_furnace"]\n')
    lines.append('[ext_resource type="AudioStream" path="res://Assets/Floor1/Audio/bone_hall_texture.wav" id="4_audio_water"]\n')
    lines.append('[ext_resource type="AudioStream" path="res://Assets/Floor1/Audio/stone_bell.wav" id="5_audio_bell"]\n')

    shape_sizes = {}
    room_chunks = []
    for name, pos, size, rtype, acous, creak, patrol, exits, cover, investigate, doors in ROOMS:
        meta = ROOM_META[name]
        chunk, sizes = build_room(name, meta)
        room_chunks.append(chunk)
        shape_sizes.update(sizes)

    lines.append('\n')
    for rid, (sx, sy, sz) in shape_sizes.items():
        lines.append(box_shape(f"Shape_{rid}_floor", (sx, 0.2, sz)))
        lines.append(box_shape(f"Shape_{rid}_ceiling", (sx, 0.2, sz)))
        lines.append(box_shape(f"Shape_{rid}_wall_ns", (sx, sy, 0.2)))
        lines.append(box_shape(f"Shape_{rid}_wall_ew", (0.2, sy, sz)))

    lines.append(box_shape("BoxShape_doorway", (2, 4, 0.5)))
    lines.append(box_shape("BoxShape_creak", (6, 4, 22)))
    lines.append(box_shape("BoxShape_wet", (10, 4, 10)))
    lines.append(box_shape("BoxShape_staircase", (4, 4, 4)))

    lines.append('\n[sub_resource type="Environment" id="Environment_floor1"]\n')
    lines.append('background_mode = 1\n')
    lines.append('background_color = Color(0.02, 0.02, 0.03, 1)\n')
    lines.append('ambient_light_source = 2\n')
    lines.append('ambient_light_color = Color(0.03, 0.03, 0.05, 1)\n')
    lines.append('ambient_light_energy = 0.3\n')
    lines.append('fog_enabled = true\n')
    lines.append('fog_density = 0.08\n')
    lines.append('fog_aerial_perspective = 0.5\n')

    lines.append('\n[sub_resource type="NavigationMesh" id="NavigationMesh_floor1"]\n')
    lines.append('agent_radius = 0.5\n')
    lines.append('agent_height = 2.0\n')
    lines.append('agent_max_slope = 45.0\n')
    lines.append('agent_max_climb = 0.5\n')
    lines.append('cell_size = 0.25\n')
    lines.append('cell_height = 0.25\n')

    lines.append('\n[node name="F1_Basement" type="Node3D"]\n')
    lines.append('\n[node name="World" type="Node3D" parent="."]\n')
    lines.append('\n[node name="WorldEnvironment" type="WorldEnvironment" parent="World"]\n')
    lines.append('environment = SubResource("Environment_floor1")\n')

    lines.append('\n[node name="Rooms" type="Node3D" parent="World"]\n')
    lines.extend(room_chunks)

    # Props
    lines.append('\n[node name="Props" type="Node3D" parent="World"]\n')
    lines.append(
        '\n[node name="PLACEHOLDER_Furnace" type="CSGBox3D" parent="World/Props"]\n'
        'transform = Transform3D(1, 0, 0, 0, 1, 0, 0, 0, 1, -16, 1.5, 40)\n'
        'size = Vector3(3, 3, 2)\n'
        'use_collision = true\n'
        'metadata/missing_asset = true\n'
    )
    lines.append(
        '\n[node name="PLACEHOLDER_RegistrationBoard" type="CSGBox3D" parent="World/Props"]\n'
        'transform = Transform3D(1, 0, 0, 0, 1, 0, 0, 0, 1, 6, 1, 16)\n'
        'size = Vector3(1.5, 2, 0.5)\n'
        'use_collision = true\n'
        'metadata/missing_asset = true\n'
    )
    lines.append(
        '\n[node name="PLACEHOLDER_Intercom" type="CSGBox3D" parent="World/Props"]\n'
        'transform = Transform3D(1, 0, 0, 0, 1, 0, 0, 0, 1, -6, 1.5, 28)\n'
        'size = Vector3(0.4, 0.6, 0.2)\n'
        'use_collision = true\n'
        'metadata/missing_asset = true\n'
    )

    # Navigation
    lines.append('\n[node name="Navigation" type="Node3D" parent="World"]\n')
    lines.append('\n[node name="NavigationRegion3D" type="NavigationRegion3D" parent="World/Navigation"]\n')
    lines.append('navigation_mesh = SubResource("NavigationMesh_floor1")\n')
    lines.append('\n[node name="PatrolWaypoints" type="Node3D" parent="World/Navigation"]\n')
    wps = [
        ("WP01_Furnace", -16, 0.1, 40),
        ("WP02_Boiler", -6, 0.1, 40),
        ("WP03_ColdStorage", -6, 0.1, 28),
        ("WP04_ServantHall", -6, 0.1, 16),
        ("WP05_WestCorridor", -6, 0.1, 4),
        ("WP06_StorageNiche", -18, 0.1, 4),
        ("WP07_SecretNiche", -18, 0.1, -8),
        ("WP08_FloodedTunnel", 6, 0.1, -8),
        ("WP09_SumpPit", 18, 0.1, -8),
        ("WP10_RitualChamber", 18, 0.1, 4),
        ("WP11_EastCorridor", 18, 0.1, 16),
        ("WP12_Crypt", 30, 0.1, 16),
        ("WP13_StaircaseHub", 30, 0.1, 4),
        ("WP14_Laundry", 6, 0.1, 28),
        ("WP15_WineCellar", 6, 0.1, 40),
        ("WP16_Pantry", 18, 0.1, 40),
    ]
    for name, x, y, z in wps:
        lines.append(f'\n[node name="{name}" type="Marker3D" parent="World/Navigation/PatrolWaypoints" groups=["Waypoint"]]\n')
        lines.append(f'transform = {transform_str(x, y, z)}\n')

    # Zones
    lines.append('\n[node name="Zones" type="Node3D" parent="World"]\n')
    lines.append('\n[node name="SpawnZone_Players" type="Marker3D" parent="World/Zones" groups=["SpawnPoint"]]\n')
    lines.append(f'transform = {transform_str(-16, 0.1, 28)}\n')
    lines.append('\n[node name="SpawnZone_Listener" type="Marker3D" parent="World/Zones" groups=["ListenerSpawnBasement"]]\n')
    lines.append(f'transform = {transform_str(30, 0.1, 16)}\n')
    for i, (nx, nz) in enumerate([(-16, 40), (6, 28), (18, 4), (30, -8)], start=1):
        lines.append(f'\n[node name="NoteSpawn_{i:02d}" type="Marker3D" parent="World/Zones" groups=["SpawnPoint"]]\n')
        lines.append(f'transform = {transform_str(nx, 0.1, nz)}\n')
    lines.append('\n[node name="CreakZone" type="Area3D" parent="World/Zones" groups=["CreakZone"]]\n')
    lines.append(f'transform = {transform_str(-6, 0, 16)}\n')
    lines.append('\n[node name="CollisionShape3D" type="CollisionShape3D" parent="World/Zones/CreakZone"]\n')
    lines.append('shape = SubResource("BoxShape_creak")\n')
    lines.append('\n[node name="WetZone" type="Area3D" parent="World/Zones" groups=["WetZone"]]\n')
    lines.append(f'transform = {transform_str(12, 0, -8)}\n')
    lines.append('\n[node name="CollisionShape3D" type="CollisionShape3D" parent="World/Zones/WetZone"]\n')
    lines.append('shape = SubResource("BoxShape_wet")\n')
    lines.append('\n[node name="StaircaseWinTrigger" type="Area3D" parent="World/Zones" groups=["StaircaseWin"]]\n')
    lines.append(f'transform = {transform_str(30, 0, 4)}\n')
    lines.append('\n[node name="CollisionShape3D" type="CollisionShape3D" parent="World/Zones/StaircaseWinTrigger"]\n')
    lines.append('shape = SubResource("BoxShape_staircase")\n')

    # Lighting
    lines.append('\n[node name="Lighting" type="Node3D" parent="World"]\n')
    lights = [
        ("FurnaceGlow", -16, 2, 40, "Color(1, 0.35, 0.05, 1)", 2.0, 8.0),
        ("WallTorch_ColdStorage", -6, 2.5, 28, "Color(1, 0.7, 0.2, 1)", 0.8, 4.0),
        ("WallTorch_ServantHall", -6, 2.5, 16, "Color(1, 0.7, 0.2, 1)", 0.6, 3.5),
        ("WallTorch_CentralHall", 6, 2.5, 16, "Color(1, 0.7, 0.2, 1)", 0.8, 4.0),
        ("WallTorch_EastCorridor", 18, 2.5, 16, "Color(1, 0.7, 0.2, 1)", 0.6, 3.5),
        ("WallTorch_Staircase", 30, 2.5, 4, "Color(1, 0.7, 0.2, 1)", 1.0, 5.0),
    ]
    for name, x, y, z, color, energy, range_ in lights:
        lines.append(f'\n[node name="{name}" type="OmniLight3D" parent="World/Lighting"]\n')
        lines.append(f'transform = {transform_str(x, y, z)}\n')
        lines.append(f'light_color = {color}\n')
        lines.append(f'light_energy = {energy}\n')
        lines.append(f'omni_range = {range_}\n')
        lines.append('shadow_enabled = true\n')

    # Audio
    lines.append('\n[node name="Audio" type="Node3D" parent="World"]\n')
    lines.append('\n[node name="Ambient_Drone" type="AudioStreamPlayer3D" parent="World/Audio"]\n')
    lines.append('stream = ExtResource("2_audio_drone")\n')
    lines.append('autoplay = true\n')
    lines.append('max_distance = 80.0\n')
    lines.append('unit_size = 40.0\n')
    lines.append('\n[node name="Ambient_Furnace" type="AudioStreamPlayer3D" parent="World/Audio"]\n')
    lines.append('stream = ExtResource("3_audio_furnace")\n')
    lines.append('autoplay = true\n')
    lines.append(f'transform = {transform_str(-16, 2, 40)}\n')
    lines.append('max_distance = 20.0\n')
    lines.append('unit_size = 10.0\n')
    lines.append('\n[node name="SFX_WaterDrip" type="AudioStreamPlayer3D" parent="World/Audio"]\n')
    lines.append('stream = ExtResource("4_audio_water")\n')
    lines.append('autoplay = true\n')
    lines.append(f'transform = {transform_str(12, 0, -8)}\n')
    lines.append('max_distance = 15.0\n')
    lines.append('unit_size = 5.0\n')
    lines.append('\n[node name="SFX_DistantBell" type="AudioStreamPlayer3D" parent="World/Audio"]\n')
    lines.append('stream = ExtResource("5_audio_bell")\n')
    lines.append('autoplay = true\n')
    lines.append('volume_db = -8.0\n')
    lines.append(f'transform = {transform_str(30, 2, 4)}\n')
    lines.append('max_distance = 40.0\n')
    lines.append('unit_size = 15.0\n')

    lines.append('\n')
    SCENE.write_text("".join(lines), encoding="utf-8")
    print(f"Wrote {SCENE}")


if __name__ == "__main__":
    main()
