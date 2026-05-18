using Godot;
using System;
using System.Linq;
using System.Collections.Generic;
using System.Net;
using System.Text;

public partial class NetworkManager : Node
{
    public static NetworkManager Instance { get; private set; }

    // ---------------------------------------------------------------------------
    // Player registry entry
    // ---------------------------------------------------------------------------
    public class PlayerInfo
    {
        public long   PeerId        { get; set; }
        public string Name          { get; set; } = "Player";
        public int    Runs          { get; set; } = 0;
        public bool   IsReady       { get; set; } = false;
        public float  MicVolume     { get; set; } = 0.0f;
    }

    public enum ConnectionMode
    {
        Matchmaking,
        PrivateCode,
        DirectDev
    }

    // ---------------------------------------------------------------------------
    // Signals
    // ---------------------------------------------------------------------------
    [Signal] public delegate void PlayerConnectedEventHandler(long peerId);
    [Signal] public delegate void PlayerDisconnectedEventHandler(long peerId);
    [Signal] public delegate void ConnectionSuccessEventHandler();
    [Signal] public delegate void ConnectionFailedEventHandler();
    [Signal] public delegate void ServerDisconnectedEventHandler();
    [Signal] public delegate void MatchmakingStatusUpdatedEventHandler(int currentCount, int requiredCount, bool isCountdownActive);
    [Signal] public delegate void LobbyFormedEventHandler();
    [Signal] public delegate void MatchStartedEventHandler();
    [Signal] public delegate void LobbyPlayersUpdatedEventHandler();
    [Signal] public delegate void ChatMessageReceivedEventHandler(string senderName, string message);
    [Signal] public delegate void PlayerMicVolumeSyncedEventHandler(long peerId, float dbValue);
    [Signal] public delegate void RoomCodeUpdatedEventHandler(string newCode);

    // ---------------------------------------------------------------------------
    // Constants
    // ---------------------------------------------------------------------------
    public const int    DefaultPort    = 7777;
    public const int    DefaultHostPort = 7778;
    public const long   ServerPeerId = 1;
    public const string DevFallbackAddress = "127.0.0.1";
    private const uint ObfuscationKey = 0x5D3E9A7F;

    // ---------------------------------------------------------------------------
    // State
    // ---------------------------------------------------------------------------
    [Export] public string MatchmakerAddress { get; set; } = DevFallbackAddress;
    [Export] public int MatchmakerPort { get; set; } = DefaultPort;
    [Export] public int HostPort { get; set; } = DefaultHostPort;
    [Export] public bool AllowDirectIpRoomCodes { get; set; } = true;
    [Export] public string DirectIpRoomCodeLabel { get; set; } = "DIRECT-IP DEV/LAN CODE";

    public int    RequiredPlayers      { get; set; } = 4;
    public bool   IsLobbyFormed        { get; private set; } = false;
    public bool   IsMatchStarted       { get; private set; } = false;
    public bool   IsOrientationModeActive { get; set; } = false;

    private string _currentRoomCode = "OFFLINE";
    public string CurrentRoomCode
    {
        get => _currentRoomCode;
        set
        {
            if (_currentRoomCode != value)
            {
                _currentRoomCode = value;
                EmitSignal(SignalName.RoomCodeUpdated, _currentRoomCode);
            }
        }
    }

    public string PlayerName           { get; set; } = "Player";
    public int    PlayerRuns           { get; set; } = 0;

    public bool IsMatchmakingSession   { get; private set; } = false;
    public List<PlayerInfo> LobbyPlayers { get; } = new();

    private bool         _isCountdownActive = false;
    private List<long>   _matchmakingQueue  = new();
    private ENetMultiplayerPeer _peer;

    private static readonly System.Net.Http.HttpClient HttpClientInstance = new();

    // ---------------------------------------------------------------------------
    // Lifecycle
    // ---------------------------------------------------------------------------
    public override void _EnterTree()
    {
        Instance = this;
    }

