using UnityEngine;

// A simple bullet. It moves itself and checks what is in front of it every frame,
// so it needs no Rigidbody and no collider of its own.
public class Projectile : MonoBehaviour
{
    public float radius = 0.15f;   // how fat the bullet is for hit detection

    Vector3 direction;
    float speed;
    float damage;
    float remainingDistance;
    Team ownerTeam;

    public void Launch(Vector3 dir, float speed, float damage, float range, Team ownerTeam)
    {
        direction = dir.normalized;
        this.speed = speed;
        this.damage = damage;
        this.remainingDistance = range;
        this.ownerTeam = ownerTeam;
    }

    void Update()
    {
        float step = speed * Time.deltaTime;

        if (Physics.SphereCast(transform.position, radius, direction, out RaycastHit hit, step,
                               ~0, QueryTriggerInteraction.Ignore))
        {
            Health health = hit.collider.GetComponentInParent<Health>();

            // Same team: fly through. Anything else (enemy, wall, obstacle) stops the bullet.
            if (health == null || health.team != ownerTeam)
            {
                if (health != null) health.TakeDamage(damage);
                Destroy(gameObject);
                return;
            }
        }

        transform.position += direction * step;
        remainingDistance -= step;
        if (remainingDistance <= 0f) Destroy(gameObject);
    }
}
