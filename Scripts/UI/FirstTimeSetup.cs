using Godot;
using System;
using System.Collections.Generic;

public partial class FirstTimeSetup : Control
{
	[Export(PropertyHint.File, "*.tscn")] public string MainMenuScenePath = "res://Scenes/MainMenu.tscn";
	[Export] public PackedScene SettingsMenuScene;

	private const string ConfigPath = "user://settings.cfg";

	private VBoxContainer _rootLayout;
	private Control[] _pages;
	private int _currentPage = 0;

	private OptionButton _regionOption;
	private CheckButton _toggleProximityPulse;
	private CheckButton _toggleSubtitles;
	private CheckButton _toggleTextBroadcaster;
	private Label _textBroadcasterWarning;
	private CheckButton _privacyAckCheck;
	private Button _finishButton;
	private Button _nextButton;
	private Button _backButton;
	private Label _pageLabel;
	private Label _statusLabel;

	private SettingsMenu _settingsMenu;
	private bool _calibrationCompleted = false;

	private static readonly string[] RegionOptions =
	{
		"NA East", "NA West", "EU West", "EU Central", "SEA", "OCE"
	};

	private readonly Dictionary<string, string> _defaultActionLabels = new()
	{
		{ "move_forward", "Move Forward" },
		{ "move_backward", "Move Backward" },
		{ "move_left", "Move Left" },
		{ "move_right", "Move Right" },
		{ "move_jump", "Jump" },
		{ "move_sprint", "Sprint" },
		{ "gesture_z", "Gesture Z" },
		{ "gesture_x", "Gesture X" },
		{ "gesture_c", "Gesture C" },
		{ "gesture_v", "Gesture V" },
		{ "gesture_b", "Gesture B" },
		{ "gesture_n", "Gesture N" },
		{ "gesture_m", "Gesture M" },
		{ "gesture_l", "Gesture L" },
		{ "clap_q", "Clap" },
		{ "sacrifice_g", "Vocal Sacrifice" },
		{ "radial_wheel_mmb", "Radial Wheel" },
		{ "spectator_j", "Spectator Marker (J)" }
	};

	public override void _Ready()
	{
		AnchorRight = 1f;
		AnchorBottom = 1f;
		GrowHorizontal = GrowDirection.Both;
		GrowVertical = GrowDirection.Both;

		_rootLayout = new VBoxContainer
		{
			AnchorRight = 1f,
			AnchorBottom = 1f,
			OffsetLeft = 40f,
			OffsetTop = 40f,
			OffsetRight = -40f,
			OffsetBottom = -40f,
			GrowHorizontal = GrowDirection.Both,
			GrowVertical = GrowDirection.Both,
			Alignment = BoxContainer.AlignmentMode.Center
		};
		AddChild(_rootLayout);

		var title = new Label
		{
			Text = "FIRST-TIME SETUP",
			HorizontalAlignment = HorizontalAlignment.Center
		};
		title.AddThemeFontSizeOverride("font_size", 32);
		_rootLayout.AddChild(title);

		_pageLabel = new Label
		{
			Text = "Step 1 of 5",
			HorizontalAlignment = HorizontalAlignment.Center
		};
		_rootLayout.AddChild(_pageLabel);

		_statusLabel = new Label
		{
			Text = "",
			HorizontalAlignment = HorizontalAlignment.Center,
			AutowrapMode = TextServer.AutowrapMode.Word
		};
		_rootLayout.AddChild(_statusLabel);

		_pages = new Control[5];
		_pages[0] = CreateRegionPage();
		_pages[1] = CreateCalibrationPage();
		_pages[2] = CreateAccessibilityPage();
		_pages[3] = CreateKeybindPage();
		_pages[4] = CreatePrivacyPage();

		foreach (var page in _pages)
		{
			page.SizeFlagsVertical = SizeFlags.ExpandFill;
			page.Visible = false;
			_rootLayout.AddChild(page);
		}

		var navRow = new HBoxContainer
		{
			Alignment = BoxContainer.AlignmentMode.End
		};
		navRow.AddThemeConstantOverride("separation", 12);
		_rootLayout.AddChild(navRow);

		_backButton = new Button { Text = "Back" };
		_backButton.Pressed += OnBackPressed;
		navRow.AddChild(_backButton);

		_nextButton = new Button { Text = "Next" };
		_nextButton.Pressed += OnNextPressed;
		navRow.AddChild(_nextButton);

		_finishButton = new Button { Text = "Finish" };
		_finishButton.Pressed += OnFinishPressed;
		_finishButton.Disabled = true;
		navRow.AddChild(_finishButton);

		LoadExistingSettings();
		if (VoiceManager.Instance != null)
			VoiceManager.Instance.CalibrationFinished += OnCalibrationFinished;
		ShowPage(0);
	}

