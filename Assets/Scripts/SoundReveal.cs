using UnityEngine;

[RequireComponent(typeof(SpriteRenderer))]
public class SoundReveal : MonoBehaviour
{
    public float fadeOutSpeed = 6f;     // Higher values fade the sprite faster.
    public float maxVisibleTime = 0.5f; // How long the sprite stays fully visible.

    // Sprite renderer that gets faded in and out.
    SpriteRenderer sr;
    // Time left before this object starts fading.
    float timer;

    void Awake()
    {
        // Cache the renderer and start hidden.
        sr = GetComponent<SpriteRenderer>();
        SetAlpha(0f); // start hidden
    }
    public void Reveal(float intensity)
    {
        // Use intensity from 0 to 1 to control how long the object stays visible.
        timer = Mathf.Lerp(0.15f, maxVisibleTime, intensity);
        SetAlpha(1f);
    }

    void Update()
    {
        // Stay fully visible while the reveal timer is active.
        if (timer > 0f)
        {
            timer -= Time.deltaTime;
            return;
        }

        // Fade back to invisible once the timer has finished.
        Color c = sr.color;
        c.a = Mathf.MoveTowards(c.a, 0f, Time.deltaTime * fadeOutSpeed);
        sr.color = c;
    }

    void SetAlpha(float a)
    {
        // Change only the alpha so the sprite keeps its original color.
        Color c = sr.color;
        c.a = a;
        sr.color = c;
    }
}
