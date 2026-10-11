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
   - In the standalone game window, leave the join code blank and click **Join Game (Player 2)** (defaults to loopback `127.0.0.1`).
3. Both players will appear in the room simultaneously and can walk around, pass the box, and interact in real time!

---

## 🌐 Testing with Two Computers (LAN / Local Network)

Connecting two computers on the same Wi-Fi or Ethernet network is now automated:

### Method 1: Zero-Click LAN Auto-Discovery 📡 *(Recommended)*
1. **On Computer 1 (Host)**: Click **Start Host (Player 1)**.
   - The host immediately begins broadcasting a UDP discovery beacon across your subnet (`255.255.255.255:47777`).
2. **On Computer 2 (Client)**: Launch the game.
   - The client scans your local network automatically in the background.
   - Under **📡 LAN Auto-Discovery**, a join button appears automatically:
     ```
     👉 Join [3W9K4V] (192.168.1.175)
     ```
   - Click it to connect instantly with zero typing!

### Method 2: 6-Digit Room Code 🔑
1. When Player 1 starts the host, a prominent yellow 6-digit room code is displayed on-screen (e.g., **`3W9K4V`**).
2. Player 2 enters that 6-digit code into the **Join with 6-Digit Room Code** text box and clicks **Join Game (Player 2)**.
3. The game mathematically decodes the code into the host's LAN IP address and establishes the connection.

### Troubleshooting: Windows Defender Firewall
If the client shows *"Scanning Wi-Fi for games..."* or fails to connect:
- Ensure the Host PC allowed Unity / SandboxGame through **Windows Defender Firewall** for both **Private and Public** networks.
- Inbound UDP ports used: **`7777`** (Game traffic) and **`47777`** (LAN Discovery beacon).

> [!TIP]
> **Playing over the internet (different houses)?**
> To test over the internet without setting up router port forwarding:
> 1. Both players install [Tailscale](https://tailscale.com) or [ZeroTier](https://www.zerotier.com).
> 2. Join the same private mesh network.
> 3. Host starts game on the Tailscale IP, and the client enters the host's 6-digit code or connects directly!

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
