using Godot;
using System;

public partial class PlayerController : CharacterBody3D
{
	[Export] public float WalkSpeed = 5.0f;
	[Export] public float RunSpeed = 8.0f;
	[Export] public float JumpVelocity = 4.5f;
	[Export] public float Gravity = 9.8f;
	
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
		// Recursively find the AnimationPlayer
		_animationPlayer = FindAnimationPlayer(this);
		_visuals = GetNodeOrNull<Node3D>("BaseCharacter");
		_cameraManager = GetNodeOrNull<Node3D>("CameraManager");
		
		if (_animationPlayer != null)
		{
			string[] allAnims = _animationPlayer.GetAnimationList();
			GD.Print("Available Animations: " + string.Join(", ", allAnims));
			
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
		Vector3 velocity = Velocity;

		// Add the gravity
		if (!IsOnFloor())
		{
			velocity.Y -= Gravity * (float)delta;
		}

		// Handle Jump (Spacebar)
		if (Input.IsKeyPressed(Key.Space) && IsOnFloor())
		{
			velocity.Y = JumpVelocity;
		}

		// Calculate Input Direction from WASD directly
		Vector2 inputDir = Vector2.Zero;
		if (Input.IsKeyPressed(Key.W)) inputDir.Y -= 1;
		if (Input.IsKeyPressed(Key.S)) inputDir.Y += 1;
		if (Input.IsKeyPressed(Key.A)) inputDir.X -= 1;
		if (Input.IsKeyPressed(Key.D)) inputDir.X += 1;
		
		inputDir = inputDir.Normalized();

		Vector3 direction = Vector3.Zero;
		if (_cameraManager != null)
		{
			Vector3 camForward = -_cameraManager.GlobalTransform.Basis.Z;
			Vector3 camRight = _cameraManager.GlobalTransform.Basis.X;
			
			camForward.Y = 0;
			camRight.Y = 0;
			camForward = camForward.Normalized();
			camRight = camRight.Normalized();
			
			direction = (camRight * inputDir.X + camForward * -inputDir.Y).Normalized();
		}
		else
		{
			direction = (Transform.Basis * new Vector3(inputDir.X, 0, inputDir.Y)).Normalized();
		}
		
		bool isRunning = Input.IsKeyPressed(Key.Shift);
		float currentSpeed = isRunning ? RunSpeed : WalkSpeed;

		if (direction != Vector3.Zero)
		{
			velocity.X = direction.X * currentSpeed;
			velocity.Z = direction.Z * currentSpeed;
			
			// Rotate the character to face the direction of movement
			if (_visuals != null)
			{
				float targetAngle = Mathf.Atan2(direction.X, direction.Z);
				Vector3 currentRotation = _visuals.Rotation;
				currentRotation.Y = Mathf.LerpAngle(currentRotation.Y, targetAngle, 10.0f * (float)delta);
				_visuals.Rotation = currentRotation;
			}
		}
		else
		{
			velocity.X = Mathf.MoveToward(Velocity.X, 0, currentSpeed);
			velocity.Z = Mathf.MoveToward(Velocity.Z, 0, currentSpeed);
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
