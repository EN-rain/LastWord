using Godot;

public enum PlayerRole
{
	None,
	Loud,
	Static,
	Mute,
	Archivist,
	Witness
}

/// <summary>
/// Holds the selected role for a player and any per-run role resources.
/// Role passives are consumed by <see cref="VoiceManager"/>,
/// <see cref="ListenerAI"/>, and the individual ability nodes.
/// </summary>
public partial class RoleData : Node
{
	[Export] public PlayerRole Role = PlayerRole.None;
	[Export] public int StaticChargeCount = 2;

	// --- Passive modifiers (§5) ---
	[Export] public float MuteDetectionRadiusMultiplier = 0.5f;
	[Export] public int LoudMinimumTier = 2; // Whisper=1, Normal=2, Shout=3
	[Export] public float ArchivistRegistrationHoldTime = 5.0f;
	[Export] public float WitnessGhostBurstDuration = 6.0f;

	public bool IsLoud => Role == PlayerRole.Loud;
	public bool IsMute => Role == PlayerRole.Mute;
	public bool IsStatic => Role == PlayerRole.Static;
	public bool IsArchivist => Role == PlayerRole.Archivist;
	public bool IsWitness => Role == PlayerRole.Witness;
	public bool HasRole => Role != PlayerRole.None;

	public void SetRole(PlayerRole role)
	{
		Role = role;
		GD.Print($"RoleData: role set to {role}");
	}
}
