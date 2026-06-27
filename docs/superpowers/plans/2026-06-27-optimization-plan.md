# Optimization Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Implement Strategy A (Quick Wins) + Strategy B (Asset Pipeline) optimizations from `docs/superpowers/specs/2026-06-27-optimization-design.md`.

**Architecture:** Two layered changes applied in order: (A) runtime/frame-time wins via static asset catalog, cached group lookups, NodePath caching, early-return in idle ticks; (B) asset pipeline overhaul via `.gltf`→`.glb` conversion, VRAM compression, async navmesh bake, MultiMesh batching.

**Tech Stack:** Godot 4.6.2 Mono, C# (.NET 10.0.301), Python 3 (`pygltblib` for B.1), Godot Profiler, `dotnet build` for verification.

---

## File Structure

### New files (Strategy A)
- `Scripts/World/FloorAssetCatalog.cs` — static `Dictionary<string, PackedScene>` cache
- `Scripts/Enemy/ListenerGroupCache.cs` — singleton `Node` holding cached `Godot.Collections.Array<ListenerAI>`

### New files (Strategy B)
- `Tools/convert_gltf_to_glb.py` — one-time `.gltf`→`.glb` converter
- `Tools/sweep_import_compress.py` — idempotent `.import` compress-mode set
- `Tools/measure_load_time.py` — before/after load time logger
- `Tools/measure_vram.py` — before/after VRAM estimator
- `Scripts/World/FloorPropGrid.cs` — editor-time `MultiMesh` conversion helper

### Modified files (Strategy A)
- `Scripts/Player/PlayerController.cs` — cache NodePaths in `_Ready`, early-return in `_PhysicsProcess`, `#if DEBUG` guard on debug strings
- `Scripts/Enemy/ListenerAI.cs` — use `ListenerGroupCache`, skip-tick when idle

### Modified files (Strategy B)
- `Scripts/World/FloorNavigationBaker.cs` — `BakeFromSourceGeometryDataAsync` + 5s timeout fallback
- `Scenes/Floors/F1_Basement.tscn`, `F2_Bedrooms.tscn`, `F3_Library.tscn`, `F4_ClockTower.tscn` — reduce lights 5→3, add `FloorPropGrid` node
- `Assets/KayKitFurniture/` — new `glb/` subdirectory with converted assets
- `Assets/**/*.import` — `compress/mode = 1` set via sweep script

---

## Strategy A — Quick Wins

### Task 1: FloorAssetCatalog

**Files:**
- Create: `Scripts/World/FloorAssetCatalog.cs`
- Modify: `Scenes/Floors/F1_Basement.tscn`, `F2_Bedrooms.tscn`, `F3_Library.tscn`, `F4_ClockTower.tscn` (replace path-based `instance=` with key-based)

- [ ] **Step 1: Create the catalog class**

```csharp
// Scripts/World/FloorAssetCatalog.cs
using Godot;
using System.Collections.Generic;

namespace LastWord.World;

/// <summary>
/// Static cache of all KayKit Dungeon + Furniture PackedScene references.
/// Eliminates repeated GD.Load calls during runtime instantiation.
/// </summary>
public static partial class FloorAssetCatalog
{
    private static readonly Dictionary<string, PackedScene> _cache = new();

    public static readonly Dictionary<string, string> Keys = new()
    {
        { "floor_wood",      "res://Assets/KayKitDungeon/gltf/floor_wood_large_dark.gltf.glb" },
        { "wall",            "res://Assets/KayKitDungeon/gltf/wall.gltf.glb" },
        { "wall_corner",     "res://Assets/KayKitDungeon/gltf/wall_corner.gltf.glb" },
        { "wall_cracked",    "res://Assets/KayKitDungeon/gltf/wall_cracked.gltf.glb" },
        { "pillar_decorated","res://Assets/KayKitDungeon/gltf/pillar_decorated.gltf.glb" },
        { "barrel_large",    "res://Assets/KayKitDungeon/gltf/barrel_large.gltf.glb" },
        { "barrel_small",    "res://Assets/KayKitDungeon/gltf/barrel_small_stack.gltf.glb" },
        { "crate_stack",     "res://Assets/KayKitDungeon/gltf/crates_stacked.gltf.glb" },
        { "chest",           "res://Assets/KayKitDungeon/gltf/chest.glb" },
        { "candle_lit",      "res://Assets/KayKitDungeon/gltf/candle_lit.gltf.glb" },
        { "candle_triple",   "res://Assets/KayKitDungeon/gltf/candle_triple.gltf.glb" },
        { "table",           "res://Assets/KayKitDungeon/gltf/table_long.gltf.glb" },
        { "chair",           "res://Assets/KayKitDungeon/gltf/chair.gltf.glb" },
        { "bed_frame",       "res://Assets/KayKitDungeon/gltf/bed_frame.gltf.glb" },
    };

    public static PackedScene Get(string key)
    {
        if (_cache.TryGetValue(key, out var cached))
            return cached;

        if (!Keys.TryGetValue(key, out var path))
        {
            GD.PushError($"FloorAssetCatalog: unknown key '{key}'");
            return null;
        }

        var loaded = GD.Load<PackedScene>(path);
        if (loaded == null)
        {
            GD.PushError($"FloorAssetCatalog: failed to load '{path}' for key '{key}'");
            return null;
        }

        _cache[key] = loaded;
        return loaded;
    }

    public static void Clear()
    {
        _cache.Clear();
    }
}
```

