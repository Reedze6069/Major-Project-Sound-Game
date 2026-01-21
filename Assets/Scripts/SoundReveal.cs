using UnityEngine;

[RequireComponent(typeof(SpriteRenderer))]
public class SoundReveal : MonoBehaviour
{
    public float fadeOutSpeed = 6f;     // higher = fades faster
    public float maxVisibleTime = 0.5f; // how long it stays visible before fading

    SpriteRenderer sr;
    float timer;

    void Awake()
    {
        sr = GetComponent<SpriteRenderer>();
        SetAlpha(0f); // start hidden
    }
    public void Reveal(float intensity)
    {
        // intensity 0..1
        timer = Mathf.Lerp(0.15f, maxVisibleTime, intensity);
        SetAlpha(1f);
    }

    void Update()
    {
        if (timer > 0f)
        {
            timer -= Time.deltaTime;
            return;
        }

        // fade to invisible
        Color c = sr.color;
        c.a = Mathf.MoveTowards(c.a, 0f, Time.deltaTime * fadeOutSpeed);
        sr.color = c;
    }

    void SetAlpha(float a)
    {
        Color c = sr.color;
        c.a = a;
        sr.color = c;
    }
}