using Godot;
using LastWord;
using System;

namespace LastWord.Items;

/// <summary>
/// One-shot throwable lighter (§10 Items & Environmental). When a player picks
/// it up we play an ignite sound, emit a Tier-1 noise event so the Listener
/// hears the click, and spawn a short-lived <see cref="OmniLight3D"/> that
/// follows the player for <see cref="DurationSeconds"/>. The lighter itself
/// is consumed.
/// </summary>
public partial class LighterItem : Node3D
{
    [Export] public string IgniteSoundPath { get; set; } = "res://Assets/Items/Audio/lighter_ignite.wav";
    [Export] public float HoldSeconds { get; set; } = 0.4f;
    [Export] public float DurationSeconds { get; set; } = 8.0f;
    [Export] public float LightEnergy { get; set; } = 1.2f;
    [Export] public Color LightColor { get; set; } = new Color(1.0f, 0.75f, 0.45f);
    [Export] public Vector3 LightLocalOffset { get; set; } = new Vector3(0.0f, 1.4f, 0.0f);

    [Export] public MeshInstance3D LighterMesh { get; set; }
    [Export] public Area3D InteractionTrigger { get; set; }

    private PlayerController _nearbyPlayer;
    private float _holdTimer;
    private bool _consumed;

    public override void _Ready()
    {
        if (LighterMesh == null)
        {
            var mesh = new MeshInstance3D
            {
                Mesh = new BoxMesh { Size = new Vector3(0.06f, 0.10f, 0.02f) },
                Name = "DefaultLighterMesh",
            };
            AddChild(mesh);
            LighterMesh = mesh;
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
            Consume(_nearbyPlayer);
    }

    private void Consume(PlayerController player)
    {
        if (_consumed) return;
        _consumed = true;

        // Play ignite (null-safe). We attach to player so the sound follows them.
        var stream = AudioAssets.Load(IgniteSoundPath);
        if (stream != null)
            AudioAssets.PlayOneShot3D(stream, this, GlobalPosition, "SFX");

        EmitNoiseEvent();
        SpawnFollowingLight(player);

        // Lighter itself is consumed immediately.
        QueueFree();
    }

    private void SpawnFollowingLight(PlayerController player)
    {
        if (player == null || !IsInstanceValid(player)) return;

        var light = new OmniLight3D
        {
            Name = "LighterLight",
            LightColor = LightColor,
            LightEnergy = LightEnergy,
            OmniRange = 4.0f,
            Position = LightLocalOffset,
        };

        // Reparent to the player so the light follows them.
        player.AddChild(light);

        var timer = GetTree().CreateTimer(DurationSeconds);
        timer.Timeout += () =>
        {
            if (IsInstanceValid(light))
                light.QueueFree();
        };
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
            Shape = new SphereShape3D { Radius = 1.5f },
        };
        trigger.AddChild(shape);
        AddChild(trigger);
        return trigger;
    }
}
