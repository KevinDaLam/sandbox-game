using Mirror;
using UnityEngine;
using UnityEngine.EventSystems;

namespace Game.Platformer
{
    /// <summary>
    /// Network-aware 3D Platformer Player Driver.
    /// Works seamlessly both offline (for solo sandbox testing) and online via Mirror (Steam/LAN).
    /// </summary>
    public class PlatformerPlayerDriver : NetworkBehaviour
    {
        private const float RagdollCameraOffset = 0.7f;

        [SerializeField] private PlatformerMotor _motor;
        [SerializeField] private PlatformerRagdoll _ragdoll;
        [SerializeField] private PlayerKick _kick;
        [SerializeField] private Transform _cameraRig;
        [SerializeField] private Transform _cameraPitch;
        [SerializeField] private float _eyeHeight = 1.6f;
        [SerializeField] private float _mouseSensitivity = 2f;
        [SerializeField] private float _pitchLimit = 85f;

        private float _yawAccumulated;
        private float _viewYaw;
        private float _pitch;
        private Vector2 _move;
        private bool _jumpPressed;
        private bool _jumpHeld;
        private bool _sprint;
        private bool _kickPressed;
        private bool _ragdollTogglePressed;
        private bool _wasRagdolled;
        private Vector3 _previousPosition;
        private Vector3 _currentPosition;
        private bool _isCursorLocked = true;

        private bool IsRagdolled => _ragdoll != null && _ragdoll.IsActive;
        private bool IsControlledLocally => (!NetworkClient.active && !NetworkServer.active) || isLocalPlayer;

        private void Awake()
        {
            _viewYaw = transform.eulerAngles.y;
            _previousPosition = _currentPosition = transform.position;
            if (_cameraRig != null)
            {
                _cameraRig.gameObject.SetActive(false);
            }
            enabled = false;
        }

        private void Start()
        {
            // If running offline without Mirror network server/client, auto-activate as local player
            if (!NetworkClient.active && !NetworkServer.active)
            {
                ActivateLocalPlayer();
            }
        }

        public override void OnStartLocalPlayer()
        {
            base.OnStartLocalPlayer();
            ActivateLocalPlayer();
        }

        public override void OnStopLocalPlayer()
        {
            base.OnStopLocalPlayer();
            SetCursorLock(false);
            if (_cameraRig != null)
            {
                _cameraRig.gameObject.SetActive(false);
            }
        }

        private void ActivateLocalPlayer()
        {
            enabled = true;
            _viewYaw = transform.eulerAngles.y;
            _previousPosition = _currentPosition = transform.position;

            if (_cameraRig != null)
            {
                _cameraRig.gameObject.SetActive(true);
                var cam = _cameraRig.GetComponentInChildren<Camera>();
                if (cam != null)
                {
                    cam.enabled = true;
                    cam.tag = "MainCamera";
                }
            }

            // Deactivate any scene fallback cameras (such as MenuCamera)
            Camera[] allCameras = FindObjectsByType<Camera>(FindObjectsSortMode.None);
            for (int i = 0; i < allCameras.Length; i++)
            {
                if (_cameraRig == null || !allCameras[i].transform.IsChildOf(_cameraRig))
                {
                    allCameras[i].gameObject.SetActive(false);
                }
            }

            SetCursorLock(true);
        }

        private void OnDisable()
        {
            if (IsControlledLocally)
            {
                SetCursorLock(false);
            }
        }

        private void SetCursorLock(bool locked)
        {
            _isCursorLocked = locked;
            Cursor.lockState = locked ? CursorLockMode.Locked : CursorLockMode.None;
            Cursor.visible = !locked;
        }

