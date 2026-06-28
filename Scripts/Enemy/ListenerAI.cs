using Godot;
using LastWord;
using System;
using System.Collections.Generic;
using System.Linq;

public partial class ListenerAI : CharacterBody3D
{
	[ExportGroup("Node References")]
	[Export] public NodePath NavAgentPath;
	[Export] public NodePath VisualsPath;
	[Export] public NodePath LeftEyePath;
	[Export] public NodePath RightEyePath;
	[Export] public NodePath HumPlayerPath;

	[ExportGroup("Behavior Tree")]
	[Export] public NodePath BehaviorTreeListenerPath;

	[ExportGroup("Debug")]
	[Export] public bool ShowDebugVisuals = true;
	[Export] public bool UseDebugShapesAsDetectionRanges = true;
	[Export] public NodePath DebugLabelPath;
	[Export] public NodePath SubWhisperDebugShapePath;
	[Export] public NodePath WhisperDebugShapePath;
	[Export] public NodePath MovementDebugShapePath;
	[Export] public NodePath NormalDebugShapePath;
	[Export] public NodePath NoticeDebugShapePath;
	[Export] public NodePath AttackDebugShapePath;
	[Export] public NodePath SubWhisperDebugMeshPath;
	[Export] public NodePath WhisperDebugMeshPath;
	[Export] public NodePath MovementDebugMeshPath;
	[Export] public NodePath NormalDebugMeshPath;
	[Export] public NodePath NoticeDebugMeshPath;
	[Export] public NodePath AttackDebugMeshPath;

	[ExportGroup("Movement")]
	[Export] public float WalkSpeed = 3.0f;
	[Export] public float JogSpeed = 3.9f;     // 1.3x Walk
	[Export] public float SprintSpeed = 5.4f;  // 1.8x Walk
	[Export] public float RotationSpeed = 10.0f;
	[Export] public float RunAnimationSpeedThreshold = 3.8f;
	[Export] public float InvestigationArriveDistance = 0.75f;
	[Export] public float InvestigationStuckTimeout = 1.0f;
	[Export] public float InvestigationProgressEpsilon = 0.05f;
	[Export] public float LurkMoveSpeedMultiplier = 0.55f;
	[Export] public float PatrolPauseDuration = 1.25f;
	[Export] public float Gravity = 9.8f;
	[Export] public float RemoteSyncInterval = 0.05f;

	[ExportGroup("Animation Names")]
	[Export] public string IdleAnimationName = "";
	[Export] public string LurkAnimationName = "";
	[Export] public string WalkAnimationName = "";
	[Export] public string RunAnimationName = "";
	[Export] public float LurkFallbackSpeedScale = 0.65f;
	[Export] public bool PrintAnimationDebug = false;

	[ExportGroup("Audio Detection")]
	[Export] public float SubWhisperRadius = 4.0f;
	[Export] public float WhisperRadius = 8.0f;
	[Export] public float NormalRadius = 20.0f;
	[Export] public float MovementInvestigateRadius = 8.0f;
	[Export] public float NormalSprintThresholdTolerance = 0.5f;
	[Export] public bool SprintOnNormalSound = true;
	[Export] public bool SprintOnRunningMovementNoise = true;
	[Export] public int MovementSprintTier = 1;
	[Export] public float HearingPriorityInterval = 1.0f;
	[Export] public float AlertSilenceTimeout = 5.0f;
	[Export] public float WhisperDecayAdd = 2.0f;
	[Export] public float WhisperDecayCap = 6.0f;
	[Export] public float FrenzyDuration = 12.0f;

	[ExportGroup("Escalation (§4.2)")]
	[Export] public float PreEscalationAlertTimeout = 8.0f;
	[Export] public float PostEscalationAlertTimeout = 15.0f;
	[Export] public bool PostEscalationAlertToHunting = true;

	public enum AIState { Idle, Alerted, Hunting, Frenzy }
	protected AIState _currentState = AIState.Idle;

	protected float _alertedSilenceTimer = 0f;
	protected float _whisperPauseDecay = 0f;
	protected float _frenzyTimer = 0f;
	private bool _isPostEscalation = false;
	protected float _hearingPriorityTimer = 0f;
	protected bool _phase3PermanentFrenzy = false;
	protected bool _isSecondListener = false;
	protected Vector3 _lastHeardLocation;
	protected Vector3 _soundInvestigateLocation;
	protected Vector3 _sprintTargetLocation;
	protected Vector3 _screamTargetLocation;
	protected bool _hasSoundInvestigateTarget = false;
	protected bool _hasSprintTarget = false;
	protected Node3D _screamTarget;
	protected Node3D _vocalSacrificeTarget;
	protected float _vocalSacrificeTimer;
	protected float _stunTimer;
	protected ListenerTargetMode _targetMode = ListenerTargetMode.None;
	private float _investigationStuckTimer = 0f;
	private float _lastInvestigationDistance = float.MaxValue;

	[ExportGroup("Proximity Ranges")]
	[Export] public float NoticeRange = 15.0f;
	[Export] public float AlertRange = 10.0f;
	[Export] public float AttackRange = 5.0f;
	[Export] public float AttackCooldown = 2.0f;

	[ExportGroup("Line of Sight")]
	// Physics layers the LoS raycast tests against (set to your Environment/World layer in the editor).
	[Export(PropertyHint.Layers3DPhysics)] public uint CollisionLayers = 1;

	private float _attackTimer = 0.0f;
	private float _remoteSyncTimer = 0.0f;
	private float _behaviorTreeDelta = 0f;
	private Node _behaviorTreeListener;
	private Label3D _stateLabel;
	private AnimationPlayer _animationPlayer;
	private string _animIdle = "";
	private string _animLurk = "";
	private string _animWalk = "";
	private string _animRun = "";
	private ListenerAnimationIntent _animationIntent = ListenerAnimationIntent.Idle;

	protected NavigationAgent3D _navAgent;
	protected Node3D _visuals;
	protected MeshInstance3D _leftEye;
	protected MeshInstance3D _rightEye;
	protected AudioStreamPlayer3D _humPlayer;

	protected List<Node3D> _waypoints = new();
	protected int _currentWaypointIndex = 0;
	private bool _patrolInitialized = false;
	private float _patrolPauseTimer = 0f;
	private bool _hasPendingEvent = false;
	private float _processTickInterval = 0.1f;
	private float _processTickAccumulator = 0f;

	private enum ListenerAnimationIntent
	{
		Idle,
		Lurk,
		Walk,
		Run
	}

