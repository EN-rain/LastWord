using Godot;
using System;
using System.Collections.Generic;

public partial class ListenerAI : CharacterBody3D
{
	[Export] public float WalkSpeed = 3.0f;
	[Export] public float JogSpeed = 3.9f;     // 1.3x Walk
	[Export] public float SprintSpeed = 5.4f;  // 1.8x Walk

	public enum AIState { Idle, Alerted, Hunting, Frenzy }
	protected AIState _currentState = AIState.Idle;

	protected float _alertedSilenceTimer = 0f;
	protected float _whisperPauseDecay = 0f;
	protected float _frenzyTimer = 0f;
	protected Vector3 _lastHeardLocation;

	protected NavigationAgent3D _navAgent;
	protected Node3D _visuals;
	protected MeshInstance3D _leftEye;
	protected MeshInstance3D _rightEye;
	protected AudioStreamPlayer3D _humPlayer;

	protected List<Node3D> _waypoints = new();
	protected int _currentWaypointIndex = 0;

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

		// Fallback search if group is empty
		if (_waypoints.Count == 0)
		{
			var container = GetTree().Root.GetNodeOrNull("GameScene/PatrolWaypoints");
			if (container != null)
			{
				foreach (Node child in container.GetChildren())
				{
					if (child is Node3D n) _waypoints.Add(n);
				}
			}
		}

		GD.Print($"ListenerAI: Found {_waypoints.Count} waypoints.");
	}

	public override void _PhysicsProcess(double delta)
	{
		if (!Multiplayer.IsServer() && Multiplayer.MultiplayerPeer != null && Multiplayer.HasMultiplayerPeer())
			return; // Server-authoritative AI execution

		UpdateStateLogic((float)delta);
	}

	protected void UpdateStateLogic(float delta)
	{
		switch (_currentState)
		{
			case AIState.Idle:
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
					if (_alertedSilenceTimer >= 5.0f) // 5 seconds of complete silence returns to Idle
					{
						TransitionState(AIState.Idle);
					}
				}
				// Stands still while alerted
				MoveTowardsTarget(delta, 0f); 
				break;

			case AIState.Hunting:
				_navAgent.TargetPosition = _lastHeardLocation;
				MoveTowardsTarget(delta, JogSpeed);
				if (_navAgent.IsNavigationFinished())
				{
					TransitionState(AIState.Alerted);
				}
				break;

			case AIState.Frenzy:
				_frenzyTimer -= delta;
				_navAgent.TargetPosition = _lastHeardLocation;
				MoveTowardsTarget(delta, SprintSpeed);
				if (_frenzyTimer <= 0)
				{
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

		switch (tier)
		{
			case 0: // Environmental / Sub-whisper
				if (distance <= 4.0f && _currentState == AIState.Idle)
					TransitionState(AIState.Alerted);
				break;

			case 1: // Whisper
				if (distance <= 8.0f)
				{
					if (_currentState == AIState.Idle) TransitionState(AIState.Alerted);
					if (_currentState == AIState.Alerted)
					{
						_whisperPauseDecay = Mathf.Min(_whisperPauseDecay + 2.0f, 6.0f); // Pauses Alerted-to-Idle decay
						LookAtOrigin(origin); // Snap head-turn
					}
				}
				break;

			case 2: // Normal Talking
				if (distance <= 20.0f && _currentState != AIState.Frenzy)
					TransitionState(AIState.Hunting);
				break;

			case 3: // Scream
				TransitionState(AIState.Frenzy);
				_frenzyTimer = 12.0f;
				break;
		}
	}

	protected void TransitionState(AIState newState)
	{
		if (_currentState == newState) return;
		_currentState = newState;

		_alertedSilenceTimer = 0f;
		_whisperPauseDecay = 0f;

		Color eyeColor = Colors.Black;

		switch (newState)
		{
			case AIState.Idle: eyeColor = Colors.Black; break;
			case AIState.Alerted: eyeColor = new Color(0, 0.5f, 1.0f); break; // Faint Blue
			case AIState.Hunting: eyeColor = new Color(1.0f, 0.6f, 0); break; // Amber
			case AIState.Frenzy: eyeColor = Colors.White; break;
		}

		Rpc(nameof(SyncEyeColor), eyeColor);
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
		Vector3 nextPathPos = _navAgent.GetNextPathPosition();
		Vector3 currentPos = GlobalPosition;
		Vector3 newVelocity = (nextPathPos - currentPos).Normalized() * speed;

		if (!IsOnFloor())
		{
			newVelocity.Y = -9.8f;
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
			rot.Y = Mathf.LerpAngle(rot.Y, targetAngle, 10f * delta);
			Rotation = rot;
		}
	}

	protected void SelectNextWaypoint()
	{
		if (_waypoints.Count == 0) return;
		_currentWaypointIndex = (_currentWaypointIndex + 1) % _waypoints.Count;
		_navAgent.TargetPosition = _waypoints[_currentWaypointIndex].GlobalPosition;
	}
}
