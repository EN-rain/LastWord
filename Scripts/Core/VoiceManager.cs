using Godot;
using System;
using System.Collections.Generic;
using System.Linq;

public enum VoiceTier
{
    Silent = 0,     // < -50dB  (RMS)
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
    [Signal] public delegate void CalibrationProgressEventHandler(float progress); // 0.0 to 1.0
    [Signal] public delegate void CalibrationFinishedEventHandler(float newBaseline);

    [Export] public string MicBusName = "Microphone";
    [Export] public float AnalysisInterval = 0.05f;

    private int _micBusIndex = -1;
    private AudioStreamPlayer _micInput;
    private AudioEffectCapture _captureEffect;

    private VoiceTier _currentTier = VoiceTier.Silent;
    private float _timer = 0f;
    private float _smoothedRms = 0f;
    private const float SmoothingFactor = 0.2f; // 0.0 to 1.0 (lower = smoother/slower)

    // Calibration
    public float BaselineAmplitude { get; set; } = 0.05f; // Default baseline
    public bool IsCalibrating { get; private set; } = false;
    private float _calibrationTimer = 0f;
    private System.Collections.Generic.List<float> _calibrationSamples = new();
    private const float CalibrationDuration = 3.0f;

    // Token System
    private Node3D _tokenHolder = null;
    public Node3D TokenHolder => _tokenHolder;
    private long _tokenHolderPeerId = 0;
    public long TokenHolderPeerId => _tokenHolderPeerId;

    public override void _EnterTree()
    {
        Instance = this;
    }

    public override async void _Ready()
    {
        GD.Print("VoiceManager: _Ready starting...");
        LoadBaseline();

        // Wait a few frames for the AudioServer and Windows drivers to stabilize
        await ToSignal(GetTree().CreateTimer(0.5f), "timeout");
        
        SetupMicBus();
        SetupMicInput();
        GD.Print("VoiceManager: _Ready finished.");
    }

    private void SetupMicBus()
    {
        _micBusIndex = AudioServer.GetBusIndex(MicBusName);

        if (_micBusIndex == -1)
        {
            GD.Print($"VoiceManager: Creating new bus '{MicBusName}'...");
            AudioServer.AddBus();
            _micBusIndex = AudioServer.BusCount - 1;
            AudioServer.SetBusName(_micBusIndex, MicBusName);
            AudioServer.SetBusSend(_micBusIndex, "Master");
        }
        
        // Ensure volume is up and bus is 'technically' muted to prevent loopback
        AudioServer.SetBusVolumeDb(_micBusIndex, 0);
        AudioServer.SetBusMute(_micBusIndex, true); 

        // Ensure an AudioEffectCapture is on the bus so we can read audio frames
        _captureEffect = null;
        for (int i = 0; i < AudioServer.GetBusEffectCount(_micBusIndex); i++)
        {
            if (AudioServer.GetBusEffect(_micBusIndex, i) is AudioEffectCapture existing)
            {
                _captureEffect = existing;
                GD.Print("VoiceManager: Found existing AudioEffectCapture.");
                break;
            }
        }

        if (_captureEffect == null)
        {
            var effect = new AudioEffectCapture();
            effect.BufferLength = 0.1f;
            AudioServer.AddBusEffect(_micBusIndex, effect);
            // The resource itself is the object we query — no separate instance needed
            int idx = AudioServer.GetBusEffectCount(_micBusIndex) - 1;
            _captureEffect = (AudioEffectCapture)AudioServer.GetBusEffect(_micBusIndex, idx);
            GD.Print($"VoiceManager: Added AudioEffectCapture to bus '{MicBusName}'.");
        }

        GD.Print($"VoiceManager: Bus '{MicBusName}' ready at index {_micBusIndex}.");
    }

    private void SetupMicInput()
    {
        GD.Print("\n--- VoiceManager: Initializing Audio Input ---");
        
        string[] inputDevices = AudioServer.GetInputDeviceList();
        
        // Validation: If the current device is missing, fallback to Default
        bool deviceFound = false;
        foreach (var dev in inputDevices)
        {
            if (dev == AudioServer.InputDevice)
            {
                deviceFound = true;
                break;
            }
        }

        if (!deviceFound && AudioServer.InputDevice != "Default")
        {
            GD.Print($"VoiceManager: Saved device '{AudioServer.InputDevice}' not found. Resetting to 'Default'.");
            AudioServer.InputDevice = "Default";
        }

        GD.Print("Available Input Devices:");
        foreach (var dev in inputDevices) GD.Print($"- {dev}");
        GD.Print($"Current Active Device: {AudioServer.InputDevice}");

        try
        {
            if (_micInput == null)
            {
                _micInput = new AudioStreamPlayer();
                _micInput.Name = "MicInputPlayer";
                AddChild(_micInput);
            }

            _micInput.Bus = MicBusName;
            _micInput.Stream = new AudioStreamMicrophone();
            _micInput.Play();
            
            GD.Print("VoiceManager: MicStreamPlayer started successfully.");
        }
        catch (Exception e)
        {
            GD.PrintErr($"VoiceManager: CRITICAL ERROR starting mic: {e.Message}");
        }
    }

