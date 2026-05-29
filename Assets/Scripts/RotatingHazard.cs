using UnityEngine;

public class RotatingHazard : MonoBehaviour
{
    // Forward movement speed for the hazard.
    public float moveSpeed = 3f;
    // Rotation speed in degrees per second.
    public float rotationSpeed = 180f;

    void Update()
    {
        // Move forward every frame.
        transform.Translate(Vector2.right * moveSpeed * Time.deltaTime);

        // Spin around the Z axis every frame.
        transform.Rotate(0f, 0f, rotationSpeed * Time.deltaTime);
    }

    void OnTriggerEnter2D(Collider2D other)
    {
        // Disable the player when the hazard touches them.
        if (other.CompareTag("Player"))
        {
            Debug.Log("Player hit by obstacle");

            // Find the menu manager and show the death screen.
            GameFlowManager flow = FindObjectOfType<GameFlowManager>();
            if (flow != null)
                flow.ShowDeathScreen();
            else
                Time.timeScale = 0f;
        }

        // Destroy the hazard when it hits the ground.
        if (other.CompareTag("Ground"))
        {
            Destroy(gameObject);
        }
    }
}