	public override void _ExitTree()
	{
		if (VoiceManager.Instance != null)
			VoiceManager.Instance.CalibrationFinished -= OnCalibrationFinished;
	}

	private Control CreateRegionPage()
	{
		var page = new VBoxContainer { Alignment = BoxContainer.AlignmentMode.Center };

		page.AddChild(new Label
		{
			Text = "Select your preferred matchmaking region.",
			AutowrapMode = TextServer.AutowrapMode.Word,
			HorizontalAlignment = HorizontalAlignment.Center
		});

		_regionOption = new OptionButton();
		foreach (var region in RegionOptions)
			_regionOption.AddItem(region);
		_regionOption.Select(0);
		page.AddChild(_regionOption);

		return page;
	}

	private Control CreateCalibrationPage()
	{
		var page = new VBoxContainer { Alignment = BoxContainer.AlignmentMode.Center };

		page.AddChild(new Label
		{
			Text = "Microphone calibration is mandatory. Speak normally while the meter fills.",
			AutowrapMode = TextServer.AutowrapMode.Word,
			HorizontalAlignment = HorizontalAlignment.Center
		});

		var openCalibrateBtn = new Button { Text = "Open Calibration" };
		openCalibrateBtn.Pressed += OnOpenCalibrationPressed;
		page.AddChild(openCalibrateBtn);

		if (SettingsMenuScene != null)
		{
			_settingsMenu = SettingsMenuScene.Instantiate<SettingsMenu>();
			_settingsMenu.IsSimplifiedMode = true;
			_settingsMenu.Visible = false;
			page.AddChild(_settingsMenu);
		}

		return page;
	}

	private Control CreateAccessibilityPage()
	{
		var page = new VBoxContainer { Alignment = BoxContainer.AlignmentMode.Center };

		page.AddChild(new Label
		{
			Text = "Accessibility options (all off by default).",
			HorizontalAlignment = HorizontalAlignment.Center
		});

		_toggleProximityPulse = new CheckButton { Text = "Listener Proximity Pulse" };
		page.AddChild(_toggleProximityPulse);

		page.AddChild(new Label
		{
			Text = "Subtitles for Listener audio events."
		});
		if (page.GetChild(page.GetChildCount() - 1) is Label subtitlesLabel)
			subtitlesLabel.AddThemeFontSizeOverride("font_size", 12);
		_toggleSubtitles = new CheckButton { Text = "Subtitles" };
		page.AddChild(_toggleSubtitles);

		_toggleTextBroadcaster = new CheckButton { Text = "Text Broadcaster mode (Phase 3 only)" };
		_toggleTextBroadcaster.Toggled += OnTextBroadcasterToggled;
		page.AddChild(_toggleTextBroadcaster);

		_textBroadcasterWarning = new Label
		{
			Text = "Warning: Text Broadcaster cannot complete Phase 1 or Phase 2 alone. If you are the sole survivor entering Phase 2, the run fails.",
			AutowrapMode = TextServer.AutowrapMode.Word,
			Visible = false
		};
		_textBroadcasterWarning.AddThemeColorOverride("font_color", Colors.Orange);
		page.AddChild(_textBroadcasterWarning);

		return page;
	}

	private Control CreateKeybindPage()
	{
		var page = new VBoxContainer { Alignment = BoxContainer.AlignmentMode.Begin };

		page.AddChild(new Label
		{
			Text = "Default controls. All keys are remappable in Settings > Controls.",
			AutowrapMode = TextServer.AutowrapMode.Word,
			HorizontalAlignment = HorizontalAlignment.Center
		});

		var scroll = new ScrollContainer { SizeFlagsVertical = SizeFlags.ExpandFill };
		var list = new VBoxContainer();

		foreach (var action in _defaultActionLabels)
		{
			string bound = "[UNBOUND]";
			var events = InputMap.ActionGetEvents(action.Key);
			if (events.Count > 0)
			{
				bound = events[0].AsText();
			}
			list.AddChild(new Label { Text = $"{action.Value}: {bound}" });
		}

		scroll.AddChild(list);
		page.AddChild(scroll);

		return page;
	}

	private Control CreatePrivacyPage()
	{
		var page = new VBoxContainer { Alignment = BoxContainer.AlignmentMode.Begin };

		var title = new Label
		{
			Text = "Privacy & Voice Data Notice",
			HorizontalAlignment = HorizontalAlignment.Center
		};
		title.AddThemeFontSizeOverride("font_size", 24);
		page.AddChild(title);

		var scroll = new ScrollContainer { SizeFlagsVertical = SizeFlags.ExpandFill };
		var notice = new Label
		{
			Text = "EARLY ACCESS NOTICE: Voice recording is not active in this version. This notice describes a planned post-launch feature. Your acknowledgement is recorded for when the feature activates.\n\n" +
				   "This game requires a working microphone. Your voice audio is processed locally to detect amplitude (volume) and drive core gameplay mechanics. No voice data is transmitted externally or saved to disk.\n\n" +
				   "By continuing, you consent to local microphone processing.",
			AutowrapMode = TextServer.AutowrapMode.Word
		};
		scroll.AddChild(notice);
		page.AddChild(scroll);

		_privacyAckCheck = new CheckButton { Text = "I understand and agree" };
		_privacyAckCheck.Toggled += OnPrivacyToggled;
		page.AddChild(_privacyAckCheck);

		return page;
	}

