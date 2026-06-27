using Godot;

namespace LastWord.World;

/// <summary>
/// Dead phone (§6.3 rooms 2B, 2D). Interacting reveals a random word/sequence hint
/// but may emit a small noise that alerts the Listener.
/// </summary>
public partial class DeadPhone : Area3D
{
    [Export] public float CooldownSeconds = 20.0f;
    [Export] public int NoiseTier = 1;
    [Export] public string[] PossibleHints { get; set; } = System.Array.Empty<string>();

    private double _cooldownTimer;
    private bool _isOnCooldown;
    private PlayerController _nearbyPlayer;

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
            Use(_nearbyPlayer);

        if (_isOnCooldown)
        {
            _cooldownTimer -= delta;
            if (_cooldownTimer <= 0.0)
            {
                _isOnCooldown = false;
                GD.Print("DeadPhone: ready.");
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

    public void Use(PlayerController player)
    {
        if (_isOnCooldown) return;
        _isOnCooldown = true;
        _cooldownTimer = CooldownSeconds;

        string hint = GetRandomHint();
        GD.Print($"DeadPhone: {player.Name} heard hint '{hint}'.");

        VoiceManager.Instance?.ReportNoiseEvent(GlobalPosition, NoiseTier, SoundKind.Special, player);
    }

    private string GetRandomHint()
    {
        if (PossibleHints == null || PossibleHints.Length == 0)
            return "...static...";
        return PossibleHints[(int)(GD.Randi() % (uint)PossibleHints.Length)];
    }
}
