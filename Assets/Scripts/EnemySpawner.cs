using System.Collections.Generic;
using UnityEngine;

// Spawns enemies over time so you always have something to test against.
// Put this on an empty object on the far side of the map from the base.
public class EnemySpawner : MonoBehaviour
{
    [Header("References")]
    public GameObject enemyPrefab;          // drag the Enemy prefab here
    public Transform[] spawnPoints;         // optional: if empty, it spawns around this object

    [Header("Spawning (tune these)")]
    public float firstDelay = 2f;           // seconds before the first enemy
    public float interval = 4f;             // seconds between enemies
    public int maxAlive = 5;                // it waits when this many of its enemies are alive
    public float spawnRadius = 4f;          // random spread around the spawn point

    readonly List<GameObject> spawned = new List<GameObject>();
    float timer;

    void Start()
    {
        timer = firstDelay;

        // Build the pathfinding grid now, while the level loads, instead of when the first enemy appears.
        _ = NavGrid.Instance;
    }

    void Update()
    {
        timer -= Time.deltaTime;
        if (timer > 0f) return;

        // Forget enemies that have died.
        for (int i = spawned.Count - 1; i >= 0; i--)
            if (spawned[i] == null) spawned.RemoveAt(i);

        // Full: keep waiting, and spawn as soon as there is room.
        if (spawned.Count >= maxAlive) return;

        Spawn();
        timer = interval;
    }

    void Spawn()
    {
        if (enemyPrefab == null) return;

        Transform p = (spawnPoints != null && spawnPoints.Length > 0)
            ? spawnPoints[Random.Range(0, spawnPoints.Length)]
            : transform;

        Vector2 offset = Random.insideUnitCircle * spawnRadius;
        Vector3 pos = new Vector3(p.position.x + offset.x,
                                  enemyPrefab.transform.position.y,   // keep the height saved in the prefab
                                  p.position.z + offset.y);

        spawned.Add(Instantiate(enemyPrefab, pos, Quaternion.identity));
    }
}
