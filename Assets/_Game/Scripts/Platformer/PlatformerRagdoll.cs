using System.Collections.Generic;
using UnityEngine;

namespace Game.Platformer
{
    [RequireComponent(typeof(PlatformerMotor), typeof(CharacterController))]
    public class PlatformerRagdoll : MonoBehaviour
    {
        [SerializeField] private Rigidbody _pelvis;
        [SerializeField] private float _autoRecoverTime = 3.5f;
        [SerializeField] private float _standingPelvisHeight = 0.95f;

        private struct Bone
        {
            public Rigidbody Body;
            public Collider[] Colliders;
            public Vector3 LocalPosition;
            public Quaternion LocalRotation;
        }

        private readonly List<Bone> _bones = new();
        private PlatformerMotor _motor;
        private CharacterController _controller;
        private float _activatedAt;

        public bool IsActive { get; private set; }
        public Transform Pelvis => _pelvis.transform;

        private void Awake()
        {
            _motor = GetComponent<PlatformerMotor>();
            _controller = GetComponent<CharacterController>();

            foreach (var body in GetComponentsInChildren<Rigidbody>())
            {
                _bones.Add(new Bone
                {
                    Body = body,
                    Colliders = body.GetComponents<Collider>(),
                    LocalPosition = body.transform.localPosition,
                    LocalRotation = body.transform.localRotation,
                });
            }

            SetBonesSimulated(false, Vector3.zero);
        }

        private void Update()
        {
            if (IsActive && _autoRecoverTime > 0f && Time.time - _activatedAt > _autoRecoverTime)
                Recover();
        }

        public void Activate(Vector3 velocityChange)
        {
            if (IsActive) return;
            IsActive = true;
            _activatedAt = Time.time;

            Vector3 inherited = _motor.Velocity;
            _controller.enabled = false;
            SetBonesSimulated(true, inherited + velocityChange);
        }

        public void Recover()
        {
            if (!IsActive) return;

            Vector3 pelvisPosition = _pelvis.position;
            SetBonesSimulated(false, Vector3.zero);
            foreach (var bone in _bones)
                bone.Body.transform.SetLocalPositionAndRotation(bone.LocalPosition, bone.LocalRotation);

            float groundY = pelvisPosition.y - _standingPelvisHeight;
            if (Physics.Raycast(pelvisPosition + Vector3.up * 0.5f, Vector3.down, out RaycastHit hit, 4f, ~0, QueryTriggerInteraction.Ignore))
                groundY = hit.point.y;

            _motor.Teleport(new Vector3(pelvisPosition.x, groundY + 0.05f, pelvisPosition.z));
            IsActive = false;
        }

        private void SetBonesSimulated(bool simulated, Vector3 velocity)
        {
            foreach (var bone in _bones)
            {
                foreach (var collider in bone.Colliders) collider.enabled = simulated;

                if (simulated)
                {
                    bone.Body.isKinematic = false;
                    bone.Body.linearVelocity = velocity;
                    bone.Body.angularVelocity = Vector3.zero;
                }
                else
                {
                    if (!bone.Body.isKinematic)
                    {
                        bone.Body.linearVelocity = Vector3.zero;
                        bone.Body.angularVelocity = Vector3.zero;
                    }
                    bone.Body.isKinematic = true;
                }
            }
        }
    }
}
