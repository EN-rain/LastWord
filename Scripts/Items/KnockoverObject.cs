using Godot;
using LastWord;
using System;

namespace LastWord.Items;

/// <summary>
/// Knockover / physics prop (§10 Items & Environmental). A
/// <see cref="RigidBody3D"/> that listens for high-speed collisions and emits
/// a Tier-1 Environment sound event with a small cooldown so one tumble does
/// not spam the Listener. Plays a random knockover-style impact sound.
/// </summary>
public partial class KnockoverObject : RigidBody3D
{
    [Export] public float NoiseVelocityThreshold { get; set; } = 2.5f;
    [Export] public float NoiseCooldownSeconds { get; set; } = 0.75f;
    [Export] public string[] KnockoverSoundPaths { get; set; } = new[]
    {
        "res://Assets/Items/Audio/knockover_01.ogg",
        "res://Assets/Items/Audio/knockover_02.ogg",
    };
    [Export] public MeshInstance3D ObjectMesh { get; set; }

    private float _noiseCooldownRemaining;
    private RandomNumberGenerator _rng = new RandomNumberGenerator();

    public override void _Ready()
    {
        _rng.Randomize();

        if (ObjectMesh == null)
        {
            var mesh = new MeshInstance3D
            {
                Mesh = new CylinderMesh { TopRadius = 0.07f, BottomRadius = 0.09f, Height = 0.45f },
                Name = "DefaultObjectMesh",
            };
            AddChild(mesh);
            ObjectMesh = mesh;
        }

        BodyEntered += OnBodyEntered;
    }

    public override void _ExitTree()
    {
        BodyEntered -= OnBodyEntered;
    }

    public override void _Process(double delta)
    {
        if (_noiseCooldownRemaining > 0.0f)
            _noiseCooldownRemaining = Math.Max(0.0f, _noiseCooldownRemaining - (float)delta);
    }

    private void OnBodyEntered(Node body)
    {
        if (body == null || body == this) return;
        if (_noiseCooldownRemaining > 0.0f) return;

        float speed = LinearVelocity.Length();
        if (speed < NoiseVelocityThreshold) return;

        _noiseCooldownRemaining = NoiseCooldownSeconds;

        PlayRandomImpact();
        EmitNoiseEvent(speed);
    }

    private void PlayRandomImpact()
    {
        if (KnockoverSoundPaths == null || KnockoverSoundPaths.Length == 0)
        {
            // Fallback to the shared world impact SFX.
            AudioAssets.PlayOneShot3D(AudioAssets.Impact01, this, GlobalPosition, "SFX");
            return;
        }

        int index = _rng.RandiRange(0, KnockoverSoundPaths.Length - 1);
        var path = KnockoverSoundPaths[index];

        var stream = AudioAssets.Load(path);
        if (stream != null)
        {
            AudioAssets.PlayOneShot3D(stream, this, GlobalPosition, "SFX");
        }
        else
        {
            // Fallback if neither custom nor loaded stream is available.
            AudioAssets.PlayOneShot3D(AudioAssets.Impact01, this, GlobalPosition, "SFX");
        }
    }

    private void EmitNoiseEvent(float speed)
    {
        foreach (Node node in GetTree().GetNodesInGroup("Listener"))
        {
            if (node is ListenerAI listener)
                listener.HearNoise(new ListenerSoundEvent(GlobalPosition, 1, SoundKind.Environment, this));
        }
    }
}
