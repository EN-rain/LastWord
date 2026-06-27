using Godot;
using LastWord;

/// <summary>
/// Witness role passive (§5.Witness): extended ghost burst on death.
/// When the Witness dies they get a longer Whisper-mode ghost burst and
/// can briefly see the Listener's planned path.
/// </summary>
public partial class WitnessBurst : Node3D
{
	[Export] public float GhostBurstDuration = 6.0f;
	[Export] public float ListenerPathRevealDuration = 3.0f;
	[Export] public Color PathColor = new Color(1f, 0.2f, 0.2f, 0.6f);

	public void OnWitnessDied(PlayerController player)
	{
		GD.Print($"WitnessBurst: {player.Name} triggered extended ghost burst.");
		AudioAssets.PlayOneShot3D(AudioAssets.AbilityWitnessBurst, player, player.GlobalPosition, "SFX");
		// The actual duration extension is applied by the PlayerController death logic
		// reading this component. Path reveal is visual-only and can be expanded once
		// the spectator map / Listener path renderer exists.
	}

	public bool TryRevealListenerPath(Node3D listener)
	{
		if (listener == null)
			return false;

		GD.Print($"WitnessBurst: revealing Listener path for {ListenerPathRevealDuration}s.");
		AudioAssets.PlayOneShot3D(AudioAssets.AbilityWitnessBurst, this, GlobalPosition, "SFX");
		// TODO: spawn line renderer from listener to current nav target once map UI exists.
		return true;
	}
}
