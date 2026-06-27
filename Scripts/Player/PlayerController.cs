using Godot;
using LastWord;
using LastWord.Core;
using System;
using LastWord.World;

public partial class PlayerController : CharacterBody3D
{
	[Export] public float WalkSpeed = 5.0f;
	[Export] public float RunSpeed = 8.0f;
	[Export] public float JumpVelocity = 4.5f;
	[Export] public float Gravity = 9.8f;
	[Export] public float RemoteSyncInterval = 0.05f;
	[Export] public float ListenerMovementThreshold = 0.12f;
	[Export] public float RecentListenerNoiseWindow = 0.75f;
	[Export] public bool SuppressInitialLandingNoise = true;
	[Export] public float InitialLandingNoiseGraceSeconds = 0.35f;
	[Export] public float LandingNoiseMinFallSpeed = 2.0f;

	[ExportGroup("Node References")]
	[Export] public NodePath VisualsPath;
	[Export] public NodePath CameraManagerPath;

	[ExportGroup("Debug")]
	[Export] public bool ShowSoundDebugLabel = true;
	[Export] public NodePath SoundDebugLabelPath;
	[Export] public float SoundDebugHoldSeconds = 1.25f;

	[ExportGroup("Death Sequence")]
	[Export] public float DeathFadeDuration = 2.0f;
	[Export] public float DeathCardDelay = 0.5f;
	[Export] public float SpectatorTransitionDuration = 0.5f;
	[Export] public float SpectatorMoveSpeed = 5.0f;
	[Export] public NodePath DeathOverlayPath;
	[Export] public NodePath DeathCardPath;
	[Export] public NodePath DeathTitleLabelPath;
	[Export] public NodePath DeathReasonLabelPath;
	[Export] public NodePath DeathAudioPlayerPath;
	[Export] public NodePath VoiceRecorderPath;
	[Export] public NodePath CollisionShapePath;

	[Export] public float DeathPlaybackPitch = 0.6f;
	
	// Node references
	private AnimationPlayer _animationPlayer;
	private Node3D _visuals;
	private Node3D _cameraManager;
	private Label3D _soundDebugLabel;
	private ColorRect _deathOverlay;
	private Control _deathCard;
	private Label _deathTitleLabel;
	private Label _deathReasonLabel;
	private AudioStreamPlayer _deathAudioPlayer;
	private VoiceRecorder _voiceRecorder;
	private CollisionShape3D _collisionShape;

	// Run-time stats tracked for the death card (§4.5)
	private float _tokenHoldTime = 0f;
	private float _speakTime = 0f;
	private bool _hadToken = false;
	private bool _wasSpeaking = false;
	private int _currentVoiceTier = 0;
	
	// Cached animation names to avoid searching every frame
	private string _animIdle = "";
	private string _animWalk = "";
	private string _animRun = "";
	private string _animJump = "";
	private string _animDeath = "";

	// Jump state tracking
	private bool _wasAirborne = false;
	private bool _jumpAnimFinished = false;
	private bool _deathAnimFinished = false;
	private bool _landingNoiseArmed = false;
	private bool _spawnGroundingSettled = false;
	private bool _initialLandingNoiseSuppressed = false;

	// Footstep Noise Emission variables
	private float _footstepTimer = 0.0f;
	private const float WalkStepInterval = 0.5f;
	private const float RunStepInterval = 0.3f;
	private float _remoteSyncTimer = 0.0f;
	private float _landingNoiseCooldown = 0f;
	private float _initialLandingNoiseGraceTimer = 0f;
	private float _maxAirborneFallSpeed = 0f;
	private const float LandingNoiseCooldownDuration = 0.5f;
	private bool _remoteIsOnFloor = true;
	private double _lastListenerNoiseTime = -999.0;
	private int _lastListenerNoiseTier = 0;
	private SoundKind _lastListenerNoiseKind = SoundKind.Movement;
	private float _soundDebugTimer = 0f;

	private string _pendingDeathReason = "";
	private string _pendingListenerName = "";

	private GestureSystem _gestureSystem;
	private ClapAbility _clapAbility;
	private VocalSacrifice _vocalSacrifice;
	private RoleData _roleData;
	private LoudStun _loudStun;
	private StaticBubble _staticBubble;
	private MuteSilentDrop _muteSilentDrop;
	private ArchivistRegistration _archivistRegistration;
	private WitnessBurst _witnessBurst;
	private EchoReplay _echoReplay;
	private RoleNotification _roleNotification;

	public bool IsDead { get; private set; } = false;
	public bool IsInWhisperMode { get; private set; } = false;
	
	private float _stationaryNearListenerTimer = 0f;
	private MeshInstance3D _spectatorMarker;
	public bool IsInteracting => Input.IsActionPressed("interact");
	public bool IsAudioIsolated { get; private set; }
	public bool IsSilenced { get; private set; }
	public bool IsMovingForListener => !IsDead && new Vector3(Velocity.X, 0, Velocity.Z).Length() > ListenerMovementThreshold;
	public bool HasRecentListenerNoise => !IsDead && Time.GetTicksMsec() / 1000.0 - _lastListenerNoiseTime <= RecentListenerNoiseWindow;
	public int LastListenerNoiseTier => _lastListenerNoiseTier;
	public SoundKind LastListenerNoiseKind => _lastListenerNoiseKind;

	/// <summary>
	/// Raised when this player begins the listener death sequence.
	/// Other systems (HUD, game mode) can subscribe without direct coupling.
	/// </summary>
	public static event Action<PlayerController, string, string> Died;

	/// <summary>
	/// Muting ability/environment flag (Wardrobe, Silence Room, etc.).
	/// </summary>
	public void SetSilenced(bool silenced)
	{
		IsSilenced = silenced;
	}

