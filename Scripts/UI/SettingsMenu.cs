using Godot;
using System;
using System.Collections.Generic;

public partial class SettingsMenu : PanelContainer
{
	[Export] public bool IsSimplifiedMode { get; set; } = false;

	// --- UI Navigation Exports ---
	[Export] public NodePath BtnGameControlsPath;
	[Export] public NodePath BtnAudioMicPath;
	[Export] public NodePath BtnOtherOptionsPath;
	[Export] public NodePath BtnAccountPath;

	[Export] public NodePath PanelGameControlsPath;
	[Export] public NodePath PanelAudioMicPath;
	[Export] public NodePath PanelOtherOptionsPath;
	[Export] public NodePath PanelAccountPath;

	[ExportGroup("Account")]
	[Export] public NodePath ProfileNameInputPath;
	[Export] public NodePath BtnSaveProfileNamePath;
	[Export] public NodePath ProfileRunsValueLabelPath;
	[Export] public NodePath ProfileStatusLabelPath;

	// --- Tab 1: Game Controls Exports ---
	[Export] public NodePath MouseSensitivitySliderPath;
	[Export] public NodePath MouseSensValueLabelPath;
	[Export] public NodePath KeybindGridContainerPath;
	[Export] public NodePath AnalogWarningBoxPath;
	[Export] public NodePath BtnResetControlsPath;

	// --- Tab 2: Audio & Mic Exports ---
	[Export] public NodePath MasterVolumeSliderPath;
	[Export] public NodePath MasterVolumeValueLabelPath;
	[Export] public NodePath SFXVolumeSliderPath;
	[Export] public NodePath SFXVolumeValueLabelPath;
	[Export] public NodePath VCVolumeSliderPath;
	[Export] public NodePath VCVolumeValueLabelPath;
	[Export] public NodePath ListenerVolumeSliderPath;
	[Export] public NodePath ListenerVolumeValueLabelPath;
	[Export] public NodePath ListenerWarningBoxPath;

	[Export] public NodePath OutputDeviceOptionPath;
	[Export] public NodePath MicDeviceOptionPath;
	[Export] public NodePath MicMonitorTogglePath;
	[Export] public NodePath MicTestMeterPath;
	[Export] public NodePath ResetAudioButtonPath;

	[Export] public NodePath CalibrateButtonPath;
	[Export] public NodePath CalibrationProgressBarPath;
	[Export] public NodePath CalibrationStatusLabelPath;

	// --- Tab 3: Other Options Exports ---
	[Export] public NodePath ToggleSubtitlesPath;
	[Export] public NodePath ToggleProximityPulsePath;
	[Export] public NodePath ToggleTextBroadcasterPath;
	[Export] public NodePath TextBroadcasterWarningPath;
	[Export] public NodePath PrivacyStatusLabelPath;
	[Export] public NodePath BtnViewPrivacyPath;
	[Export] public NodePath BtnRevokeConsentPath;

	// --- Dynamic Warnings and Configs ---
	[Export] public Color StatusSilentColor = new Color(0.5f, 0.5f, 0.5f);
	[Export] public Color StatusWhisperingColor = new Color(0.3f, 0.8f, 0.9f);
	[Export] public Color StatusNormalColor = new Color(0.2f, 0.9f, 0.2f);
	[Export] public Color StatusShoutingColor = new Color(0.95f, 0.15f, 0.5f);
	[ExportGroup("Audio Bus Names")]
	[Export] public string MasterBusName = "Master";
	[Export] public string SfxBusName = "SFX";
	[Export] public string VoiceBusName = "Microphone";
	[Export] public string ListenerBusName = "ListenerAudio";

	private const string ConfigPath = "user://settings.cfg";

	// --- Internal References ---
	private Button _btnGameControls;
	private Button _btnAudioMic;
	private Button _btnOtherOptions;
	private Button _btnAccount;

	private Control _panelGameControls;
	private Control _panelAudioMic;
	private Control _panelOtherOptions;
	private Control _panelAccount;

	private LineEdit _profileNameInput;
	private Button _btnSaveProfileName;
	private Label _profileRunsValueLabel;
	private Label _profileStatusLabel;

	private HSlider _mouseSensSlider;
	private Label _mouseSensValueLabel;
	private GridContainer _keybindGrid;
	private PanelContainer _analogWarningBox;
	private Button _btnResetControls;

	private HSlider _masterVolumeSlider;
	private Label _masterVolumeValueLabel;
	private HSlider _sfxVolumeSlider;
	private Label _sfxVolumeValueLabel;
	private HSlider _vcVolumeSlider;
	private Label _vcVolumeValueLabel;
	private HSlider _listenerVolumeSlider;
	private Label _listenerVolumeValueLabel;
	private PanelContainer _listenerWarningBox;

	private OptionButton _outputDeviceOption;
	private OptionButton _micDeviceOption;
	private CheckButton _micMonitorToggle;
	private ProgressBar _micTestMeter;
	private Button _btnResetAudio;

	private Button _calibrateButton;
	private ProgressBar _calibrationProgressBar;
	private Label _calibrationStatusLabel;

