using UnityEngine;

public class HomingProjectile2D : MonoBehaviour
{
    [Header("Tracking")]
    // Forward movement speed of the projectile.
    public float speed = 12f;
    // Maximum turning speed in degrees per second.
    public float turnSpeed = 540f;
    // Optional target this projectile should steer toward.
    public Transform target;

    // Cached Rigidbody2D; if missing, the projectile moves by transform instead.
    Rigidbody2D rb;
    // Current travel direction, updated as the projectile turns.
    Vector2 currentDirection = Vector2.right;

    void Awake()
    {
        // Cache physics body if the projectile prefab has one.
        rb = GetComponent<Rigidbody2D>();
    }

    public void Init(Vector2 direction, float moveSpeed, Transform targetTransform)
    {
        // Configure the projectile from the shooter at spawn time.
        speed = moveSpeed;
        target = targetTransform;
        // Fall back to firing right if the requested direction is too small.
        currentDirection = direction.sqrMagnitude > 0.0001f ? direction.normalized : Vector2.right;

        ApplyVelocity();
    }

    void FixedUpdate()
    {
        // If a target exists, turn gradually toward it instead of snapping.
        if (target != null)
        {
            Vector2 toTarget = ((Vector2)target.position - GetPosition()).normalized;
            if (toTarget.sqrMagnitude > 0.0001f)
            {
                float maxRadians = turnSpeed * Mathf.Deg2Rad * Time.fixedDeltaTime;
                currentDirection = Vector3.RotateTowards(currentDirection, toTarget, maxRadians, 0f).normalized;
            }
        }

        // Apply the current direction every physics step.
        ApplyVelocity();
    }

    void OnTriggerEnter2D(Collider2D other)
    {
        // Trigger hits are used by projectile colliders set as triggers.
        TryHitEnemy(other.gameObject);
    }

    void OnCollisionEnter2D(Collision2D collision)
    {
        // Collision hits are used by projectile colliders that are not triggers.
        TryHitEnemy(collision.gameObject);
    }

    void TryHitEnemy(GameObject other)
    {
        // Check the hit object and its parents for an enemy component.
        TrackingEnemy2D enemy = other.GetComponent<TrackingEnemy2D>();
        if (enemy == null)
            enemy = other.GetComponentInParent<TrackingEnemy2D>();

        // Ignore anything that is not a tracking enemy.
        if (enemy == null) return;

        // Destroy both the enemy and this projectile on hit.
        Destroy(enemy.gameObject);
        Destroy(gameObject);
    }

    void ApplyVelocity()
    {
        if (rb != null)
        {
            // Rigidbody movement keeps physics interactions consistent.
            rb.linearVelocity = currentDirection * speed;
            float angle = Mathf.Atan2(currentDirection.y, currentDirection.x) * Mathf.Rad2Deg;
            rb.MoveRotation(angle);
        }
        else
        {
            // Fallback movement for projectiles without a Rigidbody2D.
            transform.position += (Vector3)(currentDirection * speed * Time.fixedDeltaTime);
        }
    }

    Vector2 GetPosition()
    {
        // Use Rigidbody2D position when available because it matches physics state.
        return rb != null ? rb.position : (Vector2)transform.position;
    }
}
