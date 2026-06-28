using Godot;

/// <summary>
/// Runtime navigation-mesh baker for floor scenes (§6.1).
/// Godot 4's NavigationRegion3D can bake from StaticBody3D collision geometry;
/// this script tags all StaticBody3D nodes in the scene as nav-source geometry,
/// then triggers the bake once the floor is ready so the Listener can pathfind
/// without requiring a manually-baked resource in source control.
/// </summary>
public partial class FloorNavigationBaker : Node3D
{
	[Export] public float AgentRadius = 0.6f;
	[Export] public float AgentHeight = 2.0f;
	[Export] public float AgentMaxClimb = 0.4f;
	[Export] public float AgentMaxSlope = 45.0f;
	[Export] public float CellSize = 0.25f;
	[Export] public float CellHeight = 0.25f;
	[Export] public uint NavigationLayer = 1;
	[Export] public string SourceGroup = "nav_source";

	public override void _Ready()
	{
		CallDeferred(nameof(BakeNavigation));
	}

	private void BakeNavigation()
	{
		NavigationRegion3D region = GetNodeOrNull<NavigationRegion3D>("NavigationRegion3D");
		if (region == null)
		{
			// Fallback: look for a sibling NavigationRegion3D (legacy scene layout).
			region = GetParent()?.GetNodeOrNull<NavigationRegion3D>("NavigationRegion3D");
		}
		if (region == null)
		{
			GD.PushWarning("FloorNavigationBaker: no NavigationRegion3D child or sibling found.");
			return;
		}

		NavigationMesh navMesh = new NavigationMesh();
		navMesh.AgentRadius = AgentRadius;
		navMesh.AgentHeight = AgentHeight;
		navMesh.AgentMaxClimb = AgentMaxClimb;
		navMesh.AgentMaxSlope = AgentMaxSlope;
		navMesh.CellSize = CellSize;
		navMesh.CellHeight = CellHeight;
		navMesh.GeometryParsedGeometryType = NavigationMesh.ParsedGeometryType.StaticColliders;
		navMesh.GeometrySourceGeometryMode = NavigationMesh.SourceGeometryMode.GroupsWithChildren;
		navMesh.GeometrySourceGroupName = SourceGroup;
		navMesh.FilterBakingAabb = new Aabb(new Vector3(-15f, -1f, -15f), new Vector3(30f, 10f, 30f));

		// Tag every StaticBody3D in the scene as nav source geometry.
		Node root = region.GetTree().CurrentScene ?? region.GetParent();
		int tagged = 0;
		if (root != null)
		{
			TagStaticBodiesRecursive(root, ref tagged);
		}

		region.NavigationMesh = navMesh;

		// Use the navigation server to parse and bake explicitly from the scene geometry.
		var sourceGeometry = new NavigationMeshSourceGeometryData3D();
		NavigationServer3D.ParseSourceGeometryData(navMesh, sourceGeometry, region);
		NavigationServer3D.BakeFromSourceGeometryData(navMesh, sourceGeometry);
		region.NavigationMesh = navMesh;

		GD.Print($"FloorNavigationBaker: tagged {tagged} static bodies; baked navigation mesh with {navMesh.GetPolygonCount()} polygons.");
	}

	private void TagStaticBodiesRecursive(Node node, ref int tagged)
	{
		if (node is StaticBody3D body && !body.IsInGroup(SourceGroup))
		{
			body.AddToGroup(SourceGroup);
			tagged++;
		}

		foreach (var child in node.GetChildren())
		{
			TagStaticBodiesRecursive(child, ref tagged);
		}
	}
}
