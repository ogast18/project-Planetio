using UnityEngine;

/// <summary>
/// Laser model: shows the beam while the weapon is firing, from the muzzle to wherever the beam stops
/// (a wall or the first enemy). Set the beam's look (material, colour, width) on the LineRenderer in the editor.
/// </summary>
public class LaserTurretVisual : TurretVisual
{
    [Header("Beam")]
    [Tooltip("A LineRenderer (on a child object of the model) with your beam material. Its positions are set by this script")]
    [SerializeField] LineRenderer beam;
    [Tooltip("Optional particles played where the beam ends")]
    [SerializeField] ParticleSystem impact;
    [SerializeField] float pulseSpeed = 30f;
    [SerializeField, Range(0f, 0.5f)] float pulseAmount = 0.15f;

    float baseWidth = 1f;

    public override void Initialize(Transform turretRoot)
    {
        base.Initialize(turretRoot);

        if (beam)
        {
            beam.useWorldSpace = true;
            beam.positionCount = 2;
            baseWidth = beam.widthMultiplier;
            beam.enabled = false;
        }
    }

    public override void OnFireStop()
    {
        base.OnFireStop();
        if (beam) beam.enabled = false;
        if (impact) impact.Stop();
    }

    protected override void Update()
    {
        base.Update();
        if (!beam || Weapon == null) return;

        if (!Weapon.IsFiring)
        {
            if (beam.enabled) beam.enabled = false;
            return;
        }

        beam.enabled = true;
        Vector3 start = Muzzle ? Muzzle.position : transform.position;
        Vector3 end = Weapon.BeamEnd;
        end.y = start.y;
        beam.SetPosition(0, start);
        beam.SetPosition(1, end);
        beam.widthMultiplier = baseWidth * (1f + Mathf.Sin(Time.time * pulseSpeed) * pulseAmount);

        if (impact)
        {
            impact.transform.position = end;
            if (!impact.isPlaying) impact.Play();
        }
    }
}
