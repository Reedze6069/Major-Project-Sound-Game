using UnityEngine;
using UnityEngine.Serialization;

public class PlayerController : MonoBehaviour
{
    [FormerlySerializedAs("runSpeed")] [Header("Movement")]
    public float runspeed = 5f;

    [Header("Jump")]
    public float jumpForce = 8f;
    public Transform groundCheck;
    public float groundCheckRadius = 0.15f;
    public LayerMask groundLayer;

    [Header("Crouch (Collider)")]
    public Vector2 standingColliderSize = new Vector2(1f, 1.5f);
    public Vector2 crouchingColliderSize = new Vector2(1f, 0.8f);

    [Header("Crouch (Visual)")]
    [Tooltip("Makes the cube visually crouch too. 1 = normal height, 0.6-0.8 = crouched height.")]
    public float crouchVisualYScale = 0.65f;

    private bool isCrouching;

    [Header("Shoot")]
    public GameObject bulletPrefab;
    public Transform firePoint;
    public float bulletSpeed = 12f;
    public float homingLockAngle = 18f;
    public float homingLockRange = 18f;

    private Rigidbody2D rb;
    private BoxCollider2D col;

    private Vector3 originalScale;

    void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
        col = GetComponent<BoxCollider2D>();
        originalScale = transform.localScale;
    }

    void FixedUpdate()
    {
        // Auto-run forward (keep Y velocity for gravity/jumps)
        rb.linearVelocity = new Vector2(runspeed, rb.linearVelocity.y);
    }
    public void Jump()
    {
        if (!IsGrounded()) return;

        // Reset vertical velocity before applying impulse for consistent jump height
        rb.linearVelocity = new Vector2(rb.linearVelocity.x, 0f);
        rb.AddForce(Vector2.up * jumpForce, ForceMode2D.Impulse);
    }

    public void SetCrouch(bool crouch)
    {
        Debug.Log($"[PlayerController] SetCrouch({crouch}) called\n{UnityEngine.StackTraceUtility.ExtractStackTrace()}");

        if (col == null) return;
        if (isCrouching == crouch) return;

        isCrouching = crouch;

        // Preserve bottom position so the player doesn't pop up/down
        float bottomBefore = transform.position.y + col.offset.y - (col.size.y * 0.5f);

        // Collider resize
        col.size = crouch ? crouchingColliderSize : standingColliderSize;

        float bottomAfter = transform.position.y + col.offset.y - (col.size.y * 0.5f);
        float delta = bottomBefore - bottomAfter;

        col.offset = new Vector2(col.offset.x, col.offset.y + delta);

        // Visual crouch (so your cube actually LOOKS like it's crouching)
        if (crouch)
            transform.localScale = new Vector3(originalScale.x, originalScale.y * crouchVisualYScale, originalScale.z);
        else
            transform.localScale = originalScale;
    }

    public void ToggleCrouch()
    {
        SetCrouch(!isCrouching);
    }

    public void StandUp()
    {
        SetCrouch(false);
    }

    public void Shoot()
    {
        Shoot(Vector2.right);
    }

    public void Shoot(Vector2 direction)
    {
        if (bulletPrefab == null || firePoint == null) return;

        GameObject bullet = Instantiate(bulletPrefab, firePoint.position, Quaternion.identity);
        Vector2 shootDirection = direction.sqrMagnitude > 0.0001f ? direction.normalized : Vector2.right;
        Transform target = FindBestEnemyTarget(firePoint.position, shootDirection);

        Rigidbody2D bulletRb = bullet.GetComponent<Rigidbody2D>();
        if (bulletRb != null)
        {
            bulletRb.gravityScale = 0f;
            bulletRb.linearVelocity = shootDirection * bulletSpeed;
        }

        HomingProjectile2D homing = bullet.GetComponent<HomingProjectile2D>();
        if (homing == null)
            homing = bullet.AddComponent<HomingProjectile2D>();

        homing.Init(shootDirection, bulletSpeed, target);

        Destroy(bullet, 2f);
    }

    Transform FindBestEnemyTarget(Vector2 origin, Vector2 direction)
    {
        TrackingEnemy2D[] enemies = FindObjectsOfType<TrackingEnemy2D>();
        TrackingEnemy2D bestEnemy = null;
        float bestDistance = float.MaxValue;

        for (int i = 0; i < enemies.Length; i++)
        {
            if (enemies[i] == null || !enemies[i].gameObject.activeInHierarchy)
                continue;

            Vector2 toEnemy = (Vector2)enemies[i].transform.position - origin;
            float distance = toEnemy.magnitude;
            if (distance > homingLockRange || distance <= 0.001f)
                continue;

            float angle = Vector2.Angle(direction, toEnemy.normalized);
            if (angle > homingLockAngle)
                continue;

            if (distance < bestDistance)
            {
                bestDistance = distance;
                bestEnemy = enemies[i];
            }
        }

        return bestEnemy != null ? bestEnemy.transform : null;
    }

    bool IsGrounded()
    {
        if (groundCheck == null) return false;

        // This is fine as long as GroundCheck is at the feet.
        return Physics2D.OverlapCircle(groundCheck.position, groundCheckRadius, groundLayer);
    }

#if UNITY_EDITOR
    void OnDrawGizmosSelected()
    {
        if (groundCheck == null) return;
        Gizmos.DrawWireSphere(groundCheck.position, groundCheckRadius);
    }
#endif
    
}
