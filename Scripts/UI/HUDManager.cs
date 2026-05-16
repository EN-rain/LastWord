using Godot;
using System;

public partial class HUDManager : Control
{
    [Export] public NodePath TierLabelPath;
    [Export] public NodePath VolumeMeterPath;
    [Export] public NodePath TokenIndicatorPath;

    private Label _tierLabel;
    private ProgressBar _volumeMeter;
    private TextureRect _tokenIndicator;

    private Color _colorSilent = new Color(0.5f, 0.5f, 0.5f);
    private Color _colorWhisper = new Color(0.2f, 0.8f, 0.2f);
    private Color _colorNormal = new Color(0.8f, 0.8f, 0.2f);
    private Color _colorShouting = new Color(0.8f, 0.2f, 0.2f);

    public override void _Ready()
    {
        _tierLabel = GetNode<Label>(TierLabelPath);
        _volumeMeter = GetNode<ProgressBar>(VolumeMeterPath);
        _tokenIndicator = GetNode<TextureRect>(TokenIndicatorPath);

        // Connect to VoiceManager signals
        VoiceManager.Instance.TierChanged += OnTierChanged;
        VoiceManager.Instance.VolumeUpdated += OnVolumeUpdated;
        VoiceManager.Instance.TokenTransferred += OnTokenTransferred;

        // Initialize state
        _tokenIndicator.Visible = false;
        UpdateTierUI(0);
    }

    private void OnTierChanged(int newTier)
    {
        UpdateTierUI(newTier);
    }

    private void OnVolumeUpdated(float dbValue)
    {
        // Map dB (-60 to 0) to progress (0 to 100)
        float progress = Mathf.Remap(dbValue, -60f, 0f, 0f, 100f);
        _volumeMeter.Value = progress;
    }

    private void OnTokenTransferred(Node3D newHolder)
    {
        // Show skull if local player is the holder
        // In single player, it's always the local player
        _tokenIndicator.Visible = true;
        
        // Pulse effect
        var tween = CreateTween();
        tween.TweenProperty(_tokenIndicator, "scale", new Vector2(1.2f, 1.2f), 0.1f);
        tween.TweenProperty(_tokenIndicator, "scale", new Vector2(1.0f, 1.0f), 0.1f);
    }

    private void UpdateTierUI(int tier)
    {
        VoiceTier voiceTier = (VoiceTier)tier;
        _tierLabel.Text = "Voice: " + voiceTier.ToString();

        Color targetColor = _colorSilent;
        switch (voiceTier)
        {
            case VoiceTier.Whisper: targetColor = _colorWhisper; break;
            case VoiceTier.Normal: targetColor = _colorNormal; break;
            case VoiceTier.Shouting: targetColor = _colorShouting; break;
        }

        _tierLabel.Modulate = targetColor;
    }
}
