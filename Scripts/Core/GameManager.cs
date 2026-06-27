using Godot;
using LastWord;
using LastWord.Core;
using System;

public partial class GameManager : Node3D
{
	public static GameManager Instance { get; private set; }

	public enum GamePhase
	{
		Phase1,
		Phase2,
		Phase3,
		Victory,
		Failed
	}

	[Signal] public delegate void PhaseChangedEventHandler(GamePhase newPhase);
	[Signal] public delegate void Phase2StartedEventHandler();
	[Signal] public delegate void Phase3StartedEventHandler();
	[Signal] public delegate void VictoryEventHandler();
	[Signal] public delegate void RunFailedEventHandler(string reason);
	[Signal] public delegate void EscalationReachedEventHandler();

	public GamePhase CurrentPhase { get; private set; } = GamePhase.Phase1;

	[ExportGroup("Phase Triggers")]
	[Export] public PackedScene RadioItemScene;
	[Export] public NodePath RadioSpawnMarkerPath;
	[Export] public NodePath WordRegistryPath;
	[Export] public NodePath SequenceManagerPath;
	[Export] public NodePath RadioBroadcastPath;
	[Export] public NodePath RegistrationBoardPath;
	[Export] public PackedScene PlayerScene;
	[Export] public NodePath PlayersContainerPath;
	[Export] public NodePath OfflinePlayerPath;
	[Export] public NodePath NavigationRegionPath;
	[Export] public NodePath NavigationGeometryRootPath;
	[Export] public NodePath ExtraNavigationGeometryRootPath;
	[Export] public NodePath LevelVisualRootPath;
	[Export] public NodePath LevelCollisionRootPath;
	[Export] public Vector3 PlayerSpawnPosition = new Vector3(0, 1, 0);
	[Export] public float RunLogThreshold = 300.0f; // 5 minutes
	[Export] public float OrientationDuration = 210.0f; // 3 min 30 sec
	[Export] public float DuplicationTime = 1200.0f; // 20 minutes
	[Export] public float LightsOutTime = 1500.0f; // 25 minutes
	[Export] public float FinalCountdownTime = 1800.0f; // 30 minutes
	[Export] public float EscalationTime = 600.0f; // 10 minutes (§4.2, §8)

	public bool IsPostEscalation { get; set; } = false;
	public float RunElapsed => _runElapsed;
	public VocalImprintTracker ImprintTracker { get; private set; }
	public PlaybackManager Playback { get; private set; }
	public AdaptiveEvolution AdaptiveEvolution { get; private set; }

	private Node  _playersContainer;
	private System.Collections.Generic.Dictionary<long, (string word, int tier)> _heldNotesByPeer = new();
	private float _sessionElapsed    = 0.0f;
	private bool  _runLogged         = false;
	private float _orientationTimer  = 0.0f;
	private bool  _orientationActive = false;
	private float _runElapsed        = 0.0f;

	public override void _EnterTree()
	{
		Instance = this;
	}

