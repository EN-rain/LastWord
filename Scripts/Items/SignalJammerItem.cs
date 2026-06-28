using Godot;
using LastWord;
using System;

namespace LastWord.Items;

/// <summary>
/// Stationary signal jammer (§10 Items & Environmental). Toggled on by a player
/// interact: plays a low hum, enables a small status light, and exposes
/// <see cref="IsPositionJammed"/> so other systems (e.g. a future radio,
/// scanner, or networked item) can query whether their position falls within
/// the jam radius. We do NOT modify ListenerAI here; this item only exposes
/// the API.
/// </summary>
public partial class SignalJammerItem : Node3D
{
    [Export] public string HumSoundPath { get; set; } = "res://Assets/Items/Audio/jammer_hum.ogg";
    [Export] public string ToggleSoundPath { get; set; } = "res://Assets/Items/Audio/jammer_toggle.wav";
    [Export] public float HoldSeconds { get; set; } = 0.3f;
    [Export] public float JamRadius { get; set; } = 8.0f;

    [Export] public MeshInstance3D JammerMesh { get; set; }
    [Export] public OmniLight3D StatusLight { get; set; }
    [Export] public Area3D InteractionTrigger { get; set; }

    private AudioStreamPlayer3D _humPlayer;
    private PlayerController _nearbyPlayer;
    private float _holdTimer;
    private bool _isActive;

    public bool IsActive => _isActive;

    public override void _Ready()
    {
        if (JammerMesh == null)
        {
            var mesh = new MeshInstance3D
            {
                Mesh = new BoxMesh { Size = new Vector3(0.18f, 0.10f, 0.12f) },
                Name = "DefaultJammerMesh",
            };
            AddChild(mesh);
            JammerMesh = mesh;
        }

        if (StatusLight == null)
        {
            var light = new OmniLight3D
            {
                Name = "DefaultStatusLight",
                LightColor = new Color(0.3f, 0.9f, 1.0f),
                LightEnergy = 0.6f,
                OmniRange = 1.5f,
                Visible = false,
            };
            AddChild(light);
            StatusLight = light;
        }

        _humPlayer = new AudioStreamPlayer3D
        {
            Stream = AudioAssets.Load(HumSoundPath),
            Bus = "Ambience",
            MaxDistance = JamRadius,
            Autoplay = false,
            Name = "JammerHum",
        };
        AddChild(_humPlayer);

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
        if (_isActive) Deactivate();
        else Activate();
    }

    public void Activate()
    {
        if (_isActive) return;
        _isActive = true;

        if (StatusLight != null) StatusLight.Visible = true;

        if (_humPlayer != null && _humPlayer.Stream != null)
            _humPlayer.Play();

        var toggle = AudioAssets.Load(ToggleSoundPath);
        if (toggle != null)
            AudioAssets.PlayOneShot3D(toggle, this, GlobalPosition, "SFX");
    }

    public void Deactivate()
    {
        if (!_isActive) return;
        _isActive = false;

        if (StatusLight != null) StatusLight.Visible = false;

        if (_humPlayer != null && _humPlayer.Playing)
            _humPlayer.Stop();

        var toggle = AudioAssets.Load(ToggleSoundPath);
        if (toggle != null)
            AudioAssets.PlayOneShot3D(toggle, this, GlobalPosition, "SFX");
    }

    /// <summary>
    /// Returns <c>true</c> if <paramref name="position"/> is within
    /// <see cref="JamRadius"/> of this jammer AND the jammer is currently
    /// active. Use this from gameplay systems that want to suppress signals.
    /// </summary>
    public bool IsPositionJammed(Vector3 position)
    {
        if (!_isActive) return false;
        return GlobalPosition.DistanceSquaredTo(position) <= JamRadius * JamRadius;
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