    public override void _Ready()
    {
        LoadMatchmakerConfig();

        Multiplayer.PeerConnected      += OnPeerConnected;
        Multiplayer.PeerDisconnected   += OnPeerDisconnected;
        Multiplayer.ConnectedToServer  += OnConnectedToServer;
        Multiplayer.ConnectionFailed   += OnConnectionFailed;
        Multiplayer.ServerDisconnected += OnServerDisconnected;

        // Auto-host when running as a dedicated/headless server
        bool isDedicatedServer = OS.HasFeature("dedicated_server") || DisplayServer.GetName() == "headless";
        if (isDedicatedServer)
        {
            CreateHost(MatchmakerPort, true);
        }
    }

    private void LoadMatchmakerConfig()
    {
        var cfg = new ConfigFile();
        if (cfg.Load("user://settings.cfg") == Error.Ok)
        {
            MatchmakerAddress = (string)cfg.GetValue("network", "matchmaker_address", DevFallbackAddress);
            MatchmakerPort = (int)cfg.GetValue("network", "matchmaker_port", DefaultPort);
            HostPort = (int)cfg.GetValue("network", "host_port", DefaultHostPort);
            AllowDirectIpRoomCodes = (bool)cfg.GetValue("network", "allow_direct_ip_room_codes", true);
        }
        else
        {
            MatchmakerAddress = DevFallbackAddress;
            MatchmakerPort = DefaultPort;
            HostPort = DefaultHostPort;
            AllowDirectIpRoomCodes = true;
        }

        // Check command line arguments for override
        string[] args = OS.GetCmdlineArgs();
        foreach (string arg in args)
        {
            if (arg == "--env=staging")
            {
                MatchmakerAddress = (string)ProjectSettings.GetSetting("network/config/staging_matchmaker_address", "staging.lastwordgame.com");
            }
            else if (arg.StartsWith("--matchmaker-address="))
            {
                MatchmakerAddress = arg.Split('=')[1];
            }
            else if (arg.StartsWith("--matchmaker-port="))
            {
                if (int.TryParse(arg.Split('=')[1], out int portOverride))
                {
                    MatchmakerPort = portOverride;
                }
            }
            else if (arg.StartsWith("--host-port="))
            {
                if (int.TryParse(arg.Split('=')[1], out int portOverride))
                {
                    HostPort = portOverride;
                }
            }
            else if (arg == "--disable-direct-ip-room-codes")
            {
                AllowDirectIpRoomCodes = false;
            }
        }
    }

    private async void FetchAndSetPublicRoomCode()
    {
        // GD.Print("NetworkManager: Asynchronously fetching public WAN IP address...");
        string publicIp = await System.Threading.Tasks.Task.Run(async () => await GetPublicIpAddressAsync());
        if (!string.IsNullOrEmpty(publicIp))
        {
            CurrentRoomCode = IpToRoomCode(publicIp);
        }
    }

    public static async System.Threading.Tasks.Task<string> GetPublicIpAddressAsync()
    {
        try
        {
            using var cts = new System.Threading.CancellationTokenSource(TimeSpan.FromSeconds(3));
            var response = await HttpClientInstance.GetAsync("https://api.ipify.org", cts.Token);
            if (response.IsSuccessStatusCode)
            {
                string ip = (await response.Content.ReadAsStringAsync()).Trim();
                if (System.Net.IPAddress.TryParse(ip, out _))
                {
                    return ip;
                }
            }
        }
        catch (Exception)
        {
        }
        return null;
    }

    // ---------------------------------------------------------------------------
    // Connection management
    // ---------------------------------------------------------------------------
    public Error CreateHost(int port = DefaultHostPort, bool isMatchmaking = false)
    {
        GD.Print($"NetworkManager: Creating host on port {port}...");
        _peer = new ENetMultiplayerPeer();

        Error err = _peer.CreateServer(port, 4);
        if (err != Error.Ok)
        {
            return err;
        }

        Multiplayer.MultiplayerPeer = _peer;
        IsMatchmakingSession        = isMatchmaking;

        string localIp = GetLocalIpAddress();
        CurrentRoomCode = isMatchmaking ? "MATCHMAKING" : IpToRoomCode(localIp);

        if (!isMatchmaking)
        {
            FetchAndSetPublicRoomCode();
        }

        LobbyPlayers.Clear();
        LobbyPlayers.Add(new PlayerInfo
        {
            PeerId        = 1,
            Name          = PlayerName,
            Runs          = PlayerRuns,
            IsReady       = false
        });
        EmitSignal(SignalName.LobbyPlayersUpdated);

        return Error.Ok;
    }

