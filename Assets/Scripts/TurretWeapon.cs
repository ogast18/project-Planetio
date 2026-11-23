using System.Collections.Generic;
using UnityEngine;

// The cannon: fires automatically whenever an enemy is inside the firing cone and not hidden behind a wall.
// The cone is a pie slice in front of the turret (width = cone angle, length = range) that is cut off by walls.
// Every bullet flies at a random angle inside the cone, and the cone gets narrower
// (so aim gets better) while the vehicle stands still.
// Holding the fire button fires even when there is no target (for shooting walls).
// Put this on the same Turret object as TurretController. All numbers come from its TurretStats.
[RequireComponent(typeof(TurretController))]
public class TurretWeapon : MonoBehaviour
{
    [Header("References")]
    public Transform muzzle;               // drag the Muzzle object here
    public Projectile projectilePrefab;    // fallback bullet: only used if the turret's stats have none
    public Material coneMaterial;          // transparent material for the cone display (optional)
    public FireButton fireButton;          // optional: found automatically if left empty

    [Header("Setup")]
    public Team team = Team.Player;
    [Min(0), Tooltip("Bullets prepared in advance so the first shots don't have to create any. A fast turret needs more")]
    public int prewarm = 20;

    [Header("Walls")]
    public float coneSegmentAngle = 3f;    // degrees per cone slice: smaller = smoother cut at walls, more raycasts

    [Header("Cone display")]
    public bool showCone = true;
    public float groundHeight = 0.05f;     // height of the cone above the ground

    // The cone width right now (between the moving and the settled width).
    public float CurrentConeAngle { get; private set; }

    // Raised every time a Projectile turret fires a shot (for recoil, muzzle flash, sound).
    public event System.Action Fired;

    // Beam and Flame turrets are "firing" for as long as they are damaging things.
    public event System.Action FiringStarted;
    public event System.Action FiringStopped;
    public bool IsFiring { get; private set; }

    // Beam only: where the laser ends right now (nearest wall or first target), at muzzle height.
    public Vector3 BeamEnd { get; private set; }

    // The range right now, for visuals that want to fit themselves to it.
    public float Range => s.range;

    // The bullet to fire: the one in the turret's stats, or this turret's own slot if the stats have none.
    Projectile BulletPrefab =>
        (controller != null && controller.stats != null && controller.stats.projectilePrefab != null)
            ? controller.stats.projectilePrefab
            : projectilePrefab;

    TurretController controller;
    TurretStats.Resolved s;                // this frame's stats (level applied)
    float cooldown;
    float sharpness;                       // 0 = moving (full cone), 1 = fully settled
    Rigidbody body;
    GameObject coneObject;
    Mesh coneMesh;
    Vector3[] coneVerts;
    int[] coneTris;
    int builtSegments = -1;
    readonly RaycastHit[] hitBuffer = new RaycastHit[16];

    // Beam / flame state
    float tickTimer;
    readonly Dictionary<Health, float> ramp = new Dictionary<Health, float>();   // flame: seconds each target has been in the flames
    readonly List<Health> inFlames = new List<Health>();
    readonly List<Health> rampKeys = new List<Health>();

    void Start()
    {
        controller = GetComponent<TurretController>();
        body = GetComponentInParent<Rigidbody>();   // the vehicle's Rigidbody
        if (fireButton == null) fireButton = FindFirstObjectByType<FireButton>();

        if (controller.stats == null)
        {
            Debug.LogError("TurretWeapon: TurretController has no TurretStats assigned.", this);
            enabled = false;
            return;
        }

        s = controller.Current;
        CurrentConeAngle = s.coneAngle;
        ProjectilePool.Prewarm(BulletPrefab, prewarm);
        RefreshCone();
    }

    void Update()
    {
        s = controller.Current;            // read once per frame so Play Mode tuning is live
        UpdateConeAngle();

        bool manual = fireButton != null && fireButton.IsHeld;
        switch (s.fireMode)
        {
            case FireMode.Beam:  UpdateBeam(manual);  break;
            case FireMode.Flame: UpdateFlame(manual); break;
            default:             UpdateProjectile(manual); break;
        }

        RefreshCone();
    }

