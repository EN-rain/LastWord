using Godot;
using System;
using System.Linq;
using System.Collections.Generic;

/// <summary>
/// Pre-run lobby for private custom room sessions (host + code-join).
/// Host sees "Start Run" button; joining clients see "Ready".
/// Wires: LobbyPlayersUpdated, ChatMessageReceived, PlayerMicVolumeSynced, MatchStarted.
/// </summary>
public partial class CustomLobby : CanvasLayer
{
    // -- UI Node Paths --
    [Export] public NodePath RoomCodeLabelPath;
    [Export] public NodePath PlayerCountLabelPath;
    [Export] public NodePath PlayerListPath;
    [Export] public NodePath ChatLogPath;
    [Export] public NodePath ChatInputPath;
    [Export] public NodePath SendBtnPath;
    [Export] public NodePath ReadyBtnPath;
    [Export] public NodePath StartRunBtnPath;
    [Export] public NodePath CancelBtnPath;
    [Export] public NodePath OrientationLabelPath;
    [Export] public NodePath ConsentLabelPath;
    [Export] public NodePath PrivacyLabelPath;

    // -- Scene Paths --
    [Export(PropertyHint.File, "*.tscn")] public string MainMenuScenePath = "res://Scenes/MainMenu.tscn";
    [Export(PropertyHint.File, "*.tscn")] public string MainGameScenePath = "res://Scenes/GameScene.tscn";

    // -- Hardcoded Texts --
    [Export] public string NoticeConsentText = "NOTICE: In-game voice chat is required. Speaking transfers the monster's attention to you.";
    [Export] public string NoticePrivacyText = "PRIVACY: Voice is recorded locally this session for gameplay purposes only. Nothing is transmitted externally.";
    [Export] public string OrientationBannerText = "This lobby represents the orientation room. You hear murmurs in the distance.";

    // -- UI references --
    private Label         _labelRoomCode;
    private Label         _labelPlayerCount;
    private ItemList      _playerList;
    private RichTextLabel _chatLog;
    private LineEdit      _chatInput;
    private Button        _btnSend;
    private Button        _btnReady;
    private Button        _btnStartRun;   // host only
    private Button        _btnCancel;
    private Label         _labelOrientation;
    private Label         _labelConsent;
    private Label         _labelPrivacy;

    private bool _localIsReady = false;
    private bool _sendingMicLevel = false;

    public override void _Ready()
    {

        // Resolve nodes via exported paths
        _labelRoomCode    = GetNodeOrNull<Label>(RoomCodeLabelPath);
        _labelPlayerCount = GetNodeOrNull<Label>(PlayerCountLabelPath);
        _playerList       = GetNodeOrNull<ItemList>(PlayerListPath);
        _chatLog          = GetNodeOrNull<RichTextLabel>(ChatLogPath);
        _chatInput        = GetNodeOrNull<LineEdit>(ChatInputPath);
        _btnSend          = GetNodeOrNull<Button>(SendBtnPath);
        _btnReady         = GetNodeOrNull<Button>(ReadyBtnPath);
        _btnStartRun      = GetNodeOrNull<Button>(StartRunBtnPath);
        _btnCancel        = GetNodeOrNull<Button>(CancelBtnPath);
        _labelOrientation = GetNodeOrNull<Label>(OrientationLabelPath);
        _labelConsent     = GetNodeOrNull<Label>(ConsentLabelPath);
        _labelPrivacy     = GetNodeOrNull<Label>(PrivacyLabelPath);

        // Static notices
        if (_labelConsent != null)
            _labelConsent.Text = NoticeConsentText;
        if (_labelPrivacy != null)
            _labelPrivacy.Text = NoticePrivacyText;
        if (_labelOrientation != null)
            _labelOrientation.Text = OrientationBannerText;

        // Host vs Client UI
        bool isHost = Multiplayer.IsServer();
        if (_btnStartRun != null)
        {
            _btnStartRun.Visible = isHost;
        }
        if (_btnReady != null)
        {
            _btnReady.Visible = !isHost;
        }

        // Wire NetworkManager signals
        if (NetworkManager.Instance != null)
        {
            NetworkManager.Instance.LobbyPlayersUpdated    += RefreshPlayerList;
            NetworkManager.Instance.ChatMessageReceived    += OnChatReceived;
            NetworkManager.Instance.PlayerMicVolumeSynced += OnMicVolumeReceived;
            NetworkManager.Instance.MatchStarted           += OnMatchStarted;
            NetworkManager.Instance.RoomCodeUpdated        += OnRoomCodeUpdated;
        }

        if (VoiceManager.Instance != null)
        {
            VoiceManager.Instance.VolumeUpdated += OnLocalMicVolumeUpdated;
            _sendingMicLevel = true;
        }

        // Populate initial state
        RefreshPlayerList();
        UpdateRoomCodeLabel();

        // GD.Print("CustomLobby: Ready. isHost=" + isHost);
    }

