using UnityEngine;

public class RotatingHazard : MonoBehaviour
{
    public float moveSpeed = 3f;
    public float rotationSpeed = 180f;

    void Update()
    {
        // Move forward
        transform.Translate(Vector2.right * moveSpeed * Time.deltaTime);

        // Rotate
        transform.Rotate(0f, 0f, rotationSpeed * Time.deltaTime);
    }

    void OnTriggerEnter2D(Collider2D other)
    {
        // Kill player
        if (other.CompareTag("Player"))
        {
            Debug.Log("Player hit by obstacle");
            other.gameObject.SetActive(false);
        }

        // Destroy if it hits the ground
        if (other.CompareTag("Ground"))
        {
            Destroy(gameObject);
        }
    }
}