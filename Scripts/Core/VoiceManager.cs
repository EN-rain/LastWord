using Godot;
using System;
using System.Collections.Generic;
using System.Linq;

public enum VoiceTier
{
	Silent = 0,     // < -50dB  (RMS)
	Whisper = 1,    // -50dB to -35dB
	Normal = 2,     // -35dB to -15dB
	Shouting = 3    // > -15dB
}

public partial class VoiceManager : Node
{
	public static VoiceManager Instance { get; private set; }

	[Signal] public delegate void TierChangedEventHandler(int newTier);
	[Signal] public delegate void VolumeUpdatedEventHandler(float dbValue);
	[Signal] public delegate void TokenTransferredEventHandler(Node3D newHolder);
	[Signal] public delegate void CalibrationProgressEventHandler(float progress); // 0.0 to 1.0
	[Signal] public delegate void CalibrationFinishedEventHandler(float newBaseline);
	public event Action<long, string, int> RecognizedSpeechSubmitted;

	[Export] public string MicBusName = "Microphone";
	[Export] public string MicSendBusName = "Master";
	[Export] public float AnalysisInterval = 0.05f;
	[Export] public float VoiceNoiseRepeatInterval = 0.25f;

	private int _micBusIndex = -1;
	private AudioStreamPlayer _micInput;
	private AudioEffectCapture _captureEffect;

	private VoiceTier _currentTier = VoiceTier.Silent;
	public VoiceTier CurrentTier => _currentTier;

	// ---- Per-run tracking (consumed by AchievementManager) ----
	private VoiceTier _maxTierThisRun = VoiceTier.Silent;
	/// <summary>Highest tier reached this run. Used to gate "silent run" achievements.</summary>
	public VoiceTier MaxTierReachedThisRun => _maxTierThisRun;
	private float _secondsSinceLastVoice = 999f;
	/// <summary>Seconds since the player last produced any voice above Silent.</summary>
	public float SecondsSinceLastVoice => _secondsSinceLastVoice;
	/// <summary>Call at the start of a new run to reset per-run voice tracking.</summary>
	public void ResetRunTracking()
	{
		_maxTierThisRun = VoiceTier.Silent;
		_secondsSinceLastVoice = 999f;
	}

	private float _timer = 0f;
	private float _voiceNoiseRepeatTimer = 0f;
	private float _smoothedRms = 0f;
	[Export] public float SmoothingFactor = 0.2f; // 0.0 to 1.0 (lower = smoother/slower)

	// Calibration
	public float BaselineAmplitude { get; set; } = 0.05f; // Default baseline
	public bool IsGdprAccepted { get; private set; } = false;
	public bool IsCalibrating { get; private set; } = false;
	private float _calibrationTimer = 0f;
	private System.Collections.Generic.List<float> _calibrationSamples = new();
	[Export] public float CalibrationDuration = 30.0f;

	// Token System
	private Node3D _tokenHolder = null;
	public Node3D TokenHolder => _tokenHolder;
	private long _tokenHolderPeerId = 0;
	public long TokenHolderPeerId => _tokenHolderPeerId;

	public override void _EnterTree()
	{
		Instance = this;
	}

	public override async void _Ready()
	{
		ValidateTuningValues();
		LoadBaseline();
		ValidateTuningValues();
		EnsureInputActions();

		// Wait a few frames for the AudioServer and Windows drivers to stabilize
		await ToSignal(GetTree().CreateTimer(0.5f), "timeout");
		
		SetupMicBus();
		SetupMicInput();
	}

	/// <summary>
	/// Ensures gameplay input actions exist before any player controller reads them.
	/// This runs before the settings menu opens, so gestures and abilities work
	/// immediately in the main menu, lobby, and game scenes.
	/// </summary>
	private void EnsureInputActions()
	{
		var defaults = new System.Collections.Generic.Dictionary<string, Key>
		{
			{ "move_forward", Key.W },
			{ "move_backward", Key.S },
			{ "move_left", Key.A },
			{ "move_right", Key.D },
			{ "move_jump", Key.Space },
			{ "move_sprint", Key.Shift },
			{ "gesture_z", Key.Z },
			{ "gesture_x", Key.X },
			{ "gesture_c", Key.C },
			{ "gesture_v", Key.V },
			{ "gesture_b", Key.B },
			{ "gesture_n", Key.N },
			{ "gesture_m", Key.M },
			{ "gesture_l", Key.L },
			{ "interact", Key.E },
			{ "clap_q", Key.Q },
			{ "sacrifice_g", Key.G },
			{ "ability_f", Key.F },
			{ "ability_r", Key.R },
			{ "ability_t", Key.T },
			{ "spectator_j", Key.J }
		};

		foreach (var kvp in defaults)
		{
			if (!InputMap.HasAction(kvp.Key))
			{
				InputMap.AddAction(kvp.Key);
				InputMap.ActionAddEvent(kvp.Key, new InputEventKey { PhysicalKeycode = kvp.Value });
			}
		}

		if (!InputMap.HasAction("radial_wheel_mmb"))
		{
			InputMap.AddAction("radial_wheel_mmb");
			InputMap.ActionAddEvent("radial_wheel_mmb", new InputEventMouseButton { ButtonIndex = MouseButton.Middle });
		}
	}