	private CheckButton _toggleSubtitles;
	private CheckButton _toggleProximityPulse;
	private CheckButton _toggleTextBroadcaster;
	private Label _textBroadcasterWarning;
	private Label _privacyStatusLabel;
	private Button _btnViewPrivacy;
	private Button _btnRevokeConsent;

	// --- Rebinding State ---
	private string _rebindingAction = null;
	private Button _rebindingButton = null;
	private Dictionary<string, Button> _rebindButtonsMap = new();

	private bool _suppressDeviceCallback = false;
	private bool _micMonitoring = false;
	private bool _loadingSettings = false;

	// --- Rebindable Actions Map ---
	private readonly Dictionary<string, string> _actionNames = new()
	{
		{ "move_forward", "Move Forward (Key W)" },
		{ "move_backward", "Move Backward (Key S)" },
		{ "move_left", "Move Left (Key A)" },
		{ "move_right", "Move Right (Key D)" },
		{ "move_jump", "Jump (Key Space)" },
		{ "move_sprint", "Sprint/Run (Key Shift)" },
		{ "gesture_z", "Gesture Z (Key Z)" },
		{ "gesture_x", "Gesture X (Key X)" },
		{ "gesture_c", "Gesture C (Key C)" },
		{ "gesture_v", "Gesture V (Key V)" },
		{ "gesture_b", "Gesture B (Key B)" },
		{ "gesture_n", "Gesture N (Key N)" },
		{ "gesture_m", "Gesture M (Key M)" },
		{ "gesture_l", "Gesture L (Key L)" },
		{ "clap_q", "Clap Action (Key Q)" },
		{ "sacrifice_g", "Vocal Sacrifice (Key G)" },
		{ "radial_wheel_mmb", "Radial Selection Wheel (MMB)" },
		{ "spectator_j", "Spectator Skull Marker (Key J) [Reserved]" }
	};

	public override void _Ready()
	{
		EnsureDefaultActions();
		BindNodes();
		SetupTabs();
		SetupAudio();
		PopulateDevices();
		LoadSettings();

		if (_btnViewPrivacy != null) _btnViewPrivacy.Pressed += OnViewPrivacyPressed;
		if (_btnRevokeConsent != null) _btnRevokeConsent.Pressed += OnRevokeConsentPressed;

		// VoiceManager connections
		if (VoiceManager.Instance != null)
		{
			VoiceManager.Instance.VolumeUpdated += OnMicVolumeUpdated;
			VoiceManager.Instance.TierChanged += OnTierChanged;
			VoiceManager.Instance.CalibrationProgress += OnCalibrationProgress;
			VoiceManager.Instance.CalibrationFinished += OnCalibrationFinished;
		}

		// Dynamic elements configuration
		PopulateKeybindGrid();
		CheckAnalogWarnings();

		if (IsSimplifiedMode)
		{
			var sidebar = GetNodeOrNull<Control>("HBox/Sidebar");
			if (sidebar != null) sidebar.Visible = false;
			SetActiveTab(2); // Force to Audio & Mic tab
		}
	}

	public override void _ExitTree()
	{
		SetMicMonitor(false);

		if (VoiceManager.Instance != null)
		{
			VoiceManager.Instance.VolumeUpdated -= OnMicVolumeUpdated;
			VoiceManager.Instance.TierChanged -= OnTierChanged;
			VoiceManager.Instance.CalibrationProgress -= OnCalibrationProgress;
			VoiceManager.Instance.CalibrationFinished -= OnCalibrationFinished;
		}
	}

	private void EnsureDefaultActions()
	{
		var defaults = new Dictionary<string, Key>
		{
			{ "move_forward", Key.W },
			{ "move_backward", Key.S },
			{ "move_left", Key.A },
			{ "move_right", Key.D },
			{ "move_jump", Key.Space },
			{ "move_sprint", Key.Shift },
			{ "gesture_z", Key.Z },
			{ "gesture_x", Key.X },
			{ "gesture_c", Key.C },
			{ "gesture_v", Key.V },
			{ "gesture_b", Key.B },
			{ "gesture_n", Key.N },
			{ "gesture_m", Key.M },
			{ "gesture_l", Key.L },
			{ "clap_q", Key.Q },
			{ "sacrifice_g", Key.G },
			{ "spectator_j", Key.J }
		};

		foreach (var pair in defaults)
		{
			if (!InputMap.HasAction(pair.Key))
			{
				InputMap.AddAction(pair.Key);
				var keyEvent = new InputEventKey { Keycode = pair.Value };
				InputMap.ActionAddEvent(pair.Key, keyEvent);
			}
		}

		if (!InputMap.HasAction("radial_wheel_mmb"))
		{
			InputMap.AddAction("radial_wheel_mmb");
			var mouseEvent = new InputEventMouseButton { ButtonIndex = MouseButton.Middle };
			InputMap.ActionAddEvent("radial_wheel_mmb", mouseEvent);
		}
	}

