using Godot;
using LastWord;
using System;

namespace LastWord.World;

/// <summary>
/// Old Gramophone (§10.1 + §12.3). Plays a crackled music loop and emits a
/// continuous Tier-1 environmental sound event at its position so the Listener
/// hears it and pathfinds to investigate (the Listener cannot see a stationary
/// player but WILL investigate a fixed Tier-1 source per §3.3 / §10.1).
///
/// Scope note: this is the *audio + noise-event* component of the gramophone
/// item. The full item §10.1 (pickup, charges, inventory, UI hint) is out of
/// scope here — place this node in the scene with a static mesh and it will
/// behave correctly for the audio side of §12.3.
/// </summary>
public partial class GramophoneItem : Node3D
{
    [Export] public int NoiseTier = 1;
    [Export] public float LifetimeSeconds = 90.0f;
    /// <summary>Re-emit the noise event every N seconds so the Listener keeps
    /// investigating even after the first Tier-1 burst decays.</summary>
    [Export] public float NoiseRepeatInterval = 15.0f;
    [Export] public float MaxAudibleDistance = 15.0f;

    [Signal] public delegate void StartedEventHandler();
    [Signal] public delegate void StoppedEventHandler();

    private AudioStreamPlayer3D _player;
    private float _lifeRemaining;
    private float _noiseRepeatTimer;
    private bool _running;

    public bool IsRunning => _running;

    public override void _Ready()
    {
        AddToGroup("Gramophone");

        _player = new AudioStreamPlayer3D
        {
            Stream = AudioAssets.Load(AudioAssets.GramophoneMusicLoop),
            Bus = "Ambience",
            MaxDistance = MaxAudibleDistance,
            UnitSize = 1.0f,
            Autoplay = false,
        };
        AddChild(_player);

        _lifeRemaining = LifetimeSeconds;
        _noiseRepeatTimer = 0.5f; // small initial delay so the loop starts first

        // Auto-start: when placed in a scene the gramophone is on. Future item
        // pickup logic (§10.1) can flip this off via Stop().
        Start();
    }

    public override void _Process(double delta)
    {
        if (!_running)
            return;

        _lifeRemaining -= (float)delta;
        if (_lifeRemaining <= 0.0f)
        {
            Stop();
            return;
        }

        _noiseRepeatTimer -= (float)delta;
        if (_noiseRepeatTimer <= 0.0f)
        {
            EmitNoiseEvent();
            _noiseRepeatTimer = NoiseRepeatInterval;
        }
    }

    public void Start()
    {
        if (_running || _player == null || _player.Stream == null)
            return;

        _running = true;
        _player.Play();
        EmitSignal(SignalName.Started);
        GD.Print($"GramophoneItem at {GlobalPosition}: started ({LifetimeSeconds:F0}s lifetime).");
    }

    public void Stop()
    {
        if (!_running)
            return;

        _running = false;
        if (_player != null && _player.Playing)
            _player.Stop();

        EmitSignal(SignalName.Stopped);
        GD.Print($"GramophoneItem at {GlobalPosition}: stopped.");
    }

    private void EmitNoiseEvent()
    {
        foreach (Node node in GetTree().GetNodesInGroup("Listener"))
        {
            if (node is ListenerAI listener)
                listener.HearNoise(new ListenerSoundEvent(GlobalPosition, NoiseTier, SoundKind.Environment, this, isSpecialLongRange: true));
        }
    }
}
