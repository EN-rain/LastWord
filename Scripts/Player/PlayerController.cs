using Godot;
using System;

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

	[ExportGroup("Node References")]
	[Export] public NodePath VisualsPath;
	[Export] public NodePath CameraManagerPath;

	[ExportGroup("Debug")]
	[Export] public bool ShowSoundDebugLabel = true;
	[Export] public NodePath SoundDebugLabelPath;
	[Export] public float SoundDebugHoldSeconds = 1.25f;
	
	// Node references
	private AnimationPlayer _animationPlayer;
	private Node3D _visuals;
	private Node3D _cameraManager;
	private Label3D _soundDebugLabel;
	
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

	// Footstep Noise Emission variables
	private float _footstepTimer = 0.0f;
	private const float WalkStepInterval = 0.5f;
	private const float RunStepInterval = 0.3f;
	private float _remoteSyncTimer = 0.0f;
	private float _landingNoiseCooldown = 0f;
	private const float LandingNoiseCooldownDuration = 0.5f;
	private bool _remoteIsOnFloor = true;
	private double _lastListenerNoiseTime = -999.0;
	private int _lastListenerNoiseTier = 0;
	private SoundKind _lastListenerNoiseKind = SoundKind.Movement;
	private float _soundDebugTimer = 0f;

	public bool IsDead { get; private set; } = false;
	public bool IsMovingForListener => !IsDead && new Vector3(Velocity.X, 0, Velocity.Z).Length() > ListenerMovementThreshold;
	public bool HasRecentListenerNoise => !IsDead && Time.GetTicksMsec() / 1000.0 - _lastListenerNoiseTime <= RecentListenerNoiseWindow;
	public int LastListenerNoiseTier => _lastListenerNoiseTier;
	public SoundKind LastListenerNoiseKind => _lastListenerNoiseKind;

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
		SoundDebugHoldSeconds = Mathf.Max(SoundDebugHoldSeconds, 0f);
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
		if (_soundDebugTimer <= 0f)
			return;

		_soundDebugTimer -= (float)delta;
		if (_soundDebugTimer <= 0f)
			UpdateSoundDebugLabel("NO SOUND", Colors.Gray, true);
	}

	public override void _PhysicsProcess(double delta)
	{
		if (IsDead)
		{
			Velocity = Vector3.Zero;
			_footstepTimer = 0f;
			_landingNoiseCooldown = 0f;
			PlayDeathAnimation();
			return;
		}

		if (_landingNoiseCooldown > 0f)
			_landingNoiseCooldown -= (float)delta;

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
			velocity.Y -= Gravity * (float)delta;

		bool isRunning  = false;
		float currentSpeed = WalkSpeed;
		Vector3 direction  = Vector3.Zero;

		// --- Block all movement/jump input when menu is open ---
		if (!PauseMenu.IsOpen)
		{
			// Jump
			if (Input.IsActionPressed("move_jump") && IsOnFloor())
				velocity.Y = JumpVelocity;

			// WASD direction
			Vector2 inputDir = Input.GetVector("move_left", "move_right", "move_forward", "move_backward");

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
			if (!SuppressInitialLandingNoise || _landingNoiseArmed)
				EmitLandingNoise();

			_landingNoiseArmed = true;
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
		_landingNoiseArmed = true;
		
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

		// Guard: physics ground flicker can fire landing twice in consecutive frames at the same spot.
		if (_landingNoiseCooldown > 0f)
			return;

		// Only the local player authority should broadcast their landing to the server
		if (Multiplayer.MultiplayerPeer != null && Multiplayer.HasMultiplayerPeer() && !IsMultiplayerAuthority())
			return;

		_landingNoiseCooldown = LandingNoiseCooldownDuration;
		int tier = 2; // Landing = Tier 2 (Normal)
		GD.Print($"{Name}: Emitted Landing Noise — Tier 2 (Normal) at {GlobalPosition}");

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
		Velocity = Vector3.Zero;
		_footstepTimer = 0f;
		_deathAnimFinished = false;
		PlayDeathAnimation();
		GD.Print($"[PlayerController] {Name} killed by {listenerName}: {reason}");

		if (IsMultiplayerAuthority() && HUDManager.Instance != null)
		{
			HUDManager.Instance.UpdatePlayerState($"DEAD - {reason}", Colors.DeepPink);
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
}
