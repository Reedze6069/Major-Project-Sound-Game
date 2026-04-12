using UnityEngine;

public class LaneCrosshairController : MonoBehaviour
{
    public enum AimLane { Low, Middle, High }

    [Header("Placement")]
    public bool allowDownwardAim = false;
    public Transform anchor;
    public float xOffset = 6f;
    public float lowLaneY = -1.5f;
    public float middleLaneY = 0f;
    public float highLaneY = 1.5f;
    public float moveSmoothTime = 0.08f;

    [Header("Tether")]
    public bool useTetherLine = false;
    public LineRenderer tetherLine;

    Renderer[] cachedRenderers;
    AimLane currentLane = AimLane.Middle;
    bool isVisible;
    Vector3 currentVelocity;

    void Awake()
    {
        cachedRenderers = GetComponentsInChildren<Renderer>(true);
        ApplyVisibility(false);
        if (tetherLine == null)
            tetherLine = GetComponent<LineRenderer>();

        if (tetherLine != null)
            tetherLine.enabled = false;
    }

    void LateUpdate()
    {
        if (!isVisible || anchor == null) return;

        Vector3 target = GetWorldPositionForLane(currentLane);
        transform.position = Vector3.SmoothDamp(transform.position, target, ref currentVelocity, moveSmoothTime);
        UpdateTether();
    }

    public void Show(Transform followAnchor)
    {
        anchor = followAnchor;
        isVisible = true;
        ApplyVisibility(true);
        currentVelocity = Vector3.zero;
        transform.position = GetWorldPositionForLane(currentLane);
        UpdateTether();
    }

    public void Hide()
    {
        isVisible = false;
        ApplyVisibility(false);

        if (tetherLine != null)
            tetherLine.enabled = false;
    }

    public void SetLane(VoiceActionController.VoiceState state)
    {
        switch (state)
        {
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
        Vector2 target = isVisible && anchor != null
            ? (Vector2)GetWorldPositionForLane(currentLane)
            : origin + Vector2.right;

        Vector2 direction = (target - origin).normalized;
        return direction.sqrMagnitude > 0.0001f ? direction : Vector2.right;
    }

    Vector3 GetWorldPositionForLane(AimLane lane)
    {
        Vector3 basePosition = anchor != null ? anchor.position : transform.position;
        float laneY = middleLaneY;

        switch (lane)
        {
            case AimLane.Low:
                laneY = lowLaneY;
                break;

            case AimLane.High:
                laneY = highLaneY;
                break;
        }

        return new Vector3(basePosition.x + xOffset, basePosition.y + laneY, transform.position.z);
    }

    void SnapToLane()
    {
        if (anchor == null) return;
        transform.position = GetWorldPositionForLane(currentLane);
    }

    void UpdateTether()
    {
        if (!useTetherLine || tetherLine == null || anchor == null)
            return;

        tetherLine.enabled = true;
        tetherLine.positionCount = 2;
        tetherLine.SetPosition(0, anchor.position);
        tetherLine.SetPosition(1, transform.position);
    }

    void ApplyVisibility(bool visible)
    {
        if (cachedRenderers == null) return;

        for (int i = 0; i < cachedRenderers.Length; i++)
            cachedRenderers[i].enabled = visible;

        if (!visible && tetherLine != null)
            tetherLine.enabled = false;
    }
}
