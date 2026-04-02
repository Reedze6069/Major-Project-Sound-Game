using UnityEngine;

public class VoiceActionController : MonoBehaviour
{
    public enum VoiceState { Neutral, Quiet, Medium, Loud }
    public VoiceState CurrentState { get; private set; } = VoiceState.Neutral;

    [Header("References")]
    public MicrophoneInput mic;
    public PlayerController player;
    public LaneCrosshairController laneCrosshair;

    [Header("Threshold Bands")]
    [Tooltip("Anything below this is treated as Neutral (silence / noise floor).")]
    public float quietMin = 0.015f;

    [Tooltip("Quiet band upper bound.")]
    public float quietMax = 0.18f;

    [Tooltip("Loud band lower bound.")]
    public float loudMin = 0.375f;

    [Header("Stability")]
    [Tooltip("Extra margin needed to move upward into a stronger band.")]
    public float riseHysteresis = 0.02f;

    [Tooltip("Small margin used when falling back down, so Quiet is easier to re-enter.")]
    public float fallHysteresis = 0.005f;

    [Header("Neutral Return")]
    public float neutralReturnThreshold = 0.01f;
    public float neutralHoldTime = 0.5f;

    [Header("Cooldowns")]
    public float jumpCooldown = 0.25f;
    public float shootCooldown = 0.15f;

    [Header("Spacebar Confirm")]
    public KeyCode confirmKey = KeyCode.Space;
    public float confirmCooldown = 0.12f;

    float neutralTimer = 0f;
    float jumpTimer = 0f;
    float shootTimer = 0f;
    float confirmTimer = 0f;

    bool crouchLatched = false;
    bool isLoudAiming = false;

    void Update()
    {
        if (mic == null || player == null) return;

        float a = mic.SmoothedAmplitude;

        jumpTimer    -= Time.deltaTime;
        shootTimer   -= Time.deltaTime;
        confirmTimer -= Time.deltaTime;

        if (isLoudAiming && Input.GetKeyUp(confirmKey))
        {
            ReleaseLoudAim();
        }

        // --- Neutral return (silence -> Neutral) ---
        if (a < neutralReturnThreshold)
        {
            neutralTimer += Time.deltaTime;
            if (neutralTimer >= neutralHoldTime)
            {
                SetState(VoiceState.Neutral);
                ForceStand();
                return;
            }
        }
        else
        {
            neutralTimer = 0f;
        }

        // --- Update DISPLAY state with hysteresis ---
        CurrentState = GetStateWithHysteresis(a, CurrentState);

        if (isLoudAiming && laneCrosshair != null && CurrentState != VoiceState.Neutral)
        {
            laneCrosshair.SetLane(CurrentState);
        }

        // --- Confirm with Spacebar (IMPORTANT: confirm by amplitude bands, not by CurrentState) ---
        if (Input.GetKeyDown(confirmKey) && confirmTimer <= 0f)
        {
            confirmTimer = confirmCooldown;

            VoiceState confirmed = GetStateFromAmplitude(a);

            if (confirmed == VoiceState.Loud && laneCrosshair != null)
            {
                BeginLoudAim();
                return;
            }

            Debug.Log($"[VAC] Space | confirmed={confirmed} amp={{a:F4}} (quietMin={quietMin:F3} quietMax={quietMax:F3} loudMin={loudMin:F3})");

            ConfirmState(confirmed);
        }

    }

    // Pure band mapping (NO hysteresis) — used only for confirming actions.
    private VoiceState GetStateFromAmplitude(float a)
    {
        if (a < quietMin) return VoiceState.Neutral;
        if (a <= quietMax) return VoiceState.Quiet;
        if (a >= loudMin) return VoiceState.Loud;
        return VoiceState.Medium;
    }

    // Hysteresis mapping — used for smoother UI/CurrentState display.
    private VoiceState GetStateWithHysteresis(float a, VoiceState current)
    {
        // Treat under quietMin as Neutral always
        if (a < quietMin) return VoiceState.Neutral;

        switch (current)
        {
            case VoiceState.Neutral:
                // Enter quiet/medium/loud based on amplitude
                return GetStateFromAmplitude(a);

            case VoiceState.Quiet:
                if (a >= loudMin + riseHysteresis) return VoiceState.Loud;
                if (a > quietMax + riseHysteresis) return VoiceState.Medium;
                return VoiceState.Quiet;

            case VoiceState.Medium:
                if (a <= quietMax - fallHysteresis) return VoiceState.Quiet;
                if (a >= loudMin + riseHysteresis) return VoiceState.Loud;
                return VoiceState.Medium;

            case VoiceState.Loud:
                if (a <= quietMax - fallHysteresis) return VoiceState.Quiet;
                if (a <= loudMin - fallHysteresis) return VoiceState.Medium;
                return VoiceState.Loud;
        }

        return GetStateFromAmplitude(a);
    }

    private void ConfirmState(VoiceState confirmed)
    {
        // If it's not Quiet, we should NEVER remain crouched after pressing Space.
        if (confirmed != VoiceState.Quiet)
        {
            ForceStand();
        }

        switch (confirmed)
        {
            case VoiceState.Quiet:
                crouchLatched = true;
                player.SetCrouch(true);
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
                // already stood up
                break;
        }
    }

    private void ForceStand()
    {
        crouchLatched = false;
        player.StandUp();
    }

    private void BeginLoudAim()
    {
        if (laneCrosshair == null || player == null) return;

        isLoudAiming = true;
        ForceStand();
        laneCrosshair.Show(player.firePoint != null ? player.firePoint : player.transform);
        laneCrosshair.SetLane(CurrentState);
    }

    private void ReleaseLoudAim()
    {
        isLoudAiming = false;

        if (laneCrosshair == null || player == null)
            return;

        Vector2 origin = player.firePoint != null ? player.firePoint.position : player.transform.position;

        if (shootTimer <= 0f)
        {
            player.Shoot(laneCrosshair.GetAimDirection(origin));
            shootTimer = shootCooldown;
        }

        laneCrosshair.Hide();
    }

    private void SetState(VoiceState s)
    {
        CurrentState = s;
    }
}
