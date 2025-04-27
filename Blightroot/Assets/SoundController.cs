using UnityEngine;
using Spine;
using Spine.Unity;
using TarodevController;

/// <summary>
/// Centralizes all player sound pulses (footsteps, jumps, landings, etc.)
/// Listens to Spine events and PlayerController events, computes pulse radius based on state.
/// </summary>
public class SoundController : MonoBehaviour
{
    [Header("References")]
    [Tooltip("Spine SkeletonAnimation to listen for footstep events.")]
    public SkeletonAnimation skeletonAnimation;

    [Tooltip("PlayerController to listen for movement and landing events.")]
    public PlayerController playerController;

    [Tooltip("Prefab of the expanding circle pulse effect.")]
    public GameObject soundPulsePrefab;
    public Vector2 pulseOffset2D = Vector2.zero;

    [Header("Pulse Ranges (world units)")]
    public float walkPulseRange = 2f;
    public float runPulseRange = 4f;
    public float jumpPulseRange = 3f;
    public float landPulseRange = 5f;
    public float crouchPulseRange = 1f;

    [Header("Misc")]
    [Tooltip("Minimum velocity to be considered running.")]
    public float runSpeedThreshold = 3f;

    [Tooltip("Layers considered ground for material detection.")]
    public LayerMask groundLayer;

    private void Awake()
    {
        if (skeletonAnimation == null)
        {
            skeletonAnimation = GetComponent<SkeletonAnimation>();
        }

        if (playerController == null)
        {
            playerController = GetComponent<PlayerController>();
        }

        skeletonAnimation.state.Event += HandleSpineEvent;
        playerController.GroundedChanged += OnGroundedChanged;
    }

    private void OnDestroy()
    {
        skeletonAnimation.state.Event -= HandleSpineEvent;
        playerController.GroundedChanged -= OnGroundedChanged;
    }

    /// <summary>
    /// Spine event handler for footstep events (named "foot_...").
    /// </summary>
    private void HandleSpineEvent(TrackEntry trackEntry, Spine.Event e)
    {
        // filter for only our footstep events
        if (e.Data.Name.StartsWith("WalkLanding"))
        {
            // Determine forward direction based on velocity
            Vector2 forward2D;
            float vx = playerController.Velocity.x;
            if (Mathf.Abs(vx) > 0.01f)
            {
                forward2D = new Vector2(Mathf.Sign(vx), 0f);
            }
            else
            {
                forward2D = Vector2.right; // default facing right
            }

            // Compute spawn position in 2D
            Vector2 basePos2D = new Vector2(transform.position.x, transform.position.y);
            Vector2 offset2D = new Vector2(pulseOffset2D.x * forward2D.x, pulseOffset2D.y);
            Vector2 spawn2D  = basePos2D + offset2D;

            // Spawn the pulse at this 2D position (z = 0)
            Vector3 spawn3D = new Vector3(spawn2D.x, spawn2D.y, 0f);
            //Instantiate(soundPulsePrefab, spawn3D, Quaternion.identity);
            SpawnPulse(walkPulseRange, spawn3D);
        }
    }

    /// <summary>
    /// Called when grounded state changes (landing detection).
    /// </summary>
    private void OnGroundedChanged(bool grounded, float prevYVelocity)
    {
        // if (grounded && prevYVelocity < -0.1f)
        // {
        //     SpawnPulse(landPulseRange);
        // }
    }

    /// <summary>
    /// Instantiates a pulse effect at the player position with the given max scale.
    /// </summary>
    private void SpawnPulse(float maxRange, Vector3 position)
    {
        if (soundPulsePrefab == null)
        {
            return;
        }

        GameObject pulse = Instantiate(soundPulsePrefab, position, Quaternion.identity);
        SoundPulse pulseScript = pulse.GetComponent<SoundPulse>();

        if (pulseScript != null)
        {
            pulseScript.maxScale = maxRange;
        }
    }
}
