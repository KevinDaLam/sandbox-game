# Netcode for GameObjects (NGO) Multiplayer Foundation

Architecture and synchronization guide for 1-4+ player co-op sessions using Unity Netcode for GameObjects (NGO) and Unity Transport (UTP).

---

## 1. Network Topology: Host-Client Model

* **Host**: One player acts as both server and local player.
* **Clients**: Friends connect to the host via Unity Relay (join code) or Direct IP.
* **Advantage**: Zero dedicated server hosting costs, fast setup, ideal for Discord friend groups.

---

## 2. Core Netcode Components

Component | Purpose
:--- | :---
`NetworkManager` | Central networking orchestrator managing transports, player prefabs, and connected clients.
`UnityTransport` | Low-level UDP socket transport; handles direct connections and Unity Relay protocol.
`NetworkObject` | Placed on prefabs that exist across the network (Players, Thrown Items, Interactive Doors).
`NetworkTransform` | Interpolates and synchronizes position/rotation of players and moving network entities.
`NetworkVariable<T>` | Reactive synchronized state automatically sent from server to clients.

---

## 3. Essential Friendslop Network Patterns

### Synchronized Player State
```csharp
public class NetworkPlayerState : NetworkBehaviour
{
    // Synchronize health, downed state, and carried item ID
    public NetworkVariable<int> Health = new(100, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Server);
    public NetworkVariable<bool> IsDowned = new(false, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Server);
    public NetworkVariable<ulong> CarriedItemNetId = new(0, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Server);

    public override void OnNetworkSpawn()
    {
        Health.OnValueChanged += HandleHealthChanged;
        IsDowned.OnValueChanged += HandleDownedChanged;
    }
}
```

### Server-Authoritative Interaction RPCs
```csharp
[ServerRpc]
public void RequestGrabItemServerRpc(ulong itemNetworkObjectId, ServerRpcParams rpcParams = default)
{
    if (NetworkManager.Singleton.SpawnManager.SpawnedObjects.TryGetValue(itemNetworkObjectId, out var netObj))
    {
        // Server validates distance
        if (Vector3.Distance(transform.position, netObj.transform.position) < 3.5f)
        {
            // Transfer ownership or attach on server
            AttachItem(netObj);
        }
    }
}
```

### Comedic ClientRPCs (Audio & Slap Effects)
```csharp
[ClientRpc]
public void PlaySlapEffectClientRpc(Vector3 impactPoint, Vector3 slapDirection)
{
    // Spawn funny comic slap particle & squeak sound on all client screens
    VFXManager.Instance.SpawnSlapSparks(impactPoint);
    SoundManager.Instance.PlaySqueakSound(impactPoint);
}
```