	private void BindNodes()
	{
		// Navigation Binds
		_btnGameControls = GetNodeOrNull<Button>(BtnGameControlsPath);
		_btnAudioMic = GetNodeOrNull<Button>(BtnAudioMicPath);
		_btnOtherOptions = GetNodeOrNull<Button>(BtnOtherOptionsPath);
		_btnAccount = GetNodeOrNull<Button>(BtnAccountPath);

		_panelGameControls = GetNodeOrNull<Control>(PanelGameControlsPath);
		_panelAudioMic = GetNodeOrNull<Control>(PanelAudioMicPath);
		_panelOtherOptions = GetNodeOrNull<Control>(PanelOtherOptionsPath);
		_panelAccount = GetNodeOrNull<Control>(PanelAccountPath);

		_profileNameInput = GetNodeOrNull<LineEdit>(ProfileNameInputPath);
		_btnSaveProfileName = GetNodeOrNull<Button>(BtnSaveProfileNamePath);
		_profileRunsValueLabel = GetNodeOrNull<Label>(ProfileRunsValueLabelPath);
		_profileStatusLabel = GetNodeOrNull<Label>(ProfileStatusLabelPath);

		// Tab 1: Game Controls Binds
		_mouseSensSlider = GetNodeOrNull<HSlider>(MouseSensitivitySliderPath);
		_mouseSensValueLabel = GetNodeOrNull<Label>(MouseSensValueLabelPath);
		_keybindGrid = GetNodeOrNull<GridContainer>(KeybindGridContainerPath);
		_analogWarningBox = GetNodeOrNull<PanelContainer>(AnalogWarningBoxPath);
		_btnResetControls = GetNodeOrNull<Button>(BtnResetControlsPath);

		// Tab 2: Audio & Mic Binds
		_masterVolumeSlider = GetNodeOrNull<HSlider>(MasterVolumeSliderPath);
		_masterVolumeValueLabel = GetNodeOrNull<Label>(MasterVolumeValueLabelPath);
		_sfxVolumeSlider = GetNodeOrNull<HSlider>(SFXVolumeSliderPath);
		_sfxVolumeValueLabel = GetNodeOrNull<Label>(SFXVolumeValueLabelPath);
		_vcVolumeSlider = GetNodeOrNull<HSlider>(VCVolumeSliderPath);
		_vcVolumeValueLabel = GetNodeOrNull<Label>(VCVolumeValueLabelPath);
		_listenerVolumeSlider = GetNodeOrNull<HSlider>(ListenerVolumeSliderPath);
		_listenerVolumeValueLabel = GetNodeOrNull<Label>(ListenerVolumeValueLabelPath);
		_listenerWarningBox = GetNodeOrNull<PanelContainer>(ListenerWarningBoxPath);

		_outputDeviceOption = GetNodeOrNull<OptionButton>(OutputDeviceOptionPath);
		_micDeviceOption = GetNodeOrNull<OptionButton>(MicDeviceOptionPath);
		_micMonitorToggle = GetNodeOrNull<CheckButton>(MicMonitorTogglePath);
		_micTestMeter = GetNodeOrNull<ProgressBar>(MicTestMeterPath);
		_btnResetAudio = GetNodeOrNull<Button>(ResetAudioButtonPath);

		_calibrateButton = GetNodeOrNull<Button>(CalibrateButtonPath);
		_calibrationProgressBar = GetNodeOrNull<ProgressBar>(CalibrationProgressBarPath);
		_calibrationStatusLabel = GetNodeOrNull<Label>(CalibrationStatusLabelPath);

		// Tab 3: Other Options Binds
		_toggleSubtitles = GetNodeOrNull<CheckButton>(ToggleSubtitlesPath);
		_toggleProximityPulse = GetNodeOrNull<CheckButton>(ToggleProximityPulsePath);
		_toggleTextBroadcaster = GetNodeOrNull<CheckButton>(ToggleTextBroadcasterPath);
		_textBroadcasterWarning = GetNodeOrNull<Label>(TextBroadcasterWarningPath);
		_privacyStatusLabel = GetNodeOrNull<Label>(PrivacyStatusLabelPath);
		_btnViewPrivacy = GetNodeOrNull<Button>(BtnViewPrivacyPath);
		_btnRevokeConsent = GetNodeOrNull<Button>(BtnRevokeConsentPath);
	}

	private void SetupTabs()
	{
		if (_btnAccount != null) _btnAccount.Pressed += () => SetActiveTab(0);
		if (_btnGameControls != null) _btnGameControls.Pressed += () => SetActiveTab(1);
		if (_btnAudioMic != null) _btnAudioMic.Pressed += () => SetActiveTab(2);
		if (_btnOtherOptions != null) _btnOtherOptions.Pressed += () => SetActiveTab(3);

		SetActiveTab(0);
	}

	private void SetActiveTab(int tabIndex)
	{
		if (_panelAccount != null) _panelAccount.Visible = (tabIndex == 0);
		if (_panelGameControls != null) _panelGameControls.Visible = (tabIndex == 1);
		if (_panelAudioMic != null) _panelAudioMic.Visible = (tabIndex == 2);
		if (_panelOtherOptions != null) _panelOtherOptions.Visible = (tabIndex == 3);

		// Toggle visual flat status for visual selection highlights
		if (_btnAccount != null) _btnAccount.Flat = (tabIndex != 0);
		if (_btnGameControls != null) _btnGameControls.Flat = (tabIndex != 1);
		if (_btnAudioMic != null) _btnAudioMic.Flat = (tabIndex != 2);
		if (_btnOtherOptions != null) _btnOtherOptions.Flat = (tabIndex != 3);

		// Stop rebind mode if tab is switched
		CancelRebind();
	}

