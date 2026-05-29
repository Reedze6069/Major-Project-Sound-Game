using UnityEngine;

public class CameraFollow2D : MonoBehaviour
{
    public Transform target;
    public float xOffset = 4f;
    public float yOffset = 1f;
    public float smoothTime = 0.2f;
    public bool followVertical = true;
    public bool followUpwardMovement = false;
    public bool followDownwardMovement = true;
    public float upperDeadZone = 1.5f;
    public float lowerDeadZone = 0.5f;

    private Vector3 velocity = Vector3.zero;
    private float anchoredY;
    private bool hasAnchoredY;

    void LateUpdate()
    {
        if (target == null) return;

        Vector3 desired = new Vector3(
            target.position.x + xOffset,
            ResolveY(),
            transform.position.z
        );

        transform.position = Vector3.SmoothDamp(transform.position, desired, ref velocity, smoothTime);
    }

    float ResolveY()
    {
        if (!followVertical)
            return transform.position.y;

        float targetY = target.position.y + yOffset;

        if (!hasAnchoredY)
        {
            anchoredY = targetY;
            hasAnchoredY = true;
            return anchoredY;
        }

        if (followUpwardMovement && targetY > anchoredY + upperDeadZone)
            anchoredY = targetY - upperDeadZone;

        if (followDownwardMovement && targetY < anchoredY - lowerDeadZone)
            anchoredY = targetY + lowerDeadZone;

        return anchoredY;
    }

    void OnValidate()
    {
        smoothTime = Mathf.Max(0f, smoothTime);
        upperDeadZone = Mathf.Max(0f, upperDeadZone);
        lowerDeadZone = Mathf.Max(0f, lowerDeadZone);
    }
}
