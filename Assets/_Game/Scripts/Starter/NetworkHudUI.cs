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
        [SerializeField] private string _joinCodeInput = "";
        [SerializeField] private string _ipAddress = "127.0.0.1";
        [SerializeField] private ushort _port = 7777;

        private string _statusMessage = "Idle. Ready to connect.";
        private string _hostRoomCode = "";
        private bool _isConnecting = false;

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
            if (NetworkManager.Singleton != null)
            {
                NetworkManager.Singleton.OnClientConnectedCallback -= HandleClientConnected;
                NetworkManager.Singleton.OnClientDisconnectCallback -= HandleClientDisconnected;
            }
        }

        private void Start()
        {
            if (NetworkManager.Singleton != null)
            {
                NetworkManager.Singleton.OnClientConnectedCallback -= HandleClientConnected;
                NetworkManager.Singleton.OnClientConnectedCallback += HandleClientConnected;
                NetworkManager.Singleton.OnClientDisconnectCallback -= HandleClientDisconnected;
                NetworkManager.Singleton.OnClientDisconnectCallback += HandleClientDisconnected;
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
        }

        private void OnGUI()
        {
            GUILayout.BeginArea(new Rect(10, 10, 360, 380));

            if (NetworkManager.Singleton == null)
            {
                GUILayout.Label("<color=red><b>NetworkManager not found in scene!</b></color>");
                GUILayout.EndArea();
                return;
            }

            if (!NetworkManager.Singleton.IsClient && !NetworkManager.Singleton.IsServer)
            {
                GUILayout.Label("<b>--- 2 PLAYERS PROTOTYPE ---</b>");

                if (GUILayout.Button("Start Host (Player 1)", GUILayout.Height(38)))
                {
                    string localIp = GetLocalIPv4();
                    _hostRoomCode = IpToRoomCode(localIp);
                    ConfigureTransportAddress("127.0.0.1", _port, "0.0.0.0");
                    bool success = NetworkManager.Singleton.StartHost();
                    _statusMessage = success 
                        ? $"Host started on LAN ({localIp})!\nShare Room Code: <b><color=yellow>{_hostRoomCode}</color></b>" 
                        : "Failed to start Host!";
                }

                GUILayout.Space(8);
                GUILayout.Label("<b>Join with 6-Digit Room Code:</b>");
                _joinCodeInput = GUILayout.TextField(_joinCodeInput, 6).ToUpper().Trim();

                GUI.enabled = !_isConnecting;
                if (GUILayout.Button(_isConnecting ? "Connecting..." : "Join Game (Player 2)", GUILayout.Height(38)))
                {
                    if (string.IsNullOrEmpty(_joinCodeInput))
                    {
                        // Fallback to local loopback if blank
                        ConnectToIp("127.0.0.1");
                    }
                    else if (TryRoomCodeToIp(_joinCodeInput, out string decodedIp))
                    {
                        ConnectToIp(decodedIp);
                    }
                    else
                    {
                        _statusMessage = $"Invalid Room Code '{_joinCodeInput}'! Must be 6 alphanumeric letters/digits.";
                    }
                }
                GUI.enabled = true;
            }
            else
            {
                string mode = NetworkManager.Singleton.IsHost ? "Host" : "Client";
                GUILayout.Label($"<b>Running as: {mode}</b>");
                if (NetworkManager.Singleton.IsHost && !string.IsNullOrEmpty(_hostRoomCode))
                {
                    GUILayout.Label($"<b>Room Code: <color=yellow>{_hostRoomCode}</color></b>");
                }
                GUILayout.Label($"Connected Players: {NetworkManager.Singleton.ConnectedClients.Count}");

                if (GUILayout.Button("Disconnect", GUILayout.Height(35)))
                {
                    _isConnecting = false;
                    _hostRoomCode = "";
                    NetworkManager.Singleton.Shutdown();
                    _statusMessage = "Disconnected by user.";
                }
            }

            GUILayout.Space(8);
            GUILayout.Label("<b>Network Status:</b>");
            GUILayout.TextArea(_statusMessage, GUILayout.Height(75));

            GUILayout.EndArea();
        }

        private void ConnectToIp(string targetIp)
        {
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

        // --- 6-Digit Room Code Base36 Converter ---
        private const string Alphabet = "0123456789ABCDEFGHJKLMNPQRSTUVWXYZ"; // 32 unambiguous chars (no I, O)

        private static string IpToRoomCode(string ipAddress)
        {
            if (System.Net.IPAddress.TryParse(ipAddress, out var ip))
            {
                byte[] bytes = ip.GetAddressBytes();
                // 32-bit uint
                uint val = ((uint)bytes[0] << 24) | ((uint)bytes[1] << 16) | ((uint)bytes[2] << 8) | bytes[3];
                char[] chars = new char[6];
                for (int i = 5; i >= 0; i--)
                {
                    chars[i] = Alphabet[(int)(val % (uint)Alphabet.Length)];
                    val /= (uint)Alphabet.Length;
                }
                return new string(chars);
            }
            return "LOCAL1";
        }

        private static bool TryRoomCodeToIp(string code, out string ipAddress)
        {
            ipAddress = null;
            if (string.IsNullOrEmpty(code) || code.Length != 6) return false;

            if (code == "LOCAL1")
            {
                ipAddress = "127.0.0.1";
                return true;
            }

            uint val = 0;
            for (int i = 0; i < 6; i++)
            {
                int index = Alphabet.IndexOf(char.ToUpper(code[i]));
                if (index < 0) return false;
                val = val * (uint)Alphabet.Length + (uint)index;
            }

            byte b1 = (byte)((val >> 24) & 0xFF);
            byte b2 = (byte)((val >> 16) & 0xFF);
            byte b3 = (byte)((val >> 8) & 0xFF);
            byte b4 = (byte)(val & 0xFF);

            ipAddress = $"{b1}.{b2}.{b3}.{b4}";
            return true;
        }
    }
}
