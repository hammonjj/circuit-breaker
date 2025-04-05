using System.Collections;
using UnityEngine;
using UnityEngine.InputSystem;
using Platformer.Gameplay;
using static Platformer.Core.Simulation;
using Platformer.Model;
using Platformer.Core;
using Unity.IO.LowLevel.Unsafe;

namespace Platformer.Mechanics
{
    /// <summary>
    /// This is the main class used to implement control of the player.
    /// It is a superset of the AnimationController class, but is inlined to allow for any kind of customisation.
    /// </summary>
    public class PlayerController : KinematicObject
    {
        public AudioClip jumpAudio;
        public AudioClip respawnAudio;
        public AudioClip ouchAudio;

        /// <summary>
        /// Max horizontal speed of the player.
        /// </summary>
        public float maxSpeed = 7;
        /// <summary>
        /// Initial jump velocity at the start of a jump.
        /// </summary>
        public float jumpTakeOffSpeed = 7;

        public JumpState jumpState = JumpState.Grounded;
        private bool stopJump;
        public Collider2D collider2d;
        public AudioSource audioSource;
        public Health health;
        public bool controlEnabled = true;

        // Movement booleans set by MoveLeft/MoveRight actions.
        private bool movingLeft;
        private bool movingRight;

        // Double Jump: Allow one extra jump per air time.
        private bool doubleJumpUsed = false;

        // Dash variables
        public float dashSpeed = 20f;
        public float dashDuration = 0.2f;
        public float dashCooldown = 1.0f;
        private bool isDashing = false;
        private bool canDash = true;
        // During a dash the player is invulnerable to enemy attacks.
        public bool isInvulnerable = false;

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

        void Awake()
        {
            health = GetComponent<Health>();
            audioSource = GetComponent<AudioSource>();
            collider2d = GetComponent<Collider2D>();
            spriteRenderer = GetComponent<SpriteRenderer>();
            animator = GetComponent<Animator>();

            playerLayer = LayerMask.NameToLayer("Player");
            dashingLayer = LayerMask.NameToLayer("DashingPlayer");
            
        }

        protected override void OnEnable()
        {
            base.OnEnable();

            // Enable and subscribe to MoveLeft
            if (moveLeftActionReference != null)
            {
                moveLeftActionReference.action.Enable();
                moveLeftActionReference.action.performed += OnMoveLeftPerformed;
                moveLeftActionReference.action.canceled += OnMoveLeftCanceled;
            }

            // Enable and subscribe to MoveRight
            if (moveRightActionReference != null)
            {
                moveRightActionReference.action.Enable();
                moveRightActionReference.action.performed += OnMoveRightPerformed;
                moveRightActionReference.action.canceled += OnMoveRightCanceled;
            }

            // Enable and subscribe to Jump
            if (jumpActionReference != null)
            {
                jumpActionReference.action.Enable();
                jumpActionReference.action.started += OnJumpStarted;
                jumpActionReference.action.canceled += OnJumpCanceled;
            }

            // Enable and subscribe to Dash
            if (dashActionReference != null)
            {
                dashActionReference.action.Enable();
                dashActionReference.action.performed += OnDashPerformed;
            }
        }

        protected override void OnDisable()
        {
            base.OnDisable();

            // Unsubscribe and disable MoveLeft
            if (moveLeftActionReference != null)
            {
                moveLeftActionReference.action.performed -= OnMoveLeftPerformed;
                moveLeftActionReference.action.canceled -= OnMoveLeftCanceled;
                moveLeftActionReference.action.Disable();
            }

            // Unsubscribe and disable MoveRight
            if (moveRightActionReference != null)
            {
                moveRightActionReference.action.performed -= OnMoveRightPerformed;
                moveRightActionReference.action.canceled -= OnMoveRightCanceled;
                moveRightActionReference.action.Disable();
            }

            // Unsubscribe and disable Jump
            if (jumpActionReference != null)
            {
                jumpActionReference.action.started -= OnJumpStarted;
                jumpActionReference.action.canceled -= OnJumpCanceled;
                jumpActionReference.action.Disable();
            }

            // Unsubscribe and disable Dash
            if (dashActionReference != null)
            {
                dashActionReference.action.performed -= OnDashPerformed;
                dashActionReference.action.Disable();
            }
        }

        protected override void Update()
        {
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
            // Combine MoveLeft/MoveRight booleans into a single horizontal movement value.
            move.x = 0f;
            if (movingLeft) move.x -= 1f;
            if (movingRight) move.x += 1f;
        }

        // MoveLeft event handlers
        private void OnMoveLeftPerformed(InputAction.CallbackContext context)
        {
            movingLeft = true;
        }
        private void OnMoveLeftCanceled(InputAction.CallbackContext context)
        {
            movingLeft = false;
        }

        // MoveRight event handlers
        private void OnMoveRightPerformed(InputAction.CallbackContext context)
        {
            movingRight = true;
        }
        private void OnMoveRightCanceled(InputAction.CallbackContext context)
        {
            movingRight = false;
        }

        // Jump event handlers
        private void OnJumpStarted(InputAction.CallbackContext context)
        {
            if (jumpState == JumpState.Grounded)
            {
                jumpState = JumpState.PrepareToJump;
                doubleJumpUsed = false;
            }
            else if (!doubleJumpUsed && jumpState == JumpState.InFlight)
            {
                // Double jump: Reset vertical velocity and apply jump takeoff speed.
                doubleJumpUsed = true;
                velocity.y = jumpTakeOffSpeed * model.jumpModifier;
            }
        }
        private void OnJumpCanceled(InputAction.CallbackContext context)
        {
            stopJump = true;
            Schedule<PlayerStopJump>().player = this;
        }

        // Dash event handler
        private void OnDashPerformed(InputAction.CallbackContext context)
        {
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
                    doubleJumpUsed = false; // Reset double jump when the player lands.
                    break;
            }
        }

        protected override void ComputeVelocity()
        {
            if (isDashing)
            {
                // During a dash, override the normal velocity computation.
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

            if (move.x > 0.01f)
                spriteRenderer.flipX = false;
            else if (move.x < -0.01f)
                spriteRenderer.flipX = true;

            animator.SetBool("grounded", IsGrounded);
            animator.SetFloat("velocityX", Mathf.Abs(velocity.x) / maxSpeed);

            targetVelocity = move * maxSpeed;
        }

        private IEnumerator PerformDash()
        {
            isDashing = true;
            canDash = false;
            isInvulnerable = true;

            gameObject.layer = dashingLayer;
            contactFilter.SetLayerMask(Physics2D.GetLayerCollisionMask(dashingLayer));

            // Determine dash direction using horizontal input or the current facing.
            float dashDirection = 0f;
            if (movingRight) dashDirection = 1f;
            else if (movingLeft) dashDirection = -1f;
            else dashDirection = spriteRenderer.flipX ? -1f : 1f;

            velocity = new Vector2(dashSpeed * dashDirection, 0);

            // Optionally, trigger dash animation or effects here.
            yield return new WaitForSeconds(dashDuration);

            isDashing = false;
            isInvulnerable = false;

            gameObject.layer = playerLayer;
            contactFilter.SetLayerMask(Physics2D.GetLayerCollisionMask(playerLayer));

            yield return new WaitForSeconds(dashCooldown);
            canDash = true;
        }

        public enum JumpState
        {
            Grounded,
            PrepareToJump,
            Jumping,
            InFlight,
            Landed
        }
    }
}
