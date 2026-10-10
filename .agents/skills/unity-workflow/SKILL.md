---
name: unity-workflow
description: >-
  Procedures for managing the Unity 6 development loop, verifying C# script compilation, checking Unity editor logs, and running Unity headless commands. Use when the user asks to compile scripts, check for Unity errors, build the project, or inspect Unity editor logs.
---

# Unity 6 Development & Compilation Workflow

This skill provides the standard runbook for building, validating, and debugging Unity 6 projects in tandem with Antigravity.

---

## 1. Verifying C# Compilation in Batchmode

Whenever you add or modify C# scripts, verify that there are no compilation errors before asking the user to test in the Unity Editor.

### Run Script
Execute the batchmode compiler check:
```powershell
powershell -ExecutionPolicy Bypass -File .agents/skills/unity-workflow/scripts/check-compilation.ps1
```

* If compilation succeeds, exit code is `0`.
* If compilation fails with `error CSxxxx`, examine the error output, fix the offending lines, and re-run.

---

## 2. Inspecting Unity Editor Logs

When diagnosing runtime issues, missing package dependencies, or assembly reload crashes, inspect Unity's editor log:

```powershell
powershell -ExecutionPolicy Bypass -File .agents/skills/unity-workflow/scripts/view-editor-log.ps1 -Lines 80
```

Unity Editor log is located at:
`$env:LOCALAPPDATA\Unity\Editor\Editor.log`

---

## 3. Creating New Scripts with Assembly Definitions

When creating new C# scripts:
1. Place them under the appropriate namespace and folder in `Assets/_Game/Scripts/`.
2. Ensure the enclosing folder has an Assembly Definition (`.asmdef`) or is covered by one (e.g. `Game.asmdef`).
3. Include standard Unity 6 namespaces:
   * `UnityEngine`
   * `Unity.Netcode` (for networked components)
   * `UnityEngine.InputSystem` (for player input)
4. Use `[SerializeField] private` for inspector fields.
5. Cache components in `Awake()`.