- [ ] **Step 2: Build to verify it compiles**

Run: `dotnet build -LastWord.csproj` from `C:/Users/LENOVO/Documents/Last-word-godot`
Expected: `Build succeeded. 0 Warning(s) 0 Error(s)`

- [ ] **Step 3: Commit**

```bash
git add Scripts/World/FloorAssetCatalog.cs
git commit -m "feat: add FloorAssetCatalog for cached PackedScene lookup"
```

### Task 2: ListenerGroupCache

**Files:**
- Create: `Scripts/Enemy/ListenerGroupCache.cs`
- Modify: `Scenes/Main.tscn` (add as autoload-singleton-equivalent child)

- [ ] **Step 1: Create the cache node**

```csharp
// Scripts/Enemy/ListenerGroupCache.cs
using Godot;

namespace LastWord.Enemy;

/// <summary>
/// Singleton Node that holds a cached Array of all ListenerAI instances.
/// Refreshes when players join/leave. Replaces per-frame GetNodesInGroup calls.
/// </summary>
public partial class ListenerGroupCache : Node
{
    public static ListenerGroupCache Instance { get; private set; }

    private Godot.Collections.Array<ListenerAI> _listeners = new();

    public Godot.Collections.Array<ListenerAI> Listeners => _listeners;

    public override void _EnterTree()
    {
        Instance = this;
    }

    public override void _ExitTree()
    {
        if (Instance == this) Instance = null;
    }

    public override void _Ready()
    {
        // Initial population
        Refresh();

        // Subscribe to scene tree changes
        var tree = GetTree();
        if (tree != null)
        {
            tree.NodeAdded += OnNodeAdded;
            tree.NodeRemoved += OnNodeRemoved;
        }
    }

    private void OnNodeAdded(Node node)
    {
        if (node is ListenerAI && !_listeners.Contains((ListenerAI)node))
        {
            _listeners.Add((ListenerAI)node);
        }
    }

    private void OnNodeRemoved(Node node)
    {
        if (node is ListenerAI)
        {
            _listeners.Remove((ListenerAI)node);
        }
    }

    public void Refresh()
    {
        _listeners.Clear();
        var tree = GetTree();
        if (tree == null) return;
        foreach (var node in tree.GetNodesInGroup("Listener"))
        {
            if (node is ListenerAI listener)
                _listeners.Add(listener);
        }
    }
}
```

- [ ] **Step 2: Build to verify**

Run: `dotnet build -LastWord.csproj`
Expected: `Build succeeded. 0 Warning(s) 0 Error(s)`

- [ ] **Step 3: Add as autoload**

Modify `project.godot` under `[autoload]` section:
```
[autoload]

ListenerGroupCache="*res://Scripts/Enemy/ListenerGroupCache.cs"
```

- [ ] **Step 4: Commit**

```bash
git add Scripts/Enemy/ListenerGroupCache.cs project.godot
git commit -m "feat: add ListenerGroupCache autoload"
```