	private void ValidateTuningValues()
	{
		if (string.IsNullOrWhiteSpace(MicBusName))
			MicBusName = "Microphone";

		if (string.IsNullOrWhiteSpace(MicSendBusName))
			MicSendBusName = "Master";

		AnalysisInterval = Mathf.Max(AnalysisInterval, 0.01f);
		VoiceNoiseRepeatInterval = Mathf.Max(VoiceNoiseRepeatInterval, AnalysisInterval);
		SmoothingFactor = Mathf.Clamp(SmoothingFactor, 0f, 1f);
		CalibrationDuration = Mathf.Max(CalibrationDuration, 0.1f);
		BaselineAmplitude = Mathf.Max(BaselineAmplitude, 0.001f);
	}

	public void SubmitRecognizedSpeech(long peerId, string text, int tier)
	{
		string normalized = text?.Trim() ?? string.Empty;
		if (peerId <= 0 || string.IsNullOrEmpty(normalized))
			return;

		RecognizedSpeechSubmitted?.Invoke(peerId, normalized, Mathf.Clamp(tier, 0, 3));
	}

	private void SetupMicBus()
	{
		if (AudioServer.GetBusIndex(MicSendBusName) == -1)
		{
			GD.PushWarning($"VoiceManager: MicSendBusName '{MicSendBusName}' does not exist. Falling back to Master.");
			MicSendBusName = "Master";
		}

		_micBusIndex = AudioServer.GetBusIndex(MicBusName);

		if (_micBusIndex == -1)
		{
			AudioServer.AddBus();
			_micBusIndex = AudioServer.BusCount - 1;
			AudioServer.SetBusName(_micBusIndex, MicBusName);
			AudioServer.SetBusSend(_micBusIndex, MicSendBusName);
		}
		
		// Ensure volume is up and bus is 'technically' muted to prevent loopback
		AudioServer.SetBusVolumeDb(_micBusIndex, 0);
		AudioServer.SetBusMute(_micBusIndex, true); 

		// Ensure an AudioEffectCapture is on the bus so we can read audio frames
		_captureEffect = null;
		for (int i = 0; i < AudioServer.GetBusEffectCount(_micBusIndex); i++)
		{
			if (AudioServer.GetBusEffect(_micBusIndex, i) is AudioEffectCapture existing)
			{
				_captureEffect = existing;
				break;
			}
		}

		if (_captureEffect == null)
		{
			var effect = new AudioEffectCapture();
			effect.BufferLength = 0.1f;
			AudioServer.AddBusEffect(_micBusIndex, effect);
			// The resource itself is the object we query — no separate instance needed
			int idx = AudioServer.GetBusEffectCount(_micBusIndex) - 1;
			_captureEffect = (AudioEffectCapture)AudioServer.GetBusEffect(_micBusIndex, idx);
		}
	}

	private void SetupMicInput()
	{
		string[] inputDevices = AudioServer.GetInputDeviceList();
		
		// Validation: If the current device is missing, fallback to Default
		bool deviceFound = false;
		foreach (var dev in inputDevices)
		{
			if (dev == AudioServer.InputDevice)
			{
				deviceFound = true;
				break;
			}
		}

		if (!deviceFound && AudioServer.InputDevice != "Default")
		{
			AudioServer.InputDevice = "Default";
		}

		try
		{
			if (_micInput == null)
			{
				_micInput = new AudioStreamPlayer();
				_micInput.Name = "MicInputPlayer";
				AddChild(_micInput);
			}

			_micInput.Bus = MicBusName;
			_micInput.Stream = new AudioStreamMicrophone();
			_micInput.Play();
		}
		catch (Exception e)
		{
			GD.PrintErr($"VoiceManager: CRITICAL ERROR starting mic: {e.Message}");
		}
	}