	public override void _Ready()
	{
		Instance = this;
		CallDeferred(nameof(SetupNavigationRuntime));

		// ------------------------------------------------------------------
		// Vocal imprinting tracker (§3.4) — owned by GameManager so it
		// persists for the run and can be queried by the Second Listener.
		// ------------------------------------------------------------------
		ImprintTracker = new VocalImprintTracker();
		ImprintTracker.Name = "VocalImprintTracker";
		AddChild(ImprintTracker);

		Playback = new PlaybackManager();
		Playback.Name = "PlaybackManager";
		AddChild(Playback);

		AdaptiveEvolution = new AdaptiveEvolution();
		AdaptiveEvolution.Name = "AdaptiveEvolution";
		AddChild(AdaptiveEvolution);

		var escalationTimer = new EscalationTimer();
		escalationTimer.Name = "EscalationTimer";
		AddChild(escalationTimer);

		// AchievementManager: a fresh run starts when GameManager is ready. Reset per-run trackers.
		AchievementManager.Instance?.OnRunStart();

		// S9 Party-wipe tracking
		PlayerController.Died += OnPlayerDied;

		// ------------------------------------------------------------------
		// Phase State Machine wiring
		// ------------------------------------------------------------------
		var wordRegistry = EnsureWordRegistry();
		wordRegistry.AllWordsRegistered += OnAllWordsRegistered;

		var sequenceManager = GetNodeOrNull<SequenceManager>(SequenceManagerPath);
		if (sequenceManager == null)
		{
			sequenceManager = new SequenceManager { Name = "SequenceManager" };
			AddChild(sequenceManager);
			SequenceManagerPath = sequenceManager.GetPath();
		}
		sequenceManager.SequenceComplete += OnSequenceComplete;

		var radioBroadcast = GetNodeOrNull<RadioBroadcast>(RadioBroadcastPath);
		if (radioBroadcast == null)
		{
			radioBroadcast = new RadioBroadcast { Name = "RadioBroadcast" };
			AddChild(radioBroadcast);
			RadioBroadcastPath = radioBroadcast.GetPath();
		}
		radioBroadcast.BroadcastComplete += OnBroadcastComplete;
		radioBroadcast.BroadcastFailed   += OnBroadcastFailed;

		if (PlayerScene == null)
		{
			GD.PushError("GameManager: PlayerScene is not assigned. Set it on the GameScene root in the inspector.");
			return;
		}

		_playersContainer = GetNodeOrNull(PlayersContainerPath);
		if (_playersContainer == null)
		{
			_playersContainer = new Node3D();
			_playersContainer.Name = "SpawnedPlayers";
			AddChild(_playersContainer);
		}

		// ------------------------------------------------------------------
		// Phase 1 note placement (§7.1)
		// ------------------------------------------------------------------
		CallDeferred(nameof(SpawnNoteItems));

		// ------------------------------------------------------------------
		// Orientation Mode — lock a 3m30s tutorial phase if any player in the
		// matched lobby has fewer than 3 prior runs (§18.3)
		// ------------------------------------------------------------------
		if (NetworkManager.Instance != null && NetworkManager.Instance.IsOrientationModeActive)
		{
			_orientationActive = true;
			_orientationTimer  = OrientationDuration;
		}

		// ------------------------------------------------------------------
		// Multiplayer spawn setup
		// ------------------------------------------------------------------
		if (Multiplayer.MultiplayerPeer != null && Multiplayer.HasMultiplayerPeer())
		{
			// 1. Remove the static offline player node if it exists to avoid duplicates
			var offlinePlayer = GetNodeOrNull(OfflinePlayerPath);
			if (offlinePlayer != null)
			{
				offlinePlayer.QueueFree();
			}

			// 2. Setup the MultiplayerSpawner dynamically
			MultiplayerSpawner spawner = new MultiplayerSpawner();
			AddChild(spawner);
			spawner.SpawnPath = _playersContainer.GetPath();
			spawner.AddSpawnableScene(PlayerScene.ResourcePath);

			// 3. Only the server/host handles spawning player instances
			if (Multiplayer.IsServer())
			{
				Multiplayer.PeerDisconnected += OnPeerDisconnected;

				// Dedicated server runs headlessly and does not have a local host player
				bool isDedicatedServer = OS.HasFeature("dedicated_server") || DisplayServer.GetName() == "headless";
				if (!isDedicatedServer)
				{
					// Spawn host player character (Peer ID 1) immediately for local listen server/host
					SpawnPlayer(1);
				}
			}
			else
			{
				// Send ready signal to server/host so they spawn our player node
				RpcId(NetworkManager.ServerPeerId, nameof(RegisterReadyClient));
			}
		}
	}

