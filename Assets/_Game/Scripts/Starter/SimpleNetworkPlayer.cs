using FishNet.Object;
using UnityEngine;

namespace Starter
{
    /// <summary>
    /// Minimal First-Person Networked Player Controller (Fish-Net).
    /// Software Engineer Note:
    /// In Fish-Net, every spawned player exists on every machine.
    /// 'base.IsOwner' is true ONLY for the local machine that controls this character.
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

        private CharacterController _characterController;
        private float _verticalLookRotation;
        private float _verticalVelocity;

        private void Awake()
        {
            _characterController = GetComponent<CharacterController>();
        }

        public override void OnStartClient()
        {
            base.OnStartClient();

            // CRITICAL NETWORKING RULE:
            // If this player instance is NOT owned by this local computer,
            // disable its camera and movement script so we don't control each other!
            if (!base.IsOwner)
            {
                if (_playerCamera != null)
                {
                    _playerCamera.gameObject.SetActive(false);
                }
                enabled = false;
                return;
            }

            // Lock cursor for local first-person control
            Cursor.lockState = CursorLockMode.Locked;
            Cursor.visible = false;
        }

        private void Update()
        {
            if (!base.IsOwner) return;

            HandleLook();
            HandleMovement();
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
    }
}
