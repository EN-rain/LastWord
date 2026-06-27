# Optimization Design — Last Word

**Date:** 2026-06-27
**Scope:** Strategy A (Quick Wins) + Strategy B (Asset Pipeline Overhaul)
**Status:** Draft → pending user review
**Target:** Godot 4.6.2 Mono, C# (.NET 10.0.301), 4 floor scenes

## Goals
- **15–25%** reduction in per-frame CPU cost (ListenerAI + PlayerController)
- **50–70%** reduction in floor-scene load time
- **40%** reduction in texture VRAM
- Unlock the **KayKit Furniture Bits** pack (107 `.gltf` files) as usable `PackedScene` instances

## Non-Goals (deferred to Strategy C)
- Splitting `PlayerController` (900 lines) into components
- Splitting `GameManager` (400 lines) into phase managers
- Extracting a `BaseFloorScene` to deduplicate the 4 floor `.tscn` files
- Data-driven floor layouts (JSON/YAML)

## Findings (Hotspot Table)

| Area | Hotspot | Location |
|---|---|---|
| Runtime | `ListenerAI._Process` runs every frame with group lookups + pathfinding | `Scripts/Enemy/ListenerAI.cs` |
| Runtime | Each floor has ~35 instanced PackedScenes + 5 point lights | `Scenes/Floors/F{1-4}*.tscn` |
| Memory | `Godot.Collections.Array` returned by `GetNodesInGroup` per call (P/Invoke marshalling) | `Scripts/Player/PlayerController.cs:613–661` |
| Memory | `VoiceRecorder.GetRecentRecording` allocates `AudioStreamWav` per call | `Scripts/Core/VoiceRecorder.cs` |
| Load | 4 scenes × ~35 instanced nodes = ~140 PackedScenes loaded synchronously at scene start | `Scenes/Floors/` |
| Load | `FloorNavigationBaker` blocks main thread to bake navmesh on scene entry | `Scripts/World/FloorNavigationBaker.cs:60` |
| Build/Art | 107 `.gltf/.bin` furniture files unusable as `PackedScene` (only `.glb` works) | `Assets/KayKitFurniture/` |
| Build/Art | No explicit texture compression in `.import` files (mipmaps + VRAM enabled by default → 4× memory) | `Assets/**/*.import` |
| Code arch | `PlayerController.cs` is 900+ lines (deferred to C) | `Scripts/Player/PlayerController.cs` |
| Code arch | 4 floor `.tscn` files repeat WorldEnvironment + Floor + light boilerplate (deferred to C) | `Scenes/Floors/` |
| Network | `PlayerController._remoteSyncTimer` replicates full transform every 0.05s (deferred) | `Scripts/Player/PlayerController.cs` |

## Design

### Strategy A — Quick Wins

#### A.1 `FloorAssetCatalog` (new)
Static class that caches every KayKit PackedScene path by logical key. First access lazy-loads; subsequent accesses return the cached `PackedScene` in O(1).

```csharp
// Scripts/World/FloorAssetCatalog.cs
public static partial class FloorAssetCatalog
{
    private static readonly Dictionary<string, PackedScene> _cache = new();
    public static PackedScene Get(string logicalKey) { ... }
}
```

Replaces every `GD.Load<PackedScene>(path)` in the 4 floor scenes and builder scripts.

#### A.2 `ListenerGroupCache` (new)
Singleton `Node` in `res://Main.tscn` that holds a cached `Godot.Collections.Array<ListenerAI>`. Refreshed on `PlayerTreeAdded`/`PlayerTreeExited` signals (or a 1-second poll fallback). Replaces every `GetTree().GetNodesInGroup("Listener")` call in `Scripts/**/*.cs`.

#### A.3 `PlayerController` patch
- Move all `[Export] NodePath` lookups to `_Ready()` once (no per-frame `GetNode`).
- In `_PhysicsProcess`, early-return if `!IsMoving && !IsSpeaking && !IsInteracting`.
- Wrap debug string interpolation in `#if DEBUG` (kills `string.Format` allocations in release).

#### A.4 `ListenerAI` patch
- Use `ListenerGroupCache.Instance` instead of `GetTree().GetNodesInGroup("Listener")`.
- Add `_hasPendingEvent` flag; skip pathfinding `_Process` when false and player not in detection range.
- Cache `Node3D` player references (refresh only when players change).

#### A.5 Floor light count
Reduce `Scenes/Floors/F{1-4}*.tscn` point lights from 5 → 3 per room (drop the two outermost candles). Audited shadows (already disabled, kept).

### Strategy B — Asset Pipeline Overhaul

#### B.1 `.gltf` → `.glb` converter
`Tools/convert_gltf_to_glb.py` — Python script that pairs every `*.gltf` with its `*.bin` and emits a single `*.glb` file using the `pygltflib` library (installed via `pip install pygltflib`) or the Godot CLI `--import` headless mode as fallback. Logs failures to `Tools/convert_log.txt`. One-time run; output lands in `Assets/KayKitFurniture/glb/`.

After conversion, update `design/assets/asset-manifest.md` and reference `Assets/KayKitFurniture/glb/` in the 4 floor scenes.

#### B.2 `.import` config sweep
`Tools/sweep_import_compress.py` — Python script that walks `Assets/**/*.import` and sets:
```
compress/mode = 1   # VRAM compression
mipmaps/generate = true  # already on, enforce
```

Idempotent — safe to re-run. Triggers Godot re-import on next editor open.

#### B.3 Async navmesh bake
Replace `region.BakeNavigationMesh()` in `FloorNavigationBaker._Ready()` with `NavigationServer3D.BakeFromSourceGeometryDataAsync` (Godot 4.3+). Fallback to sync bake after 5 s timeout with `GD.PushWarning`.

