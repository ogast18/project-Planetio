using UnityEngine;

/// <summary>
/// Goes on the root of every turret model (cannon, minigun, ...). It marks where the bullets leave,
/// slides the barrel back when the turret fires, and plays an optional muzzle flash.
/// Weapons with their own look (spinning minigun barrels, flame, laser) can inherit from this
/// and override OnFire() / Update().
/// </summary>
public class TurretVisual : MonoBehaviour
{
    [Header("Sockets")]
    [Tooltip("Empty object at the tip of the barrel: bullets are spawned here")]
    [SerializeField] Transform muzzle;

    [Header("Orientation")]
    [Tooltip("Turns the whole model around the vertical axis when it is spawned. 180 = the model came in facing backwards (like the Blender hull). 0 = no turn")]
    [SerializeField] float modelYaw = 180f;

    [Header("Recoil")]
    [Tooltip("The part that slides back when firing (the barrel). Empty = no recoil")]
    [SerializeField] Transform recoilPart;
    [SerializeField] float recoilDistance = 0.12f;   // metres
    [SerializeField] float recoilTime = 0.18f;       // seconds to slide back to rest

    [Header("Muzzle flash (optional)")]
    [SerializeField] ParticleSystem muzzleFlash;

    public Transform Muzzle => muzzle;
    public float ModelYaw => modelYaw;

    protected Transform Root { get; private set; }   // the Turret object (the part that aims)
    protected TurretWeapon Weapon { get; private set; }   // the weapon on that object (for beam / flame looks)

    Vector3 restLocalPos, recoilAxisLocal;
    float localDistance, recoil01;

    /// <summary>Called by TurretVisualSlot right after the model is spawned, at rest.</summary>
    public virtual void Initialize(Transform turretRoot)
    {
        Root = turretRoot;
        Weapon = turretRoot.GetComponent<TurretWeapon>();

        // An Animator on the model would fight the scripts that move its parts.
        foreach (Animator an in GetComponentsInChildren<Animator>(true)) an.enabled = false;

        if (recoilPart)
        {
            Transform parent = recoilPart.parent;
            restLocalPos = recoilPart.localPosition;

            // "Backwards" = away from where the turret points, worked out in the barrel's own space,
            // so it is right whatever way the model was imported.
            Vector3 back = -Root.forward;
            recoilAxisLocal = (parent ? parent.InverseTransformDirection(back) : back).normalized;
            float s = parent ? Mathf.Max(0.0001f, parent.lossyScale.y) : 1f;
            localDistance = recoilDistance / s;
        }
    }

    /// <summary>Called every time the turret fires.</summary>
    public virtual void OnFire()
    {
        recoil01 = 1f;
        if (muzzleFlash) muzzleFlash.Play();
    }

    /// <summary>Beam and flame turrets: called when the weapon starts and stops damaging things.</summary>
    public virtual void OnFireStart() { }
    public virtual void OnFireStop() { }

    public virtual void OnUnequip() { }

    protected virtual void Update()
    {
        if (!recoilPart || Root == null) return;

        recoil01 = Mathf.MoveTowards(recoil01, 0f, Time.deltaTime / Mathf.Max(0.01f, recoilTime));
        float k = recoil01 * recoil01;   // kicks back at once, then eases forward
        recoilPart.localPosition = restLocalPos + recoilAxisLocal * (localDistance * k);
    }
}
