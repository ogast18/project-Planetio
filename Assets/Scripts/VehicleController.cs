using UnityEngine;
using UnityEngine.InputSystem;

// Basic hull (3D version): car-like movement on the ground plane (X/Z).
// The nose of the hull is local +Z. Stick up = world +Z (screen up), stick right = world +X.
// The hull can only turn while it is rolling. It prefers driving forward: it only
// reverses when the stick points inside a narrow cone straight behind the hull.
// All numbers (movement, health, armor) come from the HullStats asset at the chosen level.
[RequireComponent(typeof(Rigidbody))]
public class VehicleController : MonoBehaviour
{
    [Header("Stats")]
    public HullStats stats;              // drag the hull's stats asset here (e.g. BasicHull)
    [Range(1, HullStats.MaxLevel)] public int level = 1;

    [Header("Input")]
    public VirtualStick moveStick;       // drag the MoveZone object here

    // Final input used by the physics (length 0..1): the stick, or WASD as an editor fallback.
    public Vector2 MoveInput { get; set; }

    Rigidbody rb;
    Health health;
    HullStats.Resolved h;   // this frame's stats (level applied)
    float yaw;          // our own heading in degrees (0 = facing +Z). Collisions can't change it.
    bool reversing;

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
        rb.collisionDetectionMode = CollisionDetectionMode.Continuous;
        rb.linearDamping = 0f;
        rb.angularDamping = 0f;

        yaw = transform.eulerAngles.y;

        if (stats == null)
            Debug.LogError("VehicleController has no HullStats assigned, so the vehicle cannot move.", this);
        RefreshStats();
    }

    void Update()
    {
        RefreshStats();   // every frame, so changing the level or the asset in Play Mode is live
        ReadInput();
    }

    // Reads the stats for the current level and gives the Health its max health and armor.
    void RefreshStats()
    {
        if (stats == null) return;
        h = stats.AtLevel(level);
        if (health != null) health.Configure(h.maxHealth, h.armorPercent);
    }

    // The move stick wins when touched; WASD / arrow keys stay as an editor fallback.
    void ReadInput()
    {
        Vector2 stick = moveStick != null ? moveStick.Value : Vector2.zero;
        Vector2 keys = ReadKeyboard();
        MoveInput = stick.sqrMagnitude > 0.0001f ? stick : keys;
    }

    Vector2 ReadKeyboard()
    {
        var kb = Keyboard.current;
        if (kb == null) return Vector2.zero;

        Vector2 v = Vector2.zero;
        if (kb.wKey.isPressed || kb.upArrowKey.isPressed) v.y += 1f;
        if (kb.sKey.isPressed || kb.downArrowKey.isPressed) v.y -= 1f;
        if (kb.dKey.isPressed || kb.rightArrowKey.isPressed) v.x += 1f;
        if (kb.aKey.isPressed || kb.leftArrowKey.isPressed) v.x -= 1f;

        return v.normalized;
    }

    static Vector3 Forward(float yawDegrees)
    {
        float r = yawDegrees * Mathf.Deg2Rad;
        return new Vector3(Mathf.Sin(r), 0f, Mathf.Cos(r));
    }

    void FixedUpdate()
    {
        if (stats == null) return;

        float dt = Time.fixedDeltaTime;
        float mag = Mathf.Clamp01(MoveInput.magnitude);

        // Current speed along the facing direction (before turning this step).
        Vector3 forward = Forward(yaw);
        Vector3 vel = rb.linearVelocity;
        float currentFwdSpeed = Vector3.Dot(vel, forward);

        float targetSpeed = 0f;

        if (mag > 0.01f)
        {
            // Direction the stick points, as a heading (0 = +Z, 90 = +X).
            float targetYaw = Mathf.Atan2(MoveInput.x, MoveInput.y) * Mathf.Rad2Deg;
            float deltaFwd = Mathf.Abs(Mathf.DeltaAngle(yaw, targetYaw));

            // Reverse only if the stick points inside the cone straight behind the hull,
            // otherwise always drive forward (so a stand-still turn goes forward on both sides).
            // The margin only applies while rolling, so a fresh push from a stop decides cleanly.
            float reverseThreshold = 180f - h.reverseConeAngle * 0.5f;
            float margin = Mathf.Abs(currentFwdSpeed) > 1f ? h.reverseSwitchMargin : 0f;
            if (!reversing && deltaFwd > reverseThreshold + margin) reversing = true;
            else if (reversing && deltaFwd < reverseThreshold - margin) reversing = false;

            // When reversing, the BACK of the hull should point at the stick direction.
            float desiredYaw = reversing ? targetYaw + 180f : targetYaw;
            float delta = Mathf.DeltaAngle(yaw, desiredYaw);

            // Turn only as much as we are rolling: no speed, no turning.
            float turnFactor = Mathf.Clamp01(Mathf.Abs(currentFwdSpeed) / h.fullTurnSpeed);
            yaw = Mathf.MoveTowardsAngle(yaw, desiredYaw, h.turnSpeed * turnFactor * dt);

            // Throttle: full when aligned, never below minDrive so it can always start moving and turn.
            float alignment = Mathf.Max(h.minDrive, Mathf.Cos(delta * Mathf.Deg2Rad));
            float topSpeed = reversing ? -h.maxReverseSpeed : h.maxSpeed;
            targetSpeed = topSpeed * mag * alignment;
        }

        // Apply our heading and ignore any spin that collisions tried to add.
        rb.MoveRotation(Quaternion.Euler(0f, yaw, 0f));
        rb.angularVelocity = Vector3.zero;

        // Forward and side directions from the new heading (side = right-hand side seen from above).
        forward = Forward(yaw);
        Vector3 side = new Vector3(forward.z, 0f, -forward.x);

        Vector3 flat = new Vector3(vel.x, 0f, vel.z);
        float fwdSpeed = Vector3.Dot(flat, forward);
        float sideSpeed = Vector3.Dot(flat, side);

        // Accelerate or brake along the facing direction, and bleed off sideways slide.
        fwdSpeed = Mathf.MoveTowards(fwdSpeed, targetSpeed, h.acceleration * dt);
        sideSpeed = Mathf.Lerp(sideSpeed, 0f, h.sideGrip * dt);

        rb.linearVelocity = forward * fwdSpeed + side * sideSpeed;
    }
}
