using Godot;

/// <summary>
/// Echo role ability (§5.Echo): T-key replay decoy.
/// Places a temporary audio decoy that replays a recent voice clip to bait
/// the Listener. Post-launch; unbound in EA.
/// </summary>
public partial class EchoReplay : Node3D
{
	[Export] public bool Enabled = false;
	[Export] public float Cooldown = 60.0f;
	[Export] public float DecoyDuration = 8.0f;
	[Export] public float DecoyRadius = 6.0f;
	[Export] public NodePath VoiceRecorderPath;

	private float _cooldownTimer = 0f;
	private VoiceRecorder _recorder;

	public override void _Ready()
	{
		_recorder = GetNodeOrNull<VoiceRecorder>(VoiceRecorderPath);
	}

	public override void _Process(double delta)
	{
		if (_cooldownTimer > 0f)
			_cooldownTimer -= (float)delta;
	}

	public bool TryDeploy()
	{
		if (!Enabled)
			return false;

		if (_cooldownTimer > 0f)
			return false;

		if (_recorder == null)
			return false;

		AudioStreamWav clip = _recorder.GetRecentRecording(5f);
		if (clip == null)
			return false;

		AudioStreamPlayer3D decoy = new();
		decoy.Name = "EchoDecoy";
		decoy.Stream = clip;
		decoy.MaxDistance = DecoyRadius * 4f;
		decoy.GlobalPosition = GlobalPosition;
		GetTree().CurrentScene?.AddChild(decoy);
		decoy.Play();
		VoiceManager.Instance?.ReportNoiseEvent(decoy.GlobalPosition, 2, SoundKind.Special, Owner as Node3D, isSpecialLongRange: true);

		var timer = GetTree().CreateTimer(DecoyDuration);
		timer.Timeout += () =>
		{
			decoy.Stop();
			decoy.QueueFree();
		};

		_cooldownTimer = Cooldown;
		GD.Print("EchoReplay: decoy deployed.");
		return true;
	}
}
