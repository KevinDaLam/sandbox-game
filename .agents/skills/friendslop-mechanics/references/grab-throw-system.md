# Physics Grab, Carry & Throw System

Friendslop gameplay relies heavily on physical interactions—picking up items, throwing objects at friends, and dragging incapacitated teammates.

---

## 1. Interaction Architecture (`IInteractable`)

All physical props, items, switches, and ragdoll parts implement `IInteractable`:

```csharp
public interface IInteractable
{
    string InteractionPrompt { get; }
    void OnInteract(PlayerController interactor);
    void OnGrab(Transform holdPoint, Rigidbody playerBody);
    void OnRelease(Vector3 throwVelocity);
    bool CanBeGrabbed { get; }
}
```

---

## 2. Physics-Based Grabbing (No Clipping)

Instead of parenting grabbed objects directly (`transform.parent = hand`), use a physics attachment to preserve weight, collisions, and momentum:

### Option A: Configurable / Fixed Joint (Best for Weight & Feel)
When grabbing:
1. Attach a temporary `ConfigurableJoint` (or `FixedJoint`) between the player's hand/hold-point Rigidbody and the grabbed item's Rigidbody.
2. The item naturally knocks into doorways, swings with player rotation, and resists being dragged through walls.

### Option B: Spring / Target Force Follower (Smoothest for First-Person)
1. Move the held object toward `holdPoint.position` by applying force:
   ```csharp
   Vector3 toTarget = _holdPoint.position - _heldRigidbody.position;
   _heldRigidbody.linearVelocity = toTarget * _carryForceMultiplier;
   ```
2. Break the grab if the distance exceeds a threshold (e.g. object gets stuck behind a closed door).

---

## 3. Grabbing & Dragging Downed Players

When a teammate is ragdolled/downed:
1. Add an `IInteractable` component to their ragdoll limb (e.g., collar, shoulders, or feet).
2. Grabbing the limb attaches the player's hold point to the limb's Rigidbody.
3. The conscious player can drag their ragdolled friend away from hazards, load them into an escape vehicle, or throw them across a gap.

---

## 4. Throwing Mechanics

When releasing the grab button:
```csharp
public void Throw(Vector3 forwardDirection, float chargeMultiplier = 1f)
{
    ReleaseGrab();
    Vector3 throwVelocity = (forwardDirection + Vector3.up * 0.15f).normalized * (_baseThrowForce * chargeMultiplier);
    _heldRigidbody.linearVelocity += throwVelocity;
    _heldRigidbody.AddTorque(Random.insideUnitSphere * 10f, ForceMode.Impulse); // Tumble comedy
}
```
