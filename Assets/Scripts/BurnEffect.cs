using UnityEngine;

/// <summary>
/// Added to an enemy by the flamethrower. Keeps damaging it for a few seconds after the flames stop.
/// Hitting it again refreshes the burn (it does not stack). Removes itself when it runs out or the enemy dies.
/// </summary>
public class BurnEffect : MonoBehaviour
{
    const float TickInterval = 0.5f;

    Health health;
    float damagePerSecond;
    float timeLeft;
    float tickTimer;

    /// <summary>Sets the target burning, or refreshes the burn it already has.</summary>
    public static void Apply(Health target, float damagePerSecond, float duration)
    {
        if (target == null || !target.IsAlive) return;

        BurnEffect burn = target.GetComponent<BurnEffect>();
        if (burn == null) burn = target.gameObject.AddComponent<BurnEffect>();
        burn.Refresh(target, damagePerSecond, duration);
    }

    void Refresh(Health target, float dps, float duration)
    {
        health = target;
        damagePerSecond = Mathf.Max(damagePerSecond, dps);   // the strongest flame wins
        timeLeft = Mathf.Max(timeLeft, duration);
    }

    void Update()
    {
        if (health == null || !health.IsAlive) { Destroy(this); return; }

        timeLeft -= Time.deltaTime;
        tickTimer += Time.deltaTime;
        if (tickTimer >= TickInterval)
        {
            tickTimer -= TickInterval;
            health.TakeDamage(damagePerSecond * TickInterval, Vector3.forward, 0f, 0f);
        }

        if (timeLeft <= 0f) Destroy(this);
    }

    // If enemies are pooled and switched off, don't carry the burn over to their next life.
    void OnDisable() { Destroy(this); }
}
