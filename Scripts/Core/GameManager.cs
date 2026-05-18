using Godot;
using System;

public partial class GameManager : Node3D
{
    [Export] public PackedScene PlayerScene;

    private Node _playersContainer;

    public override void _Ready()
    {
        GD.Print("GameManager: _Ready starting...");

        _playersContainer = GetNodeOrNull("Players");
        if (_playersContainer == null)
        {
            _playersContainer = new Node3D();
            _playersContainer.Name = "Players";
            AddChild(_playersContainer);
        }

        // Check if there is an active multiplayer connection
        if (Multiplayer.MultiplayerPeer != null && Multiplayer.HasMultiplayerPeer())
        {
            GD.Print("GameManager: Multiplayer session detected. Activating network spawning...");

            // 1. Remove the static offline player node if it exists to avoid duplicates
            var offlinePlayer = GetNodeOrNull("Player");
            if (offlinePlayer != null)
            {
                GD.Print("GameManager: Removing offline player instance...");
                offlinePlayer.QueueFree();
            }

            // 2. Setup the MultiplayerSpawner dynamically
            MultiplayerSpawner spawner = new MultiplayerSpawner();
            spawner.SpawnPath = _playersContainer.GetPath();
            spawner.AddSpawnableScene("res://Scenes/Player.tscn");
            AddChild(spawner);

            // 3. Only the server/host handles spawning player instances
            if (Multiplayer.IsServer())
            {
                Multiplayer.PeerConnected += OnPeerConnected;
                Multiplayer.PeerDisconnected += OnPeerDisconnected;

                // Spawn host player character (Peer ID 1)
                SpawnPlayer(1);

                // Spawn characters for clients already in session
                foreach (var peerId in Multiplayer.GetPeers())
                {
                    SpawnPlayer(peerId);
                }
            }
        }
        else
        {
            GD.Print("GameManager: Offline single-player mode. Utilizing default static player node.");
        }
    }

    private void SpawnPlayer(long peerId)
    {
        GD.Print($"GameManager: Spawning character node for peer {peerId}");
        if (PlayerScene == null)
        {
            PlayerScene = GD.Load<PackedScene>("res://Scenes/Player.tscn");
        }

        var playerInstance = PlayerScene.Instantiate<Node3D>();
        playerInstance.Name = peerId.ToString();
        playerInstance.Transform = new Transform3D(Basis.Identity, new Vector3(0, 1, 0));
        
        _playersContainer.AddChild(playerInstance);
    }

    private void DespawnPlayer(long peerId)
    {
        GD.Print($"GameManager: Despawning character node for peer {peerId}");
        var playerNode = _playersContainer.GetNodeOrNull(peerId.ToString());
        if (playerNode != null)
        {
            playerNode.QueueFree();
        }
    }

    private void OnPeerConnected(long peerId)
    {
        SpawnPlayer(peerId);
    }

    private void OnPeerDisconnected(long peerId)
    {
        DespawnPlayer(peerId);
    }
}