    void SetFiring(bool firing)
    {
        if (firing == IsFiring) return;
        IsFiring = firing;
        if (firing) FiringStarted?.Invoke();
        else FiringStopped?.Invoke();
    }

    void UpdateConeAngle()
    {
        bool still = body != null && body.linearVelocity.magnitude < s.stationarySpeed;

        float dt = Time.deltaTime;
        if (still) sharpness = Mathf.MoveTowards(sharpness, 1f, dt / Mathf.Max(0.01f, s.settleTime));
        else sharpness = Mathf.MoveTowards(sharpness, 0f, dt / Mathf.Max(0.01f, s.loosenTime));

        CurrentConeAngle = Mathf.Lerp(s.coneAngle, s.coneAngle * s.stationaryConeMultiplier, sharpness);
    }

    // ---- Walls -------------------------------------------------------------

    // Bullets fly at the height of the muzzle, so walls are checked at that height.
    Vector3 AimOrigin()
    {
        Vector3 o = transform.position;
        if (muzzle != null) o.y = muzzle.position.y;
        return o;
    }

    Vector3 FlatForward()
    {
        Vector3 f = transform.forward;
        f.y = 0f;
        return f.normalized;
    }

    // Distance to the nearest wall along a direction. A wall is anything solid that is not
    // a Health object (enemies and allies don't cut the cone) and not part of our own vehicle.
    float WallDistance(Vector3 origin, Vector3 dir, float maxDistance)
    {
        int n = Physics.RaycastNonAlloc(origin, dir, hitBuffer, maxDistance, ~0, QueryTriggerInteraction.Ignore);
        float best = maxDistance;

        for (int i = 0; i < n; i++)
        {
            if (hitBuffer[i].distance >= best) continue;

            Collider c = hitBuffer[i].collider;
            if (body != null && c.attachedRigidbody == body) continue;
            if (c.GetComponentInParent<Health>() != null) continue;

            best = hitBuffer[i].distance;
        }
        return best;
    }

    // How far the cone reaches in the direction yawOffset degrees away from straight ahead.
    float ClipDistance(Vector3 origin, Vector3 flatForward, float yawOffset)
    {
        Vector3 dir = Quaternion.Euler(0f, yawOffset, 0f) * flatForward;
        return WallDistance(origin, dir, s.range);
    }

    // True if anything a bullet could hit (wall, obstacle, enemy) is within a short distance ahead.
    // Our own vehicle and friendly things don't count.
    bool SolidAhead(Vector3 origin, Vector3 dir, float maxDistance)
    {
        int n = Physics.RaycastNonAlloc(origin, dir, hitBuffer, maxDistance, ~0, QueryTriggerInteraction.Ignore);

        for (int i = 0; i < n; i++)
        {
            Collider c = hitBuffer[i].collider;
            if (body != null && c.attachedRigidbody == body) continue;

            Health h = c.GetComponentInParent<Health>();
            if (h != null && h.team == team) continue;

            return true;
        }
        return false;
    }

    // ---- Targeting and firing ----------------------------------------------

    // True if this enemy touches the firing cone and no wall is in the way.
    bool EnemyInCone(Health h, Vector3 origin, Vector3 forward, float half)
    {
        if (h.team == team || !h.IsAlive) return false;

        Vector3 to = h.transform.position - transform.position;
        to.y = 0f;
        float dist = to.magnitude;
        if (dist - h.radius > s.range) return false;

        // Counts as inside if any part of the target (its radius) touches the cone.
        float signed = Vector3.SignedAngle(forward, to, Vector3.up);
        float margin = dist > 0.01f ? Mathf.Asin(Mathf.Clamp01(h.radius / dist)) * Mathf.Rad2Deg : 180f;
        if (Mathf.Abs(signed) - margin > half) return false;

        // A wall in the way means the shot could never hit, so don't fire.
        float offset = Mathf.Clamp(signed, -half, half);
        if (ClipDistance(origin, forward, offset) < dist - h.radius) return false;

        return true;
    }

    bool HasTargetInCone()
    {
        Vector3 origin = AimOrigin();
        Vector3 forward = FlatForward();
        float half = CurrentConeAngle * 0.5f;

        foreach (Health h in Health.All)
            if (EnemyInCone(h, origin, forward, half)) return true;
        return false;
    }