	public override void _Ready()
	{
		_navAgent = GetNodeOrNull<NavigationAgent3D>(NavAgentPath);
		_visuals = GetNodeOrNull<Node3D>(VisualsPath);
		_animationPlayer = FindAnimationPlayer(this);
		_leftEye = GetNodeOrNull<MeshInstance3D>(LeftEyePath);
		_rightEye = GetNodeOrNull<MeshInstance3D>(RightEyePath);
		_humPlayer = GetNodeOrNull<AudioStreamPlayer3D>(HumPlayerPath);
		InitializeHumPlayer();
		_behaviorTreeListener = GetNodeOrNull<Node>(BehaviorTreeListenerPath);
		_stateLabel = GetNodeOrNull<Label3D>(DebugLabelPath);
		SubscribePhase3Signals();
		CacheAnimationNames();
		LoadDetectionRangesFromDebugShapes();
		ValidateTuningValues();
		SubscribeEscalationSignal();

		if (Name.ToString().Contains("Listener2", StringComparison.OrdinalIgnoreCase))
		{
			SetSecondListenerMode(true);
			Visible = false;
			SetPhysicsProcess(false);
			SetProcess(false);
		}

		if (_navAgent == null)
		{
			GD.PushError("ListenerAI: NavAgentPath is not assigned or points to a missing NavigationAgent3D.");
			SetPhysicsProcess(false);
			return;
		}

		// Cache patrol waypoints
		var waypointNodes = GetTree().GetNodesInGroup("Waypoint");
		foreach (var node in waypointNodes)
		{
			if (node is Node3D n) _waypoints.Add(n);
		}
		_waypoints = _waypoints.OrderBy(node => node.Name.ToString()).ToList();

		InitializeDebugVisuals();

		CallDeferred(nameof(InitializePatrol));
	}

	private void ValidateTuningValues()
	{
		WalkSpeed = Mathf.Max(WalkSpeed, 0.1f);
		JogSpeed = Mathf.Max(JogSpeed, WalkSpeed);
		SprintSpeed = Mathf.Max(SprintSpeed, JogSpeed);
		RotationSpeed = Mathf.Max(RotationSpeed, 0.1f);
		RunAnimationSpeedThreshold = Mathf.Clamp(RunAnimationSpeedThreshold, 0.01f, SprintSpeed);
		InvestigationArriveDistance = Mathf.Max(InvestigationArriveDistance, 0.1f);
		InvestigationStuckTimeout = Mathf.Max(InvestigationStuckTimeout, 0.1f);
		InvestigationProgressEpsilon = Mathf.Max(InvestigationProgressEpsilon, 0.001f);
		LurkMoveSpeedMultiplier = Mathf.Clamp(LurkMoveSpeedMultiplier, 0.1f, 1.0f);
		PatrolPauseDuration = Mathf.Max(PatrolPauseDuration, 0f);
		Gravity = Mathf.Max(Gravity, 0f);
		RemoteSyncInterval = Mathf.Max(RemoteSyncInterval, 0.01f);

		SubWhisperRadius = Mathf.Max(SubWhisperRadius, 0f);
		WhisperRadius = Mathf.Max(WhisperRadius, SubWhisperRadius);
		MovementInvestigateRadius = Mathf.Max(MovementInvestigateRadius, SubWhisperRadius);
		NormalRadius = Mathf.Max(NormalRadius, Mathf.Max(WhisperRadius, MovementInvestigateRadius));
		NormalSprintThresholdTolerance = Mathf.Max(NormalSprintThresholdTolerance, 0f);
		MovementSprintTier = Mathf.Clamp(MovementSprintTier, 0, 3);
		HearingPriorityInterval = Mathf.Max(HearingPriorityInterval, 0f);
		AlertSilenceTimeout = Mathf.Max(AlertSilenceTimeout, 0f);
		PreEscalationAlertTimeout = Mathf.Max(PreEscalationAlertTimeout, 0f);
		PostEscalationAlertTimeout = Mathf.Max(PostEscalationAlertTimeout, 0f);
		WhisperDecayAdd = Mathf.Max(WhisperDecayAdd, 0f);
		WhisperDecayCap = Mathf.Max(WhisperDecayCap, WhisperDecayAdd);
		FrenzyDuration = Mathf.Max(FrenzyDuration, 0f);

		NoticeRange = Mathf.Max(NoticeRange, 0f);
		AttackRange = Mathf.Max(AttackRange, 0f);
		AlertRange = Mathf.Max(AlertRange, AttackRange);
		AttackCooldown = Mathf.Max(AttackCooldown, 0f);
		LurkFallbackSpeedScale = Mathf.Clamp(LurkFallbackSpeedScale, 0.1f, 1.0f);
	}

	private void InitializeDebugVisuals()
	{
		if (_stateLabel != null)
		{
			_stateLabel.Visible = ShowDebugVisuals;
			_stateLabel.Text = "IDLE\nNO SOUND";
			_stateLabel.Modulate = Colors.Green;
		}
		else if (ShowDebugVisuals)
		{
			GD.PushWarning("ListenerAI: DebugLabelPath is not assigned, so sound/state text will not be visible.");
		}

		ConfigureDebugRadius(SubWhisperDebugShapePath, SubWhisperDebugMeshPath, SubWhisperRadius);
		ConfigureDebugRadius(WhisperDebugShapePath, WhisperDebugMeshPath, WhisperRadius);
		ConfigureDebugRadius(MovementDebugShapePath, MovementDebugMeshPath, MovementInvestigateRadius);
		ConfigureDebugRadius(NormalDebugShapePath, NormalDebugMeshPath, NormalRadius);
		ConfigureDebugRadius(NoticeDebugShapePath, NoticeDebugMeshPath, NoticeRange);
		ConfigureDebugRadius(AttackDebugShapePath, AttackDebugMeshPath, AttackRange);
	}

	private void LoadDetectionRangesFromDebugShapes()
	{
		if (!UseDebugShapesAsDetectionRanges)
			return;

		SubWhisperRadius = ReadDebugSphereRadius(SubWhisperDebugShapePath, SubWhisperRadius);
		WhisperRadius = ReadDebugSphereRadius(WhisperDebugShapePath, WhisperRadius);
		MovementInvestigateRadius = ReadDebugSphereRadius(MovementDebugShapePath, MovementInvestigateRadius);
		NormalRadius = ReadDebugSphereRadius(NormalDebugShapePath, NormalRadius);
		NoticeRange = ReadDebugSphereRadius(NoticeDebugShapePath, NoticeRange);
		AttackRange = ReadDebugSphereRadius(AttackDebugShapePath, AttackRange);
	}

	private float ReadDebugSphereRadius(NodePath shapePath, float fallback)
	{
		var shape = GetNodeOrNull<CollisionShape3D>(shapePath);
		return shape?.Shape is SphereShape3D sphere ? sphere.Radius : fallback;
	}

