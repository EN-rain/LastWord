using Godot;
using System;

public partial class MainMenu : CanvasLayer
{
	[Export] public NodePath PlayRandomBtnPath;
	[Export] public NodePath CreateCustomBtnPath;
	[Export] public NodePath QuitBtnPath;
	[Export] public NodePath NameInputPath;
	[Export] public NodePath StatusLabelPath;
	[Export] public NodePath CodeInputPath;
	[Export] public NodePath JoinByCodeBtnPath;
	[Export] public NodePath SettingsBtnPath;
	[Export] public NodePath GDPRPanelPath;
	[Export] public NodePath GDPRAcceptBtnPath;
	[Export] public NodePath SettingsMenuPath;
	[Export] public NodePath JoinCodePopupPath;
	[Export] public NodePath ConfirmJoinBtnPath;
	[Export] public NodePath CancelJoinBtnPath;
	[Export] public NodePath GDPRNoticeTextLabelPath;
	[Export(PropertyHint.MultilineText)] public string GDPRNoticeText = "EARLY ACCESS NOTICE: Voice recording is not active in this version. This notice describes a planned post-launch feature. Your acknowledgement is recorded for when the feature activates.\n\nThis game requires a working microphone. Your voice audio is processed locally to detect amplitude (volume) and drive core gameplay mechanics. No voice data is transmitted externally or saved to disk.\n\nBy continuing, you consent to local microphone processing.";


	private Button   _playRandomBtn;
	private Button   _createCustomBtn;
	private Button   _quitBtn;
	private LineEdit _nameInput;
	private Label    _statusLabel;
	private LineEdit _codeInput;
	private Button   _joinByCodeBtn;
	private Button   _settingsBtn;
	private Control  _gdprPanel;
	private Button   _gdprAcceptBtn;
	private Control  _settingsMenu;
	private Control  _joinCodePopup;
	private Button   _confirmJoinBtn;
	private Button   _cancelJoinBtn;


	private bool _isConnectingRandom   = false;
	private bool _isConnectedToMatchmaker = false;
	private int  _matchmakingAttemptCount = 0;

	// --- Scene Paths ---
	[Export(PropertyHint.File, "*.tscn")] public string CustomLobbyScenePath;
	[Export(PropertyHint.File, "*.tscn")] public string MatchmakingLobbyScenePath;

	// --- Hardcoded Texts ---
	[Export] public string StatusReadyText = "READY TO ENTER THE ESTEEMED ESTATE...";
	[Export] public string StatusConnectingText = "CONNECTING TO MATCHMAKING SERVER...";
	[Export] public string StatusFailedMatchmakerText = "FAILED TO REACH MATCHMAKING SERVER";
	[Export] public string StatusMatchmakerOfflineText = "MATCHMAKER OFFLINE";
	[Export] public string StatusCreatingCustomText = "CREATING CUSTOM ROOM...";
	[Export] public string StatusRoomReadyText = "ROOM READY. OPENING LOBBY...";
	[Export] public string StatusPleaseEnterCodeText = "PLEASE ENTER A ROOM CODE";
	[Export] public string StatusInvalidCodeText = "INVALID ROOM CODE FORMAT";
	[Export] public string StatusSettingsSceneMissingText = "ERROR: SETTINGS SCENE NOT FOUND";
	[Export] public string StatusNetworkUnavailableText = "NETWORK SYSTEM NOT READY";
	[Export] public string StatusConnectingPrivateText = "ROOM CODE ACCEPTED. CONNECTING...";
	[Export] public string StatusDirectCodeWarningText = "PRIVATE CODES USE DIRECT-IP DEV/LAN MODE UNTIL STEAM RELAY IS INTEGRATED.";

	// --- Hardcoded Colors ---
	[Export] public Color ColorTextNormal  = new Color(0.85f, 0.85f, 0.85f);
	[Export] public Color ColorTextAccent  = new Color(0.9f,  0.2f,  0.2f);
	[Export] public Color ColorTextSuccess = new Color(0.2f,  0.85f, 0.2f);

