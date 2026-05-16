using Godot;

public partial class PauseMenu : CanvasLayer
{
    // --- Public state flag (read by PlayerController) ---
    public static bool IsOpen { get; private set; } = false;

    // --- Panel references ---
    private Control _menuPanel;
    private ColorRect _overlay;
    private Button _resumeButton;
    private Button _quitButton;

    // --- Volume sliders ---
    private HSlider _masterVolumeSlider;
    private HSlider _sfxVolumeSlider;
    private HSlider _mouseSensitivitySlider;
    private Label _masterVolumeValueLabel;
    private Label _sfxVolumeValueLabel;
    private Label _mouseSensValueLabel;

    // --- Device dropdowns ---
    private OptionButton _outputDeviceOption;
    private OptionButton _micDeviceOption;

    // --- Mic monitor ---
    private CheckButton _micMonitorToggle;
    private ProgressBar _micTestMeter;
    private bool _micMonitoring = false;
    private bool _suppressDeviceCallback = false;

    // --- Calibration ---
    private Button _calibrateButton;
    private Button _resetAudioButton;
    private ProgressBar _calibrationProgressBar;
    private Label _calibrationStatusLabel;
    private HSlider _sensitivitySlider;

    public override void _Ready()
    {
        GD.Print("PauseMenu: _Ready starting...");
        // Use GetNodeOrNull for EVERY node to prevent any potential startup crash
        _overlay      = GetNodeOrNull<ColorRect>("Overlay");
        _menuPanel    = GetNodeOrNull<Control>("MenuPanel");
        _resumeButton = GetNodeOrNull<Button>("MenuPanel/VBox/ResumeButton");
        _quitButton   = GetNodeOrNull<Button>("MenuPanel/VBox/QuitButton");

        _masterVolumeSlider     = GetNodeOrNull<HSlider>("MenuPanel/VBox/AudioSection/MasterRow/MasterSlider");
        _sfxVolumeSlider        = GetNodeOrNull<HSlider>("MenuPanel/VBox/AudioSection/SFXRow/SFXSlider");
        _mouseSensitivitySlider = GetNodeOrNull<HSlider>("MenuPanel/VBox/ControlsSection/MouseRow/MouseSlider");
        _masterVolumeValueLabel = GetNodeOrNull<Label>("MenuPanel/VBox/AudioSection/MasterRow/MasterValueLabel");
        _sfxVolumeValueLabel    = GetNodeOrNull<Label>("MenuPanel/VBox/AudioSection/SFXRow/SFXValueLabel");
        _mouseSensValueLabel    = GetNodeOrNull<Label>("MenuPanel/VBox/ControlsSection/MouseRow/MouseValueLabel");

        _outputDeviceOption = GetNodeOrNull<OptionButton>("MenuPanel/VBox/DevicesSection/OutputRow/OutputDevice");
        _micDeviceOption    = GetNodeOrNull<OptionButton>("MenuPanel/VBox/DevicesSection/MicRow/MicDevice");
        _micMonitorToggle   = GetNodeOrNull<CheckButton>("MenuPanel/VBox/DevicesSection/MonitorRow/MicMonitorToggle");
        _micTestMeter       = GetNodeOrNull<ProgressBar>("MenuPanel/VBox/DevicesSection/MonitorRow/MicTestMeter");
        _resetAudioButton   = GetNodeOrNull<Button>("MenuPanel/VBox/DevicesSection/ResetRow/ResetButton");

        _calibrateButton        = GetNodeOrNull<Button>("MenuPanel/VBox/DevicesSection/CalibrationRow/CalibrateButton");
        _calibrationProgressBar = GetNodeOrNull<ProgressBar>("MenuPanel/VBox/DevicesSection/CalibrationRow/CalibrationProgress");
        _calibrationStatusLabel = GetNodeOrNull<Label>("MenuPanel/VBox/DevicesSection/CalibrationRow/CalibrationStatus");
        _sensitivitySlider      = GetNodeOrNull<HSlider>("MenuPanel/VBox/DevicesSection/SensitivityRow/SensitivitySlider");

        // --- Signal Connections with Null Checks ---
        if (_resumeButton != null) _resumeButton.Pressed += OnResumePressed;
        if (_quitButton != null)   _quitButton.Pressed   += OnQuitPressed;

        if (_masterVolumeSlider != null)     _masterVolumeSlider.ValueChanged     += OnMasterVolumeChanged;
        if (_sfxVolumeSlider != null)        _sfxVolumeSlider.ValueChanged        += OnSFXVolumeChanged;
        if (_mouseSensitivitySlider != null) _mouseSensitivitySlider.ValueChanged += OnMouseSensChanged;

        if (_outputDeviceOption != null) _outputDeviceOption.ItemSelected += OnOutputDeviceSelected;
        if (_micDeviceOption != null)    _micDeviceOption.ItemSelected    += OnMicDeviceSelected;
        if (_micMonitorToggle != null)   _micMonitorToggle.Toggled        += OnMicMonitorToggled;

        if (_calibrateButton != null)  _calibrateButton.Pressed  += OnCalibratePressed;
        if (_resetAudioButton != null) _resetAudioButton.Pressed += OnResetAudioPressed;
        if (_sensitivitySlider != null) _sensitivitySlider.ValueChanged += OnSensitivityChanged;

        if (VoiceManager.Instance != null)
        {
            VoiceManager.Instance.VolumeUpdated += OnMicVolumeUpdated;
            VoiceManager.Instance.TierChanged += OnTierChanged;
            VoiceManager.Instance.CalibrationProgress += OnCalibrationProgress;
            VoiceManager.Instance.CalibrationFinished += OnCalibrationFinished;
        }

        PopulateOutputDevices();
        PopulateMicDevices();
        LoadSettings();

        // Start hidden
        if (_menuPanel != null) _menuPanel.Visible = false;
        if (_overlay != null)   _overlay.Visible   = false;
        Layer = 10;
        GD.Print("PauseMenu: _Ready finished.");
    }

