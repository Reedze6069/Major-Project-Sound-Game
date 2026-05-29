using UnityEngine;

[RequireComponent(typeof(SpriteRenderer))]
public class SoundWaveForward : MonoBehaviour
{
    // Forward movement speed of the wave.
    public float speed = 10f;
    // Time in seconds before this wave destroys itself.
    public float life = 1.2f;

    [Header("Arc")]
    public float arcHeight = 1.2f;   // How tall the arc motion is.
    public float arcSpeed = 1f;      // How quickly the sine arc reaches its peak.

    [Header("Scale")]
    public float growX = 1.5f;       // Forward stretch added over lifetime.
    public float growY = 0.6f;       // Vertical spread added over lifetime.

    // Renderer used to tint and fade the wave sprite.
    SpriteRenderer sr;
    // Spawn position used as the origin for forward movement.
    Vector3 startPos;
    // Original scale used as the base for growth.
    Vector3 startScale;
    // Elapsed lifetime timer.
    float t;

    void Awake()
    {
        // Cache starting values before the wave begins moving.
        sr = GetComponent<SpriteRenderer>();
        startPos = transform.position;
        startScale = transform.localScale;
    }

    public void Init(Color color, float f, float speedOverride, float lifeOverride)
    {
        // Allow the spawner to set color, speed, and lifetime.
        if (sr != null) sr.color = color;
        speed = speedOverride;
        life = lifeOverride;
    }

    void Update()
    {
        // Track lifetime progress from 0 to 1.
        t += Time.deltaTime;
        float p = Mathf.Clamp01(t / life);

        // Move forward over time.
        float x = speed * t;

        // Add a sine-wave arc so the wave rises and falls smoothly.
        float y = Mathf.Sin(p * Mathf.PI * arcSpeed) * arcHeight;

        transform.position = startPos + new Vector3(x, y, 0f);

        // Stretch the sprite as it travels outward.
        transform.localScale = new Vector3(
            startScale.x * (1f + growX * p),
            startScale.y * (1f + growY * p),
            startScale.z
        );

        // Fade out as the wave reaches the end of its lifetime.
        if (sr != null)
        {
            Color c = sr.color;
            c.a = Mathf.Lerp(1f, 0f, p);
            sr.color = c;
        }

        // Destroy the visual when it has finished.
        if (t >= life)
            Destroy(gameObject);
    }
}
