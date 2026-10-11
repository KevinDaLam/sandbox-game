using Unity.Netcode;
using UnityEngine;

namespace Starter
{
    /// <summary>
    /// Simple immediate-mode GUI for starting Host or Client.
    /// Eliminates the need to wire up UI Canvas components while prototyping.
    /// </summary>
    public class NetworkHudUI : MonoBehaviour
    {
        [SerializeField] private string _steamIdInput = "";
        [SerializeField] private string _joinCodeInput = "";
        [SerializeField] private string _ipAddress = "127.0.0.1";
        [SerializeField] private ushort _port = 7777;
        private const int DiscoveryPort = 47777;
        private const string DiscoveryHeader = "SANDBOX_GAME_LAN_HOST";

        private string _statusMessage = "Idle. Ready to connect.";
        private string _steamErrorMessage = "";
        private string _hostRoomCode = "";
        private bool _isConnecting = false;

        // LAN Auto-Discovery
        private System.Net.Sockets.UdpClient _discoveryClient;
        private System.Threading.CancellationTokenSource _discoveryCts;
        private readonly System.Collections.Generic.Dictionary<string, string> _discoveredRooms = new System.Collections.Generic.Dictionary<string, string>(); // code -> ip
        private float _lastBroadcastTime = 0f;

        private void OnEnable()
        {
            if (NetworkManager.Singleton != null)
            {
                NetworkManager.Singleton.OnClientConnectedCallback += HandleClientConnected;
                NetworkManager.Singleton.OnClientDisconnectCallback += HandleClientDisconnected;
            }
        }

        private void OnDisable()
        {
            StopDiscovery();
            if (NetworkManager.Singleton != null)
            {
                NetworkManager.Singleton.OnClientConnectedCallback -= HandleClientConnected;
                NetworkManager.Singleton.OnClientDisconnectCallback -= HandleClientDisconnected;
            }
        }

        private void OnDestroy()
        {
            StopDiscovery();
            try
            {
                if (Steamworks.SteamClient.IsValid)
                {
                    Steamworks.SteamClient.Shutdown();
                }
            }
            catch { }
        }

        private void Start()
        {
            InitializeSteam();

            if (NetworkManager.Singleton != null)
            {
                NetworkManager.Singleton.OnClientConnectedCallback -= HandleClientConnected;
                NetworkManager.Singleton.OnClientConnectedCallback += HandleClientConnected;
                NetworkManager.Singleton.OnClientDisconnectCallback -= HandleClientDisconnected;
                NetworkManager.Singleton.OnClientDisconnectCallback += HandleClientDisconnected;
            }

            // Immediately start scanning for any hosted games on the LAN
            StartListeningForHosts();
        }

        private void InitializeSteam()
        {
            try
            {
                if (!Steamworks.SteamClient.IsValid)
                {
                    Steamworks.SteamClient.Init(480, false);
                    _steamErrorMessage = "";
                    Debug.Log($"[NetworkHudUI] Steam initialized as {Steamworks.SteamClient.Name} (ID: {Steamworks.SteamClient.SteamId})");
                }
            }
            catch (System.Exception ex)
            {
                _steamErrorMessage = ex.GetType().Name + ": " + ex.Message;
                Debug.LogWarning($"[NetworkHudUI] Steam init check: {ex}");
            }
        }

        private void Update()
        {
            // Process Steam callbacks
            try
            {
                if (Steamworks.SteamClient.IsValid)
                {
                    Steamworks.SteamClient.RunCallbacks();
                }
            }
            catch { }

            // If we are hosting, broadcast beacon every 1 second
            if (NetworkManager.Singleton != null && NetworkManager.Singleton.IsHost)
            {
                if (Time.unscaledTime - _lastBroadcastTime > 1.0f)
                {
                    _lastBroadcastTime = Time.unscaledTime;
                    BroadcastHostBeacon();
                }
            }
        }

