using Godot;
using LastWord;

/// <summary>
/// The Loud role ability (F key). Emits a Tier 2.5 sound pulse and stuns all
/// Listener AI instances for 5 seconds. 90-second cooldown.
/// </summary>
public partial class LoudStun : Node3D
{
	[Export] public float Cooldown = 90.0f;
	[Export] public float StunDuration = 5.0f;

	private float _cooldownRemaining;
	private RoleData _roleData;

	public bool IsOnCooldown => _cooldownRemaining > 0f;

	public override void _Ready()
	{
		_roleData = Owner?.GetNodeOrNull<RoleData>("RoleData");
	}

	public override void _Process(double delta)
	{
		if (_cooldownRemaining > 0f)
			_cooldownRemaining -= (float)delta;
	}

	public void TryStun()
	{
		if (_roleData == null || _roleData.Role != PlayerRole.Loud)
			return;

		if (IsOnCooldown)
			return;

		if (!IsMultiplayerAuthority())
			return;

		if (Multiplayer.IsServer())
			ExecuteStun();
		else
			RpcId(NetworkManager.ServerPeerId, nameof(RequestStun));
	}

	[Rpc(MultiplayerApi.RpcMode.AnyPeer, CallLocal = false)]
	private void RequestStun()
	{
		if (!Multiplayer.IsServer()) return;
		long senderId = Multiplayer.GetRemoteSenderId();
		if (senderId != GetMultiplayerAuthority()) return;
		ExecuteStun();
	}

	private void ExecuteStun()
	{
		_cooldownRemaining = Cooldown;

		AudioAssets.PlayOneShot3D(AudioAssets.AbilityLoudStun, this, GlobalPosition, "SFX");
		Node3D source = Owner is PlayerController player ? player : this;

		// Tier 2.5 noise: transfers Token, no Tier 3 side effects.
		VoiceManager.Instance?.ReportNoiseEvent(
			source.GlobalPosition,
			2,
			SoundKind.Special,
			source: source,
			isSpecialLongRange: false);

		// Stun every Listener on the server.
		foreach (var node in GetTree().GetNodesInGroup("Listener"))
		{
			if (node is ListenerAI ai)
				ai.ApplyStun(StunDuration);
		}

		Rpc(nameof(SyncStun), Cooldown, StunDuration);
	}

	[Rpc(MultiplayerApi.RpcMode.Authority, CallLocal = true)]
	private void SyncStun(float cooldown, float stunDuration)
	{
		_cooldownRemaining = cooldown;
		AudioAssets.PlayOneShot3D(AudioAssets.AbilityLoudStun, this, GlobalPosition, "SFX");
		HUDManager.Instance?.UpdatePlayerState($"LOUD STUN {stunDuration:F0}s", Colors.Yellow);
	}
}
