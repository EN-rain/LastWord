# Floor 1 — Room Manifest

All 20 rooms are defined under `World/Rooms` in `Scenes/Floors/F1_Basement.tscn`.  
Sizes are taken from each room's `RoomSize` property (`width × height × depth`, metres).  
Connected rooms are derived from doorway alignment and corridor proximity in the scene.

| Room ID | Room Name | Purpose | Size | Connected Rooms | Key Props / Notes |
|---|---|---|---|---|---|
| Room_01 | Furnace | Anchor / objective | 10 × 5 × 10 | Boiler Antechamber, Coal Bin | `PLACEHOLDER_Furnace` (3×3×2), `FurnaceGlow` omni light, furnace audio, note spawn point |
| Room_02 | BoilerAnte | Utility junction | 8 × 4 × 8 | Furnace, Cold Storage, Wine Cellar | `Mesh_Prop` rocks, boiler doorway to north corridor |
| Room_03 | CoalBin | Storage / player spawn | 8 × 4 × 8 | Furnace, Cold Storage | Player spawn zone (`SpawnZone_Players`), `Mesh_Prop` rubble, shelves placeholder nearby |
| Room_04 | ColdStorage | Storage / crossroads | 8 × 4 × 8 | Coal Bin, Boiler Antechamber, Laundry, Servant Hall | `WallTorch_ColdStorage` omni light, 4 doorways |
| Room_05 | WineCellar | Storage | 10 × 4 × 8 | Boiler Antechamber, Laundry, Pantry | `Mesh_Prop` rocks, south wall detail |
| Room_06 | Laundry | Utility | 8 × 4 × 8 | Cold Storage, Wine Cellar, Kitchen Prep, Central Hall | Wet ambience, note spawn point |
| Room_07 | ServantHall | Corridor / creak hazard | 10 × 4 × 10 | Cold Storage, Central Hall, West Corridor | `IsCreakZone = true`, patrol waypoint WP04, `CreakZone` area overlap |
| Room_08 | Pantry | Storage | 6 × 4 × 6 | Wine Cellar, Kitchen Prep | Small dead-end storage, south/east walls |
| Room_09 | KitchenPrep | Utility | 8 × 4 × 8 | Laundry, Pantry, East Corridor | Connects north pantry row to east corridor |
| Room_10 | CentralHall | Main hub / objective | 10 × 5 × 10 | Laundry, Servant Hall, East Corridor, North Store | `RegistrationBoard` instance, `SequenceManager`, 4 doorways, patrol waypoint WP04 area |
| Room_11 | EastCorridor | Corridor / patrol route | 8 × 4 × 8 | Kitchen Prep, Central Hall, Crypt, Ritual Chamber | `Mesh_Column_Crypt` nearby, patrol waypoint WP11 |
| Room_12 | Crypt | Danger / listener spawn | 8 × 5 × 8 | East Corridor, Staircase Hub | Listener spawn zone (`SpawnZone_Listener`), `Mesh_Column_Crypt`, wall torch |
| Room_13 | RitualChamber | Ritual / waypoint room | 10 × 5 × 8 | East Corridor, North Store, Sump Pit, Staircase Hub | `Mesh_FloorDetail_Ritual` floor accent, patrol waypoint WP10 |
| Room_14 | NorthStore | Storage / crossroads | 8 × 4 × 8 | Central Hall, Ritual Chamber, West Corridor, Flooded Tunnel | Central southern junction, connects to flooded wing |
| Room_15 | WestCorridor | Corridor / patrol route | 8 × 4 × 8 | Servant Hall, North Store, Storage Niche | Patrol waypoint WP05, west-side spine |
| Room_16 | StorageNiche | Storage | 6 × 4 × 6 | West Corridor, Secret Niche | `PLACEHOLDER_Shelves` nearby, leads to secret wing |
| Room_17 | SecretNiche | Secret / hidden reward | 6 × 4 × 6 | Storage Niche | Hidden dead-end niche, patrol waypoint WP07 |
| Room_18 | FloodedTunnel | Hazard / flooded wing | 10 × 4 × 8 | North Store, Sump Pit | `WetZone` overlap, `PLACEHOLDER_Puddle`, water audio, patrol waypoint WP08 |
| Room_19 | SumpPit | Hazard / flooded dead end | 6 × 4 × 6 | Ritual Chamber, Flooded Tunnel | `Mesh_Rocks_Sump`, note spawn point, patrol waypoint WP09 |
| Room_20 | StaircaseHub | Transition / win zone | 10 × 5 × 10 | Crypt, Ritual Chamber | Staircase prop (`Stairs_Hub`), `Staircase_WinZone`, only upward exit, patrol waypoint WP13 |

