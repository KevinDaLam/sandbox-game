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
using UnityEngine.UI;
using UnityEngine.EventSystems;

namespace Starter
{
    /// <summary>
    /// Dual Steam P2P (FizzySteamworks + Steamworks.NET) and LAN (KcpTransport) Network HUD.
    /// Uses modern Unity UI (Canvas) with Canvas Scaler (Scale With Screen Size)
    /// to ensure crisp, identical UI scaling across 1080p, 1440p, 4K, and Mac Retina displays.
    /// </summary>
    public class NetworkHudUI : MonoBehaviour
    {
        [Header("UI System")]
        [Tooltip("If checked, falls back to legacy IMGUI (OnGUI). Default false uses Unity UI Canvas.")]
        [SerializeField] private bool _useLegacyOnGUI = false;

        [Header("Network Inputs")]
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

        // Canvas UI References
        private GameObject _canvasGo;
        private GameObject _lobbyPanel;
        private GameObject _inGamePanel;
        private Transform _discoveryButtonsContainer;
        private Text _steamStatusText;
        private InputField _steamIdInputField;
        private InputField _lanIpInputField;
        private Text _statusLogText;
        private Text _inGameRoleText;
        private Text _inGameDetailsText;
        private Text _inGamePlayersText;
        private readonly Dictionary<string, GameObject> _spawnedDiscoveryButtons = new Dictionary<string, GameObject>();

        private void Awake()
        {
            EnsureTransports();
        }

        private void OnEnable()
        {
            NetworkClient.OnConnectedEvent += HandleClientConnected;
            NetworkClient.OnDisconnectedEvent += HandleClientDisconnected;
            NetworkClient.OnErrorEvent += HandleClientError;
            UnityEngine.SceneManagement.SceneManager.sceneLoaded += HandleSceneLoaded;
        }

        private void OnDisable()
        {
            NetworkClient.OnConnectedEvent -= HandleClientConnected;
            NetworkClient.OnDisconnectedEvent -= HandleClientDisconnected;
            NetworkClient.OnErrorEvent -= HandleClientError;
            UnityEngine.SceneManagement.SceneManager.sceneLoaded -= HandleSceneLoaded;
        }

        private void HandleSceneLoaded(UnityEngine.SceneManagement.Scene scene, UnityEngine.SceneManagement.LoadSceneMode mode)
        {
            if (scene.name == "PlatformerSandbox")
            {
                if (NetworkServer.active)
                {
                    foreach (var conn in NetworkServer.connections.Values)
                    {
                        if (conn.identity == null)
                        {
                            NetworkManager.singleton.OnServerAddPlayer(conn);
                        }
                    }
                }
            }
        }

