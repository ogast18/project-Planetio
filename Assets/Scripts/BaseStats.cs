using System;
using UnityEngine;

// The base's numbers for each of its upgrade tiers (the demo has 3).
// Extra upgrades on top of the tier are BaseAttribute assets (see PlayerBase).
[CreateAssetMenu(fileName = "Base", menuName = "Planetio/Base Stats")]
public class BaseStats : ScriptableObject
{
    public const int MaxTier = 3;

    [Serializable]
    public class Tier
    {
        [Min(1)] public float maxHealth = 500f;
        [Range(0f, 90f), Tooltip("Percentage of damage that is ignored")]
        public float armorPercent = 0f;

        [Header("Repairs during breaks between waves")]
        [Min(0), Tooltip("Base health restored per second during a break, as a percent of its max health. 2 = 20% over a 10 s break")]
        public float breakRegenPercentPerSecond = 2f;
        [Min(0), Tooltip("Vehicle health restored per second during a break while it is near the base, as a percent of its max health")]
        public float vehicleRepairPercentPerSecond = 5f;
        [Min(0), Tooltip("How close the vehicle must be to the base centre to be repaired")]
        public float repairRange = 8f;
    }

    [Tooltip("Tier 1, 2 and 3")]
    public Tier[] tiers =
    {
        new Tier { maxHealth = 500f,  armorPercent = 0f,  breakRegenPercentPerSecond = 2f, vehicleRepairPercentPerSecond = 5f,  repairRange = 8f },
        new Tier { maxHealth = 800f,  armorPercent = 10f, breakRegenPercentPerSecond = 3f, vehicleRepairPercentPerSecond = 7f,  repairRange = 9f },
        new Tier { maxHealth = 1200f, armorPercent = 20f, breakRegenPercentPerSecond = 4f, vehicleRepairPercentPerSecond = 10f, repairRange = 10f },
    };

    // What the base actually uses (tier numbers, before attributes are added)
    public struct Resolved
    {
        public float maxHealth, armorPercent, breakRegenPercentPerSecond, vehicleRepairPercentPerSecond, repairRange;
    }

    public Resolved AtTier(int tier)
    {
        if (tiers == null || tiers.Length == 0) return default;

        Tier t = tiers[Mathf.Clamp(tier - 1, 0, tiers.Length - 1)];
        return new Resolved
        {
            maxHealth = t.maxHealth,
            armorPercent = t.armorPercent,
            breakRegenPercentPerSecond = t.breakRegenPercentPerSecond,
            vehicleRepairPercentPerSecond = t.vehicleRepairPercentPerSecond,
            repairRange = t.repairRange
        };
    }
}