    public Error JoinLobby(string address, int port, ConnectionMode mode)
    {
        _peer = new ENetMultiplayerPeer();

        Error err = _peer.CreateClient(address, port);
        if (err != Error.Ok)
        {
            return err;
        }

        Multiplayer.MultiplayerPeer = _peer;
        IsMatchmakingSession        = mode == ConnectionMode.Matchmaking;

        if (mode == ConnectionMode.Matchmaking)
        {
            CurrentRoomCode = "MATCHMAKING";
        }
        else
        {
            CurrentRoomCode = IpToRoomCode(address);
        }

        return Error.Ok;
    }

    public void Disconnect()
    {
        if (_peer != null)
        {
            _peer.Close();
            _peer = null;
        }
        Multiplayer.MultiplayerPeer = null;
        IsLobbyFormed               = false;
        IsMatchStarted              = false;
        _isCountdownActive          = false;
        CurrentRoomCode             = "";
        _matchmakingQueue.Clear();
        LobbyPlayers.Clear();
        EmitSignal(SignalName.LobbyPlayersUpdated);
    }

    // ---------------------------------------------------------------------------
    // Matchmaking queue logic
    // ---------------------------------------------------------------------------
    private void StartLobbyCountdown()
    {
        if (_isCountdownActive) return;
        _isCountdownActive = true;
        Rpc(nameof(SyncMatchmakingStatus), _matchmakingQueue.Count, RequiredPlayers, true);

        var timer = GetTree().CreateTimer(10.0f);
        timer.Timeout += () =>
        {
            if (_isCountdownActive && _matchmakingQueue.Count >= 2 && !IsLobbyFormed)
            {
                _isCountdownActive = false;
                FormLobby();
            }
        };
    }

    private void CancelLobbyCountdown()
    {
        if (!_isCountdownActive) return;
        _isCountdownActive = false;
        Rpc(nameof(SyncMatchmakingStatus), _matchmakingQueue.Count, RequiredPlayers, false);
    }

    private void FormLobby()
    {
        IsLobbyFormed = true;
        string code = GenerateRandomRoomCode();
        Rpc(nameof(LobbyFormedRPC), code);
    }

    [Rpc(MultiplayerApi.RpcMode.Authority, CallLocal = true)]
    private void LobbyFormedRPC(string roomCode)
    {
        IsLobbyFormed = true;
        CurrentRoomCode = roomCode;
        EmitSignal(SignalName.LobbyFormed);
    }

    // ---------------------------------------------------------------------------
    // Public Wrappers for Client RPCs
    // ---------------------------------------------------------------------------
    public void SendChat(string message)
    {
        if (Multiplayer.IsServer()) SubmitChatMessageRPC(message);
        else RpcId(ServerPeerId, nameof(SubmitChatMessageRPC), message);
    }

    public void ToggleReady(bool isReady)
    {
        if (Multiplayer.IsServer()) UpdatePlayerReadyRPC(isReady);
        else RpcId(ServerPeerId, nameof(UpdatePlayerReadyRPC), isReady);
    }

    public void SendMic(float dbValue)
    {
        if (Multiplayer.IsServer()) ServerRelayMicVolume(dbValue);
        else RpcId(ServerPeerId, nameof(ServerRelayMicVolume), dbValue);
    }

    public void StartCustomMatch()
    {
        if (!Multiplayer.IsServer() || IsMatchmakingSession) return;
        bool orientationActive = LobbyPlayers.Exists(p => p.Runs < 3);
        IsMatchStarted = true;
        Rpc(nameof(StartMatch), orientationActive);
    }

