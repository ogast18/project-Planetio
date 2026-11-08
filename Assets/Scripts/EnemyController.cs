using UnityEngine;

// A basic enemy: walks in a straight line toward the base, stops at its edge and hits it
// every few seconds. Put this on the Enemy object (together with a Health component).
[RequireComponent(typeof(Rigidbody))]
public class EnemyController : MonoBehaviour
{
    [Header("Movement (tune these)")]
    public float speed = 2.5f;          // units per second
    public float turnSpeed = 360f;      // degrees per second (only affects how fast it faces the base)

    [Header("Attack (tune these)")]
    public float attackRange = 1.2f;    // how close to the base's edge it gets before attacking
    public float attackDamage = 10f;
    public float attackInterval = 1f;   // seconds between hits

    Rigidbody rb;
    float yaw;
    float attackTimer;

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
    }

    void FixedUpdate()
    {
        PlayerBase b = PlayerBase.Instance;
        if (b == null || !b.Health.IsAlive)
        {
            rb.linearVelocity = Vector3.zero;   // nothing left to attack
            return;
        }

        float dt = Time.fixedDeltaTime;

        Vector3 to = b.transform.position - transform.position;
        to.y = 0f;
        float dist = to.magnitude;

        // Face the base.
        if (dist > 0.01f)
        {
            float targetYaw = Mathf.Atan2(to.x, to.z) * Mathf.Rad2Deg;
            yaw = Mathf.MoveTowardsAngle(yaw, targetYaw, turnSpeed * dt);
        }
        rb.MoveRotation(Quaternion.Euler(0f, yaw, 0f));
        rb.angularVelocity = Vector3.zero;

        float stopDistance = b.Health.radius + attackRange;

        if (dist > stopDistance)
        {
            // Walk toward the base.
            rb.linearVelocity = to.normalized * speed;
        }
        else
        {
            // Arrived: stand still and hit the base on a timer.
            rb.linearVelocity = Vector3.zero;

            attackTimer -= dt;
            if (attackTimer <= 0f)
            {
                attackTimer = attackInterval;
                b.Health.TakeDamage(attackDamage);
            }
        }
    }
}