	// ------------------------------------------------------------------ //
	//  Called by PauseMenu when the mic device changes
	// ------------------------------------------------------------------ //
	public void RestartCapture()
	{
		if (_micInput != null)
		{
			_micInput.Stop();
			_micInput.Stream = null;
			// Force a small delay or reconstruction
			_micInput.Stream = new AudioStreamMicrophone();
			_micInput.Play();
		}
		
		SetupMicBus();
		_currentTier = VoiceTier.Silent; // Force re-classification
	}

	// ------------------------------------------------------------------ //
	//  Process
	// ------------------------------------------------------------------ //
	public override void _Process(double delta)
	{
		// Token is intentionally left null until the first validated speech event.
		// Auto-assigning here would bypass the speech-driven transfer design.

		_secondsSinceLastVoice += (float)delta;

		_timer += (float)delta;
		if (_timer >= AnalysisInterval)
		{
			float dt = _timer;
			_timer = 0f;
			AnalyzeVoice(dt);
		}
	}
	public void StartCalibration()
	{
		IsCalibrating = true;
		_calibrationTimer = 0f;
		_calibrationSamples.Clear();
	}

	public void SetGdprAccepted(bool accepted)
	{
		IsGdprAccepted = accepted;

		if (accepted)
			return;

		IsCalibrating = false;
		_calibrationTimer = 0f;
		_calibrationSamples.Clear();
		_smoothedRms = 0f;
		_tokenHolder = null;
		_tokenHolderPeerId = 0;

		if (_currentTier != VoiceTier.Silent)
		{
			_currentTier = VoiceTier.Silent;
			EmitSignal(SignalName.TierChanged, (int)_currentTier);
		}

		EmitSignal(SignalName.VolumeUpdated, -80f);
		EmitSignal(SignalName.TokenTransferred, (Node3D)null);
	}

	private void AnalyzeVoice(float delta)
	{
		_voiceNoiseRepeatTimer += delta;

		// --- Keyboard simulated voice input overrides (dev testing, GDPR-gated) ---
		// Only active when GDPR consent has been given so it cannot be abused on startup.
		if (IsGdprAccepted)
		{
			bool forceWhisper = Input.IsKeyPressed(Key.Key1);
			bool forceNormal  = Input.IsKeyPressed(Key.Key2);
			bool forceScream  = Input.IsKeyPressed(Key.Key3);
			bool isForced     = forceWhisper || forceNormal || forceScream;

			if (isForced)
			{
				VoiceTier forcedTier = VoiceTier.Silent;
				float forcedRms = 0f;
				float forcedDb  = -80f;

				if (forceWhisper)
				{
					forcedTier = VoiceTier.Whisper;
					forcedRms  = BaselineAmplitude * 0.5f;
					forcedDb   = -40f;
				}
				else if (forceNormal)
				{
					forcedTier = VoiceTier.Normal;
					forcedRms  = BaselineAmplitude * 1.2f;
					forcedDb   = -25f;
				}
				else if (forceScream)
				{
					forcedTier = VoiceTier.Shouting;
					forcedRms  = BaselineAmplitude * 2.5f;
					forcedDb   = -5f;
				}

				_smoothedRms = forcedRms;
				EmitSignal(SignalName.VolumeUpdated, forcedDb);

				if (forcedTier != _currentTier)
				{
					_currentTier = forcedTier;
					EmitSignal(SignalName.TierChanged, (int)_currentTier);

					if (_currentTier >= VoiceTier.Whisper)
						UpdateTokenHolder();

					BroadcastCurrentVoiceNoise(true);
				}
				else if (_currentTier >= VoiceTier.Normal)
				{
					BroadcastCurrentVoiceNoise(false);
				}
				return;
			}
		}

		if (!IsGdprAccepted)
		{
			if (_currentTier != VoiceTier.Silent)
			{
				_currentTier = VoiceTier.Silent;
				EmitSignal(SignalName.TierChanged, (int)_currentTier);
			}
			_voiceNoiseRepeatTimer = 0f;
			EmitSignal(SignalName.VolumeUpdated, -80f);
			return;
		}

		float db = -80f;

		// --- Primary: read actual audio frames from the capture buffer ---
		float instantaneousRms = 0f;

		if (_captureEffect != null)
		{
			var frames = _captureEffect.GetBuffer(_captureEffect.GetFramesAvailable());
			if (frames.Length > 0)
			{
				float sumSq = 0;
				foreach (var frame in frames)
					sumSq += frame.X * frame.X + frame.Y * frame.Y;

				instantaneousRms = Mathf.Sqrt(sumSq / (frames.Length * 2 + 0.000001f));
				
				// --- Software Noise Gate ---
				if (instantaneousRms < 0.0005f) instantaneousRms = 0f;
			}
		}
		else if (_micBusIndex >= 0)
		{
			// Fallback: bus peak meter
			float peak = AudioServer.GetBusPeakVolumeLeftDb(_micBusIndex, 0);
			instantaneousRms = Mathf.DbToLinear(peak);
		}

		// --- Exponential Moving Average (Smoothing) ---
		_smoothedRms = Mathf.Lerp(_smoothedRms, instantaneousRms, SmoothingFactor);
		
		// Convert to dB for UI display
		db = _smoothedRms > 0.00001f ? Mathf.LinearToDb(_smoothedRms) : -80f;

		// Emit raw dB to HUD meter
		EmitSignal(SignalName.VolumeUpdated, db);

		if (IsCalibrating)
		{
			ProcessCalibration(instantaneousRms, delta);
			return;
		}

		// Tier classification using SMOOTHED value
		VoiceTier newTier = ClassifyTier(_smoothedRms);
		newTier = (VoiceTier)Mathf.Max((int)newTier, (int)GetLocalMinimumTier());

		if (newTier != _currentTier)
		{
			_currentTier = newTier;
			if ((int)_currentTier > (int)_maxTierThisRun)
				_maxTierThisRun = _currentTier;
			if (_currentTier >= VoiceTier.Whisper)
				_secondsSinceLastVoice = 0f;
			EmitSignal(SignalName.TierChanged, (int)_currentTier);

			if (_currentTier >= VoiceTier.Whisper)
				UpdateTokenHolder();

			BroadcastCurrentVoiceNoise(true);
		}
		else if (_currentTier >= VoiceTier.Normal)
		{
			BroadcastCurrentVoiceNoise(false);
		}
	}