    // ---- Projectile turrets (cannon, minigun, shotgun) ---------------------

    void UpdateProjectile(bool manual)
    {
        SetFiring(false);
        cooldown = Mathf.Max(0f, cooldown - Time.deltaTime);

        if (cooldown <= 0f && (manual || HasTargetInCone()))
        {
            Fire();
            cooldown = 1f / Mathf.Max(0.01f, s.fireRate);
        }
    }

    // ---- Beam turret (laser) ------------------------------------------------

    // A thin straight line along the turret's aim. It damages the first enemy it touches (walls stop it),
    // a little every tick. The cone is only used to decide when to start; make it very narrow in the stats.
    void UpdateBeam(bool manual)
    {
        Vector3 origin = AimOrigin();
        Vector3 dir = FlatForward();

        bool want = manual || HasTargetInCone();
        SetFiring(want);
        if (!want) { tickTimer = 0f; return; }

        // Where the beam stops: the nearest wall, or the first enemy in front of it.
        float stopAt = WallDistance(origin, dir, s.range);
        Health target = null;
        Vector3 sideDir = new Vector3(dir.z, 0f, -dir.x);

        foreach (Health h in Health.All)
        {
            if (h.team == team || !h.IsAlive) continue;

            Vector3 to = h.transform.position - origin;
            to.y = 0f;
            float along = Vector3.Dot(to, dir);
            if (along < -h.radius || along - h.radius > stopAt) continue;

            float side = Mathf.Abs(Vector3.Dot(to, sideDir));
            if (side > h.radius + s.beamWidth * 0.5f) continue;

            float hitDist = Mathf.Max(0f, along - h.radius);
            if (hitDist < stopAt) { stopAt = hitDist; target = h; }
        }

        BeamEnd = origin + dir * stopAt;

        if (target == null) { tickTimer = 0f; return; }

        float interval = 1f / Mathf.Max(0.1f, s.ticksPerSecond);
        tickTimer += Time.deltaTime;
        while (tickTimer >= interval)
        {
            tickTimer -= interval;
            target.TakeDamage(s.damage * interval, dir, 0f, s.armorPenetration);
        }
    }

    // ---- Flame turret ---------------------------------------------------------

    // Hurts every enemy inside the cone (walls cut it) a little every tick. The longer the same enemy
    // stays in the flames, the more damage it takes. Everything hit is set burning.
    void UpdateFlame(bool manual)
    {
        Vector3 origin = AimOrigin();
        Vector3 forward = FlatForward();
        float half = CurrentConeAngle * 0.5f;
        float dt = Time.deltaTime;

        bool want = manual || HasTargetInCone();
        SetFiring(want);

        // Who is in the flames this frame.
        inFlames.Clear();
        if (want)
        {
            foreach (Health h in Health.All)
                if (EnemyInCone(h, origin, forward, half)) inFlames.Add(h);
        }

        UpdateRamp(dt);

        if (!want) { tickTimer = 0f; return; }

        float interval = 1f / Mathf.Max(0.1f, s.ticksPerSecond);
        tickTimer += dt;
        while (tickTimer >= interval)
        {
            tickTimer -= interval;
            ApplyFlameTick(interval, forward);
        }
    }

    // Targets in the flames build up their bonus, the others lose it.
    void UpdateRamp(float dt)
    {
        foreach (Health h in inFlames)
        {
            ramp.TryGetValue(h, out float t);
            ramp[h] = Mathf.Min(s.rampTime, t + dt);
        }

        rampKeys.Clear();
        rampKeys.AddRange(ramp.Keys);
        float fade = dt * s.rampTime / Mathf.Max(0.01f, s.rampDecayTime);
        foreach (Health h in rampKeys)
        {
            if (h == null || !h.IsAlive) { ramp.Remove(h); continue; }
            if (inFlames.Contains(h)) continue;

            float t = ramp[h] - fade;
            if (t <= 0f) ramp.Remove(h);
            else ramp[h] = t;
        }
    }

