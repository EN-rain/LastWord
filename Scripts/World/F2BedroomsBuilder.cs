using Godot;
using System.Collections.Generic;

namespace LastWord.World;

/// <summary>
/// Procedural art pass for F2_Bedrooms. Long corridor with bedroom alcoves.
/// Uses KayKit Dungeon Remastered (CC0).
/// </summary>
public partial class F2BedroomsBuilder : Node3D
{
    [Export] public bool BuildAtRuntime { get; set; } = true;

    private const string FloorWoodDark = "res://Assets/KayKitDungeon/gltf/floor_wood_large_dark.gltf.glb";
    private const string FloorFoundationFront = "res://Assets/KayKitDungeon/gltf/floor_foundation_front.gltf.glb";
    private const string FloorFoundationCorner = "res://Assets/KayKitDungeon/gltf/floor_foundation_corner.gltf.glb";
    private const string Wall = "res://Assets/KayKitDungeon/gltf/wall.gltf.glb";
    private const string WallCorner = "res://Assets/KayKitDungeon/gltf/wall_corner.gltf.glb";
    private const string Pillar = "res://Assets/KayKitDungeon/gltf/pillar.gltf.glb";
    private const string BarrelLarge = "res://Assets/KayKitDungeon/gltf/barrel_large.gltf.glb";
    private const string CratesStacked = "res://Assets/KayKitDungeon/gltf/crates_stacked.gltf.glb";
    private const string CandleLit = "res://Assets/KayKitDungeon/gltf/candle_lit.gltf.glb";
    private const string CandleTriple = "res://Assets/KayKitDungeon/gltf/candle_triple.gltf.glb";
    private const string Bed = "res://Assets/KayKitDungeon/gltf/table.gltf.glb";

    private readonly Dictionary<string, PackedScene> _cache = new();