    public override void _UnhandledInput(InputEvent @event)
    {
        if (@event is InputEventKey key && key.Pressed && !key.Echo
            && key.Keycode == Key.Escape)
        {
            ToggleMenu();
            GetViewport().SetInputAsHandled();
        }
    }

    // ------------------------------------------------------------------ //
    //  Toggle — NO tree pause; only blocks player input via IsOpen flag
    // ------------------------------------------------------------------ //
    private void ToggleMenu()
    {
        IsOpen = !IsOpen;
        _menuPanel.Visible = IsOpen;
        _overlay.Visible   = IsOpen;

        // Cursor management
        Input.MouseMode = IsOpen
            ? Input.MouseModeEnum.Visible
            : Input.MouseModeEnum.Captured;

        // Stop mic monitoring when menu closes
        if (!IsOpen && _micMonitoring)
            SetMicMonitor(false);
    }

    // ------------------------------------------------------------------ //
    //  Buttons
    // ------------------------------------------------------------------ //
    private void OnResumePressed() => ToggleMenu();

    private void OnQuitPressed()
    {
        IsOpen = false;
        GetTree().Quit();
    }

    // ------------------------------------------------------------------ //
    //  Mic monitoring — unmutes the Microphone bus so you hear yourself
    // ------------------------------------------------------------------ //
    private void OnMicMonitorToggled(bool enabled)
    {
        SetMicMonitor(enabled);
        SaveSettings();
    }

    private void OnCalibratePressed()
    {
        if (VoiceManager.Instance == null) return;
        
        _calibrateButton.Disabled = true;
        _calibrationStatusLabel.Text = "SPEAK NATURALLY...";
        _calibrationProgressBar.Value = 0;
        VoiceManager.Instance.StartCalibration();
    }

    private void OnCalibrationProgress(float progress)
    {
        _calibrationProgressBar.Value = progress * 100f;
    }

    private void OnSensitivityChanged(double value)
    {
        if (VoiceManager.Instance != null)
        {
            VoiceManager.Instance.BaselineAmplitude = (float)value;
            // Note: In a real app, we'd save settings here or when menu closes
        }
    }