	/// <summary>
	/// Audio isolation flag (Silence Room). When true the player neither emits
	/// nor receives external sounds.
	/// </summary>
	public void SetAudioIsolated(bool isolated)
	{
		IsAudioIsolated = isolated;
	}

	public override void _Ready()
	{
		ValidateTuningValues();
		AddToGroup("Player");

		// Parse peer authority ID from node name (e.g. if spawned named "1234567")
		if (long.TryParse(Name, out long peerId))
		{
			SetMultiplayerAuthority((int)peerId);
		}

		// Recursively find the AnimationPlayer
		_animationPlayer = FindAnimationPlayer(this);
		if (VisualsPath != null) _visuals = GetNodeOrNull<Node3D>(VisualsPath);
		if (CameraManagerPath != null) _cameraManager = GetNodeOrNull<Node3D>(CameraManagerPath);
		_soundDebugLabel = GetNodeOrNull<Label3D>(SoundDebugLabelPath);
		if (ShowSoundDebugLabel && _soundDebugLabel == null)
			GD.PushWarning("PlayerController: SoundDebugLabelPath is not assigned, so player sound debug text will not be visible.");

		_deathOverlay = GetNodeOrNull<ColorRect>(DeathOverlayPath);
		_deathCard = GetNodeOrNull<Control>(DeathCardPath);
		_deathTitleLabel = GetNodeOrNull<Label>(DeathTitleLabelPath);
		_deathReasonLabel = GetNodeOrNull<Label>(DeathReasonLabelPath);
		_deathAudioPlayer = GetNodeOrNull<AudioStreamPlayer>(DeathAudioPlayerPath);
		_voiceRecorder = GetNodeOrNull<VoiceRecorder>(VoiceRecorderPath);
		_collisionShape = GetNodeOrNull<CollisionShape3D>(CollisionShapePath);
		SubscribeVoiceManagerSignals();
		_gestureSystem = GetNodeOrNull<GestureSystem>("GestureSystem");
		_clapAbility = GetNodeOrNull<ClapAbility>("ClapAbility");
		_vocalSacrifice = GetNodeOrNull<VocalSacrifice>("VocalSacrifice");
		_roleData = GetNodeOrNull<RoleData>("RoleData");
		_loudStun = GetNodeOrNull<LoudStun>("LoudStun");
		_staticBubble = GetNodeOrNull<StaticBubble>("StaticBubble");
		_muteSilentDrop = GetNodeOrNull<MuteSilentDrop>("MuteSilentDrop");
		_archivistRegistration = GetNodeOrNull<ArchivistRegistration>("ArchivistRegistration");
		_witnessBurst = GetNodeOrNull<WitnessBurst>("WitnessBurst");
		_echoReplay = GetNodeOrNull<EchoReplay>("EchoReplay");
		_roleNotification = GetNodeOrNull<RoleNotification>("RoleNotification");

		if (_deathOverlay != null)
		{
			_deathOverlay.Color = Colors.Black;
			_deathOverlay.Modulate = new Color(1f, 1f, 1f, 0f);
			_deathOverlay.Visible = false;
		}
		if (_deathCard != null) _deathCard.Visible = false;

		UpdateSoundDebugLabel("NO SOUND", Colors.Gray, true);

		// If this node is not the local multiplayer authority, disable inputs and remove camera
		if (Multiplayer.MultiplayerPeer != null && Multiplayer.HasMultiplayerPeer() && !IsMultiplayerAuthority())
		{
			if (_cameraManager != null)
			{
				_cameraManager.QueueFree();
				_cameraManager = null;
			}
		}
		
		if (_animationPlayer != null)
		{
			string[] allAnims = _animationPlayer.GetAnimationList();
			// GD.Print("Available Animations: " + string.Join(", ", allAnims));
			
			// Map animations using fuzzy matching
			foreach (string anim in allAnims)
			{
				string lower = anim.ToLower();
				
				// Completely ignore any animations related to "carry"
				if (lower.Contains("carry")) continue;
				
				if (lower.Contains("death"))
					_animDeath = anim;
				else if (lower.Contains("defeat") && string.IsNullOrEmpty(_animDeath))
					_animDeath = anim;
				else if (lower.Contains("die") && string.IsNullOrEmpty(_animDeath))
					_animDeath = anim;
				else if (lower.Contains("idle")) _animIdle = anim;
				else if (lower.Contains("walk")) _animWalk = anim;
				else if (lower.Contains("run") || lower.Contains("sprint")) _animRun = anim;
				else if (lower.Contains("jump")) _animJump = anim;
			}
		}
		else
		{
			GD.PrintErr("ERROR: No AnimationPlayer found in the player hierarchy!");
		}
	}

	private void ValidateTuningValues()
	{
		WalkSpeed = Mathf.Max(WalkSpeed, 0.1f);
		RunSpeed = Mathf.Max(RunSpeed, WalkSpeed);
		JumpVelocity = Mathf.Max(JumpVelocity, 0f);
		Gravity = Mathf.Max(Gravity, 0f);
		RemoteSyncInterval = Mathf.Max(RemoteSyncInterval, 0.01f);
		ListenerMovementThreshold = Mathf.Max(ListenerMovementThreshold, 0f);
		RecentListenerNoiseWindow = Mathf.Max(RecentListenerNoiseWindow, 0f);
		InitialLandingNoiseGraceSeconds = Mathf.Max(InitialLandingNoiseGraceSeconds, 0f);
		LandingNoiseMinFallSpeed = Mathf.Max(LandingNoiseMinFallSpeed, 0f);
		SoundDebugHoldSeconds = Mathf.Max(SoundDebugHoldSeconds, 0f);
		DeathFadeDuration = Mathf.Max(DeathFadeDuration, 0.01f);
		DeathCardDelay = Mathf.Max(DeathCardDelay, 0f);
		SpectatorTransitionDuration = Mathf.Max(SpectatorTransitionDuration, 0f);
		SpectatorMoveSpeed = Mathf.Max(SpectatorMoveSpeed, 0f);
		DeathPlaybackPitch = Mathf.Clamp(DeathPlaybackPitch, 0.01f, 4f);
	}