    void ApplyFlameTick(float interval, Vector3 forward)
    {
        foreach (Health h in inFlames)
        {
            ramp.TryGetValue(h, out float t);
            float rampMultiplier = Mathf.Lerp(1f, s.rampMaxMultiplier, Mathf.Clamp01(t / Mathf.Max(0.01f, s.rampTime)));

            h.TakeDamage(s.damage * interval * rampMultiplier * FlameArmorMultiplier(h), forward, 0f, s.armorPenetration);

            if (s.burnDamageFraction > 0f && s.burnDuration > 0f)
                BurnEffect.Apply(h, s.damage * s.burnDamageFraction, s.burnDuration);
        }
    }

    // Reserved for later: low-armor enemies should burn more easily (take more flame damage) and
    // high-armor enemies less. For now every target takes the normal amount.
    float FlameArmorMultiplier(Health target) { return 1f; }

    void Fire()
    {
        if (BulletPrefab == null || muzzle == null) return;

        // One bullet for most turrets, several pellets for a shotgun. Each flies at its own random angle.
        int pellets = Mathf.Max(1, s.pelletsPerShot);
        for (int i = 0; i < pellets; i++) FireOne();
        Fired?.Invoke();
    }

    void FireOne()
    {
        // Random direction inside the cone.
        float half = CurrentConeAngle * 0.5f * s.spreadAmount;
        Vector3 dir = Quaternion.Euler(0f, Random.Range(-half, half), 0f) * FlatForward();

        // If a wall or an enemy sits between the turret centre and the muzzle (pressed up against it),
        // start the bullet at the centre so it hits that thing instead of starting beyond or inside it.
        Vector3 origin = AimOrigin();
        Vector3 spawn = muzzle.position;
        float toMuzzle = Vector3.Distance(origin, new Vector3(spawn.x, origin.y, spawn.z));
        if (SolidAhead(origin, dir, toMuzzle + 0.2f)) spawn = origin;

        Projectile p = ProjectilePool.Get(BulletPrefab, spawn, Quaternion.LookRotation(dir));
        p.Launch(dir, s.projectileSpeed, s.damage, s.range, team, body, s.knockback, null, s.armorPenetration, s.damageFalloff, s.projectileSize);
    }

    // ---- Cone display ------------------------------------------------------

    void RefreshCone()
    {
        if (coneMaterial == null) return;

        if (coneObject == null)
        {
            coneObject = new GameObject("FiringCone");
            coneObject.transform.SetParent(transform, false);
            coneObject.AddComponent<MeshFilter>();
            var mr = coneObject.AddComponent<MeshRenderer>();
            mr.sharedMaterial = coneMaterial;
            mr.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            mr.receiveShadows = false;
            coneMesh = new Mesh { name = "FiringCone" };
            coneObject.GetComponent<MeshFilter>().sharedMesh = coneMesh;

            // Lay it on the ground, whatever height the turret sits at.
            coneObject.transform.localPosition = new Vector3(0f, groundHeight - transform.position.y, 0f);
        }

        coneObject.SetActive(showCone);
        if (!showCone) return;

        float half = CurrentConeAngle * 0.5f;
        int segments = Mathf.Clamp(Mathf.CeilToInt(CurrentConeAngle / Mathf.Max(0.5f, coneSegmentAngle)), 2, 180);

        // Only rebuild the triangle list when the number of slices changes.
        if (segments != builtSegments)
        {
            builtSegments = segments;
            coneVerts = new Vector3[segments + 2];
            coneTris = new int[segments * 3];
            for (int i = 0; i < segments; i++)
            {
                coneTris[i * 3] = 0;
                coneTris[i * 3 + 1] = i + 1;
                coneTris[i * 3 + 2] = i + 2;
            }
            coneMesh.Clear();
            coneMesh.vertices = coneVerts;
            coneMesh.triangles = coneTris;
        }

        // The outline is re-measured every frame, so it follows walls as you drive and aim.
        Vector3 origin = AimOrigin();
        Vector3 forward = FlatForward();

        coneVerts[0] = Vector3.zero;
        for (int i = 0; i <= segments; i++)
        {
            float offset = Mathf.Lerp(-half, half, i / (float)segments);
            float d = ClipDistance(origin, forward, offset);
            float a = offset * Mathf.Deg2Rad;
            coneVerts[i + 1] = new Vector3(Mathf.Sin(a), 0f, Mathf.Cos(a)) * d;
        }

        coneMesh.vertices = coneVerts;
        coneMesh.RecalculateBounds();
    }
}