    // ---------------------------------------------------------------------------
    // Internal RPC Implementations
    // ---------------------------------------------------------------------------
    [Rpc(MultiplayerApi.RpcMode.AnyPeer, CallLocal = false)]
    private void RegisterPlayerOnServerRPC(string name, int runs)
    {
        if (!Multiplayer.IsServer()) return;
        long senderId = Multiplayer.GetRemoteSenderId();
        if (senderId == 0) senderId = 1;

        LobbyPlayers.RemoveAll(p => p.PeerId == senderId);
        LobbyPlayers.Add(new PlayerInfo { PeerId = senderId, Name = SanitizePlayerText(name), Runs = runs });
        Rpc(nameof(SyncLobbyPlayers), SerializePlayers());
    }

    [Rpc(MultiplayerApi.RpcMode.AnyPeer, CallLocal = false)]
    private void UpdatePlayerReadyRPC(bool isReady)
    {
        if (!Multiplayer.IsServer()) return;
        long senderId = Multiplayer.GetRemoteSenderId();
        if (senderId == 0) senderId = 1;

        var player = LobbyPlayers.Find(p => p.PeerId == senderId);
        if (player == null) return;

        player.IsReady = isReady;
        Rpc(nameof(SyncLobbyPlayers), SerializePlayers());

        // Matchmaking auto-starts when everyone is ready
        if (IsMatchmakingSession && IsLobbyFormed && !IsMatchStarted)
        {
            if (LobbyPlayers.Count >= 2 && LobbyPlayers.All(p => p.IsReady))
            {
                IsMatchStarted = true;
                bool orientationActive = LobbyPlayers.Exists(p => p.Runs < 3);
                Rpc(nameof(StartMatch), orientationActive);
            }
        }
    }

    [Rpc(MultiplayerApi.RpcMode.AnyPeer, CallLocal = false)]
    private void SubmitChatMessageRPC(string message)
    {
        if (!Multiplayer.IsServer()) return;
        long senderId = Multiplayer.GetRemoteSenderId();
        if (senderId == 0) senderId = 1;

        var player = LobbyPlayers.Find(p => p.PeerId == senderId);
        string senderName = player != null ? player.Name : $"Peer {senderId}";

        Rpc(nameof(ReceiveChatMessage), senderName, SanitizePlayerText(message));
    }

    [Rpc(MultiplayerApi.RpcMode.Authority, CallLocal = true)]
    private void ReceiveChatMessage(string senderName, string message)
    {
        EmitSignal(SignalName.ChatMessageReceived, senderName, message);
    }

    [Rpc(MultiplayerApi.RpcMode.AnyPeer, CallLocal = false)]
    private void ServerRelayMicVolume(float dbValue)
    {
        if (!Multiplayer.IsServer()) return;
        long senderId = Multiplayer.GetRemoteSenderId();
        if (senderId == 0) senderId = 1;
        Rpc(nameof(ReceiveMicVolume), senderId, dbValue);
    }

    [Rpc(MultiplayerApi.RpcMode.Authority, CallLocal = true)]
    private void ReceiveMicVolume(long peerId, float dbValue)
    {
        var player = LobbyPlayers.Find(p => p.PeerId == peerId);
        if (player != null) player.MicVolume = dbValue;
        EmitSignal(SignalName.PlayerMicVolumeSynced, peerId, dbValue);
    }

    [Rpc(MultiplayerApi.RpcMode.Authority, CallLocal = true)]
    private void SyncMatchmakingStatus(int currentCount, int requiredCount, bool isCountdownActive)
    {
        EmitSignal(SignalName.MatchmakingStatusUpdated, currentCount, requiredCount, isCountdownActive);
    }

    [Rpc(MultiplayerApi.RpcMode.Authority, CallLocal = true)]
    private void SyncLobbyPlayers(string serializedData)
    {
        DeserializePlayers(serializedData);
    }

    [Rpc(MultiplayerApi.RpcMode.Authority, CallLocal = true)]
    private void StartMatch(bool isOrientationMode)
    {
        IsOrientationModeActive = isOrientationMode;
        EmitSignal(SignalName.MatchStarted);

        bool isDedicated = OS.HasFeature("dedicated_server") || DisplayServer.GetName() == "headless";
        if (isDedicated && Multiplayer.IsServer())
        {
            GetTree().ChangeSceneToFile("res://Scenes/GameScene.tscn");
        }
    }

