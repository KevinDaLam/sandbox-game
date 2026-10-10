using FishNet.Object;
using UnityEngine;

namespace Starter
{
    /// <summary>
    /// Minimal Networked Physics Prop (Fish-Net).
    /// Software Engineer Note:
    /// In Fish-Net, clients request actions via [ServerRpc],
    /// and the server synchronizes results across all observers via [ObserversRpc].
    /// </summary>
    [RequireComponent(typeof(Rigidbody), typeof(NetworkObject))]
    public class SimplePickupObject : NetworkBehaviour
    {
        private Rigidbody _rigidbody;

        private void Awake()
        {
            _rigidbody = GetComponent<Rigidbody>();
        }

        public void RequestPickup(NetworkObject carrier)
        {
            if (base.IsServerInitialized)
            {
                PickupOnServer(carrier);
            }
            else
            {
                ServerRequestPickup(carrier);
            }
        }

        [ServerRpc(RequireOwnership = false)]
        private void ServerRequestPickup(NetworkObject carrier)
        {
            PickupOnServer(carrier);
        }

        private void PickupOnServer(NetworkObject carrier)
        {
            _rigidbody.isKinematic = true;
            transform.SetParent(carrier.transform);
            transform.localPosition = new Vector3(0, 0.5f, 1.5f);
            ObserversUpdateHold(carrier);
        }

        [ObserversRpc]
        private void ObserversUpdateHold(NetworkObject carrier)
        {
            _rigidbody.isKinematic = true;
            transform.SetParent(carrier.transform);
            transform.localPosition = new Vector3(0, 0.5f, 1.5f);
        }

        public void Drop()
        {
            if (base.IsServerInitialized)
            {
                DropOnServer();
            }
            else
            {
                ServerDrop();
            }
        }

        [ServerRpc(RequireOwnership = false)]
        private void ServerDrop()
        {
            DropOnServer();
        }

        private void DropOnServer()
        {
            transform.SetParent(null);
            _rigidbody.isKinematic = false;
            ObserversReleaseHold();
        }

        [ObserversRpc]
        private void ObserversReleaseHold()
        {
            transform.SetParent(null);
            _rigidbody.isKinematic = false;
        }
    }
}
