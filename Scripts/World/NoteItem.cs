using Godot;
using LastWord;
using System;

public partial class NoteItem : Node3D
{
    [Export] public string AssignedWord { get; set; } = "";
    [Export] public int Tier { get; set; } = 1;
    [Export] public float PickupHoldSeconds { get; set; } = 0.5f;
    [Export] public MeshInstance3D NoteMesh { get; set; }
    [Export] public Area3D InteractionTrigger { get; set; }

    [Signal] public delegate void NotePickedUpEventHandler(string word, int tier, long peerId);

    private PlayerController _nearbyPlayer;
    private float _pickupTimer;

    public override void _Ready()
    {
        // Simple default visual if NoteMesh not assigned: yellow CSGBox3D
        if (NoteMesh == null)
        {
            var mesh = new MeshInstance3D
            {
                Mesh = new BoxMesh { Size = new Vector3(0.2f, 0.3f, 0.02f) },
                Name = "DefaultNoteMesh"
            };
            AddChild(mesh);
            NoteMesh = mesh;
        }

        if (InteractionTrigger == null)
            InteractionTrigger = CreateDefaultTrigger();

        InteractionTrigger.BodyEntered += OnBodyEntered;
        InteractionTrigger.BodyExited += OnBodyExited;
    }

    public override void _ExitTree()
    {
        if (InteractionTrigger != null)
        {
            InteractionTrigger.BodyEntered -= OnBodyEntered;
            InteractionTrigger.BodyExited -= OnBodyExited;
        }
    }

    public override void _Process(double delta)
    {
        if (_nearbyPlayer == null || _nearbyPlayer.IsDead)
        {
            _pickupTimer = 0f;
            return;
        }

        if (!_nearbyPlayer.IsInteracting)
        {
            _pickupTimer = 0f;
            return;
        }

        _pickupTimer += (float)delta;
        if (_pickupTimer >= PickupHoldSeconds)
            PickUp(_nearbyPlayer);
    }

    // Called by player when looking at note and holding E for 2s while silent (Tier 0)
    public void PickUp(PlayerController player)
    {
        if (player == null) return;
        if (string.IsNullOrEmpty(AssignedWord)) return;

        // PlayerController.Name is set to peerId.ToString() at spawn time
        // (see GameManager.SpawnPlayer). Fall back to multiplayer authority
        // if the name is not numeric (e.g. an editor-placed preview player).
        long peerId = 0;
        if (!long.TryParse(player.Name, out peerId))
            peerId = player.GetMultiplayerAuthority();

        // Mark as picked up so it can't be picked up again
        AudioAssets.PlayOneShot3D(AudioAssets.ItemPickup01, player, player.GlobalPosition, "SFX");
        QueueFree();
        EmitSignal(SignalName.NotePickedUp, AssignedWord, Tier, peerId);
    }

    private Area3D CreateDefaultTrigger()
    {
        var trigger = new Area3D { Name = "InteractionTrigger" };
        var shape = new CollisionShape3D
        {
            Shape = new SphereShape3D { Radius = 1.5f }
        };
        trigger.AddChild(shape);
        AddChild(trigger);
        return trigger;
    }

    private void OnBodyEntered(Node3D body)
    {
        if (body is PlayerController player)
            _nearbyPlayer = player;
    }

    private void OnBodyExited(Node3D body)
    {
        if (body is PlayerController player && player == _nearbyPlayer)
        {
            _nearbyPlayer = null;
            _pickupTimer = 0f;
        }
    }
}
