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

            Cursor.lockState = CursorLockMode.Locked;
            Cursor.visible = false;
        }

        private void Update()
        {
            if (!IsOwner) return;

            HandleLook();
            HandleMovement();
            HandleInteraction();
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
