using Godot;

public partial class HUDManager : Control
{
	[Export] public NodePath TierLabelPath;
	[Export] public NodePath VolumeMeterPath;
	[Export] public NodePath TokenIndicatorPath;

	private Label _tierLabel;
	private ProgressBar _volumeMeter;
	private TextureRect _tokenIndicator;

	private static readonly Color ColorSilent   = new Color(0.45f, 0.45f, 0.45f);
	private static readonly Color ColorWhisper  = new Color(0.2f,  0.85f, 0.2f);
	private static readonly Color ColorNormal   = new Color(0.9f,  0.8f,  0.1f);
	private static readonly Color ColorShouting = new Color(0.9f,  0.2f,  0.2f);

	public override void _Ready()
	{
		GD.Print("HUDManager: _Ready starting...");
		_tierLabel       = GetNodeOrNull<Label>(TierLabelPath);
		_volumeMeter     = GetNodeOrNull<ProgressBar>(VolumeMeterPath);
		_tokenIndicator  = GetNodeOrNull<TextureRect>(TokenIndicatorPath);

		// Connect to VoiceManager
		if (VoiceManager.Instance != null)
		{
			VoiceManager.Instance.TierChanged      += OnTierChanged;
			VoiceManager.Instance.VolumeUpdated    += OnVolumeUpdated;
			VoiceManager.Instance.TokenTransferred += OnTokenTransferred;
		}
		else
		{
			GD.PrintErr("HUDManager: VoiceManager.Instance is null! Signals not connected.");
		}

		if (_tokenIndicator != null) _tokenIndicator.Visible = false;
		UpdateTierUI(0);
	}

	// ------------------------------------------------------------------ //
	//  Signal callbacks
	// ------------------------------------------------------------------ //
	private void OnTierChanged(int newTier)   => UpdateTierUI(newTier);

	private void OnVolumeUpdated(float dbValue)
	{
		if (_volumeMeter == null) return;
		// Map dB range -80..0 → progress 0..100
		float progress = Mathf.Remap(dbValue, -80f, 0f, 0f, 100f);
		_volumeMeter.Value = Mathf.Clamp(progress, 0f, 100f);
	}

	private void OnTokenTransferred(Node3D newHolder)
	{
		_tokenIndicator.Visible = true;

		// Pulse scale
		var tween = CreateTween();
		tween.TweenProperty(_tokenIndicator, "scale", new Vector2(1.3f, 1.3f), 0.1f);
		tween.TweenProperty(_tokenIndicator, "scale", new Vector2(1.0f, 1.0f), 0.15f);
	}

	// ------------------------------------------------------------------ //
	//  Tier UI update
	// ------------------------------------------------------------------ //
	private void UpdateTierUI(int tier)
	{
		if (_tierLabel == null) return;
		VoiceTier voiceTier = (VoiceTier)tier;
		_tierLabel.Text = "Voice: " + voiceTier.ToString();

		Color targetColor = tier switch
		{
			1 => ColorWhisper,
			2 => ColorNormal,
			3 => ColorShouting,
			_ => ColorSilent
		};
		_tierLabel.Modulate = targetColor;
		if (_volumeMeter != null) _volumeMeter.Modulate = targetColor;
	}
}
