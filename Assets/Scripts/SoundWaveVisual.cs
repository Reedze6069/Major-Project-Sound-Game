using UnityEngine;

public class SoundWaveVisual : MonoBehaviour
{
    public float life = 0.6f;
    public float startScale = 0.5f;
    public float endScale = 6f;

    SpriteRenderer sr;
    float t;

    void Awake()
    {
        sr = GetComponent<SpriteRenderer>();
        transform.localScale = Vector3.one * startScale;
    }

    public void Init(Color c, float endScaleOverride, float lifeOverride)
    {
        if (sr != null) sr.color = c;
        endScale = endScaleOverride;
        life = lifeOverride;
    }

    void Update()
    {
        t += Time.deltaTime;
        float p = Mathf.Clamp01(t / life);

        float s = Mathf.Lerp(startScale, endScale, p);
        transform.localScale = new Vector3(s, s, 1f);

        if (sr != null)
        {
            var c = sr.color;
            c.a = Mathf.Lerp(1f, 0f, p);
            sr.color = c;
        }

        if (t >= life) Destroy(gameObject);
    }
}