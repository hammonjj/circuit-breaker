using UnityEngine;
using Spine;
using Spine.Unity;

/// <summary>
/// Controls when the Walk animation plays and flips the skeleton based on input.
/// Works alongside SpineLegIK to drive four-segment leg IK.
/// </summary>
[RequireComponent(typeof(SkeletonAnimation))]
public class PlayerAnim : MonoBehaviour {
    [Tooltip("Drag your SkeletonAnimation component here if not on the same GameObject.")]
    public SkeletonAnimation skeletonAnimation;

    private TrackEntry _walkEntry;
    private TrackEntry _crouchEntry;

    void Awake() {
        if (skeletonAnimation == null) {
            skeletonAnimation = GetComponent<SkeletonAnimation>();
        }

        // Start in neutral pose
        skeletonAnimation.state.ClearTracks();
        skeletonAnimation.Skeleton.SetToSetupPose();
        skeletonAnimation.Skeleton.UpdateWorldTransform(Skeleton.Physics.Update);

        var controller = GetComponentInParent<TarodevController.PlayerController>();
        if (controller != null) {
            controller.Jumped += OnJumped;
        }
    }

    void OnDestroy() {
        var controller = GetComponentInParent<TarodevController.PlayerController>();
        if (controller != null) {
            controller.Jumped -= OnJumped;
        }
    }

    /// <summary>
    /// Call every frame with current move direction and grounded state.
    /// </summary>
    /// <param name="move">Player input vector (x for horizontal).</param>
    /// <param name="grounded">Whether the player is on the ground.</param>
    public void TickUpdate(Vector2 move, bool grounded, bool crouching) {
        if (crouching && _crouchEntry == null) {
            _crouchEntry = skeletonAnimation.state.SetAnimation(1, "Crouch", false);
            _crouchEntry.TimeScale = 1f;
        } else if (!crouching && _crouchEntry != null) {
            skeletonAnimation.state.ClearTrack(1);
            _crouchEntry = null;
        }

        bool moving = grounded && Mathf.Abs(move.x) > 0.01f;

        // Start walk if moving
        if (moving) {
            if (_walkEntry == null) {
                _walkEntry = skeletonAnimation.state.SetAnimation(0, "Walk", true);
                _walkEntry.TimeScale = 1f;
            }
        }
        // Stop walk when not moving
        else if (_walkEntry != null) {
            _walkEntry = null;
            skeletonAnimation.state.SetEmptyAnimation(0, 0.2f); // fade out
        }

        // Flip based on direction
        if (moving) {
            var skeleton = skeletonAnimation.Skeleton;
            // Use ScaleX to flip horizontally: negative = flip
            skeleton.ScaleX = move.x > 0 ? -1f : 1f;
        }
    }

    private void OnJumped(TarodevController.JumpType type) {
        var entry = skeletonAnimation.state.SetAnimation(2, "Jump", false);
        entry.TimeScale = 1f;
        // Fade out jump into whatever track is active
        skeletonAnimation.state.AddEmptyAnimation(2, 0.1f, 0);
    }
}