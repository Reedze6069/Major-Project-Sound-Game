using UnityEngine;

public class VoiceActionController : MonoBehaviour
{
    // 1) Add Neutral so the game doesn't start crouching by default
    public enum VoiceState { Neutral, Quiet, Medium, Loud }

    // CurrentState must be settable inside this script
    public VoiceState CurrentState { get; private set; } = VoiceState.Neutral;

    [Header("References")]
    public MicrophoneInput mic;
    public PlayerController player;

    [Header("Thresholds (tune during testing)")]
    public float quietMax = 0.18f;
    public float loudMin = 0.375f;

    [Header("Stability")]
    public float hysteresis = 0.02f;
    public float activationThreshold = 0.05f; // speak once to "activate" the system

    [Header("Cooldowns")]
    public float jumpCooldown = 0.25f;
    public float shootCooldown = 0.15f;

    private float jumpTimer = 0f;
    private float shootTimer = 0f;

    private bool hasActivated = false;
    public float quietHoldTime = 0.20f;
    private float quietHoldTimer = 0f;
    
    [Header("Neutral Return")]
    public float neutralReturnThreshold = 0.01f;  // below this = basically silence
    public float neutralHoldTime = 0.5f;          // must stay silent this long
    private float neutralTimer = 0f;


    void Update()
    {
        if (mic == null || player == null) return;

        jumpTimer -= Time.deltaTime;
        shootTimer -= Time.deltaTime;

        float a = mic.SmoothedAmplitude;
// If user goes silent again for a while, return to Neutral (stand idle)
        if (a < neutralReturnThreshold)
        {
            neutralTimer += Time.deltaTime;
            if (neutralTimer >= neutralHoldTime)
            {
                CurrentState = VoiceState.Neutral;
                player.SetCrouch(false);
                return;
            }
        }
        else
        {
            neutralTimer = 0f;
        }

        // --- Activation gate: start Neutral until user speaks once ---
        if (!hasActivated)
        {
            CurrentState = VoiceState.Neutral;

            if (a > activationThreshold)
                hasActivated = true;

            player.SetCrouch(false);
            return;
        }

        // --- Sticky transitions with hysteresis ---
        if (CurrentState == VoiceState.Neutral)
        {
            // Decide initial state after activation
            if (a <= quietMax) CurrentState = VoiceState.Quiet;
            else if (a >= loudMin) CurrentState = VoiceState.Loud;
            else CurrentState = VoiceState.Medium;
        }
        else if (CurrentState == VoiceState.Quiet)
        {
            if (a > quietMax + hysteresis) CurrentState = VoiceState.Medium;
        }
        else if (CurrentState == VoiceState.Medium)
        {
            if (a < quietMax - hysteresis) CurrentState = VoiceState.Quiet;
            else if (a > loudMin + hysteresis) CurrentState = VoiceState.Loud;
        }
        else // Loud
        {
            if (a < loudMin - hysteresis) CurrentState = VoiceState.Medium;
        }

        // --- Apply actions ---
        if (CurrentState == VoiceState.Quiet)
        {
            quietHoldTimer += Time.deltaTime;
        }
        else
        {
            quietHoldTimer = 0f;
        }

        player.SetCrouch(quietHoldTimer >= quietHoldTime);

        if (CurrentState == VoiceState.Medium && jumpTimer <= 0f)
        {
            player.Jump();
            jumpTimer = jumpCooldown;
        }

        if (CurrentState == VoiceState.Loud && shootTimer <= 0f)
        {
            player.Shoot();
            shootTimer = shootCooldown;
        }
    }
}
