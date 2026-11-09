using System.Collections.Generic;
using UnityEngine;

// A smarter enemy. It walks toward the base along a route that goes around walls (see NavGrid),
// keeps away from other enemies, and sidesteps and re-plans if it stops making progress.
// If the vehicle comes close it drops everything and goes after the vehicle instead,
// then returns to the base when the vehicle gets far away again.
// How it fights depends on its EnemyStats asset: Melee hits, Ranged shoots, Kamikaze explodes.
// Put this on the Enemy object (together with a Health component).
[RequireComponent(typeof(Rigidbody))]
public class EnemyController : MonoBehaviour
{
    static readonly List<EnemyController> All = new List<EnemyController>();

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    static void ResetStatics() { All.Clear(); }

    [Header("Stats")]
    public EnemyStats stats;                // drag the enemy's stats asset here (e.g. Warrior)
    [Tooltip("Wave / planet scaling. 1 = the asset's numbers")]
    public float healthMultiplier = 1f;
    public float damageMultiplier = 1f;

    [Header("Navigation (tune these)")]
    public float waypointReach = 0.5f;      // how close counts as "arrived" at a waypoint
    public float separationRadius = 1.2f;   // enemies closer than this push each other apart
    public float separationStrength = 0.8f;
    public float stuckTime = 0.8f;          // seconds without progress before it tries another way out
    public float baseRepathInterval = 1.5f; // how often it re-plans the route to the base
    public float chaseRepathInterval = 0.4f;// how often it re-plans while chasing the vehicle

    [Header("Knockback")]
    public float pushDeceleration = 30f;    // how fast a shove fades (units/sec²). Higher = shorter shove

    [Header("Effects and setup")]
    public float projectileHeight = 0.6f;   // height bullets fly at (Ranged enemies)
    public GameObject explosionEffect;      // optional, e.g. a sphere prefab (Kamikaze enemies)
    public bool scaleEffectToRadius = true; // scale the effect to the explosion size (right for a unit sphere)

    Rigidbody rb;
    Health health;
    EnemyStats.Resolved s;                  // this step's stats (multipliers applied)
    Vector3 pushVelocity;
    float yaw;
    float attackTimer;
    bool exploded;

    VehicleController vehicle;
    Health vehicleHealth;
    float nextVehicleSearch;
    bool chasing;
    Health currentTarget;

    readonly List<Vector3> path = new List<Vector3>();
    int pathIndex;
    float repathTimer;

    Vector3 lastProgressPos;
    float progressTimer;
    float escapeTimer;
    Vector3 escapeDir;

    readonly RaycastHit[] hitBuffer = new RaycastHit[16];

    Team MyTeam => health != null ? health.team : Team.Enemy;

    void Awake()
    {
        rb = GetComponent<Rigidbody>();
        health = GetComponent<Health>();

        // Set here so you don't have to configure the Rigidbody by hand.
        rb.useGravity = false;
        rb.constraints = RigidbodyConstraints.FreezePositionY
                       | RigidbodyConstraints.FreezeRotationX
                       | RigidbodyConstraints.FreezeRotationZ;
        rb.interpolation = RigidbodyInterpolation.Interpolate;
        rb.linearDamping = 0f;
        rb.angularDamping = 0f;

        if (stats == null)
            Debug.LogError("EnemyController has no EnemyStats assigned, so this enemy will not act.", this);
        RefreshStats();

        yaw = transform.eulerAngles.y;
        attackTimer = stats != null ? s.attackInterval : 1f;
        repathTimer = Random.value * baseRepathInterval;   // so enemies don't all plan on the same frame
        lastProgressPos = transform.position;
    }

    void OnEnable()
    {
        All.Add(this);
        if (health != null) health.Pushed += OnPushed;
    }

    void OnDisable()
    {
        All.Remove(this);
        if (health != null) health.Pushed -= OnPushed;
    }

    void OnPushed(Vector3 velocity) { pushVelocity = velocity * (1f - s.knockbackResistance); }

    // For the spawner: make this enemy stronger (e.g. later waves, boosted planets).
    public void SetScaling(float health, float damage)
    {
        healthMultiplier = health;
        damageMultiplier = damage;
        RefreshStats();
    }

    // Reads the asset (so Play Mode tuning is live) and gives Health its max health and armor.
    void RefreshStats()
    {
        if (stats == null) return;
        s = stats.Get(healthMultiplier, damageMultiplier);
        if (health != null) health.Configure(s.maxHealth, s.armorPercent);
    }