	private void ConfigureDebugRadius(NodePath shapePath, NodePath meshPath, float radius)
	{
		var shape = GetNodeOrNull<CollisionShape3D>(shapePath);
		if (shape?.Shape is SphereShape3D sphere)
			sphere.Radius = radius;
		else if (ShowDebugVisuals && !shapePath.IsEmpty)
			GD.PushWarning($"ListenerAI: Debug shape path '{shapePath}' is missing or is not a SphereShape3D.");

		var mesh = GetNodeOrNull<MeshInstance3D>(meshPath);
		if (mesh != null)
		{
			mesh.Visible = ShowDebugVisuals;
			mesh.Scale = Vector3.One * radius;
		}
		else if (ShowDebugVisuals && !meshPath.IsEmpty)
		{
			GD.PushWarning($"ListenerAI: Debug mesh path '{meshPath}' is missing or is not a MeshInstance3D.");
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

	private void CacheAnimationNames()
	{
		if (_animationPlayer == null)
		{
			GD.PushWarning("[ListenerAI] No AnimationPlayer found in Listener hierarchy; AI movement will not animate.");
			return;
		}

		_animIdle = ResolveAnimationName(IdleAnimationName, "idle", "stand", "breath");
		_animLurk = ResolveAnimationName(LurkAnimationName, "lurk", "creep", "sneak", "stalk");
		_animWalk = ResolveAnimationName(WalkAnimationName, "walk", "move");
		_animRun = ResolveAnimationName(RunAnimationName, "run", "sprint", "jog");

		foreach (string anim in _animationPlayer.GetAnimationList())
		{
			string lower = anim.ToLower();
			if (lower.Contains("carry")) continue;

			if (string.IsNullOrEmpty(_animIdle) && (lower.Contains("idle") || lower.Contains("stand") || lower.Contains("breath")))
				_animIdle = anim;
			else if (string.IsNullOrEmpty(_animLurk) && (lower.Contains("lurk") || lower.Contains("creep") || lower.Contains("sneak") || lower.Contains("stalk")))
				_animLurk = anim;
			else if (string.IsNullOrEmpty(_animWalk) && (lower.Contains("walk") || lower.Contains("move")))
				_animWalk = anim;
			else if (string.IsNullOrEmpty(_animRun) && (lower.Contains("run") || lower.Contains("sprint") || lower.Contains("jog")))
				_animRun = anim;
		}

		if (PrintAnimationDebug)
		{
			GD.Print($"[ListenerAI] Animations available: {string.Join(", ", _animationPlayer.GetAnimationList())}");
			GD.Print($"[ListenerAI] Animation map: idle='{_animIdle}', lurk='{ResolveLurkAnimation()}', walk='{_animWalk}', run='{_animRun}'");
		}

		if (string.IsNullOrEmpty(_animIdle))
			GD.PushWarning("[ListenerAI] No idle animation was found. The listener will stop animation when idle instead of playing walk.");

		if (string.IsNullOrEmpty(_animRun))
			GD.PushWarning("[ListenerAI] No run animation was found. Sprint movement will use the fastest available movement animation.");
	}

	private string ResolveAnimationName(string configuredName, params string[] keywords)
	{
		if (_animationPlayer == null)
			return "";

		if (!string.IsNullOrWhiteSpace(configuredName) && _animationPlayer.HasAnimation(configuredName))
			return configuredName;

		foreach (string anim in _animationPlayer.GetAnimationList())
		{
			string lower = anim.ToLowerInvariant();
			if (lower.Contains("carry"))
				continue;

			foreach (string keyword in keywords)
			{
				if (lower.Contains(keyword))
					return anim;
			}
		}

		return "";
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
				Rpc(nameof(SyncRemoteState), GlobalPosition, Velocity, Rotation, (int)_animationIntent);
			}
		}
	}

	public override void _Process(double delta)
	{
		// Strategy A.4: skip-tick when no pending event and accumulator hasn't elapsed
		_processTickAccumulator += (float)delta;
		if (!_hasPendingEvent && _processTickAccumulator < _processTickInterval)
			return;
		_processTickAccumulator = 0f;
		_hasPendingEvent = false;

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
		if (_stunTimer > 0f)
		{
			_stunTimer -= delta;
			_animationIntent = ListenerAnimationIntent.Idle;
			MoveTowardsTarget(delta, 0f);
			if (_stunTimer <= 0f)
				SetDebugLabel("", Colors.White, 0f);
			return;
		}

		_behaviorTreeDelta = delta;
		_targetMode = ResolveTargetPriority();
		switch (_targetMode)
		{
			case ListenerTargetMode.Phase3PermanentFrenzy:
				_animationIntent = ListenerAnimationIntent.Run;
				{
					Node3D tokenHolder = VoiceManager.Instance?.TokenHolder;
					if (tokenHolder != null && !IsPlayerDead(tokenHolder))
					{
						SetNavigationTarget(tokenHolder.GlobalPosition);
						MoveTowardsTarget(delta, SprintSpeed);
					}
					else
					{
						Node3D radio = GetTree().GetFirstNodeInGroup("RadioItem") as Node3D;
						if (radio != null)
						{
							SetNavigationTarget(radio.GlobalPosition);
							MoveTowardsTarget(delta, SprintSpeed);
						}
						else
						{
							Log("Phase 3 Permanent Frenzy active but no token holder or radio target found.");
							MoveTowardsTarget(delta, 0f);
						}
					}
				}
				break;

			case ListenerTargetMode.VocalSacrifice:
				_animationIntent = ListenerAnimationIntent.Run;
				_vocalSacrificeTimer -= delta;
				if (_vocalSacrificeTarget == null || IsPlayerDead(_vocalSacrificeTarget))
				{
					_vocalSacrificeTimer = 0f;
					_vocalSacrificeTarget = null;
					TransitionState(AIState.Alerted);
					break;
				}

				SetNavigationTarget(_vocalSacrificeTarget.GlobalPosition);
				MoveTowardsTarget(delta, SprintSpeed);
				SetDebugLabel("SACRIFICE LOCK", Colors.Orange, GlobalPosition.DistanceTo(_vocalSacrificeTarget.GlobalPosition));
				if (_vocalSacrificeTimer <= 0f)
				{
					Log("Vocal Sacrifice lock expired. Re-evaluating target priority.");
					_vocalSacrificeTarget = null;
					TransitionState(ResolveTargetPriority() == ListenerTargetMode.None ? AIState.Alerted : AIState.Hunting);
				}
				break;

			case ListenerTargetMode.ScreamFrenzy:
				_animationIntent = ListenerAnimationIntent.Run;
				_frenzyTimer -= delta;
				if (IsPlayerDead(_screamTarget))
				{
					ClearScreamFrenzy();
					TransitionState(AIState.Alerted);
					break;
				}

				SetNavigationTarget(ResolveNodePosition(_screamTarget, _screamTargetLocation));
				MoveTowardsTarget(delta, SprintSpeed);
				if (_frenzyTimer <= 0)
				{
					Log("Scream Frenzy timer expired. Re-evaluating target priority.");
					ClearScreamFrenzy();
					TransitionState(ResolveTargetPriority() == ListenerTargetMode.None ? AIState.Alerted : AIState.Hunting);
				}
				break;

			case ListenerTargetMode.NonFrenzySprint:
				_animationIntent = ListenerAnimationIntent.Run;
				_hearingPriorityTimer += delta;
				SetNavigationTarget(_sprintTargetLocation);
				MoveTowardsTarget(delta, SprintSpeed);
				if (_navAgent.IsNavigationFinished())
				{
					_hasSprintTarget = false;
					_soundInvestigateLocation = ResolveNavigableTarget(_sprintTargetLocation);
					_hasSoundInvestigateTarget = true;
					ResetInvestigationProgress();
					TransitionState(AIState.Alerted);
				}
				break;

			case ListenerTargetMode.Token:
				_animationIntent = ListenerAnimationIntent.Run;
				Node3D tokenTarget = VoiceManager.Instance?.TokenHolder;
				if (tokenTarget == null || IsPlayerDead(tokenTarget))
				{
					TransitionState(AIState.Alerted);
					break;
				}

				SetNavigationTarget(tokenTarget.GlobalPosition);
				MoveTowardsTarget(delta, JogSpeed);
				break;

			case ListenerTargetMode.SoundInvestigate:
				_animationIntent = ListenerAnimationIntent.Lurk;
				SetNavigationTarget(_soundInvestigateLocation);
				if (HasReachedTarget(_soundInvestigateLocation, InvestigationArriveDistance))
				{
					CompleteSoundInvestigation("ARRIVED");
					break;
				}

				MoveTowardsTarget(delta, GetLurkMoveSpeed());
				if (HasReachedTarget(_soundInvestigateLocation, InvestigationArriveDistance))
				{
					CompleteSoundInvestigation("ARRIVED");
				}
				else if (IsInvestigationStuck(delta))
				{
					CompleteSoundInvestigation("STUCK");
				}
				break;

			case ListenerTargetMode.SecondListenerImprint:
				_animationIntent = ListenerAnimationIntent.Run;
				Node3D imprintTarget = GetSecondListenerImprintTarget();
				if (imprintTarget == null || IsPlayerDead(imprintTarget))
				{
					TransitionState(AIState.Alerted);
					break;
				}

				SetNavigationTarget(imprintTarget.GlobalPosition);
				MoveTowardsTarget(delta, JogSpeed);
				SetDebugLabel("IMPRINT TARGET", Colors.Purple, GlobalPosition.DistanceTo(imprintTarget.GlobalPosition));
				break;

			case ListenerTargetMode.None:
				if (_currentState == AIState.Alerted)
					break;

				_animationIntent = ListenerAnimationIntent.Lurk;
				if (!_patrolInitialized)
				{
					InitializePatrol();
				}

				if (_patrolPauseTimer > 0f)
				{
					_animationIntent = ListenerAnimationIntent.Idle;
					_patrolPauseTimer -= delta;
					MoveTowardsTarget(delta, 0f);
					if (_patrolPauseTimer <= 0f)
						SelectNextWaypoint();
					break;
				}

				if (_navAgent.IsNavigationFinished())
				{
					_patrolPauseTimer = PatrolPauseDuration;
					_animationIntent = ListenerAnimationIntent.Idle;
					MoveTowardsTarget(delta, 0f);
					break;
				}

				MoveTowardsTarget(delta, GetLurkMoveSpeed());
				break;
		}

		if (_targetMode != ListenerTargetMode.None || _currentState != AIState.Alerted)
			return;

		_animationIntent = ListenerAnimationIntent.Idle;

		if (_whisperPauseDecay > 0)
		{
			_whisperPauseDecay -= delta;
		}
		else
		{
			_alertedSilenceTimer += delta;
			float timeout = _isPostEscalation ? PostEscalationAlertTimeout : PreEscalationAlertTimeout;
			if (_alertedSilenceTimer >= timeout)
			{
				if (_isPostEscalation && PostEscalationAlertToHunting)
				{
					// Post-10min: Alerted transitions to Hunting instead of resetting to Idle.
					TransitionState(AIState.Hunting);
				}
				else
				{
					TransitionState(AIState.Idle);
				}
			}
		}

		MoveTowardsTarget(delta, 0f);
	}