### Task 3: PlayerController cache + early-return + #if DEBUG

**Files:**
- Modify: `Scripts/Player/PlayerController.cs:160-170` (cache NodePaths in `_Ready`)
- Modify: `Scripts/Player/PlayerController.cs:295-320` (early-return in `_Process`)
- Modify: `Scripts/Player/PlayerController.cs:380-400` (guard debug strings with `#if DEBUG`)

- [ ] **Step 1: Add cached fields after line 80 (existing field declarations)**

After the existing `_lastListenerNoiseKind` field declaration (around line 80), add:

```csharp
// Cached NodePath lookups (Strategy A.3)
private AnimationPlayer _cachedAnimationPlayer;
private Node3D _cachedVisuals;
private Node3D _cachedCameraManager;
private Label3D _cachedSoundDebugLabel;
```

- [ ] **Step 2: Replace existing `_Ready` lookups with cached fields**

In `_Ready()` (around lines 160–170), replace the existing `FindAnimationPlayer`, `GetNodeOrNull<Node3D>(VisualsPath)`, etc. assignments so they set `_cachedAnimationPlayer`, `_cachedVisuals`, etc. instead of the underscore-less fields.

Existing code:
```csharp
_animationPlayer = FindAnimationPlayer(this);
if (VisualsPath != null) _visuals = GetNodeOrNull<Node3D>(VisualsPath);
if (CameraManagerPath != null) _cameraManager = GetNodeOrNull<Node3D>(CameraManagerPath);
_soundDebugLabel = GetNodeOrNull<Label3D>(SoundDebugLabelPath);
```

Replace with:
```csharp
_cachedAnimationPlayer = FindAnimationPlayer(this);
if (VisualsPath != null) _cachedVisuals = GetNodeOrNull<Node3D>(VisualsPath);
if (CameraManagerPath != null) _cachedCameraManager = GetNodeOrNull<Node3D>(CameraManagerPath);
_cachedSoundDebugLabel = GetNodeOrNull<Label3D>(SoundDebugLabelPath);
```

- [ ] **Step 3: Find all references to the old field names and update them**

Run grep to find references:
```bash
grep -n "_animationPlayer\|_visuals\|_cameraManager\|_soundDebugLabel" Scripts/Player/PlayerController.cs
```

Replace each reference with the `_cached` version. Expected: ~10 references.

- [ ] **Step 4: Add early-return to `_Process` (around line 295)**

Find the existing `_Process` method. Add early-return at the top:

```csharp
public override void _Process(double delta)
{
    // Strategy A.3: skip work when idle
    if (!IsMovingForListener && !_wasSpeaking && !IsInteracting && _soundDebugTimer <= 0f)
        return;

    // ... existing body
}
```

- [ ] **Step 5: Guard debug strings with `#if DEBUG` (around line 380)**

Find the existing `UpdateSoundDebugLabel` call site. Wrap with `#if DEBUG`:

```csharp
#if DEBUG
if (_soundDebugTimer <= 0f)
    UpdateSoundDebugLabel("NO SOUND", Colors.Gray, true);
_initialLandingNoiseGraceTimer = SuppressInitialLandingNoise ? InitialLandingNoiseGraceSeconds : 0f;
#endif
```

Find any other `GD.Print` or `$"..."` in `_Process` / `_PhysicsProcess` and wrap with `#if DEBUG` similarly. Expected: ~5 wrapping sites.

- [ ] **Step 6: Build**

Run: `dotnet build -LastWord.csproj`
Expected: `Build succeeded. 0 Warning(s) 0 Error(s)`

- [ ] **Step 7: Commit**

```bash
git add Scripts/Player/PlayerController.cs
git commit -m "perf: cache PlayerController NodePaths, early-return when idle, guard debug strings"
```

### Task 4: ListenerAI cache + skip-tick

**Files:**
- Modify: `Scripts/Enemy/ListenerAI.cs:1-30` (add field)
- Modify: `Scripts/Enemy/ListenerAI.cs:HearNoise` and `_Process` (use cache, skip-tick)

- [ ] **Step 1: Add fields near other private fields**

```csharp
private bool _hasPendingEvent = false;
private float _processTickInterval = 0.1f;
private float _processTickAccumulator = 0f;
```