	// --- Timeouts ---
	[Export] public float MatchmakingTimeoutSeconds = 10.0f;

	public override void _Ready()
	{

		_playRandomBtn   = GetNodeOrNull<Button>(PlayRandomBtnPath);
		_createCustomBtn = GetNodeOrNull<Button>(CreateCustomBtnPath);
		_quitBtn         = GetNodeOrNull<Button>(QuitBtnPath);
		_nameInput       = GetNodeOrNull<LineEdit>(NameInputPath);
		_statusLabel     = GetNodeOrNull<Label>(StatusLabelPath);
		_codeInput       = GetNodeOrNull<LineEdit>(CodeInputPath);
		_joinByCodeBtn   = GetNodeOrNull<Button>(JoinByCodeBtnPath);
		_settingsBtn     = GetNodeOrNull<Button>(SettingsBtnPath);
		_gdprPanel       = GetNodeOrNull<Control>(GDPRPanelPath);
		_gdprAcceptBtn   = GetNodeOrNull<Button>(GDPRAcceptBtnPath);
		_settingsMenu    = GetNodeOrNull<Control>(SettingsMenuPath);
		_joinCodePopup   = GetNodeOrNull<Control>(JoinCodePopupPath);
		_confirmJoinBtn  = GetNodeOrNull<Button>(ConfirmJoinBtnPath);
		_cancelJoinBtn   = GetNodeOrNull<Button>(CancelJoinBtnPath);

		if (GDPRNoticeTextLabelPath != null)
		{
			var gdprLabel = GetNodeOrNull<Label>(GDPRNoticeTextLabelPath);
			if (gdprLabel != null)
				gdprLabel.Text = GDPRNoticeText;
		}

		// Warn about missing critical nodes so failures are never silent
		if (_nameInput == null)
			GD.PushWarning("MainMenu: NameInput node not found — player name will fall back to random.");
		if (_statusLabel == null)
			GD.PushWarning("MainMenu: StatusLabel node not found — status updates will be silent.");

		if (_settingsMenu != null)
			_settingsMenu.Visible = false;

		if (_joinCodePopup != null)
			_joinCodePopup.Visible = false;

		if (_confirmJoinBtn != null)
			_confirmJoinBtn.Pressed += OnConfirmJoinPressed;

		if (_cancelJoinBtn != null)
			_cancelJoinBtn.Pressed += OnCancelJoinPressed;

		if (_codeInput != null)
			_codeInput.TextSubmitted += (_) => OnConfirmJoinPressed();

		if (_nameInput != null)
			_nameInput.TextSubmitted += (_) =>
			{
				SavePlayerProfile();
				_nameInput.ReleaseFocus();
			};

		// Load saved profile into NetworkManager so it's available on connect
		LoadPlayerProfile();

		// Pre-fill name input with saved profile name
		if (_nameInput != null && NetworkManager.Instance != null)
			_nameInput.Text = NetworkManager.Instance.PlayerName;

		CheckGDPR();

		// Subscribe to NetworkManager signals
		if (NetworkManager.Instance != null)
		{
			NetworkManager.Instance.ConnectionSuccess        += OnConnectionSuccess;
			NetworkManager.Instance.ConnectionFailed         += OnConnectionFailed;
			NetworkManager.Instance.ServerDisconnected       += OnServerDisconnected;
			NetworkManager.Instance.MatchmakingStatusUpdated += OnMatchmakingStatusUpdated;
			NetworkManager.Instance.LobbyFormed              += OnLobbyFormed;
		}

		UpdateStatus(StatusReadyText, ColorTextNormal);
	}

