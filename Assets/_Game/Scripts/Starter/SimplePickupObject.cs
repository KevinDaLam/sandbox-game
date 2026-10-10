using Unity.Netcode;
using UnityEngine;

namespace Starter
{
    /// <summary>
    /// Minimal Networked Physics Prop.
    /// Software Engineer Note:
    /// In server-authoritative networking, clients request physics ownership/actions
    /// via [ServerRpc], and the server synchronizes the result via NetworkVariables or NetworkTransform.
    /// </summary>
    [RequireComponent(typeof(Rigidbody), typeof(NetworkObject))]
    public class SimplePickupObject : NetworkBehaviour
    {
        private Rigidbody _rigidbody;
        private Transform _carrierHoldPoint;

        private void Awake()
        {
            _rigidbody = GetComponent<Rigidbody>();
        }

        /// <summary>
        /// Called when a player presses 'E' while looking at this object.
        /// </summary>
        public void RequestPickup(Transform holdPoint)
        {
            if (IsServer)
            {
                PickupOnServer(holdPoint);
            }
            else
            {
                RequestPickupServerRpc(NetworkManager.Singleton.LocalClientId);
            }
        }

        [Rpc(SendTo.Server)]
        private void RequestPickupServerRpc(ulong requestingClientId)
        {
            if (NetworkManager.Singleton.ConnectedClients.TryGetValue(requestingClientId, out var client))
            {
                // Find hold point on client player
                Transform hold = client.PlayerObject.transform;
                PickupOnServer(hold);
            }
        }

        private void PickupOnServer(Transform holdPoint)
        {
            _carrierHoldPoint = holdPoint;
            _rigidbody.isKinematic = true; // Disable physics simulation while carried
            transform.SetParent(holdPoint);
            transform.localPosition = new Vector3(0, 0.5f, 1.5f); // Hold in front
        }

        public void Drop()
        {
            if (IsServer)
            {
                DropOnServer();
            }
            else
            {
                DropServerRpc();
            }
        }

        [Rpc(SendTo.Server)]
        private void DropServerRpc()
        {
            DropOnServer();
        }

        private void DropOnServer()
        {
            _carrierHoldPoint = null;
            transform.SetParent(null);
            _rigidbody.isKinematic = false;
        }
    }
}
