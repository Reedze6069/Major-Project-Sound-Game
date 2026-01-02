using UnityEngine;

public class CameraFollow2D : MonoBehaviour
{
    public Transform target;
    public float xOffset = 4f;
    public float yOffset = 1f;
    public float smoothTime = 0.2f;

    private Vector3 velocity = Vector3.zero;

    void LateUpdate()
    {
        if (target == null) return;

        Vector3 desired = new Vector3(
            target.position.x + xOffset,
            target.position.y + yOffset,
            transform.position.z
        );

        transform.position = Vector3.SmoothDamp(transform.position, desired, ref velocity, smoothTime);
    }
}