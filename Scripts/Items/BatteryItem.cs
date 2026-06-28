using Godot;
using LastWord;
using System;

namespace LastWord.Items;

/// <summary>
/// Battery pickup (§10 Items & Environmental). Consumed on interact: plays the
/// shared pickup SFX and emits <see cref="BatteryPickedUpEventHandler"/> with
/// the player's peer id so any inventory / flashlight power subsystem can react.
/// There is no inventory system yet; the signal is the integration point.
/// </summary>
public partial class BatteryItem : Node3D
{
    [Export] public float HoldSeconds { get; set; } = 0.3f;
    [Export] public MeshInstance3D BatteryMesh { get; set; }
    [Export] public Area3D InteractionTrigger { get; set; }

    [Signal] public delegate void BatteryPickedUpEventHandler(long peerId);

    private PlayerController _nearbyPlayer;
    private float _holdTimer;
    private bool _consumed;

    public override void _Ready()
    {
        if (BatteryMesh == null)
        {
            var mesh = new MeshInstance3D
            {
                Mesh = new CylinderMesh { TopRadius = 0.04f, BottomRadius = 0.04f, Height = 0.12f },
                Name = "DefaultBatteryMesh",
            };
            AddChild(mesh);
            BatteryMesh = mesh;
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
        if (_consumed) return;
        if (_nearbyPlayer == null || _nearbyPlayer.IsDead)
        {
            _holdTimer = 0.0f;
            return;
        }

        if (!_nearbyPlayer.IsInteracting)
        {
            _holdTimer = 0.0f;
            return;
        }

        _holdTimer += (float)delta;
        if (_holdTimer >= HoldSeconds)
            PickUp(_nearbyPlayer);
    }

    private void PickUp(PlayerController player)
    {
        if (_consumed || player == null) return;
        _consumed = true;

        long peerId = 0;
        if (!long.TryParse(player.Name, out peerId))
            peerId = player.GetMultiplayerAuthority();

        AudioAssets.PlayOneShot3D(AudioAssets.ItemPickup01, this, GlobalPosition, "SFX");
        EmitSignal(SignalName.BatteryPickedUp, peerId);
        QueueFree();
    }

    private void OnBodyEntered(Node3D body)
    {
        if (body is PlayerController player && !player.IsDead)
            _nearbyPlayer = player;
    }

    private void OnBodyExited(Node3D body)
    {
        if (body is PlayerController player && player == _nearbyPlayer)
        {
            _nearbyPlayer = null;
            _holdTimer = 0.0f;
        }
    }

    private Area3D CreateDefaultTrigger()
    {
        var trigger = new Area3D { Name = "InteractionTrigger" };
        var shape = new CollisionShape3D
        {
            Shape = new SphereShape3D { Radius = 1.5f },
        };
        trigger.AddChild(shape);
        AddChild(trigger);
        return trigger;
    }
}
