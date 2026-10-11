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
        [SerializeField] private string _ipAddress = "127.0.0.1";
        [SerializeField] private ushort _port = 7777;

        private string _statusMessage = "Idle. Ready to connect.";
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
            // Subscribe here as well in case NetworkManager wasn't awake during OnEnable
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
                reason = "Connection timed out or host rejected connection (Check IP, Port 7777, and Host Firewall).";
            }
            _statusMessage = $"Disconnected (ID: {clientId}): {reason}";
            Debug.LogWarning($"[NetworkHudUI] Disconnected: {reason}");
        }

        private void OnGUI()
        {
            GUILayout.BeginArea(new Rect(10, 10, 360, 340));

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
                    ConfigureTransportAddress("127.0.0.1", _port, "0.0.0.0");
                    bool success = NetworkManager.Singleton.StartHost();
                    _statusMessage = success ? $"Host started. Listening on 0.0.0.0:{_port}" : "Failed to start Host!";
                }

                GUILayout.Space(5);
                GUILayout.Label("<b>Join Host by IP:</b>");
                _ipAddress = GUILayout.TextField(_ipAddress);

                GUI.enabled = !_isConnecting;
                if (GUILayout.Button(_isConnecting ? "Connecting..." : "Start Client (Player 2)", GUILayout.Height(38)))
                {
                    string targetIp = string.IsNullOrWhiteSpace(_ipAddress) ? "127.0.0.1" : _ipAddress.Trim();
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
                GUI.enabled = true;
            }
            else
            {
                string mode = NetworkManager.Singleton.IsHost ? "Host" : "Client";
                GUILayout.Label($"<b>Running as: {mode}</b>");
                GUILayout.Label($"Connected Players: {NetworkManager.Singleton.ConnectedClients.Count}");

                if (GUILayout.Button("Disconnect", GUILayout.Height(35)))
                {
                    _isConnecting = false;
                    NetworkManager.Singleton.Shutdown();
                    _statusMessage = "Disconnected by user.";
                }
            }

            GUILayout.Space(8);
            GUILayout.Label("<b>Network Status:</b>");
            GUILayout.TextArea(_statusMessage, GUILayout.Height(65));

            GUILayout.EndArea();
        }

        private void ConfigureTransportAddress(string ip, ushort port, string listenAddress = "0.0.0.0")
        {
            var transport = NetworkManager.Singleton.GetComponent<Unity.Netcode.Transports.UTP.UnityTransport>();
            if (transport != null)
            {
                transport.SetConnectionData(ip, port, listenAddress);
            }
        }
    }
}