	private void SubscribeVoiceManagerSignals()
	{
		if (VoiceManager.Instance == null)
			return;

		VoiceManager.Instance.TokenTransferred += OnTokenTransferred;
		VoiceManager.Instance.TierChanged += OnVoiceTierChanged;
	}

	private void OnTokenTransferred(Node3D newHolder)
	{
		_hadToken = newHolder == this;
	}

	private void OnVoiceTierChanged(int newTier)
	{
		_wasSpeaking = newTier >= (int)VoiceTier.Whisper;
		_currentVoiceTier = newTier;
	}
	
	private AnimationPlayer FindAnimationPlayer(Node node)
	{
		if (node is AnimationPlayer ap) return ap;
		foreach (Node child in node.GetChildren())
		{
			AnimationPlayer result = FindAnimationPlayer(child);
			if (result != null) return result;
		}
		return null;
	}

	public override void _Process(double delta)
	{
		// Always feed voice to objective systems (game-critical, cheap).
		FeedVoiceToObjectives(delta);

		// Sound debug label only when actively emitting noise.
		if (_soundDebugTimer > 0f)
		{
			_soundDebugTimer -= (float)delta;
			if (_soundDebugTimer <= 0f)
				UpdateSoundDebugLabel("NO SOUND", Colors.Gray, true);
			_initialLandingNoiseGraceTimer = SuppressInitialLandingNoise ? InitialLandingNoiseGraceSeconds : 0f;
		}
	}

	private void FeedVoiceToObjectives(double delta)
	{
		if (IsDead) return;

		// Strategy A.3: skip per-frame work when idle (no radio, no voice activity).
		bool hasRadio = GetNodeOrNull<Radio>("Radio") is { IsHeld: true };
		bool speaking = _currentVoiceTier >= (int)VoiceTier.Normal;
		if (!hasRadio && !speaking)
			return;

		// Phase 3 final broadcast.
		var radio = GetNodeOrNull<Radio>("Radio");
		if (radio != null && radio.IsHeld)
		{
			var broadcast = GameManager.Instance?.GetNodeOrNull<RadioBroadcast>(GameManager.Instance.RadioBroadcastPath);
			broadcast?.OnVoiceUpdate(_currentVoiceTier, delta);
		}

		// Phase 2 sequence — no STT yet, so we validate the next expected word
		// automatically when the player speaks at Normal tier or higher.
		if (_currentVoiceTier >= (int)VoiceTier.Normal)
		{
			var sequenceManager = GameManager.Instance?.GetNodeOrNull<SequenceManager>(GameManager.Instance.SequenceManagerPath);
			if (sequenceManager != null && !sequenceManager.IsLocked && !sequenceManager.IsComplete)
			{
				string nextWord = sequenceManager.CurrentSequence.Count > sequenceManager.CurrentIndex
					? sequenceManager.CurrentSequence[sequenceManager.CurrentIndex]
					: string.Empty;
				sequenceManager.OnVoiceUpdate(nextWord, _currentVoiceTier, delta);
			}
		}
	}

