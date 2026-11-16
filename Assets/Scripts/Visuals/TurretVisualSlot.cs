using UnityEngine;

/// <summary>
/// Goes on the Turret object (next to TurretController and TurretWeapon). It spawns the turret model for
/// the current turret and level, swaps it when the turret or its milestone design changes, points the
/// weapon's muzzle at the new model, and tells the model when a shot is fired.
/// </summary>
[RequireComponent(typeof(TurretController))]
public class TurretVisualSlot : MonoBehaviour
{
    [Tooltip("Empty child where the turret model is spawned. Created automatically if left empty")]
    [SerializeField] Transform visualParent;
    [Tooltip("Old placeholder shapes (e.g. Base and Barrel) that are hidden once a model is spawned. Leave the Muzzle empty out")]
    [SerializeField] GameObject[] hideWhenModelled;
    [Min(0.01f), Tooltip("Multiplies the turret model's size when it is spawned. 1 = as built")]
    [SerializeField] float modelScale = 1f;

    public TurretVisual Current { get; private set; }

    TurretController controller;
    TurretWeapon weapon;
    GameObject currentPrefab;
    bool warned;

    void Awake()
    {
        controller = GetComponent<TurretController>();
        weapon = GetComponent<TurretWeapon>();

        if (visualParent == null)
        {
            visualParent = new GameObject("TurretVisual").transform;
            visualParent.SetParent(transform, false);
        }
    }

    // Checked every frame (it is cheap), so changing the turret asset or its level, in Play Mode or
    // from the game, swaps the model by itself.
    void Update()
    {
        if (controller.stats == null) return;

        GameObject prefab = controller.stats.GetVisualPrefab(controller.level);
        if (prefab == null)
        {
            if (!warned)
            {
                warned = true;
                Debug.LogWarning($"TurretVisualSlot: '{controller.stats.name}' has no Visual Prefab for level {controller.level}, so the old shapes stay. Add an entry to its Visual Stages (From Level 1).", controller.stats);
            }
            return;
        }
        if (prefab != currentPrefab) Equip(prefab);
    }

    void Equip(GameObject prefab)
    {
        currentPrefab = prefab;   // set first, so a broken prefab is reported once, not every frame

        if (Current != null)
        {
            if (weapon) weapon.Fired -= Current.OnFire;
            Current.OnUnequip();
            Destroy(Current.gameObject);
            Current = null;
        }

        var go = Instantiate(prefab, visualParent);
        var visual = go.GetComponent<TurretVisual>();
        if (visual == null)
        {
            Debug.LogError($"{prefab.name} has no TurretVisual component on its root.", prefab);
            Destroy(go);
            return;
        }

        go.transform.localPosition = Vector3.zero;
        go.transform.localRotation = Quaternion.Euler(0f, visual.ModelYaw, 0f);   // turns a backwards-facing model round
        go.transform.localScale = prefab.transform.localScale * modelScale;
        visual.Initialize(transform);                                              // after the turn: it records the rest pose

        if (weapon)
        {
            if (visual.Muzzle) weapon.muzzle = visual.Muzzle;
            else Debug.LogWarning($"{prefab.name}: TurretVisual has no Muzzle assigned, so bullets still leave from the old one.", prefab);
            weapon.Fired += visual.OnFire;
        }

        foreach (GameObject g in hideWhenModelled)
            if (g) g.SetActive(false);

        Current = visual;
    }

    void OnDestroy()
    {
        if (weapon && Current != null) weapon.Fired -= Current.OnFire;
    }
}
