using Godot;
using System;

public partial class GameManager : Node3D
{
    [Export] public PackedScene PlayerScene;

    private Node  _playersContainer;
    private float _sessionElapsed    = 0.0f;
    private bool  _runLogged         = false;
    private float _orientationTimer  = 0.0f;
    private bool  _orientationActive = false;

    private const float RunLogThreshold         = 300.0f; // 5 minutes
    private const float OrientationDuration     = 210.0f; // 3 min 30 sec

    public override void _Ready()
    {
        CallDeferred(nameof(SetupNavigationRuntime));

        // Ensure PlayerScene is resolved early so we can safely read its ResourcePath
        if (PlayerScene == null)
        {
            PlayerScene = GD.Load<PackedScene>("res://Scenes/Player.tscn");
        }

        _playersContainer = GetNodeOrNull("Players");
        if (_playersContainer == null)
        {
            _playersContainer = new Node3D();
            _playersContainer.Name = "Players";
            AddChild(_playersContainer);
        }

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
            var offlinePlayer = GetNodeOrNull("Player");
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
        if (Multiplayer != null)
            Multiplayer.PeerDisconnected -= OnPeerDisconnected;
    }

    private void SetupNavigationRuntime()
    {
        // Reparent level geometry under the region once the scene tree is fully assembled,
        // then bake the mesh so agents can query a valid map on their first patrol tick.
        var navRegion = GetNodeOrNull<NavigationRegion3D>("NavigationRegion3D");
        if (navRegion == null)
            return;

        ReparentNodeUnder(navRegion, "Plane");
        ReparentNodeUnder(navRegion, "Node3D");

        navRegion.BakeNavigationMesh(false);
        GD.Print("GameManager: Reparented geometry and baked Navigation Mesh.");
    }

    private void ReparentNodeUnder(Node newParent, string childName)
    {
        var node = GetNodeOrNull(childName);
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
    // Multiplayer spawn helpers
    // ---------------------------------------------------------------------------
    [Rpc(MultiplayerApi.RpcMode.AnyPeer, CallLocal = false)]
    private void RegisterReadyClient()
    {
        if (!Multiplayer.IsServer()) return;

        long senderId = Multiplayer.GetRemoteSenderId();
        SpawnPlayer(senderId);
    }

    private void SpawnPlayer(long peerId)
    {
        if (_playersContainer.HasNode(peerId.ToString()))
        {
            return;
        }

        var playerInstance = PlayerScene.Instantiate<Node3D>();
        playerInstance.Name      = peerId.ToString();
        playerInstance.Transform = new Transform3D(Basis.Identity, new Vector3(0, 1, 0));

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
}