	public override void _PhysicsProcess(double delta)
	{
		if (IsDead)
		{
			Velocity = Vector3.Zero;
			_footstepTimer = 0f;
			_landingNoiseCooldown = 0f;

			if (IsInWhisperMode && IsMultiplayerAuthority())
			{
				HandleWhisperSpectatorMovement((float)delta);
				return;
			}

			PlayDeathAnimation();
			return;
		}

		// Accumulate death-card stats while alive.
		float dt = (float)delta;
		if (_hadToken)
			_tokenHoldTime += dt;
		if (_wasSpeaking)
			_speakTime += dt;

		// Feed vocal imprint tracker (§3.4).
		if (GameManager.Instance?.ImprintTracker != null)
			GameManager.Instance.ImprintTracker.RecordSpeaking(this, _wasSpeaking ? dt : 0f);

		// Reset playback-trap silence clock whenever this player speaks (§3.5).
		if (_wasSpeaking)
			GameManager.Instance?.Playback?.ReportSpeech();

		if (_landingNoiseCooldown > 0f)
			_landingNoiseCooldown -= (float)delta;
		if (_initialLandingNoiseGraceTimer > 0f)
			_initialLandingNoiseGraceTimer -= (float)delta;

		if (Multiplayer.MultiplayerPeer != null && Multiplayer.HasMultiplayerPeer() && !IsMultiplayerAuthority())
		{
			Vector3 horizVel = new Vector3(Velocity.X, 0, Velocity.Z);
			bool remoteMoving = horizVel.Length() > 0.1f;
			bool remoteRunning = horizVel.Length() > WalkSpeed + 0.5f;
			
			if (remoteMoving && _visuals != null)
			{
				float targetAngle = Mathf.Atan2(Velocity.X, Velocity.Z);
				Vector3 rot = _visuals.Rotation;
				rot.Y = Mathf.LerpAngle(rot.Y, targetAngle, 10f * (float)delta);
				_visuals.Rotation = rot;
			}
			
			UpdateAnimation(remoteMoving, remoteRunning, _remoteIsOnFloor);
			return;
		}

		Vector3 velocity = Velocity;

		// --- Gravity always applies regardless of menu state ---
		if (!IsOnFloor())
		{
			velocity.Y -= Gravity * (float)delta;
			_maxAirborneFallSpeed = Mathf.Max(_maxAirborneFallSpeed, -velocity.Y);
		}

		bool isRunning  = false;
		float currentSpeed = WalkSpeed;
		Vector3 direction  = Vector3.Zero;

		// --- Block all movement/jump input when menu is open ---
		if (!PauseMenu.IsOpen)
		{
			// Jump
			if (Input.IsActionPressed("move_jump") && IsOnFloor())
			{
				velocity.Y = JumpVelocity;
				_landingNoiseArmed = true;
			}

			// WASD direction
			Vector2 inputDir = Input.GetVector("move_left", "move_right", "move_forward", "move_backward");

			// Gesture keys (silent, no Token or Listener sound)
			if (IsMultiplayerAuthority() && _gestureSystem != null)
			{
				if (Input.IsActionJustPressed("gesture_z")) _gestureSystem.PlayGesture(GestureId.PointForward);
				else if (Input.IsActionJustPressed("gesture_x")) _gestureSystem.PlayGesture(GestureId.Wave);
				else if (Input.IsActionJustPressed("gesture_c")) _gestureSystem.PlayGesture(GestureId.ThumbsUp);
				else if (Input.IsActionJustPressed("gesture_v")) _gestureSystem.PlayGesture(GestureId.No);
				else if (Input.IsActionJustPressed("gesture_b")) _gestureSystem.PlayGesture(GestureId.Listen);
				else if (Input.IsActionJustPressed("gesture_n")) _gestureSystem.PlayGesture(GestureId.NoteFound);
				else if (Input.IsActionJustPressed("gesture_m")) _gestureSystem.PlayGesture(GestureId.Self);
				else if (Input.IsActionJustPressed("gesture_l")) _gestureSystem.PlayGesture(GestureId.Stop);
			}

			// Clap ability (Q) — post-Lights Out only
			if (Input.IsActionJustPressed("clap_q") && _clapAbility != null)
			{
				_clapAbility.TryClap();
			}

			// Vocal Sacrifice (G hold)
			if (_vocalSacrifice != null)
			{
				_vocalSacrifice.SetHolding(Input.IsActionPressed("sacrifice_g"));
			}

			// Role abilities (F = Loud stun, R = Static bubble)
			if (Input.IsActionJustPressed("ability_f") && _loudStun != null)
				_loudStun.TryStun();
			if (Input.IsActionJustPressed("ability_r") && _staticBubble != null)
				_staticBubble.TryDeploy();
			if (Input.IsActionJustPressed("ability_t") && _echoReplay != null)
				_echoReplay.TryDeploy();

			if (_cameraManager != null)
			{
				Vector3 camForward = -_cameraManager.GlobalTransform.Basis.Z;
				Vector3 camRight   =  _cameraManager.GlobalTransform.Basis.X;
				camForward.Y = 0; camRight.Y = 0;
				camForward = camForward.Normalized();
				camRight   = camRight.Normalized();
				direction  = (camRight * inputDir.X + camForward * -inputDir.Y).Normalized();
			}
			else
			{
				direction = (Transform.Basis * new Vector3(inputDir.X, 0, inputDir.Y)).Normalized();
			}

			isRunning    = Input.IsActionPressed("move_sprint");
			currentSpeed = isRunning ? RunSpeed : WalkSpeed;
		}

		// Apply horizontal velocity
		if (direction != Vector3.Zero)
		{
			velocity.X = direction.X * currentSpeed;
			velocity.Z = direction.Z * currentSpeed;

			if (_visuals != null)
			{
				float targetAngle     = Mathf.Atan2(direction.X, direction.Z);
				Vector3 rot           = _visuals.Rotation;
				rot.Y                 = Mathf.LerpAngle(rot.Y, targetAngle, 10f * (float)delta);
				_visuals.Rotation     = rot;
			}
		}
		else
		{
			// Decelerate to zero when no input (or menu open)
			velocity.X = Mathf.MoveToward(Velocity.X, 0, currentSpeed > 0 ? currentSpeed : WalkSpeed);
			velocity.Z = Mathf.MoveToward(Velocity.Z, 0, currentSpeed > 0 ? currentSpeed : WalkSpeed);
		}

		Velocity = velocity;
		MoveAndSlide();

		if (!_spawnGroundingSettled && IsOnFloor())
		{
			_spawnGroundingSettled = true;
			_wasAirborne = false;
			_landingNoiseArmed = false;
		}

		// Footstep Noise Emission
		if (IsOnFloor() && direction != Vector3.Zero && !PauseMenu.IsOpen)
		{
			_footstepTimer += (float)delta;
			float interval = isRunning ? RunStepInterval : WalkStepInterval;
			if (_footstepTimer >= interval)
			{
				_footstepTimer = 0f;
				EmitFootstepNoise(isRunning);
			}
		}
		else
		{
			_footstepTimer = 0f;
		}

		UpdateAnimation(direction != Vector3.Zero, isRunning);

		if (Multiplayer.MultiplayerPeer != null && Multiplayer.HasMultiplayerPeer())
		{
			_remoteSyncTimer += (float)delta;
			if (_remoteSyncTimer >= RemoteSyncInterval)
			{
				_remoteSyncTimer = 0f;
				Rpc(nameof(SyncRemoteState), GlobalPosition, Velocity, _visuals?.Rotation ?? Vector3.Zero, IsOnFloor());
			}
		}
	}
	
