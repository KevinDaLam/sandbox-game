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
   - In the standalone game window, leave the IP as `127.0.0.1` and click **Start Client (Player 2)**.
3. Both players will appear in the room simultaneously and can walk around, pass the box, and interact in real time!

---

## 🌐 Testing with Two Computers (LAN / Local Network)

To test the prototype between two physical computers connected to the same Wi-Fi or router network:

### Step 1: Find the Host PC's Local IP Address
On the computer that will run as the **Host**:
1. Open PowerShell or Command Prompt.
2. Run:
   ```shell
   ipconfig
   ```
3. Locate your active network adapter (e.g., `Wireless LAN adapter Wi-Fi` or `Ethernet adapter Ethernet`).
4. Note your **IPv4 Address** (typically looks like `192.168.1.X` or `10.0.0.X`).

### Step 2: Configure Windows Defender Firewall (Host PC)
By default, Windows Firewall can block incoming UDP connections on port `7777`.
- **Option A (Quick)**: When starting the game or Unity for the first time, Windows will prompt *"Allow Unity / SandboxGame through Windows Defender Firewall"*. Check **both** Private and Public networks, then click **Allow access**.
- **Option B (Manual Inbound Rule)**:
  1. Open **Windows Defender Firewall with Advanced Security**.
  2. Click **Inbound Rules > New Rule...**.
  3. Select **Port** > Next.
  4. Select **UDP**, specify port **7777** > Next.
  5. Select **Allow the connection** > Next > Finish.

### Step 3: Run the Game on Both Computers
1. **On Computer 1 (Host)**:
   - Launch the game (either inside the Unity Editor or via a standalone build).
   - Click **Start Host (Player 1)**.
2. **On Computer 2 (Client)**:
   - Launch the game (via a standalone build or Unity Editor).
   - In the on-screen HUD text box under **Join Host by IP:**, enter Computer 1's IPv4 address (e.g. `192.168.1.42`).
   - Click **Start Client (Player 2)**.

Both computers will connect immediately, synchronizing player movement and physics box manipulation across your local network!

> [!TIP]
> **Want to test over the internet before Steamworks is integrated?**
> Use a zero-config peer mesh tool like [Tailscale](https://tailscale.com) or [ZeroTier](https://www.zerotier.com). Install it on both PCs, join the same private network, and use the Host's 100.x.x.x Tailscale IP address in the Client HUD. No router port forwarding required!

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