	protected ListenerTargetMode ResolveTargetPriority()
	{
		if (_behaviorTreeListener != null)
		{
			_targetMode = ListenerTargetMode.None;
			_behaviorTreeListener.Call("update", _behaviorTreeDelta);
			return _targetMode;
		}

		return ResolveFallbackTargetPriority();
	}

	private ListenerTargetMode ResolveFallbackTargetPriority()
	{
		if (EvaluateBehaviorCondition(ListenerBehaviorCondition.Phase3PermanentFrenzy))
			return ListenerTargetMode.Phase3PermanentFrenzy;
		if (EvaluateBehaviorCondition(ListenerBehaviorCondition.ActiveScreamFrenzy))
			return ListenerTargetMode.ScreamFrenzy;
		if (EvaluateBehaviorCondition(ListenerBehaviorCondition.VocalSacrificeLock))
			return ListenerTargetMode.VocalSacrifice;
		if (EvaluateBehaviorCondition(ListenerBehaviorCondition.HasSprintTarget))
			return ListenerTargetMode.NonFrenzySprint;
		if (EvaluateBehaviorCondition(ListenerBehaviorCondition.HasSecondListenerImprintTarget))
			return ListenerTargetMode.SecondListenerImprint;
		if (EvaluateBehaviorCondition(ListenerBehaviorCondition.HasTokenTarget))
			return ListenerTargetMode.Token;
		if (EvaluateBehaviorCondition(ListenerBehaviorCondition.HasSoundInvestigation))
			return ListenerTargetMode.SoundInvestigate;
		return ListenerTargetMode.None;
	}

	public bool EvaluateBehaviorCondition(ListenerBehaviorCondition condition)
	{
		return condition switch
		{
			ListenerBehaviorCondition.Phase3PermanentFrenzy => IsPhase3PermanentFrenzyActive(),
			ListenerBehaviorCondition.ActiveScreamFrenzy => _currentState == AIState.Frenzy && _frenzyTimer > 0f,
			ListenerBehaviorCondition.VocalSacrificeLock => IsVocalSacrificeLockActive(),
			ListenerBehaviorCondition.HasSprintTarget => _hasSprintTarget,
			ListenerBehaviorCondition.HasSecondListenerImprintTarget => GetSecondListenerImprintTarget() != null,
			ListenerBehaviorCondition.HasTokenTarget => _currentState == AIState.Hunting
				&& VoiceManager.Instance?.TokenHolder != null
				&& !IsPlayerDead(VoiceManager.Instance.TokenHolder),
			ListenerBehaviorCondition.HasSoundInvestigation => _hasSoundInvestigateTarget,
			ListenerBehaviorCondition.Always => true,
			_ => false
		};
	}

	public bool EvaluateBehaviorConditionValue(int conditionValue)
	{
		if (!Enum.IsDefined(typeof(ListenerBehaviorCondition), conditionValue))
			return false;

		return EvaluateBehaviorCondition((ListenerBehaviorCondition)conditionValue);
	}

	public void SetBehaviorTargetModeValue(int targetModeValue)
	{
		if (!Enum.IsDefined(typeof(ListenerTargetMode), targetModeValue))
			return;

		_targetMode = (ListenerTargetMode)targetModeValue;
	}

	protected virtual bool IsPhase3PermanentFrenzyActive() => _phase3PermanentFrenzy;

	private void SubscribePhase3Signals()
	{
		if (GameManager.Instance != null)
			GameManager.Instance.Phase3Started += OnPhase3Started;
	}

	private void SubscribeEscalationSignal()
	{
		if (GameManager.Instance == null)
			return;

		GameManager.Instance.EscalationReached += OnEscalationReached;
		_isPostEscalation = GameManager.Instance.IsPostEscalation;
	}

	private void OnEscalationReached()
	{
		_isPostEscalation = true;
		Log("10-minute escalation reached — Alerted state will now transition to Hunting on silence.");
	}

	private void OnPhase3Started()
	{
		if (Multiplayer.MultiplayerPeer != null && Multiplayer.HasMultiplayerPeer() && !Multiplayer.IsServer())
			return;

		Log("GameManager entered Phase 3 — entering Permanent Frenzy.");
		_phase3PermanentFrenzy = true;
		TransitionState(AIState.Frenzy);
	}
	protected virtual bool IsVocalSacrificeLockActive() => _vocalSacrificeTimer > 0f && _vocalSacrificeTarget != null && !IsPlayerDead(_vocalSacrificeTarget);