	private void SetupAudio()
	{
		if (_masterVolumeSlider != null) _masterVolumeSlider.ValueChanged += OnMasterVolumeChanged;
		if (_sfxVolumeSlider != null) _sfxVolumeSlider.ValueChanged += OnSFXVolumeChanged;
		if (_vcVolumeSlider != null) _vcVolumeSlider.ValueChanged += OnVCVolumeChanged;
		
		if (_listenerVolumeSlider != null)
		{
			_listenerVolumeSlider.MinValue = 20.0f; // Force gameplay-critical floor
			_listenerVolumeSlider.MaxValue = 100.0f;
			_listenerVolumeSlider.Step = 1.0f;
			_listenerVolumeSlider.ValueChanged += OnListenerVolumeChanged;
		}

		if (_mouseSensSlider != null) _mouseSensSlider.ValueChanged += OnMouseSensChanged;
		
		if (_outputDeviceOption != null) _outputDeviceOption.ItemSelected += OnOutputDeviceSelected;
		if (_micDeviceOption != null) _micDeviceOption.ItemSelected += OnMicDeviceSelected;
		if (_micMonitorToggle != null) _micMonitorToggle.Toggled += OnMicMonitorToggled;
		if (_btnResetAudio != null) _btnResetAudio.Pressed += OnResetAudioPressed;

		if (_calibrateButton != null) _calibrateButton.Pressed += OnCalibratePressed;
		if (_btnSaveProfileName != null) _btnSaveProfileName.Pressed += OnSaveProfileNamePressed;
		if (_profileNameInput != null) _profileNameInput.TextSubmitted += _ => OnSaveProfileNamePressed();

		if (_toggleSubtitles != null) _toggleSubtitles.Toggled += _ => { if (!_loadingSettings) SaveSettings(); };
		if (_toggleProximityPulse != null) _toggleProximityPulse.Toggled += _ => { if (!_loadingSettings) SaveSettings(); };
		
		if (_toggleTextBroadcaster != null)
		{
			_toggleTextBroadcaster.Toggled += (enabled) =>
			{
				if (_textBroadcasterWarning != null)
					_textBroadcasterWarning.Visible = enabled;
				if (!_loadingSettings) SaveSettings();
			};
		}

		if (_btnResetControls != null) _btnResetControls.Pressed += OnResetControlsPressed;
	}

	private void PopulateDevices()
	{
		_suppressDeviceCallback = true;

		if (_outputDeviceOption != null)
		{
			_outputDeviceOption.Clear();
			string[] outs = AudioServer.GetOutputDeviceList();
			foreach (var dev in outs) _outputDeviceOption.AddItem(dev);
			SelectOptionByText(_outputDeviceOption, AudioServer.OutputDevice);
		}

		if (_micDeviceOption != null)
		{
			_micDeviceOption.Clear();
			string[] ins = AudioServer.GetInputDeviceList();
			foreach (var dev in ins) _micDeviceOption.AddItem(dev);
			SelectOptionByText(_micDeviceOption, AudioServer.InputDevice);
		}

		_suppressDeviceCallback = false;
	}

	// ------------------------------------------------------------------ //
	//  Keybinding Remapping Controls (Tab 1)
	// ------------------------------------------------------------------ //
	private void PopulateKeybindGrid()
	{
		if (_keybindGrid == null) return;

		// Clear existing children
		foreach (Node child in _keybindGrid.GetChildren())
			child.QueueFree();

		_rebindButtonsMap.Clear();

		foreach (var action in _actionNames)
		{
			var label = new Label
			{
				Text = action.Value,
				CustomMinimumSize = new Vector2(200, 0),
				VerticalAlignment = VerticalAlignment.Center
			};
			_keybindGrid.AddChild(label);

			var button = new Button
			{
				CustomMinimumSize = new Vector2(180, 36),
				SizeFlagsHorizontal = SizeFlags.ExpandFill
			};

			if (action.Key == "spectator_j")
			{
				button.Text = "J (LOCKED)";
				button.Disabled = true;
				button.TooltipText = "Reserved strictly for Spectator view markers.";
			}
			else
			{
				UpdateButtonLabel(button, action.Key);
				button.Pressed += () => StartRebind(action.Key, button);
			}

			_keybindGrid.AddChild(button);
			_rebindButtonsMap[action.Key] = button;
		}
	}

	private void UpdateButtonLabel(Button btn, string action)
	{
		var events = InputMap.ActionGetEvents(action);
		if (events.Count > 0)
		{
			var ev = events[0];
			if (ev is InputEventKey keyEv)
			{
				btn.Text = OS.GetKeycodeString(keyEv.Keycode);
			}
			else if (ev is InputEventMouseButton mouseEv)
			{
				btn.Text = $"Mouse {mouseEv.ButtonIndex}";
			}
			else
			{
				btn.Text = ev.AsText();
			}
		}
		else
		{
			btn.Text = "[ UNBOUND ]";
		}
	}

	private void StartRebind(string action, Button btn)
	{
		CancelRebind();

		_rebindingAction = action;
		_rebindingButton = btn;
		btn.Text = "[ PRESS ANY KEY / MOUSE BUTTON ]";
		btn.AddThemeColorOverride("font_color", StatusWhisperingColor);
	}