    private void OnTierChanged(int newTier)
    {
        if (_calibrationStatusLabel == null || !IsOpen) return;

        // Visual feedback in the Pause Menu so user can test live
        switch (newTier)
        {
            case 0: // Silent
                _calibrationStatusLabel.Text = "SILENT";
                _calibrationStatusLabel.AddThemeColorOverride("font_color", new Color(0.5f, 0.5f, 0.5f));
                break;
            case 1: // Whisper
                _calibrationStatusLabel.Text = "WHISPERING";
                _calibrationStatusLabel.AddThemeColorOverride("font_color", new Color(0.2f, 0.8f, 1.0f));
                break;
            case 2: // Normal
                _calibrationStatusLabel.Text = "NORMAL VOICE";
                _calibrationStatusLabel.AddThemeColorOverride("font_color", new Color(0.2f, 1.0f, 0.2f));
                break;
            case 3: // Shout
                _calibrationStatusLabel.Text = "SHOUTING!";
                _calibrationStatusLabel.AddThemeColorOverride("font_color", new Color(1.0f, 0.2f, 0.2f));
                break;
        }
    }



    private void OnCalibrationFinished(float newBaseline)
    {
        if (_sensitivitySlider != null)
            _sensitivitySlider.Value = newBaseline;

        if (_calibrateButton != null)
            _calibrateButton.Disabled = false;

        if (_calibrationStatusLabel != null)
        {
            _calibrationStatusLabel.Text = "CALIBRATED";
            _calibrationStatusLabel.AddThemeColorOverride("font_color", new Color(1, 1, 1));
        }
        SaveSettings();
    }

    private void OnMicVolumeUpdated(float db)
    {
        if (!_micMonitoring && !VoiceManager.Instance.IsCalibrating)
        {
            _micTestMeter.Value = 0;
            return;
        }

        float progress = Mathf.Remap(db, -80f, 0f, 0f, 100f);
        _micTestMeter.Value = Mathf.Clamp(progress, 0f, 100f);
    }

    private void SetMicMonitor(bool enabled)
    {
        _micMonitoring = enabled;
        _micMonitorToggle.ButtonPressed = enabled;

        int busIdx = AudioServer.GetBusIndex("Microphone");
        if (busIdx >= 0)
            AudioServer.SetBusMute(busIdx, !enabled); // unmute = hear yourself
    }

    // ------------------------------------------------------------------ //
    //  Volume sliders
    // ------------------------------------------------------------------ //
    private void OnMasterVolumeChanged(double value)
    {
        float linear = (float)value / 100f;
        AudioServer.SetBusVolumeDb(AudioServer.GetBusIndex("Master"),
            linear > 0f ? Mathf.LinearToDb(linear) : -80f);
        _masterVolumeValueLabel.Text = $"{(int)value}%";
        SaveSettings();
    }

    private void OnSFXVolumeChanged(double value)
    {
        int busIdx = AudioServer.GetBusIndex("SFX");
        if (busIdx >= 0)
        {
            float linear = (float)value / 100f;
            AudioServer.SetBusVolumeDb(busIdx,
                linear > 0f ? Mathf.LinearToDb(linear) : -80f);
        }
        _sfxVolumeValueLabel.Text = $"{(int)value}%";
        SaveSettings();
    }

    private void OnMouseSensChanged(double value)
    {
        _mouseSensValueLabel.Text = $"{value:F2}";
        var cam = GetTree().GetFirstNodeInGroup("CameraManager");
        if (cam is Node3D camNode)
            camNode.Set("CameraSensitivity", (float)value);
        SaveSettings();
    }

    // ------------------------------------------------------------------ //
    //  Device lists
    // ------------------------------------------------------------------ //
    private void PopulateOutputDevices()
    {
        if (_outputDeviceOption == null) return;
        _suppressDeviceCallback = true;
        _outputDeviceOption.Clear();
        string[] devices = AudioServer.GetOutputDeviceList();
        string current = AudioServer.OutputDevice;
        int selectIdx = 0;
        for (int i = 0; i < devices.Length; i++)
        {
            _outputDeviceOption.AddItem(devices[i]);
            if (devices[i] == current) selectIdx = i;
        }
        _outputDeviceOption.Selected = selectIdx;
        _suppressDeviceCallback = false;
    }

