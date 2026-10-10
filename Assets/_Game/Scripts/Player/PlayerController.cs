using UnityEngine;

namespace Game.Player
{
    [RequireComponent(typeof(CharacterController))]
    public class PlayerController : MonoBehaviour
    {
        [Header("Movement Settings")]
        [SerializeField] private float _walkSpeed = 4.5f;
        [SerializeField] private float _sprintSpeed = 7.5f;
        [SerializeField] private float _jumpHeight = 1.3f;
        [SerializeField] private float _gravity = -18f;

        [Header("Look Settings")]
        [SerializeField] private Transform _cameraTransform;
        [SerializeField] private float _mouseSensitivity = 2f;
        [SerializeField] private float _verticalLookLimit = 85f;

        [Header("Components")]
        [SerializeField] private CharacterController _characterController;
        [SerializeField] private PlayerInteraction _interaction;
        [SerializeField] private RagdollController _ragdoll;

        private Vector3 _velocity;
        private float _verticalRotation;
        private bool _isGrounded;

        private void Awake()
        {
            if (_characterController == null)
                _characterController = GetComponent<CharacterController>();
            if (_interaction == null)
                _interaction = GetComponent<PlayerInteraction>();
            if (_ragdoll == null)
                _ragdoll = GetComponent<RagdollController>();
        }

        private void Start()
        {
            Cursor.lockState = CursorLockMode.Locked;
            Cursor.visible = false;
        }

        private void Update()
        {
            if (_ragdoll != null && _ragdoll.IsRagdolled) return;

            HandleLook();
            HandleMovement();
            HandleInputs();
        }

        private void HandleLook()
        {
            float mouseX = Input.GetAxisRaw("Mouse X") * _mouseSensitivity;
            float mouseY = Input.GetAxisRaw("Mouse Y") * _mouseSensitivity;

            // Horizontal rotation on player body
            transform.Rotate(Vector3.up * mouseX);

            // Vertical pitch on camera
            _verticalRotation -= mouseY;
            _verticalRotation = Mathf.Clamp(_verticalRotation, -_verticalLookLimit, _verticalLookLimit);

            if (_cameraTransform != null)
            {
                _cameraTransform.localRotation = Quaternion.Euler(_verticalRotation, 0f, 0f);
            }
        }

        private void HandleMovement()
        {
            _isGrounded = _characterController.isGrounded;
            if (_isGrounded && _velocity.y < 0)
            {
                _velocity.y = -2f;
            }

            float horizontal = Input.GetAxisRaw("Horizontal");
            float vertical = Input.GetAxisRaw("Vertical");
            Vector3 inputDir = (transform.right * horizontal + transform.forward * vertical).normalized;

            bool isSprinting = Input.GetKey(KeyCode.LeftShift);
            float targetSpeed = isSprinting ? _sprintSpeed : _walkSpeed;

            _characterController.Move(inputDir * (targetSpeed * Time.deltaTime));

            // Jump
            if (Input.GetButtonDown("Jump") && _isGrounded)
            {
                _velocity.y = Mathf.Sqrt(_jumpHeight * -2f * _gravity);
            }

            // Gravity
            _velocity.y += _gravity * Time.deltaTime;
            _characterController.Move(_velocity * Time.deltaTime);
        }

        private void HandleInputs()
        {
            if (_interaction == null) return;

            // Interact (E)
            if (Input.GetKeyDown(KeyCode.E))
            {
                _interaction.TryInteract();
            }

            // Grab / Drop (G or Left Mouse)
            if (Input.GetKeyDown(KeyCode.G) || Input.GetMouseButtonDown(0))
            {
                _interaction.TryGrabOrDrop();
            }

            // Throw (Right Mouse)
            if (Input.GetMouseButtonDown(1))
            {
                _interaction.TryThrow();
            }

            // Ragdoll on demand / Comedic flop (R)
            if (Input.GetKeyDown(KeyCode.R) && _ragdoll != null)
            {
                _ragdoll.TriggerRagdollWithForce(transform.forward * 3f);
            }
        }
    }
}