	private void CancelRebind()
	{
		if (_rebindingAction != null && _rebindingButton != null)
		{
			UpdateButtonLabel(_rebindingButton, _rebindingAction);
			_rebindingButton.RemoveThemeColorOverride("font_color");
			_rebindingAction = null;
			_rebindingButton = null;
		}
	}

	public override void _UnhandledInput(InputEvent @event)
	{
		if (_rebindingAction == null || _rebindingButton == null) return;

		bool isKey = @event is InputEventKey keyEv && keyEv.Pressed && !keyEv.Echo;
		bool isMouse = @event is InputEventMouseButton mouseEv && mouseEv.Pressed;

		if (isKey || isMouse)
		{
			// Intercept Event
			GetViewport().SetInputAsHandled();

			// Guard 1: The J reservation rule
			if (isKey)
			{
				var keyEvCast = (InputEventKey)@event;
				if (keyEvCast.Keycode == Key.J)
				{
					ShowJConflictAlert();
					CancelRebind();
					return;
				}
			}

			// Remap Action
			InputMap.ActionEraseEvents(_rebindingAction);
			InputMap.ActionAddEvent(_rebindingAction, @event);

			// Complete Rebind
			UpdateButtonLabel(_rebindingButton, _rebindingAction);
			_rebindingButton.RemoveThemeColorOverride("font_color");

			string act = _rebindingAction;
			_rebindingAction = null;
			_rebindingButton = null;

			CheckAnalogWarnings();
			SaveSettings();
		}
	}

	private void ShowJConflictAlert()
	{
		var popup = new AcceptDialog
		{
			Title = "Input Binding Conflict",
			DialogText = "Key 'J' is strictly reserved for the Spectator Skull Marker and cannot be remapped to other living survivor inputs."
		};
		AddChild(popup);
		popup.PopupCentered();
	}

	private void CheckAnalogWarnings()
	{
		if (_analogWarningBox == null) return;

		// Check if G or MMB are mapped to mouse buttons
		bool warningsNeeded = false;

		string[] holdActions = { "sacrifice_g", "radial_wheel_mmb" };
		foreach (var act in holdActions)
		{
			var events = InputMap.ActionGetEvents(act);
			if (events.Count > 0 && events[0] is InputEventMouseButton)
			{
				warningsNeeded = true;
				break;
			}
		}

		_analogWarningBox.Visible = warningsNeeded;
	}

	private void OnResetControlsPressed()
	{
		InputMap.LoadFromProjectSettings();
		EnsureDefaultActions();
		PopulateKeybindGrid();
		CheckAnalogWarnings();
		SaveSettings();
	}

	private void OnSaveProfileNamePressed()
	{
		if (_profileNameInput == null) return;

		string name = _profileNameInput.Text.Trim();
		name = name.Replace(":", "").Replace("|", "");
		if (string.IsNullOrEmpty(name))
			name = "Player_" + new Random().Next(1000, 9999);

		var cfg = new ConfigFile();
		cfg.Load(ConfigPath);
		cfg.SetValue("player", "name", name);
		cfg.Save(ConfigPath);

		_profileNameInput.Text = name;
		if (NetworkManager.Instance != null)
			NetworkManager.Instance.PlayerName = name;

		if (_profileStatusLabel != null)
			_profileStatusLabel.Text = $"Saved profile name: {name}";
	}

	// ------------------------------------------------------------------ //
	//  Audio & Volume Handlers (Tab 2)
	// ------------------------------------------------------------------ //
	private void OnMasterVolumeChanged(double value)
	{
		ApplyMasterVolume(value);
		if (!_loadingSettings) SaveSettings();
	}

	private void ApplyMasterVolume(double value)
	{
		float linear = (float)value / 100f;
		int busIdx = AudioServer.GetBusIndex(MasterBusName);
		if (busIdx >= 0)
		{
			AudioServer.SetBusVolumeDb(busIdx,
				linear > 0f ? Mathf.LinearToDb(linear) : -80f);
		}
		
		if (_masterVolumeValueLabel != null)
			_masterVolumeValueLabel.Text = $"{(int)value}%";
	}

	private void OnSFXVolumeChanged(double value)
	{
		ApplySfxVolume(value);
		if (!_loadingSettings) SaveSettings();
	}

	private void ApplySfxVolume(double value)
	{
		int busIdx = AudioServer.GetBusIndex(SfxBusName);
		if (busIdx >= 0)
		{
			float linear = (float)value / 100f;
			AudioServer.SetBusVolumeDb(busIdx,
				linear > 0f ? Mathf.LinearToDb(linear) : -80f);
		}
		
		if (_sfxVolumeValueLabel != null)
			_sfxVolumeValueLabel.Text = $"{(int)value}%";
	}

	private void OnVCVolumeChanged(double value)
	{
		ApplyVoiceVolume(value);
		if (!_loadingSettings) SaveSettings();
	}

	private void ApplyVoiceVolume(double value)
	{
		int busIdx = AudioServer.GetBusIndex(VoiceBusName);
		if (busIdx >= 0)
		{
			float linear = (float)value / 100f;
			AudioServer.SetBusVolumeDb(busIdx,
				linear > 0f ? Mathf.LinearToDb(linear) : -80f);
		}

		if (_vcVolumeValueLabel != null)
			_vcVolumeValueLabel.Text = $"{(int)value}%";
	}

