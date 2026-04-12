using UnityEngine;
using UnityEngine.Rendering.Universal;

public class PlayerLightController : MonoBehaviour
{
    public MicrophoneInput mic;
    public Light2D light2D;

    [Header("Always-On Base")]
    public float baseOuterRadius = 1.2f;
    public float baseIntensity = 0.6f;

    [Header("Sound Boost")]
    public float noiseFloor = 0.001f;  // below this = no boost
    public float loudRef = 0.03f;      // speaking loud-ish level
    public float maxRadiusBoost = 4.0f;
    public float maxIntensityBoost = 0.8f;

    [Header("Smoothing")]
    public float smooth = 10f;
    [Tooltip("How long the light holds its expanded size before shrinking.")]
    public float decayDelay = 0.15f;

    float decayTimer;
    float currentTargetRadius;
    float currentTargetIntensity;

    void Reset()
    {
        light2D = GetComponent<Light2D>();
    }

    void Start()
    {
        currentTargetRadius = baseOuterRadius;
        currentTargetIntensity = baseIntensity;

        if (light2D == null) return;

        light2D.pointLightOuterRadius = baseOuterRadius;
        light2D.intensity = baseIntensity;
    }

    void Update()
    {
        if (mic == null || light2D == null) return;

        float a = mic.SmoothedAmplitude;

        // 0..1 intensity mapping
        float t = Mathf.Clamp01(Mathf.InverseLerp(noiseFloor, loudRef, a));

        float targetRadius = baseOuterRadius + (maxRadiusBoost * t);
        float targetIntensity = baseIntensity + (maxIntensityBoost * t);

        bool expanding =
            targetRadius >= currentTargetRadius ||
            targetIntensity >= currentTargetIntensity;

        if (expanding)
        {
            currentTargetRadius = targetRadius;
            currentTargetIntensity = targetIntensity;
            decayTimer = decayDelay;
        }
        else if (decayTimer > 0f)
        {
            decayTimer -= Time.deltaTime;
        }
        else
        {
            currentTargetRadius = targetRadius;
            currentTargetIntensity = targetIntensity;
        }

        light2D.pointLightOuterRadius =
            Mathf.Lerp(light2D.pointLightOuterRadius, currentTargetRadius, Time.deltaTime * smooth);

        light2D.intensity =
            Mathf.Lerp(light2D.intensity, currentTargetIntensity, Time.deltaTime * smooth);
    }
}