    // ---------------------------------------------------------------------------
    // Connection callbacks
    // ---------------------------------------------------------------------------
    private void OnPeerConnected(long id)
    {
        EmitSignal(SignalName.PlayerConnected, id);
        if (!Multiplayer.IsServer() || !IsMatchmakingSession) return;

        if (IsLobbyFormed || IsMatchStarted)
        {
            _peer.DisconnectPeer((int)id);
            return;
        }

        if (!_matchmakingQueue.Contains(id))
        {
            _matchmakingQueue.Add(id);
            if (!LobbyPlayers.Exists(p => p.PeerId == id))
            {
                LobbyPlayers.Add(new PlayerInfo { PeerId = id, Name = $"Peer {id}" });
                Rpc(nameof(SyncLobbyPlayers), SerializePlayers());
            }

            Rpc(nameof(SyncMatchmakingStatus), _matchmakingQueue.Count, RequiredPlayers, _isCountdownActive);

            if (_matchmakingQueue.Count == RequiredPlayers)
            {
                _isCountdownActive = false;
                FormLobby();
            }
            else if (_matchmakingQueue.Count >= 2)
            {
                StartLobbyCountdown();
            }
        }
    }

    private void OnPeerDisconnected(long id)
    {
        EmitSignal(SignalName.PlayerDisconnected, id);
        if (!Multiplayer.IsServer()) return;

        LobbyPlayers.RemoveAll(p => p.PeerId == id);
        Rpc(nameof(SyncLobbyPlayers), SerializePlayers());

        if (IsMatchmakingSession && _matchmakingQueue.Contains(id))
        {
            _matchmakingQueue.Remove(id);
            if (!IsLobbyFormed)
            {
                if (_matchmakingQueue.Count < 2) CancelLobbyCountdown();
                else Rpc(nameof(SyncMatchmakingStatus), _matchmakingQueue.Count, RequiredPlayers, _isCountdownActive);
            }
        }
    }

    private void OnConnectedToServer()
    {
        EmitSignal(SignalName.ConnectionSuccess);
        RpcId(ServerPeerId, nameof(RegisterPlayerOnServerRPC), PlayerName, PlayerRuns);
    }

    private void OnConnectionFailed()
    {
        EmitSignal(SignalName.ConnectionFailed);
    }

    private void OnServerDisconnected()
    {
        EmitSignal(SignalName.ServerDisconnected);
    }

    // ---------------------------------------------------------------------------
    // Serialization / Utilities
    // ---------------------------------------------------------------------------
    private string SerializePlayers()
    {
        var entries = new List<string>();
        foreach (var p in LobbyPlayers)
            entries.Add($"{p.PeerId}:{EncodeField(p.Name)}:{p.Runs}:{p.IsReady}");
        return string.Join("|", entries);
    }

    private void DeserializePlayers(string data)
    {
        LobbyPlayers.Clear();
        if (string.IsNullOrEmpty(data)) return;
        foreach (var part in data.Split('|'))
        {
            if (string.IsNullOrEmpty(part)) continue;
            var f = part.Split(':');
            if (f.Length < 4) continue;
            long.TryParse(f[0], out long peerId);
            int.TryParse(f[2],  out int  runs);
            bool.TryParse(f[3], out bool isReady);
            LobbyPlayers.Add(new PlayerInfo { PeerId = peerId, Name = DecodeField(f[1]), Runs = runs, IsReady = isReady });
        }
        EmitSignal(SignalName.LobbyPlayersUpdated);
    }

    private static string SanitizePlayerText(string value)
    {
        if (string.IsNullOrEmpty(value)) return "";
        value = value.Replace("\r", " ").Replace("\n", " ").Trim();
        return value.Length > 160 ? value[..160] : value;
    }

    private static string EncodeField(string value)
    {
        return Convert.ToBase64String(Encoding.UTF8.GetBytes(SanitizePlayerText(value)));
    }

