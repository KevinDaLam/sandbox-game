---
name: friendslop-mechanics
description: >-
  Architectures, code templates, and network synchronization patterns for friendslop game mechanics: active ragdolls, physics grab & throw, Netcode for GameObjects (NGO) co-op networking, proximity voice chat, slapstick hazards, and spectator/revive loops. Use when designing or implementing multiplayer co-op physics gameplay, player controllers, or comedic interaction mechanics.
---

# Friendslop Game Mechanics & Systems Guide

This skill provides step-by-step design blueprints, C# patterns, and architecture references for building chaotic co-op physics comedy games.

---

## 1. Quick Reference Links
* [Active Ragdoll & Physics Puppet Setup](./references/active-ragdoll.md)
* [Physics Grab, Carry & Throw System](./references/grab-throw-system.md)
* [Netcode for GameObjects (NGO) Foundation](./references/netcode-setup.md)
* [Proximity Voice Chat & Comedic 3D Audio](./references/proximity-audio.md)

---

## 2. Core Gameplay Pillars Checklist

When implementing features for the game, adhere to these checks:
1. **Is it funny when it goes wrong?**
   * If a player misjudges a jump, do they faceplant and ragdoll into their friend?
   * If an item explodes, does it fling bodies with ragdoll momentum?
2. **Can players mess with each other?**
   * Can you push, carry, or slap your teammate?
   * Can you steal items from their hands or drop heavy objects onto their head?
3. **Does the audio enhance the panic?**
   * Are voices attenuated over 3D distance?
   * Does audio get muffled behind walls and doors?
4. **Is networking responsive?**
   * Use client prediction for local player movement.
   * Sync ragdoll limbs and carried items cleanly via Netcode for GameObjects.

---

## 3. Recommended Scripting Structure

```text
Assets/_Game/Scripts/
├── Core/
│   ├── GameManager.cs
│   └── GameState.cs
├── Player/
│   ├── PlayerController.cs
│   ├── RagdollController.cs
│   └── PlayerHealth.cs
├── Interactions/
│   ├── IInteractable.cs
│   ├── PlayerGrabber.cs
│   └── PhysicsProp.cs
├── Networking/
│   ├── GameNetworkManager.cs
│   ├── NetworkPlayerSync.cs
│   └── NetworkObjectSpawner.cs
└── Audio/
    ├── ProximityAudioEmitter.cs
    └── ComedicSoundTrigger.cs
```
