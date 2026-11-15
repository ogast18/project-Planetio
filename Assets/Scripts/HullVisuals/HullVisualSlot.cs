using UnityEngine;

/// <summary>
/// Sits on the vehicle. Spawns the hull model, swaps it when the hull or its level milestone
/// changes, and moves the real turret onto the new hull's turret mount (keeping its aim and size).
/// The model is always spawned under the object that carries the vehicle's Rigidbody, so it moves with the vehicle.
/// </summary>
public class HullVisualSlot : MonoBehaviour
{
    [Tooltip("Found automatically (the object with the vehicle's Rigidbody). Only used if none is found")]
    [SerializeField] Transform vehicleRoot;
    [Tooltip("Empty child where the hull model is spawned. Created automatically if left empty")]
    [SerializeField] Transform visualParent;
    [SerializeField] Transform turret;          // the real turret object; survives hull swaps

    [Header("Size")]
    [Min(0.01f), Tooltip("Multiplies the hull model's size when it is spawned. 0.5 = half size")]
    [SerializeField] float hullScale = 0.5f;

    [Header("Start look (for testing)")]
    [SerializeField] HullStats equipOnStart;    // optional: spawn this hull's model when the scene starts
    [SerializeField, Min(1)] int startLevel = 1;

    public HullVisual Current { get; private set; }
    public event System.Action<HullVisual> VisualChanged;

    GameObject currentPrefab;

    void Awake()
    {
        // The vehicle is whatever object carries the Rigidbody (the same rule TurretWeapon uses).
        // Look on this object, above it, below it, and above the turret.
        Rigidbody rb = GetComponentInParent<Rigidbody>();
        if (rb == null) rb = GetComponentInChildren<Rigidbody>();
        if (rb == null && turret != null) rb = turret.GetComponentInParent<Rigidbody>();

        if (rb != null) vehicleRoot = rb.transform;
        else if (vehicleRoot == null) vehicleRoot = transform;

        Debug.Log($"HullVisualSlot: the hull model will follow '{vehicleRoot.name}' " +
                  (rb != null ? "(its Rigidbody was found)." : "(NO Rigidbody found - it may not move with the vehicle)."), this);

        if (visualParent == null)
        {
            visualParent = new GameObject("HullVisual").transform;
            visualParent.SetParent(vehicleRoot, false);
        }
        else if (!visualParent.IsChildOf(vehicleRoot))
        {
            Debug.LogWarning("HullVisualSlot: Visual Parent was not under the moving vehicle, so it was moved there.", this);
            visualParent.SetParent(vehicleRoot, false);
            visualParent.localPosition = Vector3.zero;
            visualParent.localRotation = Quaternion.identity;
            visualParent.localScale = Vector3.one;
        }
    }

    void Start()
    {
        if (equipOnStart != null) Equip(equipOnStart, startLevel);
        else Debug.LogWarning("HullVisualSlot: 'Equip On Start' is empty, so no hull model is spawned. Drag the hull's HullStats asset into it.", this);
    }

    /// <summary>Call this when the player equips a hull or the hull level changes.</summary>
    public void Equip(HullStats hull, int level)
    {
        GameObject prefab = hull.GetVisualPrefab(level);
        if (prefab == null)
        {
            Debug.LogWarning($"HullVisualSlot: '{hull.name}' has no Visual Prefab for level {level}. Add an entry to its Visual Stages (From Level 1) and drag the hull prefab into it.", hull);
            return;
        }
        Equip(prefab);
    }

    public void Equip(GameObject prefab)
    {
        if (prefab == null || prefab == currentPrefab) return;   // same look: nothing to do

        Quaternion turretRot = turret ? turret.rotation : Quaternion.identity;
        if (turret) turret.SetParent(vehicleRoot, true);         // rescue the turret before the old hull goes

        if (Current != null)
        {
            Current.OnUnequip();
            Destroy(Current.gameObject);
        }

        var go = Instantiate(prefab, visualParent);
        go.transform.localPosition = Vector3.zero;
        go.transform.localScale = prefab.transform.localScale * hullScale;

        Current = go.GetComponent<HullVisual>();
        if (Current == null)
        {
            Debug.LogError($"{prefab.name} has no HullVisual component on its root.", prefab);
            return;
        }
        go.transform.localRotation = Quaternion.Euler(0f, Current.ModelYaw, 0f);   // turns a backwards-facing model round
        Current.Initialize(vehicleRoot);                                           // must run after the turn: it records the rest pose

        if (turret && Current.TurretMount)
        {
            turret.SetParent(Current.TurretMount, true);          // true = keeps the turret's world size, even if the hull model is scaled
            turret.localPosition = Vector3.zero;
            turret.rotation = turretRot;                          // keep current aim direction
            if (Current.PlaceholderTurret) Current.PlaceholderTurret.SetActive(false);
        }

        currentPrefab = prefab;
        VisualChanged?.Invoke(Current);
    }
}
