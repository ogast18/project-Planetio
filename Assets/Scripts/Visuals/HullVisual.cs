using UnityEngine;

/// <summary>
/// Base class for every hull model. It only does looks (wheels, tracks, thrusters),
/// never movement. It measures how the vehicle actually moves (speed and turn rate), so it
/// matches whatever the movement script does.
/// </summary>
public abstract class HullVisual : MonoBehaviour
{
    [Header("Shared sockets (every hull model needs these)")]
    [SerializeField] Transform turretMount;
    [SerializeField] Transform muzzle;
    [Tooltip("Blender placeholder turret; hidden when the real turret is attached")]
    [SerializeField] GameObject placeholderTurret;

    [Header("Orientation")]
    [Tooltip("Turns the whole model around the vertical axis when it is spawned. 180 = the model came in facing backwards. 0 = no turn")]
    [SerializeField] float modelYaw = 180f;

    public Transform TurretMount => turretMount;
    public Transform Muzzle => muzzle;
    public GameObject PlaceholderTurret => placeholderTurret;
    public float ModelYaw => modelYaw;

    // measured motion of the vehicle (world units / s, in the vehicle's own directions)
    protected Transform Root { get; private set; }
    protected Vector3 LocalVelocity { get; private set; }
    protected float ForwardSpeed => LocalVelocity.z;
    protected float YawRate { get; private set; }          // rad/s, positive = turning right
    protected float Scale => Mathf.Max(0.0001f, transform.lossyScale.y);

    const float Smoothing = 20f;
    Rigidbody rb;
    Vector3 lastPos;
    float lastYaw;
    Vector3 measuredVel;
    float measuredYaw;

    /// <summary>Called by HullVisualSlot right after the model is spawned, at rest.</summary>
    public virtual void Initialize(Transform vehicleRoot)
    {
        Root = vehicleRoot;

        // An Animator on the model would fight the scripts that drive the wheels.
        foreach (Animator an in GetComponentsInChildren<Animator>(true)) an.enabled = false;

        rb = Root.GetComponent<Rigidbody>();
        lastPos = rb ? rb.position : Root.position;
        lastYaw = (rb ? rb.rotation : Root.rotation).eulerAngles.y;
        measuredVel = LocalVelocity = Vector3.zero;
        measuredYaw = YawRate = 0f;
    }

    public virtual void OnUnequip() { }

    // Measured once per physics step, which is when the vehicle really moves.
    void FixedUpdate()
    {
        if (Root == null) return;
        float dt = Time.fixedDeltaTime;
        if (dt <= 0f) return;

        Vector3 pos = rb ? rb.position : Root.position;
        Quaternion rot = rb ? rb.rotation : Root.rotation;
        float yaw = rot.eulerAngles.y;

        measuredVel = Quaternion.Inverse(rot) * ((pos - lastPos) / dt);
        measuredYaw = Mathf.DeltaAngle(lastYaw, yaw) * Mathf.Deg2Rad / dt;
        lastPos = pos;
        lastYaw = yaw;
    }

    void LateUpdate()
    {
        float dt = Time.deltaTime;
        if (Root == null || dt <= 0f) return;

        float k = 1f - Mathf.Exp(-Smoothing * dt);
        LocalVelocity = Vector3.Lerp(LocalVelocity, measuredVel, k);
        YawRate = Mathf.Lerp(YawRate, measuredYaw, k);

        Tick(dt);
    }

    /// <summary>Called every frame after the motion values are updated.</summary>
    protected abstract void Tick(float dt);
}
