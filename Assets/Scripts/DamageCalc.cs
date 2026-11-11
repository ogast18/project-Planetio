using UnityEngine;

// The one place where armor turns raw damage into the damage that is actually applied.
// Want armor to work differently? Change it here and every weapon follows.
public static class DamageCalc
{
    // armorPercent: the share of damage the target ignores (0 to 90).
    // armorPenetration: the fraction of that armor the attack bypasses (0 = none, 1 = all of it).
    // Example: armor 40%, penetration 0.5 -> only 20% armor counts, so 100 damage becomes 80.
    public static float Apply(float damage, float armorPercent, float armorPenetration = 0f)
    {
        float effectiveArmor = armorPercent * (1f - Mathf.Clamp01(armorPenetration));
        return damage * (1f - effectiveArmor / 100f);
    }
}
