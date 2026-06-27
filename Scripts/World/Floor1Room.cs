using Godot;

[Tool]
public partial class Floor1Room : Node3D
{
	[Export] public string RoomName = "Room";
	[Export] public string RoomType = "generic";
	[Export] public string AcousticType = "NEUTRAL";
	[Export] public bool IsCreakZone = false;
	[Export] public bool HasPatrolWaypoint = false;
	[Export] public int ExitCount = 1;
	[Export] public bool HasCover = false;
	[Export] public bool ListenerCanInvestigate = true;
	[Export] public Vector3 RoomSize = new Vector3(8f, 4f, 8f);

	public override void _Ready()
	{
		// Editor helper: draw a wireframe gizmo of the room bounds
		// Runtime: no-op
	}
}
