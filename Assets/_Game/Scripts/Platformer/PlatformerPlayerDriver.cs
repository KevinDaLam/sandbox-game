using UnityEngine;

namespace Game.Platformer
{
    public class PlatformerPlayerDriver : MonoBehaviour
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

        private bool IsRagdolled => _ragdoll != null && _ragdoll.IsActive;

        private void OnEnable()
        {
            _viewYaw = transform.eulerAngles.y;
            _previousPosition = _currentPosition = transform.position;
            Cursor.lockState = CursorLockMode.Locked;
            Cursor.visible = false;
        }

        private void Update()
        {
            float mouseX = Input.GetAxisRaw("Mouse X") * _mouseSensitivity;
            float mouseY = Input.GetAxisRaw("Mouse Y") * _mouseSensitivity;
            _yawAccumulated += mouseX;
            _viewYaw += mouseX;
            _pitch = Mathf.Clamp(_pitch - mouseY, -_pitchLimit, _pitchLimit);

            _move = new Vector2(Input.GetAxisRaw("Horizontal"), Input.GetAxisRaw("Vertical"));
            _jumpPressed |= Input.GetButtonDown("Jump");
            _jumpHeld = Input.GetButton("Jump");
            _sprint = Input.GetKey(KeyCode.LeftShift);
            _kickPressed |= Input.GetKeyDown(KeyCode.F) || Input.GetMouseButtonDown(0);
            _ragdollTogglePressed |= Input.GetKeyDown(KeyCode.R);
        }

        private void FixedUpdate()
        {
            if (_ragdollTogglePressed && _ragdoll != null)
            {
                if (_ragdoll.IsActive) _ragdoll.Recover();
                else _ragdoll.Activate(Vector3.zero);
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

                if (_kickPressed && _kick != null) _kick.TryKick(_cameraRig.forward);

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

            _cameraRig.SetPositionAndRotation(eye, Quaternion.Euler(0f, _viewYaw, 0f));
            _cameraPitch.localRotation = Quaternion.Euler(_pitch, 0f, 0f);
        }
    }
}