    private PackedScene Load(string path)
    {
        if (_cache.TryGetValue(path, out var scene)) return scene;
        var loaded = GD.Load<PackedScene>(path);
        if (loaded == null)
        {
            GD.PushWarning($"F2BedroomsBuilder: could not load {path}");
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

    public override void _Ready()
    {
        if (!BuildAtRuntime) return;
        if (Engine.IsEditorHint()) return;

        GD.Print("F2BedroomsBuilder: starting art pass...");
        BuildFloor();
        BuildWalls();
        BuildProps();
        BuildLighting();
        GD.Print($"F2BedroomsBuilder: art pass complete. {GetChildCount()} visual nodes generated.");
    }

    private void BuildFloor()
    {
        // Corridor 8x24 m
        for (int x = -1; x < 1; x++)
            for (int z = -3; z < 3; z++)
                Place(FloorWoodDark, new Vector3(x * 4f + 2f, 0f, z * 4f + 2f), Vector3.Zero, new Vector3(4f, 1f, 4f));

        float y = -0.05f;
        Place(FloorFoundationFront, new Vector3(-4.25f, y, 0f), new Vector3(0f, 90f, 0f), new Vector3(24f, 1f, 1f));
        Place(FloorFoundationFront, new Vector3(4.25f, y, 0f), new Vector3(0f, -90f, 0f), new Vector3(24f, 1f, 1f));
        Place(FloorFoundationFront, new Vector3(0f, y, -12.25f), new Vector3(0f, 180f, 0f), new Vector3(8f, 1f, 1f));
        Place(FloorFoundationFront, new Vector3(0f, y, 12.25f), Vector3.Zero, new Vector3(8f, 1f, 1f));
        Place(FloorFoundationCorner, new Vector3(-4.25f, y, -12.25f), new Vector3(0f, 180f, 0f), Vector3.One);
        Place(FloorFoundationCorner, new Vector3(4.25f, y, -12.25f), new Vector3(0f, -90f, 0f), Vector3.One);
        Place(FloorFoundationCorner, new Vector3(-4.25f, y, 12.25f), new Vector3(0f, 90f, 0f), Vector3.One);
        Place(FloorFoundationCorner, new Vector3(4.25f, y, 12.25f), Vector3.Zero, Vector3.One);
    }

    private void BuildWalls()
    {
        // Side walls with bedroom alcove gaps
        for (int z = -3; z < 3; z++)
        {
            float zPos = z * 4f + 2f;
            if (z == -2 || z == 0 || z == 2) continue; // alcove gaps
            Place(Wall, new Vector3(-4f, 0f, zPos), new Vector3(0f, 90f, 0f), new Vector3(4f, 3.5f, 1f));
            Place(Wall, new Vector3(4f, 0f, zPos), new Vector3(0f, -90f, 0f), new Vector3(4f, 3.5f, 1f));
        }

        // Back/front walls
        for (int x = -1; x < 1; x++)
        {
            float xPos = x * 4f + 2f;
            Place(Wall, new Vector3(xPos, 0f, -12f), Vector3.Zero, new Vector3(4f, 3.5f, 1f));
            Place(Wall, new Vector3(xPos, 0f, 12f), new Vector3(0f, 180f, 0f), new Vector3(4f, 3.5f, 1f));
        }

        // Alcove back walls
        Place(Wall, new Vector3(-6f, 0f, -6f), Vector3.Zero, new Vector3(4f, 3.5f, 1f));
        Place(Wall, new Vector3(6f, 0f, -6f), Vector3.Zero, new Vector3(4f, 3.5f, 1f));
        Place(Wall, new Vector3(-6f, 0f, 2f), Vector3.Zero, new Vector3(4f, 3.5f, 1f));
        Place(Wall, new Vector3(6f, 0f, 2f), Vector3.Zero, new Vector3(4f, 3.5f, 1f));
        Place(Wall, new Vector3(-6f, 0f, 10f), Vector3.Zero, new Vector3(4f, 3.5f, 1f));
        Place(Wall, new Vector3(6f, 0f, 10f), Vector3.Zero, new Vector3(4f, 3.5f, 1f));

        // Corners
        Place(WallCorner, new Vector3(-4f, 0f, -12f), new Vector3(0f, 0f, 0f), new Vector3(1f, 3.5f, 1f));
        Place(WallCorner, new Vector3(4f, 0f, -12f), new Vector3(0f, 90f, 0f), new Vector3(1f, 3.5f, 1f));
        Place(WallCorner, new Vector3(-4f, 0f, 12f), new Vector3(0f, -90f, 0f), new Vector3(1f, 3.5f, 1f));
        Place(WallCorner, new Vector3(4f, 0f, 12f), new Vector3(0f, 180f, 0f), new Vector3(1f, 3.5f, 1f));
    }

    private void BuildProps()
    {
        Place(Bed, new Vector3(-6f, 0f, -6f), new Vector3(0f, 90f, 0f), new Vector3(1.5f, 1f, 2f));
        Place(Bed, new Vector3(6f, 0f, -6f), new Vector3(0f, -90f, 0f), new Vector3(1.5f, 1f, 2f));
        Place(Bed, new Vector3(-6f, 0f, 2f), new Vector3(0f, 90f, 0f), new Vector3(1.5f, 1f, 2f));
        Place(Bed, new Vector3(6f, 0f, 2f), new Vector3(0f, -90f, 0f), new Vector3(1.5f, 1f, 2f));
        Place(Bed, new Vector3(-6f, 0f, 10f), new Vector3(0f, 90f, 0f), new Vector3(1.5f, 1f, 2f));
        Place(Bed, new Vector3(6f, 0f, 10f), new Vector3(0f, -90f, 0f), new Vector3(1.5f, 1f, 2f));

        Place(BarrelLarge, new Vector3(-2f, 0f, -8f), Vector3.Zero, Vector3.One * 1.2f);
        Place(CratesStacked, new Vector3(2f, 0f, 8f), new Vector3(0f, 30f, 0f), Vector3.One * 1.1f);
    }

    private void BuildLighting()
    {
        AddPointLight(new Vector3(0f, 2.5f, -10f), new Color(1f, 0.6f, 0.3f), 5f, 1f);
        AddPointLight(new Vector3(0f, 2.5f, 0f), new Color(1f, 0.6f, 0.3f), 5f, 1f);
        AddPointLight(new Vector3(0f, 2.5f, 10f), new Color(1f, 0.6f, 0.3f), 5f, 1f);
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
