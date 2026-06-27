using Godot;

namespace LastWord.World;

/// <summary>
/// Final Broadcast radio (§6.5). Can be picked up by a player and carried to the
/// RegistrationBoard to transmit the last words and win the round.
/// </summary>
public partial class Radio : Node3D
{
    [Export] public NodePath RegistrationBoardPath;
    [Export] public NodePath RadioBroadcastPath;
    [Export] public float InteractionRadius = 2.0f;

    [Signal] public delegate void PickedUpEventHandler(PlayerController carrier);
    [Signal] public delegate void TransmittedEventHandler(PlayerController carrier);

    public PlayerController Carrier { get; private set; }
    public bool IsHeld => Carrier != null;

    private RegistrationBoard _board;
    private RadioBroadcast _broadcast;

    public override void _Ready()
    {
        _board = GetNodeOrNull<RegistrationBoard>(RegistrationBoardPath);
        _board ??= GetTree()?.GetFirstNodeInGroup("RegistrationBoard") as RegistrationBoard;
        _broadcast = GetNodeOrNull<RadioBroadcast>(RadioBroadcastPath);
        _broadcast ??= GameManager.Instance?.GetNodeOrNull<RadioBroadcast>(GameManager.Instance.RadioBroadcastPath);
        if (_broadcast != null)
        {
            _broadcast.BroadcastComplete += OnBroadcastComplete;
            _broadcast.BroadcastFailed += OnBroadcastFailed;
        }
    }

    public override void _ExitTree()
    {
        if (_broadcast != null)
        {
            _broadcast.BroadcastComplete -= OnBroadcastComplete;
            _broadcast.BroadcastFailed -= OnBroadcastFailed;
        }
    }

    public override void _Process(double delta)
    {
        if (Carrier == null || _board == null) return;

        float dist = Carrier.GlobalPosition.DistanceTo(_board.GlobalPosition);
        if (dist <= InteractionRadius)
        {
            TryTransmit();
        }
    }

    public void PickUp(PlayerController player)
    {
        if (Carrier != null || player.IsDead) return;
        Carrier = player;
        GetParent()?.RemoveChild(this);
        player.AddChild(this);
        Position = new Vector3(0f, 1f, 0.5f);
        EmitSignal(SignalName.PickedUp, player);
        GD.Print($"Radio: picked up by {player.Name}.");
    }

    public void TryTransmit()
    {
        if (Carrier == null) return;
        if (_broadcast != null && !_broadcast.IsBroadcasting && !_broadcast.IsComplete)
        {
            long peerId = 0;
            if (!long.TryParse(Carrier.Name, out peerId))
                peerId = Carrier.GetMultiplayerAuthority();
            _broadcast.StartBroadcast(peerId);
            GD.Print($"Radio: started final broadcast from {Carrier.Name}.");
        }
    }

    private void OnBroadcastComplete()
    {
        if (Carrier == null) return;
        EmitSignal(SignalName.Transmitted, Carrier);
        GameManager.Instance?.OnFinalBroadcastTransmitted(Carrier);
    }

    private void OnBroadcastFailed()
    {
        GD.Print("Radio: final broadcast failed — retry required.");
    }

    public void Drop()
    {
        if (Carrier == null) return;
        var carrier = Carrier;
        Carrier = null;
        carrier.RemoveChild(this);
        GetTree().CurrentScene.AddChild(this);
        GlobalPosition = carrier.GlobalPosition + carrier.GlobalTransform.Basis.Z * 0.5f;
        GD.Print($"Radio: dropped by {carrier.Name}.");
    }
}