	private void UpdateAnimation(bool isMoving, bool isRunning, bool? isOnFloorOverride = null)
	{
		// Update visual debug label with movement state
		string stateText = "IDLE";
		Color stateColor = Colors.Yellow;

		bool isOnFloor = isOnFloorOverride ?? IsOnFloor();
		bool isAirborne = !isOnFloor;

		if (isAirborne)
		{
			stateText = "JUMPING/AIRBORNE";
			stateColor = Colors.Cyan;
		}
		else if (isMoving)
		{
			stateText = isRunning ? "RUNNING" : "WALKING";
			stateColor = isRunning ? Colors.Red : Colors.Green;
		}

		if (_wasAirborne && isOnFloor)
		{
			stateText = "LANDED";
			stateColor = Colors.Lime;
		}

		if (IsMultiplayerAuthority() && HUDManager.Instance != null)
		{
			HUDManager.Instance.UpdatePlayerState(stateText, stateColor);
		}

		if (_animationPlayer == null) return;
		if (IsDead)
		{
			PlayDeathAnimation();
			return;
		}

		// --- LANDING: just touched the ground this frame ---
		if (_wasAirborne && isOnFloor)
		{
			_jumpAnimFinished = false;
			if ((!SuppressInitialLandingNoise || _landingNoiseArmed)
				&& _initialLandingNoiseGraceTimer <= 0f
				&& _maxAirborneFallSpeed >= LandingNoiseMinFallSpeed)
			{
				EmitLandingNoise();
			}

			_landingNoiseArmed = true;
			_maxAirborneFallSpeed = 0f;
			// Resume playback in case we had paused on the last frame (guard against empty animation name)
			if (!string.IsNullOrEmpty(_animationPlayer.CurrentAnimation))
				_animationPlayer.Play(_animationPlayer.CurrentAnimation, 0f);
			
			// Immediately crossfade into the correct ground animation
			string landAnim = _animIdle;
			if (isMoving)
			{
				landAnim = isRunning && !string.IsNullOrEmpty(_animRun) ? _animRun : _animWalk;
				if (string.IsNullOrEmpty(landAnim)) landAnim = _animWalk;
			}
			if (string.IsNullOrEmpty(landAnim)) landAnim = _animIdle;

			if (!string.IsNullOrEmpty(landAnim) && _animationPlayer.HasAnimation(landAnim))
				_animationPlayer.Play(landAnim, 0.2f);

			_wasAirborne = false;
			return;
		}

		// --- AIRBORNE ---
		if (isAirborne)
		{
			_wasAirborne = true;
			if (_initialLandingNoiseGraceTimer <= 0f)
				_landingNoiseArmed = true;

			if (!string.IsNullOrEmpty(_animJump) && _animationPlayer.HasAnimation(_animJump))
			{
				// Start jump animation when we first go airborne with upward velocity
				if (_animationPlayer.CurrentAnimation != _animJump && Velocity.Y > 0.1f)
				{
					_jumpAnimFinished = false;
					_animationPlayer.Play(_animJump, 0.15f);
				}

				// Once the jump animation has played to the end, freeze on last frame
				if (!_jumpAnimFinished && _animationPlayer.CurrentAnimation == _animJump)
				{
					double remaining = _animationPlayer.CurrentAnimationLength - _animationPlayer.CurrentAnimationPosition;
					if (remaining <= 0.05)
					{
						_jumpAnimFinished = true;
						_animationPlayer.Pause(); // freeze on last frame
					}
				}
			}
			return;
		}

		// --- GROUNDED normal logic ---
		_wasAirborne = false;
		_maxAirborneFallSpeed = 0f;
		
		string targetAnim = _animIdle;
		if (isMoving)
		{
			targetAnim = isRunning && !string.IsNullOrEmpty(_animRun) ? _animRun : _animWalk;
			if (string.IsNullOrEmpty(targetAnim)) targetAnim = _animWalk;
		}
		if (string.IsNullOrEmpty(targetAnim)) targetAnim = _animIdle;

		if (!string.IsNullOrEmpty(targetAnim) && _animationPlayer.HasAnimation(targetAnim) && _animationPlayer.CurrentAnimation != targetAnim)
			_animationPlayer.Play(targetAnim, 0.25f);
	}

	private void EmitFootstepNoise(bool isRunning)
	{
		if (IsDead)
			return;

		// Only the local player authority should broadcast their footsteps to the server
		if (Multiplayer.MultiplayerPeer != null && Multiplayer.HasMultiplayerPeer() && !IsMultiplayerAuthority())
			return;

		int tier = isRunning ? 1 : 0; // Running = Tier 1 (Whisper), Walking = Tier 0 (Silent)
		string tierName = isRunning ? "Tier 1 (Whisper)" : "Tier 0 (Silent)";
		GD.Print($"{Name}: Emitted Footstep Noise — {tierName} at {GlobalPosition}");

		var footstepStream = isRunning ? AudioAssets.RandomFootstepRun() : AudioAssets.RandomFootstepWalk();
		AudioAssets.PlayOneShot3D(footstepStream, this, GlobalPosition, "SFX", pitchScale: (float)GD.RandRange(0.92, 1.08));

		ShowSoundEventDebug(SoundKind.Movement, tier);

		if (VoiceManager.Instance != null)
		{
			if (Multiplayer.MultiplayerPeer != null && Multiplayer.HasMultiplayerPeer())
			{
				// Do NOT send position — server derives it from sender node (anti-spoof).
				VoiceManager.Instance.BroadcastNoiseEvent(tier, SoundKind.Movement);
			}
			else
			{
				VoiceManager.Instance.ReportNoiseEvent(GlobalPosition, tier, SoundKind.Movement, this);
			}
		}
	}

