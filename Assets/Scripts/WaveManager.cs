using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;

// Runs the waves of a level: wave 1, a break, wave 2, a break, wave 3...
// It starts every wave on every EnemySpawner in the scene (each spawner spawns the wave's count),
// waits until the wave is defeated, then runs a break. The base repairs itself during breaks (see PlayerBase).
// The level counts as won only after the LAST wave is defeated, so your existing win check keeps working.
// Put this on an empty object in the scene.
public class WaveManager : MonoBehaviour
{
    public static WaveManager Instance { get; private set; }

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    static void ResetStatics() { Instance = null; }

    [System.Serializable]
    public class Wave
    {
        [Min(1), Tooltip("How many enemies EACH spawner creates in this wave")]
        public int enemiesPerSpawner = 8;
        [Min(0.1f), Tooltip("Seconds between spawns")]
        public float interval = 3f;
        [Min(1), Tooltip("A spawner waits while this many of its enemies are alive")]
        public int maxAlive = 5;
        [Min(0.1f), Tooltip("Enemy health multiplier for this wave")]
        public float healthMultiplier = 1f;
        [Min(0.1f), Tooltip("Enemy damage multiplier for this wave")]
        public float damageMultiplier = 1f;
        [Tooltip("Optional: this wave's own enemy mix. Empty = each spawner's own Enemy Types")]
        public EnemySpawner.EnemyType[] enemyTypes;
        [Min(0), Tooltip("Break after this wave, in seconds (not used after the last wave)")]
        public float breakSeconds = 10f;
    }

    [Header("Waves (tune these)")]
    public float firstWaveDelay = 3f;       // seconds before wave 1
    public Wave[] waves =
    {
        new Wave { enemiesPerSpawner = 8,  healthMultiplier = 1f,   damageMultiplier = 1f   },
        new Wave { enemiesPerSpawner = 12, healthMultiplier = 1.3f, damageMultiplier = 1.2f },
        new Wave { enemiesPerSpawner = 16, healthMultiplier = 1.6f, damageMultiplier = 1.4f },
    };

    [Header("Temporary on-screen text and skip button")]
    public bool showHud = true;             // turn off once you have a real wave display
    public float hudTopOffset = 70f;        // how far down the text starts (bigger = lower, clear of the enemies-left message)

    public enum State { Waiting, Fighting, Break, Done }
    public State CurrentState { get; private set; } = State.Waiting;

    public bool IsBreak => CurrentState == State.Break;
    public float BreakTimeLeft => Mathf.Max(0f, timer);
    public int WaveNumber => waveIndex + 1;
    public int WaveCount => waves != null ? waves.Length : 0;

    readonly List<EnemySpawner> spawners = new List<EnemySpawner>();
    int waveIndex;
    float timer;
    GUIStyle labelStyle;

    void Awake() { Instance = this; }

    void OnDestroy()
    {
        if (Instance == this) Instance = null;
    }

    void Start()
    {
        spawners.AddRange(EnemySpawner.All);

        if (spawners.Count == 0 || waves == null || waves.Length == 0)
        {
            Debug.LogWarning("WaveManager needs at least one EnemySpawner in the scene and at least one wave. It is switching itself off.", this);
            enabled = false;
            return;
        }

        // From now on the spawners wait for us, and the level can't be won between waves.
        foreach (EnemySpawner s in spawners)
        {
            s.waveControlled = true;
            s.KeepOpen = true;
        }

        waveIndex = 0;
        CurrentState = State.Waiting;
        timer = firstWaveDelay;
    }

    void Update()
    {
        switch (CurrentState)
        {
            case State.Waiting:
                timer -= Time.deltaTime;
                if (timer <= 0f) StartWave();
                break;

            case State.Fighting:
                if (AllSpawnersDone()) EndWave();
                break;

            case State.Break:
                timer -= Time.deltaTime;

                // Enter skips the break in the editor (on the phone, use the on-screen button).
                var kb = Keyboard.current;
                if (kb != null && kb.enterKey.wasPressedThisFrame) SkipBreak();

                if (timer <= 0f)
                {
                    waveIndex++;
                    StartWave();
                }
                break;
        }
    }

    // Ends the current break right away. Also what a "skip" button should call.
    public void SkipBreak()
    {
        if (IsBreak) timer = 0f;
    }

    void StartWave()
    {
        Wave w = waves[waveIndex];

        foreach (EnemySpawner s in spawners)
        {
            if (s == null) continue;
            s.BeginWave(w.enemiesPerSpawner, w.interval, w.maxAlive, w.healthMultiplier, w.damageMultiplier, w.enemyTypes);
        }

        CurrentState = State.Fighting;
    }

    void EndWave()
    {
        // Last wave defeated: let the spawners report "finished", so the level is won.
        if (waveIndex >= waves.Length - 1)
        {
            foreach (EnemySpawner s in spawners)
                if (s != null) s.KeepOpen = false;

            CurrentState = State.Done;
            return;
        }

        timer = waves[waveIndex].breakSeconds;
        CurrentState = State.Break;
    }

    bool AllSpawnersDone()
    {
        foreach (EnemySpawner s in spawners)
            if (s != null && !s.WaveDone) return false;
        return true;
    }

    // ---- Temporary display ----------------------------------------------------

    void OnGUI()
    {
        if (!showHud || CurrentState == State.Done) return;

        float scale = Screen.height / 540f;
        GUI.matrix = Matrix4x4.Scale(new Vector3(scale, scale, 1f));
        float width = Screen.width / scale;

        if (labelStyle == null)
        {
            labelStyle = new GUIStyle(GUI.skin.label)
            {
                alignment = TextAnchor.UpperCenter,
                fontSize = 18,
                fontStyle = FontStyle.Bold
            };
            labelStyle.normal.textColor = Color.white;
        }

        string text;
        if (CurrentState == State.Waiting) text = "Get ready...";
        else if (CurrentState == State.Fighting) text = "Wave " + WaveNumber + " / " + WaveCount;
        else text = "Wave " + WaveNumber + " cleared. Next wave in " + Mathf.CeilToInt(BreakTimeLeft) + " s";

        GUI.Label(new Rect(0f, hudTopOffset, width, 30f), text, labelStyle);

        if (IsBreak && GUI.Button(new Rect(width * 0.5f - 70f, hudTopOffset + 32f, 140f, 36f), "Skip break"))
            SkipBreak();
    }
}