    // ------------------------------------------------------------------ //
    //  Called by PauseMenu when the mic device changes
    // ------------------------------------------------------------------ //
    public void RestartCapture()
    {
        GD.Print("VoiceManager: Restarting microphone capture...");
        
        if (_micInput != null)
        {
            _micInput.Stop();
            _micInput.Stream = null;
            // Force a small delay or reconstruction
            _micInput.Stream = new AudioStreamMicrophone();
            _micInput.Play();
        }
        
        SetupMicBus();
        _currentTier = VoiceTier.Silent; // Force re-classification
        GD.Print("VoiceManager: Capture restarted.");
    }

    // ------------------------------------------------------------------ //
    //  Process
    // ------------------------------------------------------------------ //
    public override void _Process(double delta)
    {
        _timer += (float)delta;
        if (_timer >= AnalysisInterval)
        {
            float dt = _timer;
            _timer = 0f;
            AnalyzeVoice(dt);
        }
    }

    public void StartCalibration()
    {
        IsCalibrating = true;
        _calibrationTimer = 0f;
        _calibrationSamples.Clear();
        GD.Print("VoiceManager: Calibration started. Speak naturally...");
    }

    private void AnalyzeVoice(float delta)
    {
        float db = -80f;
        float currentRms = 0f;

        // --- Primary: read actual audio frames from the capture buffer ---
        float instantaneousRms = 0f;

        if (_captureEffect != null)
        {
            var frames = _captureEffect.GetBuffer(_captureEffect.GetFramesAvailable());
            if (frames.Length > 0)
            {
                float sumSq = 0;
                foreach (var frame in frames)
                    sumSq += frame.X * frame.X + frame.Y * frame.Y;

                instantaneousRms = Mathf.Sqrt(sumSq / (frames.Length * 2 + 0.000001f));
                
                // --- Software Noise Gate ---
                if (instantaneousRms < 0.0005f) instantaneousRms = 0f;
            }
        }
        else if (_micBusIndex >= 0)
        {
            // Fallback: bus peak meter
            float peak = AudioServer.GetBusPeakVolumeLeftDb(_micBusIndex, 0);
            instantaneousRms = Mathf.DbToLinear(peak);
        }

        // --- Exponential Moving Average (Smoothing) ---
        _smoothedRms = Mathf.Lerp(_smoothedRms, instantaneousRms, SmoothingFactor);
        
        // Convert to dB for UI display
        db = _smoothedRms > 0.00001f ? Mathf.LinearToDb(_smoothedRms) : -80f;

        // Emit raw dB to HUD meter
        EmitSignal(SignalName.VolumeUpdated, db);

        if (IsCalibrating)
        {
            ProcessCalibration(instantaneousRms, delta);
            return;
        }

        // Tier classification using SMOOTHED value
        VoiceTier newTier = ClassifyTier(_smoothedRms);
        if (newTier != _currentTier)
        {
            _currentTier = newTier;
            EmitSignal(SignalName.TierChanged, (int)_currentTier);

            if (_currentTier >= VoiceTier.Whisper)
                UpdateTokenHolder();

            GD.Print($"VoiceManager: Tier → {_currentTier} ({db:F1} dB)");
        }
    }

    private void ProcessCalibration(float rms, float delta)
    {
        _calibrationTimer += delta;
        _calibrationSamples.Add(rms);

        float progress = Mathf.Clamp(_calibrationTimer / CalibrationDuration, 0f, 1f);
        EmitSignal(SignalName.CalibrationProgress, progress);

        if (_calibrationTimer >= CalibrationDuration)
        {
            IsCalibrating = false;
            float sum = 0;
            foreach (float s in _calibrationSamples) sum += s;
            
            // Set new baseline, ensuring it's not zero to avoid div by zero
            BaselineAmplitude = Mathf.Max(sum / Mathf.Max(_calibrationSamples.Count, 1), 0.001f);
            
            GD.Print($"VoiceManager: Calibration finished. New baseline: {BaselineAmplitude:F4}");
            EmitSignal(SignalName.CalibrationFinished, BaselineAmplitude);
        }
    }

