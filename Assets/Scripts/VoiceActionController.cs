using UnityEngine;

public class VoiceActionController : MonoBehaviour
{
    public enum VoiceState { Neutral, Quiet, Medium, Loud }
    public VoiceState CurrentState { get; private set; } = VoiceState.Neutral;

    [Header("References")]
    public MicrophoneInput mic;
    public PlayerController player;

    [Header("Thresholds (tune during testing)")]
    public float quietMax = 0.18f;   // <= quietMax = Quiet
    public float loudMin = 0.375f;   // >= loudMin = Loud (else Medium)

    [Header("Stability")]
    public float hysteresis = 0.02f;
    public float activationThreshold = 0.05f;

    [Header("Neutral Return")]
    public float neutralReturnThreshold = 0.01f;
    public float neutralHoldTime = 0.5f;

    [Header("Cooldowns")]
    public float jumpCooldown = 0.25f;
    public float shootCooldown = 0.15f;

    [Header("Spacebar Confirm")]
    public KeyCode confirmKey = KeyCode.Space;
    public float confirmCooldown = 0.12f; // prevents double confirms from one press

    private float neutralTimer = 0f;
    private float jumpTimer = 0f;
    private float shootTimer = 0f;
    private float confirmTimer = 0f;

    private bool hasActivated = false;

    void Update()
    {
        if (mic == null || player == null) return;

        float a = mic.SmoothedAmplitude;

        jumpTimer -= Time.deltaTime;
        shootTimer -= Time.deltaTime;
        confirmTimer -= Time.deltaTime;

        // --- Neutral return (silence -> Neutral) ---
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

        // --- Activation gate ---
        if (!hasActivated)
        {
            CurrentState = VoiceState.Neutral;

            if (a > activationThreshold)
                hasActivated = true;

            player.SetCrouch(false);
            return;
        }

        // --- Detect state (with hysteresis) ---
        VoiceState next = CurrentState;

        if (CurrentState == VoiceState.Neutral)
        {
            if (a <= quietMax) next = VoiceState.Quiet;
            else if (a >= loudMin) next = VoiceState.Loud;
            else next = VoiceState.Medium;
        }
        else if (CurrentState == VoiceState.Quiet)
        {
            if (a > quietMax + hysteresis) next = VoiceState.Medium;
        }
        else if (CurrentState == VoiceState.Medium)
        {
            if (a < quietMax - hysteresis) next = VoiceState.Quiet;
            else if (a > loudMin + hysteresis) next = VoiceState.Loud;
        }
        else // Loud
        {
            if (a < loudMin - hysteresis) next = VoiceState.Medium;
        }

        CurrentState = next;

        // IMPORTANT: While using spacebar confirm, DO NOT auto-crouch.
        // (Crouch is an action you confirm, not a continuous state.)
        player.SetCrouch(false);

        // --- Confirm with Spacebar ---
        if (Input.GetKeyDown(confirmKey) && confirmTimer <= 0f)
        {
            confirmTimer = confirmCooldown;
            ConfirmCurrentState();
        }
    }

    private void ConfirmCurrentState()
    {
        switch (CurrentState)
        {
            case VoiceState.Quiet:
                // Confirm crouch (toggle works well for UX)
                player.ToggleCrouch();
                break;

            case VoiceState.Medium:
                if (jumpTimer <= 0f)
                {
                    player.Jump();
                    jumpTimer = jumpCooldown;
                }
                break;

            case VoiceState.Loud:
                if (shootTimer <= 0f)
                {
                    player.Shoot();
                    shootTimer = shootCooldown;
                }
                break;

            case VoiceState.Neutral:
            default:
                // Optional: do nothing
                break;
        }
    }
}
