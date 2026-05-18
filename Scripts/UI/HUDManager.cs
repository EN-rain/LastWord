using Godot;

public partial class HUDManager : Control
{
	[Export] public NodePath TierLabelPath;
	[Export] public NodePath VolumeMeterPath;
	[Export] public NodePath TokenIndicatorPath;
	[Export] public NodePath RoomCodeLabelPath;
	[Export] public NodePath HolderNameLabelPath;
	[Export] public NodePath HoldTimerLabelPath;

	private Label _tierLabel;
	private ProgressBar _volumeMeter;
	private TextureRect _tokenIndicator;
	private Label _roomCodeLabel;
	private Label _holderNameLabel;
	private Label _holdTimerLabel;
	
	private float _currentHoldDuration = 0f;

	[Export] public string RoomCodePrefixText = "ROOM CODE: ";
	[Export] public string RoomCodeOfflineText = "ROOM CODE: OFFLINE";
	[Export] public string VoiceTierPrefixText = "Voice: ";

	[Export] public Color ColorSilent   = new Color(0.45f, 0.45f, 0.45f);
	[Export] public Color ColorWhisper  = new Color(0.2f,  0.85f, 0.2f);
	[Export] public Color ColorNormal   = new Color(0.9f,  0.8f,  0.1f);
	[Export] public Color ColorShouting = new Color(0.9f,  0.2f,  0.2f);

	[Export] public float TokenPulseScale = 1.3f;
	[Export] public float TokenPulseUpDuration = 0.1f;
	[Export] public float TokenPulseDownDuration = 0.15f;

	public override void _Ready()
	{
		_tierLabel       = GetNodeOrNull<Label>(TierLabelPath);
		_volumeMeter     = GetNodeOrNull<ProgressBar>(VolumeMeterPath);
		_tokenIndicator  = GetNodeOrNull<TextureRect>(TokenIndicatorPath);
		_roomCodeLabel   = GetNodeOrNull<Label>(RoomCodeLabelPath);
		_holderNameLabel = GetNodeOrNull<Label>(HolderNameLabelPath);
		_holdTimerLabel  = GetNodeOrNull<Label>(HoldTimerLabelPath);

		// Update the room code display
		if (_roomCodeLabel != null)
		{
			if (NetworkManager.Instance != null && !string.IsNullOrEmpty(NetworkManager.Instance.CurrentRoomCode))
			{
				_roomCodeLabel.Text = $"{RoomCodePrefixText}{NetworkManager.Instance.CurrentRoomCode}";
			}
			else
			{
				_roomCodeLabel.Text = RoomCodeOfflineText;
			}
		}

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
	}

	public override void _ExitTree()
	{
		if (VoiceManager.Instance != null)
		{
			VoiceManager.Instance.TierChanged      -= OnTierChanged;
			VoiceManager.Instance.VolumeUpdated    -= OnVolumeUpdated;
			VoiceManager.Instance.TokenTransferred -= OnTokenTransferred;
		}

		if (_tokenIndicator != null) _tokenIndicator.Visible = false;
		if (_holderNameLabel != null) _holderNameLabel.Text = "Token: None";
		if (_holdTimerLabel != null) _holdTimerLabel.Text = "00:00.00";
		
		UpdateTierUI(0);
	}

	public override void _Process(double delta)
	{
		if (VoiceManager.Instance != null && VoiceManager.Instance.TokenHolder != null)
		{
			_currentHoldDuration += (float)delta;
			if (_holdTimerLabel != null)
			{
				_holdTimerLabel.Text = FormatTime(_currentHoldDuration);
			}
		}
	}

	private string FormatTime(float timeInSeconds)
	{
		int minutes = (int)(timeInSeconds / 60);
		int seconds = (int)(timeInSeconds % 60);
		int msec = (int)((timeInSeconds * 100) % 100);
		return string.Format("{0:00}:{1:00}.{2:00}", minutes, seconds, msec);
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
		if (newHolder == null)
		{
			if (_tokenIndicator != null) _tokenIndicator.Visible = false;
			if (_holderNameLabel != null) _holderNameLabel.Text = "Token: None";
			return;
		}

		// Update labels regardless of icon availability
		if (_holderNameLabel != null)
		{
			_holderNameLabel.Text = $"Token: {newHolder.Name}";
			_currentHoldDuration = 0f;
		}

		if (_tokenIndicator != null)
		{
			_tokenIndicator.Visible = true;
			var tween = CreateTween();
			tween.TweenProperty(_tokenIndicator, "scale", new Vector2(TokenPulseScale, TokenPulseScale), TokenPulseUpDuration);
			tween.TweenProperty(_tokenIndicator, "scale", new Vector2(1.0f, 1.0f), TokenPulseDownDuration);
		}
	}

	// ------------------------------------------------------------------ //
	//  Tier UI update
	// ------------------------------------------------------------------ //
	private void UpdateTierUI(int tier)
	{
		if (_tierLabel == null) return;
		VoiceTier voiceTier = (VoiceTier)tier;
		_tierLabel.Text = VoiceTierPrefixText + voiceTier.ToString();

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
