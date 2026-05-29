using UnityEngine;

public class ObstacleSpawner : MonoBehaviour
{
    // Prefab that will be spawned by this object.
    public GameObject obstaclePrefab;
    // Time in seconds before spawning an obstacle.
    public float spawnDelay = 2f;
    // Stops this spawner after the first obstacle is created.
    public bool spawnOnlyOnce = true;

    // Counts up until spawnDelay is reached.
    float timer;
    // Reference to the currently spawned obstacle.
    GameObject currentObstacle;
    // Tracks whether a one-shot spawn has already happened.
    bool hasSpawned;

    void Update()
    {
        // Stop if there is no prefab or this one-shot spawner has already fired.
        if (obstaclePrefab == null) return;
        if (spawnOnlyOnce && hasSpawned) return;

        // Advance the spawn timer.
        timer += Time.deltaTime;

        if (timer >= spawnDelay)
        {
            // Do not spawn another obstacle while the current one still exists.
            if (currentObstacle != null) return;

            SpawnObstacle();
            timer = 0f;
        }
    }

    void SpawnObstacle()
    {
        // Create the obstacle at this spawner's position with no rotation.
        currentObstacle = Instantiate(obstaclePrefab, transform.position, Quaternion.identity);
        hasSpawned = true;
    }
}
