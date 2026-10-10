# Unity & Friendslop Game Development Guidelines

See [GEMINI.md](file:///c:/Users/kvnla/Documents/Antigravity/SandboxGame/GEMINI.md) for complete details.

### Quick Reference:
- **Engine**: Unity 6 (6000.x) with URP and Netcode for GameObjects (NGO).
- **Genre**: Friendslop (chaotic physics co-op, proximity voice, ragdolls, comedic failure states).
- **C# Rules**: `[SerializeField] private`, zero-allocation in `Update`/`FixedUpdate`, `.asmdef` modular assemblies.
- **Workflow**: Automated compilation checking, `.meta` file integrity, clean host-client networking.
