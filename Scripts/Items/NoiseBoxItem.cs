using Godot;
using LastWord;
using System;

namespace LastWord.Items;

/// <summary>
/// Stationary noise box / decoy radio (§10 Items & Environmental). Toggled on
/// by a player interact: loops a noise SFX and emits Tier-2 Environment sound
/// events every <see cref="NoiseInterval"/> seconds so the Listener investigates
/// it. Toggleable off again with another interact.
/// </summary>
public partial class NoiseBoxItem : Node3D
{
    [Export] public string LoopSoundPath { get; set; } = "res://Assets/Items/Audio/noise_box_loop.ogg";
    [Export] public string ToggleSoundPath { get; set; } = "res://Assets/Items/Audio/noise_box_toggle.wav";
    [Export] public float HoldSeconds { get; set; } = 0.3f;
    [Export] public float NoiseInterval { get; set; } = 2.5f;
    [Export] public int NoiseTier { get; set; } = 2;

    [Export] public MeshInstance3D BoxMesh { get; set; }
    [Export] public Area3D InteractionTrigger { get; set; }

    private AudioStreamPlayer3D _loopPlayer;
    private PlayerController _nearbyPlayer;
    private float _holdTimer;
    private float _noiseTimer;
    private bool _isOn;

    public bool IsOn => _isOn;

    public override void _Ready()
    {
        if (BoxMesh == null)
        {
            var mesh = new MeshInstance3D
            {
                Mesh = new BoxMesh { Size = new Vector3(0.3f, 0.2f, 0.15f) },
                Name = "DefaultBoxMesh",
            };
            AddChild(mesh);
            BoxMesh = mesh;
        }

        _loopPlayer = new AudioStreamPlayer3D
        {
            Stream = AudioAssets.Load(LoopSoundPath),
            Bus = "Ambience",
            MaxDistance = 12.0f,
            Autoplay = false,
            Name = "NoiseBoxLoop",
        };
        AddChild(_loopPlayer);

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
        float dt = (float)delta;

        // Player interaction / toggle.
        if (_nearbyPlayer != null && !_nearbyPlayer.IsDead && _nearbyPlayer.IsInteracting)
        {
            _holdTimer += dt;
            if (_holdTimer >= HoldSeconds)
            {
                _holdTimer = 0.0f;
                Toggle();
            }
        }
        else
        {
            _holdTimer = 0.0f;
        }

        // Periodic noise events while running.
        if (_isOn)
        {
            _noiseTimer -= dt;
            if (_noiseTimer <= 0.0f)
            {
                EmitNoiseEvent();
                _noiseTimer = NoiseInterval;
            }
        }
    }

    public void Toggle()
    {
        if (_isOn) Stop();
        else Start();
    }

    public void Start()
    {
        if (_isOn) return;
        _isOn = true;
        _noiseTimer = 0.5f;

        if (_loopPlayer != null && _loopPlayer.Stream != null)
            _loopPlayer.Play();

        var toggle = AudioAssets.Load(ToggleSoundPath);
        if (toggle != null)
            AudioAssets.PlayOneShot3D(toggle, this, GlobalPosition, "SFX");

        EmitNoiseEvent(); // immediate first event so the listener investigates
    }

    public void Stop()
    {
        if (!_isOn) return;
        _isOn = false;

        if (_loopPlayer != null && _loopPlayer.Playing)
            _loopPlayer.Stop();

        var toggle = AudioAssets.Load(ToggleSoundPath);
        if (toggle != null)
            AudioAssets.PlayOneShot3D(toggle, this, GlobalPosition, "SFX");
    }

    private void EmitNoiseEvent()
    {
        foreach (Node node in GetTree().GetNodesInGroup("Listener"))
        {
            if (node is ListenerAI listener)
                listener.HearNoise(new ListenerSoundEvent(GlobalPosition, NoiseTier, SoundKind.Environment, this));
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
