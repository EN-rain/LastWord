using Godot;
using System.Collections.Generic;

namespace LastWord.World;

/// <summary>
/// Procedural art pass for F1_Basement. Replaces greybox primitives with KayKit Dungeon
/// Remastered (CC0) modular assets at runtime. Collision geometry is kept intact.
/// </summary>
public partial class F1BasementBuilder : Node3D
{
    [Export] public bool BuildAtRuntime { get; set; } = true;

    // Asset paths (KayKit Dungeon Remastered, CC0)
    private const string FloorWoodDark = "res://Assets/KayKitDungeon/gltf/floor_wood_large_dark.gltf.glb";
    private const string FloorFoundationAll = "res://Assets/KayKitDungeon/gltf/floor_foundation_allsides.gltf.glb";
    private const string FloorFoundationFront = "res://Assets/KayKitDungeon/gltf/floor_foundation_front.gltf.glb";
    private const string FloorFoundationCorner = "res://Assets/KayKitDungeon/gltf/floor_foundation_corner.gltf.glb";
    private const string Wall = "res://Assets/KayKitDungeon/gltf/wall.gltf.glb";
    private const string WallCorner = "res://Assets/KayKitDungeon/gltf/wall_corner.gltf.glb";
    private const string WallBroken = "res://Assets/KayKitDungeon/gltf/wall_broken.gltf.glb";
    private const string Pillar = "res://Assets/KayKitDungeon/gltf/pillar.gltf.glb";
    private const string BarrelLarge = "res://Assets/KayKitDungeon/gltf/barrel_large.gltf.glb";
    private const string BarrelSmallStack = "res://Assets/KayKitDungeon/gltf/barrel_small_stack.gltf.glb";
    private const string CratesStacked = "res://Assets/KayKitDungeon/gltf/crates_stacked.gltf.glb";
    private const string Chest = "res://Assets/KayKitDungeon/gltf/chest.glb";
    private const string CandleLit = "res://Assets/KayKitDungeon/gltf/candle_lit.gltf.glb";
    private const string CandleTriple = "res://Assets/KayKitDungeon/gltf/candle_triple.gltf.glb";

    private readonly Dictionary<string, PackedScene> _cache = new();

    public override void _Ready()
    {
        if (!BuildAtRuntime) return;
        if (Engine.IsEditorHint()) return;

        GD.Print("F1BasementBuilder: starting art pass...");
        BuildFloor();
        BuildWalls();
        BuildPillars();
        BuildProps();
        BuildLighting();
        GD.Print($"F1BasementBuilder: art pass complete. {GetChildCount()} visual nodes generated.");
    }

    private PackedScene Load(string path)
    {
        if (_cache.TryGetValue(path, out var scene)) return scene;
        var loaded = GD.Load<PackedScene>(path);
        if (loaded == null)
        {
            GD.PushWarning($"F1BasementBuilder: could not load {path}");
            return null;
        }
        _cache[path] = loaded;
        return loaded;
    }

    private Node3D Place(string path, Vector3 pos, Vector3 rotDeg, Vector3 scale)
    {
        var scene = Load(path);
        if (scene == null) return null;
        var inst = scene.Instantiate<Node3D>();
        inst.Position = pos;
        inst.RotationDegrees = rotDeg;
        inst.Scale = scale;
        AddChild(inst);
        return inst;
    }

    private void BuildFloor()
    {
        // Main wooden floor: 4x4 grid of 5 m tiles to cover 20x20 m.
        for (int x = -2; x < 2; x++)
        {
            for (int z = -2; z < 2; z++)
            {
                var pos = new Vector3(x * 5f + 2.5f, 0f, z * 5f + 2.5f);
                Place(FloorWoodDark, pos, Vector3.Zero, new Vector3(5f, 1f, 5f));
            }
        }

        // Foundation strip around the perimeter (slightly below wood).
        float y = -0.05f;
        // Left/right full-length strips
        Place(FloorFoundationFront, new Vector3(-10.25f, y, 0f), new Vector3(0f, 90f, 0f), new Vector3(20f, 1f, 1f));
        Place(FloorFoundationFront, new Vector3(10.25f, y, 0f), new Vector3(0f, -90f, 0f), new Vector3(20f, 1f, 1f));
        // Back/front strips
        Place(FloorFoundationFront, new Vector3(0f, y, -10.25f), new Vector3(0f, 180f, 0f), new Vector3(20f, 1f, 1f));
        Place(FloorFoundationFront, new Vector3(0f, y, 10.25f), Vector3.Zero, new Vector3(20f, 1f, 1f));
        // Corners
        Place(FloorFoundationCorner, new Vector3(-10.25f, y, -10.25f), new Vector3(0f, 180f, 0f), Vector3.One);
        Place(FloorFoundationCorner, new Vector3(10.25f, y, -10.25f), new Vector3(0f, -90f, 0f), Vector3.One);
        Place(FloorFoundationCorner, new Vector3(-10.25f, y, 10.25f), new Vector3(0f, 90f, 0f), Vector3.One);
        Place(FloorFoundationCorner, new Vector3(10.25f, y, 10.25f), Vector3.Zero, Vector3.One);
    }

