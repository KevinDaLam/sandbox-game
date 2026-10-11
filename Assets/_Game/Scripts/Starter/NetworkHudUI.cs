using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Mirror;
using Mirror.FizzySteam;
using kcp2k;
using Steamworks;
using UnityEngine;

namespace Starter
{
    /// <summary>
    /// Dual Steam P2P (FizzySteamworks + Steamworks.NET) and LAN (KcpTransport) Network HUD.
    /// Matches the exact networking stack of 'Gamble With Your Friends'.
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
        private bool _isSteamInitialized = false;

        // Steam Callbacks & CallResults
        private CallResult<LobbyCreated_t> _lobbyCreatedCallResult;
        private Callback<GameLobbyJoinRequested_t> _lobbyJoinRequestedCallback;
        private CSteamID _currentLobbyId = CSteamID.Nil;

        // LAN Auto-Discovery
        private UdpClient _discoveryClient;
        private CancellationTokenSource _discoveryCts;
        private readonly Dictionary<string, string> _discoveredRooms = new Dictionary<string, string>(); // code -> ip
        private float _lastBroadcastTime = 0f;

        // Cached Transports & Camera
        private FizzySteamworks _fizzyTransport;
        private KcpTransport _kcpTransport;
        private Camera _menuCamera;

        private void Awake()
        {
            EnsureTransports();
        }

        private void OnEnable()
        {
            NetworkClient.OnConnectedEvent += HandleClientConnected;
            NetworkClient.OnDisconnectedEvent += HandleClientDisconnected;
            NetworkClient.OnErrorEvent += HandleClientError;
        }

        private void OnDisable()
        {
            NetworkClient.OnConnectedEvent -= HandleClientConnected;
            NetworkClient.OnDisconnectedEvent -= HandleClientDisconnected;
            NetworkClient.OnErrorEvent -= HandleClientError;
        }

        private void Start()
        {
            EnsureMenuCamera();
            InitializeSteam();
            StartListeningForHosts();
        }

        private void EnsureMenuCamera()
        {
            if (Camera.main == null)
            {
                var camGo = new GameObject("MenuCamera");
                _menuCamera = camGo.AddComponent<Camera>();
                camGo.tag = "MainCamera";
                camGo.transform.position = new Vector3(0, 2.5f, -8f);
                camGo.transform.rotation = Quaternion.Euler(12f, 0f, 0f);
                camGo.AddComponent<AudioListener>();
            }
            else
            {
                _menuCamera = Camera.main;
            }
        }

        private void HandleClientConnected()
        {
            _isConnecting = false;
            _statusMessage = NetworkServer.active ? "Host started successfully!" : "Connected to host!";
            if (_menuCamera != null) _menuCamera.gameObject.SetActive(false);
        }

        private void HandleClientDisconnected()
        {
            _isConnecting = false;
            _statusMessage = "Disconnected from host.";
            if (_menuCamera != null) _menuCamera.gameObject.SetActive(true);
            StartListeningForHosts();
        }

        private void HandleClientError(TransportError error, string reason)
        {
            _isConnecting = false;
            _statusMessage = $"Transport Error ({error}): {reason}";
            if (_menuCamera != null) _menuCamera.gameObject.SetActive(true);
        }

        private void EnsureTransports()
        {
            if (NetworkManager.singleton == null) return;

            _fizzyTransport = NetworkManager.singleton.GetComponent<FizzySteamworks>();
            if (_fizzyTransport == null)
            {
                _fizzyTransport = NetworkManager.singleton.gameObject.AddComponent<FizzySteamworks>();
            }

            _kcpTransport = NetworkManager.singleton.GetComponent<KcpTransport>();
            if (_kcpTransport == null)
            {
                _kcpTransport = NetworkManager.singleton.gameObject.AddComponent<KcpTransport>();
            }

            // Default transport on startup
            if (NetworkManager.singleton.transport == null)
            {
                NetworkManager.singleton.transport = _fizzyTransport;
                Transport.active = _fizzyTransport;
            }
        }