    void FixedUpdate()
    {
        if (stats == null) return;
        RefreshStats();

        float dt = Time.fixedDeltaTime;

        // The current shove (fades out over time). Added on top of normal movement below.
        Vector3 push = pushVelocity;
        pushVelocity = Vector3.MoveTowards(pushVelocity, Vector3.zero, pushDeceleration * dt);

        Health target = PickTarget();
        if (target == null)
        {
            // Nothing left to attack.
            rb.linearVelocity = Vector3.zero;
            rb.angularVelocity = Vector3.zero;
            return;
        }

        // New target: forget the old route.
        if (target != currentTarget)
        {
            currentTarget = target;
            path.Clear();
            pathIndex = 0;
            repathTimer = 0f;
        }

        bool targetIsVehicle = target == vehicleHealth;
        Vector3 pos = transform.position;
        Vector3 targetPos = target.transform.position;
        Vector3 to = targetPos - pos;
        to.y = 0f;
        float dist = to.magnitude;
        float stopDistance = target.radius + s.attackRange;

        // Shooters need a clear line even to the base; everyone needs one to reach the vehicle.
        bool needsLineOfSight = targetIsVehicle || s.attackType == EnemyStats.AttackType.Ranged;

        // In range, and (when needed) no wall between us: attack.
        if (dist <= stopDistance && (!needsLineOfSight || !StaticBlocked(pos, targetPos, target)))
        {
            rb.linearVelocity = push;
            Face(to, dt);

            if (s.attackType == EnemyStats.AttackType.Kamikaze)
            {
                Explode();
                return;
            }

            attackTimer -= dt;
            if (attackTimer <= 0f)
            {
                attackTimer = s.attackInterval;
                if (s.attackType == EnemyStats.AttackType.Ranged) Shoot(to, dist);
                else target.TakeDamage(s.damage);
            }

            progressTimer = 0f;
            lastProgressPos = pos;
            return;
        }

        // Plan a route now and then.
        repathTimer -= dt;
        if (repathTimer <= 0f)
        {
            repathTimer = targetIsVehicle ? chaseRepathInterval : baseRepathInterval;
            if (!NavGrid.Instance.FindPath(pos, targetPos, stopDistance * 0.85f, targetIsVehicle, path))
                path.Clear();
            pathIndex = 0;
        }

        Vector3 desired = FollowPath(pos, to);
        Vector3 move = desired;

        if (escapeTimer > 0f)
        {
            // Sidestepping out of a jam.
            escapeTimer -= dt;
            move = (escapeDir + desired * 0.2f).normalized;
        }
        else
        {
            move += Separation(pos) * separationStrength;
        }

        if (move.sqrMagnitude > 0.0001f) move.Normalize();

        rb.linearVelocity = move * s.speed + push;
        Face(move, dt);
        CheckStuck(pos, desired, dt);
    }

    // ---- Attacks ------------------------------------------------------------------

    // Ranged: fires one projectile toward the target, with a random spread.
    void Shoot(Vector3 toTarget, float distance)
    {
        if (stats.projectilePrefab == null)
        {
            Debug.LogWarning("Ranged enemy has no Projectile Prefab in its EnemyStats.", this);
            return;
        }
        if (toTarget.sqrMagnitude < 0.0001f) return;

        float half = s.projectileSpread * 0.5f;
        Vector3 dir = Quaternion.Euler(0f, Random.Range(-half, half), 0f) * toTarget.normalized;

        float bodyRadius = health != null ? health.radius : 0.5f;
        Vector3 center = transform.position;
        center.y = projectileHeight;
        Vector3 spawn = center + dir * (bodyRadius + 0.2f);

        Projectile p = Instantiate(stats.projectilePrefab, spawn, Quaternion.LookRotation(dir));
        p.Launch(dir, s.projectileSpeed, s.damage, distance + 4f, MyTeam, rb);
    }

    // Kamikaze: damages everything of the other team inside the blast, then dies.
    void Explode()
    {
        if (exploded) return;
        exploded = true;

        Vector3 center = transform.position;

        for (int i = Health.All.Count - 1; i >= 0; i--)
        {
            Health h = Health.All[i];
            if (h == health || h.team == MyTeam || !h.IsAlive) continue;

            Vector3 d = h.transform.position - center;
            d.y = 0f;
            if (d.magnitude - h.radius <= s.explosionRadius)
                h.TakeDamage(s.damage);
        }

        if (explosionEffect != null)
        {
            GameObject fx = Instantiate(explosionEffect, center, Quaternion.identity);
            if (scaleEffectToRadius) fx.transform.localScale = Vector3.one * s.explosionRadius * 2f;
            Destroy(fx, 0.5f);
        }

        if (health != null) health.Kill();
        else Destroy(gameObject);
    }

