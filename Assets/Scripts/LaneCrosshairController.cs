using UnityEngine;

public class LaneCrosshairController : MonoBehaviour
{
    // The three vertical aim lanes the crosshair can move between.
    public enum AimLane { Low, Middle, High }

    [Header("Placement")]
    // If false, quiet voice input aims straight instead of downward.
    public bool allowDownwardAim = false;
    // Transform the crosshair follows, usually the player or fire point.
    public Transform anchor;
    // Horizontal distance in front of the anchor.
    public float xOffset = 6f;
    // Vertical offset for the low aim lane.
    public float lowLaneY = -1.5f;
    // Vertical offset for the middle aim lane.
    public float middleLaneY = 0f;
    // Vertical offset for the high aim lane.
    public float highLaneY = 1.5f;
    // SmoothDamp time used when moving between lanes.
    public float moveSmoothTime = 0.08f;

    [Header("Tether")]
    // Draws a line from the anchor to the crosshair when enabled.
    public bool useTetherLine = false;
    // Optional line renderer used for the tether.
    public LineRenderer tetherLine;

    // Renderers that get enabled and disabled when the crosshair is shown or hidden.
    Renderer[] cachedRenderers;
    // Currently selected aim lane.
    AimLane currentLane = AimLane.Middle;
    // Tracks whether the crosshair should be visible and active.
    bool isVisible;
    // SmoothDamp velocity used while moving between lane positions.
    Vector3 currentVelocity;

    void Awake()
    {
        // Cache all child renderers so visibility can be toggled cheaply.
        cachedRenderers = GetComponentsInChildren<Renderer>(true);
        ApplyVisibility(false);
        // Use a LineRenderer on this object if none was assigned manually.
        if (tetherLine == null)
            tetherLine = GetComponent<LineRenderer>();

        // Start with the tether hidden.
        if (tetherLine != null)
            tetherLine.enabled = false;
    }

    void LateUpdate()
    {
        // Only update movement while visible and following a valid anchor.
        if (!isVisible || anchor == null) return;

        // Smoothly move toward the world position for the current lane.
        Vector3 target = GetWorldPositionForLane(currentLane);
        transform.position = Vector3.SmoothDamp(transform.position, target, ref currentVelocity, moveSmoothTime);
        UpdateTether();
    }

    public void Show(Transform followAnchor)
    {
        // Begin following the provided anchor and make the crosshair visible.
        anchor = followAnchor;
        isVisible = true;
        ApplyVisibility(true);
        // Reset smoothing so the crosshair starts cleanly at the selected lane.
        currentVelocity = Vector3.zero;
        transform.position = GetWorldPositionForLane(currentLane);
        UpdateTether();
    }

    public void Hide()
    {
        // Stop showing the crosshair and hide any tether line.
        isVisible = false;
        ApplyVisibility(false);

        if (tetherLine != null)
            tetherLine.enabled = false;
    }

    public void SetLane(VoiceActionController.VoiceState state)
    {
        // Convert the current voice state into a crosshair lane.
        switch (state)
        {
            case VoiceActionController.VoiceState.Idle:
                currentLane = AimLane.Middle;
                break;

            case VoiceActionController.VoiceState.Quiet:
                currentLane = allowDownwardAim ? AimLane.Low : AimLane.Middle;
                break;

            case VoiceActionController.VoiceState.Medium:
                currentLane = AimLane.Middle;
                break;

            case VoiceActionController.VoiceState.Loud:
                currentLane = AimLane.High;
                break;
        }

    }

    public Vector2 GetAimDirection(Vector2 origin)
    {
        // Aim at the crosshair if it is visible, otherwise shoot straight right.
        Vector2 target = isVisible && anchor != null
            ? (Vector2)GetWorldPositionForLane(currentLane)
            : origin + Vector2.right;

        // Normalize the direction and guard against a zero-length vector.
        Vector2 direction = (target - origin).normalized;
        return direction.sqrMagnitude > 0.0001f ? direction : Vector2.right;
    }

    Vector3 GetWorldPositionForLane(AimLane lane)
    {
        // Start from the anchor position, falling back to the current position.
        Vector3 basePosition = anchor != null ? anchor.position : transform.position;
        float laneY = middleLaneY;

        // Choose the vertical offset that matches the requested lane.
        switch (lane)
        {
            case AimLane.Low:
                laneY = lowLaneY;
                break;

            case AimLane.High:
                laneY = highLaneY;
                break;
        }

        // Keep this object's current Z so sprite sorting depth stays unchanged.
        return new Vector3(basePosition.x + xOffset, basePosition.y + laneY, transform.position.z);
    }

    void SnapToLane()
    {
        // Instantly place the crosshair on its current lane.
        if (anchor == null) return;
        transform.position = GetWorldPositionForLane(currentLane);
    }

    void UpdateTether()
    {
        // Skip the tether unless it is enabled and all references are valid.
        if (!useTetherLine || tetherLine == null || anchor == null)
            return;

        // Draw a simple two-point line from the anchor to the crosshair.
        tetherLine.enabled = true;
        tetherLine.positionCount = 2;
        tetherLine.SetPosition(0, anchor.position);
        tetherLine.SetPosition(1, transform.position);
    }

    void ApplyVisibility(bool visible)
    {
        // Nothing to toggle until Awake has cached the renderers.
        if (cachedRenderers == null) return;

        // Toggle every child renderer so the full crosshair appears or disappears.
        for (int i = 0; i < cachedRenderers.Length; i++)
            cachedRenderers[i].enabled = visible;

        // Always hide the tether when the crosshair itself is hidden.
        if (!visible && tetherLine != null)
            tetherLine.enabled = false;
    }
}
