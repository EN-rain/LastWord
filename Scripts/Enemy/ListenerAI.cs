using Godot;
using System;
using System.Collections.Generic;
using System.Linq;

public partial class ListenerAI : CharacterBody3D
{
	[Export] public float WalkSpeed = 3.0f;
	[Export] public float JogSpeed = 3.9f;     // 1.3x Walk
	[Export] public float SprintSpeed = 5.4f;  // 1.8x Walk
	[Export] public float RotationSpeed = 10.0f;
	[Export] public float Gravity = 9.8f;
	[Export] public float RemoteSyncInterval = 0.05f;

	[ExportGroup("Audio Detection")]
	[Export] public float SubWhisperRadius = 4.0f;
	[Export] public float WhisperRadius = 8.0f;
	[Export] public float NormalRadius = 20.0f;
	[Export] public float AlertSilenceTimeout = 5.0f;
	[Export] public float WhisperDecayAdd = 2.0f;
	[Export] public float WhisperDecayCap = 6.0f;
	[Export] public float FrenzyDuration = 12.0f;

	public enum AIState { Idle, Alerted, Hunting, Frenzy }
	protected AIState _currentState = AIState.Idle;

	protected float _alertedSilenceTimer = 0f;
	protected float _whisperPauseDecay = 0f;
	protected float _frenzyTimer = 0f;
	protected Vector3 _lastHeardLocation;

	[ExportGroup("Proximity Ranges")]
	[Export] public float NoticeRange = 15.0f;
	[Export] public float AlertRange = 10.0f;
	[Export] public float AttackRange = 1.8f;
	[Export] public float AttackCooldown = 2.0f;

	[ExportGroup("Line of Sight")]
	// Physics layers the LoS raycast tests against (set to your Environment/World layer in the editor).
	[Export(PropertyHint.Layers3DPhysics)] public uint CollisionLayers = 1;

	private float _attackTimer = 0.0f;
	private float _remoteSyncTimer = 0.0f;
	private Label3D _stateLabel;

	protected NavigationAgent3D _navAgent;
	protected Node3D _visuals;
	protected MeshInstance3D _leftEye;
	protected MeshInstance3D _rightEye;
	protected AudioStreamPlayer3D _humPlayer;

	protected List<Node3D> _waypoints = new();
	protected int _currentWaypointIndex = 0;
	private bool _patrolInitialized = false;

	public override void _Ready()
	{
		_navAgent = GetNode<NavigationAgent3D>("NavAgent");
		_visuals = GetNode<Node3D>("Visuals");
		_leftEye = GetNodeOrNull<MeshInstance3D>("Visuals/Eyes/LeftEye");
		_rightEye = GetNodeOrNull<MeshInstance3D>("Visuals/Eyes/RightEye");
		_humPlayer = GetNodeOrNull<AudioStreamPlayer3D>("HumPlayer");

		// Cache patrol waypoints
		var waypointNodes = GetTree().GetNodesInGroup("Waypoint");
		foreach (var node in waypointNodes)
		{
			if (node is Node3D n) _waypoints.Add(n);
		}
		_waypoints = _waypoints.OrderBy(node => node.Name.ToString()).ToList();

		// Initialize State/Event Pop-up Label above Listener's head
		_stateLabel = new Label3D();
		_stateLabel.Billboard = BaseMaterial3D.BillboardModeEnum.Enabled; // Billboard mode so it faces the camera
		_stateLabel.Position = new Vector3(0, 2.5f, 0); // Put it above the head
		_stateLabel.FontSize = 48; // Big and highly visible
		_stateLabel.OutlineSize = 12; // Good visibility outline
		_stateLabel.Modulate = Colors.Green;
		_stateLabel.Text = "IDLE";
		AddChild(_stateLabel);
		_stateLabel.Visible = false;

		CallDeferred(nameof(InitializePatrol));
	}