        private void InitializeSteam()
        {
            try
            {
                if (!SteamAPI.IsSteamRunning())
                {
                    _steamErrorMessage = "Steam client is not running. Please launch Steam.";
                    _isSteamInitialized = false;
                    return;
                }

                if (SteamAPI.Init())
                {
                    _isSteamInitialized = true;
                    _steamErrorMessage = "";
                    string persona = SteamFriends.GetPersonaName();
                    ulong steamId = SteamUser.GetSteamID().m_SteamID;
                    Debug.Log($"[NetworkHudUI] Steamworks.NET initialized successfully! User: {persona} ({steamId})");

                    // Register Steam Lobby Friend Invite callback
                    _lobbyCreatedCallResult = CallResult<LobbyCreated_t>.Create(OnLobbyCreated);
                    _lobbyJoinRequestedCallback = Callback<GameLobbyJoinRequested_t>.Create(OnGameLobbyJoinRequested);
                }
                else
                {
                    _isSteamInitialized = false;
                    _steamErrorMessage = "SteamAPI.Init() returned false (Check steam_appid.txt).";
                }
            }
            catch (Exception ex)
            {
                _isSteamInitialized = false;
                _steamErrorMessage = $"{ex.GetType().Name}: {ex.Message}";
                Debug.LogWarning($"[NetworkHudUI] Steam init check: {ex}");
            }
        }

        private void Update()
        {
            if (_isSteamInitialized)
            {
                try
                {
                    SteamAPI.RunCallbacks();
                }
                catch { }
            }

            // If hosting on LAN, broadcast beacon every second
            if (NetworkServer.active && !string.IsNullOrEmpty(_hostRoomCode))
            {
                if (Time.unscaledTime - _lastBroadcastTime > 1.0f)
                {
                    _lastBroadcastTime = Time.unscaledTime;
                    BroadcastHostBeacon();
                }
            }
        }

        private void OnDestroy()
        {
            StopDiscovery();
            try
            {
                if (_isSteamInitialized)
                {
                    SteamAPI.Shutdown();
                    _isSteamInitialized = false;
                }
            }
            catch { }
        }

