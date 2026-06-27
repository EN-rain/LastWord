using Godot;
using LastWord;
using System;

namespace LastWord.World;

/// <summary>
/// Clock Tower bell (§6.5). Rings every 5 minutes, providing a 4-second window
/// during which moderate noises are masked from the Listener.
/// </summary>
public partial class ClockBell : Node3D
{
    [Export] public double IntervalSeconds = 300.0;
    [Export] public double MaskWindowSeconds = 4.0;
    [Export] public int NoiseTier = 2;

    [Signal] public delegate void BellRangEventHandler(double maskWindowEndTime);

    private double _timer;
    private double _maskEndTime = -1.0;
    public bool IsMaskActive => Time.GetTicksMsec() / 1000.0 < _maskEndTime;

    public override void _Ready()
    {
        AddToGroup("ClockBell");
        _timer = IntervalSeconds;
    }

    public override void _Process(double delta)
    {
        _timer -= delta;
        if (_timer <= 0.0)
        {
            Ring();
            _timer = IntervalSeconds;
        }
    }

    public void Ring()
    {
        double now = Time.GetTicksMsec() / 1000.0;
        _maskEndTime = now + MaskWindowSeconds;
        EmitSignal(SignalName.BellRang, _maskEndTime);

        AudioAssets.PlayOneShot3D(AudioAssets.ClockBell, this, GlobalPosition, "Ambience");

        foreach (Node node in GetTree().GetNodesInGroup("Listener"))
        {
            if (node is ListenerAI listener)
                listener.HearNoise(new ListenerSoundEvent(GlobalPosition, NoiseTier, SoundKind.Environment, this));
        }

        GD.Print($"ClockBell: rang at {now}, mask active until {_maskEndTime}.");
    }
}
