using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Game.Player
{
    public class RagdollController : MonoBehaviour
    {
        [Header("References")]
        [SerializeField] private Animator _animator;
        [SerializeField] private Rigidbody _mainRigidbody;
        [SerializeField] private Collider _mainCollider;

        [Header("Ragdoll Settings")]
        [SerializeField] private float _impactForceThreshold = 9f;
        [SerializeField] private float _autoRecoverTime = 3.5f;

        private readonly List<Rigidbody> _ragdollRigidbodies = new();
        private readonly List<Collider> _ragdollColliders = new();
        private bool _isRagdolled;
        private Coroutine _recoverRoutine;

        public bool IsRagdolled => _isRagdolled;

        private void Awake()
        {
            InitializeRagdollParts();
            SetRagdollActive(false);
        }

        private void InitializeRagdollParts()
        {
            _ragdollRigidbodies.Clear();
            _ragdollColliders.Clear();

            Rigidbody[] rbs = GetComponentsInChildren<Rigidbody>();
            foreach (var rb in rbs)
            {
                if (rb == _mainRigidbody) continue;
                _ragdollRigidbodies.Add(rb);
            }

            Collider[] cols = GetComponentsInChildren<Collider>();
            foreach (var col in cols)
            {
                if (col == _mainCollider) continue;
                _ragdollColliders.Add(col);
            }
        }

        public void SetRagdollActive(bool active)
        {
            _isRagdolled = active;

            if (_animator != null)
            {
                _animator.enabled = !active;
            }

            if (_mainRigidbody != null)
            {
                _mainRigidbody.isKinematic = active;
            }

            if (_mainCollider != null)
            {
                _mainCollider.enabled = !active;
            }

            foreach (var rb in _ragdollRigidbodies)
            {
                rb.isKinematic = !active;
            }

            foreach (var col in _ragdollColliders)
            {
                col.enabled = active;
            }

            if (active && _autoRecoverTime > 0)
            {
                if (_recoverRoutine != null) StopCoroutine(_recoverRoutine);
                _recoverRoutine = StartCoroutine(AutoRecoverRoutine());
            }
        }

        public void TriggerRagdollWithForce(Vector3 impactVelocity)
        {
            SetRagdollActive(true);
            foreach (var rb in _ragdollRigidbodies)
            {
                rb.linearVelocity = impactVelocity + Random.insideUnitSphere * 2f;
            }
        }

        private IEnumerator AutoRecoverRoutine()
        {
            yield return new WaitForSeconds(_autoRecoverTime);
            SetRagdollActive(false);
            _recoverRoutine = null;
        }

        private void OnCollisionEnter(Collision collision)
        {
            if (!_isRagdolled && collision.relativeVelocity.magnitude > _impactForceThreshold)
            {
                TriggerRagdollWithForce(collision.relativeVelocity * 0.5f);
            }
        }
    }
}
