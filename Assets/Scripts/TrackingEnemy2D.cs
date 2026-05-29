using UnityEngine;

public class TrackingEnemy2D : MonoBehaviour
{
    [Header("Target")]
    // Transform this enemy should move toward.
    public Transform target;
    // Tag used to find the player automatically.
    public string targetTag = "Player";

    [Header("Movement")]
    // Movement speed toward the target.
    public float moveSpeed = 3f;
    // Whether this enemy follows the target's X position.
    public bool trackX = true;
    // Whether this enemy follows the target's Y position.
    public bool trackY = true;
    // Distance at which the enemy stops moving toward the target.
    public float stopDistance = 0.15f;

    [Header("Damage")]
    // If true, touching the target counts as killing the player.
    public bool killPlayerOnTouch = true;
    // If true, killing the player freezes the game instead of disabling the player.
    public bool freezeTimeOnKill = true;

    // Optional Rigidbody2D used for physics-friendly movement.
    Rigidbody2D rb;

    void Awake()
    {
        // Cache the Rigidbody2D if one exists.
        rb = GetComponent<Rigidbody2D>();
    }

    void Start()
    {
        // Automatically find the player when no target was assigned.
        if (target == null)
        {
            GameObject targetObject = GameObject.FindGameObjectWithTag(targetTag);
            if (targetObject != null)
                target = targetObject.transform;
        }
    }

    void FixedUpdate()
    {
        // Do nothing until a target exists.
        if (target == null) return;

        // Use Rigidbody2D position when available so physics stays consistent.
        Vector2 current = rb != null ? rb.position : (Vector2)transform.position;
        Vector2 desired = current;

        // Copy only the axes this enemy is allowed to track.
        if (trackX)
            desired.x = target.position.x;

        if (trackY)
            desired.y = target.position.y;

        // Stop if the enemy is already close enough.
        Vector2 toTarget = desired - current;
        if (toTarget.sqrMagnitude <= stopDistance * stopDistance)
            return;

        // Move toward the desired target position at a fixed speed.
        Vector2 next = Vector2.MoveTowards(current, desired, moveSpeed * Time.fixedDeltaTime);

        // Move through physics when possible, otherwise move the transform.
        if (rb != null)
            rb.MovePosition(next);
        else
            transform.position = next;
    }

    void OnTriggerEnter2D(Collider2D other)
    {
        // Handle trigger-based contact with the player.
        TryKillPlayer(other.gameObject);
    }

    void OnCollisionEnter2D(Collision2D collision)
    {
        // Handle collision-based contact with the player.
        TryKillPlayer(collision.gameObject);
    }

    void TryKillPlayer(GameObject other)
    {
        // Respect the inspector toggle and only affect the configured target tag.
        if (!killPlayerOnTouch) return;
        if (!other.CompareTag(targetTag)) return;

        // Log the hit so it is visible during testing.
        Debug.Log("Player hit by tracking enemy");

        // Find the menu manager and show the death screen.
        GameFlowManager flow = FindObjectOfType<GameFlowManager>();
        if (flow != null)
            flow.ShowDeathScreen();
        else
            Time.timeScale = 0f;

        // If this enemy is configured not to freeze time, also hide the player object.
        if (!freezeTimeOnKill)
            other.SetActive(false);
    }
}
