# Active Ragdoll / Physics Puppet Architecture

In friendslop games, comedic player movement comes from physical instability and active ragdoll physics.

---

## 1. Core Concept

An **Active Ragdoll** combines an animated skeleton with a physics skeleton:
1. **Kinematic / Animated Rig**: Plays normal animations (run, jump, flail, idle) invisibly.
2. **Physical Rig**: A set of Rigidbodies and `ConfigurableJoint`s matching each body part (pelvis, spine, head, upper/lower arms, legs).
3. **Joint Drives**: Each `ConfigurableJoint` uses `slerpDrive` or `angularXDrive`/`angularYZDrive` with high `positionSpring` and `positionDamper` to exert torque trying to match the animated bone's local rotation.

```
+---------------------------+
|    Animated Bone (Guide)  |
+-------------+-------------+
              | (Target Rotation)
              v
+---------------------------+
| ConfigurableJoint (Limb)  | ---> Rigidbody reacts to walls,
+---------------------------+      props, punches & floors
```

---

## 2. State Machine: Standing vs. Ragdoll

```csharp
public enum PlayerPhysicsState
{
    ActivePuppet, // Joints powered, player walks and moves
    LimpRagdoll,  // Joint springs set to 0, completely flaccid
    GettingUp     // Moving pelvis to ground, restoring springs
}
```

### Knockout & Ragdoll Trigger
When a player takes significant impact force, trips on an obstacle, or gets hit by an explosion:
1. Disable joint spring stiffness (`positionSpring = 0`).
2. Disable character movement controller.
3. Allow ragdoll rigidbodies to tumble and collide naturally.
4. After a timer (or when velocity drops below a threshold), smoothly blend back to `ActivePuppet` or trigger a "get up" animation.

---

## 3. Comedic Tweaks
* **Slight Joint Floppiness**: Keep joint springs slightly under-damped so arms sway and flail wildly while sprinting.
* **Head Inertia**: Keep the neck joint slightly loose so the head bobs comically when running or stopping abruptly.
* **Slip & Slide**: Apply zero-friction `PhysicsMaterial` to shoes when stepping on banana peels, wet mud, or oil slicks.
