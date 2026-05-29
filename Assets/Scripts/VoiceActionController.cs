using UnityEngine;
using UnityEngine.Serialization;

public class VoiceActionController : MonoBehaviour
{
    // Voice bands used by the rest of the gameplay systems.
    public enum VoiceState { Idle, Quiet, Medium, Loud }
    // Current voice band detected from the microphone amplitude.
    public VoiceState CurrentState { get; private set; } = VoiceState.Idle;

    [Header("References")]
    // Microphone input that provides smoothed amplitude values.
    public MicrophoneInput mic;
    // Player controller that receives crouch and jump actions.
    public PlayerController player;

    [Header("Threshold Bands")]
    [FormerlySerializedAs("neutralReturnThreshold")]
    [Tooltip("Anything below this is treated as Idle (silence / noise floor).")]
    // Amplitude below this value starts returning the voice state to Idle.
    public float idleThreshold = 0.01f;

    [Tooltip("Quiet band upper bound.")]
    // Highest amplitude that still counts as Quiet.
    public float quietMax = 0.16f;

    [Tooltip("Loud band lower bound.")]
    // Lowest amplitude that counts as Loud.
    public float loudMin = 0.44f;

    [Header("Stability")]
    [Tooltip("Extra margin needed to move upward into a stronger band. Kept at 0 for exact threshold switching.")]
    // Reserved tuning value for upward threshold hysteresis.
    public float riseHysteresis = 0f;

    [Tooltip("Small margin used when falling back down. Kept at 0 for exact threshold switching.")]
    // Reserved tuning value for downward threshold hysteresis.
    public float fallHysteresis = 0f;

    [Tooltip("How long a new band must stay stable before the state changes.")]
    // Time a candidate voice state must remain stable before being applied.
    public float stateChangeHoldTime = 0f;

    [Header("Idle Return")]
    [FormerlySerializedAs("neutralHoldTime")]
    // Time below idleThreshold before the controller returns to Idle.
    public float idleHoldTime = 0.5f;

    [Header("Loud Trigger")]
    [Tooltip("Minimum time between loud-entry jump attempts.")]
    // Cooldown that prevents repeated jumps from one loud sound.
    public float jumpCooldown = 0.25f;

    // Counts how long the input has stayed below the idle threshold.
    float idleTimer;
    // Counts down until another loud jump is allowed.
    float jumpTimer;
    // Counts how long the pending state has stayed stable.
    float pendingStateTimer;
    // Candidate state waiting to pass the hold time.
    VoiceState pendingState;

    void Awake()
    {
        // Initialize the pending state to match the starting current state.
        pendingState = CurrentState;
    }

    void Update()
    {
        // This controller cannot drive gameplay without both references.
        if (mic == null || player == null) return;

        // Read the current mic volume and advance cooldown timers.
        float amplitude = mic.SmoothedAmplitude;
        jumpTimer -= Time.deltaTime;

        // Store the previous state so state-change actions can run once.
        VoiceState previousState = CurrentState;

        if (amplitude < idleThreshold)
        {
            // Below the idle threshold, wait before returning fully to Idle.
            pendingState = CurrentState;
            pendingStateTimer = 0f;
            idleTimer += Time.deltaTime;

            if (idleTimer >= idleHoldTime)
            {
                // After the hold time, commit the Idle state.
                SetState(VoiceState.Idle);
                pendingState = CurrentState;
                pendingStateTimer = 0f;
            }
        }
        else
        {
            // Any usable sound resets the idle timer and updates the voice band.
            idleTimer = 0f;
            ApplyStableState(amplitude);
        }

        // Apply continuous actions for the current state.
        ApplyHeldActions();

        if (CurrentState != previousState)
        {
            // Run one-shot actions when the state changes.
            HandleStateChanged(previousState, CurrentState, amplitude);
        }
    }

    private VoiceState GetStateFromAmplitude(float amplitude)
    {
        // Convert the current amplitude into one of the configured voice bands.
        if (amplitude < idleThreshold) return VoiceState.Idle;
        if (amplitude <= quietMax) return VoiceState.Quiet;
        if (amplitude >= loudMin) return VoiceState.Loud;
        return VoiceState.Medium;
    }

    private void ApplyStableState(float amplitude)
    {
        // Reset pending state if the sound dropped back below the idle threshold.
        if (amplitude < idleThreshold)
        {
            pendingState = CurrentState;
            pendingStateTimer = 0f;
            return;
        }

        // Choose the target state from the current amplitude.
        VoiceState targetState = GetStateFromAmplitude(amplitude);

        if (CurrentState == VoiceState.Idle)
        {
            // Leave Idle immediately when valid sound starts.
            SetState(targetState);
            pendingState = targetState;
            pendingStateTimer = 0f;
            return;
        }

        if (targetState == CurrentState)
        {
            // No transition is needed when the amplitude still matches current state.
            pendingState = CurrentState;
            pendingStateTimer = 0f;
            return;
        }

        if (targetState != pendingState)
        {
            // Start timing a new candidate state.
            pendingState = targetState;
            pendingStateTimer = 0f;

            if (stateChangeHoldTime <= 0f)
            {
                // With no hold time, switch immediately.
                SetState(pendingState);
            }

            return;
        }

        if (stateChangeHoldTime <= 0f)
        {
            // Immediate switching path when no stability delay is configured.
            SetState(pendingState);
            pendingStateTimer = 0f;
            return;
        }

        // Commit the pending state once it has stayed stable long enough.
        pendingStateTimer += Time.deltaTime;
        if (pendingStateTimer >= stateChangeHoldTime)
        {
            SetState(pendingState);
            pendingStateTimer = 0f;
        }
    }

    private void ApplyHeldActions()
    {
        // Quiet voice input keeps the player crouching; other states stand up.
        player.SetCrouch(CurrentState == VoiceState.Quiet);
    }

    private void HandleStateChanged(VoiceState previousState, VoiceState newState, float amplitude)
    {
        // Log state changes so threshold tuning is easier during playtesting.
        Debug.Log($"[VAC] state {previousState} -> {newState} amp={amplitude:F4}");

        // Only the moment of entering Loud should trigger a jump.
        if (newState != VoiceState.Loud || jumpTimer > 0f)
            return;

        // Jump once and restart the cooldown.
        player.Jump();
        jumpTimer = jumpCooldown;
    }

    private void SetState(VoiceState state)
    {
        // Central setter keeps state assignment easy to find.
        CurrentState = state;
    }
}
