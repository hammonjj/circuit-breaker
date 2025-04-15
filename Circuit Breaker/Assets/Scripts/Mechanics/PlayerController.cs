using System.Collections;
using UnityEngine;
using UnityEngine.InputSystem;
using Platformer.Gameplay;
using static Platformer.Core.Simulation;
using Platformer.Model;
using Spine.Unity;
using Spine;

namespace Platformer.Mechanics
{
  public enum WallSide
  {
    None,
    Left,
    Right
  }

  /// <summary>
  /// This is the main class used to implement control of the player.
  /// It is a superset of the AnimationController class, but is inlined to allow for any kind of customisation.
  /// </summary>
  public class PlayerController : KinematicObject
  {
    // Spine Animation
    public SkeletonAnimation skeletonAnim;
    public AnimationReferenceAsset idleAnimation;
    public AnimationReferenceAsset walkAnimation;

    public string currentState;

    public void SetAnimation(string animation, bool loop, float timeScale) 
    {
      //AnimationReferenceAsset animation
      if(animation == "Idle") {
        currentState = "Idle";
        skeletonAnim.state.SetAnimation(0, idleAnimation, loop);
      } else if(animation == "Walk") {
        currentState = "Walk";
        skeletonAnim.state.SetAnimation(0, walkAnimation, loop);
      } else {
        Debug.LogError("Invalid animation name: " + animation);
        return;
      }
  
      skeletonAnim.state.TimeScale = timeScale;
    }
    // End Spine Animation

    [Header("Debugging")]
    public bool wallDetectionLinesEnabled = false;
    public bool controlEnabled = true;
    public bool isInvulnerable = false;

    [Header("Audio")]
    public AudioClip jumpAudio;
    public AudioClip respawnAudio;
    public AudioClip ouchAudio;

    [Header("Abilities")]
    public bool dashUnlocked = true;
    public bool wallJumpUnlocked = true;

    [Header("Stats")]
    public float maxSpeed = 7;
    public float jumpTakeOffSpeed = 7;
    public float dashSpeed = 20f;
    public float dashDuration = 0.2f;
    public float dashCooldown = 1.0f;
    // Horizontal impulse applied during a wall jump (pushing away from the wall)
    public float wallJumpHorizontalSpeed = 7f;
    // Vertical impulse applied during a wall jump.
    public float wallJumpVerticalSpeed = 7f;
    // Detection distance for walls (increased to allow continuous wall jumps).
    public float wallDetectionDistance = 0.5f;
    // The speed at which the player slides down the wall while attached.
    public float wallSlideSpeed = 1.0f;

    public JumpState jumpState = JumpState.Grounded;

    [Header("Audio")]
    
    public Collider2D collider2d;
    public AudioSource audioSource;
    public Health health;

    private bool stopJump;

    // Movement booleans set by MoveLeft/MoveRight actions.
    private bool movingLeft;
    private bool movingRight;

    // Double Jump: Allow one extra jump per air time.
    private bool doubleJumpUsed = false;

    // Dash variables
    private bool isDashing = false;
    private bool canDash = true;

    private WallSide lastWallJumpSide = WallSide.None;

    // References to new Input System actions.
    [SerializeField] private InputActionReference moveLeftActionReference;
    [SerializeField] private InputActionReference moveRightActionReference;
    [SerializeField] private InputActionReference jumpActionReference;
    [SerializeField] private InputActionReference dashActionReference;

    bool jump;
    Vector2 move;
    SpriteRenderer spriteRenderer;
    internal Animator animator;
    readonly PlatformerModel model = GetModel<PlatformerModel>();

    public Bounds Bounds => collider2d.bounds;

    private int playerLayer;
    private int dashingLayer;
    private int terrainLayerMask;

    public enum JumpState
    {
      Grounded,
      PrepareToJump,
      Jumping,
      InFlight,
      Landed
    }

    void Awake()
    {
      health = GetComponent<Health>();
      audioSource = GetComponent<AudioSource>();
      collider2d = GetComponent<Collider2D>();
      spriteRenderer = GetComponent<SpriteRenderer>();
      animator = GetComponent<Animator>();

      playerLayer = LayerMask.NameToLayer("Player");
      dashingLayer = LayerMask.NameToLayer("DashingPlayer");
      terrainLayerMask = LayerMask.GetMask("Terrain");
    }

    override protected void Start()
    {
      base.Start();
      SetAnimation("Idle", true, 1f);
    }

