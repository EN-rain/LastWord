using Godot;

public partial class HUDManager : Control
{
	public static HUDManager Instance { get; private set; }

	[Export] public NodePath WordListLabelPath;

	private WordRegistry _wordRegistry;
	private int _registeredWordCount = 0;

	[Export] public NodePath TierLabelPath;
	[Export] public NodePath VolumeMeterPath;
	[Export] public NodePath TokenIndicatorPath;
	[Export] public NodePath RoomCodeLabelPath;
	[Export] public NodePath HolderNameLabelPath;
	[Export] public NodePath HoldTimerLabelPath;
	[Export] public NodePath PlayerStateLabelPath;
	[Export] public NodePath GestureWheelPath;
	[Export] public NodePath PhaseTrackerPath;
	[Export] public NodePath ProximityPulsePath;
	[Export] public NodePath FinalCountdownLabelPath;

	private Label _tierLabel;
	private ProgressBar _volumeMeter;
	private TextureRect _tokenIndicator;
	private Label _roomCodeLabel;
	private Label _holderNameLabel;
	private Label _holdTimerLabel;
	private Label _playerStateLabel;
	private Label _wordListLabel;
	private GestureWheel _gestureWheel;
	private HBoxContainer _phaseTracker;
	private ColorRect _proximityPulse;
	private readonly Label[] _phaseDots = new Label[3];
	private double _tokenPulseElapsed = 0.0;
	private double _tokenPulsePeriod = 2.0;
	private bool _tokenIndicatorActive = false;

	private float _currentHoldDuration = 0f;

	private Label _finalCountdownLabel;
	private float _finalCountdownRemaining = 0f;
	private bool _finalCountdownActive = false;

	[Export] public string RoomCodePrefixText = "ROOM CODE: ";
	[Export] public string RoomCodeOfflineText = "ROOM CODE: OFFLINE";
	[Export] public string VoiceTierPrefixText = "Voice: ";

	[Export] public Color ColorSilent   = new Color(0.45f, 0.45f, 0.45f);
	[Export] public Color ColorWhisper  = new Color(0.4f,  0.75f, 0.95f);
	[Export] public Color ColorNormal   = new Color(0.9f,  0.8f,  0.1f);
	[Export] public Color ColorShouting = new Color(0.9f,  0.2f,  0.2f);

	[Export] public float TokenPulseScale = 1.3f;
	[Export] public float TokenPulseUpDuration = 0.1f;
	[Export] public float TokenPulseDownDuration = 0.15f;

	[ExportGroup("Token Proximity Pulse")]
	[Export] public float TokenSlowDistance = 12.0f;
	[Export] public float TokenMediumDistance = 8.0f;
	[Export] public float TokenFastDistance = 4.0f;
	[Export] public float TokenSlowPeriod = 2.0f;
	[Export] public float TokenMediumPeriod = 1.0f;
	[Export] public float TokenFastPeriod = 0.5f;
	[Export] public float TokenRapidPeriod = 0.2f;

	[ExportGroup("Listener Proximity Pulse")]
	[Export] public float ProximityPulseMaxDistance = 12.0f;
	[Export] public float ProximityPulseUpdateInterval = 0.5f;
	[Export] public Color ProximityPulseColor = new Color(1.0f, 0.35f, 0.05f, 0.0f);
	[Export] public float ProximityPulseMaxAlpha = 0.35f;
	private float _proximityPulseTimer = 0f;
	private bool _proximityPulseEnabled = false;

