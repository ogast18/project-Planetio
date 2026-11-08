using System;
using System.Collections.Generic;
using UnityEngine;

public enum Team { Player, Enemy }

// Hit points for anything that can be shot: enemies, the base, and later the vehicle.
// Every Health registers itself in Health.All so turrets can find targets without layers or physics queries.
public class Health : MonoBehaviour
{
    // All living things with Health right now.
    public static readonly List<Health> All = new List<Health>();

    [Header("Stats (tune these)")]
    public Team team = Team.Enemy;
    public float maxHealth = 100f;
    [Range(0f, 90f)] public float armorPercent = 0f;  // percentage of damage that is ignored
    public float radius = 0.6f;                       // rough size, used by turrets to judge "is it in my cone"

    [Header("Feedback")]
    public bool hitPunch = true;                      // small scale pulse when hit (turn off for the vehicle later)
    public bool destroyOnDeath = true;

    public float Current { get; private set; }
    public bool IsAlive => Current > 0f;
    public event Action<Health> Died;

    Vector3 baseScale;
    float punch;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    static void ResetStatics() { All.Clear(); }

    void Awake()
    {
        Current = maxHealth;
        baseScale = transform.localScale;
    }

    void OnEnable() { All.Add(this); }
    void OnDisable() { All.Remove(this); }

    public void TakeDamage(float amount)
    {
        if (!IsAlive) return;

        Current -= amount * (1f - armorPercent / 100f);
        punch = 1f;

        if (Current <= 0f)
        {
            Current = 0f;
            Died?.Invoke(this);
            if (destroyOnDeath) Destroy(gameObject);
        }
    }

    void Update()
    {
        if (!hitPunch || punch <= 0f) return;

        punch = Mathf.MoveTowards(punch, 0f, Time.deltaTime * 6f);
        transform.localScale = baseScale * (1f + 0.2f * punch);
    }
}
