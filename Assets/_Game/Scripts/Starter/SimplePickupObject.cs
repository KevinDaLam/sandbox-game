using Unity.Netcode;
using UnityEngine;

namespace Starter
{
    /// <summary>
    /// Networked Physics Prop.
    /// In NGO, clients request pickup via [Rpc(SendTo.Server)].
    /// The server coordinates parenting and physics synchronization.
    /// </summary>
    [RequireComponent(typeof(Rigidbody), typeof(NetworkObject))]
    public class SimplePickupObject : NetworkBehaviour
    {
        private Rigidbody _rigidbody;
        private Transform _currentCarrierTransform;
        private bool _isHeld;

        private void Awake()
        {
            _rigidbody = GetComponent<Rigidbody>();
        }

        public void Pickup(ulong carrierNetworkObjectId)
        {
            RequestPickupRpc(carrierNetworkObjectId);
        }

        [Rpc(SendTo.Server)]
        private void RequestPickupRpc(ulong carrierNetworkObjectId)
        {
            if (NetworkManager.Singleton != null && 
                NetworkManager.Singleton.SpawnManager.SpawnedObjects.TryGetValue(carrierNetworkObjectId, out var carrierObj))
            {
                AttachToCarrierClientRpc(carrierNetworkObjectId);
            }
        }

        [Rpc(SendTo.ClientsAndHost)]
        private void AttachToCarrierClientRpc(ulong carrierNetworkObjectId)
        {
            if (NetworkManager.Singleton != null && 
                NetworkManager.Singleton.SpawnManager.SpawnedObjects.TryGetValue(carrierNetworkObjectId, out var carrierObj))
            {
                _currentCarrierTransform = carrierObj.transform;
                _isHeld = true;
                _rigidbody.useGravity = false;
                _rigidbody.linearDamping = 10f;
            }
        }

        public void Drop()
        {
            RequestDropRpc();
        }

        [Rpc(SendTo.Server)]
        private void RequestDropRpc()
        {
            ReleaseCarrierClientRpc();
        }

        [Rpc(SendTo.ClientsAndHost)]
        private void ReleaseCarrierClientRpc()
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
