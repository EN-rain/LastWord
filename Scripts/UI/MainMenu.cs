using Godot;
using System;

public partial class MainMenu : CanvasLayer
{
    [Export] public NodePath PlayRandomBtnPath;
    [Export] public NodePath CreateCustomBtnPath;
    [Export] public NodePath JoinRoomBtnPath;
    [Export] public NodePath QuitBtnPath;
    [Export] public NodePath AddressInputPath;
    [Export] public NodePath NameInputPath;
    [Export] public NodePath StatusLabelPath;
    [Export] public NodePath CodeInputPath;
    [Export] public NodePath JoinByCodeBtnPath;

    private Button _playRandomBtn;
    private Button _createCustomBtn;
    private Button _joinRoomBtn;
    private Button _quitBtn;
    private LineEdit _addressInput;
    private LineEdit _nameInput;
    private Label _statusLabel;
    private LineEdit _codeInput;
    private Button _joinByCodeBtn;

    private SceneTreeTimer _matchmakingTimer;
    private bool _isConnectingRandom = false;
    private int _matchmakingAttemptCount = 0;

    private static readonly Color ColorTextNormal = new Color(0.85f, 0.85f, 0.85f);
    private static readonly Color ColorTextAccent = new Color(0.9f, 0.2f, 0.2f); // Glow Crimson
    private static readonly Color ColorTextSuccess = new Color(0.2f, 0.85f, 0.2f); // Neon Green

    public override void _Ready()
    {
        GD.Print("MainMenu: _Ready starting...");

        _playRandomBtn  = GetNodeOrNull<Button>(PlayRandomBtnPath);
        _createCustomBtn = GetNodeOrNull<Button>(CreateCustomBtnPath);
        _joinRoomBtn    = GetNodeOrNull<Button>(JoinRoomBtnPath);
        _quitBtn        = GetNodeOrNull<Button>(QuitBtnPath);
        _addressInput   = GetNodeOrNull<LineEdit>(AddressInputPath);
        _nameInput      = GetNodeOrNull<LineEdit>(NameInputPath);
        _statusLabel    = GetNodeOrNull<Label>(StatusLabelPath);
        _codeInput      = GetNodeOrNull<LineEdit>(CodeInputPath);
        _joinByCodeBtn   = GetNodeOrNull<Button>(JoinByCodeBtnPath);

        // Dynamic Hover and Press Style Overrides for Premium Aesthetic
        StyleButtons();

        // Connect button press events
        if (_playRandomBtn != null) _playRandomBtn.Pressed += OnPlayRandomPressed;
        if (_createCustomBtn != null) _createCustomBtn.Pressed += OnCustomRoomPressed;
        if (_joinRoomBtn != null) _joinRoomBtn.Pressed += OnJoinRoomPressed;
        if (_joinByCodeBtn != null) _joinByCodeBtn.Pressed += OnJoinByCodePressed;
        if (_quitBtn != null) _quitBtn.Pressed += OnQuitPressed;

        // Connect to NetworkManager signals
        if (NetworkManager.Instance != null)
        {
            NetworkManager.Instance.ConnectionSuccess += OnConnectionSuccess;
            NetworkManager.Instance.ConnectionFailed += OnConnectionFailed;
            NetworkManager.Instance.ServerDisconnected += OnServerDisconnected;
        }

        UpdateStatus("READY TO ENTER THE ESTEEMED ESTATE...", ColorTextNormal);
    }

    private void StyleButtons()
    {
        // Add dynamic mouse hover micro-animations to buttons
        foreach (var btn in new[] { _playRandomBtn, _createCustomBtn, _joinRoomBtn, _joinByCodeBtn, _quitBtn })
        {
            if (btn == null) continue;
            
            btn.MouseEntered += () =>
            {
                btn.PivotOffset = btn.Size / 2;
                var tween = CreateTween();
                tween.SetParallel(true);
                tween.TweenProperty(btn, "scale", new Vector2(1.03f, 1.03f), 0.1f);
                tween.TweenProperty(btn, "modulate", ColorTextSuccess, 0.1f);
            };

            btn.MouseExited += () =>
            {
                btn.PivotOffset = btn.Size / 2;
                var tween = CreateTween();
                tween.SetParallel(true);
                tween.TweenProperty(btn, "scale", new Vector2(1.0f, 1.0f), 0.1f);
                tween.TweenProperty(btn, "modulate", new Color(1, 1, 1), 0.1f);
            };
        }
    }

    private void UpdateStatus(string text, Color color)
    {
        if (_statusLabel == null) return;
        _statusLabel.Text = text.ToUpper();
        _statusLabel.Modulate = color;
    }

    private void CancelMatchmaking()
    {
        _isConnectingRandom = false;
        _matchmakingAttemptCount++;
    }

    private void PrepareNetworkCleanSlate()
    {
        CancelMatchmaking();
        if (NetworkManager.Instance != null)
        {
            NetworkManager.Instance.Disconnect();
        }
    }

