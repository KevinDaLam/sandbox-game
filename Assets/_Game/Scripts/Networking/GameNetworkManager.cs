using UnityEngine;

namespace Game.Networking
{
    public class GameNetworkManager : MonoBehaviour
    {
        public static GameNetworkManager Instance { get; private set; }

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
    }
}