	private void OnListenerVolumeChanged(double value)
	{
		ApplyListenerVolume(value);
		if (!_loadingSettings) SaveSettings();
	}

	private void ApplyListenerVolume(double value)
	{
		// 20% hard minimum floor is handled by slider MinValue setup
		int busIdx = AudioServer.GetBusIndex(ListenerBusName);
		if (busIdx >= 0)
		{
			float linear = (float)value / 100f;
			AudioServer.SetBusVolumeDb(busIdx, Mathf.LinearToDb(linear));
		}

		if (_listenerVolumeValueLabel != null)
			_listenerVolumeValueLabel.Text = $"{(int)value}%";

		// Display Warning Box below 30% Listener volume
		if (_listenerWarningBox != null)
			_listenerWarningBox.Visible = (value < 30.0f);
	}

	private void OnMouseSensChanged(double value)
	{
		if (_mouseSensValueLabel != null)
			_mouseSensValueLabel.Text = $"{value:F2}";

		if (!_loadingSettings) SaveSettings();
	}

	private void OnOutputDeviceSelected(long index)
	{
		if (_suppressDeviceCallback || _outputDeviceOption == null) return;
		string device = _outputDeviceOption.GetItemText((int)index);
		AudioServer.OutputDevice = device;
		SaveSettings();
	}

	private void OnMicDeviceSelected(long index)
	{
		if (_suppressDeviceCallback || _micDeviceOption == null) return;
		string device = _micDeviceOption.GetItemText((int)index);
		AudioServer.InputDevice = device;
		VoiceManager.Instance?.RestartCapture();
		SaveSettings();
	}

	private void OnMicMonitorToggled(bool enabled)
	{
		SetMicMonitor(enabled);
	}

	private void SetMicMonitor(bool enabled)
	{
		_micMonitoring = enabled;
		if (_micMonitorToggle != null && _micMonitorToggle.ButtonPressed != enabled)
			_micMonitorToggle.ButtonPressed = enabled;

		int busIdx = AudioServer.GetBusIndex(VoiceBusName);
		if (busIdx >= 0)
			AudioServer.SetBusMute(busIdx, !enabled); // Unmute lets the local output hear it
	}

	private void OnResetAudioPressed()
	{
		AudioServer.InputDevice = "Default";
		PopulateDevices();
		VoiceManager.Instance?.RestartCapture();
		SaveSettings();
	}

	// ------------------------------------------------------------------ //
	//  Voice Calibration & Live Volume (Tab 2 Mechanics)
	// ------------------------------------------------------------------ //
	private void OnCalibratePressed()
	{
		if (VoiceManager.Instance == null) return;
		
		if (_calibrateButton != null) _calibrateButton.Disabled = true;
		if (_calibrationStatusLabel != null)
		{
			_calibrationStatusLabel.Text = "SPEAK NATURALLY...";
			_calibrationStatusLabel.AddThemeColorOverride("font_color", StatusWhisperingColor);
		}
		if (_calibrationProgressBar != null) _calibrationProgressBar.Value = 0;

		VoiceManager.Instance.StartCalibration();
	}

	private void OnCalibrationProgress(float progress)
	{
		if (_calibrationProgressBar != null)
			_calibrationProgressBar.Value = progress * 100f;
	}

	private void OnCalibrationFinished(float newBaseline)
	{
		if (_calibrateButton != null) _calibrateButton.Disabled = false;
		
		if (_calibrationStatusLabel != null)
		{
			_calibrationStatusLabel.Text = $"CALIBRATED (BL: {newBaseline:F3})";
			_calibrationStatusLabel.AddThemeColorOverride("font_color", StatusNormalColor);
		}

		SaveSettings();
	}

	private void OnMicVolumeUpdated(float db)
	{
		if (_micTestMeter == null) return;

		if (!_micMonitoring && (VoiceManager.Instance == null || !VoiceManager.Instance.IsCalibrating))
		{
			_micTestMeter.Value = 0;
			return;
		}

		// Remap dB from -45 to 0 onto 0.0 to 1.0 range for the ProgressBar
		float progress = Mathf.Remap(db, -45f, 0f, 0f, 1.0f);
		_micTestMeter.Value = Mathf.Clamp(progress, 0f, 1.0f);
	}

	private void OnTierChanged(int newTier)
	{
		if (_calibrationStatusLabel == null) return;

		// Skip changing status label if actively calibration counting
		if (VoiceManager.Instance != null && VoiceManager.Instance.IsCalibrating) return;

		Color targetColor;
		string text;

		switch (newTier)
		{
			case 1:
				text = "WHISPERING";
				targetColor = StatusWhisperingColor;
				break;
			case 2:
				text = "NORMAL VOICE";
				targetColor = StatusNormalColor;
				break;
			case 3:
				text = "SHOUTING!";
				targetColor = StatusShoutingColor;
				break;
			default:
				text = "SILENT";
				targetColor = StatusSilentColor;
				break;
		}

		_calibrationStatusLabel.Text = text;
		_calibrationStatusLabel.AddThemeColorOverride("font_color", targetColor);
	}

