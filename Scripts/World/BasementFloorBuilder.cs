using Godot;
using LastWord.World;
using System.Collections.Generic;

public partial class BasementFloorBuilder : Node3D
{
	private const float WallThickness = 0.35f;
	private const float RoomHeight = 4.0f;
	private const float FloorY = 0.0f;
	private const string DungeonPath = "res://Assets/KayKitDungeon/gltf/";

	private Node3D _world;
	private Node3D _roomsRoot;
	private Node3D _propsRoot;
	private Node3D _navigationRoot;
	private Node3D _zonesRoot;
	private Node3D _audioRoot;
	private Node3D _lightingRoot;
	private Node3D _spawnPoints;
	private NavigationRegion3D _navigationRegion;
	private StaticBody3D _navigationCollision;

	private readonly Dictionary<string, PackedScene> _assetCache = new();

	private readonly RoomSpec[] _rooms =
	{
		new("Entrance Spawn", "entry", "NEUTRAL", new Vector3(-36, 0, -22), new Vector3(10, RoomHeight, 8), 2, true, false),
		new("Storage Room", "storage", "MUFFLED", new Vector3(-28, 0, -12), new Vector3(12, RoomHeight, 10), 3, true, true),
		new("Split Junction A", "junction", "NEUTRAL", new Vector3(-16, 0, -8), new Vector3(10, RoomHeight, 10), 4, true, false),
		new("Wide Patrol A", "patrol", "ECHO", new Vector3(-8, 0, 8), new Vector3(16, RoomHeight, 14), 3, true, true),
		new("Dead End Reward A", "reward", "MUFFLED", new Vector3(-24, 0, 8), new Vector3(8, RoomHeight, 8), 1, true, false),
		new("Creak Corridor", "corridor", "CREAK_ZONE", new Vector3(4, 0, 0), new Vector3(8, RoomHeight, 28), 4, false, true),
		new("Ambush Corridor A", "ambush", "ECHO", new Vector3(14, 0, 14), new Vector3(6, RoomHeight, 18), 2, false, true),
		new("Guard Room", "antechamber", "ECHO", new Vector3(24, 0, 24), new Vector3(16, RoomHeight, 12), 3, true, true),
		new("Staircase Hub", "staircase", "ECHO", new Vector3(42, 0, 34), new Vector3(12, RoomHeight, 12), 2, true, false),
		new("Registration Board Alcove", "objective", "NEUTRAL", new Vector3(30, 0, 36), new Vector3(8, RoomHeight, 8), 1, false, false),
		new("Alternate Silent Route", "alternate", "MUFFLED", new Vector3(-6, 0, -26), new Vector3(30, RoomHeight, 7), 3, true, false),
		new("Flooded Section", "flooded", "ECHO", new Vector3(18, 0, -28), new Vector3(16, RoomHeight, 12), 3, false, true),
		new("Hidden Alcove", "secret", "MUFFLED", new Vector3(34, 0, -34), new Vector3(7, RoomHeight, 7), 1, true, false),
		new("Wide Patrol B", "patrol", "ECHO", new Vector3(30, 0, -12), new Vector3(18, RoomHeight, 14), 3, true, true),
		new("Lockbox Room", "reward", "MUFFLED", new Vector3(44, 0, -16), new Vector3(8, RoomHeight, 8), 1, true, false),
		new("Split Junction B", "junction", "NEUTRAL", new Vector3(10, 0, -14), new Vector3(10, RoomHeight, 10), 4, true, false),
		new("Collapsed Room", "collapsed", "ECHO", new Vector3(-2, 0, -42), new Vector3(16, RoomHeight, 10), 2, true, true),
		new("Boiler Utility", "utility", "MUFFLED", new Vector3(-22, 0, -36), new Vector3(12, RoomHeight, 10), 2, true, false),
		new("Split Junction C", "junction", "NEUTRAL", new Vector3(-34, 0, -34), new Vector3(10, RoomHeight, 10), 3, true, false),
		new("Furnace Room", "furnace", "MUFFLED", new Vector3(-46, 0, -42), new Vector3(14, RoomHeight, 12), 1, true, false),
		new("Dead End Reward B", "reward", "MUFFLED", new Vector3(2, 0, 30), new Vector3(8, RoomHeight, 8), 1, true, false),
		new("Ambush Corridor B", "ambush", "ECHO", new Vector3(18, 0, 38), new Vector3(18, RoomHeight, 6), 2, false, true),
	};