	/// <summary>
	/// Server-authoritative call from VocalSacrifice to lock the Listener onto
	/// a target for the configured duration.
	/// </summary>
	public void SetVocalSacrificeTarget(Node3D target, float duration)
	{
		if (!Multiplayer.IsServer())
			return;

		_vocalSacrificeTarget = target;
		_vocalSacrificeTimer = duration;
		Rpc(nameof(SyncVocalSacrificeTarget), target?.GetPath() ?? new NodePath(""), duration);
	}

	[Rpc(MultiplayerApi.RpcMode.Authority, CallLocal = true)]
	private void SyncVocalSacrificeTarget(NodePath targetPath, float duration)
	{
		_vocalSacrificeTarget = GetNodeOrNull<Node3D>(targetPath);
		_vocalSacrificeTimer = duration;
	}

	/// <summary>
	/// Server-authoritative call from LoudStun to freeze this Listener.
	/// </summary>
	public void ApplyStun(float duration)
	{
		if (!Multiplayer.IsServer())
			return;

		_stunTimer = duration;
		Rpc(nameof(SyncStun), duration);
	}

	[Rpc(MultiplayerApi.RpcMode.Authority, CallLocal = true)]
	private void SyncStun(float duration)
	{
		_stunTimer = duration;
		SetDebugLabel("STUNNED", Colors.Yellow, 0f);
	}

	public void SetSecondListenerMode(bool enabled)
	{
		_isSecondListener = enabled;
	}

	protected virtual Node3D GetSecondListenerImprintTarget()
	{
		return _isSecondListener
			? GameManager.Instance?.ImprintTracker?.GetMostImprintedLivingTarget()
			: null;
	}
	protected virtual bool IsAudioSuppressedByFutureSystems(ListenerSoundEvent soundEvent)
	{
		return StaticBubble.IsPositionSuppressed(soundEvent.Origin);
	}

	protected static bool IsPlayerDead(Node3D node)
	{
		return node is PlayerController player && player.IsDead;
	}

	protected static Vector3 ResolveNodePosition(Node3D node, Vector3 fallback)
	{
		return node != null && !IsPlayerDead(node) ? node.GlobalPosition : fallback;
	}

	protected void ClearScreamFrenzy()
	{
		_screamTarget = null;
		_frenzyTimer = 0f;
	}

	// Called by VoiceManager when a player makes noise
	public void HearNoise(Vector3 origin, int tier)
	{
		HearNoise(new ListenerSoundEvent(origin, tier, SoundKind.Voice));
	}

	public void HearNoise(ListenerSoundEvent soundEvent)
	{
		_hasPendingEvent = true;
		if (!Multiplayer.IsServer()) return;
		if (IsAudioSuppressedByFutureSystems(soundEvent)) return;
		if (soundEvent.Source != null && IsPlayerDead(soundEvent.Source)) return;

		if (_phase3PermanentFrenzy)
		{
			Log($"Phase 3 Permanent Frenzy locks targeting — ignoring {FormatSoundKind(soundEvent.Kind)} T{soundEvent.Tier} at {soundEvent.Origin}");
			return;
		}

		float distance = GlobalPosition.DistanceTo(soundEvent.Origin);
		_lastHeardLocation = soundEvent.Origin;

		// Set temporary label text to show noise heard!
		if (_stateLabel != null)
		{
			_stateLabel.Text = $"{_currentState.ToString().ToUpper()}\nHEARD TIER {soundEvent.Tier} {soundEvent.Kind} ({distance:F1}m)";
			_stateLabel.Modulate = GetDebugColorForTier(soundEvent.Tier, soundEvent.Kind);
		}

		if (_currentState == AIState.Frenzy && _frenzyTimer > 0f)
		{
			Log($"Ignored {FormatSoundKind(soundEvent.Kind)} T{soundEvent.Tier}; Scream Frenzy is locked.");
			return;
		}

		switch (soundEvent.Tier)
		{
			case 0: // Environmental / Walking Footstep
				if (soundEvent.IsMovementOrNoise)
				{
					HandleMovementNoise(soundEvent, distance);
				}
				else if (distance <= SubWhisperRadius * GetPlayerDetectionMultiplier(soundEvent.Source))
				{
					Log($"Wary of sub-whisper voice near {soundEvent.Origin} (Distance: {distance:F2}m)");
					MarkWary(soundEvent.Origin, distance);
				}
				break;

			case 1: // Whisper / Running Footstep
				if (soundEvent.IsVoice)
				{
					if (distance <= WhisperRadius * GetPlayerDetectionMultiplier(soundEvent.Source))
					{
						Log($"Oriented toward whisper at {soundEvent.Origin} (Distance: {distance:F2}m)");
						_whisperPauseDecay = Mathf.Min(_whisperPauseDecay + WhisperDecayAdd, WhisperDecayCap);
						LookAtOrigin(soundEvent.Origin);
						MarkWary(soundEvent.Origin, distance);
					}
				}
				else
				{
					HandleMovementNoise(soundEvent, distance);
				}
				break;

			case 2: // Normal Talking
				if (distance <= NormalRadius * GetPlayerDetectionMultiplier(soundEvent.Source))
				{
					Log($"Heard {FormatSoundKind(soundEvent.Kind)} T{soundEvent.Tier} at {soundEvent.Origin} (Distance: {distance:F2}m). Initiating chase.");
					LookAtOrigin(soundEvent.Origin);
					if (ShouldSprintToNormalSound(distance, soundEvent))
					{
						StartNonFrenzySprint(soundEvent.Origin, "SPRINTING TO SOUND");
					}
					else
					{
						StartSoundInvestigation(soundEvent.Origin);
						TransitionState(soundEvent.IsVoice ? AIState.Hunting : AIState.Alerted);
					}
				}
				break;

			case 3: // Scream
				Log($"Heard scream at {soundEvent.Origin}. Entering Scream Frenzy.");
				LookAtOrigin(soundEvent.Origin);
				_screamTarget = soundEvent.Source;
				_screamTargetLocation = ResolveNavigableTarget(soundEvent.Origin);
				SetNavigationTarget(_screamTargetLocation);
				TransitionState(AIState.Frenzy);
				_frenzyTimer = FrenzyDuration;
				_targetMode = ListenerTargetMode.ScreamFrenzy;
				SetDebugLabel("SCREAM LOCK", Colors.Red, distance);
				break;
		}
	}

	private void HandleMovementNoise(ListenerSoundEvent soundEvent, float distance)
	{
		float effectiveRadius = MovementInvestigateRadius * GetPlayerDetectionMultiplier(soundEvent.Source);

		if (distance > effectiveRadius)
		{
			Log($"Ignored {FormatSoundKind(soundEvent.Kind)} T{soundEvent.Tier} outside MovementArea at {soundEvent.Origin} (Distance: {distance:F2}m)");
			SetDebugLabel("OUTSIDE MOVEMENT AREA", Colors.Gray, distance);
			return;
		}

		if (ShouldSprintToMovementNoise(soundEvent))
		{
			Log($"Sprinting to {FormatSoundKind(soundEvent.Kind)} T{soundEvent.Tier} at {soundEvent.Origin} (Distance: {distance:F2}m)");
			StartNonFrenzySprint(soundEvent.Origin, "SPRINTING TO MOVEMENT");
			return;
		}

		Log($"Investigating {FormatSoundKind(soundEvent.Kind)} T{soundEvent.Tier} at {soundEvent.Origin} (Distance: {distance:F2}m)");
		StartSoundInvestigation(soundEvent.Origin);
		// Reset the silence timer so a fresh detection never triggers an immediate Idle transition.
		_alertedSilenceTimer = 0f;
		SetDebugLabel("INVESTIGATING", Colors.LightBlue, distance);
		if (_currentState == AIState.Idle || _currentState == AIState.Alerted)
		{
			TransitionState(AIState.Alerted);
		}

		if (_hasSprintTarget && _hearingPriorityTimer >= HearingPriorityInterval)
		{
			_sprintTargetLocation = ResolveNavigableTarget(soundEvent.Origin);
			_hearingPriorityTimer = 0f;
			SetDebugLabel("SPRINT RETARGET", Colors.Orange, distance);
		}
	}