    private void PopulateMicDevices()
    {
        if (_micDeviceOption == null) return;
        _suppressDeviceCallback = true;
        _micDeviceOption.Clear();
        string[] devices = AudioServer.GetInputDeviceList();
        string current = AudioServer.InputDevice;
        int selectIdx = 0;
        for (int i = 0; i < devices.Length; i++)
        {
            _micDeviceOption.AddItem(devices[i]);
            if (devices[i] == current) selectIdx = i;
        }
        _micDeviceOption.Selected = selectIdx;
        _suppressDeviceCallback = false;
    }

    private void OnResetAudioPressed()
    {
        GD.Print("PauseMenu: Hard-resetting audio system...");
        AudioServer.InputDevice = "Default";
        PopulateMicDevices();
        VoiceManager.Instance?.RestartCapture();
        SaveSettings();
    }

    private void OnOutputDeviceSelected(long index)
    {
        if (_suppressDeviceCallback) return;
        string device = _outputDeviceOption.GetItemText((int)index);
        AudioServer.OutputDevice = device;
        SaveSettings();
    }

    private void OnMicDeviceSelected(long index)
    {
        if (_suppressDeviceCallback) return;
        string device = _micDeviceOption.GetItemText((int)index);
        AudioServer.InputDevice = device;
        VoiceManager.Instance?.RestartCapture();
        SaveSettings();
    }

    // ------------------------------------------------------------------ //
    //  Persistence
    // ------------------------------------------------------------------ //
    private const string ConfigPath = "user://settings.cfg";

    private void SaveSettings()
    {
        var cfg = new ConfigFile();
        cfg.SetValue("audio",    "master",      _masterVolumeSlider.Value);
        cfg.SetValue("audio",    "sfx",         _sfxVolumeSlider.Value);
        cfg.SetValue("controls", "mouse_sens",  _mouseSensitivitySlider.Value);
        cfg.SetValue("devices",  "output",      AudioServer.OutputDevice);
        cfg.SetValue("devices",  "input",       AudioServer.InputDevice);
        
        if (VoiceManager.Instance != null)
            cfg.SetValue("audio", "baseline", VoiceManager.Instance.BaselineAmplitude);

        cfg.Save(ConfigPath);
    }

    private void LoadSettings()
    {
        var cfg = new ConfigFile();
        if (cfg.Load(ConfigPath) == Error.Ok)
        {
            double master = (double)cfg.GetValue("audio",    "master",     100.0);
            double sfx    = (double)cfg.GetValue("audio",    "sfx",        80.0);
            double mouse  = (double)cfg.GetValue("controls", "mouse_sens", 0.2);
            string output = (string)cfg.GetValue("devices",  "output",     "Default");
            string input  = (string)cfg.GetValue("devices",  "input",      "Default");
            float baseline = (float)cfg.GetValue("audio",   "baseline",   0.05f);

            if (_masterVolumeSlider != null) _masterVolumeSlider.Value = master;
            if (_sfxVolumeSlider != null) _sfxVolumeSlider.Value = sfx;
            if (_mouseSensitivitySlider != null) _mouseSensitivitySlider.Value = mouse;

            if (VoiceManager.Instance != null)
                VoiceManager.Instance.BaselineAmplitude = baseline;

            OnMasterVolumeChanged(master);
            OnSFXVolumeChanged(sfx);
            OnMouseSensChanged(mouse);

            AudioServer.OutputDevice = output;
            SelectOptionByText(_outputDeviceOption, output);
            AudioServer.InputDevice = input;
            SelectOptionByText(_micDeviceOption, input);

            VoiceManager.Instance?.RestartCapture();
        }
        else
        {
            if (_masterVolumeSlider != null) OnMasterVolumeChanged(_masterVolumeSlider.Value);
            if (_sfxVolumeSlider != null) OnSFXVolumeChanged(_sfxVolumeSlider.Value);
            if (_mouseSensitivitySlider != null) OnMouseSensChanged(_mouseSensitivitySlider.Value);
        }
    }

    private void SelectOptionByText(OptionButton btn, string text)
    {
        if (btn == null) return;
        for (int i = 0; i < btn.ItemCount; i++)
            if (btn.GetItemText(i) == text) { btn.Selected = i; return; }
    }
}
