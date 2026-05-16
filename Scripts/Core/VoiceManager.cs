using Godot;
using System;

public enum VoiceTier
{
    Silent = 0,     // < -50dB
    Whisper = 1,    // -50dB to -35dB
    Normal = 2,     // -35dB to -15dB
    Shouting = 3    // > -15dB
}

public partial class VoiceManager : Node
{
    public static VoiceManager Instance { get; private set; }

    [Signal] public delegate void TierChangedEventHandler(int newTier);
    [Signal] public delegate void VolumeUpdatedEventHandler(float dbValue);
    [Signal] public delegate void TokenTransferredEventHandler(Node3D newHolder);

    [Export] public string MicBusName = "Microphone";
    [Export] public float AnalysisInterval = 0.05f; // How often to check volume

    private int _micBusIndex;
    private AudioStreamPlayer _micInput;
    private VoiceTier _currentTier = VoiceTier.Silent;
    private float _timer = 0f;

    // Token System
    private Node3D _tokenHolder = null;
    public Node3D TokenHolder => _tokenHolder;

    public override void _Ready()
    {
        Instance = this;
        
        _micBusIndex = AudioServer.GetBusIndex(MicBusName);
        if (_micBusIndex == -1)
        {
            GD.PrintErr($"VoiceManager: Audio bus '{MicBusName}' not found. Please create it in the Audio Mixer.");
            return;
        }

        // Setup microphone input
        _micInput = new AudioStreamPlayer();
        AddChild(_micInput);
        _micInput.Bus = MicBusName;
        _micInput.Stream = new AudioStreamMicrophone();
        _micInput.Play();
        
        GD.Print("VoiceManager: Microphone initialized on bus " + MicBusName);
    }

    public override void _Process(double delta)
    {
        if (_micBusIndex == -1) return;

        _timer += (float)delta;
        if (_timer >= AnalysisInterval)
        {
            _timer = 0f;
            AnalyzeVoice();
        }
    }

    private void AnalyzeVoice()
    {
        // Get peak volume from the microphone bus
        float peakDb = AudioServer.GetBusPeakVolumeLeftDb(_micBusIndex, 0);
        
        // Emit raw volume for HUD meters
        EmitSignal(SignalName.VolumeUpdated, peakDb);

        // Classify into Tiers
        VoiceTier newTier = ClassifyTier(peakDb);
        
        if (newTier != _currentTier)
        {
            _currentTier = newTier;
            EmitSignal(SignalName.TierChanged, (int)_currentTier);
            
            // If speaking (Whisper or higher), update Token Holder
            if (_currentTier >= VoiceTier.Whisper)
            {
                UpdateTokenHolder();
            }
        }
    }

    private VoiceTier ClassifyTier(float db)
    {
        if (db < -50f) return VoiceTier.Silent;
        if (db < -35f) return VoiceTier.Whisper;
        if (db < -15f) return VoiceTier.Normal;
        return VoiceTier.Shouting;
    }

    private void UpdateTokenHolder()
    {
        // For now, we assume the local player is the only one
        // In multiplayer, this would check which player is speaking
        Node3D localPlayer = GetTree().GetFirstNodeInGroup("Player") as Node3D;
        
        if (localPlayer != null && _tokenHolder != localPlayer)
        {
            _tokenHolder = localPlayer;
            EmitSignal(SignalName.TokenTransferred, _tokenHolder);
            GD.Print("Token Transferred to: " + localPlayer.Name);
        }
    }
}
