using UnityEngine;
using UnityEngine.Serialization;

public class PlayerController : MonoBehaviour
{
    // Forward auto-run speed.
    [FormerlySerializedAs("runSpeed")] [Header("Movement")]
    public float runspeed = 5f;

    [Header("Jump")]
    // Upward impulse applied when the player jumps.
    public float jumpForce = 8f;
    // Transform placed at the player's feet for ground detection.
    public Transform groundCheck;
    // Radius used by the ground overlap check.
    public float groundCheckRadius = 0.15f;
    // Layers that count as ground.
    public LayerMask groundLayer;

    [Header("Crouch (Collider)")]
    // Collider size used while standing.
    public Vector2 standingColliderSize = new Vector2(1f, 1.5f);
    // Collider size used while crouching.
    public Vector2 crouchingColliderSize = new Vector2(1f, 0.8f);

    [Header("Crouch (Visual)")]
    [Tooltip("Makes the cube visually crouch too. 1 = normal height, 0.6-0.8 = crouched height.")]
    // Y scale multiplier applied while crouching.
    public float crouchVisualYScale = 0.65f;

    // Tracks the current crouch state.
    private bool isCrouching;

    [Header("Shoot")]
    // Projectile prefab spawned when shooting.
    public GameObject bulletPrefab;
    // Position bullets spawn from.
    public Transform firePoint;
    // Initial speed assigned to spawned bullets.
    public float bulletSpeed = 12f;
    // Maximum angle from the aim direction that a homing target can be locked.
    public float homingLockAngle = 18f;
    // Maximum distance a homing target can be locked.
    public float homingLockRange = 18f;

    // Cached physics body used for movement and jumping.
    private Rigidbody2D rb;
    // Box collider support for crouching if the player uses a BoxCollider2D.
    private BoxCollider2D boxCol;
    // Capsule collider support for crouching if the player uses a CapsuleCollider2D.
    private CapsuleCollider2D capsuleCol;
    // Optional component that plays attack visuals when shooting.
    private PlayerAttackVisuals attackVisuals;

    // Original transform scale restored when standing.
    private Vector3 originalScale;
    // Standing collider offset captured at startup.
    private Vector2 standingColliderOffset;
    // Crouching offset calculated so the collider bottom stays planted.
    private Vector2 crouchingColliderOffset;
    // Visual scale used while crouching.
    private Vector3 crouchingScale;

    void Awake()
    {
        // Cache components needed by movement, crouching, and attack visuals.
        rb = GetComponent<Rigidbody2D>();
        capsuleCol = GetComponent<CapsuleCollider2D>();
        boxCol = capsuleCol == null ? GetComponent<BoxCollider2D>() : null;
        attackVisuals = GetComponent<PlayerAttackVisuals>();
        originalScale = transform.localScale;

        // Read the starting collider settings so crouch can restore them later.
        if (HasSupportedCollider())
        {
            standingColliderSize = GetColliderSize();
            standingColliderOffset = GetColliderOffset();
            crouchingColliderOffset = CalculateBottomAnchoredOffset(crouchingColliderSize);
        }

        // Precalculate the visual crouch scale from the original scale.
        crouchingScale = new Vector3(
            originalScale.x,
            originalScale.y * crouchVisualYScale,
            originalScale.z);

        // Start in the standing state.
        ApplyCrouchState(false);
    }

    void FixedUpdate()
    {
        // Auto-run forward (keep Y velocity for gravity/jumps)
        rb.linearVelocity = new Vector2(runspeed, rb.linearVelocity.y);
    }
    public void Jump()
    {
        // Only allow jumping while grounded.
        if (!IsGrounded()) return;

        // Reset vertical velocity before applying impulse for consistent jump height
        rb.linearVelocity = new Vector2(rb.linearVelocity.x, 0f);
        rb.AddForce(Vector2.up * jumpForce, ForceMode2D.Impulse);
    }

    public void SetCrouch(bool crouch)
    {
        // Avoid reapplying crouch settings when the state did not change.
        if (isCrouching == crouch) return;

        isCrouching = crouch;
        ApplyCrouchState(crouch);
    }

    public void ToggleCrouch()
    {
        // Switch between standing and crouching.
        SetCrouch(!isCrouching);
    }

    public void StandUp()
    {
        // Force the player back to standing.
        SetCrouch(false);
    }

    public void Shoot()
    {
        // Default shooting direction is straight right.
        Shoot(Vector2.right);
    }

