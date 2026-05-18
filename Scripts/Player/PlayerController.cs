using Godot;
using System;

public partial class PlayerController : CharacterBody3D
{
	[Export] public float WalkSpeed = 5.0f;
	[Export] public float RunSpeed = 8.0f;
	[Export] public float JumpVelocity = 4.5f;
	[Export] public float Gravity = 9.8f;

	[ExportGroup("Node References")]
	[Export] public NodePath VisualsPath = "BaseCharacter";
	[Export] public NodePath CameraManagerPath = "CameraManager";
	
	// Node references
	private AnimationPlayer _animationPlayer;
	private Node3D _visuals;
	private Node3D _cameraManager;
	
	// Cached animation names to avoid searching every frame
	private string _animIdle = "";
	private string _animWalk = "";
	private string _animRun = "";
	private string _animJump = "";

	// Jump state tracking
	private bool _wasAirborne = false;
	private bool _jumpAnimFinished = false;

	public override void _Ready()
	{
		// Parse peer authority ID from node name (e.g. if spawned named "1234567")
		if (long.TryParse(Name, out long peerId))
		{
			SetMultiplayerAuthority((int)peerId);
		}

		// Recursively find the AnimationPlayer
		_animationPlayer = FindAnimationPlayer(this);
		if (VisualsPath != null) _visuals = GetNodeOrNull<Node3D>(VisualsPath);
		if (CameraManagerPath != null) _cameraManager = GetNodeOrNull<Node3D>(CameraManagerPath);

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
				
				if (lower.Contains("idle")) _animIdle = anim;
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

	public override void _PhysicsProcess(double delta)
	{
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
			
			UpdateAnimation(remoteMoving, remoteRunning);
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

		UpdateAnimation(direction != Vector3.Zero, isRunning);
	}
	
	private void UpdateAnimation(bool isMoving, bool isRunning)
	{
		if (_animationPlayer == null) return;

		bool isOnFloor = IsOnFloor();
		bool isAirborne = !isOnFloor;

		// --- LANDING: just touched the ground this frame ---
		if (_wasAirborne && isOnFloor)
		{
			_jumpAnimFinished = false;
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
}
