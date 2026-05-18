using Godot;
using System;

public partial class NetworkManager : Node
{
    public static NetworkManager Instance { get; private set; }

    [Signal] public delegate void PlayerConnectedEventHandler(long peerId);
    [Signal] public delegate void PlayerDisconnectedEventHandler(long peerId);
    [Signal] public delegate void ConnectionSuccessEventHandler();
    [Signal] public delegate void ConnectionFailedEventHandler();
    [Signal] public delegate void ServerDisconnectedEventHandler();

    public const int DefaultPort = 7777;
    public const string DefaultAddress = "127.0.0.1";

    private ENetMultiplayerPeer _peer;

    public override void _EnterTree()
    {
        Instance = this;
    }

    public override void _Ready()
    {
        GD.Print("NetworkManager: _Ready starting...");
        
        // Register connection events on the MultiplayerAPI instance
        Multiplayer.PeerConnected += OnPeerConnected;
        Multiplayer.PeerDisconnected += OnPeerDisconnected;
        Multiplayer.ConnectedToServer += OnConnectedToServer;
        Multiplayer.ConnectionFailed += OnConnectionFailed;
        Multiplayer.ServerDisconnected += OnServerDisconnected;
        
        GD.Print("NetworkManager: Registered connection callbacks.");
    }

    public string CurrentRoomCode { get; set; } = "OFFLINE";

    public static string GetLocalIpAddress()
    {
        foreach (string ip in IP.GetLocalAddresses())
        {
            // Filter for IPv4 and exclude loopbacks & internal virtualization IPs if possible
            if (ip.Contains(".") && !ip.StartsWith("127.") && !ip.StartsWith("169.254"))
            {
                return ip;
            }
        }
        return "127.0.0.1";
    }

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
            
            // Encode uint to Base36
            const string chars = "0123456789ABCDEFGHIJKLMNOPQRSTUVWXYZ";
            string code = "";
            uint temp = ipInt;
            while (temp > 0)
            {
                code = chars[(int)(temp % 36)] + code;
                temp /= 36;
            }
            
            return code.PadLeft(6, '0');
        }
        catch
        {
            return "ERROR";
        }
    }

    public static string RoomCodeToIp(string code)
    {
        try
        {
            code = code.ToUpper().Trim();
            const string chars = "0123456789ABCDEFGHIJKLMNOPQRSTUVWXYZ";
            uint ipInt = 0;
            foreach (char c in code)
            {
                int index = chars.IndexOf(c);
                if (index < 0) return "";
                ipInt = (ipInt * 36) + (uint)index;
            }
            
            byte d = (byte)(ipInt & 0xFF);
            byte cVal = (byte)((ipInt >> 8) & 0xFF);
            byte b = (byte)((ipInt >> 16) & 0xFF);
            byte a = (byte)((ipInt >> 24) & 0xFF);
            
            return $"{a}.{b}.{cVal}.{d}";
        }
        catch
        {
            return "";
        }
    }

    public Error CreateHost(int port = DefaultPort)
    {
        GD.Print($"NetworkManager: Creating host on port {port}...");
        _peer = new ENetMultiplayerPeer();
        
        // Limit to 4 players as defined in the game design specifications (§13.1)
        Error err = _peer.CreateServer(port, 4); 
        if (err != Error.Ok)
        {
            GD.PrintErr($"NetworkManager: Failed to create host server! Error: {err}");
            return err;
        }

        Multiplayer.MultiplayerPeer = _peer;
        
        // Generate current room code
        string localIp = GetLocalIpAddress();
        CurrentRoomCode = IpToRoomCode(localIp);
        GD.Print($"NetworkManager: Host server successfully started. Room Code: {CurrentRoomCode} (IP: {localIp})");
        return Error.Ok;
    }

    public Error JoinLobby(string address = DefaultAddress, int port = DefaultPort)
    {
        GD.Print($"NetworkManager: Joining lobby at {address}:{port}...");
        _peer = new ENetMultiplayerPeer();
        Error err = _peer.CreateClient(address, port);
        if (err != Error.Ok)
        {
            GD.PrintErr($"NetworkManager: Failed to create client! Error: {err}");
            return err;
        }

        Multiplayer.MultiplayerPeer = _peer;
        
        // Set room code
        CurrentRoomCode = IpToRoomCode(address);
        GD.Print($"NetworkManager: Client successfully initiated connection. Room Code: {CurrentRoomCode} (IP: {address})");
        return Error.Ok;
    }

    public void Disconnect()
    {
        GD.Print("NetworkManager: Disconnecting...");
        if (_peer != null)
        {
            _peer.Close();
            _peer = null;
        }
        Multiplayer.MultiplayerPeer = null;
    }

    private void OnPeerConnected(long id)
    {
        GD.Print($"NetworkManager: Peer connected with ID: {id}");
        EmitSignal(SignalName.PlayerConnected, id);
    }

    private void OnPeerDisconnected(long id)
    {
        GD.Print($"NetworkManager: Peer disconnected with ID: {id}");
        EmitSignal(SignalName.PlayerDisconnected, id);
    }

    private void OnConnectedToServer()
    {
        GD.Print("NetworkManager: Successfully connected to host server.");
        EmitSignal(SignalName.ConnectionSuccess);
    }

    private void OnConnectionFailed()
    {
        GD.PrintErr("NetworkManager: Connection to host server failed!");
        EmitSignal(SignalName.ConnectionFailed);
    }

    private void OnServerDisconnected()
    {
        GD.Print("NetworkManager: Host server disconnected.");
        EmitSignal(SignalName.ServerDisconnected);
    }
}
