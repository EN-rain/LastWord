# Floor 1 — The Basement Layout

**Grid scale:** each cell = 2 metres.  
**Total footprint:** ~52 m × 40 m.  
**Shape:** fractured Y / amoeba sprawl.  
**Fixed anchors:**
- `P` Player Spawn / Entrance — NW edge
- `F` Furnace Room — SW corner
- `S` Staircase Hub (only upward exit) — NE corner

---

## ASCII Top-Down Map

```text
     1 1 1 1 1 1 1 1 1 1 2 2 2 2 2 2
     1 2 3 4 5 6 7 8 9 0 1 2 3 4 5 6
  1  # # # # # # # # # # # # # # # # # # # # # # # # # # #
  2  # P P . # . . . . . . . . . . # . . . . . . . . S S #
  3  # P P . # . . . . . . . . . . # . . . . . . . . S S #
  4  # . . . # . . . . . . . . . . # . . . . . . . . . . #
  5  # # . # # # # # # . # # # # # # # # # # . # # # # # #
  6  # . . . T T . . . . . . . . . . . . . . . . . . . . #
  7  # . . . T T . . . . . . . . . . . . . . . . . G G . #
  8  # # . # # # # # # # # # . # # # # # . # # # . G G . #
  9  # . . . . . . . . . . . . . . . . . . . . . . . . . #
 10  # . V V V . . . . . . . . . . . . . . . . . . . . . #
 11  # . V V V . . C C C C C C C C . . . . . . . . . . . #
 12  # . . . . . . C C C C C C C C . . . . . . . . . . . #
 13  # # . # # # # # # # . # # # # # # # . # # # # # # # #
 14  # . . . . . . . . . . . . . . . . . . . . . . . . . #
 15  # . X X X . . B B . . . . . . . . . . . . V V . . . #
 16  # . X X X . . B B . . W W W W . . . . . . V V . . . #
 17  # . X X X . . . . . . W W W W . . . . . . V V . . . #
 18  # # . # # # # # # # # # # # # # . # # # . # # . # # #
 19  # . . . . . . . . . . . . . . . . . . . . . . . L L #
 20  # . F F F . . . . . . . . . . . . . . . . . . . L L #
 21  # . F F F . . . . . . . . . . . . . . . . . . . . . #
 22  # # # # # # # # # # # # # # # # # # # # # # # # # # #
```

### Room / Zone Legend

| Symbol | Room / Zone |
|---|---|
| `P` | Player Spawn / Entrance |
| `F` | Furnace Room |
| `S` | Staircase Hub |
| `C` | Creak Corridor (central, unavoidable main path) |
| `A` | Alternate Route (silent bypass) — see detailed map below |
| `L` | Lockbox Room |
| `R` | Registration Board Alcove |
| `B` | Boiler / Utility Room |
| `W` | Flooded Section |
| `V` | Wide Patrol Chamber |
| `M` | Ambush Corridor |
| `D` | Dead-End Reward Room |
| `J` | Split-Path Junction |
| `X` | Collapsed Room |
| `H` | Hidden Alcove |
| `T` | Storage Room |
| `G` | Guard Room / Antechamber |
| `.` | Generic hallway / traversable floor |
| `#` | Wall / rock / blocked space |
| `~` | Shallow water / puddle |

---

## Detailed Alternate Route & Hidden Paths

The ASCII map above is simplified. The alternate route is a longer eastern loop that
bypasses the Creak Corridor. It re-enters the main floor south of the flooded section.

```text
  Alternate Route (eastern loop, silent):

       ┌─ Guard Room (G) ─┐
       │                  │
  Creak Corridor (C)     Alternate hallway
       │                  │
       └─ Split J2 ───────┘
                            │
                       Flooded Section (W)
                            │
                       Hidden Alcove (H)
                            │
                       Lockbox Room (L)
                            │
                       Staircase Hub (S)  ← rejoin
```

---

## Connectivity Graph

```
Entrance (P)
  │
  ├─ Storage Room (T)
  │
  ├─ Wide Patrol Chamber A (V1) ─ Dead-End Reward A (D1)
  │
  ├─ Creak Corridor (C) ─────────────┬── Split-Path Junction A (J1)
  │                                  │
  │                                  ├── Ambush Corridor A (M1)
  │                                  │
  │                                  └── Split-Path Junction B (J2) ─ Guard Room (G)
  │
  └─ Alternate Route (A) ────────────┬── Registration Board Alcove (R)
                                     │
                                     ├── Flooded Section (W) ─ Hidden Alcove (H)
                                     │
                                     ├── Wide Patrol Chamber B (V2)
                                     │
                                     └── Lockbox Room (L)

South spine:
Split-Path Junction A (J1)
  │
  ├─ Collapsed Room (X)
  │
  ├─ Boiler / Utility Room (B)
  │
  ├─ Split-Path Junction C (J3)
  │
  └─ Furnace Room (F)

East spine:
Guard Room (G)
  │
  ├─ Staircase Hub (S)
  │
  └─ Alternate Route (A) ─ Lockbox Room (L)
```

---

## Patrol Waypoint Path

Perimeter patrol for The Listener. Waypoints are placed at room centres / choke points
and connected in a clockwise loop around the floor.

1. WP01 — Player Spawn landing
2. WP02 — Storage Room entrance
3. WP03 — Wide Patrol Chamber A centre
4. WP04 — Creak Corridor midpoint
5. WP05 — Guard Room / Antechamber
6. WP06 — Staircase Hub
7. WP07 — Wide Patrol Chamber B
8. WP08 — Flooded Section east edge
9. WP09 — Lockbox Room entrance
10. WP10 — Collapsed Room north edge
11. WP11 — Boiler / Utility Room
12. WP12 — Furnace Room east side
13. WP13 — Furnace Room north side  → back to WP03

(The Listener uses `NavigationRegion3D` and these `Marker3D` waypoints to cycle.)

---

## Acoustic Tags

| Room | Acoustic Tag | Reason |
|---|---|---|
| Furnace Room | MUFFLED | Heavy machinery, enclosed |
| Creak Corridor | CREAK_ZONE | Central creak tiles — Tier 0, 15% chance, 4m radius |
| Alternate Route | NEUTRAL | Silent bypass |
| Flooded Section | ECHO | Water surface, large open area |
| Wide Patrol Chambers | ECHO | Tall ceilings, stone walls |
| Storage Room | MUFFLED | Crates absorb sound |
| Guard Room | ECHO | Wide dramatic chamber |
| Boiler / Utility | MUFFLED | Machinery, small |
| Collapsed Room | ECHO | Open ceiling, rubble |
| Staircase Hub | NEUTRAL | Transition space |
| All halls | NEUTRAL | Standard traversal |
