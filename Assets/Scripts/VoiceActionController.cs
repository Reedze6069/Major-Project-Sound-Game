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
    public float quietMax = 0.16f;

    [Tooltip("Loud band lower bound.")]
    public float loudMin = 0.44f;

    [Header("Stability")]
    [Tooltip("Extra margin needed to move upward into a stronger band.")]
    public float riseHysteresis = 0.02f;

    [Tooltip("Small margin used when falling back down, so Quiet is easier to re-enter.")]
    public float fallHysteresis = 0.02f;

    [Tooltip("How long a new band must stay stable before the state changes.")]
    public float stateChangeHoldTime = 0.1f;

    [Header("Neutral Return")]
    public float neutralReturnThreshold = 0.01f;
    public float neutralHoldTime = 0.5f;

    [Header("Cooldowns")]
    public float jumpCooldown = 0.25f;
    public float shootCooldown = 0.15f;

    [Header("Confirm Input")]
    public bool allowKeyboardConfirm = true;
    public KeyCode confirmKey = KeyCode.Space;
    public float confirmCooldown = 0.12f;

    float neutralTimer = 0f;
    float jumpTimer = 0f;
    float shootTimer = 0f;
    float confirmTimer = 0f;
    float pendingStateTimer = 0f;

    bool crouchLatched = false;
    bool isLoudAiming = false;
    VoiceState pendingState;

    void Awake()
    {
        pendingState = CurrentState;
    }

    void Update()
    {
        if (mic == null || player == null) return;

        float a = mic.SmoothedAmplitude;
        jumpTimer    -= Time.deltaTime;
        shootTimer   -= Time.deltaTime;
        confirmTimer -= Time.deltaTime;

        if (isLoudAiming && allowKeyboardConfirm && Input.GetKeyUp(confirmKey))
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
                pendingState = CurrentState;
                pendingStateTimer = 0f;
                ForceStand();
                return;
            }
        }
        else
        {
            neutralTimer = 0f;
        }

        // --- Update DISPLAY state with hysteresis ---
        ApplyStableState(a);

        if (isLoudAiming && laneCrosshair != null && CurrentState != VoiceState.Neutral)
        {
            laneCrosshair.SetLane(CurrentState);
        }

        if (allowKeyboardConfirm && Input.GetKeyDown(confirmKey) && confirmTimer <= 0f)
        {
            VoiceState confirmed = GetStateFromAmplitude(mic.SmoothedAmplitude);
            ConfirmResolvedState(confirmed, "Space", allowLoudAim: true);
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

    private void ApplyStableState(float amplitude)
    {
        VoiceState targetState = GetStateWithHysteresis(amplitude, CurrentState);

        if (targetState == CurrentState)
        {
            pendingState = CurrentState;
            pendingStateTimer = 0f;
            return;
        }

        if (targetState != pendingState)
        {
            pendingState = targetState;
            pendingStateTimer = 0f;
            return;
        }

        pendingStateTimer += Time.deltaTime;
        if (pendingStateTimer >= stateChangeHoldTime)
        {
            SetState(pendingState);
            pendingStateTimer = 0f;
        }
    }

    private void ConfirmState(VoiceState confirmed)
    {
        // If it's not Quiet, we should never remain crouched after any confirm input.
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

    private void ConfirmResolvedState(VoiceState confirmed, string sourceLabel, bool allowLoudAim)
    {
        confirmTimer = confirmCooldown;

        if (isLoudAiming)
        {
            ReleaseLoudAim();
            return;
        }

        if (confirmed == VoiceState.Loud && laneCrosshair != null && allowLoudAim)
        {
            BeginLoudAim();
            return;
        }

        Debug.Log($"[VAC] {sourceLabel} | confirmed={confirmed} amp={mic.SmoothedAmplitude:F4} (quietMin={quietMin:F3} quietMax={quietMax:F3} loudMin={loudMin:F3})");

        ConfirmState(confirmed);
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
