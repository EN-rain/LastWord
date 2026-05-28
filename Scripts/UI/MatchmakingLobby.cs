using Godot;
using System;
using System.Linq;
using System.Collections.Generic;

/// <summary>
/// Pre-run lobby for server-authoritative random matchmaking sessions.
/// Wires: LobbyPlayersUpdated, ChatMessageReceived, PlayerMicVolumeSynced, MatchStarted.
/// </summary>
public partial class MatchmakingLobby : CanvasLayer
{
    // -- UI Node Paths --
    [Export] public NodePath RoomCodeLabelPath;
    [Export] public NodePath PlayerCountLabelPath;
    [Export] public NodePath PlayerListPath;
    [Export] public NodePath ChatLogPath;
    [Export] public NodePath ChatInputPath;
    [Export] public NodePath SendBtnPath;
    [Export] public NodePath ReadyBtnPath;
    [Export] public NodePath CancelBtnPath;
    [Export] public NodePath OrientationLabelPath;
    [Export] public NodePath ConsentLabelPath;
    [Export] public NodePath PrivacyLabelPath;

    // -- Scene Paths --
    [Export(PropertyHint.File, "*.tscn")] public string MainMenuScenePath;
    [Export(PropertyHint.File, "*.tscn")] public string MainGameScenePath;

    // -- Hardcoded Texts --
    [Export] public string NoticeConsentText = "NOTICE: In-game voice chat is required. Speaking transfers the monster's attention to you.";
    [Export] public string NoticePrivacyText = "PRIVACY: Voice is recorded locally this session for gameplay purposes only. Nothing is transmitted externally.";
    [Export] public string OrientationBannerText = "This lobby represents the orientation room. You hear murmurs in the distance.";

    // -- UI references (resolved in _Ready via NodePaths) --
    private Label        _labelRoomCode;
    private Label        _labelPlayerCount;
    private ItemList     _playerList;
    private RichTextLabel _chatLog;
    private LineEdit     _chatInput;
    private Button       _btnSend;
    private Button       _btnReady;
    private Button       _btnCancel;
    private Label        _labelOrientation;
    private Label        _labelConsent;
    private Label        _labelPrivacy;
    private bool         _sendingMicLevel = false;

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
    }

    public override void _ExitTree()
    {
        // Disconnect signals to avoid stale callbacks
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

    // ---------------------------------------------------------------------------
    // UI Refresh
    // ---------------------------------------------------------------------------
    private void UpdateRoomCodeLabel()
    {
        if (_labelRoomCode == null || NetworkManager.Instance == null) return;
        string mode = NetworkManager.Instance.IsMatchmakingSession ? "MATCHMAKING" : NetworkManager.Instance.DirectIpRoomCodeLabel;
        _labelRoomCode.Text = $"ROOM CODE: {NetworkManager.Instance.CurrentRoomCode} ({mode})";
    }

    private void OnRoomCodeUpdated(string newCode)
    {
        UpdateRoomCodeLabel();
    }

    private void RefreshPlayerList()
    {
        if (NetworkManager.Instance == null) return;

        var players = NetworkManager.Instance.LobbyPlayers;

        // Player count header
        if (_labelPlayerCount != null)
            _labelPlayerCount.Text = $"PLAYERS: {players.Count} / {NetworkManager.Instance.RequiredPlayers}";

        // Rebuild ItemList
        if (_playerList != null)
        {
            _playerList.Clear();
            foreach (var p in players)
            {
                string readyTag = p.IsReady ? "[READY]" : "[WAIT]";
                
                // Host in custom match doesn't need to be 'ready'
                if (!NetworkManager.Instance.IsMatchmakingSession && p.PeerId == NetworkManager.ServerPeerId)
                    readyTag = "[HOST]";
                    
                string micTag   = $"[MIC: {p.MicVolume:F0} dB]";
                _playerList.AddItem($"{p.Name}  {readyTag}  {micTag}");
            }
        }

        // Orientation Mode banner: visible if any player has < 3 runs
        bool showOrientation = players.Exists(p => p.Runs < 3);
        if (_labelOrientation != null)
        {
            _labelOrientation.Visible = showOrientation;
            _labelOrientation.Text    = "ORIENTATION MODE ACTIVE: A survivor with fewer than 3 runs is in this lobby. Tutorial phase is locked.";
        }

        // Host 'Start Match' UI overrides
        if (!NetworkManager.Instance.IsMatchmakingSession && Multiplayer.IsServer())
        {
            if (_btnReady != null)
            {
                long localId = Multiplayer.GetUniqueId();
                bool othersReady = players.Where(p => p.PeerId != localId).All(p => p.IsReady);
                
                _btnReady.Text = "START MATCH";
                // Host can start if there are at least 2 players and everyone else is ready
                _btnReady.Disabled = players.Count < 2 || !othersReady;
            }
        }
        else if (_btnReady != null)
        {
            long localId = Multiplayer.GetUniqueId();
            var local = players.Find(p => p.PeerId == localId);
            bool isReady = local != null && local.IsReady;
            _btnReady.Text = isReady ? "UNREADY" : "READY";
            _btnReady.Disabled = false;
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
    // Mic volume — update player list row live
    // ---------------------------------------------------------------------------
    private void OnMicVolumeReceived(long peerId, float dbValue)
    {
        // Full list rebuild keeps it simple; perf is fine for max 4 players
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
    // Ready toggle
    // ---------------------------------------------------------------------------
    private void OnReadyPressed()
    {
        if (NetworkManager.Instance == null) return;

        long localId = Multiplayer.GetUniqueId();

        // Host in custom match uses this button to start the game
        if (!NetworkManager.Instance.IsMatchmakingSession && Multiplayer.IsServer())
        {
            var players = NetworkManager.Instance.LobbyPlayers;
            bool othersReady = players.Where(p => p.PeerId != localId).All(p => p.IsReady);
            
            if (players.Count >= 2 && othersReady)
            {
                NetworkManager.Instance.StartCustomMatch();
            }
            return;
        }

        // Find local player and toggle
        var local = NetworkManager.Instance.LobbyPlayers.Find(p => p.PeerId == localId);
        bool newReady = local == null || !local.IsReady;

        NetworkManager.Instance.ToggleReady(newReady);

        if (_btnReady != null)
            _btnReady.Text = newReady ? "UNREADY" : "READY";
    }

    // ---------------------------------------------------------------------------
    // Cancel — disconnect and return to MainMenu
    // ---------------------------------------------------------------------------
    private void OnCancelPressed()
    {
        NetworkManager.Instance?.Disconnect();
        GetTree().ChangeSceneToFile(MainMenuScenePath);
    }

    // ---------------------------------------------------------------------------
    // Match started — transition to game
    // ---------------------------------------------------------------------------
    private void OnMatchStarted()
    {
        GetTree().ChangeSceneToFile(MainGameScenePath);
    }
}