	private void EmitLandingNoise()
	{
		if (IsDead)
			return;

		if (SuppressInitialLandingNoise && !_initialLandingNoiseSuppressed)
		{
			_initialLandingNoiseSuppressed = true;
			return;
		}

		// Guard: physics ground flicker can fire landing twice in consecutive frames at the same spot.
		if (_landingNoiseCooldown > 0f)
			return;

		// Only the local player authority should broadcast their landing to the server
		if (Multiplayer.MultiplayerPeer != null && Multiplayer.HasMultiplayerPeer() && !IsMultiplayerAuthority())
			return;

		_landingNoiseCooldown = LandingNoiseCooldownDuration;
		int tier = 2; // Landing = Tier 2 (Normal)
		GD.Print($"{Name}: Emitted Landing Noise — Tier 2 (Normal) at {GlobalPosition}");

		AudioAssets.PlayOneShot3D(AudioAssets.Landing, this, GlobalPosition, "SFX", pitchScale: (float)GD.RandRange(0.9, 1.1));

		ShowSoundEventDebug(SoundKind.Landing, tier);

		if (VoiceManager.Instance != null)
		{
			if (Multiplayer.MultiplayerPeer != null && Multiplayer.HasMultiplayerPeer())
			{
				// Do NOT send position — server derives it from sender node (anti-spoof).
				VoiceManager.Instance.BroadcastNoiseEvent(tier, SoundKind.Landing);
			}
			else
			{
				VoiceManager.Instance.ReportNoiseEvent(GlobalPosition, tier, SoundKind.Landing, this);
			}
		}
	}

	public void NotifyListenerNoise(int tier, SoundKind kind)
	{
		if (IsDead)
			return;

		_lastListenerNoiseTime = Time.GetTicksMsec() / 1000.0;
		_lastListenerNoiseTier = Mathf.Clamp(tier, 0, 3);
		_lastListenerNoiseKind = kind;
		ShowSoundEventDebug(kind, _lastListenerNoiseTier);
	}

	private void ShowSoundEventDebug(SoundKind kind, int tier)
	{
		_soundDebugTimer = SoundDebugHoldSeconds;
		UpdateSoundDebugLabel($"{kind.ToString().ToUpper()} T{tier}", GetSoundDebugColor(tier, kind), false);
	}

	private void UpdateSoundDebugLabel(string text, Color color, bool isIdle)
	{
		if (_soundDebugLabel == null)
			return;

		_soundDebugLabel.Visible = ShowSoundDebugLabel;
		_soundDebugLabel.Text = isIdle ? text : $"NOISE\n{text}";
		_soundDebugLabel.Modulate = color;
	}

	private static Color GetSoundDebugColor(int tier, SoundKind kind)
	{
		if (kind != SoundKind.Voice)
			return Colors.LightBlue;

		return tier switch
		{
			1 => Colors.Yellow,
			2 => Colors.Orange,
			3 => Colors.Red,
			_ => Colors.LightGreen
		};
	}

	public void KillByListener(Node3D listener, string reason)
	{
		if (IsDead)
			return;

		string listenerName = listener?.Name ?? "Listener";
		ApplyListenerDeath(reason, listenerName);

		if (Multiplayer.MultiplayerPeer != null && Multiplayer.HasMultiplayerPeer() && Multiplayer.IsServer())
		{
			Rpc(nameof(SyncListenerDeath), reason, listenerName);
		}
	}

	[Rpc(MultiplayerApi.RpcMode.AnyPeer, CallLocal = false)]
	private void SyncListenerDeath(string reason, string listenerName)
	{
		if (Multiplayer.MultiplayerPeer != null
			&& Multiplayer.HasMultiplayerPeer()
			&& Multiplayer.GetRemoteSenderId() != NetworkManager.ServerPeerId)
		{
			return;
		}

		ApplyListenerDeath(reason, listenerName);
	}

	private void ApplyListenerDeath(string reason, string listenerName)
	{
		if (IsDead)
			return;

		IsDead = true;
		IsInWhisperMode = false;
		_pendingDeathReason = reason;
		_pendingListenerName = listenerName;
		Velocity = Vector3.Zero;
		_footstepTimer = 0f;
		_deathAnimFinished = false;
		PlayDeathAnimation();
		PlayDeathVoicePlayback();
		GD.Print($"[PlayerController] {Name} killed by {listenerName}: {reason}");

		// Achievement hook: Last Breath — you killed the Listener via Vocal Sacrifice.
		if (string.Equals(reason, "sacrifice", StringComparison.OrdinalIgnoreCase))
			AchievementManager.Instance?.Unlock(AchievementManager.Id.LastBreath);

		// Notify other systems via event (keeps gameplay code decoupled from UI).
		Died?.Invoke(this, reason, listenerName);

		// Mark imprint profile as dead so it decays instead of persisting.
		GameManager.Instance?.ImprintTracker?.MarkDead(this);

		// Role death notification (§5).
		if (_roleData != null && _roleData.HasRole)
		{
			string roleName = _roleData.Role.ToString().ToUpper();
			_roleNotification?.Show($"{roleName} LOST");
			RoleNotification.ShowFor(this, $"{roleName} ability lost on death.");
			if (_roleData.IsWitness && _witnessBurst != null)
				_witnessBurst.OnWitnessDied(this);
		}

		if (IsMultiplayerAuthority())
		{
			FadeToBlack();
		}
	}

	private void FadeToBlack()
	{
		if (_deathOverlay == null)
		{
			CallDeferred(nameof(ShowDeathCard));
			CallDeferred(nameof(EnterWhisperMode));
			return;
		}

		_deathOverlay.Visible = true;
		_deathOverlay.Modulate = new Color(1f, 1f, 1f, 0f);

		var tween = CreateTween();
		tween.SetTrans(Tween.TransitionType.Linear);
		tween.SetEase(Tween.EaseType.InOut);
		tween.TweenProperty(_deathOverlay, "modulate", new Color(1f, 1f, 1f, 1f), DeathFadeDuration);
		tween.TweenCallback(Callable.From(OnFadeComplete));
	}

