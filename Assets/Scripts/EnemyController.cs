using System.Collections.Generic;
using UnityEngine;

// A smarter enemy. It walks toward the base along a route that goes around walls (see NavGrid),
// keeps away from other enemies, and sidesteps and re-plans if it stops making progress.
// If the vehicle comes close it drops everything and goes after the vehicle instead,
// then returns to the base when the vehicle gets far away again.
// Put this on the Enemy object (together with a Health component).
[RequireComponent(typeof(Rigidbody))]
public class EnemyController : MonoBehaviour
{
    static readonly List<EnemyController> All = new List<EnemyController>();

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    static void ResetStatics() { All.Clear(); }

    [Header("Movement (tune these)")]
    public float speed = 2.5f;              // units per second
    public float turnSpeed = 360f;          // degrees per second (only affects how fast it faces where it walks)
    public float waypointReach = 0.5f;      // how close counts as "arrived" at a waypoint

    [Header("Avoiding walls and each other")]
    public float separationRadius = 1.2f;   // enemies closer than this push each other apart
    public float separationStrength = 0.8f;
    public float stuckTime = 0.8f;          // seconds without progress before it tries another way out
    public float baseRepathInterval = 1.5f; // how often it re-plans the route to the base
    public float chaseRepathInterval = 0.4f;// how often it re-plans while chasing the vehicle

    [Header("Choosing a target")]
    public float chaseRange = 8f;           // the vehicle this close makes it go after the vehicle
    public float loseRange = 12f;           // ...and it gives up beyond this distance

    [Header("Attack (tune these)")]
    public float attackRange = 1.2f;        // how close to the target's edge it gets before attacking
    public float attackDamage = 10f;
    public float attackInterval = 1f;       // seconds between hits

    Rigidbody rb;
    float yaw;
    float attackTimer;

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

    void Awake()
    {
        rb = GetComponent<Rigidbody>();

        // Set here so you don't have to configure the Rigidbody by hand.
        rb.useGravity = false;
        rb.constraints = RigidbodyConstraints.FreezePositionY
                       | RigidbodyConstraints.FreezeRotationX
                       | RigidbodyConstraints.FreezeRotationZ;
        rb.interpolation = RigidbodyInterpolation.Interpolate;
        rb.linearDamping = 0f;
        rb.angularDamping = 0f;

        yaw = transform.eulerAngles.y;
        attackTimer = attackInterval;
        repathTimer = Random.value * baseRepathInterval;   // so enemies don't all plan on the same frame
        lastProgressPos = transform.position;
    }

    void OnEnable() { All.Add(this); }
    void OnDisable() { All.Remove(this); }

    void FixedUpdate()
    {
        float dt = Time.fixedDeltaTime;

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
        float stopDistance = target.radius + attackRange;

        // In range, and (for the vehicle) no wall between us: stand and fight.
        if (dist <= stopDistance && (!targetIsVehicle || !StaticBlocked(pos, targetPos)))
        {
            rb.linearVelocity = Vector3.zero;
            Face(to, dt);

            attackTimer -= dt;
            if (attackTimer <= 0f)
            {
                attackTimer = attackInterval;
                target.TakeDamage(attackDamage);
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

        rb.linearVelocity = move * speed;
        Face(move, dt);
        CheckStuck(pos, desired, dt);
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
            if (!chasing && dist <= chaseRange) chasing = true;
            else if (chasing && dist > loseRange) chasing = false;
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

        if (moved < speed * stuckTime * 0.25f)
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
            yaw = Mathf.MoveTowardsAngle(yaw, targetYaw, turnSpeed * dt);
        }
        rb.MoveRotation(Quaternion.Euler(0f, yaw, 0f));
        rb.angularVelocity = Vector3.zero;
    }

    // True if a fixed wall or obstacle is between two points (moving things don't count).
    bool StaticBlocked(Vector3 a, Vector3 b)
    {
        a.y = 0.6f;
        b.y = 0.6f;
        Vector3 d = b - a;
        float len = d.magnitude;
        if (len < 0.01f) return false;

        int n = Physics.RaycastNonAlloc(a, d / len, hitBuffer, len, ~0, QueryTriggerInteraction.Ignore);
        for (int i = 0; i < n; i++)
            if (hitBuffer[i].collider.attachedRigidbody == null) return true;
        return false;
    }
}