        private void Start()
        {
            EnsureMenuCamera();
            EnsureEventSystem();
            if (!_useLegacyOnGUI)
            {
                CreateCanvasUI();
            }
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

        private void EnsureEventSystem()
        {
            if (EventSystem.current == null)
            {
                var esGo = new GameObject("EventSystem");
                esGo.transform.SetParent(transform, false);
                esGo.AddComponent<EventSystem>();
                esGo.AddComponent<UnityEngine.InputSystem.UI.InputSystemUIInputModule>();
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

            if (!_useLegacyOnGUI)
            {
                UpdateCanvasUI();
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

        // ==========================================
        // MODERN UNITY UI (CANVAS SCALER) SYSTEM
        // ==========================================

        private static Font GetDefaultFont()
        {
            var font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            if (font == null) font = Resources.GetBuiltinResource<Font>("Arial.fontdata");
            return font;
        }

        private void CreateCanvasUI()
        {
            if (_canvasGo != null) return;

            Font font = GetDefaultFont();

            // 1. Root Canvas with CanvasScaler set to ScaleWithScreenSize (1920x1080)
            _canvasGo = new GameObject("NetworkHUD_Canvas");
            _canvasGo.transform.SetParent(transform, false);

            var canvas = _canvasGo.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = 999;

            var scaler = _canvasGo.AddComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920, 1080);
            scaler.screenMatchMode = CanvasScaler.ScreenMatchMode.MatchWidthOrHeight;
            scaler.matchWidthOrHeight = 0.5f;

            _canvasGo.AddComponent<GraphicRaycaster>();

            // 2. Lobby Menu Panel (Left Sidebar card)
            _lobbyPanel = CreatePanel(_canvasGo.transform, "LobbyPanel",
                new Vector2(0, 1), new Vector2(0, 1), new Vector2(0, 1),
                new Vector2(40, -40), new Vector2(460, 720),
                new Color(0.08f, 0.10f, 0.14f, 0.94f));

            var lobbyVlg = _lobbyPanel.AddComponent<VerticalLayoutGroup>();
            lobbyVlg.padding = new RectOffset(20, 20, 16, 16);
            lobbyVlg.spacing = 8;
            lobbyVlg.childControlWidth = true;
            lobbyVlg.childControlHeight = true;
            lobbyVlg.childForceExpandWidth = true;
            lobbyVlg.childForceExpandHeight = false;

            var lobbyCsf = _lobbyPanel.AddComponent<ContentSizeFitter>();
            lobbyCsf.verticalFit = ContentSizeFitter.FitMode.PreferredSize;

            // Title
            CreateText(_lobbyPanel.transform, "SANDBOX MULTIPLAYER", 22, FontStyle.Bold, TextAnchor.MiddleLeft, Color.white, 30);
            CreateText(_lobbyPanel.transform, "Friendslop Co-op Prototype", 13, FontStyle.Normal, TextAnchor.MiddleLeft, new Color(0.6f, 0.7f, 0.8f), 18);

            CreateDivider(_lobbyPanel.transform, 10);

            // --- Steam P2P Section ---
            CreateText(_lobbyPanel.transform, "STEAM P2P (SPACEWAR 480)", 13, FontStyle.Bold, TextAnchor.MiddleLeft, new Color(0.35f, 0.75f, 1f), 20);
            _steamStatusText = CreateText(_lobbyPanel.transform, "Checking Steam status...", 12, FontStyle.Normal, TextAnchor.MiddleLeft, new Color(0.85f, 0.85f, 0.85f), 22);

            CreateButton(_lobbyPanel.transform, "🎮 Host Steam Lobby", 15, new Color(0.12f, 0.53f, 0.90f), new Color(0.20f, 0.65f, 1.0f), StartSteamHost, 40);

            // Steam Join Row
            var steamJoinRow = CreateHorizontalRow(_lobbyPanel.transform, 38, 8);
            _steamIdInputField = CreateInputField(steamJoinRow.transform, "Enter Friend's Steam ID64...", _steamIdInput, 13, (val) => _steamIdInput = val);
            var steamJoinBtn = CreateButton(steamJoinRow.transform, "🚀 Join", 14, new Color(0.18f, 0.65f, 0.28f), new Color(0.24f, 0.78f, 0.35f), () =>
            {
                if (ulong.TryParse(_steamIdInput.Trim(), out ulong targetId) && targetId > 0)
                {
                    StartSteamClient(targetId);
                }
                else
                {
                    _statusMessage = "Please enter a valid 17-digit Steam ID64!";
                }
            }, 38);
            steamJoinBtn.GetComponent<LayoutElement>().preferredWidth = 110;

            CreateDivider(_lobbyPanel.transform, 12);

            // --- LAN / Direct IP Section ---
            CreateText(_lobbyPanel.transform, "LOCAL LAN / DIRECT IP", 13, FontStyle.Bold, TextAnchor.MiddleLeft, new Color(1.0f, 0.75f, 0.35f), 20);
            CreateButton(_lobbyPanel.transform, "🏠 Host LAN Game", 15, new Color(0.90f, 0.45f, 0.10f), new Color(1.0f, 0.55f, 0.15f), StartLanHost, 40);

            // LAN Join Row
            var lanJoinRow = CreateHorizontalRow(_lobbyPanel.transform, 38, 8);
            _lanIpInputField = CreateInputField(lanJoinRow.transform, "127.0.0.1 or Room Code...", string.IsNullOrEmpty(_joinCodeInput) ? "127.0.0.1" : _joinCodeInput, 13, (val) => _joinCodeInput = val);
            var lanJoinBtn = CreateButton(lanJoinRow.transform, "🔗 Join", 14, new Color(0.0f, 0.55f, 0.50f), new Color(0.0f, 0.68f, 0.62f), () =>
            {
                string input = string.IsNullOrEmpty(_joinCodeInput) ? "127.0.0.1" : _joinCodeInput.Trim();
                if (IPAddress.TryParse(input, out _)) ConnectToIp(input);
                else if (TryRoomCodeToIp(input, out string decodedIp)) ConnectToIp(decodedIp);
                else _statusMessage = $"Invalid input '{input}'! Enter valid IPv4 or Room Code.";
            }, 38);
            lanJoinBtn.GetComponent<LayoutElement>().preferredWidth = 110;

            CreateDivider(_lobbyPanel.transform, 10);

            // --- LAN Auto-Discovery Section ---
            CreateText(_lobbyPanel.transform, "📡 Discovered LAN Games:", 12, FontStyle.Bold, TextAnchor.MiddleLeft, new Color(0.55f, 0.85f, 1f), 18);
            var discoveryContainer = new GameObject("DiscoveryList", typeof(RectTransform));
            discoveryContainer.transform.SetParent(_lobbyPanel.transform, false);
            var dLayout = discoveryContainer.AddComponent<VerticalLayoutGroup>();
            dLayout.spacing = 4;
            dLayout.childControlWidth = true;
            dLayout.childControlHeight = false;
            dLayout.childForceExpandWidth = true;
            discoveryContainer.AddComponent<LayoutElement>().preferredHeight = 65;
            _discoveryButtonsContainer = discoveryContainer.transform;

            // Status Log Area
            var logPanel = CreatePanel(_lobbyPanel.transform, "StatusLogBox",
                new Vector2(0, 0), new Vector2(1, 0), new Vector2(0.5f, 0),
                Vector2.zero, new Vector2(0, 60), new Color(0.04f, 0.05f, 0.07f, 1f));
            logPanel.AddComponent<LayoutElement>().preferredHeight = 65;
            _statusLogText = CreateText(logPanel.transform, _statusMessage, 12, FontStyle.Normal, TextAnchor.UpperLeft, new Color(0.8f, 0.85f, 0.9f), 60);
            var logRect = _statusLogText.GetComponent<RectTransform>();
            logRect.anchorMin = Vector2.zero;
            logRect.anchorMax = Vector2.one;
            logRect.offsetMin = new Vector2(8, 6);
            logRect.offsetMax = new Vector2(-8, -6);

            // 3. In-Game HUD Panel (Shown when connected)
            _inGamePanel = new GameObject("InGameHUD", typeof(RectTransform));
            _inGamePanel.transform.SetParent(_canvasGo.transform, false);
            var inGameRect = _inGamePanel.GetComponent<RectTransform>();
            inGameRect.anchorMin = Vector2.zero;
            inGameRect.anchorMax = Vector2.one;
            inGameRect.offsetMin = Vector2.zero;
            inGameRect.offsetMax = Vector2.zero;

            // Top-Left In-Game Status Badge
            var badgePanel = CreatePanel(_inGamePanel.transform, "StatusBadge",
                new Vector2(0, 1), new Vector2(0, 1), new Vector2(0, 1),
                new Vector2(30, -30), new Vector2(360, 140),
                new Color(0.08f, 0.10f, 0.14f, 0.88f));

            var badgeVlg = badgePanel.AddComponent<VerticalLayoutGroup>();
            badgeVlg.padding = new RectOffset(16, 16, 12, 12);
            badgeVlg.spacing = 6;
            badgeVlg.childControlWidth = true;
            badgeVlg.childControlHeight = true;
            badgeVlg.childForceExpandWidth = true;
            badgeVlg.childForceExpandHeight = false;

            var csf = badgePanel.AddComponent<ContentSizeFitter>();
            csf.verticalFit = ContentSizeFitter.FitMode.PreferredSize;

            _inGameRoleText = CreateText(badgePanel.transform, "🟢 ONLINE • HOST", 16, FontStyle.Bold, TextAnchor.MiddleLeft, new Color(0.5f, 0.9f, 0.5f), 22);
            _inGameDetailsText = CreateText(badgePanel.transform, "Room Code: ----", 13, FontStyle.Normal, TextAnchor.MiddleLeft, Color.white, 20);
            _inGamePlayersText = CreateText(badgePanel.transform, "Connected Players: 1", 13, FontStyle.Normal, TextAnchor.MiddleLeft, new Color(0.7f, 0.8f, 0.9f), 20);

            CreateButton(badgePanel.transform, "Disconnect", 14, new Color(0.80f, 0.20f, 0.20f), new Color(0.92f, 0.25f, 0.25f), Disconnect, 32);

            // Bottom-Center Controls Hint Bar
            var hintPanel = CreatePanel(_inGamePanel.transform, "ControlsHintBar",
                new Vector2(0.5f, 0), new Vector2(0.5f, 0), new Vector2(0.5f, 0),
                new Vector2(0, 30), new Vector2(620, 38),
                new Color(0.06f, 0.08f, 0.10f, 0.78f));

            var hintText = CreateText(hintPanel.transform, "[ESC / TAB] Unlock Mouse   •   [WASD] Move   •   [E] Pick up Box", 14, FontStyle.Normal, TextAnchor.MiddleCenter, new Color(0.78f, 0.86f, 0.92f), 36);
            var hintRect = hintText.GetComponent<RectTransform>();
            hintRect.anchorMin = Vector2.zero;
            hintRect.anchorMax = Vector2.one;
            hintRect.offsetMin = Vector2.zero;
            hintRect.offsetMax = Vector2.zero;

            _inGamePanel.SetActive(false);
        }

        private void UpdateCanvasUI()
        {
            if (_lobbyPanel == null || _inGamePanel == null) return;

            bool isNetworkActive = NetworkServer.active || NetworkClient.isConnected;

            _lobbyPanel.SetActive(!isNetworkActive);
            _inGamePanel.SetActive(isNetworkActive);

            if (!isNetworkActive)
            {
                // Update Steam status text
                if (_isSteamInitialized)
                {
                    string persona = SteamFriends.GetPersonaName();
                    ulong steamId = SteamUser.GetSteamID().m_SteamID;
                    _steamStatusText.text = $"👤 Steam: <b>{persona}</b> (ID: {steamId})";
                    _steamStatusText.color = new Color(0.4f, 0.85f, 1f);
                }
                else
                {
                    _steamStatusText.text = string.IsNullOrEmpty(_steamErrorMessage)
                        ? "⚠️ Steam not detected (LAN available)"
                        : $"⚠️ Steam: {_steamErrorMessage}";
                    _steamStatusText.color = new Color(1f, 0.8f, 0.4f);
                }

                if (_statusLogText != null)
                {
                    _statusLogText.text = _statusMessage;
                }

                // Update dynamic LAN discovery buttons
                UpdateDiscoveryButtons();
            }
            else
            {
                bool isHost = NetworkServer.active;
                _inGameRoleText.text = isHost ? "🟢 ONLINE • HOST" : "🔵 ONLINE • CLIENT";
                _inGameRoleText.color = isHost ? new Color(0.45f, 0.90f, 0.45f) : new Color(0.45f, 0.75f, 1f);

                if (isHost)
                {
                    if (!string.IsNullOrEmpty(_hostRoomCode))
                    {
                        _inGameDetailsText.text = $"LAN Room: <b>{_hostRoomCode}</b> (IP: {GetLocalIPv4()})";
                    }
                    else if (_isSteamInitialized)
                    {
                        _inGameDetailsText.text = $"Steam Host ID: <b>{SteamUser.GetSteamID().m_SteamID}</b>";
                    }
                    else
                    {
                        _inGameDetailsText.text = "Host active";
                    }
                }
                else
                {
                    _inGameDetailsText.text = "Connected to Host";
                }

                int playerCount = NetworkServer.active ? NetworkServer.connections.Count : (NetworkClient.isConnected ? 1 : 0);
                _inGamePlayersText.text = $"Connected Players: <b>{playerCount}</b>";
            }
        }

        private void UpdateDiscoveryButtons()
        {
            if (_discoveryButtonsContainer == null) return;

            lock (_discoveredRooms)
            {
                foreach (var kvp in _discoveredRooms)
                {
                    string code = kvp.Key;
                    string ip = kvp.Value;

                    if (!_spawnedDiscoveryButtons.ContainsKey(code))
                    {
                        var btn = CreateButton(_discoveryButtonsContainer, $"👉 Join [{code}] ({ip})", 13,
                            new Color(0.18f, 0.38f, 0.58f), new Color(0.24f, 0.48f, 0.72f), () => ConnectToIp(ip), 28);
                        _spawnedDiscoveryButtons[code] = btn;
                    }
                }
            }
        }

        // ==========================================
        // UI BUILDING HELPERS
        // ==========================================

        private static GameObject CreatePanel(Transform parent, string name, Vector2 anchorMin, Vector2 anchorMax, Vector2 pivot, Vector2 anchoredPosition, Vector2 size, Color bgColor)
        {
            var go = new GameObject(name, typeof(RectTransform), typeof(Image));
            go.transform.SetParent(parent, false);
            var rect = go.GetComponent<RectTransform>();
            rect.anchorMin = anchorMin;
            rect.anchorMax = anchorMax;
            rect.pivot = pivot;
            rect.anchoredPosition = anchoredPosition;
            rect.sizeDelta = size;

            var img = go.GetComponent<Image>();
            img.color = bgColor;
            return go;
        }

        private static GameObject CreateHorizontalRow(Transform parent, float height, float spacing)
        {
            var go = new GameObject("Row", typeof(RectTransform), typeof(HorizontalLayoutGroup), typeof(LayoutElement));
            go.transform.SetParent(parent, false);
            var hlg = go.GetComponent<HorizontalLayoutGroup>();
            hlg.spacing = spacing;
            hlg.childControlWidth = true;
            hlg.childControlHeight = true;
            hlg.childForceExpandWidth = false;
            hlg.childForceExpandHeight = true;

            var le = go.GetComponent<LayoutElement>();
            le.minHeight = height;
            le.preferredHeight = height;
            le.flexibleWidth = 1f;
            return go;
        }

        private static Text CreateText(Transform parent, string content, int fontSize, FontStyle style, TextAnchor alignment, Color color, float preferredHeight)
        {
            var go = new GameObject("Text", typeof(RectTransform), typeof(Text), typeof(LayoutElement));
            go.transform.SetParent(parent, false);

            var text = go.GetComponent<Text>();
            text.font = GetDefaultFont();
            text.text = content;
            text.fontSize = fontSize;
            text.fontStyle = style;
            text.alignment = alignment;
            text.color = color;
            text.horizontalOverflow = HorizontalWrapMode.Wrap;
            text.verticalOverflow = VerticalWrapMode.Overflow;

            var le = go.GetComponent<LayoutElement>();
            le.minHeight = preferredHeight;
            le.preferredHeight = preferredHeight;
            le.flexibleWidth = 1f;
            return text;
        }

        private static GameObject CreateButton(Transform parent, string label, int fontSize, Color normalColor, Color hoverColor, UnityEngine.Events.UnityAction onClick, float height)
        {
            var go = new GameObject("Button", typeof(RectTransform), typeof(Image), typeof(Button), typeof(LayoutElement));
            go.transform.SetParent(parent, false);

            var img = go.GetComponent<Image>();
            img.color = normalColor;

            var btn = go.GetComponent<Button>();
            var colors = btn.colors;
            colors.normalColor = normalColor;
            colors.highlightedColor = hoverColor;
            colors.pressedColor = normalColor * 0.8f;
            btn.colors = colors;
            if (onClick != null) btn.onClick.AddListener(onClick);

            var le = go.GetComponent<LayoutElement>();
            le.minHeight = height;
            le.preferredHeight = height;
            le.flexibleWidth = 1f;

            // Label
            var textGo = new GameObject("Label", typeof(RectTransform), typeof(Text));
            textGo.transform.SetParent(go.transform, false);
            var rect = textGo.GetComponent<RectTransform>();
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.offsetMin = Vector2.zero;
            rect.offsetMax = Vector2.zero;

            var txt = textGo.GetComponent<Text>();
            txt.font = GetDefaultFont();
            txt.text = label;
            txt.fontSize = fontSize;
            txt.fontStyle = FontStyle.Bold;
            txt.alignment = TextAnchor.MiddleCenter;
            txt.color = Color.white;

            return go;
        }

        private static InputField CreateInputField(Transform parent, string placeholder, string initialText, int fontSize, Action<string> onValueChanged)
        {
            var go = new GameObject("InputField", typeof(RectTransform), typeof(Image), typeof(InputField), typeof(LayoutElement));
            go.transform.SetParent(parent, false);

            var img = go.GetComponent<Image>();
            img.color = new Color(0.12f, 0.15f, 0.20f, 1f);

            var inputField = go.GetComponent<InputField>();

            // Text
            var textGo = new GameObject("Text", typeof(RectTransform), typeof(Text));
            textGo.transform.SetParent(go.transform, false);
            var textRect = textGo.GetComponent<RectTransform>();
            textRect.anchorMin = Vector2.zero;
            textRect.anchorMax = Vector2.one;
            textRect.offsetMin = new Vector2(10, 0);
            textRect.offsetMax = new Vector2(-10, 0);

            var txt = textGo.GetComponent<Text>();
            txt.font = GetDefaultFont();
            txt.fontSize = fontSize;
            txt.alignment = TextAnchor.MiddleLeft;
            txt.color = Color.white;
            txt.supportRichText = false;

            // Placeholder
            var placeGo = new GameObject("Placeholder", typeof(RectTransform), typeof(Text));
            placeGo.transform.SetParent(go.transform, false);
            var placeRect = placeGo.GetComponent<RectTransform>();
            placeRect.anchorMin = Vector2.zero;
            placeRect.anchorMax = Vector2.one;
            placeRect.offsetMin = new Vector2(10, 0);
            placeRect.offsetMax = new Vector2(-10, 0);

            var placeTxt = placeGo.GetComponent<Text>();
            placeTxt.font = GetDefaultFont();
            placeTxt.text = placeholder;
            placeTxt.fontSize = fontSize;
            placeTxt.fontStyle = FontStyle.Italic;
            placeTxt.alignment = TextAnchor.MiddleLeft;
            placeTxt.color = new Color(0.45f, 0.55f, 0.65f);

            inputField.textComponent = txt;
            inputField.placeholder = placeTxt;
            inputField.text = initialText ?? "";

            if (onValueChanged != null) inputField.onValueChanged.AddListener((v) => onValueChanged(v));

            var le = go.GetComponent<LayoutElement>();
            le.minHeight = 36;
            le.preferredHeight = 36;
            le.flexibleWidth = 1f;

            return inputField;
        }

        private static void CreateDivider(Transform parent, float spacing)
        {
            var go = new GameObject("Divider", typeof(RectTransform), typeof(Image), typeof(LayoutElement));
            go.transform.SetParent(parent, false);

            var img = go.GetComponent<Image>();
            img.color = new Color(0.20f, 0.24f, 0.32f, 0.8f);

            var le = go.GetComponent<LayoutElement>();
            le.minHeight = 2;
            le.preferredHeight = 2;
            le.flexibleWidth = 1f;
        }

        // ==========================================
        // LEGACY ONGUI FALLBACK
        // ==========================================

        private void OnGUI()
        {
            if (!_useLegacyOnGUI) return;

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
                    if (string.IsNullOrEmpty(input)) ConnectToIp("127.0.0.1");
                    else if (IPAddress.TryParse(input, out _)) ConnectToIp(input);
                    else if (TryRoomCodeToIp(input, out string decodedIp)) ConnectToIp(decodedIp);
                    else _statusMessage = $"Invalid input '{input}'! Enter a valid IPv4 address or Room Code.";
                }
                GUI.enabled = true;

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

        // ==========================================
        // STEAM NETWORKING METHODS
        // ==========================================

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
                _isConnecting = false;
                _statusMessage = $"Steam Connect Exception: {ex.Message}";
                Debug.LogError($"[NetworkHudUI] Steam Connect Error: {ex}");
            }
        }

        // ==========================================
        // LAN & DIRECT IP NETWORKING METHODS
        // ==========================================

        private void StartLanHost()
        {
            try
            {
                StopDiscovery();
                EnsureTransports();

                // Switch transport to KCP
                NetworkManager.singleton.transport = _kcpTransport;
                Transport.active = _kcpTransport;

                NetworkManager.singleton.StartHost();

                string localIp = GetLocalIPv4();
                _hostRoomCode = IpToRoomCode(localIp);
                _statusMessage = $"Hosting LAN Game!\nLocal IP: {localIp}\nRoom Code: {_hostRoomCode}";
            }
            catch (Exception ex)
            {
                _statusMessage = $"LAN Host Exception: {ex.Message}";
                Debug.LogError($"[NetworkHudUI] LAN Host Error: {ex}");
            }
        }

        private void ConnectToIp(string targetIp)
        {
            try
            {
                StopDiscovery();
                EnsureTransports();

                // Switch transport to KCP
                NetworkManager.singleton.transport = _kcpTransport;
                Transport.active = _kcpTransport;

                NetworkManager.singleton.networkAddress = targetIp.Trim();
                NetworkManager.singleton.StartClient();

                _isConnecting = true;
                _statusMessage = $"Connecting to LAN Host at {targetIp}...";
            }
            catch (Exception ex)
            {
                _isConnecting = false;
                _statusMessage = $"LAN Connect Exception: {ex.Message}";
                Debug.LogError($"[NetworkHudUI] LAN Connect Error: {ex}");
            }
        }

        private void Disconnect()
        {
            try
            {
                _isConnecting = false;
                _hostRoomCode = "";

                if (NetworkServer.active && NetworkClient.isConnected)
                {
                    NetworkManager.singleton.StopHost();
                }
                else if (NetworkClient.isConnected)
                {
                    NetworkManager.singleton.StopClient();
                }

                if (_isSteamInitialized && _currentLobbyId != CSteamID.Nil)
                {
                    SteamMatchmaking.LeaveLobby(_currentLobbyId);
                    _currentLobbyId = CSteamID.Nil;
                }

                _statusMessage = "Disconnected from session.";
            }
            catch (Exception ex)
            {
                Debug.LogWarning($"[NetworkHudUI] Disconnect warning: {ex.Message}");
            }
        }

        // ==========================================
        // UDP LAN AUTO-DISCOVERY BEACONS
        // ==========================================

        private void BroadcastHostBeacon()
        {
            try
            {
                using (var udp = new UdpClient())
                {
                    udp.EnableBroadcast = true;
                    string beacon = $"{DiscoveryHeader}:{_hostRoomCode}";
                    byte[] data = Encoding.UTF8.GetBytes(beacon);
                    udp.Send(data, data.Length, new IPEndPoint(IPAddress.Broadcast, DiscoveryPort));
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
