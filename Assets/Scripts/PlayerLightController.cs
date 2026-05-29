using UnityEngine;
using UnityEngine.Rendering.Universal;

public class PlayerLightController : MonoBehaviour
{
    // Microphone source used to drive light strength.
    public MicrophoneInput mic;
    // Voice state source used to know when the light action is active.
    public VoiceActionController voice;
    // URP 2D light controlled by this script.
    public Light2D light2D;

    [Header("Always-On Base")]
    // Minimum light radius when there is no sound boost.
    public float baseOuterRadius = 1.2f;
    // Minimum light intensity when there is no sound boost.
    public float baseIntensity = 0.6f;

    [Header("Sound Boost")]
    public float noiseFloor = 0.001f;  // Below this, microphone input gives no boost.
    public float loudRef = 0.03f;      // Volume treated as fully boosted speech.
    // Maximum radius added on top of the base radius.
    public float maxRadiusBoost = 4.0f;
    // Maximum intensity added on top of the base intensity.
    public float maxIntensityBoost = 0.8f;

    [Header("Smoothing")]
    // Speed used when the light is expanding.
    public float smooth = 10f;
    [Tooltip("How slowly the light shrinks after the Light state ends.")]
    // Speed used when the light is shrinking.
    public float decaySmooth = 2.5f;
    [Tooltip("How long the light holds its expanded size before shrinking.")]
    // Delay before the target starts shrinking after a quieter input.
    public float decayDelay = 0.15f;

    [Header("Light State Exit")]
    [Tooltip("How long the light stays expanded after leaving the Light state.")]
    // Extra hold time after leaving the medium voice state.
    public float exitHoldTime = 0.45f;

    // Counts down before letting the target radius decay.
    float decayTimer;
    // Smoothed target radius that the actual light moves toward.
    float currentTargetRadius;
    // Smoothed target intensity that the actual light moves toward.
    float currentTargetIntensity;
    // Counts down after leaving the light state.
    float exitHoldTimer;

    void Reset()
    {
        // Auto-assign the Light2D when the component is added.
        light2D = GetComponent<Light2D>();
    }

    void Start()
    {
        // Find the voice controller in the scene if one was not assigned.
        if (voice == null)
            voice = FindObjectOfType<VoiceActionController>();

        // Initialize the target values at the base light settings.
        currentTargetRadius = baseOuterRadius;
        currentTargetIntensity = baseIntensity;
        exitHoldTimer = 0f;

        // Apply base settings immediately if a light is available.
        if (light2D == null) return;

        light2D.pointLightOuterRadius = baseOuterRadius;
        light2D.intensity = baseIntensity;
    }

    void Update()
    {
        // The light needs both microphone input and a Light2D to work.
        if (mic == null || light2D == null) return;

        // Treat the light as active only during the Medium voice state.
        bool inLightState = voice == null || voice.CurrentState == VoiceActionController.VoiceState.Medium;
        // Ignore microphone amplitude when the light state is not active.
        float a = inLightState ? mic.SmoothedAmplitude : 0f;

        // Map the microphone volume into a 0 to 1 boost value.
        float t = Mathf.Clamp01(Mathf.InverseLerp(noiseFloor, loudRef, a));

        // Calculate the desired boosted radius and intensity.
        float targetRadius = baseOuterRadius + (maxRadiusBoost * t);
        float targetIntensity = baseIntensity + (maxIntensityBoost * t);

        if (inLightState)
        {
            // Refresh the exit hold while the light action remains active.
            exitHoldTimer = exitHoldTime;

            // Expanding should happen immediately, shrinking can be delayed.
            bool expanding =
                targetRadius >= currentTargetRadius ||
                targetIntensity >= currentTargetIntensity;

            if (expanding)
            {
                // Update target values right away when input gets louder.
                currentTargetRadius = targetRadius;
                currentTargetIntensity = targetIntensity;
                decayTimer = decayDelay;
            }
            else if (decayTimer > 0f)
            {
                // Hold the larger size briefly before shrinking.
                decayTimer -= Time.deltaTime;
            }
            else
            {
                // After the delay, allow the target to shrink.
                currentTargetRadius = targetRadius;
                currentTargetIntensity = targetIntensity;
            }
        }
        else if (exitHoldTimer > 0f)
        {
            // Keep the current target briefly after leaving the light state.
            exitHoldTimer -= Time.deltaTime;
        }
        else
        {
            // Return to the base light after the exit hold finishes.
            decayTimer = 0f;
            currentTargetRadius = baseOuterRadius;
            currentTargetIntensity = baseIntensity;
        }

        // Use faster smoothing for growth and slower smoothing for decay.
        float radiusSmooth = currentTargetRadius >= light2D.pointLightOuterRadius ? smooth : decaySmooth;
        float intensitySmooth = currentTargetIntensity >= light2D.intensity ? smooth : decaySmooth;

        // Smoothly move the actual light radius toward its target.
        light2D.pointLightOuterRadius =
            Mathf.Lerp(light2D.pointLightOuterRadius, currentTargetRadius, Time.deltaTime * radiusSmooth);

        // Smoothly move the actual light intensity toward its target.
        light2D.intensity =
            Mathf.Lerp(light2D.intensity, currentTargetIntensity, Time.deltaTime * intensitySmooth);
    }
}
