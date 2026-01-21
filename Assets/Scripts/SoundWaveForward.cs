using UnityEngine;

[RequireComponent(typeof(SpriteRenderer))]
public class SoundWaveForward : MonoBehaviour
{
    public float speed = 10f;
    public float life = 1.2f;

    [Header("Arc")]
    public float arcHeight = 1.2f;   // how tall the arc is
    public float arcSpeed = 1f;      // how fast it curves

    [Header("Scale")]
    public float growX = 1.5f;       // forward stretch
    public float growY = 0.6f;       // vertical spread

    SpriteRenderer sr;
    Vector3 startPos;
    Vector3 startScale;
    float t;

    void Awake()
    {
        sr = GetComponent<SpriteRenderer>();
        startPos = transform.position;
        startScale = transform.localScale;
    }

    public void Init(Color color, float f, float speedOverride, float lifeOverride)
    {
        if (sr != null) sr.color = color;
        speed = speedOverride;
        life = lifeOverride;
    }

    void Update()
    {
        t += Time.deltaTime;
        float p = Mathf.Clamp01(t / life);

        // Forward motion
        float x = speed * t;

        // Arc motion (sine wave)
        float y = Mathf.Sin(p * Mathf.PI * arcSpeed) * arcHeight;

        transform.position = startPos + new Vector3(x, y, 0f);

        // Spread / stretch
        transform.localScale = new Vector3(
            startScale.x * (1f + growX * p),
            startScale.y * (1f + growY * p),
            startScale.z
        );

        // Fade out
        if (sr != null)
        {
            Color c = sr.color;
            c.a = Mathf.Lerp(1f, 0f, p);
            sr.color = c;
        }

        if (t >= life)
            Destroy(gameObject);
    }
}