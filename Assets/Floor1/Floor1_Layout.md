# Floor 1 — The Basement Layout

**Grid scale:** each ASCII cell = 2 metres.  
**Total footprint:** approximately 56 m (W-E) × 60 m (N-S).  
**Shape:** fractured, asymmetric service-basement sprawl with a flooded southern wing and a secret western niche.  
**Fixed anchors:**
- `P` Player Spawn — inside Room 03 CoalBin (NW cluster)
- `F` Furnace Room — Room 01 Furnace (SW of north row)
- `^` Staircase Hub / win zone — Room 20 StaircaseHub (NE)
- `R` RegistrationBoard — inside Room 10 CentralHall
- `f` / `u` Flooded wing — Room 18 FloodedTunnel + Room 19 SumpPit
- `s` Secret wing — Room 17 SecretNiche

---

## ASCII Top-Down Map

```text
      2 2 1 1 1 1 1 1 1 1 1 1 0 0 0 0 0 0 0 0 1 1 1 1 2 2 2 3
x:   -2 -2 -1 -1 -1 -0 -0 -0  0  0  0  0  0  0  0  0  0  0  1  1  1  1  1  1  1  1  1  1
      2 0 8 6 4 2  0  8  6  4  2  0  2  4  6  8  0  2  4  6  8  0  2  4  6  8  0  2  4
z:46 #############################
  44 #FFFFFBBBBB.WWWWW############
  42 #FFFFFBBBBB.WWWWW##ppp#######
  40 #FFFFFBBBBB.WWWWW##ppp#######
  38 #FFFFFBBBBB.WWWWW##ppp#######
  36 #FFFFFBBBBB.WWWWW############
  34 #................############
  32 #CCCCCDDDDD.LLLLL.KKKKK######
  30 #CCCCCDDDDD.LLLLL.KKKKK######
  28 #CCPCCDDDDD.LLLLL.KKKKK######
  26 #CCCCCDDDDD.LLLLL.KKKKK######
  24 #CCCCCDDDDD.LLLLL.KKKKK######
  22 ######.................######
  20 ######HHHHH.RRRRR.EEEEE.YYYYY
  18 ######HHHHH.RRRRR.EEEEE.YYYYY
  16 ######HHHHH.RRRRR.EEEEE.YYYYY
  14 ######HHHHH.RRRRR.EEEEE.YYYYY
  12 ######HHHHH.RRRRR.EEEEE.YYYYY
  10 ######.......................
   8 ######VVVVV.NNNNN.TTTTT.^^^^^
   6 #SSS##VVVVV.NNNNN.TTTTT.^^^^^
   4 #SSS##VVVVV.NNNNN.TTTTT.^^^^^
   2 #SSS##VVVVV.NNNNN.TTTTT.^^^^^
   0 ######VVVVV.NNNNN.TTTTT.^^^^^
  -2 ############.....############
  -4 ############fffff############
  -6 #sss########fffff##uuu#######
  -8 #sss########fffff##uuu#######
 -10 #sss########fffff##uuu#######
 -12 ############fffff############
 -14 #############################
```

### Map Legend

| Symbol | Room / Zone |
|---|---|
| `F` | Room 01 Furnace |
| `B` | Room 02 Boiler Antechamber |
| `C` | Room 03 Coal Bin |
| `D` | Room 04 Cold Storage |
| `W` | Room 05 Wine Cellar |
| `L` | Room 06 Laundry |
| `H` | Room 07 Servant Hall (CreakZone) |
| `p` | Room 08 Pantry |
| `K` | Room 09 Kitchen Prep |
| `R` | Room 10 Central Hall (RegistrationBoard) |
| `E` | Room 11 East Corridor |
| `Y` | Room 12 Crypt |
| `T` | Room 13 Ritual Chamber |
| `N` | Room 14 North Store |
| `V` | Room 15 West Corridor |
| `S` | Room 16 Storage Niche |
| `s` | Room 17 Secret Niche |
| `f` | Room 18 Flooded Tunnel |
| `u` | Room 19 Sump Pit |
| `^` | Room 20 Staircase Hub |
| `P` | Player spawn point (inside Coal Bin) |
| `.` | Traversable hallway / corridor |
| `#` | Wall / void / blocked space |

---

## Patrol Waypoint Order