    private void OnPlayRandomPressed()
    {
        SavePlayerName();
        PrepareNetworkCleanSlate();

        _isConnectingRandom = true;
        int currentAttempt = _matchmakingAttemptCount;
        UpdateStatus("FINDING AN ACTIVE ROOM...", ColorTextAccent);

        if (NetworkManager.Instance != null)
        {
            // Step A: Attempt to join as a client first
            Error err = NetworkManager.Instance.JoinLobby(NetworkManager.DefaultAddress, NetworkManager.DefaultPort);
            if (err != Error.Ok)
            {
                // Fallback immediately if client initialization fails
                HostAutoLobby();
                return;
            }

            // Step B: Set a matchmaking time-out timer of 1.5 seconds.
            // If connection success signal doesn't fire by then, we auto-host.
            _matchmakingTimer = GetTree().CreateTimer(1.5f);
            _matchmakingTimer.Timeout += () =>
            {
                if (_isConnectingRandom && _matchmakingAttemptCount == currentAttempt)
                {
                    GD.Print("MainMenu: Matchmaking client connection timed out. Autohosting room...");
                    NetworkManager.Instance.Disconnect();
                    HostAutoLobby();
                }
            };
        }
    }

    private void HostAutoLobby()
    {
        _isConnectingRandom = false;
        if (NetworkManager.Instance != null)
        {
            Error err = NetworkManager.Instance.CreateHost(NetworkManager.DefaultPort);
            if (err == Error.Ok)
            {
                UpdateStatus("HOSTED RANDOM MATCHROOM", ColorTextSuccess);
                LoadGameScene();
            }
            else
            {
                UpdateStatus("MATCHMAKING ENCOUNTERED ERROR", ColorTextAccent);
            }
        }
    }

    private void OnCustomRoomPressed()
    {
        SavePlayerName();
        PrepareNetworkCleanSlate();
        UpdateStatus("CREATING CUSTOM MATCHROOM...", ColorTextSuccess);

        if (NetworkManager.Instance != null)
        {
            Error err = NetworkManager.Instance.CreateHost(NetworkManager.DefaultPort);
            if (err == Error.Ok)
            {
                UpdateStatus("ROOM INITIALIZED SUCCESSFULLY!", ColorTextSuccess);
                LoadGameScene();
            }
            else
            {
                UpdateStatus($"FAILED TO HOST ON PORT {NetworkManager.DefaultPort}", ColorTextAccent);
            }
        }
    }

    private void OnJoinRoomPressed()
    {
        SavePlayerName();
        PrepareNetworkCleanSlate();
        string address = _addressInput != null ? _addressInput.Text.Trim() : NetworkManager.DefaultAddress;
        if (string.IsNullOrEmpty(address)) address = NetworkManager.DefaultAddress;

        UpdateStatus($"CONNECTING TO SERVER AT {address}...", ColorTextAccent);

        if (NetworkManager.Instance != null)
        {
            Error err = NetworkManager.Instance.JoinLobby(address, NetworkManager.DefaultPort);
            if (err != Error.Ok)
            {
                UpdateStatus("FAILED TO INITIATE NETWORK PEER", ColorTextAccent);
            }
        }
    }

    private void OnJoinByCodePressed()
    {
        SavePlayerName();
        PrepareNetworkCleanSlate();

        string code = _codeInput != null ? _codeInput.Text.Trim().ToUpper() : "";
        if (string.IsNullOrEmpty(code))
        {
            UpdateStatus("PLEASE ENTER A ROOM CODE", ColorTextAccent);
            return;
        }

        string ip = NetworkManager.RoomCodeToIp(code);
        if (string.IsNullOrEmpty(ip) || ip == "0.0.0.0" || ip == "INVALID")
        {
            UpdateStatus("INVALID ROOM CODE FORMAT", ColorTextAccent);
            return;
        }

        UpdateStatus($"DECODED CODE... CONNECTING TO {ip}...", ColorTextAccent);

        if (NetworkManager.Instance != null)
        {
            Error err = NetworkManager.Instance.JoinLobby(ip, NetworkManager.DefaultPort);
            if (err != Error.Ok)
            {
                UpdateStatus("FAILED TO INITIATE NETWORK PEER", ColorTextAccent);
            }
        }
    }

    private void OnQuitPressed()
    {
        CancelMatchmaking();
        GetTree().Quit();
    }

    // ------------------------------------------------------------------ //
    //  Network Event callbacks
    // ------------------------------------------------------------------ //
    private void OnConnectionSuccess()
    {
        CancelMatchmaking();
        UpdateStatus("CONNECTED! SYNCING STATE...", ColorTextSuccess);
        
        // Add subtle screen flash before transition
        var tween = CreateTween();
        tween.TweenInterval(0.2f);
        tween.Finished += LoadGameScene;
    }

    private void OnConnectionFailed()
    {
        CancelMatchmaking();
        UpdateStatus("CONNECTION ATTEMPT FAILED!", ColorTextAccent);
    }

    private void OnServerDisconnected()
    {
        CancelMatchmaking();
        UpdateStatus("DISCONNECTED FROM HOST SERVER", ColorTextAccent);
    }

    private void SavePlayerName()
    {
        string name = _nameInput != null ? _nameInput.Text.Trim() : "";
        if (string.IsNullOrEmpty(name))
        {
            name = "Player_" + new Random().Next(1000, 9999);
        }

        // Store name locally in project Config to persist across scenes
        var cfg = new ConfigFile();
        cfg.Load("user://settings.cfg");
        cfg.SetValue("player", "name", name);
        cfg.Save("user://settings.cfg");
        
        GD.Print($"MainMenu: Saved active player profile name: {name}");
    }

    private void LoadGameScene()
    {
        GD.Print("MainMenu: Transitioning to game world...");
        GetTree().ChangeSceneToFile("res://Scenes/Main.tscn");
    }
}
