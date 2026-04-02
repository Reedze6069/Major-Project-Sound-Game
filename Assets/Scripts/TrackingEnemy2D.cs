using UnityEngine;

public class TrackingEnemy2D : MonoBehaviour
{
    [Header("Target")]
    public Transform target;
    public string targetTag = "Player";

    [Header("Movement")]
    public float moveSpeed = 3f;
    public bool trackX = true;
    public bool trackY = true;
    public float stopDistance = 0.15f;

    [Header("Damage")]
    public bool killPlayerOnTouch = true;
    public bool freezeTimeOnKill = true;

    Rigidbody2D rb;

    void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
    }

    void Start()
    {
        if (target == null)
        {
            GameObject targetObject = GameObject.FindGameObjectWithTag(targetTag);
            if (targetObject != null)
                target = targetObject.transform;
        }
    }

    void FixedUpdate()
    {
        if (target == null) return;

        Vector2 current = rb != null ? rb.position : (Vector2)transform.position;
        Vector2 desired = current;

        if (trackX)
            desired.x = target.position.x;

        if (trackY)
            desired.y = target.position.y;

        Vector2 toTarget = desired - current;
        if (toTarget.sqrMagnitude <= stopDistance * stopDistance)
            return;

        Vector2 next = Vector2.MoveTowards(current, desired, moveSpeed * Time.fixedDeltaTime);

        if (rb != null)
            rb.MovePosition(next);
        else
            transform.position = next;
    }

    void OnTriggerEnter2D(Collider2D other)
    {
        TryKillPlayer(other.gameObject);
    }

    void OnCollisionEnter2D(Collision2D collision)
    {
        TryKillPlayer(collision.gameObject);
    }

    void TryKillPlayer(GameObject other)
    {
        if (!killPlayerOnTouch) return;
        if (!other.CompareTag(targetTag)) return;

        Debug.Log("Player hit by tracking enemy");

        if (freezeTimeOnKill)
            Time.timeScale = 0f;
        else
            other.SetActive(false);
    }
}
