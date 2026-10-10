# Fish-Net + FishySteamworks Multiplayer Foundation

Architecture and synchronization guide for 1-4+ player co-op friendslop sessions using **Fish-Net** (`FishNet.Runtime`) and **FishySteamworks** (Steam P2P SDR).

---

## 1. Network Topology: Host-Client via Steam P2P

* **Host**: One player acts as both server and local player.
* **Clients**: Friends connect via Steam lobby invite, friends list join, or direct SteamID.
* **Transports**:
  * **Tugboat (Default)**: Used for local prototyping (`127.0.0.1`) without requiring Steam.
  * **FishySteamworks**: Used for production Steam release (NAT punchthrough, encrypted relays, 0 port forwarding).

---

## 2. Core Fish-Net Components

Component | Purpose
:--- | :---
`NetworkManager` | Central networking orchestrator managing transports, server, and client connections.
`Tugboat` / `FishySteamworks` | The socket transport layer (Local UDP vs Steam SDR).
`PlayerSpawner` | Automatically spawns the player prefab when a client connects.
`NetworkObject` | Placed on prefabs that exist across the network (Players, Props, Doors).
`NetworkTransform` | Interpolates and synchronizes position/rotation with built-in client prediction.
`NetworkRigidbody` | Synchronizes active physics bodies (thrown items, ragdolls).

---

## 3. Essential Fish-Net Friendslop Patterns

### Synchronized Player State (`[SyncVar]`)
```csharp
using FishNet.Object;
using FishNet.Object.Synchronizing;

public class NetworkPlayerState : NetworkBehaviour
{
    // [SyncVar] automatically replicates state from server to clients
    [SyncVar(OnChange = nameof(OnHealthChanged))]
    public int Health = 100;

    [SyncVar(OnChange = nameof(OnDownedChanged))]
    public bool IsDowned = false;

    private void OnHealthChanged(int prev, int next, bool asServer) { /* Update UI */ }
    private void OnDownedChanged(bool prev, bool next, bool asServer) { /* Trigger ragdoll */ }
}
```

### Server-Authoritative Interaction RPCs (`[ServerRpc]`)
```csharp
using FishNet.Object;

public class PlayerInteraction : NetworkBehaviour
{
    // Client calls this, executes on Server/Host
    [ServerRpc(RequireOwnership = false)]
    public void RequestGrabItemServerRpc(NetworkObject targetItem)
    {
        if (targetItem == null) return;

        // Server validates distance
        if (Vector3.Distance(transform.position, targetItem.transform.position) < 3.5f)
        {
            targetItem.transform.SetParent(transform);
            ObserversAttachItem(targetItem);
        }
    }

    [ObserversRpc]
    private void ObserversAttachItem(NetworkObject targetItem)
    {
        targetItem.transform.SetParent(transform);
    }
}
```

### Comedic Visual & Sound Broadcasts (`[ObserversRpc]`)
```csharp
using FishNet.Object;

public class SlapMechanic : NetworkBehaviour
{
    [ObserversRpc]
    public void PlaySlapEffectObserversRpc(Vector3 impactPoint)
    {
        VFXManager.Instance.SpawnSlapSparks(impactPoint);
        SoundManager.Instance.PlaySqueakSound(impactPoint);
    }
}
```
