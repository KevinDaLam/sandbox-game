using UnityEngine;

namespace Game.Interactions
{
    [RequireComponent(typeof(Rigidbody))]
    public class PhysicsProp : MonoBehaviour, IInteractable
    {
        [SerializeField] private string _prompt = "Pick up Item";
        [SerializeField] private bool _canBeGrabbed = true;
        [SerializeField] private float _dragWhenHeld = 10f;

        private Rigidbody _rigidbody;
        private Transform _currentHoldPoint;
        private float _originalDrag;
        private bool _isHeld;

        public string InteractionPrompt => _prompt;
        public bool CanBeGrabbed => _canBeGrabbed;
        public bool IsHeld => _isHeld;

        private void Awake()
        {
            _rigidbody = GetComponent<Rigidbody>();
            _originalDrag = _rigidbody.linearDamping;
        }

        public void OnInteract(GameObject interactor)
        {
            // Default interaction: highlight or inspect
        }

        public void OnGrab(Transform holdPoint, Rigidbody interactorBody)
        {
            _currentHoldPoint = holdPoint;
            _isHeld = true;
            _rigidbody.linearDamping = _dragWhenHeld;
            _rigidbody.useGravity = false;
        }

        public void OnRelease(Vector3 throwVelocity)
        {
            _currentHoldPoint = null;
            _isHeld = false;
            _rigidbody.linearDamping = _originalDrag;
            _rigidbody.useGravity = true;
            _rigidbody.linearVelocity = throwVelocity;
            _rigidbody.AddTorque(Random.insideUnitSphere * 8f, ForceMode.Impulse);
        }

        private void FixedUpdate()
        {
            if (_isHeld && _currentHoldPoint != null)
            {
                // Smooth physics pull towards hold point without clipping walls
                Vector3 targetDelta = _currentHoldPoint.position - transform.position;
                _rigidbody.linearVelocity = targetDelta * 14f;
            }
        }
    }
}