	public override void _ExitTree()
	{
		// Unsubscribe all NetworkManager signals to prevent stale callbacks
		// after scene transitions. Forgetting this causes callbacks to fire
		// on freed objects which leads to hard-to-trace crashes.
		if (NetworkManager.Instance != null)
		{
			NetworkManager.Instance.ConnectionSuccess        -= OnConnectionSuccess;
			NetworkManager.Instance.ConnectionFailed         -= OnConnectionFailed;
			NetworkManager.Instance.ServerDisconnected       -= OnServerDisconnected;
			NetworkManager.Instance.MatchmakingStatusUpdated -= OnMatchmakingStatusUpdated;
			NetworkManager.Instance.LobbyFormed              -= OnLobbyFormed;
		}
	}

	// ---------------------------------------------------------------------------
	// Profile helpers
	// ---------------------------------------------------------------------------
	private void LoadPlayerProfile()
	{
		var cfg = new ConfigFile();
		cfg.Load("user://settings.cfg");

		string name = (string)cfg.GetValue("player", "name", "Player_" + new Random().Next(1000, 9999));
		int    runs = (int)   cfg.GetValue("player", "runs", 0);

		if (NetworkManager.Instance != null)
		{
			NetworkManager.Instance.PlayerName = name;
			NetworkManager.Instance.PlayerRuns = runs;
		}
	}

	private void SavePlayerProfile()
	{
		if (_nameInput == null)
		{
			GD.PushError("MainMenu: _nameInput is null! Cannot save player name properly. Falling back to random.");
			UpdateStatus("ERROR: NAME INPUT NODE MISSING", ColorTextAccent);
		}

		string name = _nameInput != null ? _nameInput.Text.Trim() : "";
		name = name.Replace(":", "").Replace("|", ""); // Sanitize for serialization
		if (string.IsNullOrEmpty(name))
			name = "Player_" + new Random().Next(1000, 9999);

		var cfg = new ConfigFile();
		cfg.Load("user://settings.cfg");
		cfg.SetValue("player", "name", name);
		cfg.Save("user://settings.cfg");

		if (NetworkManager.Instance != null)
		{
			NetworkManager.Instance.PlayerName = name;
		}
	}

	private void CheckGDPR()
	{
		var cfg = new ConfigFile();
		cfg.Load("user://settings.cfg");
		bool accepted = (bool)cfg.GetValue("settings", "gdpr_accepted", false);

		if (!accepted && _gdprPanel != null)
		{
			_gdprPanel.Visible = true;
		}
	}

	private void OnGDPRAccepted()
	{
		var cfg = new ConfigFile();
		cfg.Load("user://settings.cfg");
		cfg.SetValue("settings", "gdpr_accepted", true);
		cfg.SetValue("settings", "gdpr_timestamp", DateTimeOffset.UtcNow.ToUnixTimeSeconds());
		cfg.Save("user://settings.cfg");

		if (VoiceManager.Instance != null)
		{
			VoiceManager.Instance.SetGdprAccepted(true);
		}

		if (_settingsMenu is SettingsMenu menu)
			menu.LoadSettings();

		if (_gdprPanel != null)
		{
			_gdprPanel.Visible = false;
		}
	}

	private void UpdateStatus(string text, Color color)
	{
		if (_statusLabel == null) return;
		_statusLabel.Text     = text.ToUpper();
		_statusLabel.Modulate = color;
	}

	private void CancelMatchmaking()
	{
		_isConnectingRandom      = false;
		_isConnectedToMatchmaker = false;
		_matchmakingAttemptCount++;
	}

	private void PrepareNetworkCleanSlate()
	{
		CancelMatchmaking();
		NetworkManager.Instance?.Disconnect();
	}

	// ---------------------------------------------------------------------------
	// Button handlers
	// ---------------------------------------------------------------------------