	public override void _Ready()
	{
		Instance = this;
		_tierLabel       = GetNodeOrNull<Label>(TierLabelPath);
		_volumeMeter     = GetNodeOrNull<ProgressBar>(VolumeMeterPath);
		_tokenIndicator  = GetNodeOrNull<TextureRect>(TokenIndicatorPath);
		_roomCodeLabel   = GetNodeOrNull<Label>(RoomCodeLabelPath);
		_holderNameLabel = GetNodeOrNull<Label>(HolderNameLabelPath);
		_holdTimerLabel  = GetNodeOrNull<Label>(HoldTimerLabelPath);
		_playerStateLabel = GetNodeOrNull<Label>(PlayerStateLabelPath);
		_wordListLabel    = GetNodeOrNull<Label>(WordListLabelPath);
		_gestureWheel     = GetNodeOrNull<GestureWheel>(GestureWheelPath);
		if (_gestureWheel != null)
			_gestureWheel.GestureSelected += OnGestureSelected;
		_sacrificeCountdownLabel = GetNodeOrNull<Label>(SacrificeCountdownLabelPath);
		_phaseTracker     = GetNodeOrNull<HBoxContainer>(PhaseTrackerPath);
		_proximityPulse   = GetNodeOrNull<ColorRect>(ProximityPulsePath);
		_finalCountdownLabel = GetNodeOrNull<Label>(FinalCountdownLabelPath);
		if (_finalCountdownLabel != null) _finalCountdownLabel.Visible = false;

		BindPhaseTrackerDots();
		LoadProximityPulseSetting();

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

		// Reset token UI to a clean hidden state (#7: visible-by-default in tscn)
		if (_tokenIndicator  != null) _tokenIndicator.Visible  = false;
		if (_holderNameLabel != null) _holderNameLabel.Text    = "Token: None";
		if (_holdTimerLabel  != null) _holdTimerLabel.Text     = "00:00.00";
		_currentHoldDuration = 0f;
		UpdateTierUI(0);
		UpdatePhaseTracker(GameManager.GamePhase.Phase1);
		SetProximityPulseActive(false);

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

		if (NetworkManager.Instance != null)
			NetworkManager.Instance.RoomCodeUpdated += OnRoomCodeUpdated;

		// Connect to GameManager for phase/objective updates.
		var gameManager = GameManager.Instance;
		if (gameManager != null)
		{
			gameManager.PhaseChanged += OnPhaseChanged;
			gameManager.FinalCountdownStarted += OnFinalCountdownStarted;
			gameManager.FinalCountdownTick += OnFinalCountdownTick;
			UpdateObjectiveText(gameManager.CurrentPhase);
		}

		// Connect to WordRegistry (Phase 3 integration). If the node doesn't
		// exist as a singleton/autoload yet, fall back to scanning the tree.
		_wordRegistry = WordRegistry.Instance;
		if (_wordRegistry == null)
		{
			_wordRegistry = GetTree().Root.FindChild("WordRegistry", true, false) as WordRegistry;
		}
		if (_wordRegistry != null)
		{
			WordRegistry.Instance = _wordRegistry;
			_wordRegistry.RegisteredWord       += OnRegisteredWord;
			_wordRegistry.AllWordsRegistered   += OnAllWordsRegistered;
			GD.Print($"HUDManager: connected to WordRegistry (total target = {_wordRegistry.TotalWords}).");
		}
		else
		{
			GD.Print("HUDManager: WordRegistry not found; deferring connection.");
			CallDeferred(nameof(ConnectWordRegistry));
		}
	}

	private void ConnectWordRegistry()
	{
		_wordRegistry = WordRegistry.Instance ?? GetTree().Root.FindChild("WordRegistry", true, false) as WordRegistry;
		if (_wordRegistry == null) return;
		WordRegistry.Instance = _wordRegistry;
		_wordRegistry.RegisteredWord     += OnRegisteredWord;
		_wordRegistry.AllWordsRegistered += OnAllWordsRegistered;
	}

