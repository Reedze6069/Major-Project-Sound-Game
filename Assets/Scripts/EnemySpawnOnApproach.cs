using UnityEngine;

public class EnemySpawnOnApproach : MonoBehaviour
{
    [Header("Spawn")]
    public GameObject enemyPrefab;
    public Transform spawnPoint;
    public bool spawnOnlyOnce = true;

    [Header("Activation")]
    [Tooltip("Spawn when this point is just off the right side of the screen.")]
    public float spawnViewportX = 1.1f;
    public Camera targetCamera;

    bool hasSpawned;

    void Awake()
    {
        if (spawnPoint == null)
            spawnPoint = transform;

        if (targetCamera == null)
            targetCamera = Camera.main;
    }

    void Update()
    {
        if (enemyPrefab == null) return;
        if (spawnOnlyOnce && hasSpawned) return;
        if (targetCamera == null) return;

        Vector3 viewport = targetCamera.WorldToViewportPoint(spawnPoint.position);

        if (viewport.z <= 0f) return;
        if (viewport.x > spawnViewportX) return;
        if (viewport.x < -0.1f) return;

        SpawnEnemy();
    }

    void SpawnEnemy()
    {
        Instantiate(enemyPrefab, spawnPoint.position, spawnPoint.rotation);
        hasSpawned = true;
    }
}