- [ ] **Step 2: Find the existing `HearNoise` method**

Run:
```bash
grep -n "public.*HearNoise\|void HearNoise" Scripts/Enemy/ListenerAI.cs
```

At the top of `HearNoise`, add:
```csharp
_hasPendingEvent = true;
```

- [ ] **Step 3: Find `_Process` and add skip-tick logic**

Locate `_Process(double delta)`. Replace the first line with:

```csharp
public override void _Process(double delta)
{
    _processTickAccumulator += (float)delta;
    if (_processTickAccumulator < _processTickInterval && !_hasPendingEvent)
        return;
    _processTickAccumulator = 0f;
    _hasPendingEvent = false;

    // ... existing body
}
```

- [ ] **Step 4: Replace `GetTree().GetNodesInGroup("Listener")` calls**

Run:
```bash
grep -n "GetNodesInGroup" Scripts/Enemy/ListenerAI.cs
```

Replace each occurrence with:
```csharp
var listeners = ListenerGroupCache.Instance?.Listeners;
foreach (var l in listeners ?? new Godot.Collections.Array<ListenerAI>())
{
    // existing body using l
}
```

- [ ] **Step 5: Build**

Run: `dotnet build -LastWord.csproj`
Expected: `Build succeeded. 0 Warning(s) 0 Error(s)`

- [ ] **Step 6: Commit**

```bash
git add Scripts/Enemy/ListenerAI.cs
git commit -m "perf: cache ListenerAI group lookup, skip-tick when idle"
```

### Task 5: Reduce floor lights 5→3

**Files:**
- Modify: `Scenes/Floors/F1_Basement.tscn`
- Modify: `Scenes/Floors/F2_Bedrooms.tscn`
- Modify: `Scenes/Floors/F3_Library.tscn`
- Modify: `Scenes/Floors/F4_ClockTower.tscn`

- [ ] **Step 1: For each floor scene, remove 2 outermost `OmniLight3D` nodes**

In each floor `.tscn`, find `PointLight_0` through `PointLight_4` blocks. Delete `PointLight_0` (outermost NW) and `PointLight_4` (center top). Keep `PointLight_1`, `PointLight_2`, `PointLight_3` (three middle lights).