	public override void _PhysicsProcess(double delta)
	{
		if (!Multiplayer.IsServer() && Multiplayer.MultiplayerPeer != null && Multiplayer.HasMultiplayerPeer())
			return; // Server-authoritative AI execution

		CheckProximitySensors((float)delta);
		UpdateStateLogic((float)delta);

		if (Multiplayer.MultiplayerPeer != null && Multiplayer.HasMultiplayerPeer())
		{
			_remoteSyncTimer += (float)delta;
			if (_remoteSyncTimer >= RemoteSyncInterval)
			{
				_remoteSyncTimer = 0f;
				Rpc(nameof(SyncRemoteState), GlobalPosition, Velocity, Rotation);
			}
		}
	}

	public override void _Process(double delta)
	{
		if (_currentState == AIState.Frenzy)
		{
			float scale = 1.0f + 0.3f * Mathf.Sin(Time.GetTicksMsec() / 50.0f);
			if (_leftEye != null) _leftEye.Scale = new Vector3(scale, scale, scale);
			if (_rightEye != null) _rightEye.Scale = new Vector3(scale, scale, scale);
		}
		else
		{
			if (_leftEye != null && _leftEye.Scale.X != 1.0f) _leftEye.Scale = Vector3.One;
			if (_rightEye != null && _rightEye.Scale.X != 1.0f) _rightEye.Scale = Vector3.One;
		}
	}

	protected void UpdateStateLogic(float delta)
	{
		switch (_currentState)
		{
			case AIState.Idle:
				if (!_patrolInitialized)
				{
					InitializePatrol();
				}
				if (_navAgent.IsNavigationFinished()) SelectNextWaypoint();
				MoveTowardsTarget(delta, WalkSpeed);
				break;

			case AIState.Alerted:
				if (_whisperPauseDecay > 0)
				{
					_whisperPauseDecay -= delta;
				}
				else
				{
					_alertedSilenceTimer += delta;
					if (_alertedSilenceTimer >= AlertSilenceTimeout)
					{
						TransitionState(AIState.Idle);
					}
				}
				// Stands still while alerted
				MoveTowardsTarget(delta, 0f); 
				break;

			case AIState.Hunting:
				if (VoiceManager.Instance != null && VoiceManager.Instance.TokenHolder != null)
				{
					_navAgent.TargetPosition = VoiceManager.Instance.TokenHolder.GlobalPosition;
				}
				else
				{
					_navAgent.TargetPosition = _lastHeardLocation;
				}
				MoveTowardsTarget(delta, JogSpeed);
				if (_navAgent.IsNavigationFinished())
				{
					GD.Print("[ListenerAI] Hunting navigation finished. Transitioning to Alerted.");
					TransitionState(AIState.Alerted);
				}
				break;

			case AIState.Frenzy:
				_frenzyTimer -= delta;
				if (VoiceManager.Instance != null && VoiceManager.Instance.TokenHolder != null)
				{
					_navAgent.TargetPosition = VoiceManager.Instance.TokenHolder.GlobalPosition;
				}
				else
				{
					_navAgent.TargetPosition = _lastHeardLocation;
				}
				MoveTowardsTarget(delta, SprintSpeed);
				if (_frenzyTimer <= 0)
				{
					GD.Print("[ListenerAI] Frenzy timer expired. Transitioning to Alerted.");
					TransitionState(AIState.Alerted);
				}
				break;
		}
	}

