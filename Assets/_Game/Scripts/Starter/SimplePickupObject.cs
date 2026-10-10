using UnityEngine;

namespace Starter
{
    /// <summary>
    /// Minimal Physics Prop for Proof-of-Concept.
    /// Can be picked up, carried in front of the player, and dropped with physics momentum.
    /// </summary>
    [RequireComponent(typeof(Rigidbody))]
    public class SimplePickupObject : MonoBehaviour
    {
        private Rigidbody _rigidbody;
        private Transform _currentHoldPoint;
        private bool _isHeld;

        private void Awake()
        {
            _rigidbody = GetComponent<Rigidbody>();
        }

        public void Pickup(Transform holdPoint)
        {
            _currentHoldPoint = holdPoint;
            _isHeld = true;
            _rigidbody.useGravity = false;
            _rigidbody.linearDamping = 10f;
        }

        public void Drop()
        {
            _isHeld = false;
            _currentHoldPoint = null;
            _rigidbody.useGravity = true;
            _rigidbody.linearDamping = 0.05f;
            _rigidbody.linearVelocity = Vector3.forward * 2f;
        }

        private void FixedUpdate()
        {
            if (_isHeld && _currentHoldPoint != null)
            {
                // Smooth physics pull to target point without clipping through walls
                Vector3 targetPos = _currentHoldPoint.position + _currentHoldPoint.forward * 1.5f;
                Vector3 delta = targetPos - transform.position;
                _rigidbody.linearVelocity = delta * 12f;
            }
        }
    }
}