	public override void _Ready()
	{
		if (HasNode("World"))
			return;

		BuildFloor();
	}

	private void BuildFloor()
	{
		BuildRoots();
		AddEnvironment();
		AddRooms();
		AddDoorways();
		AddGameplayZones();
		AddGameplayMarkers();
		AddProps();
		AddLighting();
		AddAudioNodes();
	}

	private void BuildRoots()
	{
		_world = AddNode<Node3D>(this, "World");
		_roomsRoot = AddNode<Node3D>(_world, "Rooms");
		_propsRoot = AddNode<Node3D>(_world, "Props");
		_navigationRoot = AddNode<Node3D>(_world, "Navigation");
		_zonesRoot = AddNode<Node3D>(_world, "Zones");
		_audioRoot = AddNode<Node3D>(_world, "Audio");
		_lightingRoot = AddNode<Node3D>(_world, "Lighting");
		_spawnPoints = AddNode<Node3D>(this, "SpawnPoints");

		_navigationRegion = AddNode<NavigationRegion3D>(_navigationRoot, "NavigationRegion3D");
		_navigationRegion.NavigationMesh = new NavigationMesh
		{
			AgentRadius = 0.5f,
			AgentHeight = 2.0f,
			AgentMaxClimb = 0.5f,
			AgentMaxSlope = 45.0f,
			CellSize = 0.25f,
			CellHeight = 0.25f,
			GeometryParsedGeometryType = NavigationMesh.ParsedGeometryType.StaticColliders,
			GeometrySourceGeometryMode = NavigationMesh.SourceGeometryMode.GroupsWithChildren,
			GeometrySourceGroupName = "nav_source"
		};

		_navigationCollision = AddNode<StaticBody3D>(_navigationRegion, "LevelCollision");
		_navigationCollision.AddToGroup("nav_source");
	}

	private void AddEnvironment()
	{
		var env = new Environment
		{
			BackgroundMode = Environment.BGMode.Color,
			BackgroundColor = new Color(0.006f, 0.007f, 0.01f),
			AmbientLightSource = Environment.AmbientSource.Color,
			AmbientLightColor = new Color(0.025f, 0.025f, 0.035f),
			AmbientLightEnergy = 0.18f,
			FogEnabled = true,
			FogDensity = 0.055f,
			FogAerialPerspective = 0.65f,
			GlowEnabled = true,
			GlowIntensity = 0.25f
		};

		var worldEnvironment = AddNode<WorldEnvironment>(_world, "WorldEnvironment");
		worldEnvironment.Environment = env;
	}

	private void AddRooms()
	{
		foreach (RoomSpec spec in _rooms)
		{
			var room = AddNode<Floor1Room>(_roomsRoot, ToNodeName(spec.Name, spec.Type == "corridor" ? "Hall" : "Room"));
			room.Position = spec.Position;
			room.RoomName = spec.Name;
			room.RoomType = spec.Type;
			room.AcousticType = spec.Acoustic;
			room.IsCreakZone = spec.Acoustic == "CREAK_ZONE";
			room.HasPatrolWaypoint = spec.Patrol;
			room.ExitCount = spec.Exits;
			room.HasCover = spec.HasCover;
			room.ListenerCanInvestigate = true;
			room.RoomSize = spec.Size;

			AddRoomBody(room, spec);
			AddNavFloor(spec);
		}
	}

