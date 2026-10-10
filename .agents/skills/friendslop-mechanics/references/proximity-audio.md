# Proximity Voice Chat & Comedic 3D Audio

Sound design and proximity audio are critical for comedy in friendslop games.

---

## 1. 3D Spatial Audio Setup in Unity

To make voices and footsteps feel physical and directional:

1. **AudioSource Settings**:
   * `Spatial Blend`: Set to `1.0` (Full 3D).
   * `Volume Rolloff`: `Logarithmic` or `Custom Curve`.
   * `Min Distance`: `1.5m` (voice stays loud up close).
   * `Max Distance`: `25m` - `35m` (friends fade out into the dark).
   * `Doppler Level`: `0.5` (slight pitch bend when running past or falling off a cliff screaming).

---

## 2. Occlusion & Wall Muffling (The "Muffled Scream" Effect)

When a teammate is behind a closed door or around a concrete corner, muffle their sound using `AudioLowPassFilter`:

```csharp
public class AudioOcclusion : MonoBehaviour
{
    [SerializeField] private AudioLowPassFilter _lowPassFilter;
    [SerializeField] private Transform _targetListener;
    [SerializeField] private LayerMask _occlusionMask;

    private const float ClearCutoff = 22000f;   // Crisp sound
    private const float MuffledCutoff = 900f;    // Behind wall/door

    private void Update()
    {
        if (_targetListener == null) return;

        Vector3 dir = _targetListener.position - transform.position;
        bool isOccluded = Physics.Raycast(transform.position, dir.normalized, dir.magnitude, _occlusionMask);

        float targetFreq = isOccluded ? MuffledCutoff : ClearCutoff;
        _lowPassFilter.cutoffFrequency = Mathf.Lerp(_lowPassFilter.cutoffFrequency, targetFreq, Time.deltaTime * 10f);
    }
}
```

---

## 3. Comedic Audio Tropes

* **Death Cutoff**: When a player dies or ragdolls violently, cut off their mic / sound effect immediately (instant silence after a scream).
* **Pitch Shifting**: Shift sound pitch up on small lightweight hits, and down with heavy reverb on big bonks.
* **Ghost Audio Filter**: Apply a high-pass / chorus / echo filter to fallen players when they talk in spectator mode.
