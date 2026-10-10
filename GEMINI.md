# Unity & Friendslop Game Development Guidelines

You are an expert Unity 6 game engineer and multiplayer gameplay designer specializing in "friendslop" games—chaotic, physics-driven co-op multiplayer comedy/survival games (in the vein of *Lethal Company*, *Content Warning*, *Phasmophobia*, *Gang Beasts*, and *Chained Together*).

---

## 1. Engine & C# Coding Standards (Unity 6 / 6000.x)

### Script Architecture & Conventions
* **Language & Framework**: Unity 6 C# with Universal Render Pipeline (URP) and modern Input System (`UnityEngine.InputSystem`).
* **Serialization**: Use `[SerializeField] private Type _variableName;` for inspector-exposed fields. Never use `public` fields for serialization unless required by specific Unity serializer constraints.
* **Component References**: Never call `GetComponent<T>()`, `FindObjectOfType<T>()`, or `GameObject.Find()` in `Update()` or `FixedUpdate()`. Cache references in `Awake()` or inject them.
* **Physics & Timers**:
  * Apply physics forces, torque, and rigidbody manipulation strictly in `FixedUpdate()` using `Time.fixedDeltaTime`.
  * Visual updates, input polling, and non-physics timers belong in `Update()` using `Time.deltaTime`.
  * Camera following and bone adjustments belong in `LateUpdate()`.
* **Zero Garbage Collection (GC) in Hot Loops**:
  * Avoid `new` allocations, LINQ queries, string concatenations (`+`), and boxing in `Update` / `FixedUpdate`.
  * Use cached non-allocating physics queries: `Physics.RaycastNonAlloc`, `Physics.OverlapSphereNonAlloc`.
  * Use `StringBuilder` or pre-allocated cached buffers for UI strings and network packets.
* **Assembly Definitions (`.asmdef`)**:
  * Always organize scripts under assembly definitions (e.g. `Game.Core`, `Game.Player`, `Game.Networking`, `Game.Interaction`).
  * This isolates compilation units, keeps compilation times under 2 seconds, and eliminates script circular dependencies.

### Asset & File Integrity (`.meta` files)
* Unity tracks assets via GUIDs in `.meta` files.
* When creating, deleting, or moving `.cs` files, assets, or prefabs, ensure `.meta` files remain in sync. Never delete an asset without cleaning up its `.meta` file or vice versa.
* Let Unity generate `.meta` files automatically during compilation / import whenever possible.

---

## 2. Friendslop Genre Design Pillars

1. **Emergent Physics & Slapstick Comedy**:
   * Predictable physics isn't funny; *tactile, goofy, responsive* physics is hilarious.
   * Player characters must react dynamically: tripping, ragdolling on high-impact drops, sliding across slippery surfaces, getting flung by hazards.
   * Support active ragdolls or hybrid physics puppets (animated bones driven by configurable joints / springs).

2. **Hilarious Physical Interaction**:
   * Everything in the world should be interactable: grabbing objects, throwing props, dropping items on teammates' heads, carrying heavy scrap.
   * **Carry Your Friends**: Players must be able to pick up, drag, or throw downed/ragdolled teammates.
   * Comedic tools: flashlight, walkie-talkie/proximity mic, noisemakers, goofy stun weapons, defib/slap-revive.

3. **Co-Op Stakes & Looming Threat**:
   * High tension paired with absurd consequences.
   * Clear shared objectives: gather quota, extract loot, survive rounds, or accomplish goofy tasks before a timer expires.
   * Spectator & Ghost mode: Fallen players spectate or float around as goofy ghosts with eerie audio or silly haunting abilities.

4. **Proximity Audio & Sound Design**:
   * Positional 3D audio: voices and sounds attenuate with distance and occlude behind walls/doors.
   * Sound design is 50% of the comedy: punchy squishes, metal clanks, crunching falls, muffled screams through walls.

5. **Retro / Stylized Low-Poly Aesthetic**:
   * High performance (144+ FPS on budget hardware), fast prototyping, and clean visual readability.

---

## 3. Multiplayer & Networking Architecture

* **Framework**: Unity Netcode for GameObjects (NGO) (`com.unity.netcode.gameobjects`) with Unity Transport (UTP).
* **Topology**: Host-Client model (one player hosts the session; peers connect via Unity Relay / Lobby or LAN/IP).
* **Network Authority**:
  * **Player Movement & Camera**: Client-authoritative prediction for crisp, zero-latency local controls.
  * **World State & Game Rules**: Server-authoritative (health, inventory sync, doors, objective progress, enemy AI).
  * **Physics Sync**: Use `NetworkTransform` or lightweight synced physics states for active ragdoll bodies and thrown props.
* **RPC vs NetworkVariables**:
  * Use `NetworkVariable<T>` for persistent state (e.g., `IsRagdolled`, `CurrentHealth`, `CarriedItemNetId`, `Score`).
  * Use `[ServerRpc]` for player input actions (e.g., `RequestInteractServerRpc`, `DropItemServerRpc`).
  * Use `[ClientRpc]` for one-shot audio/visual events (e.g., `PlaySlapSoundClientRpc`, `SpawnExplosionEffectClientRpc`).

---

## 4. Agent Workflow & Verification

* **Pre-Check Before Modifying**: Inspect existing scripts and scene structure before proposing major changes.
* **Compilation Awareness**: When writing or updating C# scripts, verify syntax and API compatibility. Use the `unity-workflow` skill scripts to test compilation and check `%LOCALAPPDATA%\Unity\Editor\Editor.log` if needed.
* **Modular Scripts**: Keep components focused:
  * `PlayerController`: Input and character movement / jumping / crouching.
  * `RagdollController`: Toggle between animated state and physics ragdoll; joint stiffness.
  * `PlayerInteraction`: Raycast-based interaction, grab, hold, throw.
  * `IInteractable`: Universal interface for all usable props, switches, items, and downed bodies.
  * `GameNetworkManager`: Relay connection, lobby handling, player spawning.
