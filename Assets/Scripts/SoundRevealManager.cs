using UnityEngine;
using System.Collections.Generic;

public class SoundRevealManager : MonoBehaviour
{
    public Transform player;

    [Header("Test Pulse")]
    public float pulseEvery = 1.0f;
    public float testRadius = 6f;
    public float testIntensity = 1f;

    List<SoundReveal> revealables = new List<SoundReveal>();
    float t;

    void Start()
    {
        revealables.AddRange(FindObjectsOfType<SoundReveal>(true));
        Debug.Log($"[RevealManager] Found {revealables.Count} SoundReveal objects.");
    }

    void Update()
    {
        if (player == null) return;

        t += Time.deltaTime;
        if (t < pulseEvery) return;
        t = 0f;

        int revealed = 0;
        foreach (var r in revealables)
        {
            if (r == null) continue;

            float d = Vector2.Distance(player.position, r.transform.position);
            if (d <= testRadius)
            {
                r.Reveal(testIntensity);
                revealed++;
            }
        }

        Debug.Log($"[RevealManager] Pulse reveal: {revealed} objects within {testRadius} units.");
    }
}