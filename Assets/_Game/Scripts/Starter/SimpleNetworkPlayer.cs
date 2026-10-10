using UnityEngine;

namespace Starter
{
    /// <summary>
    /// Minimal First-Person Player Controller.
    /// Software Engineer Note:
    /// In local proof-of-concept, controls character movement and camera pitch.
    /// When multiplayer (Fish-Net) is connected, checks 'IsOwner' before reading input.
    /// </summary>
    [RequireComponent(typeof(CharacterController))]
    public class SimpleNetworkPlayer : MonoBehaviour
    {
        [Header("Movement")]
        [SerializeField] private float _moveSpeed = 5f;
        [SerializeField] private float _mouseSensitivity = 2f;
        [SerializeField] private float _gravity = -9.81f;

        [Header("Camera")]
        [SerializeField] private Camera _playerCamera;

        [Header("Interaction")]
        [SerializeField] private float _interactionRange = 3f;
        [SerializeField] private Transform _holdPoint;

        private CharacterController _characterController;
        private float _verticalLookRotation;
        private float _verticalVelocity;
        private SimplePickupObject _heldObject;

        private void Awake()
        {
            _characterController = GetComponent<CharacterController>();
        }

        private void Start()
        {
            Cursor.lockState = CursorLockMode.Locked;
            Cursor.visible = false;
        }

        private void Update()
        {
            HandleLook();
            HandleMovement();
            HandleInteraction();
        }

        private void HandleLook()
        {
            float mouseX = Input.GetAxisRaw("Mouse X") * _mouseSensitivity;
            float mouseY = Input.GetAxisRaw("Mouse Y") * _mouseSensitivity;

            // Turn player body horizontally
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
            // Press E to pick up or drop object
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
                            Transform hold = _holdPoint != null ? _holdPoint : _playerCamera.transform;
                            pickup.Pickup(hold);
                            _heldObject = pickup;
                        }
                    }
                }
            }
        }
    }
}
