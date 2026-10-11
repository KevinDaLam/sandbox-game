using Unity.Netcode;
using UnityEngine;

namespace Starter
{
    /// <summary>
    /// Networked First-Person Player Controller (Netcode for GameObjects).
    /// Software Engineer Note:
    /// In NGO, every connected client has a copy of all players.
    /// 'IsOwner' is true ONLY for the local machine controlling this character.
    /// </summary>
    [RequireComponent(typeof(CharacterController))]
    public class SimpleNetworkPlayer : NetworkBehaviour
    {
        [Header("Movement")]
        [SerializeField] private float _moveSpeed = 5f;
        [SerializeField] private float _mouseSensitivity = 2f;
        [SerializeField] private float _gravity = -9.81f;

        [Header("Camera")]
        [SerializeField] private Camera _playerCamera;

        [Header("Interaction")]
        [SerializeField] private float _interactionRange = 3f;

        private CharacterController _characterController;
        private float _verticalLookRotation;
        private float _verticalVelocity;
        private SimplePickupObject _heldObject;
        private bool _isCursorLocked = true;

        private void Awake()
        {
            _characterController = GetComponent<CharacterController>();
        }

        public override void OnNetworkSpawn()
        {
            // CRITICAL: Disable camera and controls for other players so we don't control each other!
            if (!IsOwner)
            {
                if (_playerCamera != null)
                {
                    _playerCamera.gameObject.SetActive(false);
                }
                enabled = false;
                return;
            }

            SetCursorLock(true);
        }

        private void OnDisable()
        {
            if (IsOwner)
            {
                SetCursorLock(false);
            }
        }

        private void OnDestroy()
        {
            if (IsOwner)
            {
                SetCursorLock(false);
            }
        }

        private void Update()
        {
            if (!IsOwner) return;

            // Press Escape or Tab to toggle mouse lock and click on-screen UI buttons!
            if (Input.GetKeyDown(KeyCode.Escape) || Input.GetKeyDown(KeyCode.Tab))
            {
                SetCursorLock(!_isCursorLocked);
            }

            // If cursor is unlocked and player clicks the game screen, lock it back
            if (!_isCursorLocked && Input.GetMouseButtonDown(0))
            {
                // Only lock back if not clicking on the top-left HUD area
                if (Input.mousePosition.x > 380 || Input.mousePosition.y < (Screen.height - 480))
                {
                    SetCursorLock(true);
                }
            }

            if (_isCursorLocked)
            {
                HandleLook();
            }

            HandleMovement();
            HandleInteraction();
        }

        private void SetCursorLock(bool locked)
        {
            _isCursorLocked = locked;
            Cursor.lockState = locked ? CursorLockMode.Locked : CursorLockMode.None;
            Cursor.visible = !locked;
        }

        private void HandleLook()
        {
            float mouseX = Input.GetAxisRaw("Mouse X") * _mouseSensitivity;
            float mouseY = Input.GetAxisRaw("Mouse Y") * _mouseSensitivity;

            // Turn player horizontally
            transform.Rotate(Vector3.up * mouseX);

            // Pitch camera vertically
            if (_playerCamera != null)
            {
                _verticalLookRotation -= mouseY;
                _verticalLookRotation = Mathf.Clamp(_verticalLookRotation, -80f, 80f);
                _playerCamera.transform.localRotation = Quaternion.Euler(_verticalLookRotation, 0f, 0f);
            }
        }

        private void HandleMovement()
        {
            if (_characterController.isGrounded && _verticalVelocity < 0)
            {
                _verticalVelocity = -2f;
            }

            float h = Input.GetAxisRaw("Horizontal");
            float v = Input.GetAxisRaw("Vertical");
            Vector3 moveDirection = (transform.right * h + transform.forward * v).normalized;

            _characterController.Move(moveDirection * (_moveSpeed * Time.deltaTime));

            _verticalVelocity += _gravity * Time.deltaTime;
            _characterController.Move(Vector3.up * (_verticalVelocity * Time.deltaTime));
        }

        private void HandleInteraction()
        {
            if (Input.GetKeyDown(KeyCode.E))
            {
                if (_heldObject != null)
                {
                    _heldObject.Drop();
                    _heldObject = null;
                }
                else if (_playerCamera != null)
                {
                    Ray ray = new Ray(_playerCamera.transform.position, _playerCamera.transform.forward);
                    if (Physics.Raycast(ray, out RaycastHit hit, _interactionRange))
                    {
                        if (hit.collider.TryGetComponent<SimplePickupObject>(out var pickup))
                        {
                            pickup.Pickup(NetworkObjectId);
                            _heldObject = pickup;
                        }
                    }
                }
            }
        }
    }
}
