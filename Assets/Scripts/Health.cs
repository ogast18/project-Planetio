using System;
using System.Collections.Generic;
using UnityEngine;

public enum Team { Player, Enemy }

// Hit points for anything that can be shot: enemies, the base, and the vehicle.
// Every Health registers itself in Health.All so turrets can find targets without layers or physics queries.
// It also gets a health bar (red for enemies, green for allies) and shows floating damage numbers.
public class Health : MonoBehaviour
{
    // All living things with Health right now.
    public static readonly List<Health> All = new List<Health>();

    [Header("Stats (tune these)")]
    public Team team = Team.Enemy;
    public float maxHealth = 100f;
    [Range(0f, 90f)] public float armorPercent = 0f;  // percentage of damage that is ignored
    public float radius = 0.6f;                       // rough size, used by turrets to judge "is it in my cone"

    [Header("Display")]
    public bool showBar = true;
    public float barOffset = 55f;      // how far above the object the bar floats (screen units, bigger objects need more)
    public float barWidth = 90f;       // bar width (screen units)
    public bool showDamageNumbers = true;

    [Header("Feedback")]
    public bool hitPunch = true;       // small scale pulse when hit (turn off for the vehicle and for enemies hit by fast-firing turrets)
    public bool destroyOnDeath = true;

    public float Current { get; private set; }
    public bool IsAlive => Current > 0f;
    public event Action<Health> Died;
    public event Action<Vector3> Pushed;   // fires with the shove velocity when a hit has knockback

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

    void Start()
    {
        if (showBar) WorldUI.Instance.AddBar(this);
    }

    // Called by a stats asset (hull, later enemies) to set max health and armor.
    // Keeps the same health percentage, so leveling up mid-fight doesn't heal or hurt.
    public void Configure(float newMaxHealth, float newArmorPercent)
    {
        armorPercent = Mathf.Clamp(newArmorPercent, 0f, 90f);
        if (Mathf.Approximately(newMaxHealth, maxHealth)) return;

        float ratio = maxHealth > 0f ? Current / maxHealth : 1f;
        maxHealth = newMaxHealth;
        Current = Mathf.Min(maxHealth, ratio * maxHealth);
    }

    // hitDirection and knockback are optional: TakeDamage(10) still works, with no push.
    public void TakeDamage(float amount, Vector3 hitDirection = default, float knockback = 0f)
    {
        if (!IsAlive) return;

        float applied = amount * (1f - armorPercent / 100f);
        Current -= applied;
        punch = 1f;

        if (showDamageNumbers) WorldUI.Instance.ShowDamage(this, applied);

        if (Current <= 0f)
        {
            Current = 0f;
            Died?.Invoke(this);
            if (destroyOnDeath) Destroy(gameObject);
        }
        else if (knockback > 0f)
        {
            hitDirection.y = 0f;
            if (hitDirection.sqrMagnitude > 0.0001f)
                Pushed?.Invoke(hitDirection.normalized * knockback);
        }
    }

    void Update()
    {
        if (!hitPunch || punch <= 0f) return;

        punch = Mathf.MoveTowards(punch, 0f, Time.deltaTime * 6f);
        transform.localScale = baseScale * (1f + 0.2f * punch);
    }
}
