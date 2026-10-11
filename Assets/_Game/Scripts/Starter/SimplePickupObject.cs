using Mirror;
using UnityEngine;

namespace Starter
{
    /// <summary>
    /// Networked Physics Prop (Mirror).
    /// Clients request pickup via [Command(requiresAuthority = false)].
    /// The server coordinates parenting and physics synchronization via [ClientRpc].
    /// </summary>
    [RequireComponent(typeof(Rigidbody), typeof(NetworkIdentity))]
    public class SimplePickupObject : NetworkBehaviour
    {
        private Rigidbody _rigidbody;
        private Transform _currentCarrierTransform;
        private bool _isHeld;

        private void Awake()
        {
            _rigidbody = GetComponent<Rigidbody>();
        }

        public void Pickup(uint carrierNetId)
        {
            CmdRequestPickup(carrierNetId);
        }

        [Command(requiresAuthority = false)]
        private void CmdRequestPickup(uint carrierNetId)
        {
            if (NetworkServer.spawned.TryGetValue(carrierNetId, out NetworkIdentity carrierIdentity))
            {
                RpcAttachToCarrier(carrierNetId);
            }
        }

        [ClientRpc]
        private void RpcAttachToCarrier(uint carrierNetId)
        {
            if (NetworkClient.spawned.TryGetValue(carrierNetId, out NetworkIdentity carrierIdentity))
            {
                _currentCarrierTransform = carrierIdentity.transform;
                _isHeld = true;
                _rigidbody.useGravity = false;
                _rigidbody.linearDamping = 10f;
            }
        }

        public void Drop()
        {
            CmdRequestDrop();
        }

        [Command(requiresAuthority = false)]
        private void CmdRequestDrop()
        {
            RpcReleaseCarrier();
        }

        [ClientRpc]
        private void RpcReleaseCarrier()
        {
            _isHeld = false;
            _currentCarrierTransform = null;
            _rigidbody.useGravity = true;
            _rigidbody.linearDamping = 0.05f;
            _rigidbody.linearVelocity = Vector3.forward * 2f;
        }

        private void FixedUpdate()
        {
            if (_isHeld && _currentCarrierTransform != null)
            {
                Vector3 targetPos = _currentCarrierTransform.position + _currentCarrierTransform.forward * 1.5f + Vector3.up * 0.5f;
                Vector3 delta = targetPos - transform.position;
                _rigidbody.linearVelocity = delta * 12f;
            }
        }
    }
}
