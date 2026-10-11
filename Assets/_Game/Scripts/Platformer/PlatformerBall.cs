using UnityEngine;

namespace Game.Platformer
{
    [RequireComponent(typeof(Rigidbody))]
    public class PlatformerBall : MonoBehaviour
    {
        [SerializeField] private float _knockdownSpeed = 9f;
        [SerializeField] private float _knockdownScale = 0.35f;
        [SerializeField] private float _respawnHeight = -15f;

        private Rigidbody _body;
        private Vector3 _spawnPosition;

        private void Awake()
        {
            _body = GetComponent<Rigidbody>();
            _spawnPosition = transform.position;
        }

        private void FixedUpdate()
        {
            if (_body.position.y < _respawnHeight) Respawn();
        }

        public void Respawn()
        {
            _body.linearVelocity = Vector3.zero;
            _body.angularVelocity = Vector3.zero;
            _body.position = _spawnPosition;
        }

        private void OnCollisionEnter(Collision collision)
        {
            float speed = collision.relativeVelocity.magnitude;
            if (speed < _knockdownSpeed) return;

            var ragdoll = collision.collider.GetComponentInParent<PlatformerRagdoll>();
            if (ragdoll == null) return;

            Vector3 away = ragdoll.transform.position - transform.position;
            away.y = 0f;
            Vector3 direction = (away.normalized + Vector3.up * 0.3f).normalized;
            ragdoll.Activate(direction * (speed * _knockdownScale));
        }
    }
}
