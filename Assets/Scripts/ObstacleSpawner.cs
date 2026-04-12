using UnityEngine;

public class ObstacleSpawner : MonoBehaviour
{
    public GameObject obstaclePrefab;
    public float spawnDelay = 2f;
    public bool spawnOnlyOnce = true;

    float timer;
    GameObject currentObstacle;
    bool hasSpawned;

    void Update()
    {
        if (obstaclePrefab == null) return;
        if (spawnOnlyOnce && hasSpawned) return;

        timer += Time.deltaTime;

        if (timer >= spawnDelay)
        {
            if (currentObstacle != null) return;

            SpawnObstacle();
            timer = 0f;
        }
    }

    void SpawnObstacle()
    {
        currentObstacle = Instantiate(obstaclePrefab, transform.position, Quaternion.identity);
        hasSpawned = true;
    }
}
