using UnityEngine;

[RequireComponent(typeof(LineRenderer))]
public class EchoArcWave : MonoBehaviour
{
    [Header("Arc Shape")]
    public int segments = 28;          // smoothness
    public float arcWidth = 6f;        // how wide the arc reaches forward
    public float arcDrop = 4f;         // how far it curves down

    [Header("Motion")]
    public float speed = 10f;
    public float life = 1.2f;

    [Header("Fade")]
    public float startAlpha = 0.9f;

    LineRenderer lr;
    float t;
    Vector3 startPos;

    void Awake()
    {
        lr = GetComponent<LineRenderer>();
        startPos = transform.position;
        BuildArc();
    }

    public void Init(Color c, float width, float drop, float speedOverride, float lifeOverride)
    {
        arcWidth = width;
        arcDrop = drop;
        speed = speedOverride;
        life = lifeOverride;

        // set initial color/alpha
        c.a = startAlpha;
        lr.startColor = c;
        lr.endColor = new Color(c.r, c.g, c.b, 0f);

        BuildArc();
    }

    void Update()
    {
        t += Time.deltaTime;
        float p = Mathf.Clamp01(t / life);

        // Move forward
        transform.position = startPos + Vector3.right * (speed * t);

        // Fade
        Color sc = lr.startColor; sc.a = Mathf.Lerp(startAlpha, 0f, p);
        Color ec = lr.endColor;   ec.a = Mathf.Lerp(0f, 0f, p);
        lr.startColor = sc;
        lr.endColor = ec;

        if (t >= life) Destroy(gameObject);
    }

    void BuildArc()
    {
        if (segments < 2) segments = 2;

        lr.positionCount = segments;

        // Arc curve: x goes forward, y drops down smoothly (like your sketch)
        for (int i = 0; i < segments; i++)
        {
            float u = i / (segments - 1f); // 0..1
            float x = u * arcWidth;

            // smooth downward bend (ease-in): y = -drop * (u^2)
            float y = -arcDrop * (u * u);

            lr.SetPosition(i, new Vector3(x, y, 0f));
        }
    }
}