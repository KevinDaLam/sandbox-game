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

        private void OnGUI()
        {
            GUILayout.BeginArea(new Rect(10, 10, 320, 280));

            if (NetworkManager.Singleton == null)
            {
                GUILayout.Label("NetworkManager not found in scene!");
                GUILayout.EndArea();
                return;
            }

            if (!NetworkManager.Singleton.IsClient && !NetworkManager.Singleton.IsServer)
            {
                GUILayout.Label("<b>--- 2 PLAYERS PROTOTYPE ---</b>");

                if (GUILayout.Button("Start Host (Player 1)", GUILayout.Height(40)))
                {
                    ConfigureTransportAddress("127.0.0.1", _port, "0.0.0.0");
                    NetworkManager.Singleton.StartHost();
                }

                GUILayout.Space(5);
                GUILayout.Label("<b>Join Host by IP:</b>");
                _ipAddress = GUILayout.TextField(_ipAddress);

                if (GUILayout.Button("Start Client (Player 2)", GUILayout.Height(40)))
                {
                    string targetIp = string.IsNullOrWhiteSpace(_ipAddress) ? "127.0.0.1" : _ipAddress.Trim();
                    ConfigureTransportAddress(targetIp, _port, "0.0.0.0");
                    NetworkManager.Singleton.StartClient();
                }
            }
            else
            {
                string mode = NetworkManager.Singleton.IsHost ? "Host" : "Client";
                GUILayout.Label($"<b>Running as: {mode}</b>");
                GUILayout.Label($"Connected Players: {NetworkManager.Singleton.ConnectedClients.Count}");

                if (GUILayout.Button("Disconnect", GUILayout.Height(35)))
                {
                    NetworkManager.Singleton.Shutdown();
                }
            }

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