	private void AddRoomBody(Node3D room, RoomSpec spec)
	{
		Color floorColor = spec.Acoustic switch
		{
			"MUFFLED" => new Color(0.12f, 0.095f, 0.07f),
			"CREAK_ZONE" => new Color(0.11f, 0.075f, 0.04f),
			"ECHO" => new Color(0.09f, 0.095f, 0.105f),
			_ => new Color(0.095f, 0.09f, 0.085f)
		};
		Color wallColor = spec.Type == "furnace"
			? new Color(0.13f, 0.085f, 0.055f)
			: new Color(0.075f, 0.08f, 0.09f);

		AddBox(room, "Floor", new Vector3(0, -0.08f, 0), new Vector3(spec.Size.X, 0.16f, spec.Size.Z), floorColor, collision: true);
		AddBox(room, "Ceiling", new Vector3(0, RoomHeight, 0), new Vector3(spec.Size.X, 0.2f, spec.Size.Z), new Color(0.035f, 0.037f, 0.042f), collision: true);

		AddBox(room, "Wall_North", new Vector3(0, RoomHeight / 2f, spec.Size.Z / 2f), new Vector3(spec.Size.X, RoomHeight, WallThickness), wallColor, collision: true);
		AddBox(room, "Wall_South", new Vector3(0, RoomHeight / 2f, -spec.Size.Z / 2f), new Vector3(spec.Size.X, RoomHeight, WallThickness), wallColor, collision: true);
		AddBox(room, "Wall_East", new Vector3(spec.Size.X / 2f, RoomHeight / 2f, 0), new Vector3(WallThickness, RoomHeight, spec.Size.Z), wallColor, collision: true);
		AddBox(room, "Wall_West", new Vector3(-spec.Size.X / 2f, RoomHeight / 2f, 0), new Vector3(WallThickness, RoomHeight, spec.Size.Z), wallColor, collision: true);
	}

	private void AddNavFloor(RoomSpec spec)
	{
		var shape = AddNode<CollisionShape3D>(_navigationCollision, $"NavFloor_{ToSafeName(spec.Name)}");
		shape.Position = spec.Position + new Vector3(0, -0.1f, 0);
		shape.Shape = new BoxShape3D { Size = new Vector3(spec.Size.X, 0.12f, spec.Size.Z) };
	}

	private void AddDoorways()
	{
		AddDoorway("Door_Entrance_Storage", new Vector3(-32, 2, -17), new Vector3(4, 4, 0.7f));
		AddDoorway("Door_Storage_JunctionA", new Vector3(-22, 2, -10), new Vector3(0.7f, 4, 4));
		AddDoorway("Door_Storage_RewardA", new Vector3(-26, 2, -2), new Vector3(0.7f, 4, 4));
		AddDoorway("Door_JunctionA_PatrolA", new Vector3(-12, 2, 0), new Vector3(4, 4, 0.7f));
		AddDoorway("Door_JunctionA_Creak", new Vector3(-4, 2, -4), new Vector3(0.7f, 4, 4));
		AddDoorway("Door_Creak_Guard", new Vector3(10, 2, 12), new Vector3(0.7f, 4, 4));
		AddDoorway("Door_Guard_Stairs", new Vector3(34, 2, 29), new Vector3(0.7f, 4, 4));
		AddDoorway("Door_Stairs_Board", new Vector3(36, 2, 35), new Vector3(4, 4, 0.7f));
		AddDoorway("Door_Alternate_FromStorage", new Vector3(-18, 2, -24), new Vector3(4, 4, 0.7f));
		AddDoorway("Door_Alternate_Flooded", new Vector3(8, 2, -26), new Vector3(0.7f, 4, 4));
		AddDoorway("Door_Flooded_PatrolB", new Vector3(24, 2, -22), new Vector3(4, 4, 0.7f));
		AddDoorway("Door_PatrolB_Guard", new Vector3(28, 2, -4), new Vector3(4, 4, 0.7f));
		AddDoorway("Door_PatrolB_Lockbox", new Vector3(39, 2, -15), new Vector3(0.7f, 4, 3));
		AddDoorway("Door_Flooded_Hidden", new Vector3(28, 2, -31), new Vector3(0.7f, 4, 3));
		AddDoorway("Door_JunctionB_Collapsed", new Vector3(6, 2, -28), new Vector3(4, 4, 0.7f));
		AddDoorway("Door_Collapsed_Boiler", new Vector3(-12, 2, -40), new Vector3(0.7f, 4, 4));
		AddDoorway("Door_Boiler_SplitC", new Vector3(-28, 2, -35), new Vector3(0.7f, 4, 4));
		AddDoorway("Door_SplitC_Furnace", new Vector3(-40, 2, -39), new Vector3(0.7f, 4, 4));
		AddDoorway("Door_Guard_AmbushB", new Vector3(20, 2, 32), new Vector3(4, 4, 0.7f));
		AddDoorway("Door_AmbushB_RewardB", new Vector3(10, 2, 34), new Vector3(0.7f, 4, 3));
	}