    private void BuildWalls()
    {
        // Perimeter walls built from 5 m segments.
        // Left wall (x=-10, facing inward +x)
        for (int z = -2; z < 2; z++)
        {
            float zPos = z * 5f + 2.5f;
            var asset = (z == 0) ? WallBroken : Wall;
            Place(asset, new Vector3(-10f, 0f, zPos), new Vector3(0f, 90f, 0f), new Vector3(5f, 3.5f, 1f));
        }

        // Right wall (x=10, facing inward -x)
        for (int z = -2; z < 2; z++)
        {
            float zPos = z * 5f + 2.5f;
            Place(Wall, new Vector3(10f, 0f, zPos), new Vector3(0f, -90f, 0f), new Vector3(5f, 3.5f, 1f));
        }

        // Back wall (z=-10, facing inward +z), with window gaps at x=±3
        for (int x = -2; x < 2; x++)
        {
            float xPos = x * 5f + 2.5f;
            // Skip segments that would overlap the windows (approx x in [-5,0] and [0,5] spans).
            // Windows are at x=-3 and x=3, 1.5 m wide. Place wall segments on either side.
            if (Mathf.Abs(xPos + 3f) < 2.5f || Mathf.Abs(xPos - 3f) < 2.5f) continue;
            Place(Wall, new Vector3(xPos, 0f, -10f), Vector3.Zero, new Vector3(5f, 3.5f, 1f));
        }

        // Front wall: mostly open stairs, but short segments flanking the stairwell.
        Place(Wall, new Vector3(-6.5f, 0f, 10f), new Vector3(0f, 180f, 0f), new Vector3(7f, 3.5f, 1f));
        Place(Wall, new Vector3(6.5f, 0f, 10f), new Vector3(0f, 180f, 0f), new Vector3(7f, 3.5f, 1f));

        // Corner columns at the four corners.
        Place(WallCorner, new Vector3(-10f, 0f, -10f), new Vector3(0f, 0f, 0f), new Vector3(1f, 3.5f, 1f));
        Place(WallCorner, new Vector3(10f, 0f, -10f), new Vector3(0f, 90f, 0f), new Vector3(1f, 3.5f, 1f));
        Place(WallCorner, new Vector3(-10f, 0f, 10f), new Vector3(0f, -90f, 0f), new Vector3(1f, 3.5f, 1f));
        Place(WallCorner, new Vector3(10f, 0f, 10f), new Vector3(0f, 180f, 0f), new Vector3(1f, 3.5f, 1f));
    }

    private void BuildPillars()
    {
        // Central support pillar (existing collision is 2x2 at origin).
        Place(Pillar, new Vector3(0f, 0f, 0f), Vector3.Zero, new Vector3(2f, 3.5f, 2f));
    }

    private void BuildProps()
    {
        // NW corner: barrels
        Place(BarrelLarge, new Vector3(-8f, 0f, -8f), new Vector3(0f, 23f, 0f), Vector3.One * 1.2f);
        Place(BarrelSmallStack, new Vector3(-7f, 0f, -8.5f), new Vector3(0f, -15f, 0f), Vector3.One * 1.2f);

        // NE corner: crates and chest
        Place(CratesStacked, new Vector3(8f, 0f, -8f), new Vector3(0f, 12f, 0f), Vector3.One * 1.1f);
        Place(Chest, new Vector3(7f, 0f, -7f), new Vector3(0f, -30f, 0f), Vector3.One * 1.2f);

        // SE corner: lone barrel
        Place(BarrelLarge, new Vector3(8f, 0f, 8f), Vector3.Zero, Vector3.One * 1.2f);

        // SW corner: candle cluster
        Place(CandleTriple, new Vector3(-8f, 0f, 8f), Vector3.Zero, Vector3.One * 1.5f);

        // Back wall candles near windows
        Place(CandleLit, new Vector3(-3f, 0f, -9f), Vector3.Zero, Vector3.One * 1.5f);
        Place(CandleLit, new Vector3(3f, 0f, -9f), Vector3.Zero, Vector3.One * 1.5f);

        // Near registration board
        Place(CandleTriple, new Vector3(2.5f, 0f, 9.5f), Vector3.Zero, Vector3.One * 1.5f);
    }

    private void BuildLighting()
    {
        // Ambient override handled by WorldEnvironment; add local candle points.
        AddPointLight(new Vector3(-8f, 2f, 8f), new Color(1f, 0.6f, 0.3f), 4f, 1.2f);
        AddPointLight(new Vector3(2.5f, 2f, 9.5f), new Color(1f, 0.6f, 0.3f), 4f, 1.2f);
        AddPointLight(new Vector3(-3f, 1.5f, -8f), new Color(1f, 0.6f, 0.3f), 3f, 0.8f);
        AddPointLight(new Vector3(3f, 1.5f, -8f), new Color(1f, 0.6f, 0.3f), 3f, 0.8f);
    }

    private void AddPointLight(Vector3 pos, Color color, float range, float energy)
    {
        var light = new OmniLight3D
        {
            Position = pos,
            LightColor = color,
            OmniRange = range,
            LightEnergy = energy,
            ShadowEnabled = false
        };
        AddChild(light);
    }
}