	public override void _ExitTree()
	{
		if (Instance == this) Instance = null;

		if (VoiceManager.Instance != null)
		{
			VoiceManager.Instance.TierChanged      -= OnTierChanged;
			VoiceManager.Instance.VolumeUpdated    -= OnVolumeUpdated;
			VoiceManager.Instance.TokenTransferred -= OnTokenTransferred;
		}

		if (NetworkManager.Instance != null)
			NetworkManager.Instance.RoomCodeUpdated -= OnRoomCodeUpdated;

		if (_tokenIndicator != null) _tokenIndicator.Visible = false;
		if (_holderNameLabel != null) _holderNameLabel.Text = "Token: None";
		if (_holdTimerLabel != null) _holdTimerLabel.Text = "00:00.00";
		_currentHoldDuration = 0f;

		if (_wordRegistry != null)
		{
			_wordRegistry.RegisteredWord     -= OnRegisteredWord;
			_wordRegistry.AllWordsRegistered -= OnAllWordsRegistered;
		}

		if (_gestureWheel != null)
			_gestureWheel.GestureSelected -= OnGestureSelected;

		var gameManager = GameManager.Instance;
		if (gameManager != null)
		{
			gameManager.PhaseChanged -= OnPhaseChanged;
			gameManager.FinalCountdownStarted -= OnFinalCountdownStarted;
			gameManager.FinalCountdownTick -= OnFinalCountdownTick;
		}

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

		UpdateTokenProximityPulse(delta);
		UpdateProximityPulse(delta);
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

	private void OnGestureSelected(GestureId gesture)
	{
		GetLocalGestureSystem()?.PlayGesture(gesture);
	}

	private GestureSystem GetLocalGestureSystem()
	{
		foreach (Node node in GetTree().GetNodesInGroup("Player"))
		{
			if (node is not PlayerController player || player.IsDead)
				continue;

			bool networked = Multiplayer.MultiplayerPeer != null && Multiplayer.HasMultiplayerPeer();
			if (!networked || player.IsMultiplayerAuthority())
				return player.GetNodeOrNull<GestureSystem>("GestureSystem");
		}

		return null;
	}

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
			// #8: clear stale hold-timer text so it never shows a leftover duration
			if (_tokenIndicator  != null) _tokenIndicator.Visible  = false;
			if (_holderNameLabel != null) _holderNameLabel.Text    = "Token: None";
			if (_holdTimerLabel  != null) _holdTimerLabel.Text     = "00:00.00";
			_currentHoldDuration = 0f;
			_tokenIndicatorActive = false;
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
			_tokenIndicatorActive = true;
			var tween = CreateTween();
			tween.TweenProperty(_tokenIndicator, "scale", new Vector2(TokenPulseScale, TokenPulseScale), TokenPulseUpDuration);
			tween.TweenProperty(_tokenIndicator, "scale", new Vector2(1.0f, 1.0f), TokenPulseDownDuration);
		}
	}

	private void OnRoomCodeUpdated(string newCode)
	{
		if (_roomCodeLabel != null)
			_roomCodeLabel.Text = string.IsNullOrEmpty(newCode) ? RoomCodeOfflineText : $"{RoomCodePrefixText}{newCode}";
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

	public void UpdatePlayerState(string stateText, Color stateColor)
	{
		if (_playerStateLabel != null)
		{
			_playerStateLabel.Text = $"State: {stateText}";
			_playerStateLabel.Modulate = stateColor;
		}
	}

	public void UpdateFrenzyTarget(Node3D target)
	{
		if (_playerStateLabel != null && target != null)
		{
			_playerStateLabel.Text = $"State: SCREAM LOCK - {target.Name}";
			_playerStateLabel.Modulate = Colors.Red;
		}
	}

	// ------------------------------------------------------------------ //
	//  Objective HUD markers
	// ------------------------------------------------------------------ //
	private void OnPhaseChanged(GameManager.GamePhase phase)
	{
		UpdateObjectiveText(phase);
		UpdatePhaseTracker(phase);
	}

	private void UpdateObjectiveText(GameManager.GamePhase phase)
	{
		if (_playerStateLabel == null) return;
		string text = phase switch
		{
			GameManager.GamePhase.Phase1 => "Objective: Collect and register 4 words",
			GameManager.GamePhase.Phase2 => "Objective: Speak the sequence in order",
			GameManager.GamePhase.Phase3 => "Objective: Transmit the final broadcast",
			GameManager.GamePhase.Victory => "ESCAPE SUCCESSFUL",
			GameManager.GamePhase.Failed => "RUN FAILED",
			_ => "Objective: Survive"
		};
		_playerStateLabel.Text = $"State: {text}";
		_playerStateLabel.Modulate = phase == GameManager.GamePhase.Victory ? Colors.Green
			: phase == GameManager.GamePhase.Failed ? Colors.Red
			: Colors.White;
	}

	// ------------------------------------------------------------------ //
	//  Vocal Sacrifice HUD feedback
	// ------------------------------------------------------------------ //
	private Label _sacrificeCountdownLabel;
	private string _sacrificePlayerName = "";

	[Export] public NodePath SacrificeCountdownLabelPath;

	public void PulseTeammateForSacrifice(string playerName)
	{
		// TODO: amber pulse on dedicated teammate status bar when implemented.
		// For MVP, flash the player state label amber briefly.
		UpdatePlayerState($"Sacrifice pre-signal from {playerName}", new Color(1f, 0.65f, 0f));
	}

	public void ShowSacrificeCountdown(float duration, string playerName)
	{
		_sacrificePlayerName = playerName;
		UpdateSacrificeCountdown(duration);
	}

	public void UpdateSacrificeCountdown(float remaining)
	{
		if (_sacrificeCountdownLabel != null)
		{
			_sacrificeCountdownLabel.Text = $"SACRIFICE LOCK: {_sacrificePlayerName} {remaining:F1}s";
			_sacrificeCountdownLabel.Modulate = Colors.Orange;
			_sacrificeCountdownLabel.Visible = remaining > 0f;
		}
	}

	public void HideSacrificeCountdown()
	{
		if (_sacrificeCountdownLabel != null)
			_sacrificeCountdownLabel.Visible = false;
	}

	private void OnFinalCountdownStarted(float duration)
	{
		_finalCountdownActive = true;
		_finalCountdownRemaining = duration;
		if (_finalCountdownLabel != null)
		{
			_finalCountdownLabel.Visible = true;
			_finalCountdownLabel.Text = $"FINAL COUNTDOWN: {FormatTime(_finalCountdownRemaining)}";
		}
	}

	private void OnFinalCountdownTick(float remaining)
	{
		_finalCountdownRemaining = remaining;
		if (_finalCountdownLabel != null && _finalCountdownActive)
		{
			_finalCountdownLabel.Text = $"FINAL COUNTDOWN: {FormatTime(_finalCountdownRemaining)}";
			if (remaining <= 0f)
				_finalCountdownLabel.Visible = false;
		}
	}

	// ------------------------------------------------------------------ //
	//  Phase 3 — WordRegistry signal handlers
	// ------------------------------------------------------------------ //
	private void OnRegisteredWord(string word, string peerName)
	{
		_registeredWordCount++;
		GD.Print($"HUDManager.PhaseTracker: word registered '{word}' by {peerName} ({_registeredWordCount}/{(_wordRegistry != null ? _wordRegistry.TotalWords : 0)}).");
		if (_wordListLabel != null)
		{
			var existing = string.IsNullOrEmpty(_wordListLabel.Text) ? "Words:" : _wordListLabel.Text;
			_wordListLabel.Text = $"{existing}\n  - {word} ({peerName})";
		}
	}

	private void OnAllWordsRegistered()
	{
		GD.Print($"HUDManager.PhaseTracker: ALL words registered ({_registeredWordCount}). Round complete.");
		if (_wordListLabel != null)
		{
			_wordListLabel.Text = $"Words (COMPLETE):\n{_wordListLabel.Text}";
		}
	}


	// ------------------------------------------------------------------ //
	//  Phase Tracker
	// ------------------------------------------------------------------ //
	private void BindPhaseTrackerDots()
	{
		if (_phaseTracker == null) return;
		for (int i = 0; i < 3; i++)
		{
			_phaseDots[i] = _phaseTracker.GetNodeOrNull<Label>($"Dot{i + 1}");
		}
	}

	private void UpdatePhaseTracker(GameManager.GamePhase phase)
	{
		int current = phase switch
		{
			GameManager.GamePhase.Phase1 => 0,
			GameManager.GamePhase.Phase2 => 1,
			GameManager.GamePhase.Phase3 => 2,
			GameManager.GamePhase.Victory => 3,
			GameManager.GamePhase.Failed => 3,
			_ => 0
		};

		for (int i = 0; i < 3; i++)
		{
			var dot = _phaseDots[i];
			if (dot == null) continue;

			bool completed = i < current;
			bool currentPhase = (i == current && current < 3);
			bool failed = phase == GameManager.GamePhase.Failed && i == current && current < 3;
			bool victory = phase == GameManager.GamePhase.Victory;

			if (failed)
			{
				dot.Modulate = Colors.Red;
				dot.AddThemeFontSizeOverride("font_size", 32);
			}
			else if (victory)
			{
				dot.Modulate = new Color(1.0f, 0.84f, 0.0f);
				dot.AddThemeFontSizeOverride("font_size", 32);
			}
			else if (completed)
			{
				dot.Modulate = Colors.White;
				dot.AddThemeFontSizeOverride("font_size", 28);
			}
			else if (currentPhase)
			{
				dot.Modulate = new Color(0.7f, 0.85f, 1.0f);
				dot.AddThemeFontSizeOverride("font_size", 32);
			}
			else
			{
				dot.Modulate = new Color(0.5f, 0.5f, 0.5f, 0.5f);
				dot.AddThemeFontSizeOverride("font_size", 28);
			}
		}
	}

	// ------------------------------------------------------------------ //
	//  Token proximity pulse
	// ------------------------------------------------------------------ //
	private void UpdateTokenProximityPulse(double delta)
	{
		if (_tokenIndicator == null || !_tokenIndicatorActive) return;

		var listener = FindListener();
		var localPlayer = FindLocalPlayer();
		double distance = listener != null && localPlayer != null
			? listener.GlobalPosition.DistanceTo(localPlayer.GlobalPosition)
			: TokenSlowDistance + 1.0;

		double targetPeriod;
		if (distance > TokenSlowDistance)
			targetPeriod = TokenSlowPeriod;
		else if (distance >= TokenMediumDistance)
			targetPeriod = TokenMediumPeriod;
		else if (distance >= TokenFastDistance)
			targetPeriod = TokenFastPeriod;
		else
			targetPeriod = TokenRapidPeriod;

		_tokenPulsePeriod = targetPeriod;
		_tokenPulseElapsed += delta;

		double halfPeriod = _tokenPulsePeriod * 0.5;
		double t = (_tokenPulseElapsed % _tokenPulsePeriod) / halfPeriod;
		if (t > 1.0) t = 2.0 - t;
		float scale = Mathf.Lerp(1.0f, TokenPulseScale, (float)t);
		_tokenIndicator.Scale = new Vector2(scale, scale);
	}

	// ------------------------------------------------------------------ //
	//  Listener proximity pulse (accessibility)
	// ------------------------------------------------------------------ //
	private void LoadProximityPulseSetting()
	{
		var cfg = new ConfigFile();
		if (cfg.Load("user://settings.cfg") == Error.Ok)
		{
			_proximityPulseEnabled = (bool)cfg.GetValue("settings", "proximity_pulse", false);
		}
		else
		{
			_proximityPulseEnabled = false;
		}
	}

	private void UpdateProximityPulse(double delta)
	{
		if (_proximityPulse == null || !_proximityPulseEnabled) return;

		_proximityPulseTimer += (float)delta;
		if (_proximityPulseTimer < ProximityPulseUpdateInterval) return;
		_proximityPulseTimer = 0f;

		var listener = FindListener();
		var localPlayer = FindLocalPlayer();
		if (listener == null || localPlayer == null)
		{
			SetProximityPulseActive(false);
			return;
		}

		float distance = listener.GlobalPosition.DistanceTo(localPlayer.GlobalPosition);
		if (distance > ProximityPulseMaxDistance)
		{
			SetProximityPulseActive(false);
			return;
		}

		float intensity = 1.0f - Mathf.Clamp(distance / ProximityPulseMaxDistance, 0f, 1f);
		float alpha = ProximityPulseMaxAlpha * intensity * intensity;
		SetProximityPulseActive(true, alpha);
	}

	private void SetProximityPulseActive(bool active, float alpha = 0f)
	{
		if (_proximityPulse == null) return;
		_proximityPulse.Visible = active;
		if (active)
		{
			_proximityPulse.Color = new Color(ProximityPulseColor.R, ProximityPulseColor.G, ProximityPulseColor.B, alpha);
		}
	}

	private Node3D FindLocalPlayer()
	{
		return GetTree().GetFirstNodeInGroup("Player") as Node3D;
	}

	private ListenerAI FindListener()
	{
		return GetTree().GetFirstNodeInGroup("Listener") as ListenerAI;
	}
}

public static class WordRegistryLocator
{
	// Helper for callers that need a strongly-typed accessor.
	public static WordRegistry Resolve(Node from)
	{
		if (WordRegistry.Instance != null) return WordRegistry.Instance;
		return from.GetTree().Root.FindChild("WordRegistry", true, false) as WordRegistry;
	}
}