        private void OnGUI()
        {
            GUI.Box(new Rect(10, 10, 360, 580), GUIContent.none);
            GUILayout.BeginArea(new Rect(16, 16, 348, 568));

            if (NetworkManager.singleton == null)
            {
                GUILayout.Label("<color=red><b>NetworkManager not found in scene!</b></color>");
                GUILayout.EndArea();
                return;
            }

            bool isNetworkActive = NetworkServer.active || NetworkClient.active;

            if (!isNetworkActive)
            {
                GUILayout.Label("<b>--- STEAM P2P (SPACEWAR 480) ---</b>");
                if (!_isSteamInitialized)
                {
                    GUILayout.Label("<color=yellow>Steam client not initialized.</color>");
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
                    string persona = SteamFriends.GetPersonaName();
                    ulong mySteamId = SteamUser.GetSteamID().m_SteamID;
                    GUILayout.Label($"Steam User: <b><color=cyan>{persona}</color></b>");
                    GUILayout.Label($"Your ID64: <color=yellow>{mySteamId}</color>");
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
                    StartLanHost();
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
                        ConnectToIp("127.0.0.1");
                    }
                    else if (IPAddress.TryParse(input, out _))
                    {
                        ConnectToIp(input);
                    }
                    else if (TryRoomCodeToIp(input, out string decodedIp))
                    {
                        ConnectToIp(decodedIp);
                    }
                    else
                    {
                        _statusMessage = $"Invalid input '{input}'! Enter a valid IPv4 address or Room Code.";
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
                bool isHost = NetworkServer.active && NetworkClient.isConnected;
                string mode = isHost ? "Host" : (NetworkClient.isConnected ? "Client" : "Connecting...");
                GUILayout.Label($"<b>Running as: {mode}</b>");

                if (isHost)
                {
                    if (!string.IsNullOrEmpty(_hostRoomCode))
                    {
                        GUILayout.Label($"<b>Your IP: <color=cyan>{GetLocalIPv4()}</color></b>");
                        GUILayout.Label($"<b>Room Code: <color=yellow>{_hostRoomCode}</color></b>");
                    }
                    else if (_isSteamInitialized)
                    {
                        GUILayout.Label($"<b>Steam ID: <color=yellow>{SteamUser.GetSteamID().m_SteamID}</color></b>");
                    }
                    GUILayout.Label($"Connected Players: {NetworkServer.connections.Count}");
                }

                GUILayout.Label("<color=#CCCCCC><i>(Press Esc or Tab to unlock mouse)</i></color>");

                if (GUILayout.Button("Disconnect", GUILayout.Height(35)))
                {
                    Disconnect();
                }
            }

            GUILayout.Space(8);
            GUILayout.Label("<b>Network Status:</b>");
            GUILayout.TextArea(_statusMessage, GUILayout.Height(70));

            GUILayout.EndArea();
        }

        // --- STEAM NETWORKING METHODS ---

        private void StartSteamHost()
        {
            if (!_isSteamInitialized)
            {
                _statusMessage = "Cannot host on Steam: Steam client is not running or logged in!";
                return;
            }

            try
            {
                StopDiscovery();
                EnsureTransports();

                // Switch transport to FizzySteamworks
                NetworkManager.singleton.transport = _fizzyTransport;
                Transport.active = _fizzyTransport;

                // Create public Steam lobby
                SteamAPICall_t call = SteamMatchmaking.CreateLobby(ELobbyType.k_ELobbyTypePublic, 4);
                _lobbyCreatedCallResult.Set(call);

                NetworkManager.singleton.StartHost();
                ulong mySteamId = SteamUser.GetSteamID().m_SteamID;
                _statusMessage = $"Hosting via Steam P2P!\nYour Steam ID64:\n<b><color=yellow>{mySteamId}</color></b>\n(Invite friends via Steam Overlay Shift+Tab)";
            }
            catch (Exception ex)
            {
                _statusMessage = $"Steam Host Exception: {ex.Message}";
                Debug.LogError($"[NetworkHudUI] Steam Host Error: {ex}");
            }
        }

        private void OnLobbyCreated(LobbyCreated_t callback, bool ioFailure)
        {
            if (ioFailure || callback.m_eResult != EResult.k_EResultOK)
            {
                Debug.LogWarning($"[NetworkHudUI] Steam lobby creation result: {callback.m_eResult}");
                return;
            }

            _currentLobbyId = new CSteamID(callback.m_ulSteamIDLobby);
            SteamMatchmaking.SetLobbyData(_currentLobbyId, "HostSteamID", SteamUser.GetSteamID().ToString());
            SteamMatchmaking.SetLobbyData(_currentLobbyId, "name", $"{SteamFriends.GetPersonaName()}'s Game");
            Debug.Log($"[NetworkHudUI] Steam Lobby created successfully! Lobby ID: {callback.m_ulSteamIDLobby}");
        }

        private void OnGameLobbyJoinRequested(GameLobbyJoinRequested_t callback)
        {
            Debug.Log($"[NetworkHudUI] Steam Lobby Join requested by invite to lobby: {callback.m_steamIDLobby}");
            string hostSteamIdStr = SteamMatchmaking.GetLobbyData(callback.m_steamIDLobby, "HostSteamID");
            if (ulong.TryParse(hostSteamIdStr, out ulong hostSteamId))
            {
                StartSteamClient(hostSteamId);
            }
            else
            {
                CSteamID owner = SteamMatchmaking.GetLobbyOwner(callback.m_steamIDLobby);
                StartSteamClient(owner.m_SteamID);
            }
        }

        private void StartSteamClient(ulong hostSteamId)
        {
            if (!_isSteamInitialized)
            {
                _statusMessage = "Cannot join on Steam: Steam client is not running or logged in!";
                return;
            }

            try
            {
                StopDiscovery();
                EnsureTransports();

                // Switch transport to FizzySteamworks
                NetworkManager.singleton.transport = _fizzyTransport;
                Transport.active = _fizzyTransport;

                NetworkManager.singleton.networkAddress = hostSteamId.ToString();
                NetworkManager.singleton.StartClient();

                _isConnecting = true;
                _statusMessage = $"Connecting to Steam Host {hostSteamId} via Valve Relay...";
            }
            catch (Exception ex)
            {
                _statusMessage = $"Steam Client Exception: {ex.Message}";
                Debug.LogError($"[NetworkHudUI] Steam Client Error: {ex}");
            }
        }

        // --- LAN METHODS ---

        private void StartLanHost()
        {
            StopDiscovery();
            EnsureTransports();

            string localIp = GetLocalIPv4();
            _hostRoomCode = IpToRoomCode(localIp);

            // Switch transport to KCP
            NetworkManager.singleton.transport = _kcpTransport;
            Transport.active = _kcpTransport;
            _kcpTransport.Port = _port;

            NetworkManager.singleton.StartHost();
            _statusMessage = $"LAN Host started!\nYour IP: <b><color=cyan>{localIp}</color></b>\nRoom Code: <b><color=yellow>{_hostRoomCode}</color></b>";
        }

        private void ConnectToIp(string targetIp)
        {
            StopDiscovery();
            EnsureTransports();

            // Switch transport to KCP
            NetworkManager.singleton.transport = _kcpTransport;
            Transport.active = _kcpTransport;
            _kcpTransport.Port = _port;

            NetworkManager.singleton.networkAddress = targetIp;
            NetworkManager.singleton.StartClient();

            _isConnecting = true;
            _statusMessage = $"Connecting to {targetIp}:{_port}... (Waiting for handshake)";
        }

        private void Disconnect()
        {
            _isConnecting = false;
            _hostRoomCode = "";
            Cursor.lockState = CursorLockMode.None;
            Cursor.visible = true;

            if (NetworkServer.active && NetworkClient.isConnected)
            {
                NetworkManager.singleton.StopHost();
            }
            else if (NetworkClient.active)
            {
                NetworkManager.singleton.StopClient();
            }
            else if (NetworkServer.active)
            {
                NetworkManager.singleton.StopServer();
            }

            if (_currentLobbyId != CSteamID.Nil && _isSteamInitialized)
            {
                SteamMatchmaking.LeaveLobby(_currentLobbyId);
                _currentLobbyId = CSteamID.Nil;
            }

            _statusMessage = "Disconnected by user.";
            if (_menuCamera != null) _menuCamera.gameObject.SetActive(true);
            StartListeningForHosts();
        }

        // --- LAN BROADCAST & SCANNING ---

        private void BroadcastHostBeacon()
        {
            try
            {
                if (string.IsNullOrEmpty(_hostRoomCode)) return;
                using (var udp = new UdpClient())
                {
                    udp.EnableBroadcast = true;
                    string message = $"{DiscoveryHeader}:{_hostRoomCode}:{_port}";
                    byte[] data = Encoding.UTF8.GetBytes(message);

                    udp.Send(data, data.Length, new IPEndPoint(IPAddress.Broadcast, DiscoveryPort));

                    string localIp = GetLocalIPv4();
                    int lastDot = localIp.LastIndexOf('.');
                    if (lastDot > 0)
                    {
                        string subnetBroadcast = localIp.Substring(0, lastDot) + ".255";
                        if (IPAddress.TryParse(subnetBroadcast, out var subnetAddr))
                        {
                            udp.Send(data, data.Length, new IPEndPoint(subnetAddr, DiscoveryPort));
                        }
                    }
                }
            }
            catch { }
        }

        private void StartListeningForHosts()
        {
            StopDiscovery();
            _discoveryCts = new CancellationTokenSource();
            var token = _discoveryCts.Token;

            Task.Run(async () =>
            {
                try
                {
                    _discoveryClient = new UdpClient();
                    _discoveryClient.Client.SetSocketOption(SocketOptionLevel.Socket, SocketOptionName.ReuseAddress, true);
                    _discoveryClient.Client.Bind(new IPEndPoint(IPAddress.Any, DiscoveryPort));

                    while (!token.IsCancellationRequested)
                    {
                        var result = await _discoveryClient.ReceiveAsync();
                        string msg = Encoding.UTF8.GetString(result.Buffer);
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
                var host = Dns.GetHostEntry(Dns.GetHostName());
                foreach (var ip in host.AddressList)
                {
                    if (ip.AddressFamily == AddressFamily.InterNetwork)
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

        private static string IpToRoomCode(string ipAddress)
        {
            if (IPAddress.TryParse(ipAddress, out var ip))
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
                    byte b0 = Convert.ToByte(code.Substring(0, 2), 16);
                    byte b1 = Convert.ToByte(code.Substring(2, 2), 16);
                    byte b2 = Convert.ToByte(code.Substring(4, 2), 16);
                    byte b3 = Convert.ToByte(code.Substring(6, 2), 16);
                    ipAddress = $"{b0}.{b1}.{b2}.{b3}";
                    return true;
                }
                catch { }
            }

            return false;
        }
    }
}