        private void HandleClientConnected(ulong clientId)
        {
            _isConnecting = false;
            if (NetworkManager.Singleton.IsHost)
            {
                _statusMessage = $"Player {clientId} joined! Total: {NetworkManager.Singleton.ConnectedClients.Count}";
            }
            else
            {
                _statusMessage = $"Successfully connected to host as Client ID: {clientId}!";
            }
        }

        private void HandleClientDisconnected(ulong clientId)
        {
            _isConnecting = false;
            string reason = NetworkManager.Singleton.DisconnectReason;
            if (string.IsNullOrEmpty(reason))
            {
                reason = "Connection timed out or rejected. Make sure both PCs are on the same Wi-Fi and Host firewall allows UDP port 7777.";
            }
            _statusMessage = $"Disconnected (ID: {clientId}): {reason}";
            Debug.LogWarning($"[NetworkHudUI] Disconnected: {reason}");
            
            // Resume listening when disconnected
            StartListeningForHosts();
        }

        private void OnGUI()
        {
            GUILayout.BeginArea(new Rect(10, 10, 360, 580));

            if (NetworkManager.Singleton == null)
            {
                GUILayout.Label("<color=red><b>NetworkManager not found in scene!</b></color>");
                GUILayout.EndArea();
                return;
            }

            if (!NetworkManager.Singleton.IsClient && !NetworkManager.Singleton.IsServer)
            {
                GUILayout.Label("<b>--- STEAM P2P (SPACEWAR 480) ---</b>");
                bool steamReady = Steamworks.SteamClient.IsValid;
                if (!steamReady)
                {
                    GUILayout.Label("<color=yellow>Steam client not connected.</color>");
                    if (!string.IsNullOrEmpty(_steamErrorMessage))
                    {
                        GUILayout.Label($"<color=red><size=10>{_steamErrorMessage}</size></color>");
                    }
                    if (GUILayout.Button("🔄 Retry Steam Init", GUILayout.Height(24)))
                    {
                        InitializeSteam();
                    }
                }
                else
                {
                    GUILayout.Label($"Steam User: <b><color=cyan>{Steamworks.SteamClient.Name}</color></b>");
                }

                if (GUILayout.Button("🎮 Host Steam Lobby", GUILayout.Height(38)))
                {
                    StartSteamHost();
                }

                GUILayout.Space(4);
                GUILayout.Label("<b>Join by Steam ID / Friend ID:</b>");
                _steamIdInput = GUILayout.TextField(_steamIdInput).Trim();

                if (GUILayout.Button("🚀 Join Steam Host", GUILayout.Height(36)))
                {
                    if (ulong.TryParse(_steamIdInput, out ulong targetSteamId) && targetSteamId > 0)
                    {
                        StartSteamClient(targetSteamId);
                    }
                    else
                    {
                        _statusMessage = "Please enter a valid 17-digit Steam ID64!";
                    }
                }

                GUILayout.Space(10);
                GUILayout.Label("<b>--- LOCAL LAN / DIRECT IP ---</b>");

                if (GUILayout.Button("Start Host (LAN)", GUILayout.Height(34)))
                {
                    StopDiscovery();
                    string localIp = GetLocalIPv4();
                    _hostRoomCode = IpToRoomCode(localIp);
                    ConfigureTransportAddress("127.0.0.1", _port, "0.0.0.0");
                    bool success = NetworkManager.Singleton.StartHost();
                    _statusMessage = success 
                        ? $"LAN Host started!\nYour IP: <b><color=cyan>{localIp}</color></b>\nRoom Code: <b><color=yellow>{_hostRoomCode}</color></b>" 
                        : "Failed to start Host!";
                }

                GUILayout.Space(6);
                GUILayout.Label("<b>Join by LAN IP / Hex Code:</b>");
                _joinCodeInput = GUILayout.TextField(_joinCodeInput).Trim();

                GUI.enabled = !_isConnecting;
                if (GUILayout.Button(_isConnecting ? "Connecting..." : "Join Game (LAN)", GUILayout.Height(34)))
                {
                    string input = _joinCodeInput;
                    if (string.IsNullOrEmpty(input))
                    {
                        // Fallback to local loopback if blank
                        ConnectToIp("127.0.0.1");
                    }
                    else if (System.Net.IPAddress.TryParse(input, out _))
                    {
                        // Direct IP entered (e.g. 192.168.1.175)
                        ConnectToIp(input);
                    }
                    else if (TryRoomCodeToIp(input, out string decodedIp))
                    {
                        // Room Code entered
                        ConnectToIp(decodedIp);
                    }
                    else
                    {
                        _statusMessage = $"Invalid input '{input}'! Enter a valid IPv4 address (e.g. 192.168.1.175) or a Room Code.";
                    }
                }
                GUI.enabled = true;

                // --- LAN AUTO-DISCOVERY SCANNER ---
                GUILayout.Space(6);
                GUILayout.Label("<b>📡 LAN Auto-Discovery:</b>");
                lock (_discoveredRooms)
                {
                    if (_discoveredRooms.Count == 0)
                    {
                        GUILayout.Label("<color=#AAAAAA><i>Scanning Wi-Fi for games...</i></color>");
                    }
                    else
                    {
                        foreach (var kvp in _discoveredRooms)
                        {
                            string roomCode = kvp.Key;
                            string hostIp = kvp.Value;
                            if (GUILayout.Button($"👉 Join [{roomCode}] ({hostIp})", GUILayout.Height(30)))
                            {
                                ConnectToIp(hostIp);
                                break;
                            }
                        }
                    }
                }
            }
            else
            {
                string mode = NetworkManager.Singleton.IsHost ? "Host" : "Client";
                GUILayout.Label($"<b>Running as: {mode}</b>");
                if (NetworkManager.Singleton.IsHost && !string.IsNullOrEmpty(_hostRoomCode))
                {
                    GUILayout.Label($"<b>Your IP: <color=cyan>{GetLocalIPv4()}</color></b>");
                    GUILayout.Label($"<b>Room Code: <color=yellow>{_hostRoomCode}</color></b>");
                }
                GUILayout.Label($"Connected Players: {NetworkManager.Singleton.ConnectedClients.Count}");

                GUILayout.Label("<color=#CCCCCC><i>(Press Esc or Tab to unlock mouse)</i></color>");

                if (GUILayout.Button("Disconnect", GUILayout.Height(35)))
                {
                    _isConnecting = false;
                    _hostRoomCode = "";
                    Cursor.lockState = CursorLockMode.None;
                    Cursor.visible = true;
                    NetworkManager.Singleton.Shutdown();
                    _statusMessage = "Disconnected by user.";
                    StartListeningForHosts();
                }
            }

            GUILayout.Space(8);
            GUILayout.Label("<b>Network Status:</b>");
            GUILayout.TextArea(_statusMessage, GUILayout.Height(70));

            GUILayout.EndArea();
        }

