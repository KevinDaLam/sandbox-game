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
        private void OnGUI()
        {
            GUILayout.BeginArea(new Rect(10, 10, 300, 300));

            if (NetworkManager.Singleton == null)
            {
                GUILayout.Label("NetworkManager not found in scene!");
                GUILayout.EndArea();
                return;
            }

            if (!NetworkManager.Singleton.IsClient && !NetworkManager.Singleton.IsServer)
            {
                GUILayout.Label("--- MULTIPLAYER PROTOTYPE ---");
                if (GUILayout.Button("Start Host (Player 1)", GUILayout.Height(40)))
                {
                    NetworkManager.Singleton.StartHost();
                }

                if (GUILayout.Button("Start Client (Player 2)", GUILayout.Height(40)))
                {
                    NetworkManager.Singleton.StartClient();
                }
            }
            else
            {
                string mode = NetworkManager.Singleton.IsHost ? "Host" : "Client";
                GUILayout.Label($"Running as: {mode}");
                GUILayout.Label($"Connected Clients: {NetworkManager.Singleton.ConnectedClients.Count}");

                if (GUILayout.Button("Disconnect", GUILayout.Height(30)))
                {
                    NetworkManager.Singleton.Shutdown();
                }
            }

            GUILayout.EndArea();
        }
    }
}
