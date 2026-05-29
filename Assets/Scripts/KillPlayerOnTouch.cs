using UnityEngine;

public class KillPlayerOnTouch : MonoBehaviour
{
    private void OnTriggerEnter2D(Collider2D other)
    {
        // Only react when the object entering this trigger is tagged as the player.
        if (other.CompareTag("Player"))
        {
            // Log the death event so it is visible in the Unity Console.
            Debug.Log("Player died!");

            // Find the menu manager and show the death screen.
            GameFlowManager flow = FindObjectOfType<GameFlowManager>();
            if (flow != null)
                flow.ShowDeathScreen();
            else
                Time.timeScale = 0f;
        }
    }
}