	private void BroadcastCurrentVoiceNoise(bool force)
	{
		if (_currentTier < VoiceTier.Whisper)
			return;

		if (!force && _currentTier < VoiceTier.Normal)
			return;

		if (!force && _voiceNoiseRepeatTimer < VoiceNoiseRepeatInterval)
			return;

		_voiceNoiseRepeatTimer = 0f;
		BroadcastNoiseEvent((int)_currentTier, SoundKind.Voice);
	}

	private void ProcessCalibration(float rms, float delta)
	{
		_calibrationTimer += delta;
		_calibrationSamples.Add(rms);

		float progress = Mathf.Clamp(_calibrationTimer / CalibrationDuration, 0f, 1f);
		EmitSignal(SignalName.CalibrationProgress, progress);

		if (_calibrationTimer >= CalibrationDuration)
		{
			IsCalibrating = false;
			float sum = 0;
			foreach (float s in _calibrationSamples) sum += s;
			
			BaselineAmplitude = Mathf.Max(sum / Mathf.Max(_calibrationSamples.Count, 1), 0.001f);
			EmitSignal(SignalName.CalibrationFinished, BaselineAmplitude);
		}
	}

	private VoiceTier GetLocalMinimumTier()
	{
		Node3D localPlayer = FindLocalPlayerNode();
		RoleData role = localPlayer?.GetNodeOrNull<RoleData>("RoleData");
		if (role != null && role.IsLoud)
			return (VoiceTier)Mathf.Clamp(role.LoudMinimumTier, 0, 3);
		return VoiceTier.Silent;
	}

	private Node3D FindLocalPlayerNode()
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