	private void MarkWary(Vector3 origin, float distance)
	{
		LookAtOrigin(origin);
		StartSoundInvestigation(origin);
		// Reset silence timer on any fresh wary detection to prevent premature Idle transition.
		_alertedSilenceTimer = 0f;
		if (_currentState == AIState.Idle)
			TransitionState(AIState.Alerted);
		SetDebugLabel("WARY", Colors.Yellow, distance);
	}

	private bool ShouldSprintToNormalSound(float distance, ListenerSoundEvent soundEvent)
	{
		float multiplier = GetPlayerDetectionMultiplier(soundEvent.Source);
		return soundEvent.Tier >= 3
			|| (SprintOnNormalSound && soundEvent.Tier >= 2)
			|| soundEvent.IsSpecialLongRange
			|| distance >= NormalRadius * multiplier - NormalSprintThresholdTolerance;
	}

	private float GetPlayerDetectionMultiplier(Node3D source)
	{
		float multiplier = GetAdaptiveHearingMultiplier();
		if (source == null)
			return multiplier;

		RoleData role = source.GetNodeOrNull<RoleData>("RoleData");
		if (role != null && role.IsMute)
			multiplier *= role.MuteDetectionRadiusMultiplier;

		return multiplier;
	}

	private bool ShouldSprintToMovementNoise(ListenerSoundEvent soundEvent)
	{
		return SprintOnRunningMovementNoise
			&& soundEvent.IsMovementOrNoise
			&& soundEvent.Tier >= MovementSprintTier;
	}

	private void StartSoundInvestigation(Vector3 origin)
	{
		_soundInvestigateLocation = ResolveNavigableTarget(origin);
		_hasSoundInvestigateTarget = true;
		SetNavigationTarget(_soundInvestigateLocation);
		ResetInvestigationProgress();
	}

	private bool HasReachedTarget(Vector3 target, float arriveDistance)
	{
		Vector3 delta = target - GlobalPosition;
		delta.Y = 0f;
		return delta.LengthSquared() <= arriveDistance * arriveDistance;
	}

	private bool IsInvestigationStuck(float delta)
	{
		float currentDistance = HorizontalDistanceTo(_soundInvestigateLocation);
		bool madeProgress = currentDistance < _lastInvestigationDistance - InvestigationProgressEpsilon;

		if (madeProgress)
		{
			_investigationStuckTimer = 0f;
			_lastInvestigationDistance = currentDistance;
			return false;
		}

		_investigationStuckTimer += delta;
		_lastInvestigationDistance = currentDistance;
		return _investigationStuckTimer >= InvestigationStuckTimeout;
	}

	private float HorizontalDistanceTo(Vector3 target)
	{
		Vector3 delta = target - GlobalPosition;
		delta.Y = 0f;
		return delta.Length();
	}

	private void ResetInvestigationProgress()
	{
		_investigationStuckTimer = 0f;
		_lastInvestigationDistance = HorizontalDistanceTo(_soundInvestigateLocation);
	}

	private void CompleteSoundInvestigation(string reason)
	{
		_hasSoundInvestigateTarget = false;
		ResetInvestigationProgress();
		StopMovementAtCurrentPosition();
		SetDebugLabel($"INVESTIGATION {reason}", Colors.LightBlue, 0f);
		TransitionState(AIState.Alerted);
	}

	private void StopMovementAtCurrentPosition()
	{
		if (_navAgent != null)
			_navAgent.TargetPosition = GlobalPosition;

		_animationIntent = ListenerAnimationIntent.Idle;
		Velocity = Vector3.Zero;
		UpdateAnimation(0f, false, ListenerAnimationIntent.Idle);
	}

	private float GetLurkMoveSpeed()
	{
		return WalkSpeed * LurkMoveSpeedMultiplier;
	}

	private static float GetAdaptiveSpeedMultiplier()
	{
		return GameManager.Instance?.AdaptiveEvolution?.SpeedMultiplier ?? 1f;
	}

	private static float GetAdaptiveHearingMultiplier()
	{
		return GameManager.Instance?.AdaptiveEvolution?.HearingMultiplier ?? 1f;
	}

	private static float GetAdaptiveAttackRangeMultiplier()
	{
		return GameManager.Instance?.AdaptiveEvolution?.AttackRangeMultiplier ?? 1f;
	}

	private void SetNavigationTarget(Vector3 target)
	{
		if (_navAgent == null)
			return;

		_navAgent.TargetPosition = ResolveNavigableTarget(target);
	}

	private Vector3 ResolveNavigableTarget(Vector3 target)
	{
		if (!HasUsableNavigationMap())
			return target;

		Rid navigationMap = _navAgent.GetNavigationMap();
		Vector3 closestPoint = NavigationServer3D.MapGetClosestPoint(navigationMap, target);
		if (!IsFinite(closestPoint))
			return target;

		return closestPoint;
	}

	private static bool IsFinite(Vector3 value)
	{
		return float.IsFinite(value.X) && float.IsFinite(value.Y) && float.IsFinite(value.Z);
	}

	private void StartNonFrenzySprint(Vector3 target, string label)
	{
		_sprintTargetLocation = ResolveNavigableTarget(target);
		_hasSprintTarget = true;
		_hasSoundInvestigateTarget = false;
		_hearingPriorityTimer = 0f;
		SetNavigationTarget(_sprintTargetLocation);
		TransitionState(AIState.Hunting);
		SetDebugLabel(label, Colors.Orange, GlobalPosition.DistanceTo(_sprintTargetLocation));
	}

	private void SetDebugLabel(string label, Color color, float distance)
	{
		if (_stateLabel == null) return;
		_stateLabel.Text = $"{label}\n({distance:F1}m)";
		_stateLabel.Modulate = color;
	}

	private void Log(string message)
	{
		GD.Print($"{LogPrefix()} {message}");
	}

	private string LogPrefix()
	{
		return $"[ListenerAI:{Name}]";
	}

	private static string FormatSoundKind(SoundKind kind)
	{
		return kind.ToString().ToUpperInvariant();
	}

	private static Color GetDebugColorForTier(int tier, SoundKind kind)
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

	protected void TransitionState(AIState newState)
	{
		if (_currentState == newState)
			return;

		if (Multiplayer.MultiplayerPeer != null && Multiplayer.HasMultiplayerPeer())
		{
			Rpc(nameof(SyncState), (int)newState);
			return;
		}

		ApplyState(newState);
	}