    private VoiceTier ClassifyTier(float currentRms)
    {
        float ratio = currentRms / BaselineAmplitude;
        
        // --- Hysteresis Logic ---
        // We require a higher threshold to ENTER a tier than to STAY in it.
        // This prevents flickering between states.
        
        float enterWhisper = 0.45f;
        float exitWhisper = 0.30f;
        
        float enterNormal = 1.10f;
        float exitNormal = 0.90f;
        
        float enterShouting = 2.20f;
        float exitShouting = 1.80f;

        if (_currentTier == VoiceTier.Silent)
        {
            if (ratio >= enterWhisper) return VoiceTier.Whisper;
            return VoiceTier.Silent;
        }
        else if (_currentTier == VoiceTier.Whisper)
        {
            if (ratio < exitWhisper) return VoiceTier.Silent;
            if (ratio >= enterNormal) return VoiceTier.Normal;
            return VoiceTier.Whisper;
        }
        else if (_currentTier == VoiceTier.Normal)
        {
            if (ratio < exitNormal) return VoiceTier.Whisper;
            if (ratio >= enterShouting) return VoiceTier.Shouting;
            return VoiceTier.Normal;
        }
        else if (_currentTier == VoiceTier.Shouting)
        {
            if (ratio < exitShouting) return VoiceTier.Normal;
            return VoiceTier.Shouting;
        }

        return VoiceTier.Silent;
    }

    private void UpdateTokenHolder()
    {
        if (Multiplayer.MultiplayerPeer != null && Multiplayer.HasMultiplayerPeer())
        {
            long myPeerId = Multiplayer.GetUniqueId();
            if (_tokenHolderPeerId != myPeerId)
            {
                RpcId(1, nameof(RequestTokenTransfer), myPeerId);
            }
        }
        else
        {
            Node3D localPlayer = GetTree().GetFirstNodeInGroup("Player") as Node3D;
            if (localPlayer != null && _tokenHolder != localPlayer)
            {
                _tokenHolder = localPlayer;
                EmitSignal(SignalName.TokenTransferred, _tokenHolder);
                GD.Print($"VoiceManager: Token → {localPlayer.Name}");
            }
        }
    }

    [Rpc(MultiplayerApi.RpcMode.AnyPeer, CallLocal = true)]
    private void RequestTokenTransfer(long peerId)
    {
        if (!Multiplayer.IsServer()) return;
        Rpc(nameof(SyncTokenHolder), peerId);
    }

    [Rpc(MultiplayerApi.RpcMode.AnyPeer, CallLocal = true)]
    private void SyncTokenHolder(long peerId)
    {
        _tokenHolderPeerId = peerId;
        Node3D playerNode = FindPlayerNodeByPeerId(peerId);
        if (playerNode != null)
        {
            _tokenHolder = playerNode;
            EmitSignal(SignalName.TokenTransferred, _tokenHolder);
            GD.Print($"VoiceManager: Token synced over network → {playerNode.Name} (Peer {peerId})");
        }
    }

    private Node3D FindPlayerNodeByPeerId(long peerId)
    {
        if (Multiplayer.MultiplayerPeer == null || !Multiplayer.HasMultiplayerPeer() || peerId == 0)
        {
            return GetTree().GetFirstNodeInGroup("Player") as Node3D;
        }

        var playersContainer = GetTree().Root.GetNodeOrNull("Main/Players");
        if (playersContainer != null)
        {
            var pNode = playersContainer.GetNodeOrNull<Node3D>(peerId.ToString());
            if (pNode != null) return pNode;
        }

        var playerNodes = GetTree().GetNodesInGroup("Player");
        foreach (Node node in playerNodes)
        {
            if (node is Node3D node3D)
            {
                if (node3D.Name == peerId.ToString() || node3D.Name == "Player")
                {
                    return node3D;
                }
            }
        }

        return GetTree().GetFirstNodeInGroup("Player") as Node3D;
    }

    private void LoadBaseline()
    {
        var cfg = new ConfigFile();
        if (cfg.Load("user://settings.cfg") == Error.Ok)
        {
            // Safety: Ensure we never load a baseline of 0
            BaselineAmplitude = Mathf.Max((float)cfg.GetValue("audio", "baseline", 0.05f), 0.001f);
            GD.Print($"VoiceManager: Loaded baseline {BaselineAmplitude:F4}");
        }
    }
}
