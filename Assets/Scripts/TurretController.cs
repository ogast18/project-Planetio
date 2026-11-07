using UnityEngine;

// Independent turret (3D version): it rotates toward the aim stick direction in WORLD space,
// so it keeps pointing where you aimed even while the hull turns underneath it.
// The barrel points along local +Z. Put this on a "Turret" object that is a child of the Vehicle.
public class TurretController : MonoBehaviour
{
    [Header("Input")]
    public VirtualStick aimStick;      // drag the AimZone object here

    [Header("Feel (tune these)")]
    public float turnSpeed = 270f;     // degrees per second

    // World heading the turret is facing (0 = +Z / screen up, 90 = +X / screen right).
    public float Angle { get; private set; }

    void Start()
    {
        Angle = transform.eulerAngles.y;
    }

    // LateUpdate so we override the rotation inherited from the hull after it has moved.
    void LateUpdate()
    {
        Vector2 aim = aimStick != null ? aimStick.Value : Vector2.zero;

        // Stick released: the turret simply stays where it was pointing.
        if (aim.sqrMagnitude > 0.0001f)
        {
            float target = Mathf.Atan2(aim.x, aim.y) * Mathf.Rad2Deg;
            Angle = Mathf.MoveTowardsAngle(Angle, target, turnSpeed * Time.deltaTime);
        }

        transform.rotation = Quaternion.Euler(0f, Angle, 0f);
    }
}