	private void LoadExistingSettings()
	{
		var cfg = new ConfigFile();
		if (cfg.Load(ConfigPath) != Error.Ok) return;

		string region = (string)cfg.GetValue("settings", "region", "NA East");
		for (int i = 0; i < RegionOptions.Length; i++)
		{
			if (RegionOptions[i] == region)
			{
				_regionOption.Select(i);
				break;
			}
		}

		_toggleSubtitles.ButtonPressed = (bool)cfg.GetValue("settings", "subtitles", false);
		_toggleProximityPulse.ButtonPressed = (bool)cfg.GetValue("settings", "proximity_pulse", false);
		_toggleTextBroadcaster.ButtonPressed = (bool)cfg.GetValue("settings", "text_broadcaster", false);
		OnTextBroadcasterToggled(_toggleTextBroadcaster.ButtonPressed);

		_calibrationCompleted = (bool)cfg.GetValue("settings", "setup_complete", false);
	}

	private void ShowPage(int index)
	{
		for (int i = 0; i < _pages.Length; i++)
			_pages[i].Visible = (i == index);

		_currentPage = index;
		_pageLabel.Text = $"Step {index + 1} of {_pages.Length}";
		_backButton.Visible = index > 0;
		_nextButton.Visible = index < _pages.Length - 1;
		_nextButton.Disabled = index == 1 && !_calibrationCompleted;
		_finishButton.Visible = index == _pages.Length - 1;
		_finishButton.Disabled = !CanFinishSetup();

		if (index == 1)
		{
			_statusLabel.Text = _calibrationCompleted
				? "Calibration already completed. You may re-run it."
				: "Open the calibration panel and complete the 30-second calibration.";
		}
		else
		{
			_statusLabel.Text = "";
		}
	}

	private void OnBackPressed()
	{
		if (_currentPage > 0)
			ShowPage(_currentPage - 1);
	}

	private void OnNextPressed()
	{
		if (_currentPage < _pages.Length - 1)
			ShowPage(_currentPage + 1);
	}

	private void OnOpenCalibrationPressed()
	{
		if (_settingsMenu == null)
		{
			_statusLabel.Text = "Settings scene not assigned. Calibration cannot be completed.";
			return;
		}

		_settingsMenu.Visible = true;
		if (_settingsMenu.GetParent() != null)
			_settingsMenu.GetParent().MoveChild(_settingsMenu, -1);
	}

	private void OnTextBroadcasterToggled(bool enabled)
	{
		if (_textBroadcasterWarning != null)
			_textBroadcasterWarning.Visible = enabled;
	}

	private void OnPrivacyToggled(bool enabled)
	{
		if (_finishButton != null)
			_finishButton.Disabled = !CanFinishSetup();
	}

	private void OnFinishPressed()
	{
		if (!CanFinishSetup())
		{
			_statusLabel.Text = !_calibrationCompleted
				? "Complete microphone calibration before finishing setup."
				: "Accept the privacy notice before finishing setup.";
			return;
		}

		var cfg = new ConfigFile();
		cfg.Load(ConfigPath);

		cfg.SetValue("settings", "region", _regionOption.GetItemText(_regionOption.Selected));
		cfg.SetValue("settings", "subtitles", _toggleSubtitles.ButtonPressed);
		cfg.SetValue("settings", "proximity_pulse", _toggleProximityPulse.ButtonPressed);
		cfg.SetValue("settings", "text_broadcaster", _toggleTextBroadcaster.ButtonPressed);
		cfg.SetValue("settings", "setup_complete", true);
		cfg.SetValue("settings", "gdpr_accepted", true);
		cfg.SetValue("settings", "gdpr_timestamp", DateTimeOffset.UtcNow.ToUnixTimeSeconds());

		cfg.Save(ConfigPath);

		if (VoiceManager.Instance != null)
			VoiceManager.Instance.SetGdprAccepted(true);

		GetTree().ChangeSceneToFile(MainMenuScenePath);
	}

	private void OnCalibrationFinished(float _newBaseline)
	{
		_calibrationCompleted = true;
		_statusLabel.Text = "Calibration complete.";
		if (_currentPage == 1)
			_nextButton.Disabled = false;
		if (_finishButton != null)
			_finishButton.Disabled = !CanFinishSetup();
	}

	private bool CanFinishSetup()
	{
		return _calibrationCompleted && _privacyAckCheck != null && _privacyAckCheck.ButtonPressed;
	}
}
