using UnityEngine;
using Spine;
using Spine.Unity;
using System;

/// <summary>
/// Controls when the Walk animation plays and flips the skeleton based on input.
/// Works alongside SpineLegIK to drive four-segment leg IK.
/// </summary>
[RequireComponent(typeof(SkeletonAnimation))]
public class PlayerAnim : MonoBehaviour {
    [Tooltip("Drag your SkeletonAnimation component here if not on the same GameObject.")]
    public SkeletonAnimation skeletonAnimation;
    public GameObject soundPulsePrefab;
    public Vector2 pulseOffset2D = Vector2.zero;
    int _dirSign = 1;

    private TrackEntry _walkEntry;
    private TrackEntry _crouchEntry;
    private TrackEntry _jumpEntry;
    private TrackEntry _landEntry;
    private TrackEntry _fallEntry;
    private TrackEntry _slideEntry;


    private TarodevController.PlayerController _controller;

    private bool _wasGrounded;
    private bool _wasFalling;

    void Awake() {
        skeletonAnimation ??= GetComponent<SkeletonAnimation>();

        // Start in neutral pose
        skeletonAnimation.state.ClearTracks();
        skeletonAnimation.Skeleton.SetToSetupPose();
        skeletonAnimation.Skeleton.UpdateWorldTransform(Skeleton.Physics.Update);
        skeletonAnimation.state.Event += HandleSpineEvent;

        _controller = GetComponentInParent<TarodevController.PlayerController>();
        if (_controller != null) {
            _controller.Jumped += OnJumped;
        }
    }

    void OnDestroy() {
        if (_controller != null) {
            _controller.Jumped -= OnJumped;
        }

        skeletonAnimation.state.Event -= HandleSpineEvent;
    }

    /// <summary>
    /// Call every frame with current move direction and grounded state.
    /// </summary>
    /// <param name="move">Player input vector (x for horizontal).</param>
    /// <param name="grounded">Whether the player is on the ground.</param>
    public void TickUpdate(Vector2 move, bool grounded, bool crouching, bool wallSliding, int wallDirection)
    {
        bool moving = grounded && Mathf.Abs(move.x) > 0.01f;
        
        handleCrouching(crouching);
        handleWallSliding(wallSliding);
        handleWalking(moving);
        handleFalling(grounded);
        handleLanding(grounded);
        handleFlip(move, moving, wallSliding, wallDirection);

        _wasFalling = !grounded && _controller.Velocity.y < 0;
        _wasGrounded = grounded;
    }

  private void handleCrouching(bool crouching)
  {
    if (crouching && _crouchEntry == null)
    {
      _crouchEntry = skeletonAnimation.state.SetAnimation(1, "Crouch", false);
      _crouchEntry.TimeScale = 1f;
    }
    else if (!crouching && _crouchEntry != null)
    {
      skeletonAnimation.state.SetEmptyAnimation(1, 0.2f);
      _crouchEntry = null;
    }
  }

  private void handleWallSliding(bool wallSliding)
  {
    if (wallSliding && _slideEntry == null)
    {
      _slideEntry = skeletonAnimation.state.SetAnimation(5, "WallSlide", true);
      _slideEntry.TimeScale = 1f;
    }
    else if (!wallSliding && _slideEntry != null)
    {
      skeletonAnimation.state.SetEmptyAnimation(5, 0.2f);
      _slideEntry = null;
    }
  }

  private void handleWalking(bool moving)
  {
    // Start walk if moving
    if (moving)
    {
      if (_walkEntry == null)
      {
        _walkEntry = skeletonAnimation.state.SetAnimation(0, "Walk", true);
        _walkEntry.TimeScale = 1f;
      }
    }
    // Stop walk when not moving
    else if (_walkEntry != null)
    {
      _walkEntry = null;
      skeletonAnimation.state.SetEmptyAnimation(0, 0.2f); // fade out
    }
  }

  private void handleFalling(bool grounded)
  {
    bool falling = !grounded && _controller.Velocity.y < 0;
    if (falling && !_wasFalling && _jumpEntry == null)
    {
      _fallEntry = skeletonAnimation.state.SetAnimation(3, "Falling", true);
      _fallEntry.TimeScale = 1f;
    }
  }

  private void handleLanding(bool grounded)
  {
    if (grounded && _wasFalling)
    {
      // clear Fall
      if (_fallEntry != null)
      {
        skeletonAnimation.state.SetEmptyAnimation(3, 0.1f);
        _fallEntry = null;
      }

      skeletonAnimation.state.SetEmptyAnimation(4, 0.1f);

      _landEntry = skeletonAnimation.state.SetAnimation(4, "Landing", false);
      _landEntry.TimeScale = 1f;
      //when Land completes, clear it so Walk/Crouch can resume
      _landEntry.Complete += entry =>
      {
       skeletonAnimation.state.SetEmptyAnimation(4, 0.1f);
       _landEntry = null;
      };
    }
  }

  private void handleFlip(Vector2 move, bool moving, bool wallSliding, int wallDir)
  {
    var skeleton = skeletonAnimation.Skeleton;

    // Flip based on direction
    if (moving)
    {
        // Use ScaleX to flip horizontally: negative = flip
        skeleton.ScaleX = move.x > 0 ? -1f : 1f;
        _dirSign = move.x < 0 ? -1 : 1;
    } else if (wallSliding) {
        // Slide‐flip: if the wall is on your right (wallDir=+1), face right, else face left
        skeleton.ScaleX = wallDir < 0 ? -1f : 1f;
    }
  }

  private void OnJumped(TarodevController.JumpType type) {
        _jumpEntry = skeletonAnimation.state.SetAnimation(2, "Jump", false);
        _jumpEntry.TimeScale = 1f;
        
        _jumpEntry.Complete += entry => {
            // cross‐fade out jump
            skeletonAnimation.state.AddEmptyAnimation(2, 0.1f, 0);
            _jumpEntry = null;
        };
    }

    private void HandleSpineEvent(TrackEntry trackEntry, Spine.Event e)
    {
        // filter for only our footstep events
        if (e.Data.Name.StartsWith("WalkLanding"))
        {
            // find the bone that drove this event, e.g. FrontForeground_Reference
            // (you can encode the bone name in your event string if you want)
            string boneName = e.Data.Name == "foot_L"
                ? "FrontForeground_Reference"
                : "FrontBackground_Reference";

            var base2D = new Vector2(transform.position.x, transform.position.y);
            var spawn2D = base2D + new Vector2(pulseOffset2D.x * _dirSign, pulseOffset2D.y);

            // 2) lift back to 3D (z = 0 for a 2D game)
            Vector3 spawn3D = new Vector3(spawn2D.x, spawn2D.y, 0f);

            // 3) spawn your pulse prefab there
            Instantiate(soundPulsePrefab, spawn3D, Quaternion.identity);
        }
    }
}