	private VoiceTier ClassifyTier(float currentRms)
	{
		float ratio = currentRms / BaselineAmplitude;
		
		// --- Hysteresis Logic ---
		// We require a higher threshold to ENTER a tier than to STAY in it.
		// This prevents flickering between states.
		
		float enterWhisper = 0.35f;
		float exitWhisper = 0.30f;
		
		float enterNormal = 1.00f;
		float exitNormal = 0.95f;
		
		float enterShouting = 2.00f;
		float exitShouting = 1.90f;

		if (_currentTier == VoiceTier.Shouting && ratio >= exitShouting)
			return VoiceTier.Shouting;

		if (ratio >= enterShouting)
			return VoiceTier.Shouting;

		if (_currentTier == VoiceTier.Normal && ratio >= exitNormal)
			return VoiceTier.Normal;

		if (ratio >= enterNormal)
			return VoiceTier.Normal;

		if (_currentTier == VoiceTier.Whisper && ratio >= exitWhisper)
			return VoiceTier.Whisper;

		if (ratio >= enterWhisper)
			return VoiceTier.Whisper;

		return VoiceTier.Silent;
	}

	private void UpdateTokenHolder()
	{
		if (Multiplayer.MultiplayerPeer != null && Multiplayer.HasMultiplayerPeer())
		{
			long myPeerId = Multiplayer.GetUniqueId();
			if (_tokenHolderPeerId != myPeerId)
			{
				if (Multiplayer.IsServer())
				{
					RequestTokenTransfer(0);
				}
				else
				{
					RpcId(NetworkManager.ServerPeerId, nameof(RequestTokenTransfer), myPeerId);
				}
			}
		}
		else
		{
			Node3D localPlayer = GetTree().GetFirstNodeInGroup("Player") as Node3D;
			if (localPlayer != null && _tokenHolder != localPlayer)
			{
				_tokenHolder = localPlayer;
				_tokenHolderPeerId = ResolvePeerId(localPlayer);
				EmitSignal(SignalName.TokenTransferred, _tokenHolder);
			}
		}
	}

	[Rpc(MultiplayerApi.RpcMode.AnyPeer, CallLocal = false)]
	private void RequestTokenTransfer(long _ignoredPeerId)
	{
		// Security: derive sender identity from transport layer, never from payload.
		if (!Multiplayer.IsServer()) return;
		long trustedPeerId = Multiplayer.GetRemoteSenderId();

		if (trustedPeerId == 0)
		{
			// Host is calling locally: resolve via group rather than peer-id name match.
			Node3D hostPlayer = GetTree().GetFirstNodeInGroup("Player") as Node3D;
			if (hostPlayer != null && _tokenHolder != hostPlayer)
			{
				Rpc(nameof(SyncTokenHolder), (long)Multiplayer.GetUniqueId());
			}
			return;
		}

		Rpc(nameof(SyncTokenHolder), trustedPeerId);
	}

	[Rpc(MultiplayerApi.RpcMode.Authority, CallLocal = true)]
	private void SyncTokenHolder(long peerId)
	{
		Node3D playerNode = FindPlayerNodeByPeerId(peerId);
		if (playerNode != null)
		{
			_tokenHolderPeerId = peerId;
			_tokenHolder = playerNode;
			EmitSignal(SignalName.TokenTransferred, _tokenHolder);
		}
	}

	private Node3D FindPlayerNodeByPeerId(long peerId)
	{
		if (Multiplayer.MultiplayerPeer == null || !Multiplayer.HasMultiplayerPeer() || peerId == 0)
		{
			return GetTree().GetFirstNodeInGroup("Player") as Node3D;
		}

		// Search by group membership first (robust against scene-root naming changes)
		var playerNodes = GetTree().GetNodesInGroup("Player");
		foreach (Node node in playerNodes)
		{
			if (node is Node3D node3D && node3D.Name == peerId.ToString())
				return node3D;
		}

		// Fallback: any Player node
		return GetTree().GetFirstNodeInGroup("Player") as Node3D;
	}

	public void BroadcastNoiseEvent(int tier) => BroadcastNoiseEvent(tier, SoundKind.Voice);

	public void BroadcastNoiseEvent(int tier, SoundKind kind, bool isSpecialLongRange = false)
	{
		tier = Mathf.Clamp(tier, 0, 3);
		int kindValue = (int)kind;
		if (Multiplayer.MultiplayerPeer != null && Multiplayer.HasMultiplayerPeer() && !Multiplayer.IsServer())
		{
			// Client sends only the tier — position is derived server-side from the sender's node.
            if (kind == SoundKind.Voice)
                NotifyLocalPlayerNoiseDebug(tier, kind);

            RpcId(NetworkManager.ServerPeerId, nameof(ReportNoiseEventRPC), tier, kindValue, isSpecialLongRange);
        }
        else
        {
            // Offline / Server / Solo host: resolve locally and immediately without RPC.
            Node3D localPlayer = GetTree().GetFirstNodeInGroup("Player") as Node3D;
            if (localPlayer != null && IsNoiseSourceAlive(localPlayer))
                ReportNoiseEvent(localPlayer.GlobalPosition, tier, kind, localPlayer, isSpecialLongRange);
        }
    }