	public override void _ExitTree()
	{
		if (Instance == this) Instance = null;

		PlayerController.Died -= OnPlayerDied;

		if (Multiplayer != null)
			Multiplayer.PeerDisconnected -= OnPeerDisconnected;

		var wordRegistry = GetNodeOrNull<WordRegistry>(WordRegistryPath);
		if (wordRegistry != null)
		{
			wordRegistry.AllWordsRegistered -= OnAllWordsRegistered;
		}
		else if (WordRegistry.Instance != null)
		{
			WordRegistry.Instance.AllWordsRegistered -= OnAllWordsRegistered;
		}

		var sequenceManager = GetNodeOrNull<SequenceManager>(SequenceManagerPath);
		if (sequenceManager != null)
			sequenceManager.SequenceComplete -= OnSequenceComplete;

		var radioBroadcast = GetNodeOrNull<RadioBroadcast>(RadioBroadcastPath);
		if (radioBroadcast != null)
		{
			radioBroadcast.BroadcastComplete -= OnBroadcastComplete;
			radioBroadcast.BroadcastFailed   -= OnBroadcastFailed;
		}
	}

	private void SetupNavigationRuntime()
	{
		// Reparent level geometry under the region once the scene tree is fully assembled,
		// then bake the mesh so agents can query a valid map on their first patrol tick.
		var navRegion = GetNodeOrNull<NavigationRegion3D>(NavigationRegionPath);
		if (navRegion == null)
			return;

		ReparentNodeUnder(navRegion, NavigationGeometryRootPath);
		ReparentNodeUnder(navRegion, ExtraNavigationGeometryRootPath);

		navRegion.BakeNavigationMesh(false);
		GD.Print("GameManager: Reparented geometry and baked Navigation Mesh.");
	}

	private void ReparentNodeUnder(Node newParent, NodePath childPath)
	{
		if (childPath == null || childPath.IsEmpty)
			return;

		var node = GetNodeOrNull(childPath);
		if (node == null || node.GetParent() == newParent)
			return;

		node.GetParent().RemoveChild(node);
		newParent.AddChild(node);
	}

	// ---------------------------------------------------------------------------
	// _Process — track session duration and orientation countdown
	// ---------------------------------------------------------------------------
	public override void _Process(double delta)
	{
		float dt = (float)delta;

		// -- continuous run timer for escalation / death-card stats --
		_runElapsed += dt;

		

		// -- 5-minute run logging (§18.2) --
		if (!_runLogged)
		{
			_sessionElapsed += dt;
			if (_sessionElapsed >= RunLogThreshold)
			{
				_runLogged = true;
				IncrementRunCount();
			}
		}

		// -- Orientation Mode countdown (§18.3) --
		if (_orientationActive)
		{
			_orientationTimer -= dt;
			if (_orientationTimer <= 0.0f)
			{
				_orientationActive = false;
				// GD.Print("GameManager: Orientation Mode timer expired. Active run phase begins.");
				OnOrientationComplete();
			}
		}
	}

	// ---------------------------------------------------------------------------
	// Run count logging
	// ---------------------------------------------------------------------------
	private void IncrementRunCount()
	{
		var cfg = new ConfigFile();
		cfg.Load("user://settings.cfg");

		int currentRuns = (int)cfg.GetValue("player", "runs", 0);
		currentRuns++;
		cfg.SetValue("player", "runs", currentRuns);
		cfg.Save("user://settings.cfg");

		// GD.Print($"GameManager: Session reached 5-minute mark. Run logged. Total runs: {currentRuns}");

		// Keep NetworkManager in sync so subsequent lobby checks are correct
		if (NetworkManager.Instance != null)
			NetworkManager.Instance.PlayerRuns = currentRuns;
	}

	// ---------------------------------------------------------------------------
	// Orientation complete callback (extend here to hide tutorial HUD etc.)
	// ---------------------------------------------------------------------------
	private void OnOrientationComplete()
	{
		// TODO: Signal HUDManager to hide orientation overlay when it is implemented
		// GD.Print("GameManager: OnOrientationComplete — tutorial overlay should be dismissed.");
	}

