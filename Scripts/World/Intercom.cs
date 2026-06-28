using Godot;
using LastWord.Core;

namespace LastWord.World;

/// <summary>
/// Broken intercom (§6.2 Floor 1, §6.4 Floor 3). Emits a loud burst of static/noise
/// when a player interacts with it, attracting the Listener.
/// </summary>
public partial class Intercom : Area3D
{
    [Export] public float CooldownSeconds = 15.0f;
    [Export] public int NoiseTier = 2;
    [Export] public float NoiseDuration = 2.0f;

    private double _cooldownTimer;
    private bool _isOnCooldown;
    private PlayerController _nearbyPlayer;

    public bool CanTrigger => !_isOnCooldown;

    public override void _Ready()
    {
        BodyEntered += OnBodyEntered;
        BodyExited += OnBodyExited;
    }

    public override void _ExitTree()
    {
        BodyEntered -= OnBodyEntered;
        BodyExited -= OnBodyExited;
    }

    public override void _Process(double delta)
    {
        if (_nearbyPlayer != null && !_nearbyPlayer.IsDead && _nearbyPlayer.IsInteracting)
            Trigger(_nearbyPlayer);

        if (_isOnCooldown)
        {
            _cooldownTimer -= delta;
            if (_cooldownTimer <= 0.0)
            {
                _isOnCooldown = false;
                GD.Print("Intercom: ready.");
            }
        }
    }

    private void OnBodyEntered(Node3D body)
    {
        if (body is not PlayerController player || player.IsDead)
            return;
        _nearbyPlayer = player;
    }

    private void OnBodyExited(Node3D body)
    {
        if (body == _nearbyPlayer)
            _nearbyPlayer = null;
    }

    public void Trigger(PlayerController player)
    {
        if (_isOnCooldown) return;
        _isOnCooldown = true;
        _cooldownTimer = CooldownSeconds;

        VoiceManager.Instance?.ReportNoiseEvent(GlobalPosition, NoiseTier, SoundKind.Special, player);
        AchievementManager.Instance?.UnlockWrongNumber();

        GD.Print($"Intercom: triggered noise event at {GlobalPosition}, tier {NoiseTier}.");
    }
}