    // Security: position is NOT accepted from clients — it is derived server-side
	// from the sender's owned player node to prevent position spoofing.
	[Rpc(MultiplayerApi.RpcMode.AnyPeer, CallLocal = false)]
	private void ReportNoiseEventRPC(int tier, int kindValue = 0, bool isSpecialLongRange = false)
	{
		if (!Multiplayer.IsServer()) return;

		long senderId = Multiplayer.GetRemoteSenderId();
		Node3D senderPlayer = FindPlayerNodeByPeerId(senderId == 0 ? (long)Multiplayer.GetUniqueId() : senderId);
		if (senderPlayer == null || !IsNoiseSourceAlive(senderPlayer) || IsNoiseSuppressed(senderPlayer)) return;

		DispatchNoiseToListeners(senderPlayer.GlobalPosition, Mathf.Clamp(tier, 0, 3), ParseSoundKind(kindValue), senderPlayer, isSpecialLongRange);
	}

	// Local (offline / host) entry-point — always safe because position is read from the actual node.
	public void ReportNoiseEvent(Vector3 trustedPosition, int tier) => ReportNoiseEvent(trustedPosition, tier, SoundKind.Voice, null);

	public void ReportNoiseEvent(Vector3 trustedPosition, int tier, SoundKind kind, Node3D source = null, bool isSpecialLongRange = false)
	{
		if (source != null && (!IsNoiseSourceAlive(source) || IsNoiseSuppressed(source)))
			return;
		if (IsMaskedByClockBell(tier, kind, isSpecialLongRange))
			return;

		DispatchNoiseToListeners(trustedPosition, Mathf.Clamp(tier, 0, 3), kind, source, isSpecialLongRange);
	}

	private void DispatchNoiseToListeners(Vector3 position, int tier, SoundKind kind, Node3D source, bool isSpecialLongRange)
	{
		if (source is PlayerController player)
			player.NotifyListenerNoise(tier, kind);

		var listenerEvent = new ListenerSoundEvent(position, tier, kind, source, isSpecialLongRange);
		var listeners = GetTree().GetNodesInGroup("Listener");
		foreach (var node in listeners)
		{
			if (node is ListenerAI ai)
				ai.HearNoise(listenerEvent);
		}
	}

	private bool IsMaskedByClockBell(int tier, SoundKind kind, bool isSpecialLongRange)
	{
		if (tier <= 0 || tier >= 3 || kind == SoundKind.Special || isSpecialLongRange)
			return false;

		foreach (Node node in GetTree().GetNodesInGroup("ClockBell"))
		{
			if (node is LastWord.World.ClockBell bell && bell.IsMaskActive)
				return true;
		}
		return false;
	}

	private void NotifyLocalPlayerNoiseDebug(int tier, SoundKind kind)
	{
		Node3D localPlayer = FindPlayerNodeByPeerId(Multiplayer.GetUniqueId());
		if (localPlayer is PlayerController player)
			player.NotifyListenerNoise(tier, kind);
	}

	private static SoundKind ParseSoundKind(int kindValue)
	{
		return Enum.IsDefined(typeof(SoundKind), kindValue)
			? (SoundKind)kindValue
			: SoundKind.Voice;
	}

	private static bool IsNoiseSourceAlive(Node3D source)
	{
		return source is not PlayerController player || !player.IsDead;
	}

	private static bool IsNoiseSuppressed(Node3D source)
	{
		return source is PlayerController player && (player.IsSilenced || player.IsAudioIsolated);
	}

	private static long ResolvePeerId(Node3D player)
	{
		if (player == null)
			return 0;
		return long.TryParse(player.Name, out long peerId)
			? peerId
			: player.GetMultiplayerAuthority();
	}

	private void LoadBaseline()
	{
		var cfg = new ConfigFile();
		if (cfg.Load("user://settings.cfg") == Error.Ok)
		{
			// Safety: Ensure we never load a baseline of 0
			BaselineAmplitude = Mathf.Max((float)cfg.GetValue("audio", "baseline", 0.05f), 0.001f);
			SetGdprAccepted((bool)cfg.GetValue("settings", "gdpr_accepted", false));
		}
	}
}
