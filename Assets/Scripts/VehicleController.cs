using UnityEngine;
using UnityEngine.InputSystem;

// Basic hull: car-like movement driven by a single direction input.
// The hull can only turn while it is rolling. It prefers driving forward: it only
// reverses when the stick points inside a narrow cone straight behind the hull.
[RequireComponent(typeof(Rigidbody2D))]
public class VehicleController : MonoBehaviour
{
    [Header("Movement (tune these)")]
    public float maxSpeed = 6f;          // forward top speed, units per second
    public float maxReverseSpeed = 3.5f; // reverse top speed
    public float acceleration = 14f;     // how fast it speeds up and brakes
    public float turnSpeed = 220f;       // degrees per second at full turn rate
    public float sideGrip = 8f;          // higher = less drifting

    [Header("Turning needs movement")]
    public float fullTurnSpeed = 2f;     // speed at which it turns at the full rate (0 speed = no turning)
    public float minDrive = 0.35f;       // minimum throttle when the target is not in front, so it can start turning

    [Header("Forward / reverse choice")]
    public float reverseConeAngle = 90f;    // total width of the cone straight behind the hull that triggers reverse
    public float reverseSwitchMargin = 12f; // degrees of hysteresis, stops flicking between forward and reverse at the cone edge

    [Header("Input")]
    public VirtualStick moveStick;       // drag the MoveZone object here

    // Final input used by the physics (length 0..1): the stick, or WASD as an editor fallback.
    public Vector2 MoveInput { get; set; }

    Rigidbody2D rb;
    bool reversing;

    void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
    }

    void Update()
    {
        ReadInput();
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

    void FixedUpdate()
    {
        float dt = Time.fixedDeltaTime;
        float mag = Mathf.Clamp01(MoveInput.magnitude);
        float angle = rb.rotation;

        // Current speed along the facing direction (before turning this step).
        float rad = angle * Mathf.Deg2Rad;
        Vector2 forward = new Vector2(Mathf.Cos(rad), Mathf.Sin(rad));
        float currentFwdSpeed = Vector2.Dot(rb.linearVelocity, forward);

        float targetSpeed = 0f;

        if (mag > 0.01f)
        {
            float targetAngle = Mathf.Atan2(MoveInput.y, MoveInput.x) * Mathf.Rad2Deg;
            float deltaFwd = Mathf.Abs(Mathf.DeltaAngle(angle, targetAngle));

            // Decide: reverse only if the stick points inside the cone straight behind the hull,
            // otherwise always drive forward (so a stand-still turn goes forward on both sides).
            // The margin only applies while rolling, so a fresh push from a stop decides cleanly.
            float reverseThreshold = 180f - reverseConeAngle * 0.5f;
            float margin = Mathf.Abs(currentFwdSpeed) > 1f ? reverseSwitchMargin : 0f;
            if (!reversing && deltaFwd > reverseThreshold + margin) reversing = true;
            else if (reversing && deltaFwd < reverseThreshold - margin) reversing = false;

            // When reversing, the BACK of the hull should point at the stick direction.
            float desiredAngle = reversing ? targetAngle + 180f : targetAngle;
            float delta = Mathf.DeltaAngle(angle, desiredAngle);

            // Turn only as much as we are rolling: no speed, no turning.
            float turnFactor = Mathf.Clamp01(Mathf.Abs(currentFwdSpeed) / fullTurnSpeed);
            angle = Mathf.MoveTowardsAngle(angle, desiredAngle, turnSpeed * turnFactor * dt);
            rb.MoveRotation(angle);

            // Throttle: full when aligned, never below minDrive so it can always start moving and turn.
            float alignment = Mathf.Max(minDrive, Mathf.Cos(delta * Mathf.Deg2Rad));
            float topSpeed = reversing ? -maxReverseSpeed : maxSpeed;
            targetSpeed = topSpeed * mag * alignment;
        }

        // Forward (+X of the hull) and side directions from the new angle.
        rad = angle * Mathf.Deg2Rad;
        forward = new Vector2(Mathf.Cos(rad), Mathf.Sin(rad));
        Vector2 side = new Vector2(-forward.y, forward.x);

        Vector2 vel = rb.linearVelocity;
        float fwdSpeed = Vector2.Dot(vel, forward);
        float sideSpeed = Vector2.Dot(vel, side);

        // Accelerate or brake along the facing direction, and bleed off sideways slide.
        fwdSpeed = Mathf.MoveTowards(fwdSpeed, targetSpeed, acceleration * dt);
        sideSpeed = Mathf.Lerp(sideSpeed, 0f, sideGrip * dt);

        rb.linearVelocity = forward * fwdSpeed + side * sideSpeed;
    }
}
