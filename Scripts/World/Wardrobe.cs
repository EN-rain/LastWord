using Godot;
using LastWord;

namespace LastWord.World;

/// <summary>
/// Wardrobe hiding spot (§6.3 room 2A). Player enters and is muted for 20 seconds.
/// After 5 minutes the Listener may perform an aggressive check of the wardrobe.
/// </summary>
public partial class Wardrobe : Area3D
{
    [Export] public float MuteDuration = 20.0f;
    [Export] public float RiskDelay = 300.0f;

    private PlayerController _occupant;
    private double _occupantTime;
    private bool _riskTriggered;

    public override void _Ready()
    {
        BodyEntered += OnBodyEntered;
        BodyExited += OnBodyExited;
    }

    public override void _Process(double delta)
    {
        if (_occupant == null || _occupant.IsDead) return;
        _occupantTime += delta;

        // Mute is refreshed while inside.
        if (_occupantTime <= MuteDuration)
        {
            _occupant.SetSilenced(true);
        }
        else if (_occupantTime <= RiskDelay)
        {
            _occupant.SetSilenced(false);
        }
        else if (!_riskTriggered)
        {
            _riskTriggered = true;
            TriggerRiskCheck();
        }
    }

    private void OnBodyEntered(Node3D body)
    {
        if (body is not PlayerController player || player.IsDead) return;
        if (_occupant != null) return;
        _occupant = player;
        _occupantTime = 0.0;
        _riskTriggered = false;
        player.SetSilenced(true);
        AudioAssets.PlayOneShot3D(AudioAssets.DoorOpen01, this, GlobalPosition, "SFX");
        GD.Print($"Wardrobe: {player.Name} entered hiding.");
    }

    private void OnBodyExited(Node3D body)
    {
        if (body is not PlayerController player) return;
        if (player != _occupant) return;
        player.SetSilenced(false);
        _occupant = null;
        _occupantTime = 0.0;
        _riskTriggered = false;
        AudioAssets.PlayOneShot3D(AudioAssets.DoorClose01, this, GlobalPosition, "SFX");
        GD.Print($"Wardrobe: {player.Name} exited hiding.");
    }

    private void TriggerRiskCheck()
    {
        if (_occupant == null) return;
        GD.Print($"Wardrobe: aggressive risk check on {_occupant.Name}.");
        foreach (Node node in GetTree().GetNodesInGroup("Listener"))
        {
            if (node is ListenerAI listener)
                listener.HearNoise(new ListenerSoundEvent(GlobalPosition, 1, SoundKind.Special, _occupant, true));
        }
    }
}