	// ---------------------------------------------------------------------------
	// Phase State Machine transitions
	// ---------------------------------------------------------------------------
	private void OnAllWordsRegistered()
	{
		if (CurrentPhase != GamePhase.Phase1)
			return;

		AudioAssets.PlayOneShot2D(AudioAssets.LockOpen01, this, "UI");
		GD.Print("GameManager: All words registered — advancing to Phase 2.");
		CurrentPhase = GamePhase.Phase2;

		// Generate the Phase 2 word sequence from registered words.
		var sequenceManager = GetNodeOrNull<SequenceManager>(SequenceManagerPath);
		int playerCount = _playersContainer?.GetChildCount() ?? 1;
		sequenceManager?.GenerateSequenceFromRegistry(playerCount);

		EmitSignal(SignalName.PhaseChanged, (int)CurrentPhase);
		EmitSignal(SignalName.Phase2Started);
	}

	private void OnSequenceComplete()
	{
		if (CurrentPhase != GamePhase.Phase2)
			return;

		GD.Print("GameManager: Sequence complete — advancing to Phase 3.");
		CurrentPhase = GamePhase.Phase3;
		EmitSignal(SignalName.PhaseChanged, (int)CurrentPhase);
		EmitSignal(SignalName.Phase3Started);
		SpawnRadioItem();
	}

	private void OnBroadcastComplete()
	{
		if (CurrentPhase != GamePhase.Phase3)
			return;

		AudioAssets.PlayOneShot2D(AudioAssets.VictoryStinger, this, "UI");
		GD.Print("GameManager: Broadcast complete — VICTORY.");
		CurrentPhase = GamePhase.Victory;
		EmitSignal(SignalName.PhaseChanged, (int)CurrentPhase);
		EmitSignal(SignalName.Victory);
	}

	private void OnBroadcastFailed()
	{
		// Broadcast failure may be recoverable (grace period / handoff), so we
		// do not auto-fail the run here.  Hook external logic if needed.
		GD.Print("GameManager: Broadcast failed — waiting for retry or handoff.");
	}

	public void TriggerRunFailed(string reason)
	{
		if (CurrentPhase == GamePhase.Victory || CurrentPhase == GamePhase.Failed)
			return;

		AudioAssets.PlayOneShot2D(AudioAssets.FailureStinger, this, "UI");
		GD.Print($"GameManager: Run failed — {reason}");
		CurrentPhase = GamePhase.Failed;
		EmitSignal(SignalName.PhaseChanged, (int)CurrentPhase);
		EmitSignal(SignalName.RunFailed, reason);

		// Achievement hook: Final Girl — if the run fails AND a local player is the only survivor,
		// they're the Final Girl. Cheap check; only fires once thanks to dedup in Unlock.
		var localPlayer = GetTree().GetFirstNodeInGroup("Player") as PlayerController;
		if (localPlayer != null && !localPlayer.IsDead)
		{
			int otherDead = 0;
			foreach (var node in GetTree().GetNodesInGroup("Player"))
			{
				if (node is PlayerController pc && pc != localPlayer && pc.IsDead)
					otherDead++;
			}
			if (otherDead > 0)
				AchievementManager.Instance?.UnlockFinalGirl();
		}
	}

	/// <summary>
	/// Called when the Radio final broadcast is transmitted at the RegistrationBoard.
	/// </summary>
	public void OnFinalBroadcastTransmitted(PlayerController carrier)
	{
		GD.Print($"GameManager: Final broadcast transmitted by {carrier?.Name ?? "unknown"}.");
		OnBroadcastComplete();
	}

	private void SpawnRadioItem()
	{
		if (RadioItemScene == null)
		{
			GD.PushError("GameManager: RadioItemScene is not assigned. Cannot spawn radio for Phase 3.");
			return;
		}

		Node3D spawnMarker = GetNodeOrNull<Node3D>(RadioSpawnMarkerPath);
		Vector3 spawnPos = spawnMarker?.GlobalPosition ?? PlayerSpawnPosition;

		var radioItem = RadioItemScene.Instantiate<Node3D>();
		radioItem.GlobalPosition = spawnPos;
		if (radioItem is RadioItem item)
			item.RadioPickedUp += OnRadioPickedUp;
		AddChild(radioItem);

		GD.Print($"GameManager: Spawned RadioItem at {spawnPos} for Phase 3.");
	}