	private void OnFadeComplete()
	{
		if (DeathCardDelay > 0f)
		{
			var timer = GetTree().CreateTimer(DeathCardDelay);
			timer.Timeout += () =>
			{
				ShowDeathCard();
				var transitionTimer = GetTree().CreateTimer(SpectatorTransitionDuration);
				transitionTimer.Timeout += EnterWhisperMode;
			};
		}
		else
		{
			ShowDeathCard();
			CallDeferred(nameof(EnterWhisperMode));
		}
	}

	private void PlayDeathVoicePlayback()
	{
		if (_deathAudioPlayer == null || _voiceRecorder == null)
			return;

		AudioStreamWav clip = _voiceRecorder.GetRecentRecording(10f);
		if (clip == null)
			return;

		_deathAudioPlayer.Stream = clip;
		_deathAudioPlayer.PitchScale = DeathPlaybackPitch;
		_deathAudioPlayer.Play();
		GD.Print($"[PlayerController] Playing death voice playback at pitch {DeathPlaybackPitch:P0}.");
	}

	private void ShowDeathCard()
	{
		if (_deathCard != null)
		{
			_deathCard.Visible = true;
			if (_deathTitleLabel != null)
				_deathTitleLabel.Text = "CAUGHT BY THE LISTENER";
			if (_deathReasonLabel != null)
			{
				(string severity, string tip) = ComputeDeathSeverityAndTip();
				_deathReasonLabel.Text =
					$"Cause: {_pendingDeathReason}\n" +
					$"Severity: {severity}\n" +
					$"Token held: {FormatTime(_tokenHoldTime)}\n" +
					$"Time speaking: {FormatTime(_speakTime)}\n" +
					$"Tip: {tip}";
			}
		}
	}

	private (string severity, string tip) ComputeDeathSeverityAndTip()
	{
		bool wasLoud = _speakTime > 5f && _pendingDeathReason.Contains("noise", StringComparison.OrdinalIgnoreCase);
		bool wasHoldingToken = _tokenHoldTime > 10f;

		if (wasLoud && wasHoldingToken)
			return ("CRITICAL", "You were loud AND held the Token. Pass it faster next time.");
		if (wasHoldingToken)
			return ("HIGH", "The Token makes you a priority target. Keep moving or pass it.");
		if (wasLoud)
			return ("MODERATE", "Noise draws the Listener. Crouch-walk and whisper.");
		return ("LOW", "Even small sounds add up. Stay still when the Listener is near.");
	}

	private static string FormatTime(float seconds)
	{
		int totalSeconds = Mathf.RoundToInt(seconds);
		int minutes = totalSeconds / 60;
		int secs = totalSeconds % 60;
		return $"{minutes}:{secs:D2}";
	}

	private void EnterWhisperMode()
	{
		if (IsInWhisperMode)
			return;

		IsInWhisperMode = true;

		if (_visuals != null)
			_visuals.Visible = false;

		if (_collisionShape != null)
			_collisionShape.Disabled = true;

		if (IsMultiplayerAuthority() && HUDManager.Instance != null)
		{
			HUDManager.Instance.UpdatePlayerState("WHISPER SPECTATOR", Colors.Gray);
		}

		GD.Print($"[PlayerController] {Name} entered Whisper spectator mode.");
	}

