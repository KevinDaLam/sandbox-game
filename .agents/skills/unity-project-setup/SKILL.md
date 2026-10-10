---
name: unity-project-setup
description: >-
  Initializes and configures a clean Unity 6 project structure for friendslop games with Netcode for GameObjects, Universal Render Pipeline (URP), Input System, .gitignore, and Assembly Definitions. Use when setting up or repairing the Unity project repository.
---

# Unity 6 Friendslop Project Setup

Use this skill to initialize or audit the Unity 6 project structure and its dependencies.

---

## 1. Running the Project Scaffolding

To scaffold or verify all directories, manifest dependencies, gitignore, and assembly definitions:

```powershell
powershell -ExecutionPolicy Bypass -File .agents/skills/unity-project-setup/scripts/init-project.ps1
```

---

## 2. Opening the Project in Unity Hub

Once scaffolded:
1. Open **Unity Hub**.
2. Click **Add > Add project from disk**.
3. Select this folder (`c:\Users\kvnla\Documents\Antigravity\SandboxGame`).
4. Unity Hub will recognize editor version **6000.6.5f1** and download the declared packages (Netcode for GameObjects, URP, Input System) automatically upon first launch.
