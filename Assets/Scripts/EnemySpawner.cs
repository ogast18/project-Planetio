using System.Collections.Generic;
using UnityEngine;

// Spawns enemies over time. Spawns a fixed number in total (then the level can be won),
// or keeps going forever if Total Enemies is 0.
// Each spawn picks one of the Enemy Types at random; a higher weight means it is picked more often.
// Put this on an empty object on the far side of the map from the base.
public class EnemySpawner : MonoBehaviour
{
    public static readonly List<EnemySpawner> All = new List<EnemySpawner>();

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    static void ResetStatics() { All.Clear(); }

    [System.Serializable]
    public class EnemyType
    {
        public GameObject prefab;           // e.g. the Warrior, Thrower or Detonator prefab
        [Min(0)] public float weight = 1f;  // 0 = never picked; 2 = twice as likely as 1
    }

    [Header("References")]
    public EnemyType[] enemyTypes;          // the mix of enemies this spawner can create
    public GameObject enemyPrefab;          // old single slot: only used if Enemy Types is empty
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

    // Picks one prefab from Enemy Types by weight (falls back to the old single Enemy Prefab slot).
    GameObject PickPrefab()
    {
        float total = 0f;
        GameObject last = null;

        if (enemyTypes != null)
        {
            foreach (EnemyType t in enemyTypes)
            {
                if (t == null || t.prefab == null || t.weight <= 0f) continue;
                total += t.weight;
                last = t.prefab;
            }
        }

        if (total <= 0f) return enemyPrefab;

        float roll = Random.value * total;
        foreach (EnemyType t in enemyTypes)
        {
            if (t == null || t.prefab == null || t.weight <= 0f) continue;
            roll -= t.weight;
            if (roll <= 0f) return t.prefab;
        }
        return last;   // only reached by rounding at the very end
    }

    void Spawn()
    {
        GameObject prefab = PickPrefab();
        if (prefab == null) return;

        Transform p = (spawnPoints != null && spawnPoints.Length > 0)
            ? spawnPoints[Random.Range(0, spawnPoints.Length)]
            : transform;

        Vector2 offset = Random.insideUnitCircle * spawnRadius;
        Vector3 pos = new Vector3(p.position.x + offset.x,
                                  prefab.transform.position.y,   // keep the height saved in the prefab
                                  p.position.z + offset.y);

        spawned.Add(Instantiate(prefab, pos, Quaternion.identity));
        spawnedTotal++;
    }
}