	private void AddGameplayZones()
	{
		var creak = AddNode<CreakZone>(_zonesRoot, "CreakZone");
		creak.Position = new Vector3(4, 0.05f, 0);
		creak.AddToGroup("CreakZone");
		AddAreaShape(creak, new Vector3(8, 3, 28));

		var wet = AddNode<Area3D>(_zonesRoot, "WetZone");
		wet.Position = new Vector3(18, 0.05f, -28);
		wet.AddToGroup("WetZone");
		AddAreaShape(wet, new Vector3(16, 3, 12));
	}

	private void AddGameplayMarkers()
	{
		AddMarker(_zonesRoot, "SpawnZone_Players", new Vector3(-36, 0.2f, -22), "SpawnPoint");
		AddMarker(_zonesRoot, "SpawnZone_Listener", new Vector3(-24, 0.2f, -10), "SpawnPoint");
		AddMarker(_spawnPoints, "NoteSpawn_Storage", new Vector3(-29, 0.2f, -10), "SpawnPoint");
		AddMarker(_spawnPoints, "NoteSpawn_RewardA", new Vector3(-24, 0.2f, 8), "SpawnPoint");
		AddMarker(_spawnPoints, "NoteSpawn_Lockbox", new Vector3(44, 0.2f, -16), "SpawnPoint");
		AddMarker(_spawnPoints, "NoteSpawn_Hidden", new Vector3(34, 0.2f, -34), "SpawnPoint");
		AddMarker(_spawnPoints, "RadioSpawn", new Vector3(32, 0.2f, 34), "SpawnPoint");

		var patrolRoot = AddNode<Node3D>(_navigationRoot, "PatrolWaypoints");
		Vector3[] patrol =
		{
			new(-36, 0.2f, -22), new(-28, 0.2f, -12), new(-8, 0.2f, 8), new(4, 0.2f, 0),
			new(24, 0.2f, 24), new(42, 0.2f, 34), new(18, 0.2f, 38), new(30, 0.2f, -12),
			new(18, 0.2f, -28), new(-2, 0.2f, -42), new(-22, 0.2f, -36), new(-46, 0.2f, -42)
		};

		for (int i = 0; i < patrol.Length; i++)
			AddMarker(patrolRoot, $"Waypoint_{i + 1:00}", patrol[i], "Waypoint");
	}

	private void AddProps()
	{
		AddRegistrationBoard();
		AddIntercom();
		AddFurnace();
		AddRoomPropClusters();
		AddWaterPlane();
	}

	private void AddRegistrationBoard()
	{
		var board = AddNode<RegistrationBoard>(_propsRoot, "RegistrationBoard");
		board.Position = new Vector3(30, 0.2f, 36);
		board.AddToGroup("RegistrationBoard");
		AddAreaShape(board, new Vector3(5.5f, 3, 5.5f));
		AddBox(board, "Board_Backplate", new Vector3(0, 1.5f, -1.8f), new Vector3(4.5f, 2.4f, 0.25f), new Color(0.08f, 0.045f, 0.025f), collision: false);
		AddBox(board, "Board_Frame", new Vector3(0, 1.5f, -1.95f), new Vector3(5.0f, 2.8f, 0.12f), new Color(0.18f, 0.12f, 0.07f), collision: false);
	}

	private void AddIntercom()
	{
		var intercom = AddNode<Intercom>(_propsRoot, "Intercom_FurnaceWall");
		intercom.Position = new Vector3(-41, 1.2f, -36.5f);
		AddAreaShape(intercom, new Vector3(3, 2.5f, 3));
		AddBox(intercom, "PLACEHOLDER_IntercomSpeakerBox", new Vector3(0, 0.2f, 0), new Vector3(1.0f, 0.75f, 0.18f), new Color(0.08f, 0.08f, 0.075f), collision: false, missingAsset: true);
	}

	private void AddFurnace()
	{
		var furnace = AddNode<Node3D>(_propsRoot, "Furnace_Mesh");
		furnace.Position = new Vector3(-49, 0.1f, -45);
		AddBox(furnace, "PLACEHOLDER_IndustrialFurnace_Body", Vector3.Zero, new Vector3(4.5f, 2.8f, 3.0f), new Color(0.12f, 0.055f, 0.035f), collision: true, missingAsset: true);
		AddBox(furnace, "Furnace_Mouth_Glow", new Vector3(0, 0.15f, -1.55f), new Vector3(2.6f, 1.1f, 0.08f), new Color(1.0f, 0.24f, 0.03f), collision: false);
		AddBox(furnace, "PLACEHOLDER_BrokenPipe_A", new Vector3(2.7f, 1.1f, 0), new Vector3(0.45f, 0.45f, 6.0f), new Color(0.1f, 0.085f, 0.07f), collision: true, missingAsset: true);
	}

