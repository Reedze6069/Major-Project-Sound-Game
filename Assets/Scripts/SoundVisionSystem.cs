using UnityEngine;

public class SoundVisionSystem : MonoBehaviour
{
    public GameObject echoArcPrefab;

    [Header("References")]
    public MicrophoneInput mic;
    public VoiceActionController voice;
    public Transform player;

    [Header("Reveal Targets")]
    [Tooltip("Only objects on these layers will be revealed (Ground/Platforms/etc).")]
    public LayerMask revealMask;

    [Header("Wave Prefabs")]
    public GameObject waveRingPrefab; // Quiet + Loud
    public GameObject waveConePrefab; // Medium

    [Header("Timing")]
    [Tooltip("How often to spawn a wave while sound is present.")]
    public float pulseInterval = 0.10f;

    [Header("Noise Gate")]
    [Tooltip("Below this, do nothing (prevents constant pulsing from room noise).")]
    public float noiseFloor = 0.001f;

    [Header("Quiet (Green)")]
    public float quietRadius = 2.0f;
    public float quietWaveLife = 0.40f;

    [Header("Medium (Orange) - forward scan")]
    public float mediumRange = 5.0f;
    [Range(10f, 120f)] public float mediumAngle = 40f;
    public float mediumWaveLife = 0.30f;

    [Header("Loud (Red)")]
    public float loudRadius = 7.0f;
    public float loudWaveLife = 0.55f;

    float timer;

    void Update()
    {
        if (mic == null || voice == null || player == null) return;

        float a = mic.SmoothedAmplitude;

        // 1) Noise gate (prevents constant waves)
        if (a < noiseFloor) return;

        // 2) Rate limit wave spawning
        timer -= Time.deltaTime;
        if (timer > 0f) return;
        timer = pulseInterval;

        // 3) Strength (0..1) so reveal time scales slightly with loudness
        float intensity = ComputeIntensity(a);

        // 4) Choose behaviour based on CURRENT voice state (passive layer)
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
                // Neutral: do nothing
                break;
        }
    }

    float ComputeIntensity(float amp)
    {
        // Simple curve: maps noiseFloor..(noiseFloor*10) -> 0..1
        float top = noiseFloor * 10f;
        return Mathf.Clamp01(Mathf.InverseLerp(noiseFloor, top, amp));
    }

    void SpawnRing(Color c, float radius, float life)
    {
        if (waveRingPrefab == null) return;

        GameObject go = Instantiate(waveRingPrefab, player.position, Quaternion.identity);

        SoundWaveVisual v = go.GetComponent<SoundWaveVisual>();
        if (v != null) v.Init(c, radius, life);
    }

    void SpawnCone(Color c, float range, float life)
    {
        if (waveConePrefab == null) return;

        GameObject go = Instantiate(waveConePrefab, player.position, Quaternion.identity);

        // forward echo behaviour
        var forward = go.GetComponent<SoundWaveForward>();
        if (forward != null)
        {
            forward.Init(c, 12f, 1.2f, life); // speed=10, lifetime=life
            return;
        }

        // fallback if you still have SoundWaveVisual on it
        var v = go.GetComponent<SoundWaveVisual>();
        if (v != null) v.Init(c, range, life);
    }


    void RevealCircle(float radius, float intensity)
    {
        Collider2D[] hits = Physics2D.OverlapCircleAll(player.position, radius, revealMask);
        for (int i = 0; i < hits.Length; i++)
        {
            SoundReveal r = hits[i].GetComponent<SoundReveal>();
            if (r != null) r.Reveal(intensity);
        }
    }

    void RevealCone(float range, float angleDeg, float intensity)
    {
        // 1) Get nearby in a circle
        Collider2D[] hits = Physics2D.OverlapCircleAll(player.position, range, revealMask);

        // 2) Filter to a cone in front
        Vector2 forward = Vector2.right;
        float half = angleDeg * 0.5f;

        for (int i = 0; i < hits.Length; i++)
        {
            Vector2 to = (Vector2)hits[i].transform.position - (Vector2)player.position;
            if (to.sqrMagnitude < 0.0001f) continue;

            float ang = Vector2.Angle(forward, to.normalized);
            if (ang <= half)
            {
                SoundReveal r = hits[i].GetComponent<SoundReveal>();
                if (r != null) r.Reveal(intensity);
            }
        }
    }

#if UNITY_EDITOR
    void OnDrawGizmosSelected()
    {
        if (player == null) return;
        Gizmos.DrawWireSphere(player.position, quietRadius);
        Gizmos.DrawWireSphere(player.position, loudRadius);
    }
#endif
}
