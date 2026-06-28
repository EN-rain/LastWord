using Godot;

public enum GestureId
{
	PointForward,
	Wave,
	ThumbsUp,
	No,
	Listen,
	NoteFound,
	Self,
	Stop
}

/// <summary>
/// Plays short, silent visual gestures for the local player and replicates them
/// to peers. Gestures do not transfer the Token or emit Listener sounds.
/// </summary>
public partial class GestureSystem : Node3D
{
	[Signal] public delegate void GesturePlayedEventHandler(GestureId gesture);

	[Export] public float MaxVisibilityDistance = 6f;
	[Export] public float GestureDuration = 1.5f;

	private GestureId? _currentGesture;
	private float _gestureTimer;
	private AnimationPlayer _animPlayer;

	public GestureId? CurrentGesture => _currentGesture;
	public float RemainingTime => _gestureTimer;

	public override void _Ready()
	{
		_animPlayer = Owner?.GetNodeOrNull<AnimationPlayer>("AnimationPlayer");
	}

	/// <summary>
	/// Starts playing the requested gesture locally and requests server replication.
	/// Safe to call from client input handlers; non-authority calls are ignored.
	/// </summary>
	public void PlayGesture(GestureId gesture)
	{
		if (_currentGesture == gesture)
			return;

		bool networked = Multiplayer.MultiplayerPeer != null && Multiplayer.HasMultiplayerPeer();
		if (!networked)
		{
			ApplyGesture(gesture);
			return;
		}

		if (!IsMultiplayerAuthority())
			return;

		if (Multiplayer.IsServer())
		{
			Rpc(nameof(SyncGesture), (int)gesture);
		}
		else
		{
			RpcId(NetworkManager.ServerPeerId, nameof(RequestPlayGesture), (int)gesture);
		}
	}

	/// <summary>
	/// Client → server request. The server validates the sender before syncing.
	/// </summary>
	[Rpc(MultiplayerApi.RpcMode.AnyPeer, CallLocal = false)]
	private void RequestPlayGesture(int gestureId)
	{
		if (!Multiplayer.IsServer())
			return;
		if (!IsValidGestureId(gestureId))
			return;

		long senderId = Multiplayer.GetRemoteSenderId();
		if (senderId != GetMultiplayerAuthority())
			return;

		Rpc(nameof(SyncGesture), gestureId);
	}

	/// <summary>
	/// Server → all clients sync. CallLocal is true so the server/host also applies it.
	/// </summary>
	[Rpc(MultiplayerApi.RpcMode.Authority, CallLocal = true, TransferMode = MultiplayerPeer.TransferModeEnum.Unreliable)]
	private void SyncGesture(int gestureId)
	{
		if (!IsValidGestureId(gestureId))
			return;

		ApplyGesture((GestureId)gestureId);
	}

	private void ApplyGesture(GestureId gesture)
	{
		if (!IsGestureVisibleToLocalPlayer())
			return;

		_currentGesture = gesture;
		_gestureTimer = GestureDuration;
		EmitSignal(SignalName.GesturePlayed, (int)gesture);
		TryPlayAnimation(gesture);
	}

	private bool IsGestureVisibleToLocalPlayer()
	{
		if (Owner is not Node3D ownerNode)
			return true;

		bool networked = Multiplayer.MultiplayerPeer != null && Multiplayer.HasMultiplayerPeer();
		if (!networked || ownerNode.IsMultiplayerAuthority())
			return true;

		foreach (Node node in GetTree().GetNodesInGroup("Player"))
		{
			if (node is not Node3D player || !player.IsMultiplayerAuthority())
				continue;
			return player.GlobalPosition.DistanceTo(ownerNode.GlobalPosition) <= MaxVisibilityDistance;
		}

		return true;
	}

	private void TryPlayAnimation(GestureId gesture)
	{
		if (_animPlayer == null)
			return;

		string clip = GestureToAnimationName(gesture);
		if (!string.IsNullOrEmpty(clip) && _animPlayer.HasAnimation(clip))
			_animPlayer.Play(clip, 0.15f);
	}

	private static string GestureToAnimationName(GestureId gesture) => gesture switch
	{
		GestureId.PointForward => "gesture_point_forward",
		GestureId.Wave => "gesture_wave",
		GestureId.ThumbsUp => "gesture_thumbs_up",
		GestureId.No => "gesture_no",
		GestureId.Listen => "gesture_listen",
		GestureId.NoteFound => "gesture_note_found",
		GestureId.Self => "gesture_self",
		GestureId.Stop => "gesture_stop",
		_ => null
	};

	private static bool IsValidGestureId(int id)
	{
		return id >= 0 && id <= (int)GestureId.Stop;
	}

	public override void _Process(double delta)
	{
		if (_gestureTimer > 0f)
		{
			_gestureTimer -= (float)delta;
			if (_gestureTimer <= 0f)
				_currentGesture = null;
		}
	}
}