    public override void _ExitTree()
    {
        if (NetworkManager.Instance != null)
        {
            NetworkManager.Instance.LobbyPlayersUpdated    -= RefreshPlayerList;
            NetworkManager.Instance.ChatMessageReceived    -= OnChatReceived;
            NetworkManager.Instance.PlayerMicVolumeSynced -= OnMicVolumeReceived;
            NetworkManager.Instance.MatchStarted           -= OnMatchStarted;
            NetworkManager.Instance.RoomCodeUpdated        -= OnRoomCodeUpdated;
        }

        if (_sendingMicLevel && VoiceManager.Instance != null)
            VoiceManager.Instance.VolumeUpdated -= OnLocalMicVolumeUpdated;
    }

    private void OnRoomCodeUpdated(string newCode)
    {
        UpdateRoomCodeLabel();
    }

    // ---------------------------------------------------------------------------
    // UI Refresh
    // ---------------------------------------------------------------------------
    private void UpdateRoomCodeLabel()
    {
        if (_labelRoomCode == null || NetworkManager.Instance == null) return;
        string mode = NetworkManager.Instance.IsMatchmakingSession ? "MATCHMAKING" : NetworkManager.Instance.DirectIpRoomCodeLabel;
        _labelRoomCode.Text = $"ROOM CODE: {NetworkManager.Instance.CurrentRoomCode} ({mode})";
    }

    private void RefreshPlayerList()
    {
        if (NetworkManager.Instance == null) return;

        var players = NetworkManager.Instance.LobbyPlayers;

        if (_labelPlayerCount != null)
            _labelPlayerCount.Text = $"PLAYERS: {players.Count} / {NetworkManager.Instance.RequiredPlayers}";

        if (_playerList != null)
        {
            _playerList.Clear();
            foreach (var p in players)
            {
                string readyTag = p.IsReady ? "[READY]" : "[WAIT]";
                if (p.PeerId == NetworkManager.ServerPeerId) readyTag = "[HOST]";
                
                string micTag   = $"[MIC: {p.MicVolume:F0} dB]";
                _playerList.AddItem($"{p.Name}  {readyTag}  {micTag}");
            }
        }

        // Orientation Mode banner
        bool showOrientation = players.Exists(p => p.Runs < 3);
        if (_labelOrientation != null)
        {
            _labelOrientation.Visible = showOrientation;
            _labelOrientation.Text    = "ORIENTATION MODE ACTIVE: A survivor with fewer than 3 runs is in this lobby. Tutorial phase is locked.";
        }

        // Enable Start Run only when all OTHER players are ready (host only)
        if (_btnStartRun != null && Multiplayer.IsServer())
        {
            long localId = Multiplayer.GetUniqueId();
            bool othersReady = players.Where(p => p.PeerId != localId).All(p => p.IsReady);
            _btnStartRun.Disabled = !othersReady;
        }
    }

    // ---------------------------------------------------------------------------
    // Chat
    // ---------------------------------------------------------------------------
    private void OnSendPressed(string _ = "")
    {
        if (_chatInput == null || NetworkManager.Instance == null) return;
        string msg = _chatInput.Text.Trim();
        if (string.IsNullOrEmpty(msg)) return;

        NetworkManager.Instance.SendChat(msg);
        _chatInput.Text = "";
    }

    private void OnChatReceived(string senderName, string message)
    {
        if (_chatLog == null) return;
        _chatLog.AppendText($"\n[b]{EscapeBbcode(senderName)}:[/b] {EscapeBbcode(message)}");
    }

    // ---------------------------------------------------------------------------
    // Mic volume
    // ---------------------------------------------------------------------------
    private void OnMicVolumeReceived(long peerId, float dbValue)
    {
        RefreshPlayerList();
    }

    private void OnLocalMicVolumeUpdated(float dbValue)
    {
        NetworkManager.Instance?.SendMic(dbValue);
    }

    private static string EscapeBbcode(string value)
    {
        if (string.IsNullOrEmpty(value)) return "";
        return value.Replace("[", "[lb]").Replace("]", "[rb]");
    }

    // ---------------------------------------------------------------------------
    // Ready (clients only)
    // ---------------------------------------------------------------------------
    private void OnReadyPressed()
    {
        if (NetworkManager.Instance == null) return;
        _localIsReady = !_localIsReady;
        NetworkManager.Instance.ToggleReady(_localIsReady);
        if (_btnReady != null)
            _btnReady.Text = _localIsReady ? "UNREADY" : "READY";
    }

    private void OnStartRunPressed()
    {
        if (NetworkManager.Instance == null || !Multiplayer.IsServer()) return;

        NetworkManager.Instance.StartCustomMatch();
    }

    // ---------------------------------------------------------------------------
    // Cancel
    // ---------------------------------------------------------------------------
    private void OnCancelPressed()
    {
        NetworkManager.Instance?.Disconnect();
        GetTree().ChangeSceneToFile(MainMenuScenePath);
    }

    // ---------------------------------------------------------------------------
    // MatchStarted (fired by server-authoritative matchmaking — should not fire
    // in a custom room, but handled defensively)
    // ---------------------------------------------------------------------------
    private void OnMatchStarted()
    {
        GetTree().ChangeSceneToFile(MainGameScenePath);
    }
}
