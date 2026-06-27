using Godot;
using System;

public partial class RadioItem : Node3D
{
    [Export] public float PickupDelay { get; set; } = 1.0f;
    [Export] public float GracePeriod { get; set; } = 2.0f;
    [Export] public MeshInstance3D RadioMesh { get; set; }
    [Export] public Area3D InteractionTrigger { get; set; }

    [Signal] public delegate void RadioPickedUpEventHandler(long peerId, bool isGracePickup);

    private PlayerController _nearbyPlayer;
    private float _pickupProgressTimer = 0.0f;
    private bool _pickupUnlocked = false;
    private float _graceTimer = 0.0f;

    public override void _Ready()
    {
        AddToGroup("RadioItem");

        if (RadioMesh == null)
        {
            var mesh = new MeshInstance3D
            {
                Mesh = new BoxMesh { Size = new Vector3(0.25f, 0.15f, 0.1f) },
                Name = "DefaultRadioMesh"
            };
            AddChild(mesh);
            RadioMesh = mesh;
        }

        if (InteractionTrigger != null)
        {
            InteractionTrigger.BodyEntered += OnBodyEntered;
            InteractionTrigger.BodyExited += OnBodyExited;
        }
        else
        {
            InteractionTrigger = CreateDefaultTrigger();
            InteractionTrigger.BodyEntered += OnBodyEntered;
            InteractionTrigger.BodyExited += OnBodyExited;
        }
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
        if (_nearbyPlayer == null)
            return;

        bool isInteracting = _nearbyPlayer.IsInteracting;
        float dt = (float)delta;

        if (!_pickupUnlocked)
        {
            if (isInteracting)
            {
                _pickupProgressTimer += dt;
            }
            else
            {
                _pickupProgressTimer = Math.Max(0.0f, _pickupProgressTimer - dt);
            }

            if (_pickupProgressTimer >= PickupDelay)
            {
                _pickupUnlocked = true;
                _graceTimer = GracePeriod;
            }
        }
        else
        {
            if (isInteracting)
            {
                ExecutePickup(_graceTimer > 0.0f);
                return;
            }
            else
            {
                _graceTimer -= dt;
            }
        }
    }

    private void OnBodyEntered(Node3D body)
    {
        if (body is PlayerController player)
        {
            _nearbyPlayer = player;
        }
    }

    private void OnBodyExited(Node3D body)
    {
        if (body is PlayerController player && player == _nearbyPlayer)
        {
            _nearbyPlayer = null;
            _pickupProgressTimer = 0.0f;
            _pickupUnlocked = false;
            _graceTimer = 0.0f;
        }
    }

    private void ExecutePickup(bool isGracePickup)
    {
        long peerId = 0;
        if (!long.TryParse(_nearbyPlayer.Name, out peerId))
            peerId = _nearbyPlayer.GetMultiplayerAuthority();

        EmitSignal(SignalName.RadioPickedUp, peerId, isGracePickup);
        QueueFree();
    }

    private Area3D CreateDefaultTrigger()
    {
        var trigger = new Area3D { Name = "InteractionTrigger" };
        var shape = new CollisionShape3D
        {
            Shape = new SphereShape3D { Radius = 1.8f }
        };
        trigger.AddChild(shape);
        AddChild(trigger);
        return trigger;
    }
}
