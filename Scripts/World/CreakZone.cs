using Godot;
using LastWord;

/// <summary>
/// Creaking floor tile (§6.2). Emits a Tier-0/1 sound event when a player
/// walks or runs across it. Frequency and tier scale with player velocity.
/// </summary>
public partial class CreakZone : Area3D
{
	[Export] public float CreakCooldown = 1.5f;
	[Export] public float WalkingTierThreshold = 1.5f;
	[Export] public float RunningTierThreshold = 4.0f;
	[Export] public bool CanBeSilencedByJammer = true;

	private double _lastCreakTime = -999.0;

	public override void _Ready()
	{
		BodyEntered += OnBodyEntered;
	}

	public override void _ExitTree()
	{
		BodyEntered -= OnBodyEntered;
	}

	private void OnBodyEntered(Node3D body)
	{
		if (body is not PlayerController player)
			return;
		if (player.IsDead)
			return;
		if (player.IsSilenced || player.IsAudioIsolated)
			return;
		if (CanBeSilencedByJammer && StaticBubble.IsPositionSuppressed(GlobalPosition))
			return;

		double now = Time.GetTicksMsec() / 1000.0;
		if (now - _lastCreakTime < CreakCooldown)
			return;

		Vector3 horizontalVelocity = new Vector3(player.Velocity.X, 0, player.Velocity.Z);
		float speed = horizontalVelocity.Length();
		if (speed < 0.1f)
			return;

		int tier = speed >= RunningTierThreshold ? 1 : 0;
		_lastCreakTime = now;

		AudioAssets.PlayOneShot3D(AudioAssets.Creak, this, GlobalPosition, "SFX", pitchScale: (float)GD.RandRange(0.9, 1.1));

		VoiceManager.Instance?.ReportNoiseEvent(GlobalPosition, tier, SoundKind.Environment, player);

		GD.Print($"CreakZone at {GlobalPosition}: creak from {player.Name} tier {tier}.");
	}
}
