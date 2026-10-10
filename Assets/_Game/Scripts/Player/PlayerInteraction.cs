using Game.Interactions;
using UnityEngine;

namespace Game.Player
{
    public class PlayerInteraction : MonoBehaviour
    {
        [Header("Interaction Settings")]
        [SerializeField] private float _interactRange = 3f;
        [SerializeField] private LayerMask _interactLayerMask = ~0;
        [SerializeField] private Transform _holdPoint;
        [SerializeField] private float _baseThrowForce = 12f;

        [Header("References")]
        [SerializeField] private Camera _playerCamera;
        [SerializeField] private Rigidbody _playerRigidbody;

        private IInteractable _currentLookedInteractable;
        private IInteractable _currentlyHeldItem;

        public IInteractable CurrentLookedInteractable => _currentLookedInteractable;
        public IInteractable CurrentlyHeldItem => _currentlyHeldItem;
        public bool IsHoldingItem => _currentlyHeldItem != null;

        private void Update()
        {
            UpdateLookTarget();
        }

        private void UpdateLookTarget()
        {
            if (_playerCamera == null) return;

            Ray ray = new Ray(_playerCamera.transform.position, _playerCamera.transform.forward);
            if (Physics.Raycast(ray, out RaycastHit hit, _interactRange, _interactLayerMask))
            {
                if (hit.collider.TryGetComponent<IInteractable>(out var interactable))
                {
                    _currentLookedInteractable = interactable;
                    return;
                }
            }

            _currentLookedInteractable = null;
        }

        public void TryInteract()
        {
            if (_currentLookedInteractable != null)
            {
                _currentLookedInteractable.OnInteract(gameObject);
            }
        }

        public void TryGrabOrDrop()
        {
            if (_currentlyHeldItem != null)
            {
                DropHeldItem();
            }
            else if (_currentLookedInteractable != null && _currentLookedInteractable.CanBeGrabbed)
            {
                GrabItem(_currentLookedInteractable);
            }
        }

        public void TryThrow(float forceMultiplier = 1f)
        {
            if (_currentlyHeldItem == null || _playerCamera == null) return;

            Vector3 throwDirection = (_playerCamera.transform.forward + Vector3.up * 0.12f).normalized;
            Vector3 throwVelocity = throwDirection * (_baseThrowForce * forceMultiplier);
            
            if (_playerRigidbody != null)
            {
                throwVelocity += _playerRigidbody.linearVelocity * 0.5f;
            }

            var item = _currentlyHeldItem;
            _currentlyHeldItem = null;
            item.OnRelease(throwVelocity);
        }

        private void GrabItem(IInteractable item)
        {
            _currentlyHeldItem = item;
            _currentlyHeldItem.OnGrab(_holdPoint != null ? _holdPoint : transform, _playerRigidbody);
        }

        private void DropHeldItem()
        {
            if (_currentlyHeldItem == null) return;

            var item = _currentlyHeldItem;
            _currentlyHeldItem = null;
            item.OnRelease(Vector3.down * 0.5f);
        }
    }
}