    private static string DecodeField(string value)
    {
        try
        {
            return Encoding.UTF8.GetString(Convert.FromBase64String(value));
        }
        catch
        {
            return SanitizePlayerText(value);
        }
    }

    public static string GetLocalIpAddress()
    {
        foreach (string ip in IP.GetLocalAddresses())
        {
            if (ip.Contains(".") && !ip.StartsWith("127.") && !ip.StartsWith("169.254"))
                return ip;
        }
        return "127.0.0.1";
    }

    public bool TryRoomCodeToIp(string code, out string ipAddress, out string error)
    {
        ipAddress = "";
        error = "";

        if (!AllowDirectIpRoomCodes)
        {
            error = "Direct-IP room codes are disabled until Steam Lobby/Relay is integrated.";
            return false;
        }

        ipAddress = RoomCodeToIp(code);
        if (!IsConnectableIpv4(ipAddress))
        {
            error = "Invalid or unsupported room code.";
            ipAddress = "";
            return false;
        }

        return true;
    }

    public static bool IsConnectableIpv4(string ipAddress)
    {
        if (string.IsNullOrWhiteSpace(ipAddress)) return false;
        if (!IPAddress.TryParse(ipAddress, out IPAddress parsed)) return false;
        if (parsed.AddressFamily != System.Net.Sockets.AddressFamily.InterNetwork) return false;

        byte[] bytes = parsed.GetAddressBytes();
        if (bytes.SequenceEqual(new byte[] { 0, 0, 0, 0 })) return false;
        if (bytes.SequenceEqual(new byte[] { 255, 255, 255, 255 })) return false;
        if (bytes[0] >= 224 && bytes[0] <= 239) return false; // multicast
        if (bytes[0] == 169 && bytes[1] == 254) return false; // link-local

        return true;
    }

    // TODO: Replace with Steam Lobby ID when Steamworks.NET is integrated.
    // Currently using opaque XOR-obfuscated IP addresses for LAN/dev testing.
    public static string IpToRoomCode(string ipAddress)
    {
        try
        {
            var parts = ipAddress.Split('.');
            if (parts.Length != 4) return "INVALID";
            uint ipInt = 0;
            for (int i = 0; i < 4; i++)
            {
                if (!byte.TryParse(parts[i], out byte b)) return "INVALID";
                ipInt = (ipInt << 8) | b;
            }

            // Apply bitwise XOR obfuscation to mask the IP address
            ipInt ^= ObfuscationKey;

            const string chars = "0123456789ABCDEFGHIJKLMNOPQRSTUVWXYZ";
            string code = "";
            while (ipInt > 0)
            {
                code = chars[(int)(ipInt % 36)] + code;
                ipInt /= 36;
            }
            return code.PadLeft(6, '0');
        }
        catch { return "ERROR"; }
    }

    // TODO: Replace with Steam Lobby ID when Steamworks.NET is integrated.
    public static string RoomCodeToIp(string code)
    {
        try
        {
            code = code.ToUpper().Trim();
            if (code.Length < 6 || code.Length > 7) return "";

            const string chars = "0123456789ABCDEFGHIJKLMNOPQRSTUVWXYZ";
            uint ipInt = 0;
            checked
            {
                foreach (char c in code)
                {
                    int index = chars.IndexOf(c);
                    if (index < 0) return "";
                    ipInt = (ipInt * 36) + (uint)index;
                }
            }

            // Reverse the bitwise XOR obfuscation to retrieve the real IP
            ipInt ^= ObfuscationKey;

            byte d = (byte)(ipInt & 0xFF), cVal = (byte)((ipInt >> 8) & 0xFF);
            byte b = (byte)((ipInt >> 16) & 0xFF), a = (byte)((ipInt >> 24) & 0xFF);
            return $"{a}.{b}.{cVal}.{d}";
        }
        catch { return ""; }
    }

    private string GenerateRandomRoomCode()
    {
        const string chars = "ABCDEFGHIJKLMNOPQRSTUVWXYZ0123456789";
        var rng = new Random();
        return new string(Enumerable.Repeat(chars, 6).Select(s => s[rng.Next(s.Length)]).ToArray());
    }
}
