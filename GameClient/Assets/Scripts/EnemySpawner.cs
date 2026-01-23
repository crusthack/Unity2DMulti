using UnityEngine;
using System.Collections.Generic;

public class EnemySpawner : MonoBehaviour
{
    public GameObject EnemyPrefab;
    Collider2D SpawnArea;

    public float SpawnInterval = 2f;
    public int MaxEnemies = 20;
    List<GameObject> EnemyPool;
    public int CurrentEnemyCount = 0;

    float SpawnTimer = 0f;


    void Awake()
    {
        SpawnArea = GetComponent<Collider2D>();
        SpawnArea.isTrigger = true;

        EnemyPool = new();
    }
    void Start()
    {

    }

    void Update()
    {
        SpawnTimer += Time.deltaTime;
        if (SpawnTimer >= SpawnInterval)
        {
            SpawnTimer = 0f;
            if (CurrentEnemyCount < MaxEnemies)
            {
                SpawnEnemy();
                CurrentEnemyCount++;
            }
        }
    }

    void SpawnEnemy()
    {
        Vector2 spawnPoint = Vector2.zero;
        int maxAttempts = 10;
        int attempts = 0;
        do
        {
            spawnPoint = GetRandomSpawnPoint();
            attempts++;
        } while (!IsPositionValid(spawnPoint) && attempts < maxAttempts);
        if (attempts < maxAttempts)
        {
            foreach (var e in EnemyPool)
            {
                if (!e.activeSelf)
                {
                    e.transform.position = spawnPoint;
                    e.SetActive(true);
                    return;
                }
            }
            var enemy = Instantiate(EnemyPrefab, spawnPoint, Quaternion.identity, this.transform);

            EnemyPool.Add(enemy);
        }
        else
        {
            Debug.LogWarning("Failed to find a valid spawn point for the enemy.");
        }
    }

    Vector2 GetRandomSpawnPoint()
    {
        Bounds bounds = SpawnArea.bounds;
        float x = Random.Range(bounds.min.x, bounds.max.x);
        float y = Random.Range(bounds.min.y, bounds.max.y);
        Vector2 spawnPoint = new Vector2(x, y);

        return spawnPoint;
    }

    bool IsPositionValid(Vector2 pos)
    {
        float radius = 1f;
        return !Physics2D.OverlapCircle(
            pos,
            radius,
            Physics2D.AllLayers
        );
    }
}
