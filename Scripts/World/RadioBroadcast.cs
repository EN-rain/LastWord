// Scripts/World/RadioBroadcast.cs
// Tracks a sustained voice broadcast into the radio for the final phase.
// Driven externally by GameManager / PlayerController so it can stay a plain Node.

using Godot;

public partial class RadioBroadcast : Node
{
    [Signal] public delegate void BroadcastStartedEventHandler(long peerId);
    [Signal] public delegate void BroadcastProgressEventHandler(float progress);
    [Signal] public delegate void BroadcastCompleteEventHandler();
    [Signal] public delegate void BroadcastFailedEventHandler();

    [Export] public float MonologueDuration = 10.0f;
    [Export] public float SustainedTierRequired = 2;
    [Export] public float GraceTimer = 2.0f;

    public bool IsBroadcasting { get; private set; }
    public long CurrentBroadcasterPeerId { get; private set; }
    public float CurrentProgress { get; private set; }
    public bool IsComplete { get; private set; }

    private float _broadcastProgressTimer = 0.0f;
    private float _graceTimer = 0.0f;

    public void StartBroadcast(long peerId)
    {
        if (IsBroadcasting)
            return;

        CurrentBroadcasterPeerId = peerId;
        IsBroadcasting = true;
        CurrentProgress = 0.0f;
        IsComplete = false;
        _broadcastProgressTimer = 0.0f;
        _graceTimer = GraceTimer;

        EmitSignal(SignalName.BroadcastStarted, peerId);
    }

    public void OnVoiceUpdate(int currentTier, double delta)
    {
        if (!IsBroadcasting || IsComplete)
            return;

        float dt = (float)delta;

        if (currentTier >= SustainedTierRequired)
        {
            _broadcastProgressTimer += dt;
            _graceTimer = GraceTimer;
        }
        else
        {
            _graceTimer -= dt;
            if (_graceTimer <= 0.0f)
            {
                FailBroadcast();
                return;
            }
        }

        CurrentProgress = Mathf.Clamp(_broadcastProgressTimer / MonologueDuration, 0.0f, 1.0f);
        EmitSignal(SignalName.BroadcastProgress, CurrentProgress);

        if (_broadcastProgressTimer >= MonologueDuration)
        {
            CompleteBroadcast();
        }
    }

    public void OnBroadcasterDied(long peerId)
    {
        if (peerId != CurrentBroadcasterPeerId)
            return;

        CurrentBroadcasterPeerId = 0;
        IsBroadcasting = false;
        CurrentProgress = 0.0f;
        _broadcastProgressTimer = 0.0f;
        _graceTimer = GraceTimer;
    }

    private void CompleteBroadcast()
    {
        IsComplete = true;
        IsBroadcasting = false;
        EmitSignal(SignalName.BroadcastComplete);
    }

    private void FailBroadcast()
    {
        IsBroadcasting = false;
        CurrentProgress = 0.0f;
        _broadcastProgressTimer = 0.0f;
        EmitSignal(SignalName.BroadcastFailed);
    }
}
