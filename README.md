# SandboxGame 🎮

A chaotic physics-driven co-op multiplayer ("friendslop") prototype built in **Unity 6 (`6000.6.5f1`)** with the **Universal Render Pipeline (URP)** and **Netcode for GameObjects (NGO)**.

Designed as a collaborative sandbox for engineers learning game development through clean systems, Git version control, and modular architecture.

---

## 🎯 Current Milestone: 1 Room, 2 Players, 1 Object

* **1 Room**: Floor and enclosing perimeter walls with physical colliders (`MainGame.unity`).
* **2 Players**: Dynamic player spawning via `NetworkManager` — local first-person controller with isolated camera/inputs per client.
* **1 Object**: Physics-driven dynamic prop (`PickupBox`) with smooth spring-pull holding and momentum throwing synced over the network.

---

## 🛠️ Tech Stack & Architecture

| Component | Technology | Description |
| :--- | :--- | :--- |
| **Engine** | Unity 6 (`6000.6.5f1`) | Modern LTS runtime with sub-second C# compilation. |
| **Graphics** | Universal Render Pipeline (URP) | High-performance scriptable rendering for stylized aesthetics. |
| **Networking** | Netcode for GameObjects (NGO) | Host-Client topology over UDP (`UnityTransport`). |
| **Future Transport** | Fish-Net + FishySteamworks | Planned Steam P2P SDR integration for zero port forwarding. |
| **Version Control** | Git + Git LFS | `.gitattributes` pre-configured to route 3D models, textures, and audio through LFS. |
| **Assembly Defs** | `Game.asmdef`, `Game.Editor.asmdef` | Isolated assemblies keeping script iteration cycles under 2 seconds. |

---

## 🚀 Quick Start Guide

### 1. Prerequisites
* **Unity Hub** with **Unity 6 (`6000.6.5f1`)** installed.
* **Git** with **Git LFS** enabled (`git lfs install`).

### 2. Clone the Repository
```bash
git clone https://github.com/KevinDaLam/sandbox-game.git
cd sandbox-game
git lfs pull
```

### 3. Open in Unity Hub
1. Open **Unity Hub**.
2. Click **Add > Add project from disk**.
3. Select the `sandbox-game` directory.
4. Launch using **Unity 6000.6.5f1**.
5. In the Project browser, open the starting scene: **`Assets/_Game/Scenes/MainGame.unity`**.

---

## 🕹️ Controls

| Key / Input | Action |
| :--- | :--- |
| `W` `A` `S` `D` | Move character |
| `Mouse` | Look around (body yaw + camera pitch clamp) |
| `Space` | Jump |
| `E` | Pick up / drop the physics box |

---

## 👥 Testing 2 Players on One Machine

You don't need two separate computers to test multiplayer:

1. **Host (Player 1)**: In the Unity Editor, click the **Play (▶)** button and select **Start Host (Player 1)**.
2. **Client (Player 2)**:
   - Go to **File > Build Settings > Build and Run** (or run an existing build).
   - In the standalone game window, click **Start Client (Player 2)**.
3. Both players will appear in the room simultaneously and can walk around, pass the box, and interact in real time!

---

## 📂 Project Structure

```text
sandbox-game/
├── Assets/
│   └── _Game/
│       ├── Prefabs/          # Networked prefabs (PlayerPrefab.prefab)
│       ├── Scenes/           # Playable scenes (MainGame.unity)
│       ├── Scripts/          # Core C# logic
│       │   ├── Editor/       # Unity Editor utilities (URP setup tooling)
│       │   ├── Interactions/ # Universal interaction contracts & physical props
│       │   ├── Networking/   # Session & connection managers
│       │   ├── Player/       # Controllers, camera look, and ragdoll systems
│       │   └── Starter/      # Beginner-friendly prototype scripts
│       └── Settings/         # URP pipeline assets & renderer configs
├── Packages/                 # Package manager manifest (URP, NGO, Input System)
├── ProjectSettings/          # Tag, layer, graphics, and build configurations
└── docs/                     # Architectural documentation & developer guides
```

---

## 📚 Documentation & Developer Guides

* 📐 **[Architecture Breakdown](docs/ARCHITECTURE_BREAKDOWN.md)**: Deep dive into prefabs, player identity guards (`IsOwner`), and the networked pickup RPC flow.
* 🧠 **[Software Engineer's Guide to Unity](docs/ENGINEER_GUIDE.md)**: The mental model shifts (game loop vs. event-driven, composition over inheritance, `.meta` file hygiene).
* ⚙️ **[Agent & Workspace Setup Guide](docs/SETUP_GUIDE.md)**: Antigravity agent configuration, headless batchmode verification, and Unity MCP integration.
