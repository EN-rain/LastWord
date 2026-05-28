using Godot;

public enum ListenerTargetMode
{
	None,
	Token,
	SoundInvestigate,
	NonFrenzySprint,
	ScreamFrenzy,
	VocalSacrifice,
	Phase3PermanentFrenzy,
	SecondListenerImprint
}

public enum SoundKind
{
	Voice,
	Movement,
	Landing,
	Environment,
	Special
}

public readonly struct ListenerSoundEvent
{
	public ListenerSoundEvent(Vector3 origin, int tier, SoundKind kind, Node3D source = null, bool isSpecialLongRange = false)
	{
		Origin = origin;
		Tier = Mathf.Clamp(tier, 0, 3);
		Kind = kind;
		Source = source;
		IsSpecialLongRange = isSpecialLongRange;
	}

	public Vector3 Origin { get; }
	public int Tier { get; }
	public SoundKind Kind { get; }
	public Node3D Source { get; }
	public bool IsSpecialLongRange { get; }

	public bool IsVoice => Kind == SoundKind.Voice;
	public bool IsMovementOrNoise => Kind == SoundKind.Movement
		|| Kind == SoundKind.Landing
		|| Kind == SoundKind.Environment
		|| Kind == SoundKind.Special;
}