        private void ConnectToIp(string targetIp)
        {
            StopDiscovery();
            ConfigureTransportAddress(targetIp, _port, "0.0.0.0");
            bool success = NetworkManager.Singleton.StartClient();
            if (success)
            {
                _isConnecting = true;
                _statusMessage = $"Connecting to {targetIp}:{_port}... (Waiting for handshake)";
            }
            else
            {
                _isConnecting = false;
                _statusMessage = $"Failed to initiate client connection to {targetIp}:{_port}!";
            }
        }

        private void ConfigureTransportAddress(string ip, ushort port, string listenAddress = "0.0.0.0")
        {
            var transport = NetworkManager.Singleton.GetComponent<Unity.Netcode.Transports.UTP.UnityTransport>();
            if (transport != null)
            {
                transport.SetConnectionData(ip, port, listenAddress);
            }
        }

        // --- LAN BROADCAST & SCANNING ---

        private void BroadcastHostBeacon()
        {
            try
            {
                if (string.IsNullOrEmpty(_hostRoomCode)) return;
                using (var udp = new System.Net.Sockets.UdpClient())
                {
                    udp.EnableBroadcast = true;
                    string message = $"{DiscoveryHeader}:{_hostRoomCode}:{_port}";
                    byte[] data = System.Text.Encoding.UTF8.GetBytes(message);

                    // Broadcast to standard broadcast address
                    udp.Send(data, data.Length, new System.Net.IPEndPoint(System.Net.IPAddress.Broadcast, DiscoveryPort));

                    // Also broadcast on local subnet directly (e.g. 192.168.1.255)
                    string localIp = GetLocalIPv4();
                    int lastDot = localIp.LastIndexOf('.');
                    if (lastDot > 0)
                    {
                        string subnetBroadcast = localIp.Substring(0, lastDot) + ".255";
                        if (System.Net.IPAddress.TryParse(subnetBroadcast, out var subnetAddr))
                        {
                            udp.Send(data, data.Length, new System.Net.IPEndPoint(subnetAddr, DiscoveryPort));
                        }
                    }
                }
            }
            catch { }
        }

