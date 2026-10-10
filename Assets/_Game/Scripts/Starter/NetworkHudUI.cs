using FishNet;
using UnityEngine;

namespace Starter
{
    /// <summary>
    /// Simple immediate-mode GUI for starting Fish-Net Host or Client.
    /// Eliminates the need to wire up UI Canvas components while prototyping.
    /// </summary>
    public class NetworkHudUI : MonoBehaviour
    {
        private void OnGUI()
        {
            GUILayout.BeginArea(new Rect(10, 10, 320, 260));

            if (InstanceFinder.NetworkManager == null)
            {
                GUILayout.Label("FishNet NetworkManager not found in scene!");
                GUILayout.EndArea();
                return;
            }

            bool serverStarted = InstanceFinder.ServerManager != null && InstanceFinder.ServerManager.Started;
            bool clientStarted = InstanceFinder.ClientManager != null && InstanceFinder.ClientManager.Started;

            if (!serverStarted && !clientStarted)
            {
                GUILayout.Label("--- MULTIPLAYER PROTOTYPE (FISH-NET) ---");
                if (GUILayout.Button("Start Host (Server + Client)", GUILayout.Height(40)))
                {
                    InstanceFinder.ServerManager.StartConnection();
                    InstanceFinder.ClientManager.StartConnection();
                }

                if (GUILayout.Button("Start Client (Player 2)", GUILayout.Height(40)))
                {
                    InstanceFinder.ClientManager.StartConnection();
                }
            }
            else
            {
                string mode = (serverStarted && clientStarted) ? "Host" : (serverStarted ? "Dedicated Server" : "Client");
                GUILayout.Label($"Running as: {mode}");

                if (GUILayout.Button("Disconnect", GUILayout.Height(30)))
                {
                    if (serverStarted) InstanceFinder.ServerManager.StopConnection(true);
                    if (clientStarted) InstanceFinder.ClientManager.StopConnection();
                }
            }

            GUILayout.EndArea();
        }
    }
}
