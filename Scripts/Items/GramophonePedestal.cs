using Godot;
using LastWord;
using LastWord.World;
using System;

namespace LastWord.Items;

/// <summary>
/// Pedestal wrapper around the existing <see cref="GramophoneItem"/> (§10 +
/// §12.3). The pedestal owns a <see cref="GramophoneItem"/> child and toggles
/// its Start()/Stop() when a player interacts. Also emits a Tier-1 noise event
/// on each toggle so the Listener gets an immediate "something changed here"
/// ping in addition to the periodic emissions from the gramophone itself.
/// </summary>
public partial class GramophonePedestal : StaticBody3D
{
    [Export] public string ToggleSoundPath { get; set; } = "res://Assets/Items/Audio/gramophone_toggle.wav";
    [Export] public float HoldSeconds { get; set; } = 0.3f;

    [Export] public MeshInstance3D PedestalMesh { get; set; }
    [Export] public GramophoneItem Gramophone { get; set; }
    [Export] public Area3D InteractionTrigger { get; set; }

    private PlayerController _nearbyPlayer;
    private float _holdTimer;

    public override void _Ready()
    {
        if (PedestalMesh == null)
        {
            var mesh = new MeshInstance3D
            {
                Mesh = new BoxMesh { Size = new Vector3(0.45f, 0.95f, 0.45f) },
                Name = "DefaultPedestalMesh",
                Transform = new Transform3D(Basis.Identity, new Vector3(0, 0.475f, 0)),
            };
            AddChild(mesh);
            PedestalMesh = mesh;
        }

        if (Gramophone == null)
        {
            // Look for a GramophoneItem child assigned in the .tscn.
            foreach (Node child in GetChildren())
            {
                if (child is GramophoneItem g)
                {
                    Gramophone = g;
                    break;
                }
            }
        }

        if (Gramophone != null && Gramophone.IsRunning)
        {
            // Caller placed a pre-running gramophone — leave it alone, but
            // ensure we don't double-emit on first toggle.
        }
        else if (Gramophone != null)
        {
            // Stop the auto-started gramophone so the pedestal controls it.
            Gramophone.Stop();
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
        {
            _holdTimer = 0.0f;
            Toggle();
        }
    }

    public void Toggle()
    {
        if (Gramophone == null)
        {
            GD.PushWarning("GramophonePedestal: no GramophoneItem child assigned.");
            return;
        }

        if (Gramophone.IsRunning)
            Gramophone.Stop();
        else
            Gramophone.Start();

        var stream = AudioAssets.Load(ToggleSoundPath);
        if (stream != null)
            AudioAssets.PlayOneShot3D(stream, this, GlobalPosition, "SFX");

        EmitNoiseEvent();
    }

    private void EmitNoiseEvent()
    {
        foreach (Node node in GetTree().GetNodesInGroup("Listener"))
        {
            if (node is ListenerAI listener)
                listener.HearNoise(new ListenerSoundEvent(GlobalPosition, 1, SoundKind.Environment, this));
        }
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
            Shape = new SphereShape3D { Radius = 1.6f },
        };
        trigger.AddChild(shape);
        AddChild(trigger);
        return trigger;
    }
}