    // ---- Choosing who to go after ------------------------------------------------

    Health PickTarget()
    {
        PlayerBase b = PlayerBase.Instance;
        Health baseHealth = (b != null && b.Health.IsAlive) ? b.Health : null;

        // Find the vehicle (a few times a second at most, until we have it).
        if (vehicle == null && Time.time >= nextVehicleSearch)
        {
            nextVehicleSearch = Time.time + 1f;
            vehicle = FindFirstObjectByType<VehicleController>();
            vehicleHealth = vehicle != null ? vehicle.GetComponent<Health>() : null;
        }

        if (vehicleHealth != null && vehicleHealth.IsAlive)
        {
            Vector3 d = vehicleHealth.transform.position - transform.position;
            d.y = 0f;
            float dist = d.magnitude;

            // Two distances (start / give up) so it doesn't flicker between targets at the edge.
            if (!chasing && dist <= s.chaseRange) chasing = true;
            else if (chasing && dist > s.loseRange) chasing = false;
        }
        else
        {
            chasing = false;
        }

        return chasing ? vehicleHealth : baseHealth;
    }

    // ---- Moving --------------------------------------------------------------------

    Vector3 FollowPath(Vector3 pos, Vector3 toTarget)
    {
        while (pathIndex < path.Count)
        {
            Vector3 w = path[pathIndex] - pos;
            w.y = 0f;
            if (w.magnitude < waypointReach) { pathIndex++; continue; }
            return w.normalized;
        }

        // No route (or it is finished): walk straight at the target.
        return toTarget.sqrMagnitude > 0.0001f ? toTarget.normalized : Vector3.zero;
    }

    // Pushes away from other enemies that are too close, so they spread out instead of piling up.
    Vector3 Separation(Vector3 pos)
    {
        Vector3 push = Vector3.zero;
        for (int i = 0; i < All.Count; i++)
        {
            EnemyController e = All[i];
            if (e == this) continue;

            Vector3 d = pos - e.transform.position;
            d.y = 0f;
            float m = d.magnitude;
            if (m > 0.001f && m < separationRadius)
                push += d / m * (1f - m / separationRadius);
        }
        return push;
    }

    // If it hasn't gone anywhere for a while, sidestep one way for a moment and plan a new route.
    void CheckStuck(Vector3 pos, Vector3 desired, float dt)
    {
        progressTimer += dt;
        if (progressTimer < stuckTime) return;

        float moved = Vector3.Distance(pos, lastProgressPos);
        progressTimer = 0f;
        lastProgressPos = pos;

        if (moved < s.speed * stuckTime * 0.25f)
        {
            Vector3 side = new Vector3(desired.z, 0f, -desired.x) * (Random.value < 0.5f ? 1f : -1f);
            escapeDir = (side - desired * 0.3f).normalized;
            escapeTimer = 0.6f;
            repathTimer = 0f;
        }
    }

    void Face(Vector3 dir, float dt)
    {
        if (dir.sqrMagnitude > 0.0001f)
        {
            float targetYaw = Mathf.Atan2(dir.x, dir.z) * Mathf.Rad2Deg;
            yaw = Mathf.MoveTowardsAngle(yaw, targetYaw, s.turnSpeed * dt);
        }
        rb.MoveRotation(Quaternion.Euler(0f, yaw, 0f));
        rb.angularVelocity = Vector3.zero;
    }

    // True if a fixed wall or obstacle is between two points (moving things don't count).
    // The target itself never counts as a wall (the base has no Rigidbody, so it would otherwise block its own attackers).
    bool StaticBlocked(Vector3 a, Vector3 b, Health ignore)
    {
        a.y = 0.6f;
        b.y = 0.6f;
        Vector3 d = b - a;
        float len = d.magnitude;
        if (len < 0.01f) return false;

        int n = Physics.RaycastNonAlloc(a, d / len, hitBuffer, len, ~0, QueryTriggerInteraction.Ignore);
        for (int i = 0; i < n; i++)
        {
            Collider c = hitBuffer[i].collider;
            if (c.attachedRigidbody != null) continue;
            if (ignore != null && c.GetComponentInParent<Health>() == ignore) continue;
            return true;
        }
        return false;
    }
}
