using UnityEngine;

public class SoundVisionSystem : MonoBehaviour
{
    // Optional prefab for an echo arc effect.
    public GameObject echoArcPrefab;

    [Header("References")]
    // Microphone input that provides smoothed amplitude.
    public MicrophoneInput mic;
    // Voice state controller that chooses quiet, medium, or loud behavior.
    public VoiceActionController voice;
    // Player transform used as the origin for waves and reveal checks.
    public Transform player;

    [Header("Reveal Targets")]
    [Tooltip("Only objects on these layers will be revealed (Ground/Platforms/etc).")]
    // Layer mask used by Physics2D reveal overlap checks.
    public LayerMask revealMask;

    [Header("Wave Prefabs")]
    public GameObject waveRingPrefab; // Used by quiet and loud circular waves.
    public GameObject waveConePrefab; // Used by medium forward-scan waves.

    [Header("Timing")]
    [Tooltip("How often to spawn a wave while sound is present.")]
    // Minimum time between sound wave spawns.
    public float pulseInterval = 0.10f;

    [Header("Noise Gate")]
    [Tooltip("Below this, do nothing (prevents constant pulsing from room noise).")]
    // Amplitude below this value is treated as silence.
    public float noiseFloor = 0.001f;

    [Header("Quiet (Green)")]
    // Reveal radius for quiet sound.
    public float quietRadius = 2.0f;
    // Lifetime of the quiet ring visual.
    public float quietWaveLife = 0.40f;

    [Header("Medium (Orange) - forward scan")]
    // Forward reveal range for medium sound.
    public float mediumRange = 5.0f;
    // Cone width for the medium forward scan.
    [Range(10f, 120f)] public float mediumAngle = 40f;
    // Lifetime of the medium wave visual.
    public float mediumWaveLife = 0.30f;

    [Header("Loud (Red)")]
    // Reveal radius for loud sound.
    public float loudRadius = 7.0f;
    // Lifetime of the loud ring visual.
    public float loudWaveLife = 0.55f;

    // Countdown that rate-limits wave spawning.
    float timer;

    void Update()
    {
        // Required references must exist before sound vision can run.
        if (mic == null || voice == null || player == null) return;

        // Use the smoothed microphone amplitude for stable reveal behavior.
        float a = mic.SmoothedAmplitude;

        // Noise gate prevents constant waves from low room noise.
        if (a < noiseFloor) return;

        // Rate limit wave spawning so sound does not create too many objects.
        timer -= Time.deltaTime;
        if (timer > 0f) return;
        timer = pulseInterval;

        // Strength from 0 to 1 lets reveal time scale with loudness.
        float intensity = ComputeIntensity(a);

        // Choose reveal shape and visual color from the current voice state.
        switch (voice.CurrentState)
        {
            case VoiceActionController.VoiceState.Quiet:
                SpawnRing(Color.green, quietRadius, quietWaveLife);
                RevealCircle(quietRadius, intensity);
                break;

            case VoiceActionController.VoiceState.Medium:
                SpawnCone(new Color(1f, 0.55f, 0f), mediumRange, mediumWaveLife);
                RevealCone(mediumRange, mediumAngle, intensity);
                break;

            case VoiceActionController.VoiceState.Loud:
                SpawnRing(Color.red, loudRadius, loudWaveLife);
                RevealCircle(loudRadius, intensity);
                break;

            default:
                // Idle creates no sound vision effect.
                break;
        }
    }

    float ComputeIntensity(float amp)
    {
        // Simple curve maps noiseFloor to noiseFloor * 10 into a 0 to 1 range.
        float top = noiseFloor * 10f;
        return Mathf.Clamp01(Mathf.InverseLerp(noiseFloor, top, amp));
    }

    void SpawnRing(Color c, float radius, float life)
    {
        // Skip the visual if no ring prefab was assigned.
        if (waveRingPrefab == null) return;

        // Spawn the ring at the player and initialize its color, size, and lifetime.
        GameObject go = Instantiate(waveRingPrefab, player.position, Quaternion.identity);

        SoundWaveVisual v = go.GetComponent<SoundWaveVisual>();
        if (v != null) v.Init(c, radius, life);
    }

    void SpawnCone(Color c, float range, float life)
    {
        // Skip the visual if no cone prefab was assigned.
        if (waveConePrefab == null) return;

        // Spawn the forward wave at the player.
        GameObject go = Instantiate(waveConePrefab, player.position, Quaternion.identity);

        // Prefer SoundWaveForward when the prefab has forward echo behavior.
        var forward = go.GetComponent<SoundWaveForward>();
        if (forward != null)
        {
            forward.Init(c, 12f, 1.2f, life); // Color, range-like value, speed, lifetime.
            return;
        }

        // Fall back to the expanding ring visual if the prefab uses SoundWaveVisual.
        var v = go.GetComponent<SoundWaveVisual>();
        if (v != null) v.Init(c, range, life);
    }


    void RevealCircle(float radius, float intensity)
    {
        // Find every reveal target within a circular area around the player.
        Collider2D[] hits = Physics2D.OverlapCircleAll(player.position, radius, revealMask);
        for (int i = 0; i < hits.Length; i++)
        {
            // Look on the collider, parent, and children for a SoundReveal.
            SoundReveal r = hits[i].GetComponent<SoundReveal>();
            if (r == null) r = hits[i].GetComponentInParent<SoundReveal>();
            if (r == null) r = hits[i].GetComponentInChildren<SoundReveal>();

            // Reveal the object if one was found.
            if (r != null) r.Reveal(intensity);
        }
    }

    void RevealCone(float range, float angleDeg, float intensity)
    {
        // Start with nearby colliders, then filter them into a forward cone.
        Collider2D[] hits = Physics2D.OverlapCircleAll(player.position, range, revealMask);

        // This game faces right, so the cone points along Vector2.right.
        Vector2 forward = Vector2.right;
        float half = angleDeg * 0.5f;

        for (int i = 0; i < hits.Length; i++)
        {
            // Use closest point so large colliders reveal when their edge enters the cone.
            Vector2 closest = hits[i].ClosestPoint(player.position);
            Vector2 to = closest - (Vector2)player.position;

            // Ignore colliders exactly on top of the player.
            if (to.sqrMagnitude < 0.0001f) continue;

            // Only reveal colliders inside half the cone angle.
            float ang = Vector2.Angle(forward, to.normalized);
            if (ang <= half)
            {
                // Look on the collider, parent, and children for a SoundReveal.
                SoundReveal r = hits[i].GetComponent<SoundReveal>();
                if (r == null) r = hits[i].GetComponentInParent<SoundReveal>();
                if (r == null) r = hits[i].GetComponentInChildren<SoundReveal>();

                // Reveal the object if one was found.
                if (r != null) r.Reveal(intensity);
            }
        }
    }

#if UNITY_EDITOR
    void OnDrawGizmosSelected()
    {
        // Draw quiet and loud reveal ranges in the Scene view for tuning.
        if (player == null) return;
        Gizmos.DrawWireSphere(player.position, quietRadius);
        Gizmos.DrawWireSphere(player.position, loudRadius);
    }
#endif
}