    protected override void OnEnable()
    {
      base.OnEnable();

      if (moveLeftActionReference != null)
      {
        moveLeftActionReference.action.Enable();
        moveLeftActionReference.action.performed += OnMoveLeftPerformed;
        moveLeftActionReference.action.canceled += OnMoveLeftCanceled;
      }

      if (moveRightActionReference != null)
      {
        moveRightActionReference.action.Enable();
        moveRightActionReference.action.performed += OnMoveRightPerformed;
        moveRightActionReference.action.canceled += OnMoveRightCanceled;
      }

      if (jumpActionReference != null)
      {
        jumpActionReference.action.Enable();
        jumpActionReference.action.started += OnJumpStarted;
        jumpActionReference.action.canceled += OnJumpCanceled;
      }

      if (dashActionReference != null)
      {
        dashActionReference.action.Enable();
        dashActionReference.action.performed += OnDashPerformed;
      }
    }

    protected override void OnDisable()
    {
      base.OnDisable();

      if (moveLeftActionReference != null)
      {
        moveLeftActionReference.action.performed -= OnMoveLeftPerformed;
        moveLeftActionReference.action.canceled -= OnMoveLeftCanceled;
        moveLeftActionReference.action.Disable();
      }

      if (moveRightActionReference != null)
      {
        moveRightActionReference.action.performed -= OnMoveRightPerformed;
        moveRightActionReference.action.canceled -= OnMoveRightCanceled;
        moveRightActionReference.action.Disable();
      }

      if (jumpActionReference != null)
      {
        jumpActionReference.action.started -= OnJumpStarted;
        jumpActionReference.action.canceled -= OnJumpCanceled;
        jumpActionReference.action.Disable();
      }

      if (dashActionReference != null)
      {
        dashActionReference.action.performed -= OnDashPerformed;
        dashActionReference.action.Disable();
      }
    }

    protected override void Update()
    {
      UpdateWallContact();

      if (controlEnabled)
      {
        HandlePlayerInput();
      }
      else
      {
        move.x = 0;
      }

      UpdateJumpState();
      base.Update();
    }

    private void HandlePlayerInput()
    {
      move.x = 0f;
      if (movingLeft)
      {
        move.x -= 1f;
      }
      if (movingRight)
      {
        move.x += 1f;
      }
    }

    // Update wall contact by raycasting left and right using an extended detection distance.
    private int UpdateWallContact()
    {
      if (IsGrounded)
      {
        return 0;
      }
      
      // Use the collider's bounds to determine left/right origins.
      Vector2 center = collider2d.bounds.center;
      Vector2 leftOrigin = new Vector2(collider2d.bounds.min.x, center.y);
      Vector2 rightOrigin = new Vector2(collider2d.bounds.max.x, center.y);
      
      RaycastHit2D hitLeft = Physics2D.Raycast(leftOrigin, Vector2.left, wallDetectionDistance, terrainLayerMask);
      RaycastHit2D hitRight = Physics2D.Raycast(rightOrigin, Vector2.right, wallDetectionDistance, terrainLayerMask);
      
      int wallDirection;
      
      // Ignore hits on the player's own collider.
      if (hitLeft.collider != null && hitLeft.collider != collider2d)
      {
        wallDirection = -1;
      }
      else if (hitRight.collider != null && hitRight.collider != collider2d)
      {
        wallDirection = 1;
      }
      else
      {
        wallDirection = 0;
      }

      return wallDirection;
    }

    private void drawWallDetectionLines()
    {
      if (!wallDetectionLinesEnabled)
      {
        return;
      }
      
      // Use the collider's bounds to determine left/right origins.
      Vector2 center = collider2d.bounds.center;
      Vector2 leftOrigin = new Vector2(collider2d.bounds.min.x, center.y);
      Vector2 rightOrigin = new Vector2(collider2d.bounds.max.x, center.y);
      
      // Draw a green line to the left.
      Debug.DrawLine(leftOrigin, leftOrigin + Vector2.left * wallDetectionDistance, Color.green);
      // Draw a green line to the right.
      Debug.DrawLine(rightOrigin, rightOrigin + Vector2.right * wallDetectionDistance, Color.green);
    }

    private void OnDrawGizmos()
    {
      drawWallDetectionLines();
    }

    // MoveLeft event handlers
    private void OnMoveLeftPerformed(InputAction.CallbackContext context)
    {
      movingLeft = true;
      SetAnimation("Walk", true, 1f);
    }
    private void OnMoveLeftCanceled(InputAction.CallbackContext context)
    {
      movingLeft = false;
      SetAnimation("Idle", true, 1f);
    }

    // MoveRight event handlers
    private void OnMoveRightPerformed(InputAction.CallbackContext context)
    {
      movingRight = true;
      SetAnimation("Walk", true, 1f);
    }
    private void OnMoveRightCanceled(InputAction.CallbackContext context)
    {
      movingRight = false;
      SetAnimation("Idle", true, 1f);
    }

