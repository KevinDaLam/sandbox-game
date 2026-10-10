using FishNet;
using UnityEngine;

namespace Game.Networking
{
    public class GameNetworkManager : MonoBehaviour
    {
        public static GameNetworkManager Instance { get; private set; }

        [Header("Status")]
        [SerializeField] private bool _autoStartHostInEditor = false;

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }
            Instance = this;
            DontDestroyOnLoad(gameObject);
        }

        private void Start()
        {
#if UNITY_EDITOR
            if (_autoStartHostInEditor && InstanceFinder.NetworkManager != null)
            {
                if (!InstanceFinder.ServerManager.Started && !InstanceFinder.ClientManager.Started)
                {
                    StartHost();
                }
            }
#endif
        }

        public void StartHost()
        {
            if (InstanceFinder.NetworkManager == null)
            {
                Debug.LogError("[GameNetworkManager] FishNet NetworkManager is missing!");
                return;
            }

            Debug.Log("[GameNetworkManager] Starting Host session (Server + Client)...");
            InstanceFinder.ServerManager.StartConnection();
            InstanceFinder.ClientManager.StartConnection();
        }

        public void StartClient()
        {
            if (InstanceFinder.NetworkManager == null)
            {
                Debug.LogError("[GameNetworkManager] FishNet NetworkManager is missing!");
                return;
            }

            Debug.Log("[GameNetworkManager] Connecting as Client...");
            InstanceFinder.ClientManager.StartConnection();
        }

        public void Disconnect()
        {
            if (InstanceFinder.NetworkManager != null)
            {
                if (InstanceFinder.ServerManager.Started)
                    InstanceFinder.ServerManager.StopConnection(true);
                if (InstanceFinder.ClientManager.Started)
                    InstanceFinder.ClientManager.StopConnection();
            }
        }
    }
}
