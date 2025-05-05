using UnityEngine;

public class EnemyBase : MonoBehaviourBase
{
    [Header("Movement Settings")]
    public float moveSpeed = 2f;
    public Transform groundCheck;
    public Transform wallCheck;
    public float checkDistance = 0.1f;
    public LayerMask groundLayer;

    private Rigidbody2D rb;
    private bool movingRight = true;

    private void Start()
    {
        rb = GetComponent<Rigidbody2D>();
    }

    private void Update()
    {
        Patrol();
    }

    private void Patrol()
    {
        // Move the enemy in the current direction
        rb.linearVelocity = new Vector2((movingRight ? 1 : -1) * moveSpeed, rb.linearVelocity.y);

        // Ground check to prevent falling off edges
        bool isGrounded = Physics2D.Raycast(groundCheck.position, Vector2.down, checkDistance, groundLayer);

        // Wall check to detect obstacles
        bool isHittingWall = Physics2D.Raycast(wallCheck.position, movingRight ? Vector2.right : Vector2.left, checkDistance, groundLayer);

        // Flip direction if there's no ground or a wall ahead
        if (!isGrounded || isHittingWall)
        {
            Flip();
        }
    }

    private void Flip()
    {
        movingRight = !movingRight;
        Vector3 scale = transform.localScale;
        scale.x *= -1;
        transform.localScale = scale;
    }

    protected override void DrawGizmosSafe()
    {
        if (groundCheck != null)
        {
            Gizmos.color = Color.red;
            Gizmos.DrawLine(groundCheck.position, groundCheck.position + Vector3.down * checkDistance);
        }

        if (wallCheck != null)
        {
            Gizmos.color = Color.blue;
            Vector3 dir = transform.localScale.x > 0 ? Vector3.right : Vector3.left;
            Gizmos.DrawLine(wallCheck.position, wallCheck.position + dir * checkDistance);
        }
    }
}
