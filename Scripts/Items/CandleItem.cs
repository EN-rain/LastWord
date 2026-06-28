using Godot;
using LastWord;
using System;

namespace LastWord.Items;

/// <summary>
/// Tabletop candle (§10 Items & Environmental). Smaller and shorter-ranged
/// than <see cref="TorchItem"/>. Toggleable on interact: ignites (plays sound
/// null-safe, emits Tier-1 noise), extinguishes on next interact. Repeatable.
/// </summary>
public partial class CandleItem : Node3D
{
    [Export] public string IgniteSoundPath { get; set; } = "res://Assets/Items/Audio/candle_ignite.wav";
    [Export] public float HoldSeconds { get; set; } = 0.3f;
    [Export] public float LightEnergy { get; set; } = 0.8f;
    [Export] public Color LightColor { get; set; } = new Color(1.0f, 0.8f, 0.45f);
    [Export] public float LightRange { get; set; } = 3.5f;

    [Export] public MeshInstance3D CandleMesh { get; set; }
    [Export] public MeshInstance3D FlameMesh { get; set; }
    [Export] public OmniLight3D FlameLight { get; set; }
    [Export] public Area3D InteractionTrigger { get; set; }

    public bool IsLit { get; private set; }

    private PlayerController _nearbyPlayer;
    private float _holdTimer;

    public override void _Ready()
    {
        if (CandleMesh == null)
        {
            var mesh = new MeshInstance3D
            {
                Mesh = new CylinderMesh { TopRadius = 0.04f, BottomRadius = 0.05f, Height = 0.18f },
                Name = "DefaultCandleMesh",
            };
            AddChild(mesh);
            CandleMesh = mesh;
        }

        if (FlameMesh == null)
        {
            var flame = new MeshInstance3D
            {
                Mesh = new SphereMesh { Radius = 0.025f, Height = 0.06f },
                Name = "DefaultFlameMesh",
                Visible = false,
            };
            AddChild(flame);
            FlameMesh = flame;
        }

        if (FlameLight == null)
        {
            var light = new OmniLight3D
            {
                Name = "DefaultFlameLight",
                LightColor = LightColor,
                LightEnergy = LightEnergy,
                OmniRange = LightRange,
                Visible = false,
            };
            AddChild(light);
            FlameLight = light;
        }

        if (InteractionTrigger == null)
            InteractionTrigger = CreateDefaultTrigger();

        InteractionTrigger.BodyEntered += OnBodyEntered;
        InteractionTrigger.BodyExited += OnBodyExited;

        ApplyLitState();
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
        IsLit = !IsLit;
        ApplyLitState();

        if (IsLit)
        {
            var stream = AudioAssets.Load(IgniteSoundPath);
            if (stream != null)
                AudioAssets.PlayOneShot3D(stream, this, GlobalPosition, "SFX");
            EmitNoiseEvent();
        }
    }

    public void SetLit(bool lit)
    {
        if (IsLit == lit) return;
        IsLit = lit;
        ApplyLitState();
    }

    private void ApplyLitState()
    {
        if (FlameLight != null) FlameLight.Visible = IsLit;
        if (FlameMesh != null) FlameMesh.Visible = IsLit;
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
            Shape = new SphereShape3D { Radius = 1.2f },
        };
        trigger.AddChild(shape);
        AddChild(trigger);
        return trigger;
    }
}