	// ---------------------------------------------------------------------------
	// Multiplayer spawn helpers
	// ---------------------------------------------------------------------------
	[Rpc(MultiplayerApi.RpcMode.AnyPeer, CallLocal = false)]
	private void RegisterReadyClient()
	{
		if (!Multiplayer.IsServer()) return;

		long senderId = Multiplayer.GetRemoteSenderId();
		SpawnPlayer(senderId);
	}

	public (string word, int tier) GetHeldNote(long peerId)
	{
		return _heldNotesByPeer.TryGetValue(peerId, out var note) ? note : ("", 0);
	}

	public void ClearHeldNote(long peerId)
	{
		_heldNotesByPeer.Remove(peerId);
	}

	// ---------------------------------------------------------------------------
	// Phase 1 note placement (§7.1)
	// ---------------------------------------------------------------------------
	private void SpawnNoteItems()
	{
		var spawnRoot = GetNodeOrNull("F1_Basement/SpawnPoints");
		var markers = new System.Collections.Generic.List<Marker3D>();

		if (spawnRoot != null)
		{
			foreach (Node child in spawnRoot.GetChildren())
			{
				if (child is Marker3D marker && child.Name.ToString().StartsWith("NoteSpawn"))
					markers.Add(marker);
			}
		}
		else
		{
			// Fallback: scan the entire scene for NoteSpawn markers.
			foreach (Node node in GetTree().GetNodesInGroup("SpawnPoint"))
			{
				if (node is Marker3D marker && node.Name.ToString().StartsWith("NoteSpawn"))
					markers.Add(marker);
			}
		}

		if (markers.Count < 4)
		{
			GD.PushWarning($"GameManager: only {markers.Count} NoteSpawn markers found; need 4.");
			return;
		}

		var rng = new System.Random();
		// Shuffle markers in place
		for (int i = markers.Count - 1; i > 0; i--)
		{
			int j = rng.Next(i + 1);
			(markers[i], markers[j]) = (markers[j], markers[i]);
		}

		string[] words = WordPool.DrawDistinct(WordPool.Tier.One, 4, rng);
		for (int i = 0; i < 4; i++)
		{
			var note = new NoteItem
			{
				AssignedWord = words[i],
				Tier = 1,
				Name = $"NoteItem_{words[i]}"
			};
			note.NotePickedUp += OnNotePickedUp;
			Node parent = spawnRoot ?? this;
			parent.AddChild(note);
			note.GlobalPosition = markers[i].GlobalPosition;
		}

		GD.Print($"GameManager: spawned 4 Phase-1 notes.");
	}

	private void OnNotePickedUp(string word, int tier, long peerId)
	{
		_heldNotesByPeer[peerId] = (word, tier);
		GD.Print($"GameManager: peer {peerId} picked up note '{word}' (tier {tier}).");
	}

	private WordRegistry EnsureWordRegistry()
	{
		var wordRegistry = GetNodeOrNull<WordRegistry>(WordRegistryPath) ?? WordRegistry.Instance;
		if (wordRegistry == null)
		{
			wordRegistry = new WordRegistry { Name = "WordRegistry" };
			AddChild(wordRegistry);
		}

		WordRegistryPath = wordRegistry.GetPath();
		return wordRegistry;
	}

	private void OnRadioPickedUp(long peerId, bool _isGracePickup)
	{
		var carrier = FindPlayerByPeerId(peerId);
		if (carrier == null)
			return;

		var radio = new LastWord.World.Radio
		{
			Name = "Radio",
			RegistrationBoardPath = RegistrationBoardPath,
			RadioBroadcastPath = RadioBroadcastPath
		};
		AddChild(radio);
		radio.PickUp(carrier);
	}