	// PLAY RANDOM — connects to the configured matchmaking server, then transitions
	// to MatchmakingLobby.tscn once the server sends LobbyFormed.
	// NOTE: MatchmakerAddress defaults to 127.0.0.1 (dev only). Set a real
	// server address via the Inspector or --matchmaker-address= CLI flag before shipping.
	private void OnPlayRandomPressed()
	{
		SavePlayerProfile();
		PrepareNetworkCleanSlate();

		_isConnectingRandom = true;
		int currentAttempt  = _matchmakingAttemptCount;
		UpdateStatus(StatusConnectingText, ColorTextNormal);

		if (NetworkManager.Instance == null)
		{
			_isConnectingRandom = false;
			UpdateStatus(StatusNetworkUnavailableText, ColorTextAccent);
			return;
		}

		// Use the configurable address/port — never hardcode DefaultAddress here.
		string addr = NetworkManager.Instance.MatchmakerAddress;
		int    port = NetworkManager.Instance.MatchmakerPort;

		Error err = NetworkManager.Instance.JoinLobby(addr, port, NetworkManager.ConnectionMode.Matchmaking);

		if (err != Error.Ok)
		{
			_isConnectingRandom = false;
			UpdateStatus(StatusFailedMatchmakerText, ColorTextAccent);
			return;
		}

		// Connection timeout guard
		var timer = GetTree().CreateTimer(MatchmakingTimeoutSeconds);
		timer.Timeout += () =>
		{
			if (_isConnectingRandom && !_isConnectedToMatchmaker
					&& _matchmakingAttemptCount == currentAttempt)
			{
				NetworkManager.Instance?.Disconnect();
				_isConnectingRandom = false;
				UpdateStatus(StatusMatchmakerOfflineText, ColorTextAccent);
			}
		};
	}

	// CREATE CUSTOM — hosts a listen-server on the configured port and opens CustomLobby.tscn.
	private void OnCustomRoomPressed()
	{
		SavePlayerProfile();
		PrepareNetworkCleanSlate();
		UpdateStatus(StatusCreatingCustomText, ColorTextNormal);

		if (NetworkManager.Instance == null)
		{
			UpdateStatus(StatusNetworkUnavailableText, ColorTextAccent);
			return;
		}

		// Use the configurable port — never hardcode DefaultPort here.
		int port = NetworkManager.Instance.HostPort;
		Error err = NetworkManager.Instance.CreateHost(port);
		if (err == Error.Ok)
		{
			UpdateStatus(StatusRoomReadyText, ColorTextSuccess);
			GetTree().ChangeSceneToFile(CustomLobbyScenePath);
		}
		else
		{
			UpdateStatus($"FAILED TO HOST ON PORT {port}", ColorTextAccent);
		}
	}

	// JOIN BY CODE — opens the popup.
	private void OnJoinByCodePressed()
	{
		if (_joinCodePopup != null)
		{
			_joinCodePopup.Visible = true;
			if (_codeInput != null)
			{
				_codeInput.Text = "";
				_codeInput.GrabFocus();
			}
		}
		else
		{
			OnConfirmJoinPressed();
		}
	}

	private void OnCancelJoinPressed()
	{
		if (_joinCodePopup != null)
		{
			_joinCodePopup.Visible = false;
		}
		UpdateStatus(StatusReadyText, ColorTextNormal);
	}

	private void OnConfirmJoinPressed()
	{
		SavePlayerProfile();
		PrepareNetworkCleanSlate();

		string code = _codeInput != null ? _codeInput.Text.Trim().ToUpper() : "";
		if (string.IsNullOrEmpty(code))
		{
			UpdateStatus(StatusPleaseEnterCodeText, ColorTextAccent);
			return;
		}

		if (NetworkManager.Instance == null)
		{
			UpdateStatus(StatusNetworkUnavailableText, ColorTextAccent);
			return;
		}

		if (!NetworkManager.Instance.TryRoomCodeToIp(code, out string ip, out string decodeError))
		{
			UpdateStatus(StatusInvalidCodeText, ColorTextAccent);
			return;
		}

		if (_joinCodePopup != null)
		{
			_joinCodePopup.Visible = false;
		}

		UpdateStatus(StatusConnectingPrivateText, ColorTextNormal);

		// Warn about direct IP connection for internet sessions without relay.
		// Only 172.16.0.0/12 (second octet 16-31) is RFC1918 private space;
		// addresses like 172.1.x.x or 172.32.x.x are public.
		if (!IsRfc1918(ip))
		{
			GD.PushWarning($"MainMenu: Direct-IP connect to {ip} attempted. This will fail for internet hosts behind NAT until Steam Relay is integrated.");
		}

		NetworkManager.Instance.CurrentRoomCode = code;
		Error err = NetworkManager.Instance.JoinLobby(ip, NetworkManager.Instance.HostPort, NetworkManager.ConnectionMode.PrivateCode);
		if (err != Error.Ok)
		{
			UpdateStatus("FAILED TO INITIATE CONNECTION", ColorTextAccent);
		}
		// Transition to CustomLobby fires from OnConnectionSuccess (non-matchmaking path)
	}