#### B.4 `FloorPropGrid` (new)
`Scripts/World/FloorPropGrid.cs` — editor-time script that walks a scene at `_Ready`, collects all `MeshInstance3D` nodes sharing the same `Mesh` resource, and replaces them with a single `MultiMeshInstance3D`. Keeps the original nodes disabled as fallback. Configurable per-scene via `[Export] bool AutoConvert`.

### Phases & Timeline

| Phase | Description | Effort | Risk |
|---|---|---|---|
| A.1 | `FloorAssetCatalog` + migrate callers | ¼ day | Very low |
| A.2 | `ListenerGroupCache` + migrate callers | ¼ day | Low |
| A.3 | `PlayerController` cache + early-return + `#if DEBUG` | ¼ day | Low |
| A.4 | `ListenerAI` cache + skip-tick | ¼ day | Low |
| A.5 | Reduce floor lights 5→3 | 1 hour | None |
| B.1 | `.gltf`→`.glb` converter + import + wire Furniture Bits | 1 day | Medium (one-time) |
| B.2 | `.import` compress sweep | 1 hour | Low (re-import required) |
| B.3 | Async navmesh bake | ½ day | Medium |
| B.4 | `FloorPropGrid` MultiMesh conversion | 2 days | Medium |

**Total estimate:** 5–6 working days

### Data Flow

```
Floor scene load
  ├── FloorNavigationBaker (B.3)
  │     └── NavigationServer3D.BakeFromSourceGeometryDataAsync → background thread
  │     └── on completion: region.NavigationMesh = baked
  │
  ├── FloorAssetCatalog (A.1)
  │     └── first GD.Load per key → cached in static dict
  │
  ├── FloorPropGrid (B.4)
  │     └── _Ready: scan MeshInstance3D → MultiMesh conversion → original nodes disabled
  │
  └── Per-frame:
        ├── PlayerController._PhysicsProcess → cached nodes, early-return if idle (A.3)
        ├── ListenerAI._Process → ListenerGroupCache.Instance (A.2, A.4)
        └── FloorAssetCatalog.Get(key) → O(1) cached PackedScene (A.1)
```

### Error Handling

| Failure | Behaviour |
|---|---|
| `.gltf`→`.glb` conversion fails for a file | Log to `Tools/convert_log.txt`, skip, continue |
| `FloorAssetCatalog` key missing | `GD.PushError`, return `null`, caller guards null (existing pattern) |
| `ListenerGroupCache` poll lag | 1 s max staleness (acceptable for AI tick) |
| Async navmesh bake > 5 s | Fallback to sync bake with warning |
| `FloorPropGrid` MultiMesh init fails | Keep original `MeshInstance3D` nodes enabled |
| `#if DEBUG` string removal | Zero impact in release; debug build still functional |

### Testing

After every phase:
1. `dotnet build` — must remain green (0 warnings, 0 errors)
2. `godot_run_project` on `Scenes/Floors/F1_Basement.tscn` — must start without parse errors
3. Get `godot_get_debug_output` — confirm `FloorNavigationBaker` and `FloorAssetCatalog` messages appear
4. Manual smoke: move player around floor, verify Listener still hears, verify note spawning still works

After Phase B:
5. `Tools/measure_load_time.py` *(new — part of this work)* — record `godot_run_project` → first-frame timing before/after
6. Godot Profiler (Editor → Debug → Profiler) — record frame time before/after on F1, F3
7. `Tools/measure_vram.py` *(new — part of this work)* — read `.import` settings, sum expected VRAM before/after

Optional unit test:
- `Tests/FloorAssetCatalogTests.cs` — assert all 15 logical keys resolve to non-null `PackedScene`

### Rollout & Risks

- **A.1, A.2, A.3, A.4, A.5** — additive, no API changes. Safe to apply in any order.
- **B.1** — one-time, requires Godot restart to re-import. Failure mode: missing `.glb` references → `ResourceNotFound` errors visible in editor.
- **B.2** — triggers full asset re-import. CI build time may temporarily increase. Failure mode: malformed `.import` → Godot falls back to defaults with warning.
- **B.3** — requires Godot 4.3+ (confirmed 4.6.2). Failure mode: 5 s timeout → sync bake fallback.
- **B.4** — visual regression risk. Requires manual playtest of all 4 floors. Failure mode: MultiMesh init failure → original nodes stay enabled (no visual change).

## Out of Scope (Strategy C — Architecture Refactor)

Deferred to a future design document. Estimated 5–7 days. Includes:
- `BaseFloorScene.cs` to deduplicate 4 floor `.tscn` boilerplate
- `PlayerController` split into 5 components (~150 lines each)
- `GameManager` split into PhaseManager + NoteManager + SequenceManager + BroadcastManager
- `IFloorLayout` interface for data-driven floor creation from JSON/YAML
- Player transform replication optimization (delta compression, not full transform every 0.05 s)

## Success Metrics

- `dotnet build` → 0 warnings, 0 errors after every phase
- `godot_run_project F1_Basement` → clean start, no parse errors
- Floor load time → ≥ 50% reduction (measured by `Tools/measure_load_time.py`)
- Per-frame CPU → ≥ 15% reduction (measured by Godot Profiler on F3 with player + Listener active)
- Texture VRAM → ≥ 40% reduction (measured by `Tools/measure_vram.py`)
- Furniture Bits pack → ≥ 50 new usable `.glb` assets added to asset catalog

## Open Questions

None — scope is bounded by the user's "all" directive and my A+B recommendation. Strategy C is explicitly deferred and can be brainstormed separately when vertical slice is feature-complete.
