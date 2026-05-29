using UnityEngine;

public class SoundWaveVisual : MonoBehaviour
{
    // Time in seconds before this wave destroys itself.
    public float life = 0.6f;
    // Scale used when the wave first appears.
    public float startScale = 0.5f;
    // Scale reached at the end of the wave lifetime.
    public float endScale = 6f;

    // Renderer used to tint and fade the wave.
    SpriteRenderer sr;
    // Elapsed lifetime timer.
    float t;

    void Awake()
    {
        // Cache the renderer and start at the configured small scale.
        sr = GetComponent<SpriteRenderer>();
        transform.localScale = Vector3.one * startScale;
    }

    public void Init(Color c, float endScaleOverride, float lifeOverride)
    {
        // Allow the spawner to set color, final size, and lifetime.
        if (sr != null) sr.color = c;
        endScale = endScaleOverride;
        life = lifeOverride;
    }

    void Update()
    {
        // Convert elapsed time into normalized lifetime progress.
        t += Time.deltaTime;
        float p = Mathf.Clamp01(t / life);

        // Expand evenly from the start scale to the end scale.
        float s = Mathf.Lerp(startScale, endScale, p);
        transform.localScale = new Vector3(s, s, 1f);

        // Fade out while the wave expands.
        if (sr != null)
        {
            var c = sr.color;
            c.a = Mathf.Lerp(1f, 0f, p);
            sr.color = c;
        }

        // Remove the wave after its lifetime ends.
        if (t >= life) Destroy(gameObject);
    }
}
