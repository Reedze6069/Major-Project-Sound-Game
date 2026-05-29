using UnityEngine;
using UnityEngine.Serialization;

public class VoiceActionController : MonoBehaviour
{
    public enum VoiceState { Idle, Quiet, Medium, Loud }
    public VoiceState CurrentState { get; private set; } = VoiceState.Idle;

    [Header("References")]
    public MicrophoneInput mic;
    public PlayerController player;

    [Header("Threshold Bands")]
    [FormerlySerializedAs("neutralReturnThreshold")]
    [Tooltip("Anything below this is treated as Idle (silence / noise floor).")]
    public float idleThreshold = 0.01f;

    [Tooltip("Quiet band upper bound.")]
    public float quietMax = 0.16f;

    [Tooltip("Loud band lower bound.")]
    public float loudMin = 0.44f;

    [Header("Stability")]
    [Tooltip("Extra margin needed to move upward into a stronger band. Kept at 0 for exact threshold switching.")]
    public float riseHysteresis = 0f;

    [Tooltip("Small margin used when falling back down. Kept at 0 for exact threshold switching.")]
    public float fallHysteresis = 0f;

    [Tooltip("How long a new band must stay stable before the state changes.")]
    public float stateChangeHoldTime = 0f;

    [Header("Idle Return")]
    [FormerlySerializedAs("neutralHoldTime")]
    public float idleHoldTime = 0.5f;

    [Header("Loud Trigger")]
    [Tooltip("Minimum time between loud-entry jump attempts.")]
    public float jumpCooldown = 0.25f;

    float idleTimer;
    float jumpTimer;
    float pendingStateTimer;
    VoiceState pendingState;

    void Awake()
    {
        pendingState = CurrentState;
    }

    void Update()
    {
        if (mic == null || player == null) return;

        float amplitude = mic.SmoothedAmplitude;
        jumpTimer -= Time.deltaTime;

        VoiceState previousState = CurrentState;

        if (amplitude < idleThreshold)
        {
            pendingState = CurrentState;
            pendingStateTimer = 0f;
            idleTimer += Time.deltaTime;

            if (idleTimer >= idleHoldTime)
            {
                SetState(VoiceState.Idle);
                pendingState = CurrentState;
                pendingStateTimer = 0f;
            }
        }
        else
        {
            idleTimer = 0f;
            ApplyStableState(amplitude);
        }

        ApplyHeldActions();

        if (CurrentState != previousState)
        {
            HandleStateChanged(previousState, CurrentState, amplitude);
        }
    }

    private VoiceState GetStateFromAmplitude(float amplitude)
    {
        if (amplitude < idleThreshold) return VoiceState.Idle;
        if (amplitude <= quietMax) return VoiceState.Quiet;
        if (amplitude >= loudMin) return VoiceState.Loud;
        return VoiceState.Medium;
    }

    private void ApplyStableState(float amplitude)
    {
        if (amplitude < idleThreshold)
        {
            pendingState = CurrentState;
            pendingStateTimer = 0f;
            return;
        }

        VoiceState targetState = GetStateFromAmplitude(amplitude);

        if (CurrentState == VoiceState.Idle)
        {
            SetState(targetState);
            pendingState = targetState;
            pendingStateTimer = 0f;
            return;
        }

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

            if (stateChangeHoldTime <= 0f)
            {
                SetState(pendingState);
            }

            return;
        }

        if (stateChangeHoldTime <= 0f)
        {
            SetState(pendingState);
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

    private void ApplyHeldActions()
    {
        player.SetCrouch(CurrentState == VoiceState.Quiet);
    }

    private void HandleStateChanged(VoiceState previousState, VoiceState newState, float amplitude)
    {
        Debug.Log($"[VAC] state {previousState} -> {newState} amp={amplitude:F4}");

        if (newState != VoiceState.Loud || jumpTimer > 0f)
            return;

        player.Jump();
        jumpTimer = jumpCooldown;
    }

    private void SetState(VoiceState state)
    {
        CurrentState = state;
    }
}
