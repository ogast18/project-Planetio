using UnityEngine;

// Independent turret (3D version): it rotates toward the aim stick direction in WORLD space,
// so it keeps pointing where you aimed even while the hull turns underneath it.
// The barrel points along local +Z. Put this on a "Turret" object that is a child of the Vehicle.
public class TurretController : MonoBehaviour
{
    [Header("Input")]
    public VirtualStick aimStick;      // drag the AimZone object here

    [Header("Stats")]
    public TurretStats stats;          // drag the Cannon asset here
    [Range(1, TurretStats.MaxLevel)] public int level = 1;

    // Stats for the current level. The firing script reads this too.
    public TurretStats.Resolved Current => stats != null ? stats.AtLevel(level) : default;

    // World heading the turret is facing (0 = +Z / screen up, 90 = +X / screen right).
    public float Angle { get; private set; }

    void Start()
    {
        Angle = transform.eulerAngles.y;
        if (stats == null)
            Debug.LogError("TurretController has no TurretStats assigned.", this);
    }

    // LateUpdate so we override the rotation inherited from the hull after it has moved.
    void LateUpdate()
    {
        Vector2 aim = aimStick != null ? aimStick.Value : Vector2.zero;

        // Stick released: the turret simply stays where it was pointing.
        if (aim.sqrMagnitude > 0.0001f)
        {
            float target = Mathf.Atan2(aim.x, aim.y) * Mathf.Rad2Deg;
            Angle = Mathf.MoveTowardsAngle(Angle, target, Current.turnSpeed * Time.deltaTime);
        }

        transform.rotation = Quaternion.Euler(0f, Angle, 0f);
    }
}