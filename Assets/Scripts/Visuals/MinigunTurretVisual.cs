using UnityEngine;

/// <summary>
/// Minigun model: the barrel cluster spins up while the turret is firing and winds down after it stops.
/// Everything else (muzzle, optional recoil, flash) comes from TurretVisual.
/// </summary>
public class MinigunTurretVisual : TurretVisual
{
    [Header("Spinning barrels")]
    [Tooltip("The barrel cluster (Minigun_Barrels). It spins around the direction the turret points")]
    [SerializeField] Transform spinPart;
    [SerializeField] float maxSpinSpeed = 1080f;   // degrees per second
    [SerializeField] float spinUpTime = 0.4f;      // seconds to reach full speed
    [SerializeField] float spinDownTime = 0.8f;    // seconds to wind down after the last shot
    [Tooltip("Barrels keep spinning this long after a shot, so fast fire looks continuous")]
    [SerializeField] float keepSpinningFor = 0.2f;
    [SerializeField] bool invertSpin;

    Quaternion spinStart;
    Vector3 spinAxis = Vector3.forward;
    float spinAngle, spinSpeed, lastShotTime = -999f;

    public override void Initialize(Transform turretRoot)
    {
        base.Initialize(turretRoot);

        if (spinPart)
        {
            // Spin around the way the turret points, worked out in the barrels' own space,
            // so it is right whatever way the model was imported.
            Transform parent = spinPart.parent;
            Vector3 fwd = Root.forward;
            spinStart = spinPart.localRotation;
            spinAxis = (parent ? parent.InverseTransformDirection(fwd) : fwd).normalized;
        }
    }

    public override void OnFire()
    {
        base.OnFire();
        lastShotTime = Time.time;
    }

    protected override void Update()
    {
        base.Update();   // recoil, if a recoil part is set
        if (!spinPart || Root == null) return;

        bool firing = Time.time - lastShotTime < keepSpinningFor;
        float target = firing ? maxSpinSpeed : 0f;
        float rate = maxSpinSpeed / Mathf.Max(0.01f, firing ? spinUpTime : spinDownTime);
        spinSpeed = Mathf.MoveTowards(spinSpeed, target, rate * Time.deltaTime);

        spinAngle = Mathf.Repeat(spinAngle + (invertSpin ? -spinSpeed : spinSpeed) * Time.deltaTime, 360f);
        spinPart.localRotation = Quaternion.AngleAxis(spinAngle, spinAxis) * spinStart;
    }
}
