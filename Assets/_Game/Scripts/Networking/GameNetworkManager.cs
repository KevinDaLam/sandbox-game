using Unity.Netcode;
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
            if (_autoStartHostInEditor && NetworkManager.Singleton != null && !NetworkManager.Singleton.IsListening)
            {
                StartHost();
            }
#endif
        }

        public void StartHost()
        {
            if (NetworkManager.Singleton == null)
            {
                Debug.LogError("[GameNetworkManager] NetworkManager singleton is missing!");
                return;
            }

            Debug.Log("[GameNetworkManager] Starting Host session...");
            NetworkManager.Singleton.StartHost();
        }

        public void StartClient()
        {
            if (NetworkManager.Singleton == null)
            {
                Debug.LogError("[GameNetworkManager] NetworkManager singleton is missing!");
                return;
            }

            Debug.Log("[GameNetworkManager] Connecting as Client...");
            NetworkManager.Singleton.StartClient();
        }

        public void Disconnect()
        {
            if (NetworkManager.Singleton != null && NetworkManager.Singleton.IsListening)
            {
                NetworkManager.Singleton.Shutdown();
            }
        }
    }
}
