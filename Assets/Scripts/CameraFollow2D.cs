using UnityEngine;

public class CameraFollow2D : MonoBehaviour
{
    // The object the camera should follow, usually the player.
    public Transform target;
    // Extra horizontal space so the player is not centered too tightly.
    public float xOffset = 4f;
    // Extra vertical space above or below the target.
    public float yOffset = 1f;
    // Time used by SmoothDamp to soften camera movement.
    public float smoothTime = 0.2f;
    // Turns vertical following on or off.
    public bool followVertical = true;
    // Allows the camera to rise when the player moves above the upper dead zone.
    public bool followUpwardMovement = false;
    // Allows the camera to drop when the player moves below the lower dead zone.
    public bool followDownwardMovement = true;
    // Distance the player can move upward before the camera follows.
    public float upperDeadZone = 1.5f;
    // Distance the player can move downward before the camera follows.
    public float lowerDeadZone = 0.5f;

    // SmoothDamp stores its current movement velocity here between frames.
    private Vector3 velocity = Vector3.zero;
    // The current vertical camera target after dead-zone rules are applied.
    private float anchoredY;
    // Tracks whether anchoredY has been initialized from the target yet.
    private bool hasAnchoredY;

    void LateUpdate()
    {
        // Do nothing until a valid target has been assigned.
        if (target == null) return;

        // Build the camera's desired position while keeping its existing Z depth.
        Vector3 desired = new Vector3(
            target.position.x + xOffset,
            ResolveY(),
            transform.position.z
        );

        // Move smoothly toward the desired camera position.
        transform.position = Vector3.SmoothDamp(transform.position, desired, ref velocity, smoothTime);
    }

    float ResolveY()
    {
        // If vertical following is disabled, keep the current camera height.
        if (!followVertical)
            return transform.position.y;

        // Start from the target's vertical position plus the configured offset.
        float targetY = target.position.y + yOffset;

        // First frame setup prevents the camera from snapping from zero.
        if (!hasAnchoredY)
        {
            anchoredY = targetY;
            hasAnchoredY = true;
            return anchoredY;
        }

        // Move the anchor upward only after the target leaves the upper dead zone.
        if (followUpwardMovement && targetY > anchoredY + upperDeadZone)
            anchoredY = targetY - upperDeadZone;

        // Move the anchor downward only after the target leaves the lower dead zone.
        if (followDownwardMovement && targetY < anchoredY - lowerDeadZone)
            anchoredY = targetY + lowerDeadZone;

        // Return the final vertical position the camera should follow.
        return anchoredY;
    }

    void OnValidate()
    {
        // Keep inspector values safe when they are edited in Unity.
        smoothTime = Mathf.Max(0f, smoothTime);
        upperDeadZone = Mathf.Max(0f, upperDeadZone);
        lowerDeadZone = Mathf.Max(0f, lowerDeadZone);
    }
}
