using System.Collections.Generic;
using UnityEngine;

namespace Game.Platformer
{
    public class PlayerKick : MonoBehaviour
    {
        [SerializeField] private float _reach = 1.1f;
        [SerializeField] private float _radius = 0.9f;
        [SerializeField] private float _impulse = 6f;
        [SerializeField] private float _lift = 0.35f;
        [SerializeField] private float _cooldown = 0.4f;
        [SerializeField] private float _ragdollKnockbackScale = 0.6f;
        [SerializeField] private Transform _kickLeg;
        [SerializeField] private float _swingDuration = 0.2f;
        [SerializeField] private float _swingAngle = 85f;

        private readonly HashSet<Object> _handled = new();
        private Quaternion _legRest;
        private float _nextKickTime;
        private float _swingStart = -1f;

        private void Awake()
        {
            if (_kickLeg != null) _legRest = _kickLeg.localRotation;
        }

        public bool TryKick(Vector3 facing)
        {
            if (Time.time < _nextKickTime) return false;
            _nextKickTime = Time.time + _cooldown;
            _swingStart = Time.time;

            facing.y = 0f;
            facing = facing.sqrMagnitude > 1e-4f ? facing.normalized : transform.forward;
            Vector3 direction = (facing + Vector3.up * _lift).normalized;
            Vector3 center = transform.position + Vector3.up * 0.6f + facing * _reach;

            _handled.Clear();
            foreach (var hit in Physics.OverlapSphere(center, _radius, ~0, QueryTriggerInteraction.Ignore))
            {
                if (hit.transform.IsChildOf(transform)) continue;

                var ragdoll = hit.GetComponentInParent<PlatformerRagdoll>();
                if (ragdoll != null)
                {
                    if (_handled.Add(ragdoll)) ragdoll.Activate(direction * (_impulse * _ragdollKnockbackScale));
                    continue;
                }

                var body = hit.attachedRigidbody;
                if (body != null && !body.isKinematic && _handled.Add(body))
                    body.AddForce(direction * _impulse, ForceMode.Impulse);
            }
            return true;
        }

        private void LateUpdate()
        {
            if (_kickLeg == null || _swingStart < 0f) return;

            float t = (Time.time - _swingStart) / _swingDuration;
            if (t >= 1f)
            {
                _kickLeg.localRotation = _legRest;
                _swingStart = -1f;
                return;
            }
            _kickLeg.localRotation = _legRest * Quaternion.Euler(-_swingAngle * Mathf.Sin(t * Mathf.PI), 0f, 0f);
        }
    }
}
