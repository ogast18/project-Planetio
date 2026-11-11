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
    System.Action<float, Vector3> onDamageDealt;   // told (damage, where it was fired from) when it hurts something
    Vector3 launchPosition;
    float remainingDistance;
    Team ownerTeam;
    Rigidbody ignoreBody;          // the shooter's own body, never hit by its own bullets
    bool startChecked;

    public void Launch(Vector3 dir, float speed, float damage, float range, Team ownerTeam, Rigidbody ignoreBody = null, float knockback = 0f,
                       System.Action<float, Vector3> onDamageDealt = null, float armorPenetration = 0f)
    {
        this.onDamageDealt = onDamageDealt;
        this.armorPenetration = armorPenetration;
        launchPosition = transform.position;
        direction = dir.normalized;
        this.speed = speed;
        this.damage = damage;
        this.remainingDistance = range;
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
                        health.TakeDamage(damage, direction, knockback, armorPenetration);
                        onDamageDealt?.Invoke(damage, launchPosition);
                    }
                    Destroy(gameObject);
                    return;
                }
            }
        }

        transform.position += direction * step;
        remainingDistance -= step;
        if (remainingDistance <= 0f) Destroy(gameObject);
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
            target.TakeDamage(damage, direction, knockback, armorPenetration);
            onDamageDealt?.Invoke(damage, launchPosition);
        }
        if (target != null || blocked)
        {
            Destroy(gameObject);
            return true;
        }
        return false;
    }
}
