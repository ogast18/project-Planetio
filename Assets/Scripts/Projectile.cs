using UnityEngine;

// A simple bullet. It moves itself and checks what is in front of it every frame,
// so it needs no Rigidbody and no collider of its own.
public class Projectile : MonoBehaviour
{
    public float radius = 0.15f;   // how fat the bullet is for hit detection

    static readonly Collider[] overlapBuffer = new Collider[16];

    Vector3 direction;
    float speed;
    float damage;
    float knockback;               // shove speed given to whatever it hits (0 = none)
    float armorPenetration;        // fraction of the target's armor this bullet ignores (0 to 1)
    AnimationCurve damageFalloff;  // damage multiplier along the flight (null = full damage all the way)
    float totalRange;              // how far it can fly, to turn distance travelled into 0..1 for the curve
    System.Action<float, Vector3> onDamageDealt;   // told (damage, where it was fired from) when it hurts something
    Vector3 launchPosition;
    float remainingDistance;
    Team ownerTeam;
    Rigidbody ignoreBody;          // the shooter's own body, never hit by its own bullets
    bool startChecked;
    TrailRenderer[] trails;

    // Set by the ProjectilePool, which owns the bullet's life.
    [System.NonSerialized] public Projectile PrefabKey;   // the prefab this bullet was made from
    [System.NonSerialized] public bool InPool;            // true while it is waiting unused

    void Awake()
    {
        trails = GetComponentsInChildren<TrailRenderer>(true);
    }

    // The pool calls this every time the bullet is handed out, so a reused bullet starts clean.
    public void OnSpawned()
    {
        InPool = false;
        startChecked = false;
        for (int i = 0; i < trails.Length; i++) trails[i].Clear();   // no streak from where it was last
    }

    // The pool calls this when the bullet goes back, so it doesn't hold on to the shooter.
    public void OnDespawned()
    {
        InPool = true;
        onDamageDealt = null;
        ignoreBody = null;
    }

    // Instead of Destroy: gives the bullet back to the pool.
    void Despawn() { ProjectilePool.Release(this); }

    // The damage this bullet would deal right now: full damage, scaled by the falloff curve for how far it has flown.
    float DamageNow()
    {
        if (damageFalloff == null || damageFalloff.length == 0 || totalRange <= 0f) return damage;

        float flown = Mathf.Clamp01((totalRange - remainingDistance) / totalRange);
        return damage * Mathf.Max(0f, damageFalloff.Evaluate(flown));
    }

    public void Launch(Vector3 dir, float speed, float damage, float range, Team ownerTeam, Rigidbody ignoreBody = null, float knockback = 0f,
                       System.Action<float, Vector3> onDamageDealt = null, float armorPenetration = 0f,
                       AnimationCurve damageFalloff = null)
    {
        this.onDamageDealt = onDamageDealt;
        this.armorPenetration = armorPenetration;
        this.damageFalloff = damageFalloff;
        launchPosition = transform.position;
        direction = dir.normalized;
        this.speed = speed;
        this.damage = damage;
        this.remainingDistance = range;
        totalRange = range;
        this.ownerTeam = ownerTeam;
        this.ignoreBody = ignoreBody;
        this.knockback = knockback;
    }

    void Update()
    {
        // Sweeps ignore anything the bullet already starts inside of. That let bullets fly straight
        // through an enemy or wall they were fired from point blank, so check the start position first.
        if (!startChecked)
        {
            startChecked = true;
            if (HitSomethingAtStart()) return;
        }

        float step = speed * Time.deltaTime;

        if (Physics.SphereCast(transform.position, radius, direction, out RaycastHit hit, step,
                               ~0, QueryTriggerInteraction.Ignore))
        {
            if (ignoreBody == null || hit.collider.attachedRigidbody != ignoreBody)
            {
                Health health = hit.collider.GetComponentInParent<Health>();

                // Same team: fly through. Anything else (enemy, wall, obstacle) stops the bullet.
                if (health == null || health.team != ownerTeam)
                {
                    if (health != null)
                    {
                        float dealt = DamageNow();
                        health.TakeDamage(dealt, direction, knockback, armorPenetration);
                        onDamageDealt?.Invoke(dealt, launchPosition);
                    }
                    Despawn();
                    return;
                }
            }
        }

        transform.position += direction * step;
        remainingDistance -= step;
        if (remainingDistance <= 0f) Despawn();
    }

    // True if the bullet was already touching something when it appeared (and was used up by it).
    bool HitSomethingAtStart()
    {
        int n = Physics.OverlapSphereNonAlloc(transform.position, radius, overlapBuffer, ~0,
                                              QueryTriggerInteraction.Ignore);
        Health target = null;
        bool blocked = false;

        for (int i = 0; i < n; i++)
        {
            Collider c = overlapBuffer[i];
            if (ignoreBody != null && c.attachedRigidbody == ignoreBody) continue;

            Health h = c.GetComponentInParent<Health>();
            if (h != null && h.team == ownerTeam) continue;     // friendly: ignore

            if (h != null) target = h;     // an enemy wins over a wall it is standing against
            else blocked = true;
        }

        if (target != null)
        {
            float dealt = DamageNow();
            target.TakeDamage(dealt, direction, knockback, armorPenetration);
            onDamageDealt?.Invoke(dealt, launchPosition);
        }
        if (target != null || blocked)
        {
            Despawn();
            return true;
        }
        return false;
    }
}
