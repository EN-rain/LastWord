using Godot;
using System.Collections.Generic;

namespace LastWord.World;

/// <summary>
/// Silence Room (§6.4). Audio-isolated in both directions: sounds made inside do not
/// travel out, and external sounds do not reach players inside. The Listener performs
/// an aggressive check if it suspects a player is inside.
/// </summary>
public partial class SilenceRoom : Area3D
{
    [Export] public int Capacity = 2;

    private readonly HashSet<PlayerController> _occupants = new();

    public override void _Ready()
    {
        BodyEntered += OnBodyEntered;
        BodyExited += OnBodyExited;
    }

    private void OnBodyEntered(Node3D body)
    {
        if (body is not PlayerController player || player.IsDead) return;
        if (_occupants.Count >= Capacity) return;
        _occupants.Add(player);
        player.SetAudioIsolated(true);
        GD.Print($"SilenceRoom: {player.Name} entered isolation.");
    }

    private void OnBodyExited(Node3D body)
    {
        if (body is not PlayerController player) return;
        if (_occupants.Remove(player))
        {
            player.SetAudioIsolated(false);
            GD.Print($"SilenceRoom: {player.Name} left isolation.");
        }
    }

    /// <summary>
    /// Called by ListenerAI when it performs an aggressive check. Returns true if
    /// the room is occupied; the Listener should then enter/attack.
    /// </summary>
    public bool AggressiveCheck()
    {
        GD.Print($"SilenceRoom: aggressive check, {_occupants.Count} occupants.");
        return _occupants.Count > 0;
    }
}