	// ------------------------------------------------------------------ //
	//  Persistence Methods
	// ------------------------------------------------------------------ //
	public void SaveSettings()
	{
		var cfg = new ConfigFile();
		cfg.Load(ConfigPath);

		// Core settings
		if (_masterVolumeSlider != null) cfg.SetValue("audio", "master", _masterVolumeSlider.Value);
		if (_sfxVolumeSlider != null) cfg.SetValue("audio", "sfx", _sfxVolumeSlider.Value);
		if (_vcVolumeSlider != null) cfg.SetValue("audio", "vc", _vcVolumeSlider.Value);
		if (_listenerVolumeSlider != null) cfg.SetValue("audio", "listener", _listenerVolumeSlider.Value);
		if (_mouseSensSlider != null) cfg.SetValue("controls", "mouse_sens", _mouseSensSlider.Value);

		cfg.SetValue("devices", "output", AudioServer.OutputDevice);
		cfg.SetValue("devices", "input", AudioServer.InputDevice);

		if (VoiceManager.Instance != null)
			cfg.SetValue("audio", "baseline", VoiceManager.Instance.BaselineAmplitude);

		if (NetworkManager.Instance != null)
		{
			cfg.SetValue("network", "matchmaker_address", NetworkManager.Instance.MatchmakerAddress);
			cfg.SetValue("network", "matchmaker_port", NetworkManager.Instance.MatchmakerPort);
			cfg.SetValue("network", "host_port", NetworkManager.Instance.HostPort);
			cfg.SetValue("network", "allow_direct_ip_room_codes", NetworkManager.Instance.AllowDirectIpRoomCodes);
		}

		// Accessibility Toggles
		if (_toggleSubtitles != null) cfg.SetValue("settings", "subtitles", _toggleSubtitles.ButtonPressed);
		if (_toggleProximityPulse != null) cfg.SetValue("settings", "proximity_pulse", _toggleProximityPulse.ButtonPressed);
		if (_toggleTextBroadcaster != null) cfg.SetValue("settings", "text_broadcaster", _toggleTextBroadcaster.ButtonPressed);

		// NOTE: gdpr_accepted and gdpr_timestamp are intentionally NOT written here.
		// Consent is a deliberate, one-time user action — it must only be recorded
		// from OnGDPRAccepted() in MainMenu.cs. Writing it here would silently
		// re-grant consent on every settings change, defeating GDPR Article 7(3).

		cfg.Save(ConfigPath);
	}

	public void LoadSettings()
	{
		_loadingSettings = true;
		var cfg = new ConfigFile();
		Error loadError = cfg.Load(ConfigPath);
		string profileName = (string)cfg.GetValue("player", "name", "Player_" + new Random().Next(1000, 9999));
		int profileRuns = (int)cfg.GetValue("player", "runs", 0);

		if (_profileNameInput != null)
			_profileNameInput.Text = profileName;
		if (_profileRunsValueLabel != null)
			_profileRunsValueLabel.Text = profileRuns.ToString();
		if (_profileStatusLabel != null)
			_profileStatusLabel.Text = "Change your public player name here.";

		if (loadError == Error.Ok)
		{
			double master = (double)cfg.GetValue("audio", "master", 100.0);
			double sfx = (double)cfg.GetValue("audio", "sfx", 80.0);
			double vc = (double)cfg.GetValue("audio", "vc", 80.0);
			double listener = (double)cfg.GetValue("audio", "listener", 80.0);
			double mouse = (double)cfg.GetValue("controls", "mouse_sens", 0.2);

			string output = (string)cfg.GetValue("devices", "output", "Default");
			string input = (string)cfg.GetValue("devices", "input", "Default");
			float baseline = (float)cfg.GetValue("audio", "baseline", 0.05f);

			bool subs = (bool)cfg.GetValue("settings", "subtitles", false);
			bool pulse = (bool)cfg.GetValue("settings", "proximity_pulse", false);
			bool textBroad = (bool)cfg.GetValue("settings", "text_broadcaster", false);
			long gdprTime = (long)cfg.GetValue("settings", "gdpr_timestamp", 0L);

			if (NetworkManager.Instance != null)
			{
				NetworkManager.Instance.MatchmakerAddress = (string)cfg.GetValue("network", "matchmaker_address", NetworkManager.DevFallbackAddress);
				NetworkManager.Instance.MatchmakerPort = (int)cfg.GetValue("network", "matchmaker_port", NetworkManager.DefaultPort);
				NetworkManager.Instance.HostPort = (int)cfg.GetValue("network", "host_port", NetworkManager.DefaultHostPort);
				NetworkManager.Instance.AllowDirectIpRoomCodes = (bool)cfg.GetValue("network", "allow_direct_ip_room_codes", true);
				NetworkManager.Instance.PlayerName = profileName;
				NetworkManager.Instance.PlayerRuns = profileRuns;
			}

			// Apply values
			if (_masterVolumeSlider != null) _masterVolumeSlider.Value = master;
			if (_sfxVolumeSlider != null) _sfxVolumeSlider.Value = sfx;
			if (_vcVolumeSlider != null) _vcVolumeSlider.Value = vc;
			if (_listenerVolumeSlider != null) _listenerVolumeSlider.Value = listener;
			if (_mouseSensSlider != null) _mouseSensSlider.Value = mouse;

			if (_toggleSubtitles != null) _toggleSubtitles.ButtonPressed = subs;
			if (_toggleProximityPulse != null) _toggleProximityPulse.ButtonPressed = pulse;
			if (_toggleTextBroadcaster != null)
			{
				_toggleTextBroadcaster.ButtonPressed = textBroad;
				if (_textBroadcasterWarning != null)
					_textBroadcasterWarning.Visible = textBroad;
			}

			if (_privacyStatusLabel != null)
			{
				if (gdprTime > 0L)
				{
					var dt = DateTimeOffset.FromUnixTimeSeconds(gdprTime).LocalDateTime;
					_privacyStatusLabel.Text = $"CONSENT STATUS: Acknowledged ({dt:yyyy-MM-dd HH:mm})";
				}
				else
				{
					_privacyStatusLabel.Text = "CONSENT STATUS: Not acknowledged";
				}
			}

			if (VoiceManager.Instance != null)
				VoiceManager.Instance.BaselineAmplitude = baseline;

			AudioServer.OutputDevice = output;
			SelectOptionByText(_outputDeviceOption, output);
			AudioServer.InputDevice = input;
			SelectOptionByText(_micDeviceOption, input);

			ApplyMasterVolume(master);
			ApplySfxVolume(sfx);
			ApplyVoiceVolume(vc);
			ApplyListenerVolume(listener);
			OnMouseSensChanged(mouse);

			SetMicMonitor(false);
			VoiceManager.Instance?.RestartCapture();
		}
		else
		{
			// Apply defaults
			if (_masterVolumeSlider != null) ApplyMasterVolume(_masterVolumeSlider.Value);
			if (_sfxVolumeSlider != null) ApplySfxVolume(_sfxVolumeSlider.Value);
			if (_vcVolumeSlider != null) ApplyVoiceVolume(_vcVolumeSlider.Value);
			if (_listenerVolumeSlider != null) ApplyListenerVolume(_listenerVolumeSlider.Value);
			if (_mouseSensSlider != null) OnMouseSensChanged(_mouseSensSlider.Value);

			if (_privacyStatusLabel != null)
			{
				_privacyStatusLabel.Text = "CONSENT STATUS: Not acknowledged";
			}
		}
		_loadingSettings = false;
	}

