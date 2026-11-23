using UnityEngine;

/// <summary>
/// Flamethrower model: plays a flame particle effect while the weapon is firing. The flame is fitted to the
/// turret's range and cone, so it reaches as far as the damage does.
/// </summary>
public class FlameTurretVisual : TurretVisual
{
    [Header("Flame")]
    [Tooltip("A ParticleSystem on a child object at the nozzle, pointing forward (use a Cone shape)")]
    [SerializeField] ParticleSystem flame;
    [Tooltip("Sets the flame's lifetime from the turret's range and its cone angle from the turret's cone, each time it starts")]
    [SerializeField] bool fitToStats = true;

    public override void OnFireStart()
    {
        base.OnFireStart();
        if (!flame) return;

        if (fitToStats) Fit();
        if (!flame.isPlaying) flame.Play();
    }

    public override void OnFireStop()
    {
        base.OnFireStop();
        if (flame) flame.Stop();   // stops making new flames, the ones already out fade away
    }

    // The flame always points the way the turret aims, whatever way the model was imported.
    protected override void Update()
    {
        base.Update();
        if (flame && Root != null && Weapon != null && Weapon.IsFiring)
            flame.transform.rotation = Quaternion.LookRotation(Root.forward, Vector3.up);
    }

    void Fit()
    {
        if (Weapon == null) return;

        var main = flame.main;
        float speed = main.startSpeed.constant;
        if (speed > 0.01f) main.startLifetime = Weapon.Range / speed;

        var shape = flame.shape;
        if (shape.shapeType == ParticleSystemShapeType.Cone)
            shape.angle = Weapon.CurrentConeAngle * 0.5f;
    }
}
