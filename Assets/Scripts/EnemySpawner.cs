using System.Collections.Generic;
using UnityEngine;

// Spawns enemies over time. Spawns a fixed number in total (then the level can be won),
// or keeps going forever if Total Enemies is 0.
// Put this on an empty object on the far side of the map from the base.
public class EnemySpawner : MonoBehaviour
{
    public static readonly List<EnemySpawner> All = new List<EnemySpawner>();

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    static void ResetStatics() { All.Clear(); }

    [Header("References")]
    public GameObject enemyPrefab;          // drag the Enemy prefab here
    public Transform[] spawnPoints;         // optional: if empty, it spawns around this object

    [Header("Spawning (tune these)")]
    public int totalEnemies = 15;           // how many it spawns in total (0 = endless, no victory)
    public float firstDelay = 2f;           // seconds before the first enemy
    public float interval = 4f;             // seconds between enemies
    public int maxAlive = 5;                // it waits when this many of its enemies are alive
    public float spawnRadius = 4f;          // random spread around the spawn point

    readonly List<GameObject> spawned = new List<GameObject>();
    float timer;
    int spawnedTotal;

    // Enemies of this spawner that are alive right now.
    public int Alive
    {
        get { Prune(); return spawned.Count; }
    }

    // Enemies still to be defeated: not spawned yet plus alive (0 when endless).
    public int Remaining
    {
        get { return totalEnemies > 0 ? (totalEnemies - spawnedTotal) + Alive : 0; }
    }

    // Everything has been spawned and defeated.
    public bool IsFinished
    {
        get { return totalEnemies > 0 && spawnedTotal >= totalEnemies && Alive == 0; }
    }

    void OnEnable() { All.Add(this); }
    void OnDisable() { All.Remove(this); }

    void Start()
    {
        timer = firstDelay;

        // Build the pathfinding grid now, while the level loads, instead of when the first enemy appears.
        _ = NavGrid.Instance;
    }

    void Update()
    {
        if (totalEnemies > 0 && spawnedTotal >= totalEnemies) return;   // all spawned

        timer -= Time.deltaTime;
        if (timer > 0f) return;

        // Full: keep waiting, and spawn as soon as there is room.
        Prune();
        if (spawned.Count >= maxAlive) return;

        Spawn();
        timer = interval;
    }

    // Forget enemies that have died.
    void Prune()
    {
        for (int i = spawned.Count - 1; i >= 0; i--)
            if (spawned[i] == null) spawned.RemoveAt(i);
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
        spawnedTotal++;
    }
}