	private void SelectOptionByText(OptionButton btn, string text)
	{
		if (btn == null) return;
		for (int i = 0; i < btn.ItemCount; i++)
		{
			if (btn.GetItemText(i) == text)
			{
				btn.Selected = i;
				return;
			}
		}
	}

	private void OnViewPrivacyPressed()
	{
		var dialog = new AcceptDialog();
		dialog.Title = "PRIVACY & AUDIO NOTICE";
		dialog.Size = new Vector2I(600, 400);

		var scroll = new ScrollContainer();
		scroll.CustomMinimumSize = new Vector2I(560, 300);

		var label = new Label();
		label.Text = "GDPR PRIVACY NOTICE & POLICY (POST-CONSENT)\n\n" +
			"EARLY ACCESS NOTICE: Voice recording is not active in this version. This notice describes a planned post-launch feature. Your acknowledgement is recorded for when the feature activates.\n\n" +
			"This game requires a working microphone. Your voice audio is processed locally to detect amplitude (volume) and drive core gameplay mechanics. No voice data is transmitted externally or saved to disk.\n\n" +
			"By continuing, you consent to local microphone processing.";
		label.AutowrapMode = TextServer.AutowrapMode.Word;
		label.CustomMinimumSize = new Vector2I(540, 0);

		scroll.AddChild(label);
		dialog.AddChild(scroll);
		
		// Clean up on close
		dialog.VisibilityChanged += () => {
			if (!dialog.Visible)
			{
				dialog.QueueFree();
			}
		};

		AddChild(dialog);
		dialog.PopupCentered();
	}

	private void OnRevokeConsentPressed()
	{
		var dialog = new ConfirmationDialog();
		dialog.Title = "REVOKE CONSENT";
		dialog.Size = new Vector2I(500, 150);

		var label = new Label();
		label.Text = "Revoking consent will disable voice processing entirely. You will need to accept the privacy notice again on the main menu to use voice features. Continue?";
		label.AutowrapMode = TextServer.AutowrapMode.Word;
		label.CustomMinimumSize = new Vector2I(440, 0);

		dialog.AddChild(label);
		
		dialog.Confirmed += () => {
			var cfg = new ConfigFile();
			cfg.Load("user://settings.cfg");
			cfg.SetValue("settings", "gdpr_accepted", false);
			cfg.SetValue("settings", "gdpr_timestamp", 0L);
			cfg.Save("user://settings.cfg");

			if (VoiceManager.Instance != null)
			{
				VoiceManager.Instance.SetGdprAccepted(false);
				GD.Print("SettingsMenu: Live GDPR state revoked in VoiceManager.");
			}

			if (_privacyStatusLabel != null)
				_privacyStatusLabel.Text = "CONSENT STATUS: Not acknowledged";
		};

		// Clean up on close
		dialog.VisibilityChanged += () => {
			if (!dialog.Visible)
			{
				dialog.QueueFree();
			}
		};

		AddChild(dialog);
		dialog.PopupCentered();
	}
}