	// Called by VoiceManager when a player makes noise
	public void HearNoise(Vector3 origin, int tier)
	{
		if (!Multiplayer.IsServer()) return;

		float distance = GlobalPosition.DistanceTo(origin);
		_lastHeardLocation = origin;

		// Set temporary label text to show noise heard!
		if (_stateLabel != null)
		{
			_stateLabel.Text = $"{_currentState.ToString().ToUpper()}\nHEARD TIER {tier} ({distance:F1}m)";
		}

		switch (tier)
		{
			case 0: // Environmental / Walking Footstep
				if (distance <= SubWhisperRadius)
				{
					GD.Print($"[ListenerAI] Heard walking footstep at {origin} (Distance: {distance:F2}m)");
					if (_currentState == AIState.Idle)
					{
						TransitionState(AIState.Alerted);
					}
					else if (_currentState == AIState.Alerted || _currentState == AIState.Hunting)
					{
						// Actively investigate/move to walking noise location
						_navAgent.TargetPosition = origin;
					}
				}
				break;

			case 1: // Whisper / Running Footstep
				if (distance <= WhisperRadius)
				{
					GD.Print($"[ListenerAI] Heard running footstep at {origin} (Distance: {distance:F2}m)");
					_whisperPauseDecay = Mathf.Min(_whisperPauseDecay + WhisperDecayAdd, WhisperDecayCap);
					LookAtOrigin(origin); // Snap head-turn

					if (_currentState == AIState.Idle)
					{
						TransitionState(AIState.Alerted);
					}
					
					// Actively chase running footsteps if already alerted or hunting!
					if (_currentState == AIState.Alerted || _currentState == AIState.Hunting)
					{
						TransitionState(AIState.Hunting);
						_navAgent.TargetPosition = origin;
					}
				}
				break;

			case 2: // Normal Talking
				if (distance <= NormalRadius)
				{
					GD.Print($"[ListenerAI] Heard talking noise at {origin} (Distance: {distance:F2}m). Initiating chase!");
					LookAtOrigin(origin);
					_navAgent.TargetPosition = origin;
					if (_currentState != AIState.Frenzy)
					{
						TransitionState(AIState.Hunting);
					}
				}
				break;

			case 3: // Scream
				GD.Print($"[ListenerAI] Heard scream noise at {origin}! Entering Frenzy sprint!");
				LookAtOrigin(origin);
				_navAgent.TargetPosition = origin;
				TransitionState(AIState.Frenzy);
				_frenzyTimer = FrenzyDuration;
				break;
		}
	}

	protected void TransitionState(AIState newState)
	{
		if (_currentState == newState) return;
		GD.Print($"[ListenerAI] State transitioning from {_currentState} to {newState}");
		_currentState = newState;

		_alertedSilenceTimer = 0f;
		_whisperPauseDecay = 0f;

		Color eyeColor = Colors.Black;

		switch (newState)
		{
			case AIState.Idle: eyeColor = new Color(0.2f, 0.2f, 0.2f); break; // Dim White
			case AIState.Alerted: eyeColor = new Color(1.0f, 0.5f, 0.0f); break; // Amber
			case AIState.Hunting: eyeColor = new Color(1.0f, 0.0f, 0.0f); break; // Red
			case AIState.Frenzy: eyeColor = Colors.White; break;
		}

		Rpc(nameof(SyncEyeColor), eyeColor);
		UpdateStateLabel(newState);
	}

	[Rpc(MultiplayerApi.RpcMode.AnyPeer, CallLocal = true)]
	protected void SyncEyeColor(Color color)
	{
		// Simple implementation for eyes materials
		if (_leftEye?.Mesh is PrimitiveMesh lMesh)
		{
			if (lMesh.Material is StandardMaterial3D lMat) lMat.AlbedoColor = color;
		}
		if (_rightEye?.Mesh is PrimitiveMesh rMesh)
		{
			if (rMesh.Material is StandardMaterial3D rMat) rMat.AlbedoColor = color;
		}
	}

	protected void LookAtOrigin(Vector3 origin)
	{
		// Simple instant rotation towards the noise
		var lookTarget = new Vector3(origin.X, GlobalPosition.Y, origin.Z);
		if (GlobalPosition.DistanceTo(lookTarget) > 0.1f)
		{
			LookAt(lookTarget, Vector3.Up);
		}
	}

