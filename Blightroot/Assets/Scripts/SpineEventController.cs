using UnityEngine;
using Spine;
using Spine.Unity;

/// <summary>
/// Emits a circular “sound pulse” whenever the specified Spine event fires on this enemy.
/// </summary>
public class SpineEventController : MonoBehaviour
{
    [Header("References")]
    [Tooltip("The SkeletonAnimation whose events we listen to.")]
    public SkeletonAnimation skeletonAnimation;
    [Tooltip("Prefab for the expanding circle pulse effect.")]
    public GameObject soundPulsePrefab;
    [Tooltip("Offset from the enemy’s transform for spawning the pulse.")]
    public Vector2 pulseOffset = Vector2.zero;

    [Header("Pulse Settings")]
    [Tooltip("Name of the Spine event that triggers the pulse.")]
    public string eventName = "emit_noise";
    [Tooltip("Determines the radius of the emitted circle.")]
    public float strength = 3f;

    private void Awake()
    {
        if (skeletonAnimation == null)
            skeletonAnimation = GetComponent<SkeletonAnimation>();

        if (skeletonAnimation != null)
            skeletonAnimation.state.Event += HandleSpineEvent;
    }

    private void OnDestroy()
    {
        if (skeletonAnimation != null)
            skeletonAnimation.state.Event -= HandleSpineEvent;
    }

    private void HandleSpineEvent(TrackEntry entry, Spine.Event e)
    {
        if (e.Data.Name == eventName)
        {
            var spawnPos = new Vector3(
                transform.position.x + pulseOffset.x,
                transform.position.y + pulseOffset.y,
                transform.position.z
            );
            SpawnPulse(strength, spawnPos);
        }
    }

    private void SpawnPulse(float radius, Vector3 position)
    {
        if (soundPulsePrefab == null) return;

        var go = Instantiate(soundPulsePrefab, position, Quaternion.identity);
        var pulse = go.GetComponent<SoundPulse>();
        if (pulse != null)
            pulse.maxScale = radius;
    }
}