---

## Connectivity Summary

```
Furnace (01) — BoilerAnte (02) — WineCellar (05) — Pantry (08)
    |                |                 |              |
CoalBin (03) — ColdStorage (04) — Laundry (06) — KitchenPrep (09)
    |                |                 |              |
               ServantHall (07) — CentralHall (10) — EastCorridor (11) — Crypt (12) — StaircaseHub (20)
                    |                    |               |
               WestCorridor (15) — NorthStore (14) — RitualChamber (13) — SumpPit (19)
                    |                    |
               StorageNiche (16)      FloodedTunnel (18)
                    |
               SecretNiche (17)
```

---

## Patrol Waypoint Order

The 16 `Marker3D` patrol waypoints form a perimeter loop under `World/Navigation/PatrolWaypoints`. The Listener cycles them in this order:

1. **WP01_Furnace** — (-16, 0.1, 40)
2. **WP02_Boiler** — (-6, 0.1, 40)
3. **WP03_ColdStorage** — (-6, 0.1, 28)
4. **WP04_ServantHall** — (-6, 0.1, 16)
5. **WP05_WestCorridor** — (-6, 0.1, 4)
6. **WP06_StorageNiche** — (-18, 0.1, 4)
7. **WP07_SecretNiche** — (-18, 0.1, -8)
8. **WP08_FloodedTunnel** — (6, 0.1, -8)
9. **WP09_SumpPit** — (18, 0.1, -8)
10. **WP10_RitualChamber** — (18, 0.1, 4)
11. **WP11_EastCorridor** — (18, 0.1, 16)
12. **WP12_Crypt** — (30, 0.1, 16)
13. **WP13_StaircaseHub** — (30, 0.1, 4)
14. **WP14_Laundry** — (6, 0.1, 28)
15. **WP15_WineCellar** — (6, 0.1, 40)
16. **WP16_Pantry** — (18, 0.1, 40)

## Placeholder Replacement

All previous `PLACEHOLDER_*` CSGBox3D nodes have been replaced with downloaded Kenney mini-dungeon mesh assets. Walls, floors, and ceilings still use CSGBox3D for collision; every prop/object now uses a real mesh.

| Object | Mesh asset(s) used | Scene path |
|---|---|---|
| Furnace | `rocks.glb`, `stones.glb`, `column.glb` | `World/Props/Furnace` |
| Intercom | `chest.glb` (scaled) | `World/Props/Intercom` |
| Hanging chains | `banner.glb`, `trap.glb` | `World/Props/HangingChains` |
| Puddle | `dirt.glb` (flattened) | `World/Props/Puddle` |
| Shelves | `wood-structure.glb` | `World/Props/Shelves` |
| Workbench | `wood-structure.glb`, `wood-support.glb` | `World/Props/Workbench` |
| Pipes | `column.glb` (scaled thin) | `World/Props/Pipes` |

Other prop categories (`Crates`, `Barrels`, `Chests`, `Columns`, `Rubble`, `WoodSupports`, `Doors`, `Stairs`, `Coins`, `Traps`) also use downloaded assets.

## Design Checklist

- **20 rooms:** present and listed above.
- **2+ exit rooms:** BoilerAnte, ColdStorage, WineCellar, Laundry, ServantHall, CentralHall, EastCorridor, RitualChamber, NorthStore, WestCorridor, StaircaseHub.
- **Anchor rooms:** Furnace (SW of north row), StaircaseHub (NE), player spawn opposite staircase in CoalBin.
- **Hazard zones:** ServantHall (CreakZone), FloodedTunnel + SumpPit (WetZone).
- **Secret wing:** StorageNiche → SecretNiche.
- **Patrol coverage:** 16 waypoints covering all major zones.