	protected void MoveTowardsTarget(float delta, float speed)
	{
		Vector3 nextPathPos = ResolveSteeringTarget();
		Vector3 currentPos = GlobalPosition;
		
		// Flatten the direction vector BEFORE normalizing so we don't lose horizontal speed
		Vector3 direction = nextPathPos - currentPos;
		direction.Y = 0;
		
		// Prevent normalization errors if we are exactly on the point
		if (direction.LengthSquared() > 0.001f)
		{
			direction = direction.Normalized();
		}
		else
		{
			direction = Vector3.Zero;
		}

		Vector3 newVelocity = direction * speed;

		// Preserve existing Y velocity if falling
		if (!IsOnFloor())
		{
			newVelocity.Y = Velocity.Y - (Gravity * delta);
		}
		else
		{
			newVelocity.Y = 0f;
		}

		Velocity = newVelocity;
		MoveAndSlide();

		// Smooth rotation towards path direction, except when standing still
		if (Velocity.Length() > 0.1f)
		{
			float targetAngle = Mathf.Atan2(Velocity.X, Velocity.Z);
			Vector3 rot = Rotation;
			rot.Y = Mathf.LerpAngle(rot.Y, targetAngle, RotationSpeed * delta);
			Rotation = rot;
		}
	}

	private void InitializePatrol()
	{
		if (_patrolInitialized)
			return;

		if (_waypoints.Count == 0)
		{
			GD.PushWarning("[ListenerAI] No patrol waypoints found. Listener will only react to proximity/noise.");
			return;
		}

		_currentWaypointIndex = FindClosestWaypointIndex();
		_navAgent.TargetPosition = _waypoints[_currentWaypointIndex].GlobalPosition;
		_patrolInitialized = true;
		GD.Print($"[ListenerAI] Patrol initialized at waypoint {_currentWaypointIndex}.");
	}

	private int FindClosestWaypointIndex()
	{
		int closestIndex = 0;
		float closestDistance = float.MaxValue;

		for (int i = 0; i < _waypoints.Count; i++)
		{
			float distance = GlobalPosition.DistanceSquaredTo(_waypoints[i].GlobalPosition);
			if (distance < closestDistance)
			{
				closestDistance = distance;
				closestIndex = i;
			}
		}

		return closestIndex;
	}

	private Vector3 ResolveSteeringTarget()
	{
		bool hasNavMap = HasUsableNavigationMap();
		if (hasNavMap && !_navAgent.IsNavigationFinished())
		{
			Vector3 nextPathPos = _navAgent.GetNextPathPosition();
			Vector3 flattenedDelta = nextPathPos - GlobalPosition;
			flattenedDelta.Y = 0f;
			if (flattenedDelta.LengthSquared() > 0.0025f)
			{
				return nextPathPos;
			}
		}

		// Fallback keeps the listener moving even when the baked nav mesh is empty or not ready yet.
		return _navAgent.TargetPosition;
	}

	private bool HasUsableNavigationMap()
	{
		Rid navigationMap = _navAgent.GetNavigationMap();
		return navigationMap.IsValid && NavigationServer3D.MapGetIterationId(navigationMap) > 0;
	}

	protected void SelectNextWaypoint()
	{
		if (_waypoints.Count == 0) return;
		_currentWaypointIndex = (_currentWaypointIndex + 1) % _waypoints.Count;
		_navAgent.TargetPosition = _waypoints[_currentWaypointIndex].GlobalPosition;
		GD.Print($"[ListenerAI] Moving to waypoint {_currentWaypointIndex}");
	}

