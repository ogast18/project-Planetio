using UnityEngine;

/// <summary>Wheeled (car-like) hull: front wheels steer like a real car, all wheels spin with speed.
/// Steering angle comes from the real turn rate: tan(angle) = wheelBase * yawRate / speed.
/// The steering and spin axes are worked out from the vehicle's own up and right directions,
/// so it works whatever way the model was imported.</summary>
public class WheeledHullVisual : HullVisual
{
    [Header("Front wheels (steering)")]
    [Tooltip("The SteerPivot object (the parent of the front wheel), NOT the wheel itself")]
    [SerializeField] Transform steerPivotLeft;
    [SerializeField] Transform steerPivotRight;
    [SerializeField] float wheelBase = 2.3f;        // front axle to rear axle in the unscaled model (BasicHull = 2.3)
    [SerializeField] float maxSteerAngle = 25f;
    [SerializeField] float steerSpeed = 240f;       // deg/s
    [SerializeField] float minSteerSpeed = 0.3f;    // below this the wheels return to straight
    [SerializeField] bool invertSteer;

    [Header("Wheels (spin)")]
    [Tooltip("All four WHEEL objects (Wheel_FL, Wheel_FR, Wheel_RL, Wheel_RR)")]
    [SerializeField] Transform[] wheels;
    [SerializeField] float wheelRadius = 0.42f;
    [SerializeField] bool invertSpin;

    [Header("Debug")]
    [Tooltip("Ignores the driving and turns the front wheels by the angle below, to check that the wheels can steer at all")]
    [SerializeField] bool useTestSteer;
    [SerializeField, Range(-30f, 30f)] float testSteerAngle;
    [Tooltip("Prints the measured speed, turn rate and steering angle to the Console twice a second while driving")]
    [SerializeField] bool debugLog;
    float nextLog;

    Quaternion leftStart, rightStart;
    Vector3 leftAxis, rightAxis;
    Quaternion[] wheelStart;
    Vector3[] wheelAxis;
    float steerAngle, spinAngle;
    bool ready;

    public override void Initialize(Transform vehicleRoot)
    {
        base.Initialize(vehicleRoot);

        // Rest pose. Axes are the vehicle's up (steering) and right (wheel axle),
        // expressed in the space of each part's parent.
        Vector3 up = Root.up, right = Root.right;

        CheckPivot(steerPivotLeft, "Steer Pivot Left");
        CheckPivot(steerPivotRight, "Steer Pivot Right");
        if (steerPivotLeft) { leftStart = steerPivotLeft.localRotation; leftAxis = AxisInParent(steerPivotLeft, up); }
        if (steerPivotRight) { rightStart = steerPivotRight.localRotation; rightAxis = AxisInParent(steerPivotRight, up); }

        int n = wheels != null ? wheels.Length : 0;
        wheelStart = new Quaternion[n];
        wheelAxis = new Vector3[n];
        for (int i = 0; i < n; i++)
        {
            if (!wheels[i]) { Debug.LogWarning($"WheeledHullVisual: Wheels element {i} is empty.", this); continue; }
            if (!wheels[i].IsChildOf(transform)) Debug.LogWarning($"WheeledHullVisual: Wheels element {i} ('{wheels[i].name}') is not part of this hull model. Drag the object from the prefab's hierarchy.", this);
            if (wheels[i] == steerPivotLeft || wheels[i] == steerPivotRight) Debug.LogWarning($"WheeledHullVisual: Wheels element {i} is a Steer Pivot. It should be the wheel under it.", this);
            wheelStart[i] = wheels[i].localRotation;
            wheelAxis[i] = AxisInParent(wheels[i], right);
        }
        ready = true;
    }

    bool CheckPivot(Transform pivot, string label)
    {
        if (!pivot) { Debug.LogWarning($"WheeledHullVisual: {label} is empty, so the front wheels can't steer.", this); return false; }
        if (!pivot.IsChildOf(transform)) { Debug.LogWarning($"WheeledHullVisual: {label} ('{pivot.name}') is not part of this hull model. Drag the SteerPivot from the prefab's hierarchy.", this); return false; }
        if (pivot.childCount == 0) { Debug.LogWarning($"WheeledHullVisual: {label} ('{pivot.name}') has no wheel under it. It should be the SteerPivot, not the wheel.", this); return false; }
        return true;
    }

    static Vector3 AxisInParent(Transform t, Vector3 worldAxis)
    {
        Vector3 a = t.parent ? t.parent.InverseTransformDirection(worldAxis) : worldAxis;
        return a.normalized;
    }

    protected override void Tick(float dt)
    {
        if (!ready) return;

        // steering
        float target = 0f;
        if (Mathf.Abs(ForwardSpeed) > minSteerSpeed)
        {
            float rad = Mathf.Atan(wheelBase * Scale * YawRate / ForwardSpeed);
            target = Mathf.Clamp(rad * Mathf.Rad2Deg, -maxSteerAngle, maxSteerAngle);
        }
        if (invertSteer) target = -target;
        if (useTestSteer) target = testSteerAngle;
        steerAngle = Mathf.MoveTowards(steerAngle, target, steerSpeed * dt);

        if (steerPivotLeft) steerPivotLeft.localRotation = Quaternion.AngleAxis(steerAngle, leftAxis) * leftStart;
        if (steerPivotRight) steerPivotRight.localRotation = Quaternion.AngleAxis(steerAngle, rightAxis) * rightStart;

        if (debugLog && Time.time >= nextLog && (Mathf.Abs(ForwardSpeed) > 0.05f || Mathf.Abs(YawRate) > 0.01f))
        {
            nextLog = Time.time + 0.5f;
            Debug.Log($"Wheeled hull: speed {ForwardSpeed:0.00} m/s, turn rate {YawRate * Mathf.Rad2Deg:0} deg/s, steer angle {steerAngle:0.0} deg (target {target:0.0})", this);
        }

        // spin
        float deg = ForwardSpeed / (wheelRadius * Scale) * Mathf.Rad2Deg * dt;
        spinAngle = Mathf.Repeat(spinAngle + (invertSpin ? -deg : deg), 360f);
        for (int i = 0; i < wheels.Length; i++)
            if (wheels[i]) wheels[i].localRotation = Quaternion.AngleAxis(spinAngle, wheelAxis[i]) * wheelStart[i];
    }
}
