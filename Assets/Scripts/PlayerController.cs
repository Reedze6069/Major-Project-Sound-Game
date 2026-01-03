using UnityEngine;

public class PlayerController : MonoBehaviour
{
    [Header("Movement")]
    public float runSpeed = 5f;

    [Header("Jump")]
    public float jumpForce = 8f;
    public Transform groundCheck;
    public float groundCheckRadius = 0.15f;
    public LayerMask groundLayer;

    [Header("Crouch")]
    public Vector2 standingColliderSize = new Vector2(1f, 1.5f);
    public Vector2 crouchingColliderSize = new Vector2(1f, 0.8f);
    private bool isCrouching;

    [Tooltip("Adjust so the bottom of the standing collider stays on the ground (usually size.y / 2 if pivot is centered).")]
    public Vector2 standingColliderOffset = new Vector2(0f, 0.75f);

    [Tooltip("Adjust so the bottom of the crouching collider stays on the ground (usually size.y / 2 if pivot is centered).")]
    public Vector2 crouchingColliderOffset = new Vector2(0f, 0.4f);

    [Header("Shoot")]
    public GameObject bulletPrefab;
    public Transform firePoint;
    public float bulletSpeed = 12f;

    private Rigidbody2D rb;
    private BoxCollider2D col;

    void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
        col = GetComponent<BoxCollider2D>();
    }

    void FixedUpdate()
    {
        // Auto-run forward (keep Y velocity for gravity/jumps)
        rb.linearVelocity = new Vector2(runSpeed, rb.linearVelocity.y);
    }

    public void Jump()
    {
        if (!IsGrounded()) return;

        // reset vertical velocity before applying impulse for consistent jump height
        rb.linearVelocity = new Vector2(rb.linearVelocity.x, 0f);
        rb.AddForce(Vector2.up * jumpForce, ForceMode2D.Impulse);
    }

    public void SetCrouch(bool crouch)
    {
        if (col == null) return;
        if (isCrouching == crouch) return; // prevents repeated resizing every frame
        isCrouching = crouch;

        // Preserve bottom position so the player doesn't pop up/down
        float bottomBefore = transform.position.y + col.offset.y - (col.size.y * 0.5f);

        col.size = crouch ? crouchingColliderSize : standingColliderSize;

        float bottomAfterWithoutFix = transform.position.y + col.offset.y - (col.size.y * 0.5f);
        float delta = bottomBefore - bottomAfterWithoutFix;

        col.offset = new Vector2(col.offset.x, col.offset.y + delta);
    }

    // ✅ NEW: used by the spacebar-confirm system
    public void ToggleCrouch()
    {
        SetCrouch(!isCrouching);
    }

    // (Optional) If you want to force stand from other scripts
    public void StandUp()
    {
        SetCrouch(false);
    }

    public void Shoot()
    {
        if (bulletPrefab == null || firePoint == null) return;

        GameObject bullet = Instantiate(bulletPrefab, firePoint.position, Quaternion.identity);

        Rigidbody2D bulletRb = bullet.GetComponent<Rigidbody2D>();
        if (bulletRb != null)
        {
            bulletRb.gravityScale = 0f;
            bulletRb.linearVelocity = Vector2.right * bulletSpeed;
        }

        Destroy(bullet, 2f);
    }

    bool IsGrounded()
    {
        if (groundCheck == null) return false;

        return Physics2D.OverlapCircle(
            groundCheck.position,
            groundCheckRadius,
            groundLayer
        );
    }

#if UNITY_EDITOR
    void OnDrawGizmosSelected()
    {
        if (groundCheck == null) return;
        Gizmos.DrawWireSphere(groundCheck.position, groundCheckRadius);
    }
#endif
}