        private void StartListeningForHosts()
        {
            StopDiscovery();
            _discoveryCts = new System.Threading.CancellationTokenSource();
            var token = _discoveryCts.Token;

            System.Threading.Tasks.Task.Run(async () =>
            {
                try
                {
                    _discoveryClient = new System.Net.Sockets.UdpClient();
                    _discoveryClient.Client.SetSocketOption(System.Net.Sockets.SocketOptionLevel.Socket, System.Net.Sockets.SocketOptionName.ReuseAddress, true);
                    _discoveryClient.Client.Bind(new System.Net.IPEndPoint(System.Net.IPAddress.Any, DiscoveryPort));

                    while (!token.IsCancellationRequested)
                    {
                        var result = await _discoveryClient.ReceiveAsync();
                        string msg = System.Text.Encoding.UTF8.GetString(result.Buffer);
                        if (msg.StartsWith(DiscoveryHeader))
                        {
                            var parts = msg.Split(':');
                            if (parts.Length >= 2)
                            {
                                string roomCode = parts[1];
                                string senderIp = result.RemoteEndPoint.Address.ToString();

                                lock (_discoveredRooms)
                                {
                                    _discoveredRooms[roomCode] = senderIp;
                                }
                            }
                        }
                    }
                }
                catch { }
            }, token);
        }

        private void StopDiscovery()
        {
            try
            {
                if (_discoveryCts != null)
                {
                    _discoveryCts.Cancel();
                    _discoveryCts.Dispose();
                    _discoveryCts = null;
                }
                if (_discoveryClient != null)
                {
                    _discoveryClient.Close();
                    _discoveryClient.Dispose();
                    _discoveryClient = null;
                }
            }
            catch { }
        }

        private static string GetLocalIPv4()
        {
            try
            {
                var host = System.Net.Dns.GetHostEntry(System.Net.Dns.GetHostName());
                foreach (var ip in host.AddressList)
                {
                    if (ip.AddressFamily == System.Net.Sockets.AddressFamily.InterNetwork)
                    {
                        string str = ip.ToString();
                        if (!str.StartsWith("127.") && !str.StartsWith("169.254."))
                        {
                            return str;
                        }
                    }
                }
            }
            catch { }
            return "127.0.0.1";
        }

        // --- Room Code Hex Converter (Lossless 8-hex format) ---
        private static string IpToRoomCode(string ipAddress)
        {
            if (System.Net.IPAddress.TryParse(ipAddress, out var ip))
            {
                byte[] bytes = ip.GetAddressBytes();
                return $"{bytes[0]:X2}{bytes[1]:X2}{bytes[2]:X2}{bytes[3]:X2}";
            }
            return "LOCAL1";
        }

