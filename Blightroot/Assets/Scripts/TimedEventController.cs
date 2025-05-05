using System.Collections;
using UnityEngine;

/// <summary>
/// Emits a circular “sound pulse” on a fixed timer.
/// Every <see cref="intervalSeconds"/> it will spawn <see cref="eventPrefab"/>
/// at this GameObject’s position plus <see cref="pulseOffset"/>, scaled by <see cref="strength"/>.
/// </summary>
public class TimedEventController : MonoBehaviour
{
    [Header("Pulse Settings")]
    [Tooltip("Prefab for the expanding circle pulse effect.")]
    public GameObject eventPrefab;
    [Tooltip("How often, in seconds, to emit a pulse.")]
    public float intervalSeconds = 2f;
    [Tooltip("Determines the radius/size of each emitted circle.")]
    public float strength = 3f;
    [Tooltip("Offset from this object’s position to spawn the pulse.")]
    public Vector2 pulseOffset = Vector2.zero;

    private Coroutine _pulseRoutine;

    private void OnEnable()
    {
        // Start the repeating pulse coroutine
        _pulseRoutine = StartCoroutine(PulseRoutine());
    }

    private void OnDisable()
    {
        // Stop it when disabled
        if (_pulseRoutine != null)
            StopCoroutine(_pulseRoutine);
    }

    private IEnumerator PulseRoutine()
    {
        // Optionally emit one immediately:
        // SpawnPulse();

        while (true)
        {
            yield return new WaitForSeconds(intervalSeconds);
            SpawnPulse();
        }
    }

    private void SpawnPulse()
    {
        if (eventPrefab == null) return;

        // Instantiate as a child of this GameObject
        var go = Instantiate(
            eventPrefab,
            transform.position,            // world position (we’ll override with local)
            Quaternion.identity,
            transform                     // parent
        );

        // move it into place relative to the parent
        go.transform.localPosition = new Vector3(pulseOffset.x, pulseOffset.y, 0f);
        }
}
