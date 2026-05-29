using UnityEngine;
using UnityEngine.Rendering.Universal;

public class PlayerLightController : MonoBehaviour
{
    public MicrophoneInput mic;
    public VoiceActionController voice;
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
    [Tooltip("How slowly the light shrinks after the Light state ends.")]
    public float decaySmooth = 2.5f;
    [Tooltip("How long the light holds its expanded size before shrinking.")]
    public float decayDelay = 0.15f;

    [Header("Light State Exit")]
    [Tooltip("How long the light stays expanded after leaving the Light state.")]
    public float exitHoldTime = 0.45f;

    float decayTimer;
    float currentTargetRadius;
    float currentTargetIntensity;
    float exitHoldTimer;

    void Reset()
    {
        light2D = GetComponent<Light2D>();
    }

    void Start()
    {
        if (voice == null)
            voice = FindObjectOfType<VoiceActionController>();

        currentTargetRadius = baseOuterRadius;
        currentTargetIntensity = baseIntensity;
        exitHoldTimer = 0f;

        if (light2D == null) return;

        light2D.pointLightOuterRadius = baseOuterRadius;
        light2D.intensity = baseIntensity;
    }

    void Update()
    {
        if (mic == null || light2D == null) return;

        bool inLightState = voice == null || voice.CurrentState == VoiceActionController.VoiceState.Medium;
        float a = inLightState ? mic.SmoothedAmplitude : 0f;

        // 0..1 intensity mapping
        float t = Mathf.Clamp01(Mathf.InverseLerp(noiseFloor, loudRef, a));

        float targetRadius = baseOuterRadius + (maxRadiusBoost * t);
        float targetIntensity = baseIntensity + (maxIntensityBoost * t);

        if (inLightState)
        {
            exitHoldTimer = exitHoldTime;

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
        }
        else if (exitHoldTimer > 0f)
        {
            exitHoldTimer -= Time.deltaTime;
        }
        else
        {
            decayTimer = 0f;
            currentTargetRadius = baseOuterRadius;
            currentTargetIntensity = baseIntensity;
        }

        float radiusSmooth = currentTargetRadius >= light2D.pointLightOuterRadius ? smooth : decaySmooth;
        float intensitySmooth = currentTargetIntensity >= light2D.intensity ? smooth : decaySmooth;

        light2D.pointLightOuterRadius =
            Mathf.Lerp(light2D.pointLightOuterRadius, currentTargetRadius, Time.deltaTime * radiusSmooth);

        light2D.intensity =
            Mathf.Lerp(light2D.intensity, currentTargetIntensity, Time.deltaTime * intensitySmooth);
    }
}
