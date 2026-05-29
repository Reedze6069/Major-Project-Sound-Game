using UnityEngine;

public class EnemySpawnOnApproach : MonoBehaviour
{
    [Header("Spawn")]
    // Enemy prefab that will be created when the spawn point enters view.
    public GameObject enemyPrefab;
    // Position and rotation used for the spawned enemy.
    public Transform spawnPoint;
    // Prevents the same spawner from creating multiple enemies.
    public bool spawnOnlyOnce = true;

    [Header("Activation")]
    [Tooltip("Spawn when this point is just off the right side of the screen.")]
    // Viewport X value where 1 is the right edge of the camera.
    public float spawnViewportX = 1.1f;
    // Camera used to check when the spawn point is near the screen.
    public Camera targetCamera;

    // Tracks whether this spawner has already created its enemy.
    bool hasSpawned;

    void Awake()
    {
        // Use this object's transform as the spawn point if none was assigned.
        if (spawnPoint == null)
            spawnPoint = transform;

        // Default to the main camera if a specific camera was not assigned.
        if (targetCamera == null)
            targetCamera = Camera.main;
    }

    void Update()
    {
        // Stop early if required references are missing or spawning is finished.
        if (enemyPrefab == null) return;
        if (spawnOnlyOnce && hasSpawned) return;
        if (targetCamera == null) return;

        // Convert the world position into viewport space for screen-edge checks.
        Vector3 viewport = targetCamera.WorldToViewportPoint(spawnPoint.position);

        // Ignore points behind the camera.
        if (viewport.z <= 0f) return;
        // Wait until the point reaches the configured right-side viewport value.
        if (viewport.x > spawnViewportX) return;
        // Ignore points that have already moved too far past the left side.
        if (viewport.x < -0.1f) return;

        SpawnEnemy();
    }

    void SpawnEnemy()
    {
        // Create the enemy and remember that this spawner has fired.
        Instantiate(enemyPrefab, spawnPoint.position, spawnPoint.rotation);
        hasSpawned = true;
    }
}