The Listener patrols the 16 `Marker3D` nodes under `World/Navigation/PatrolWaypoints`.  
The intended loop starts at the furnace, sweeps west and south, crosses the flooded wing, loops back through the east and north, and returns to the furnace.

1. **WP01_Furnace** — Room 01 Furnace centre (`-16, 0.1, 40`)
2. **WP02_Boiler** — Room 02 BoilerAnte centre (`-6, 0.1, 40`)
3. **WP03_ColdStorage** — Room 04 ColdStorage centre (`-6, 0.1, 28`)
4. **WP04_ServantHall** — Room 07 ServantHall centre (`-6, 0.1, 16`)
5. **WP05_WestCorridor** — Room 15 WestCorridor centre (`-6, 0.1, 4`)
6. **WP06_StorageNiche** — Room 16 StorageNiche centre (`-18, 0.1, 4`)
7. **WP07_SecretNiche** — Room 17 SecretNiche centre (`-18, 0.1, -8`)
8. **WP08_FloodedTunnel** — Room 18 FloodedTunnel centre (`6, 0.1, -8`)
9. **WP09_SumpPit** — Room 19 SumpPit centre (`18, 0.1, -8`)
10. **WP10_RitualChamber** — Room 13 RitualChamber centre (`18, 0.1, 4`)
11. **WP11_EastCorridor** — Room 11 EastCorridor centre (`18, 0.1, 16`)
12. **WP12_Crypt** — Room 12 Crypt centre (`30, 0.1, 16`)
13. **WP13_StaircaseHub** — Room 20 StaircaseHub centre (`30, 0.1, 4`)
14. **WP14_Laundry** — Room 06 Laundry centre (`6, 0.1, 28`)
15. **WP15_WineCellar** — Room 05 WineCellar centre (`6, 0.1, 40`)
16. **WP16_Pantry** — Room 08 Pantry centre (`18, 0.1, 40`)

*Cycle:* WP16 → WP01 to close the loop.

---

## PLACEHOLDER CSGBox3D List

These `CSGBox3D` nodes are under `World/Props` and are tagged with `metadata/missing_asset = true`.  
They stand in for final art assets.

| Node | Purpose | World Position | Size (m) |
|---|---|---|---|
| `PLACEHOLDER_Furnace` | Main furnace body | `-16, 1.5, 40` | `3 × 3 × 2` |
| `PLACEHOLDER_Intercom` | Wall intercom / objective trigger | `-6, 1.5, 28` | `0.4 × 0.6 × 0.2` |
| `PLACEHOLDER_Chains` | Hanging chains / ambience prop | `-6, 2, 26` | `0.2 × 4 × 0.2` |
| `PLACEHOLDER_Puddle` | Shallow water surface in flooded wing | `18, 0.05, -8` | `4 × 0.1 × 4` |
| `PLACEHOLDER_Shelves` | Storage shelving (CoalBin area) | `-14, 1.5, 30` | `4 × 2.5 × 1` |
| `PLACEHOLDER_Workbench` | Workbench / utility table | `-18, 1, 22` | `3 × 1.5 × 1.5` |
| `PLACEHOLDER_Pipes` | Vertical pipe cluster | `8, 1.5, 16` | `0.5 × 3 × 0.5` |

---

## Key Zone Markers

| Marker | Node Path | World Position | Purpose |
|---|---|---|---|
| Player spawn | `World/Zones/SpawnZone_Players` | `-16, 0.1, 28` | Player start (inside Coal Bin) |
| Listener spawn | `World/Zones/SpawnZone_Listener` | `30, 0.1, 16` | Listener start (inside Crypt) |
| Registration board | `World/Props/RegistrationBoard` | `6, 1, 16` | Central objective board (inside Central Hall) |
| Creak zone | `World/Zones/CreakZone` | `-6, 0, 16` | `6 × 4 × 22` hazard area centred on Servant Hall |
| Wet zone | `World/Zones/WetZone` | `12, 0, -8` | `10 × 4 × 10` water hazard centred on Flooded Tunnel |
| Staircase win zone | `World/Zones/Staircase_WinZone` | `34, 0.5, 16` | Triggers end-of-floor sequence after objective complete |
| Note spawns | `World/Zones/NoteSpawn_01..04` | `-16,0.1,40` / `6,0.1,28` / `18,0.1,4` / `30,0.1,-8` | Collectible lore-note spawn points |