	private void AddRoomPropClusters()
	{
		PlaceAsset("barrel_large.gltf.glb", "Barrel_Storage_A", new Vector3(-30, 0, -9), 1.3f);
		PlaceAsset("barrel_small_stack.gltf.glb", "Barrel_Storage_B", new Vector3(-26, 0, -14), 1.2f);
		PlaceAsset("crates_stacked.gltf.glb", "Crates_Storage_Cover", new Vector3(-31, 0, -15), 1.25f);
		PlaceAsset("chest.glb", "RewardChest_A", new Vector3(-24, 0, 8), 1.4f);
		PlaceAsset("chest.glb", "Lockbox_Chest", new Vector3(44, 0, -16), 1.25f);
		PlaceAsset("shelves.gltf.glb", "Shelves_Guard_Cover", new Vector3(22, 0, 21), 1.4f);
		PlaceAsset("shelf_large.gltf.glb", "Shelves_PatrolB", new Vector3(30, 0, -7), 1.3f);
		PlaceAsset("rubble_large.gltf.glb", "Rubble_Collapsed_A", new Vector3(-4, 0, -43), 1.6f);
		PlaceAsset("rubble_half.gltf.glb", "Rubble_Collapsed_B", new Vector3(1, 0, -39), 1.5f);
		PlaceAsset("pillar_decorated.gltf.glb", "LOS_Pillar_PatrolA_A", new Vector3(-11, 0, 5), 1.8f);
		PlaceAsset("pillar_decorated.gltf.glb", "LOS_Pillar_PatrolA_B", new Vector3(-3, 0, 11), 1.8f);
		PlaceAsset("crates_stacked.gltf.glb", "Crates_Guard_A", new Vector3(28, 0, 27), 1.2f);
		PlaceAsset("barrier.gltf.glb", "IronBarrier_Hidden", new Vector3(34, 0, -31), 1.2f);
		PlaceAsset("stairs_wide.gltf.glb", "Staircase_Exit", new Vector3(43, 0, 37), 1.4f);
		PlaceAsset("candle_lit.gltf.glb", "Candle_Storage", new Vector3(-29, 0, -6), 1.5f);
		PlaceAsset("candle_triple.gltf.glb", "Candle_Board", new Vector3(28, 0, 34), 1.4f);
		PlaceAsset("candle_lit.gltf.glb", "Candle_Guard", new Vector3(22, 0, 29), 1.3f);

		PlaceAsset("trunk_large_A.gltf.glb", "Cabinet_HiddenReward", new Vector3(34, 0, -36), 1.2f);
		PlaceAsset("keyring.gltf.glb", "LoreKeyring_RewardB", new Vector3(2, 0, 30), 1.1f);
	}

	private void AddWaterPlane()
	{
		AddBox(_propsRoot, "Water_Flooded_Section", new Vector3(18, 0.03f, -28), new Vector3(15.5f, 0.04f, 11.5f), new Color(0.015f, 0.035f, 0.045f, 0.75f), collision: false);
	}

	private void AddLighting()
	{
		AddLight("Furnace_Glow", new Vector3(-48, 2.0f, -43), new Color(1f, 0.25f, 0.04f), 4.0f, 1.8f);
		AddLight("Torch_Storage", new Vector3(-28, 2.5f, -8), new Color(1f, 0.55f, 0.18f), 4.0f, 0.9f);
		AddLight("Torch_PatrolA", new Vector3(-8, 2.6f, 10), new Color(1f, 0.55f, 0.18f), 4.5f, 0.8f);
		AddLight("Torch_Guard", new Vector3(24, 2.8f, 25), new Color(1f, 0.52f, 0.16f), 4.5f, 0.9f);
		AddLight("Board_CandleGlow", new Vector3(30, 2.0f, 34), new Color(0.9f, 0.45f, 0.14f), 3.0f, 0.65f);
	}

	private void AddAudioNodes()
	{
		AddNode<AudioStreamPlayer3D>(_audioRoot, "Ambient_Drone").Position = new Vector3(-8, 1.6f, -8);
		AddNode<AudioStreamPlayer3D>(_audioRoot, "Ambient_Furnace").Position = new Vector3(-48, 1.4f, -43);
		AddNode<AudioStreamPlayer3D>(_audioRoot, "SFX_WaterDrip").Position = new Vector3(18, 1.8f, -28);
	}