	private PlayerController FindPlayerByPeerId(long peerId)
	{
		foreach (Node node in GetTree().GetNodesInGroup("Player"))
		{
			if (node is not PlayerController player)
				continue;
			if (long.TryParse(player.Name, out long id) && id == peerId)
				return player;
			if (player.GetMultiplayerAuthority() == peerId)
				return player;
		}
		return null;
	}

	

	private void SpawnPlayer(long peerId)
	{
		if (_playersContainer.HasNode(peerId.ToString()))
		{
			return;
		}

		var playerInstance = PlayerScene.Instantiate<Node3D>();
		playerInstance.Name      = peerId.ToString();
		playerInstance.Transform = new Transform3D(Basis.Identity, PlayerSpawnPosition);

		// Apply lobby-selected role (§5).
		var roleData = playerInstance.GetNodeOrNull<RoleData>("RoleData");
		if (roleData != null && NetworkManager.Instance != null)
		{
			var info = NetworkManager.Instance.LobbyPlayers.Find(p => p.PeerId == peerId);
			if (info != null)
				roleData.SetRole(info.SelectedRole);
		}

		_playersContainer.AddChild(playerInstance);
	}

	private void DespawnPlayer(long peerId)
	{
		var playerNode = _playersContainer.GetNodeOrNull(peerId.ToString());
		playerNode?.QueueFree();
	}

	private void OnPeerDisconnected(long peerId)
	{
		DespawnPlayer(peerId);
	}
	// ---------------------------------------------------------------------------
	// S9 Death tracking - Total Party Wipe
	// ---------------------------------------------------------------------------
	private void OnPlayerDied(PlayerController player, string reason, string killerName)
	{
		int livingCount = 0;
		foreach (var node in GetTree().GetNodesInGroup("Player"))
		{
			if (node is PlayerController pc && !pc.IsDead)
				livingCount++;
		}
		GD.Print($"[GameManager] Player {player.Name} died. Living: {livingCount}");
		if (livingCount == 0)
		{
			var t = GetTree().CreateTimer(1.0);
			t.Timeout += ShowPartyWipeScreen;
		}
	}

	private void ShowPartyWipeScreen()
	{
		if (CurrentPhase == GamePhase.Victory) return;
		GD.Print("[GameManager] Total party wipe.");
		var canvas = new CanvasLayer { Name = "PartyWipeCanvas", Layer = 100 };
		GetTree().Root.AddChild(canvas);
		var bg = new ColorRect();
		bg.Color = new Color(0f, 0f, 0f, 0.92f);
		bg.AnchorRight = 1f; bg.AnchorBottom = 1f;
		bg.GrowHorizontal = Control.GrowDirection.Both;
		bg.GrowVertical = Control.GrowDirection.Both;
		canvas.AddChild(bg);
		var lbl = new Label();
		lbl.Text = "The estate is silent.\nNo voice remains.";
		lbl.HorizontalAlignment = HorizontalAlignment.Center;
		lbl.VerticalAlignment = VerticalAlignment.Center;
		lbl.AnchorRight = 1f; lbl.AnchorBottom = 1f;
		lbl.GrowHorizontal = Control.GrowDirection.Both;
		lbl.GrowVertical = Control.GrowDirection.Both;
		lbl.AddThemeFontSizeOverride("font_size", 52);
		canvas.AddChild(lbl);
		// Fade in via children (CanvasLayer has no Modulate in Godot 4)
		bg.Modulate = new Color(1f, 1f, 1f, 0f);
		lbl.Modulate = new Color(1f, 1f, 1f, 0f);
		var tw = bg.CreateTween();
		tw.TweenProperty(bg, "modulate", new Color(1f, 1f, 1f, 1f), 2.0);
		tw.Parallel().TweenProperty(lbl, "modulate", new Color(1f, 1f, 1f, 1f), 2.0);
		TriggerRunFailed("party_wipe");
	}

}