    // Jump event handlers
    private void OnJumpStarted(InputAction.CallbackContext context)
    {
      Debug.Log("Jump started");
      if (jumpState == JumpState.Grounded)
      {
        jumpState = JumpState.PrepareToJump;
        doubleJumpUsed = false;
        lastWallJumpSide = WallSide.None;
        return;
      }

        var wallDir = UpdateWallContact();
        WallSide currentWallSide = wallDir == -1 ? WallSide.Left : wallDir == 1 ? WallSide.Right : WallSide.None;

        // Wall jump: only if unlocked, player is near a wall, and from a different wall side than last time.
        if (wallJumpUnlocked && currentWallSide != WallSide.None && currentWallSide != lastWallJumpSide)
        {
            Debug.Log("Wall jump - Side: " + currentWallSide);
            lastWallJumpSide = currentWallSide;
            velocity.y = wallJumpVerticalSpeed * model.jumpModifier;

            // Push away from the wall.
            velocity.x = wallJumpHorizontalSpeed * (currentWallSide == WallSide.Left ? 1f : -1f);
            
            // Reset dash and double jump on wall jump.
            canDash = true;
            doubleJumpUsed = false;
        }
        else if (!doubleJumpUsed && jumpState == JumpState.InFlight)
        {
            // Normal double jump.
            doubleJumpUsed = true;
            velocity.y = jumpTakeOffSpeed * model.jumpModifier;
        }
    }

    private void OnJumpCanceled(InputAction.CallbackContext context)
    {
      stopJump = true;
      Schedule<PlayerStopJump>().player = this;
    }

    private void OnDashPerformed(InputAction.CallbackContext context)
    {
      if (!dashUnlocked)
      {
        return;
      }
      
      if (canDash && !isDashing)
      {
        StartCoroutine(PerformDash());
      }
    }

    void UpdateJumpState()
    {
      jump = false;
      switch (jumpState)
      {
        case JumpState.PrepareToJump:
          jumpState = JumpState.Jumping;
          jump = true;
          stopJump = false;
          break;
        case JumpState.Jumping:
          if (!IsGrounded)
          {
            Schedule<PlayerJumped>().player = this;
            jumpState = JumpState.InFlight;
          }
          break;
        case JumpState.InFlight:
          if (IsGrounded)
          {
            Schedule<PlayerLanded>().player = this;
            jumpState = JumpState.Landed;
          }
          break;
        case JumpState.Landed:
          jumpState = JumpState.Grounded;
          doubleJumpUsed = false;
          break;
      }
    }

    protected override void ComputeVelocity()
    {
      if (isDashing)
      {
        targetVelocity = new Vector2(velocity.x, 0);
        return;
      }

      if (jump && IsGrounded)
      {
        velocity.y = jumpTakeOffSpeed * model.jumpModifier;
        jump = false;
      }
      else if (stopJump)
      {
        stopJump = false;
        if (velocity.y > 0)
        {
          velocity.y = velocity.y * model.jumpDeceleration;
        }
      }

      // Wall slide: if in the air and touching a wall, clamp the downward speed.
      if (!IsGrounded && UpdateWallContact() != 0)
      {
        if (velocity.y < -wallSlideSpeed)
        {
          velocity.y = -wallSlideSpeed;
        }
      }

      if (move.x > 0.01f)
      {
        skeletonAnim.skeleton.ScaleX = -1f;

      }
      else if (move.x < -0.01f)
      {
        skeletonAnim.skeleton.ScaleX = 1f;
      }

      targetVelocity = move * maxSpeed;
    }

    private IEnumerator PerformDash()
    {
      isDashing = true;
      canDash = false;
      isInvulnerable = true;

      gameObject.layer = dashingLayer;
      contactFilter.SetLayerMask(Physics2D.GetLayerCollisionMask(dashingLayer));

      float dashDirection = 0f;
      if (movingRight)
      {
        dashDirection = 1f;
      }
      else if (movingLeft)
      {
        dashDirection = -1f;
      }
      else
      {
        dashDirection = spriteRenderer.flipX ? -1f : 1f;
      }

      velocity = new Vector2(dashSpeed * dashDirection, 0);

      yield return new WaitForSeconds(dashDuration);

      isDashing = false;
      isInvulnerable = false;

      gameObject.layer = playerLayer;
      contactFilter.SetLayerMask(Physics2D.GetLayerCollisionMask(playerLayer));

      yield return new WaitForSeconds(dashCooldown);
      canDash = true;
    }
  }
}