    public void Shoot(Vector2 direction)
    {
        // Cannot shoot without a projectile prefab and spawn point.
        if (bulletPrefab == null || firePoint == null) return;

        // Spawn the projectile and normalize the requested aim direction.
        GameObject bullet = Instantiate(bulletPrefab, firePoint.position, Quaternion.identity);
        Vector2 shootDirection = direction.sqrMagnitude > 0.0001f ? direction.normalized : Vector2.right;
        // Find the best enemy to home toward within the aim cone.
        Transform target = FindBestEnemyTarget(firePoint.position, shootDirection);
        // Play the matching attack animation if the visual component exists.
        attackVisuals?.PlayShoot(shootDirection);

        // Give the bullet immediate velocity if it has a Rigidbody2D.
        Rigidbody2D bulletRb = bullet.GetComponent<Rigidbody2D>();
        if (bulletRb != null)
        {
            bulletRb.gravityScale = 0f;
            bulletRb.linearVelocity = shootDirection * bulletSpeed;
        }

        // Ensure the projectile has homing behavior and initialize it.
        HomingProjectile2D homing = bullet.GetComponent<HomingProjectile2D>();
        if (homing == null)
            homing = bullet.AddComponent<HomingProjectile2D>();

        homing.Init(shootDirection, bulletSpeed, target);

        // Clean up bullets that do not hit anything.
        Destroy(bullet, 2f);
    }

    Transform FindBestEnemyTarget(Vector2 origin, Vector2 direction)
    {
        // Search all tracking enemies and choose the closest valid target.
        TrackingEnemy2D[] enemies = FindObjectsOfType<TrackingEnemy2D>();
        TrackingEnemy2D bestEnemy = null;
        float bestDistance = float.MaxValue;

        for (int i = 0; i < enemies.Length; i++)
        {
            // Skip destroyed or inactive enemies.
            if (enemies[i] == null || !enemies[i].gameObject.activeInHierarchy)
                continue;

            // Ignore enemies outside the homing range.
            Vector2 toEnemy = (Vector2)enemies[i].transform.position - origin;
            float distance = toEnemy.magnitude;
            if (distance > homingLockRange || distance <= 0.001f)
                continue;

            // Ignore enemies outside the aim cone.
            float angle = Vector2.Angle(direction, toEnemy.normalized);
            if (angle > homingLockAngle)
                continue;

            // Keep the closest enemy that passed the range and angle checks.
            if (distance < bestDistance)
            {
                bestDistance = distance;
                bestEnemy = enemies[i];
            }
        }

        // Return the selected enemy transform or null when nothing was valid.
        return bestEnemy != null ? bestEnemy.transform : null;
    }

    bool IsGrounded()
    {
        // Without a ground check transform, the player cannot confirm grounding.
        if (groundCheck == null) return false;

        // This is fine as long as GroundCheck is at the feet.
        return Physics2D.OverlapCircle(groundCheck.position, groundCheckRadius, groundLayer);
    }

    private void ApplyCrouchState(bool crouch)
    {
        // Change collider dimensions so crouching affects collisions.
        if (HasSupportedCollider())
        {
            SetColliderSize(crouch ? crouchingColliderSize : standingColliderSize);
            SetColliderOffset(crouch ? crouchingColliderOffset : standingColliderOffset);
        }

        // Change the visual height to match the crouch state.
        transform.localScale = crouch ? crouchingScale : originalScale;
    }

    private Vector2 CalculateBottomAnchoredOffset(Vector2 targetSize)
    {
        // Keep the collider bottom at the same world height while resizing.
        float standingBottom = standingColliderOffset.y - (standingColliderSize.y * 0.5f);
        float targetOffsetY = standingBottom + (targetSize.y * 0.5f);
        return new Vector2(standingColliderOffset.x, targetOffsetY);
    }

    private bool HasSupportedCollider()
    {
        // Crouch supports either capsule or box colliders.
        return capsuleCol != null || boxCol != null;
    }

    private Vector2 GetColliderOffset()
    {
        // Read the active supported collider offset.
        if (capsuleCol != null) return capsuleCol.offset;
        if (boxCol != null) return boxCol.offset;
        return Vector2.zero;
    }

    private Vector2 GetColliderSize()
    {
        // Read the active supported collider size.
        if (capsuleCol != null) return capsuleCol.size;
        if (boxCol != null) return boxCol.size;
        return Vector2.zero;
    }

    private void SetColliderOffset(Vector2 offset)
    {
        // Apply offset to whichever supported collider is present.
        if (capsuleCol != null)
        {
            capsuleCol.offset = offset;
            return;
        }

        if (boxCol != null)
        {
            boxCol.offset = offset;
        }
    }

    private void SetColliderSize(Vector2 size)
    {
        // Apply size to whichever supported collider is present.
        if (capsuleCol != null)
        {
            capsuleCol.size = size;
            return;
        }

        if (boxCol != null)
        {
            boxCol.size = size;
        }
    }

#if UNITY_EDITOR
    void OnDrawGizmosSelected()
    {
        // Draw the ground check radius in the Scene view for easier setup.
        if (groundCheck == null) return;
        Gizmos.DrawWireSphere(groundCheck.position, groundCheckRadius);
    }
#endif
    
}
