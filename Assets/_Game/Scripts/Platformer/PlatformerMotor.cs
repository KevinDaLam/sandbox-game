using UnityEngine;

namespace Game.Platformer
{
    [RequireComponent(typeof(CharacterController))]
    public class PlatformerMotor : MonoBehaviour
    {
        [Header("Ground")]
        [SerializeField] private float _walkSpeed = 6f;
        [SerializeField] private float _sprintSpeed = 9f;
        [SerializeField] private float _groundAcceleration = 60f;
        [SerializeField] private float _airAcceleration = 30f;

        [Header("Jump")]
        [SerializeField] private float _jumpHeight = 2.2f;
        [SerializeField] private float _coyoteTime = 0.1f;
        [SerializeField] private float _jumpBufferTime = 0.12f;

        [Header("Gravity")]
        [SerializeField] private float _gravity = -30f;
        [SerializeField] private float _fallGravityMultiplier = 1.5f;
        [SerializeField] private float _jumpCutGravityMultiplier = 2.5f;
        [SerializeField] private float _maxFallSpeed = 30f;

        private CharacterController _controller;
        private Vector3 _velocity;
        private float _coyoteTimer;
        private float _jumpBufferTimer;

        public bool IsGrounded { get; private set; }
        public Vector3 Velocity => _velocity;

        private void Awake() => _controller = GetComponent<CharacterController>();

        public void Simulate(in PlayerInputState input, float dt)
        {
            transform.Rotate(0f, input.YawDelta, 0f);

            _coyoteTimer = IsGrounded ? _coyoteTime : _coyoteTimer - dt;
            _jumpBufferTimer = input.JumpPressed ? _jumpBufferTime : _jumpBufferTimer - dt;

            Vector3 wish = transform.right * input.Move.x + transform.forward * input.Move.y;
            if (wish.sqrMagnitude > 1f) wish.Normalize();
            wish *= input.Sprint ? _sprintSpeed : _walkSpeed;

            var horizontal = new Vector3(_velocity.x, 0f, _velocity.z);
            float acceleration = IsGrounded ? _groundAcceleration : _airAcceleration;
            horizontal = Vector3.MoveTowards(horizontal, wish, acceleration * dt);

            float vertical = _velocity.y;
            if (IsGrounded && vertical < 0f) vertical = -2f;

            if (_jumpBufferTimer > 0f && _coyoteTimer > 0f)
            {
                vertical = Mathf.Sqrt(2f * _jumpHeight * -_gravity);
                _jumpBufferTimer = 0f;
                _coyoteTimer = 0f;
            }

            float gravity = _gravity;
            if (vertical < 0f) gravity *= _fallGravityMultiplier;
            else if (!input.JumpHeld) gravity *= _jumpCutGravityMultiplier;
            vertical = Mathf.Max(vertical + gravity * dt, -_maxFallSpeed);

            _velocity = horizontal + Vector3.up * vertical;
            CollisionFlags flags = _controller.Move(_velocity * dt);

            IsGrounded = (flags & CollisionFlags.Below) != 0;
            if ((flags & CollisionFlags.Above) != 0 && _velocity.y > 0f) _velocity.y = 0f;
        }

        public void Teleport(Vector3 position)
        {
            _controller.enabled = false;
            transform.position = position;
            _controller.enabled = true;
            _velocity = Vector3.zero;
        }
    }
}