	private void InitializeHumPlayer()
	{
		if (_humPlayer == null)
			return;

		_humPlayer.Bus = "ListenerAudio";
		_humPlayer.Stream = AudioAssets.Load(AudioAssets.ListenerHum);
		_humPlayer.Autoplay = true;
		_humPlayer.MaxDb = -6.0f;
		if (_humPlayer.Stream != null && !_humPlayer.Playing)
			_humPlayer.Play();
	}

	private void ApplyState(AIState newState)
	{
		if (_currentState == newState)
			return;

		Log($"State transitioning from {_currentState} to {newState}");
		_currentState = newState;

		_alertedSilenceTimer = 0f;
		_whisperPauseDecay = 0f;

		ApplyEyeColor(GetEyeColorForState(newState));
		UpdateStateLabel(newState);

		if (newState == AIState.Alerted)
		{
			AudioAssets.PlayOneShot3D(AudioAssets.ListenerAlertClick, this, GlobalPosition, bus: "ListenerAudio");
		}
		else if (newState == AIState.Hunting)
		{
			// §12.2 Listener Hunting breath (8 m audible) — one-shot trigger on
			// entering Hunting so it layers on top of the continuous hum and
			// signals escalation to anyone nearby.
			AudioAssets.PlayOneShot3D(AudioAssets.ListenerHuntingBreath, this, GlobalPosition, bus: "ListenerAudio", pitchScale: (float)GD.RandRange(0.95, 1.05));
		}
		else if (newState == AIState.Frenzy)
		{
			// §12.2 Listener Frenzy tone — harsh dissonant cluster marks
			// scream-lock / Phase-3 permanent frenzy. Variant pitch by trigger
			// so successive frenzies don't sound identical.
			AudioAssets.PlayOneShot3D(AudioAssets.ListenerFrenzyTone, this, GlobalPosition, bus: "ListenerAudio", pitchScale: (float)GD.RandRange(0.92, 1.08));
		}

		UpdateHumPitch();
	}

	private void UpdateHumPitch()
	{
		if (_humPlayer == null || _humPlayer.Stream == null)
			return;

		_humPlayer.PitchScale = _currentState switch
		{
			AIState.Idle => 1.0f,
			AIState.Alerted => 1.05f,
			AIState.Hunting => 1.15f,
			AIState.Frenzy => 1.35f,
			_ => 1.0f
		};
	}

	[Rpc(MultiplayerApi.RpcMode.Authority, CallLocal = true)]
	private void SyncState(int stateValue)
	{
		if (!Enum.IsDefined(typeof(AIState), stateValue))
			return;

		ApplyState((AIState)stateValue);
	}

	[Rpc(MultiplayerApi.RpcMode.Authority, CallLocal = true)]
	protected void SyncEyeColor(Color color)
	{
		ApplyEyeColor(color);
	}

	private Color GetEyeColorForState(AIState state)
	{
		return state switch
		{
			AIState.Idle => new Color(0.2f, 0.2f, 0.2f),
			AIState.Alerted => new Color(1.0f, 0.5f, 0.0f),
			AIState.Hunting => new Color(1.0f, 0.0f, 0.0f),
			AIState.Frenzy => Colors.White,
			_ => Colors.Black
		};
	}

	private void ApplyEyeColor(Color color)
	{
		ApplyEyeMaterial(_leftEye, color);
		ApplyEyeMaterial(_rightEye, color);
	}

	private static void ApplyEyeMaterial(MeshInstance3D eye, Color color)
	{
		if (eye == null)
			return;

		StandardMaterial3D material = eye.MaterialOverride as StandardMaterial3D;
		if (material == null)
		{
			Material source = eye.MaterialOverride;
			if (source == null && eye.Mesh is PrimitiveMesh primitiveMesh)
				source = primitiveMesh.Material;

			material = source is StandardMaterial3D standardMaterial
				? standardMaterial.Duplicate() as StandardMaterial3D
				: new StandardMaterial3D();

			eye.MaterialOverride = material;
		}

		material.AlbedoColor = color;
	}

	protected void LookAtOrigin(Vector3 origin)
	{
		var lookTarget = new Vector3(origin.X, GlobalPosition.Y, origin.Z);
		FaceDirection(lookTarget - GlobalPosition, 1.0f);
	}

	protected void MoveTowardsTarget(float delta, float speed)
	{
		speed *= GetAdaptiveSpeedMultiplier();

		if (_navAgent == null)
		{
			Velocity = Vector3.Zero;
			UpdateAnimation(0f, false, ListenerAnimationIntent.Idle);
			return;
		}

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

		Vector3 horizontalVelocity = new Vector3(Velocity.X, 0f, Velocity.Z);
		bool isMovingHorizontally = horizontalVelocity.Length() > 0.1f;

		// Smooth rotation towards path direction, except when standing still
		if (isMovingHorizontally)
		{
			FaceDirection(horizontalVelocity, RotationSpeed * delta);
		}

		UpdateAnimation(speed, isMovingHorizontally, _animationIntent);
	}

	private void FaceDirection(Vector3 direction, float weight)
	{
		direction.Y = 0f;
		if (direction.LengthSquared() <= 0.001f)
			return;

		float targetAngle = Mathf.Atan2(direction.X, direction.Z);
		Vector3 rot = Rotation;
		rot.Y = Mathf.LerpAngle(rot.Y, targetAngle, Mathf.Clamp(weight, 0f, 1f));
		Rotation = rot;
	}

	private void UpdateAnimation(float intendedSpeed, bool isMoving, ListenerAnimationIntent intent)
	{
		if (_animationPlayer == null) return;

		string targetAnim = ResolveTargetAnimation(intent, intendedSpeed, isMoving);
		if (string.IsNullOrEmpty(targetAnim))
		{
			if (!string.IsNullOrEmpty(_animationPlayer.CurrentAnimation))
				_animationPlayer.Stop();
			return;
		}

		if (_animationPlayer.HasAnimation(targetAnim)
			&& _animationPlayer.CurrentAnimation != targetAnim)
		{
			_animationPlayer.Play(targetAnim, 0.2f);
		}

		_animationPlayer.SpeedScale = ResolveAnimationSpeedScale(targetAnim, intendedSpeed, isMoving);
	}

	private string ResolveTargetAnimation(ListenerAnimationIntent intent, float intendedSpeed, bool isMoving)
	{
		if (!isMoving || intendedSpeed <= 0f || intent == ListenerAnimationIntent.Idle)
			return FirstAvailable(_animIdle);

		if (intent == ListenerAnimationIntent.Run || intendedSpeed >= RunAnimationSpeedThreshold)
			return FirstAvailable(_animRun, _animWalk, _animLurk, _animIdle);

		if (intent == ListenerAnimationIntent.Lurk)
			return FirstAvailable(ResolveLurkAnimation(), _animRun, _animIdle);

		return FirstAvailable(_animWalk, _animLurk, _animRun, _animIdle);
	}

	private string ResolveLurkAnimation()
	{
		return FirstAvailable(_animLurk, _animWalk);
	}

	private static string FirstAvailable(params string[] names)
	{
		foreach (string name in names)
		{
			if (!string.IsNullOrEmpty(name))
				return name;
		}

		return "";
	}