        private static bool TryRoomCodeToIp(string code, out string ipAddress)
        {
            ipAddress = null;
            if (string.IsNullOrEmpty(code)) return false;

            code = code.Trim().ToUpper();
            if (code == "LOCAL1" || code == "LOCAL")
            {
                ipAddress = "127.0.0.1";
                return true;
            }

            if (code.Length == 8)
            {
                try
                {
                    byte b0 = System.Convert.ToByte(code.Substring(0, 2), 16);
                    byte b1 = System.Convert.ToByte(code.Substring(2, 2), 16);
                    byte b2 = System.Convert.ToByte(code.Substring(4, 2), 16);
                    byte b3 = System.Convert.ToByte(code.Substring(6, 2), 16);
                    ipAddress = $"{b0}.{b1}.{b2}.{b3}";
                    return true;
                }
                catch { }
            }

            return false;
        }

        // --- STEAMWORKS P2P METHODS ---

        private async void StartSteamHost()
        {
            if (!Steamworks.SteamClient.IsValid)
            {
                _statusMessage = "Cannot host on Steam: Steam client is not running or logged in!";
                return;
            }

            try
            {
                StopDiscovery();
                EnsureFacepunchTransport();

                // Create a Steam lobby for up to 4 players
                var lobby = await Steamworks.SteamMatchmaking.CreateLobbyAsync(4);
                if (lobby.HasValue)
                {
                    lobby.Value.SetPublic();
                    lobby.Value.SetJoinable(true);
                    lobby.Value.SetData("name", $"{Steamworks.SteamClient.Name}'s Lobby");

                    bool success = NetworkManager.Singleton.StartHost();
                    if (success)
                    {
                        ulong mySteamId = Steamworks.SteamClient.SteamId.Value;
                        _statusMessage = $"Steam Lobby Created!\nYour Steam ID64:\n<b><color=yellow>{mySteamId}</color></b>\n(Tell friend your Steam ID or invite via Shift+Tab!)";
                    }
                    else
                    {
                        _statusMessage = "Failed to start NetworkManager as Host!";
                    }
                }
                else
                {
                    _statusMessage = "Failed to create Steam Lobby with Valve servers!";
                }
            }
            catch (System.Exception ex)
            {
                _statusMessage = $"Steam Host Exception: {ex.Message}";
                Debug.LogError($"[NetworkHudUI] Steam Host Error: {ex}");
            }
        }

        private void StartSteamClient(ulong hostSteamId)
        {
            if (!Steamworks.SteamClient.IsValid)
            {
                _statusMessage = "Cannot join on Steam: Steam client is not running or logged in!";
                return;
            }

            try
            {
                StopDiscovery();
                var transport = EnsureFacepunchTransport();
                transport.targetSteamId = hostSteamId;

                bool success = NetworkManager.Singleton.StartClient();
                if (success)
                {
                    _isConnecting = true;
                    _statusMessage = $"Connecting to Steam Host {hostSteamId} via Valve Relay...";
                }
                else
                {
                    _statusMessage = $"Failed to start client connection to Steam ID {hostSteamId}!";
                }
            }
            catch (System.Exception ex)
            {
                _statusMessage = $"Steam Client Exception: {ex.Message}";
                Debug.LogError($"[NetworkHudUI] Steam Client Error: {ex}");
            }
        }

        private Netcode.Transports.Facepunch.FacepunchTransport EnsureFacepunchTransport()
        {
            var nm = NetworkManager.Singleton;
            var fp = nm.GetComponent<Netcode.Transports.Facepunch.FacepunchTransport>();
            if (fp == null)
            {
                fp = nm.gameObject.AddComponent<Netcode.Transports.Facepunch.FacepunchTransport>();
            }

            // Assign as active transport on NetworkManager
            nm.NetworkConfig.NetworkTransport = fp;
            return fp;
        }
    }
}