        private void Update()
        {
            if (!IsControlledLocally) return;

            // Press Esc or Tab to toggle mouse lock for UI
            if (Input.GetKeyDown(KeyCode.Escape) || Input.GetKeyDown(KeyCode.Tab))
            {
                SetCursorLock(!_isCursorLocked);
            }

            // Click world to lock mouse back
            if (!_isCursorLocked && Input.GetMouseButtonDown(0))
            {
                if (EventSystem.current == null || !EventSystem.current.IsPointerOverGameObject())
                {
                    SetCursorLock(true);
                }
            }

            if (_isCursorLocked)
            {
                float mouseX = Input.GetAxisRaw("Mouse X") * _mouseSensitivity;
                float mouseY = Input.GetAxisRaw("Mouse Y") * _mouseSensitivity;
                _yawAccumulated += mouseX;
                _viewYaw += mouseX;
                _pitch = Mathf.Clamp(_pitch - mouseY, -_pitchLimit, _pitchLimit);
            }

            _move = new Vector2(Input.GetAxisRaw("Horizontal"), Input.GetAxisRaw("Vertical"));
            _jumpPressed |= Input.GetButtonDown("Jump");
            _jumpHeld = Input.GetButton("Jump");
            _sprint = Input.GetKey(KeyCode.LeftShift);
            _kickPressed |= Input.GetKeyDown(KeyCode.F) || (_isCursorLocked && Input.GetMouseButtonDown(0));
            _ragdollTogglePressed |= Input.GetKeyDown(KeyCode.R);
        }

        private void FixedUpdate()
        {
            if (!IsControlledLocally) return;

            if (_ragdollTogglePressed && _ragdoll != null)
            {
                if (_ragdoll.IsActive)
                {
                    _ragdoll.Recover();
                    if (NetworkClient.active) CmdSyncRagdoll(false, Vector3.zero);
                }
                else
                {
                    _ragdoll.Activate(Vector3.zero);
                    if (NetworkClient.active) CmdSyncRagdoll(true, Vector3.zero);
                }
            }

            bool ragdolled = IsRagdolled;
            if (_wasRagdolled && !ragdolled)
            {
                transform.rotation = Quaternion.Euler(0f, _viewYaw, 0f);
                _previousPosition = _currentPosition = transform.position;
            }

            if (!ragdolled)
            {
                var input = new PlayerInputState
                {
                    Move = _move,
                    YawDelta = _yawAccumulated,
                    JumpPressed = _jumpPressed,
                    JumpHeld = _jumpHeld,
                    Sprint = _sprint,
                };
                _motor.Simulate(input, Time.fixedDeltaTime);

                if (_kickPressed && _kick != null)
                {
                    if (_kick.TryKick(_cameraRig.forward))
                    {
                        if (NetworkClient.active) CmdKick();
                    }
                }

                _previousPosition = _currentPosition;
                _currentPosition = transform.position;
            }

            _yawAccumulated = 0f;
            _jumpPressed = false;
            _kickPressed = false;
            _ragdollTogglePressed = false;
            _wasRagdolled = ragdolled;
        }

        private void LateUpdate()
        {
            if (!IsControlledLocally) return;

            Vector3 eye;
            if (IsRagdolled)
            {
                eye = _ragdoll.Pelvis.position + Vector3.up * RagdollCameraOffset;
            }
            else
            {
                float alpha = Mathf.Clamp01((Time.time - Time.fixedTime) / Time.fixedDeltaTime);
                eye = Vector3.Lerp(_previousPosition, _currentPosition, alpha) + Vector3.up * _eyeHeight;
            }

            if (_cameraRig != null)
            {
                _cameraRig.SetPositionAndRotation(eye, Quaternion.Euler(0f, _viewYaw, 0f));
            }
            if (_cameraPitch != null)
            {
                _cameraPitch.localRotation = Quaternion.Euler(_pitch, 0f, 0f);
            }
        }

        // ==========================================
        // MIRROR NETWORK COMMANDS & OBSERVER RPCS
        // ==========================================

        [Command]
        private void CmdKick()
        {
            RpcKick();
        }

        [ClientRpc(includeOwner = false)]
        private void RpcKick()
        {
            if (_kick != null)
            {
                _kick.PlayKickVisual();
            }
        }

        [Command]
        public void CmdSyncRagdoll(bool active, Vector3 velocity)
        {
            RpcSyncRagdoll(active, velocity);
        }

        [ClientRpc(includeOwner = false)]
        private void RpcSyncRagdoll(bool active, Vector3 velocity)
        {
            if (_ragdoll == null) return;
            if (active)
            {
                _ragdoll.Activate(velocity);
            }
            else
            {
                _ragdoll.Recover();
            }
        }
    }
}
