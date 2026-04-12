using UnityEngine;

public class HomingProjectile2D : MonoBehaviour
{
    [Header("Tracking")]
    public float speed = 12f;
    public float turnSpeed = 540f;
    public Transform target;

    Rigidbody2D rb;
    Vector2 currentDirection = Vector2.right;

    void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
    }

    public void Init(Vector2 direction, float moveSpeed, Transform targetTransform)
    {
        speed = moveSpeed;
        target = targetTransform;
        currentDirection = direction.sqrMagnitude > 0.0001f ? direction.normalized : Vector2.right;

        ApplyVelocity();
    }

    void FixedUpdate()
    {
        if (target != null)
        {
            Vector2 toTarget = ((Vector2)target.position - GetPosition()).normalized;
            if (toTarget.sqrMagnitude > 0.0001f)
            {
                float maxRadians = turnSpeed * Mathf.Deg2Rad * Time.fixedDeltaTime;
                currentDirection = Vector3.RotateTowards(currentDirection, toTarget, maxRadians, 0f).normalized;
            }
        }

        ApplyVelocity();
    }

    void OnTriggerEnter2D(Collider2D other)
    {
        TryHitEnemy(other.gameObject);
    }

    void OnCollisionEnter2D(Collision2D collision)
    {
        TryHitEnemy(collision.gameObject);
    }

    void TryHitEnemy(GameObject other)
    {
        TrackingEnemy2D enemy = other.GetComponent<TrackingEnemy2D>();
        if (enemy == null)
            enemy = other.GetComponentInParent<TrackingEnemy2D>();

        if (enemy == null) return;

        Destroy(enemy.gameObject);
        Destroy(gameObject);
    }

    void ApplyVelocity()
    {
        if (rb != null)
        {
            rb.linearVelocity = currentDirection * speed;
            float angle = Mathf.Atan2(currentDirection.y, currentDirection.x) * Mathf.Rad2Deg;
            rb.MoveRotation(angle);
        }
        else
        {
            transform.position += (Vector3)(currentDirection * speed * Time.fixedDeltaTime);
        }
    }

    Vector2 GetPosition()
    {
        return rb != null ? rb.position : (Vector2)transform.position;
    }
}
