using System.Collections.Generic;
using UnityEngine;

// Marks the object as THE base that enemies walk toward.
// Put this on the Base object. It uses the Health component for hit points and its health bar.
// Its numbers come from a BaseStats asset (the tier) plus any installed BaseAttribute upgrades.
// During the breaks between waves it repairs itself, and the vehicle when it is parked close by.
[RequireComponent(typeof(Health))]
public class PlayerBase : MonoBehaviour
{
    public static PlayerBase Instance { get; private set; }
    public Health Health { get; private set; }

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    static void ResetStatics() { Instance = null; }

    [Header("Stats")]
    public BaseStats stats;                 // drag the base's stats asset here
    [Range(1, BaseStats.MaxTier)] public int tier = 1;

    [Header("Attributes (upgrades)")]
    [Tooltip("Installed upgrades, each one once. Their effects are added to the tier's numbers")]
    public List<BaseAttribute> attributes = new List<BaseAttribute>();
    [Tooltip("Where attribute visuals appear. Empty = the base itself")]
    public Transform visualAnchor;

    // The numbers in use right now: tier + attributes.
    public BaseStats.Resolved Current { get; private set; }

    Health vehicleHealth;
    float nextVehicleSearch;
    readonly Dictionary<BaseAttribute, GameObject> visuals = new Dictionary<BaseAttribute, GameObject>();
    readonly List<BaseAttribute> removeTemp = new List<BaseAttribute>();

    void Awake()
    {
        Instance = this;
        Health = GetComponent<Health>();

        // The base is always on the player's side: green bar, and the turret never targets it.
        Health.team = Team.Player;

        if (stats == null)
        {
            Debug.LogError("PlayerBase has no BaseStats assigned, so it keeps the Health component's own numbers and does not repair.", this);
            return;
        }

        Current = Resolve();
        Health.Configure(Current.maxHealth, Current.armorPercent);

        // Built here, before the pathfinding grid is measured, so visuals with colliders count as obstacles.
        SyncVisuals();
    }

    void OnDestroy()
    {
        if (Instance == this) Instance = null;
    }

    void Update()
    {
        if (stats == null) return;

        // Every frame, so changing the tier, an attribute or the asset in Play Mode is live.
        Current = Resolve();
        Health.Configure(Current.maxHealth, Current.armorPercent);
        SyncVisuals();

        if (WaveManager.Instance != null && WaveManager.Instance.IsBreak)
            RepairDuringBreak(Time.deltaTime);
    }

    // The tier's numbers with every installed attribute added on top.
    BaseStats.Resolved Resolve()
    {
        BaseStats.Resolved r = stats.AtTier(tier);

        foreach (BaseAttribute a in attributes)
        {
            if (a == null) continue;
            r.maxHealth *= a.maxHealthMultiplier;
            r.armorPercent += a.armorBonus;
            r.breakRegenPercentPerSecond += a.breakRegenBonus;
            r.vehicleRepairPercentPerSecond += a.vehicleRepairBonus;
            r.repairRange += a.repairRangeBonus;
        }

        r.armorPercent = Mathf.Clamp(r.armorPercent, 0f, 90f);
        return r;
    }

    // ---- Repairs -------------------------------------------------------------

    void RepairDuringBreak(float dt)
    {
        // The base itself.
        if (Current.breakRegenPercentPerSecond > 0f)
            Health.Heal(Health.maxHealth * Current.breakRegenPercentPerSecond * 0.01f * dt);

        // The vehicle, if it is close enough.
        if (Current.vehicleRepairPercentPerSecond <= 0f) return;

        if (vehicleHealth == null && Time.time >= nextVehicleSearch)
        {
            nextVehicleSearch = Time.time + 1f;
            VehicleController v = FindFirstObjectByType<VehicleController>();
            vehicleHealth = v != null ? v.GetComponent<Health>() : null;
        }
        if (vehicleHealth == null || !vehicleHealth.IsAlive) return;

        Vector3 d = vehicleHealth.transform.position - transform.position;
        d.y = 0f;
        if (d.magnitude <= Current.repairRange)
            vehicleHealth.Heal(vehicleHealth.maxHealth * Current.vehicleRepairPercentPerSecond * 0.01f * dt);
    }

    // ---- Attribute visuals -----------------------------------------------------

    // Shows the visual of every installed attribute and removes the ones that were taken away.
    void SyncVisuals()
    {
        Transform parent = visualAnchor != null ? visualAnchor : transform;

        foreach (BaseAttribute a in attributes)
        {
            if (a == null || a.visualPrefab == null || visuals.ContainsKey(a)) continue;
            visuals[a] = Instantiate(a.visualPrefab, parent);
        }

        removeTemp.Clear();
        foreach (var pair in visuals)
            if (!attributes.Contains(pair.Key)) removeTemp.Add(pair.Key);

        foreach (BaseAttribute a in removeTemp)
        {
            Destroy(visuals[a]);
            visuals.Remove(a);
        }
    }
}