	private void HandleWhisperSpectatorMovement(float delta)
	{
		if (_cameraManager == null)
			return;

		Vector3 input = Vector3.Zero;

		if (!PauseMenu.IsOpen)
		{
			Vector2 inputDir = Input.GetVector("move_left", "move_right", "move_forward", "move_backward");
			Vector3 camForward = -_cameraManager.GlobalTransform.Basis.Z;
			Vector3 camRight = _cameraManager.GlobalTransform.Basis.X;
			camForward.Y = 0f;
			camRight.Y = 0f;
			camForward = camForward.Normalized();
			camRight = camRight.Normalized();

			input += camForward * -inputDir.Y;
			input += camRight * inputDir.X;

			if (Input.IsActionPressed("move_jump"))
				input += Vector3.Up;
			if (Input.IsKeyPressed(Key.Ctrl))
				input += Vector3.Down;
		}

		input = input.Normalized();
		
		bool isStationary = input.LengthSquared() < 0.01f;
		var listenerNodes = GetTree().GetNodesInGroup("Listener");
		float closestListenerDist = float.MaxValue;
		Node3D closestListener = null;

		foreach (var node in listenerNodes)
		{
			if (node is Node3D listener)
			{
				float dist = GlobalPosition.DistanceTo(listener.GlobalPosition);
				if (dist < closestListenerDist)
				{
					closestListenerDist = dist;
					closestListener = listener;
				}
			}
		}

		if (closestListener != null && closestListenerDist < 2.1f)
		{
			Vector3 pushDir = (GlobalPosition - closestListener.GlobalPosition).Normalized();
			if (pushDir.LengthSquared() < 0.01f) pushDir = Vector3.Up;

			if (isStationary)
			{
				_stationaryNearListenerTimer += (float)delta;
				if (_stationaryNearListenerTimer >= 5.0f)
				{
					GlobalPosition = closestListener.GlobalPosition + pushDir * 3.0f;
					_stationaryNearListenerTimer = 0f;
				}
				else if (closestListenerDist < 2.0f)
				{
					GlobalPosition = closestListener.GlobalPosition + pushDir * 2.0f;
				}
			}
			else
			{
				_stationaryNearListenerTimer = 0f;
				if (closestListenerDist < 2.0f)
				{
					GlobalPosition = closestListener.GlobalPosition + pushDir * 2.0f;
				}
			}
		}
		else
		{
			_stationaryNearListenerTimer = 0f;
		}

		GlobalPosition += input * SpectatorMoveSpeed * (float)delta;
		Velocity = Vector3.Zero;

		if (!PauseMenu.IsOpen && Input.IsPhysicalKeyPressed(Key.J))
		{
			if (_spectatorMarker == null)
			{
				_spectatorMarker = new MeshInstance3D();
				var mesh = new SphereMesh() { Radius = 0.2f, Height = 0.4f };
				_spectatorMarker.Mesh = mesh;
				var mat = new StandardMaterial3D() { AlbedoColor = Colors.Red, ShadingMode = BaseMaterial3D.ShadingModeEnum.Unshaded };
				_spectatorMarker.MaterialOverride = mat;
				GetTree().Root.AddChild(_spectatorMarker);
			}
			_spectatorMarker.GlobalPosition = GlobalPosition;
			
			// Timer to remove marker after 15s
			var timer = _spectatorMarker.GetNodeOrNull<Timer>("DespawnTimer");
			if (timer == null)
			{
				timer = new Timer();
				timer.Name = "DespawnTimer";
				timer.OneShot = true;
				timer.WaitTime = 15.0f;
				timer.Timeout += () => { if (_spectatorMarker != null) _spectatorMarker.QueueFree(); _spectatorMarker = null; };
				_spectatorMarker.AddChild(timer);
			}
			timer.Start();

			if (Multiplayer.MultiplayerPeer != null && Multiplayer.HasMultiplayerPeer() && Multiplayer.IsServer())
			{
				Rpc(nameof(SyncSpectatorMarker), GlobalPosition);
			}
			else if (Multiplayer.MultiplayerPeer != null && Multiplayer.HasMultiplayerPeer())
			{
				RpcId(NetworkManager.ServerPeerId, nameof(RequestSpectatorMarker), GlobalPosition);
			}
		}

		if (Multiplayer.MultiplayerPeer != null && Multiplayer.HasMultiplayerPeer())
		{
			_remoteSyncTimer += delta;
			if (_remoteSyncTimer >= RemoteSyncInterval)
			{
				_remoteSyncTimer = 0f;
				Rpc(nameof(SyncRemoteState), GlobalPosition, Velocity, _visuals?.Rotation ?? Vector3.Zero, IsOnFloor());
			}
		}
	}

	private void PlayDeathAnimation()
	{
		if (_animationPlayer == null || string.IsNullOrEmpty(_animDeath) || !_animationPlayer.HasAnimation(_animDeath))
			return;

		if (_deathAnimFinished)
			return;

		if (_animationPlayer.CurrentAnimation != _animDeath)
		{
			_animationPlayer.Play(_animDeath, 0.1f);
			return;
		}

		double remaining = _animationPlayer.CurrentAnimationLength - _animationPlayer.CurrentAnimationPosition;
		if (remaining <= 0.05)
		{
			_animationPlayer.Seek(_animationPlayer.CurrentAnimationLength, true);
			_animationPlayer.Pause();
			_deathAnimFinished = true;
		}
	}

	[Rpc(MultiplayerApi.RpcMode.Authority, CallLocal = false, TransferMode = MultiplayerPeer.TransferModeEnum.Unreliable)]
	private void SyncRemoteState(Vector3 position, Vector3 velocity, Vector3 visualsRotation, bool isOnFloor)
	{
		if (Multiplayer.MultiplayerPeer == null || !Multiplayer.HasMultiplayerPeer() || IsMultiplayerAuthority())
			return;

		long senderId = Multiplayer.GetRemoteSenderId();
		if (senderId != 0 && senderId != GetMultiplayerAuthority())
			return;

		GlobalPosition = position;
		Velocity = velocity;
		_remoteIsOnFloor = isOnFloor;
		if (_visuals != null)
			_visuals.Rotation = visualsRotation;
	}

	[Rpc(MultiplayerApi.RpcMode.AnyPeer, CallLocal = false)]
	private void RequestSpectatorMarker(Vector3 pos)
	{
		if (!Multiplayer.IsServer()) return;
		Rpc(nameof(SyncSpectatorMarker), pos);
		SyncSpectatorMarker(pos);
	}

	[Rpc(MultiplayerApi.RpcMode.Authority, CallLocal = false)]
	private void SyncSpectatorMarker(Vector3 pos)
	{
		if (_spectatorMarker == null)
		{
			_spectatorMarker = new MeshInstance3D();
			var mesh = new SphereMesh() { Radius = 0.2f, Height = 0.4f };
			_spectatorMarker.Mesh = mesh;
			var mat = new StandardMaterial3D() { AlbedoColor = Colors.Red, ShadingMode = BaseMaterial3D.ShadingModeEnum.Unshaded };
			_spectatorMarker.MaterialOverride = mat;
			GetTree().Root.AddChild(_spectatorMarker);
		}
		_spectatorMarker.GlobalPosition = pos;
		
		var timer = _spectatorMarker.GetNodeOrNull<Timer>("DespawnTimer");
		if (timer == null)
		{
			timer = new Timer();
			timer.Name = "DespawnTimer";
			timer.OneShot = true;
			timer.WaitTime = 15.0f;
			timer.Timeout += () => { if (_spectatorMarker != null) _spectatorMarker.QueueFree(); _spectatorMarker = null; };
			_spectatorMarker.AddChild(timer);
		}
		timer.Start();
	}
}

