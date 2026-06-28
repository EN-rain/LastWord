using Godot;
using LastWord;
using System.Collections.Generic;

/// <summary>
/// The Static role ability (R key). Deploys a 4m radius white-noise bubble
/// that suppresses sounds inside it for 40 seconds. 2 charges per run.
/// </summary>
public partial class StaticBubble : Node3D
{
	[Export] public float Radius = 4.0f;
	[Export] public float Duration = 40.0f;
	[Export] public int MaxCharges = 2;
	[Export] public float VisibleRange = 15.0f;
	[Export] public float VisibleRangePostLightsOut = 20.0f;

	public static readonly List<StaticBubble> ActiveBubbles = new();

	private float _remaining;
	private Area3D _area;
	private MeshInstance3D _visual;
	private RoleData _roleData;

	public bool IsDeployed => _remaining > 0f;

	public override void _Ready()
	{
		_area = GetNodeOrNull<Area3D>("Area3D");
		_visual = GetNodeOrNull<MeshInstance3D>("Visual");
		_roleData = Owner?.GetNodeOrNull<RoleData>("RoleData");

		if (_area != null)
		{
			_area.BodyEntered += OnBodyEntered;
			_area.BodyExited += OnBodyExited;
		}

		SetBubbleVisible(false);
	}

	public override void _ExitTree()
	{
		ActiveBubbles.Remove(this);
		if (_area != null)
		{
			_area.BodyEntered -= OnBodyEntered;
			_area.BodyExited -= OnBodyExited;
		}
	}

	public bool CanDeploy()
	{
		if (_roleData == null || _roleData.Role != PlayerRole.Static)
			return false;
		if (_roleData.StaticChargeCount <= 0)
			return false;
		return !IsDeployed;
	}

	public void TryDeploy()
	{
		if (!CanDeploy())
			return;

		if (!IsMultiplayerAuthority())
			return;

		if (Multiplayer.IsServer())
			Deploy();
		else
			RpcId(NetworkManager.ServerPeerId, nameof(RequestDeploy));
	}

	[Rpc(MultiplayerApi.RpcMode.AnyPeer, CallLocal = false)]
	private void RequestDeploy()
	{
		if (!Multiplayer.IsServer()) return;
		long senderId = Multiplayer.GetRemoteSenderId();
		if (senderId != GetMultiplayerAuthority()) return;
		Deploy();
	}

	private void Deploy()
	{
		if (_roleData != null)
			_roleData.StaticChargeCount--;

		_remaining = Duration;
		RegisterActiveBubble();
		AudioAssets.PlayOneShot3D(AudioAssets.AbilityStaticBubble, this, GlobalPosition, "SFX");
		Rpc(nameof(SyncDeploy), Duration, _roleData?.StaticChargeCount ?? 0);
	}

	[Rpc(MultiplayerApi.RpcMode.Authority, CallLocal = true)]
	private void SyncDeploy(float duration, int chargesRemaining)
	{
		_remaining = duration;
		RegisterActiveBubble();
		SetBubbleVisible(true);
		AudioAssets.PlayOneShot3D(AudioAssets.AbilityStaticBubble, this, GlobalPosition, "SFX");
		HUDManager.Instance?.UpdatePlayerState($"STATIC BUBBLE {chargesRemaining} left", Colors.White);
	}

	private void RegisterActiveBubble()
	{
		if (!ActiveBubbles.Contains(this))
			ActiveBubbles.Add(this);
	}

	public void Collapse()
	{
		_remaining = 0f;
		ActiveBubbles.Remove(this);
		SetBubbleVisible(false);
	}

	public override void _Process(double delta)
	{
		if (_remaining <= 0f)
			return;

		_remaining -= (float)delta;
		if (_remaining <= 0f)
			Collapse();
		else
			UpdateVisibilityForLocalPlayer();
	}

	private void SetBubbleVisible(bool visible)
	{
		if (_visual != null)
			_visual.Visible = visible;
		if (_area != null)
			_area.Monitoring = visible;
	}

	private void OnBodyEntered(Node body)
	{
		// Optional per-body tracking if needed in future.
	}

	private void OnBodyExited(Node body)
	{
		// Optional per-body tracking if needed in future.
	}

	private void UpdateVisibilityForLocalPlayer()
	{
		if (_visual == null)
			return;

		Node3D localPlayer = FindLocalPlayer();
		if (localPlayer == null)
		{
			_visual.Visible = IsDeployed;
			return;
		}

		float range = ClapAbility.LightsOutActive ? VisibleRangePostLightsOut : VisibleRange;
		_visual.Visible = IsDeployed && localPlayer.GlobalPosition.DistanceTo(GlobalPosition) <= range;
	}

	private Node3D FindLocalPlayer()
	{
		bool networked = Multiplayer.MultiplayerPeer != null && Multiplayer.HasMultiplayerPeer();
		foreach (Node node in GetTree().GetNodesInGroup("Player"))
		{
			if (node is not Node3D player)
				continue;
			if (!networked || player.IsMultiplayerAuthority())
				return player;
		}
		return null;
	}

	/// <summary>
	/// Returns true if the given world position is inside any active bubble.
	/// </summary>
	public static bool IsPositionSuppressed(Vector3 worldPosition)
	{
		foreach (var bubble in ActiveBubbles)
		{
			if (bubble == null || !bubble.IsDeployed)
				continue;

			float distance = bubble.GlobalPosition.DistanceTo(worldPosition);
			if (distance <= bubble.Radius)
				return true;
		}
		return false;
	}
}