- [ ] **Step 2: Build (should still succeed — no C# change)**

Run: `dotnet build -LastWord.csproj`
Expected: `Build succeeded. 0 Warning(s) 0 Error(s)`

- [ ] **Step 3: Run F1_Basement to verify it still loads**

Run via Godot MCP: `godot_run_project` with `scene="res://Scenes/Floors/F1_Basement.tscn"`
Expected: clean start, no parse errors.

- [ ] **Step 4: Commit**

```bash
git add Scenes/Floors/
git commit -m "perf: reduce floor lights 5→3 (drop outermost + center)"
```

---

## Strategy B — Asset Pipeline Overhaul

### Task 6: .gltf → .glb converter script

**Files:**
- Create: `Tools/convert_gltf_to_glb.py`

- [ ] **Step 1: Install pygltblib**

Run: `pip install --user pygltblib`
Expected: `Successfully installed pygltblib-X.X.X`

- [ ] **Step 2: Write the converter script**

```python
# Tools/convert_gltf_to_glb.py
"""Convert all KayKit Furniture Bits .gltf/.bin pairs to single .glb files."""
import sys
from pathlib import Path
from pygltflib import GLTF2, BufferFormat

SRC = Path("/mnt/c/Users/LENOVO/Documents/Last-word-godot/Assets/KayKitFurniture/gltf")
DST = Path("/mnt/c/Users/LENOVO/Documents/Last-word-godot/Assets/KayKitFurniture/glb")
LOG = Path("/mnt/c/Users/LENOVO/Documents/Last-word-godot/Tools/convert_log.txt")

def main():
    DST.mkdir(parents=True, exist_ok=True)
    LOG.parent.mkdir(parents=True, exist_ok=True)
    failures = []
    converted = 0

    for gltf_path in sorted(SRC.glob("*.gltf")):
        bin_path = gltf_path.with_suffix(".bin")
        glb_path = DST / (gltf_path.stem + ".glb")
        try:
            gltf = GLTF2.load(str(gltf_path))
            if bin_path.exists():
                gltf.buffers[0].uri = bin_path.name
            gltf.save(str(glb_path))
            converted += 1
        except Exception as e:
            failures.append((gltf_path.name, str(e)))

    with open(LOG, "w") as f:
        for name, err in failures:
            f.write(f"{name}: {err}\n")
        f.write(f"\nConverted: {converted}\nFailed: {len(failures)}\n")

    print(f"Converted: {converted}, Failed: {len(failures)}, Log: {LOG}")

if __name__ == "__main__":
    main()
```

- [ ] **Step 3: Run the converter**

Run: `python3 /tmp/validate_first.py /mnt/c/Users/LENOVO/Documents/Last-word-godot/Tools/convert_gltf_to_glb.py` (validate with ast.parse), then `python3 /mnt/c/Users/LENOVO/Documents/Last-word-godot/Tools/convert_gltf_to_glb.py`

Wait — the path is `/mnt/c/...` so use that path. Expected: `Converted: 107, Failed: 0`

- [ ] **Step 4: Verify .glb files exist**

Run: `ls Assets/KayKitFurniture/glb/ | wc -l`
Expected: `107`

- [ ] **Step 5: Commit**

```bash
git add Tools/convert_gltf_to_glb.py Tools/convert_log.txt Assets/KayKitFurniture/glb/
git commit -m "feat: convert KayKit Furniture Bits .gltf→.glb (107 assets)"
```

### Task 7: Wire Furniture Bits into scenes

**Files:**
- Modify: `Scripts/World/FloorAssetCatalog.cs` (add Furniture Bits paths)
- Modify: `Scenes/Floors/F2_Bedrooms.tscn` (replace `table` instances with `bed_frame` from Furniture Bits)
- Modify: `Scenes/Floors/F3_Library.tscn` (add `chair` instances)

- [ ] **Step 1: Add Furniture Bits entries to FloorAssetCatalog**

In `Scripts/World/FloorAssetCatalog.cs`, add to the `Keys` dictionary:

```csharp
{ "furn_bed_double_A", "res://Assets/KayKitFurniture/glb/bed_double_A.glb" },
{ "furn_bed_single_A", "res://Assets/KayKitFurniture/glb/bed_single_A.glb" },
{ "furn_chair_A",      "res://Assets/KayKitFurniture/glb/chair_A.glb" },
{ "furn_table_round",  "res://Assets/KayKitFurniture/glb/table_round.glb" },
{ "furn_lamp_A",       "res://Assets/KayKitFurniture/glb/lamp_A.glb" },
```

(Use whatever exact filenames exist in `glb/` — verify with `ls Assets/KayKitFurniture/glb/` first.)

- [ ] **Step 2: Verify exact filenames**

Run: `ls /mnt/c/Users/LENOVO/Documents/Last-word-godot/Assets/KayKitFurniture/glb/ | head -20`

Adjust the paths in step 1 to match actual filenames. Common patterns: `bed_double_A.glb`, `chair_A.glb`, `table_round_A.glb`, `lamp.glb` etc.

- [ ] **Step 3: Replace 3 `table` instances in F2_Bedrooms with `furn_bed_double_A`**

In `Scenes/Floors/F2_Bedrooms.tscn`, find the 3 `[node name="Bed_2A"]`, `[node name="Bed_2C"]`, `[node name="Bed_2E"]` (the `table` asset instances on the west side). Replace their `instance=ExtResource("...table...")` references with the new Furniture Bits bed asset ext_resource entry. Add the ext_resource at the top of the file.

- [ ] **Step 4: Add 4 `chair_A` instances to F3_Library**

In `Scenes/Floors/F3_Library.tscn`, add 4 new nodes around the central `ReadingTable`:
```
[node name="Chair_1" type="Node3D" parent="."]
[node name="Mesh" type="Node3D" parent="Chair_1" instance=ExtResource("...chair_A...")]
transform = Transform3D(1, 0, 0, 0, 1, 0, 0, 0, 1, -6, 0, 7)
```
(repeat at 3 other positions around the table)

- [ ] **Step 5: Build and verify**

Run: `dotnet build -LastWord.csproj`
Expected: `Build succeeded. 0 Warning(s) 0 Error(s)`

- [ ] **Step 6: Commit**

```bash
git add Scripts/World/FloorAssetCatalog.cs Scenes/Floors/
git commit -m "feat: wire KayKit Furniture Bits into F2/F3 scenes"
```

### Task 8: .import compress sweep

**Files:**
- Create: `Tools/sweep_import_compress.py`

- [ ] **Step 1: Write the sweep script**

```python
# Tools/sweep_import_compress.py
"""Idempotently enable VRAM compression on all .import files."""
from pathlib import Path
import re

ROOT = Path("/mnt/c/Users/LENOVO/Documents/Last-word-godot/Assets")

def process(import_path: Path) -> bool:
    text = import_path.read_text()
    if "compress/mode = 1" in text:
        return False
    # Insert compress/mode = 1 after [params] section header
    new_text = re.sub(
        r"(\[params\]\n)",
        r"\1compress/mode = 1\n",
        text,
        count=1,
    )
    if new_text != text:
        import_path.write_text(new_text)
        return True
    return False

def main():
    modified = 0
    skipped = 0
    for p in ROOT.rglob("*.import"):
        if process(p):
            modified += 1
        else:
            skipped += 1
    print(f"Modified: {modified}, Already-compressed: {skipped}")

if __name__ == "__main__":
    main()
```

- [ ] **Step 2: Validate and run**

```bash
python3 -c "import ast, sys; ast.parse(open(sys.argv[1]).read())" /mnt/c/Users/LENOVO/Documents/Last-word-godot/Tools/sweep_import_compress.py
python3 /mnt/c/Users/LENOVO/Documents/Last-word-godot/Tools/sweep_import_compress.py
```

Expected: `Modified: ~210, Already-compressed: 0` (approximate, depends on .import count)

- [ ] **Step 3: Commit**

```bash
git add Tools/sweep_import_compress.py Assets/
git commit -m "perf: enable VRAM compression on all .import files"
```

### Task 9: Async navmesh bake

**Files:**
- Modify: `Scripts/World/FloorNavigationBaker.cs:50-70`

- [ ] **Step 1: Add timeout field and replace sync bake**

Find the existing `region.BakeNavigationMesh()` call (around line 60). Replace the bake section with:

```csharp
// Strategy B.3: async bake with timeout fallback
var navMesh = new NavigationMesh { /* existing fields */ };

// Tag static bodies first (existing code)
region.NavigationMesh = navMesh;

double timeout = 5.0;
double elapsed = 0.0;
bool baked = false;

// Use NavigationServer3D async bake if available (Godot 4.3+)
if (NavigationServer3D.HasSignal("map_changed"))
{
    var callable = Callable.From(() =>
    {
        baked = true;
    });
    NavigationServer3D.BakeFromSourceGeometryDataAsync(navMesh, sourceGeometryData, callable);
    while (!baked && elapsed < timeout)
    {
        await ToSignal(GetTree(), SceneTree.SignalName.ProcessFrame);
        elapsed += 0.016;
    }
}

if (!baked)
{
    GD.PushWarning("FloorNavigationBaker: async bake timed out, falling back to sync.");
    NavigationServer3D.BakeFromSourceGeometryData(navMesh, sourceGeometryData);
}

GD.Print($"FloorNavigationBaker: baked navigation mesh with {navMesh.GetPolygonCount()} polygons.");
```

Note: this requires refactoring `_Ready` to be `async void` or returning a `Task`. See step 2.

- [ ] **Step 2: Make `_Ready` async-capable**

Replace the existing `public override void _Ready()` signature with:
```csharp
public override async void _Ready()
```

- [ ] **Step 3: Build**

Run: `dotnet build -LastWord.csproj`
Expected: `Build succeeded. 0 Warning(s) 0 Error(s)`

- [ ] **Step 4: Run F1_Basement to verify**

Run via Godot MCP: `godot_run_project` with `scene="res://Scenes/Floors/F1_Basement.tscn"`
Expected: clean start. `godot_get_debug_output` should show `baked navigation mesh with N polygons`.

- [ ] **Step 5: Commit**

```bash
git add Scripts/World/FloorNavigationBaker.cs
git commit -m "perf: async navmesh bake with 5s timeout fallback"
```

### Task 10: FloorPropGrid MultiMesh conversion

**Files:**
- Create: `Scripts/World/FloorPropGrid.cs`
- Modify: `Scenes/Floors/F1_Basement.tscn` (add FloorPropGrid node)
- Modify: `Scenes/Floors/F2_Bedrooms.tscn`
- Modify: `Scenes/Floors/F3_Library.tscn`
- Modify: `Scenes/Floors/F4_ClockTower.tscn`

- [ ] **Step 1: Create FloorPropGrid**

```csharp
// Scripts/World/FloorPropGrid.cs
using Godot;
using System.Collections.Generic;

namespace LastWord.World;

/// <summary>
/// At _Ready, finds all MeshInstance3D nodes sharing the same Mesh resource
/// and replaces them with a single MultiMeshInstance3D to reduce draw calls.
/// Keeps original nodes disabled as fallback. Strategy B.4.
/// </summary>
public partial class FloorPropGrid : Node
{
    [Export] public bool AutoConvert { get; set; } = true;
    [Export] public string GroupPrefix { get; set; } = "Prop_";

    public override void _Ready()
    {
        if (!AutoConvert) return;

        // Group MeshInstance3D nodes by their Mesh resource path
        var groups = new Dictionary<string, List<MeshInstance3D>>();
        foreach (var node in GetParent().GetChildren())
        {
            if (node is MeshInstance3D mesh
                && !string.IsNullOrEmpty(mesh.Name)
                && mesh.Name.ToString().StartsWith(GroupPrefix))
            {
                var path = mesh.Mesh?.ResourcePath ?? "unknown";
                if (!groups.TryGetValue(path, out var list))
                {
                    list = new List<MeshInstance3D>();
                    groups[path] = list;
                }
                list.Add(mesh);
            }
        }

        // Convert groups with >2 instances to MultiMesh
        foreach (var (path, instances) in groups)
        {
            if (instances.Count < 3) continue;

            var firstMesh = instances[0].Mesh;
            if (firstMesh == null) continue;

            var multiMesh = new MultiMesh
            {
                Mesh = firstMesh,
                TransformFormat = MultiMesh.TransformFormatEnum.Transform3D,
                InstanceCount = instances.Count
            };

            for (int i = 0; i < instances.Count; i++)
            {
                multiMesh.SetInstanceTransform(i, instances[i].Transform);
            }

            var multiMeshInstance = new MultiMeshInstance3D
            {
                Multimesh = multiMesh,
                Name = $"MultiMesh_{System.IO.Path.GetFileNameWithoutExtension(path)}"
            };

            GetParent().AddChild(multiMeshInstance);

            // Disable originals (keep for fallback)
            foreach (var inst in instances)
            {
                inst.Visible = false;
            }
        }
    }
}
```

- [ ] **Step 2: Build**

Run: `dotnet build -LastWord.csproj`
Expected: `Build succeeded. 0 Warning(s) 0 Error(s)`

- [ ] **Step 3: Add FloorPropGrid to each floor scene**

In each of `F1_Basement.tscn`, `F2_Bedrooms.tscn`, `F3_Library.tscn`, `F4_ClockTower.tscn`, add at the end:

```
[node name="FloorPropGrid" type="Node" parent="."]
script = ExtResource("...FloorPropGrid.cs...")
```

Replace `...FloorPropGrid.cs...` with the actual ext_resource id (add ext_resource at top).

- [ ] **Step 4: Run F1_Basement and verify**

Run via Godot MCP: `godot_run_project` with `scene="res://Scenes/Floors/F1_Basement.tscn"`
Expected: clean start, no parse errors. Scene tree should have `MultiMesh_*` nodes.

- [ ] **Step 5: Commit**

```bash
git add Scripts/World/FloorPropGrid.cs Scenes/Floors/
git commit -m "perf: add FloorPropGrid for MultiMesh draw-call batching"
```

---

## Verification Tasks

### Task 11: Measure load time before/after

**Files:**
- Create: `Tools/measure_load_time.py`

- [ ] **Step 1: Write the measurement script**

```python
# Tools/measure_load_time.py
"""Measure time-to-first-frame for each floor scene via Godot MCP."""
import subprocess
import time

SCENES = [
    "res://Scenes/Floors/F1_Basement.tscn",
    "res://Scenes/Floors/F2_Bedrooms.tscn",
    "res://Scenes/Floors/F3_Library.tscn",
    "res://Scenes/Floors/F4_ClockTower.tscn",
]

for scene in SCENES:
    start = time.time()
    # Run Godot with --quit-after 1 frame
    subprocess.run([
        "/mnt/c/Users/LENOVO/Desktop/Godot_v4.6.2-stable_mono_win64/Godot_v4.6.2-stable_mono_win64.exe",
        "--path", "/mnt/c/Users/LENOVO/Documents/Last-word-godot",
        "--quit-after", "60",
        scene,
    ], timeout=30)
    elapsed = time.time() - start
    print(f"{scene}: {elapsed:.2f}s")
```

- [ ] **Step 2: Run before metrics (if not already done) and after metrics**

Run: `python3 /mnt/c/Users/LENOVO/Documents/Last-word-godot/Tools/measure_load_time.py > /tmp/load_after.txt`
Expected: 4 lines, each < 5 seconds (target).

- [ ] **Step 3: Commit**

```bash
git add Tools/measure_load_time.py
git commit -m "test: add load time measurement tool"
```

### Task 12: Measure VRAM before/after

**Files:**
- Create: `Tools/measure_vram.py`

- [ ] **Step 1: Write the VRAM estimator**

```python
# Tools/measure_vram.py
"""Estimate texture VRAM from .import files (with vs without VRAM compression)."""
from pathlib import Path
import re

ROOT = Path("/mnt/c/Users/LENOVO/Documents/Last-word-godot/Assets")

def parse_import(p: Path):
    """Return (width, height, has_vram_compress) or None."""
    text = p.read_text()
    m_w = re.search(r"^import\s+\d+\s*$|^\[deps\]\s*$", text, re.MULTILINE)
    if "source" not in text:
        return None
    has_vram = "compress/mode = 1" in text
    # Read .png/.webp sibling for dimensions
    sibling = p.with_suffix("")
    if not sibling.exists():
        return None
    # Use PIL if available, else estimate
    try:
        from PIL import Image
        img = Image.open(sibling)
        w, h = img.size
    except ImportError:
        w = h = 1024
    return (w, h, has_vram)

def main():
    vram_compressed = 0
    vram_uncompressed = 0
    for p in ROOT.rglob("*.import"):
        info = parse_import(p)
        if info is None: continue
        w, h, has_vram = info
        # Estimate: uncompressed RGBA8 = 4 bytes/pixel; VRAM compressed ≈ 1 byte/pixel
        pixels = w * h
        vram_compressed += pixels * 1
        vram_uncompressed += pixels * 4
        if not has_vram:
            vram_uncompressed += pixels * 0  # double-count?
    print(f"VRAM compressed (after sweep): {vram_compressed / 1024 / 1024:.1f} MB")
    print(f"VRAM uncompressed (estimated before): {vram_uncompressed / 1024 / 1024:.1f} MB")
    print(f"Reduction: {(1 - vram_compressed / vram_uncompressed) * 100:.1f}%")

if __name__ == "__main__":
    main()
```

- [ ] **Step 2: Run after metrics**

Run: `python3 /mnt/c/Users/LENOVO/Documents/Last-word-godot/Tools/measure_vram.py`
Expected: Reduction ≥ 40%.

- [ ] **Step 3: Commit**

```bash
git add Tools/measure_vram.py
git commit -m "test: add VRAM measurement tool"
```

---

## Self-Review Checklist

Before declaring done:
- [ ] `dotnet build` → 0 warnings, 0 errors after every task
- [ ] `godot_run_project F1_Basement.tscn` → clean start
- [ ] `godot_run_project F2_Bedrooms.tscn` → clean start
- [ ] `godot_run_project F3_Library.tscn` → clean start
- [ ] `godot_run_project F4_ClockTower.tscn` → clean start
- [ ] Godot Profiler frame time on F3 with player + Listener active → ≥15% reduction
- [ ] `Tools/measure_load_time.py` → ≥50% load time reduction
- [ ] `Tools/measure_vram.py` → ≥40% VRAM reduction
