using System.Collections.Generic;
using UnityEngine;

// Spawns enemies over time. Spawns a fixed number in total (then the level can be won),
// or keeps going forever if Total Enemies is 0.
// Each spawn picks one of the Enemy Types at random; a higher weight means it is picked more often.
// If a WaveManager is in the scene, it takes over: it starts each wave on every spawner
// (with that wave's count, speed, enemy mix and strength) and the spawner waits in between.
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

    [Header("Waves (set by the WaveManager, no need to touch)")]
    public bool waveControlled;             // true = waits for the WaveManager to start each wave

    readonly List<GameObject> spawned = new List<GameObject>();
    float timer;
    int spawnedTotal;

    // Wave state
    bool waveRunning;
    float waveHealth = 1f, waveDamage = 1f;
    EnemyType[] waveTypes;                  // this wave's own mix (null = use Enemy Types)

    // While true the spawner never reports "finished" (so the level isn't won between waves).
    public bool KeepOpen { get; set; }

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
        get { return !KeepOpen && totalEnemies > 0 && spawnedTotal >= totalEnemies && Alive == 0; }
    }

    // The current wave has been spawned and defeated.
    public bool WaveDone
    {
        get { return waveRunning && spawnedTotal >= totalEnemies && Alive == 0; }
    }

    void OnEnable() { All.Add(this); }
    void OnDisable() { All.Remove(this); }

    void Start()
    {
        timer = firstDelay;

        // Build the pathfinding grid now, while the level loads, instead of when the first enemy appears.
        _ = NavGrid.Instance;
    }

    // Called by the WaveManager at the start of every wave.
    public void BeginWave(int count, float newInterval, int newMaxAlive,
                          float healthMultiplier, float damageMultiplier, EnemyType[] typesOverride)
    {
        waveControlled = true;
        waveRunning = true;

        totalEnemies = Mathf.Max(1, count);   // 0 would mean "endless"
        spawnedTotal = 0;
        interval = newInterval;
        maxAlive = newMaxAlive;

        waveHealth = healthMultiplier;
        waveDamage = damageMultiplier;
        waveTypes = typesOverride;

        timer = 0.5f;
    }

    void Update()
    {
        if (waveControlled && !waveRunning) return;                     // waiting for the next wave
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

    // Picks one prefab from the enemy mix by weight (falls back to the old single Enemy Prefab slot).
    GameObject PickPrefab()
    {
        EnemyType[] types = (waveTypes != null && waveTypes.Length > 0) ? waveTypes : enemyTypes;

        float total = 0f;
        GameObject last = null;

        if (types != null)
        {
            foreach (EnemyType t in types)
            {
                if (t == null || t.prefab == null || t.weight <= 0f) continue;
                total += t.weight;
                last = t.prefab;
            }
        }

        if (total <= 0f) return enemyPrefab;

        float roll = Random.value * total;
        foreach (EnemyType t in types)
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

        GameObject enemy = Instantiate(prefab, pos, Quaternion.identity);

        // Later waves are stronger.
        if (waveControlled)
        {
            EnemyController ec = enemy.GetComponent<EnemyController>();
            if (ec != null) ec.SetScaling(waveHealth, waveDamage);
        }

        spawned.Add(enemy);
        spawnedTotal++;
    }
}
