using UnityEngine;

// The cannon: fires automatically whenever an enemy is inside the firing cone.
// The cone is a pie slice in front of the turret (width = coneAngle, length = range).
// Put this on the same Turret object as TurretController.
public class TurretWeapon : MonoBehaviour
{
    [Header("References")]
    public Transform muzzle;               // drag the Muzzle object here
    public Projectile projectilePrefab;    // drag the Projectile prefab here
    public Material coneMaterial;          // transparent material for the cone display (optional)

    [Header("Cannon (tune these)")]
    public Team team = Team.Player;
    public float damage = 25f;
    public float fireRate = 1.5f;          // shots per second
    public float projectileSpeed = 18f;    // units per second
    public float range = 12f;              // how far it shoots and how long the cone is
    [Range(1f, 360f)]
    public float coneAngle = 30f;          // total width of the firing cone, in degrees

    [Header("Cone display")]
    public bool showCone = true;
    public float groundHeight = 0.05f;     // height of the cone above the ground

    float cooldown;
    GameObject coneObject;
    Mesh coneMesh;
    float builtAngle = -1f, builtRange = -1f;

    void Start()
    {
        RefreshCone();
    }

    void Update()
    {
        cooldown = Mathf.Max(0f, cooldown - Time.deltaTime);

        if (cooldown <= 0f && HasTargetInCone())
        {
            Fire();
            cooldown = 1f / Mathf.Max(0.01f, fireRate);
        }

        RefreshCone();
    }

    bool HasTargetInCone()
    {
        Vector3 forward = transform.forward;
        forward.y = 0f;

        foreach (Health h in Health.All)
        {
            if (h.team == team || !h.IsAlive) continue;

            Vector3 to = h.transform.position - transform.position;
            to.y = 0f;
            float dist = to.magnitude;
            if (dist - h.radius > range) continue;

            // Counts as inside if any part of the target (its radius) touches the cone.
            float angle = Vector3.Angle(forward, to);
            float margin = dist > 0.01f ? Mathf.Asin(Mathf.Clamp01(h.radius / dist)) * Mathf.Rad2Deg : 180f;
            if (angle - margin <= coneAngle * 0.5f) return true;
        }
        return false;
    }

    void Fire()
    {
        if (projectilePrefab == null || muzzle == null) return;

        Vector3 dir = muzzle.forward;
        dir.y = 0f;

        Projectile p = Instantiate(projectilePrefab, muzzle.position, Quaternion.LookRotation(dir));
        p.Launch(dir, projectileSpeed, damage, range, team);
    }

    // ---- Cone display ----------------------------------------------------

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

        // Rebuild only when the numbers changed (for example while tuning in Play mode).
        if (Mathf.Approximately(builtAngle, coneAngle) && Mathf.Approximately(builtRange, range)) return;
        builtAngle = coneAngle;
        builtRange = range;

        int segments = Mathf.Max(2, Mathf.CeilToInt(coneAngle / 5f));
        float half = coneAngle * 0.5f;

        var verts = new Vector3[segments + 2];
        var tris = new int[segments * 3];
        verts[0] = Vector3.zero;

        for (int i = 0; i <= segments; i++)
        {
            float a = Mathf.Lerp(-half, half, i / (float)segments) * Mathf.Deg2Rad;
            verts[i + 1] = new Vector3(Mathf.Sin(a), 0f, Mathf.Cos(a)) * range;
        }

        for (int i = 0; i < segments; i++)
        {
            tris[i * 3] = 0;
            tris[i * 3 + 1] = i + 1;
            tris[i * 3 + 2] = i + 2;
        }

        coneMesh.Clear();
        coneMesh.vertices = verts;
        coneMesh.triangles = tris;
        coneMesh.RecalculateNormals();
        coneMesh.RecalculateBounds();
    }
}
