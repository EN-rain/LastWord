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
        { "furn_armchair",   "res://Assets/KayKitFurniture/glb/armchair.glb" },
        { "furn_bed_double_A","res://Assets/KayKitFurniture/glb/bed_double_A.glb" },
        { "furn_bed_single_A","res://Assets/KayKitFurniture/glb/bed_single_A.glb" },
        { "furn_book_set",   "res://Assets/KayKitFurniture/glb/book_set.glb" },
        { "furn_book_single","res://Assets/KayKitFurniture/glb/book_single.glb" },
        { "furn_cabinet_medium","res://Assets/KayKitFurniture/glb/cabinet_medium.glb" },
        { "furn_chair_A",    "res://Assets/KayKitFurniture/glb/chair_A.glb" },
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