	/// <summary>Returns true for all RFC1918 private IPv4 addresses.</summary>
	private static bool IsRfc1918(string ip)
	{
		if (ip == "127.0.0.1") return true;
		if (ip.StartsWith("10.")) return true;
		if (ip.StartsWith("192.168.")) return true;
		// 172.16.0.0/12 → second octet must be 16–31
		if (ip.StartsWith("172."))
		{
			var parts = ip.Split('.');
			if (parts.Length >= 2 && int.TryParse(parts[1], out int octet2))
				return octet2 >= 16 && octet2 <= 31;
		}
		return false;
	}

	private void OnQuitPressed()
	{
		CancelMatchmaking();
		GetTree().Quit();
	}

	private void OnSettingsPressed()
	{
		if (_settingsMenu != null)
		{
			_settingsMenu.Visible = !_settingsMenu.Visible;
			if (_settingsMenu.Visible && _settingsMenu is SettingsMenu menu)
				menu.LoadSettings();
		}
		else
		{
			GD.PushError("MainMenu: SettingsMenu scene not found. Check SettingsMenuPath export.");
			UpdateStatus(StatusSettingsSceneMissingText, ColorTextAccent);
		}
	}


	// ---------------------------------------------------------------------------
	// NetworkManager callbacks
	// ---------------------------------------------------------------------------
	private void OnConnectionSuccess()
	{
		if (_isConnectingRandom)
		{
			// Matchmaking path — wait in the queue until server fires MatchStarted
			_isConnectedToMatchmaker = true;
			UpdateStatus("IN QUEUE. WAITING FOR PLAYERS...", ColorTextSuccess);
		}
		else
		{
			// Custom join path — go straight to CustomLobby
			CancelMatchmaking();
			UpdateStatus("CONNECTED! OPENING LOBBY...", ColorTextSuccess);
			GetTree().ChangeSceneToFile(CustomLobbyScenePath);
		}
	}

	private void OnMatchmakingStatusUpdated(int currentCount, int requiredCount, bool isCountdownActive)
	{
		if (!_isConnectingRandom) return;

		string status = isCountdownActive
			? $"MATCHMAKING: {currentCount}/{requiredCount} PLAYERS — STARTING SOON..."
			: $"MATCHMAKING: {currentCount}/{requiredCount} PLAYERS FOUND...";

		UpdateStatus(status, ColorTextSuccess);
	}

	private void OnLobbyFormed()
	{
		if (!_isConnectingRandom) return;
		CancelMatchmaking();
		UpdateStatus("MATCH FOUND! ENTERING LOBBY...", ColorTextSuccess);
		GetTree().ChangeSceneToFile(MatchmakingLobbyScenePath);
	}

	private void OnConnectionFailed()
	{
		CancelMatchmaking();
		UpdateStatus("CONNECTION FAILED!", ColorTextAccent);
	}

	private void OnServerDisconnected()
	{
		CancelMatchmaking();
		UpdateStatus("DISCONNECTED FROM HOST", ColorTextAccent);
	}
}