	private T AddNode<T>(Node parent, string name) where T : Node, new()
	{
		var node = new T { Name = name };
		parent.AddChild(node);
		return node;
	}

	private void AddMarker(Node parent, string name, Vector3 position, string group)
	{
		var marker = AddNode<Marker3D>(parent, name);
		marker.Position = position;
		if (!string.IsNullOrEmpty(group))
			marker.AddToGroup(group);
	}

	private void AddAreaShape(Area3D area, Vector3 size)
	{
		var shape = AddNode<CollisionShape3D>(area, "CollisionShape3D");
		shape.Shape = new BoxShape3D { Size = size };
	}

	private void AddDoorway(string name, Vector3 position, Vector3 size)
	{
		var doorway = AddNode<Area3D>(_zonesRoot, name);
		doorway.Position = position;
		doorway.AddToGroup("Doorway");
		AddAreaShape(doorway, size);
		AddBox(_propsRoot, $"Frame_{name}", position + new Vector3(0, -1.8f, 0), new Vector3(size.X + 0.4f, 0.2f, size.Z + 0.4f), new Color(0.08f, 0.07f, 0.06f), collision: false);
		AddNavConnector(name, position, size);
	}

	private void AddNavConnector(string name, Vector3 position, Vector3 doorwaySize)
	{
		var shape = AddNode<CollisionShape3D>(_navigationCollision, $"NavConnector_{name}");
		shape.Position = new Vector3(position.X, -0.1f, position.Z);
		shape.Shape = new BoxShape3D
		{
			Size = new Vector3(Mathf.Max(doorwaySize.X, 3.0f), 0.12f, Mathf.Max(doorwaySize.Z, 3.0f))
		};
	}

	private void AddBox(Node parent, string name, Vector3 localPosition, Vector3 size, Color color, bool collision, bool missingAsset = false)
	{
		var box = AddNode<CsgBox3D>(parent, name);
		box.Position = localPosition;
		box.Size = size;
		box.Material = MakeMaterial(color);
		if (missingAsset)
			box.SetMeta("missing_asset", true);

		if (!collision)
			return;

		var body = AddNode<StaticBody3D>(box, $"{name}_Collision");
		var shape = AddNode<CollisionShape3D>(body, "CollisionShape3D");
		shape.Shape = new BoxShape3D { Size = size };
	}

	private void PlaceAsset(string file, string name, Vector3 position, float scale)
	{
		PlacePacked(DungeonPath + file, name, position, scale);
	}

	private void PlacePacked(string path, string name, Vector3 position, float scale)
	{
		if (!_assetCache.TryGetValue(path, out var scene))
		{
			scene = ResourceLoader.Load<PackedScene>(path);
			_assetCache[path] = scene;
		}

		if (scene == null)
		{
			AddBox(_propsRoot, $"PLACEHOLDER_{name}", position + new Vector3(0, 0.5f, 0), new Vector3(1.5f, 1f, 1.5f), new Color(0.18f, 0.08f, 0.08f), collision: true, missingAsset: true);
			return;
		}

		var node = scene.Instantiate<Node3D>();
		node.Name = name;
		node.Position = position;
		node.Scale = Vector3.One * scale;
		_propsRoot.AddChild(node);
	}

	private void AddLight(string name, Vector3 position, Color color, float range, float energy)
	{
		var light = AddNode<OmniLight3D>(_lightingRoot, name);
		light.Position = position;
		light.LightColor = color;
		light.OmniRange = range;
		light.LightEnergy = energy;
		light.ShadowEnabled = true;
	}

	private static StandardMaterial3D MakeMaterial(Color color)
	{
		return new StandardMaterial3D
		{
			AlbedoColor = color,
			Roughness = 0.85f,
			Metallic = 0.0f
		};
	}

	private static string ToNodeName(string name, string prefix)
	{
		return $"{prefix}_{ToSafeName(name)}";
	}

	private static string ToSafeName(string name)
	{
		return name.Replace(" ", "_").Replace("/", "_");
	}

	private readonly record struct RoomSpec(
		string Name,
		string Type,
		string Acoustic,
		Vector3 Position,
		Vector3 Size,
		int Exits,
		bool HasCover,
		bool Patrol);
}