	// Proximity check sensor sweep (Notice, Alert & Attack Ranges)
	protected void CheckProximitySensors(float delta)
	{
		if (!Multiplayer.IsServer()) return;

		// Resolve all active player nodes in the game tree
		var players = GetTree().GetNodesInGroup("Player");
		Node3D closestPlayer = null;
		float minDistance = float.MaxValue;

		foreach (Node node in players)
		{
			if (node is Node3D player)
			{
				float dist = GlobalPosition.DistanceTo(player.GlobalPosition);
				if (dist < minDistance)
				{
					minDistance = dist;
					closestPlayer = player;
				}
			}
		}

		if (closestPlayer == null) return;

		// Shared LoS helper: shoot a ray from the Listener's eye level to the player's centre.
		// Returns true only when no static geometry blocks the sightline.
		bool HasLineOfSight(Node3D target)
		{
			var spaceState = GetWorld3D().DirectSpaceState;
			var from = GlobalPosition + Vector3.Up * 1.6f;   // approximate eye height
			var to   = target.GlobalPosition + Vector3.Up * 0.9f;
			var query = PhysicsRayQueryParameters3D.Create(from, to);
			query.CollisionMask = CollisionLayers;
			query.Exclude = new Godot.Collections.Array<Rid> { GetRid() };
			var result = spaceState.IntersectRay(query);
			// Clear LoS means the first thing the ray hits is the target itself (or nothing)
			if (result.Count == 0) return true;
			var hitCollider = result["collider"].AsGodotObject();
			return hitCollider == target;
		}

		// --- NOTICE RANGE: look toward the player only when we can actually see them ---
		if (minDistance <= NoticeRange && minDistance > AlertRange && HasLineOfSight(closestPlayer))
		{
			LookAtOrigin(closestPlayer.GlobalPosition);
			if (_stateLabel != null && _currentState == AIState.Idle)
			{
				_stateLabel.Text = $"IDLE\nNOTICED PLAYER ({minDistance:F1}m)";
				_stateLabel.Modulate = Colors.Yellow;
			}
		}

		// --- ALERT RANGE: chase only when LoS is clear (no wallhack) ---
		if (minDistance <= AlertRange && HasLineOfSight(closestPlayer))
		{
			if (_currentState == AIState.Idle || _currentState == AIState.Alerted)
			{
				GD.Print($"[ListenerAI] Player spotted (LoS) inside Alert Range ({minDistance:F2}m)! Initiating chase!");
				TransitionState(AIState.Hunting);
			}

			// Keep chasing; actual nav target is already updated by heard-noise events.
			_lastHeardLocation = closestPlayer.GlobalPosition;
			_navAgent.TargetPosition = _lastHeardLocation;
		}

		// --- ATTACK RANGE (Registers player hit) ---
		if (minDistance <= AttackRange)
		{
			if (_attackTimer <= 0f)
			{
				GD.Print($"[ListenerAI] HIT PLAYER! Attack registered at distance: {minDistance:F2}m!");
				if (_stateLabel != null)
				{
					_stateLabel.Text = $"HIT PLAYER!\n({minDistance:F1}m)";
					_stateLabel.Modulate = Colors.DeepPink;
				}
				_attackTimer = AttackCooldown;
			}
		}

		if (_attackTimer > 0f)
		{
			_attackTimer -= delta;
		}
	}

	// Update the 3D billboard text above the Listener's head
	private void UpdateStateLabel(AIState state)
	{
		if (_stateLabel == null) return;
		_stateLabel.Text = state.ToString().ToUpper();
		switch (state)
		{
			case AIState.Idle:
				_stateLabel.Modulate = Colors.Green;
				break;
			case AIState.Alerted:
				_stateLabel.Modulate = Colors.LightBlue;
				break;
			case AIState.Hunting:
				_stateLabel.Modulate = Colors.Orange;
				break;
			case AIState.Frenzy:
				_stateLabel.Modulate = Colors.Red;
				break;
		}
	}

	[Rpc(MultiplayerApi.RpcMode.Authority, CallLocal = false, TransferMode = MultiplayerPeer.TransferModeEnum.Unreliable)]
	private void SyncRemoteState(Vector3 position, Vector3 velocity, Vector3 rotation)
	{
		if (Multiplayer.MultiplayerPeer == null || !Multiplayer.HasMultiplayerPeer() || Multiplayer.IsServer())
			return;

		GlobalPosition = position;
		Velocity = velocity;
		Rotation = rotation;
	}
}