	private float ResolveAnimationSpeedScale(string targetAnim, float intendedSpeed, bool isMoving)
	{
		if (!isMoving || string.IsNullOrEmpty(targetAnim) || targetAnim == _animIdle)
			return 1.0f;

		if (targetAnim == _animLurk || (string.IsNullOrEmpty(_animLurk) && targetAnim == _animWalk && _animationIntent == ListenerAnimationIntent.Lurk))
			return LurkFallbackSpeedScale;

		if (targetAnim == _animWalk)
			return Mathf.Clamp(intendedSpeed / WalkSpeed, 0.75f, 1.35f);

		if (targetAnim == _animRun)
			return Mathf.Clamp(intendedSpeed / SprintSpeed, 0.85f, 1.2f);

		return 1.0f;
	}

	private void InitializePatrol()
	{
		if (_patrolInitialized)
			return;

		if (_waypoints.Count == 0)
		{
			GD.PushWarning($"{LogPrefix()} No patrol waypoints found. Listener will only react to proximity/noise.");
			return;
		}

		_currentWaypointIndex = FindClosestWaypointIndex();
		_navAgent.TargetPosition = _waypoints[_currentWaypointIndex].GlobalPosition;
		_patrolInitialized = true;
		Log($"Patrol initialized at waypoint {_currentWaypointIndex}.");
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

		if (hasNavMap)
		{
			return GlobalPosition;
		}

		// Fallback only before the runtime navigation bake is ready.
		return _navAgent?.TargetPosition ?? GlobalPosition;
	}

	private bool HasUsableNavigationMap()
	{
		if (_navAgent == null)
			return false;

		Rid navigationMap = _navAgent.GetNavigationMap();
		return navigationMap.IsValid && NavigationServer3D.MapGetIterationId(navigationMap) > 0;
	}

	protected void SelectNextWaypoint()
	{
		if (_navAgent == null || _waypoints.Count == 0) return;
		_currentWaypointIndex = (_currentWaypointIndex + 1) % _waypoints.Count;
		_navAgent.TargetPosition = _waypoints[_currentWaypointIndex].GlobalPosition;
		Log($"Moving to waypoint {_currentWaypointIndex}");
	}

	// Proximity check sensor sweep (Notice, Alert & Attack Ranges)
	protected void CheckProximitySensors(float delta)
	{
		if (!Multiplayer.IsServer()) return;

		var players = GetTree().GetNodesInGroup("Player");
		Node3D visibleMovingPlayer = null;
		float visibleMovingDistance = float.MaxValue;
		bool ignoredStationaryInRange = false;
		float ignoredStationaryDistance = float.MaxValue;

		foreach (Node node in players)
		{
			if (node is not Node3D player || IsPlayerDead(player))
				continue;

			if (_currentState == AIState.Frenzy && _frenzyTimer > 0f && _screamTarget != null && player != _screamTarget)
				continue;

			float dist = GlobalPosition.DistanceTo(player.GlobalPosition);
			bool hasLineOfSight = HasLineOfSight(player);
			bool attackEligible = IsPlayerAttackEligible(player);

			if (dist <= AttackRange * GetAdaptiveAttackRangeMultiplier())
			{
				if (attackEligible && hasLineOfSight && _attackTimer <= 0f && player is PlayerController controller)
				{
					// §12.2 Catch silence — the air being pulled out before the
					// Listener closes the distance. Plays on this node so the
					// listenerAudio bus ducks the hum momentarily via attenuation.
					AudioAssets.PlayOneShot3D(AudioAssets.ListenerCatchSilence, this, GlobalPosition, bus: "ListenerAudio");
					controller.KillByListener(this, GetAttackReason(player));
					SetDebugLabel("ATTACK KILL", Colors.DeepPink, dist);
					_attackTimer = AttackCooldown;
					continue;
				}

				if (!attackEligible && dist < ignoredStationaryDistance)
				{
					ignoredStationaryInRange = true;
					ignoredStationaryDistance = dist;
				}
			}

			if (!attackEligible || !hasLineOfSight || dist > NoticeRange)
				continue;

			if (dist < visibleMovingDistance)
			{
				visibleMovingPlayer = player;
				visibleMovingDistance = dist;
			}
		}

		if (visibleMovingPlayer != null)
		{
			LookAtOrigin(visibleMovingPlayer.GlobalPosition);
			StartSoundInvestigation(visibleMovingPlayer.GlobalPosition);
			if (_currentState == AIState.Idle)
				TransitionState(AIState.Alerted);

			SetDebugLabel("WARY INVESTIGATE", Colors.Yellow, visibleMovingDistance);
		}
		else if (ignoredStationaryInRange && _currentState != AIState.Frenzy)
		{
			SetDebugLabel("IGNORED STATIONARY", Colors.Gray, ignoredStationaryDistance);
		}

		if (_attackTimer > 0f)
		{
			_attackTimer -= delta;
		}
	}

	private bool HasLineOfSight(Node3D target)
	{
		var spaceState = GetWorld3D().DirectSpaceState;
		var from = GlobalPosition + Vector3.Up * 1.6f;
		var to = target.GlobalPosition + Vector3.Up * 0.9f;
		var query = PhysicsRayQueryParameters3D.Create(from, to);
		query.CollisionMask = CollisionLayers;
		var exclusions = new Godot.Collections.Array<Rid>();
		AddCollisionExclusions(this, exclusions);
		query.Exclude = exclusions;

		var result = spaceState.IntersectRay(query);
		if (result.Count == 0) return true;

		var hitCollider = result["collider"].AsGodotObject();
		if (hitCollider is not Node hitNode) return false;
		return hitNode == target || target.IsAncestorOf(hitNode);
	}

	private void AddCollisionExclusions(Node node, Godot.Collections.Array<Rid> exclusions)
	{
		if (node is CollisionObject3D collisionObject)
			exclusions.Add(collisionObject.GetRid());

		foreach (Node child in node.GetChildren())
			AddCollisionExclusions(child, exclusions);
	}

	private bool IsPlayerAttackEligible(Node3D player)
	{
		if (_currentState == AIState.Frenzy && _frenzyTimer > 0f && player == _screamTarget)
			return true;

		return player is PlayerController controller
			&& (controller.IsMovingForListener || controller.HasRecentListenerNoise);
	}

	private string GetAttackReason(Node3D player)
	{
		if (_currentState == AIState.Frenzy && _frenzyTimer > 0f && player == _screamTarget)
			return "SCREAM LOCK";

		if (player is not PlayerController controller)
			return "LISTENER ATTACK";

		if (controller.HasRecentListenerNoise)
			return $"{FormatSoundKind(controller.LastListenerNoiseKind)} T{controller.LastListenerNoiseTier}";

		return "MOVEMENT";
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
	private void SyncRemoteState(Vector3 position, Vector3 velocity, Vector3 rotation, int animationIntentValue)
	{
		if (Multiplayer.MultiplayerPeer == null || !Multiplayer.HasMultiplayerPeer() || Multiplayer.IsServer())
			return;

		GlobalPosition = position;
		Velocity = velocity;
		Rotation = rotation;

		if (Enum.IsDefined(typeof(ListenerAnimationIntent), animationIntentValue))
			_animationIntent = (ListenerAnimationIntent)animationIntentValue;

		Vector3 horizontalVelocity = new Vector3(velocity.X, 0f, velocity.Z);
		float speed = horizontalVelocity.Length();
		UpdateAnimation(speed, speed > 0.1f, _animationIntent);
	}
}
