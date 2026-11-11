using System;
using UnityEngine;
using UnityEngine.Serialization;

[CreateAssetMenu(fileName = "NewTurret", menuName = "Planetio/Turret Stats")]
public class TurretStats : ScriptableObject
{
    public const int MaxLevel = 30;

    [Header("Base stats (level 1)")]
    [Min(0)] public float baseDamage = 25f;
    [FormerlySerializedAs("shotsPerSecond"), Min(0.1f)] public float fireRate = 1.5f;   // shots per second
    [FormerlySerializedAs("bulletSpeed"), Min(1)] public float projectileSpeed = 18f;  // units per second
    [Min(1)] public float range = 12f;
    [Range(1f, 360f)] public float coneAngle = 30f;    // total width while moving, in degrees
    [Range(0f, 1f), Tooltip("Fraction of the target's armor that is ignored. 0 = none, 0.5 = half the armor, 1 = ignores armor completely")]
    public float armorPenetration = 0f;
    [Min(1)] public float turnSpeed = 270f;            // degrees per second
    [Min(0), Tooltip("Push on hit: starting shove speed in units/sec. 0 = no knockback (minigun, flamethrower)")]
    public float knockback = 0f;

    [Header("Accuracy")]
    [Range(0f, 1f), Tooltip("1 = bullets can go anywhere in the cone, 0 = always straight")]
    public float spreadAmount = 1f;

    [Header("Aim improves when standing still")]
    [FormerlySerializedAs("standingConeFactor"), Range(0.1f, 1f)]
    public float stationaryConeMultiplier = 0.6f;
    public float stationarySpeed = 0.3f;   // below this speed the vehicle counts as still
    public float settleTime = 0.5f;        // seconds to fully tighten after stopping
    public float loosenTime = 0.15f;       // seconds to widen again once moving

    [Header("Damage growth")]
    [Tooltip("X = level (1-30), Y = damage multiplier (1 = base damage)")]
    public AnimationCurve damageMultiplier = AnimationCurve.Linear(1, 1, MaxLevel, 3);

    [Header("Milestones (cumulative)")]
    public Milestone[] milestones =
    {
        new Milestone { level = 10, rangeBonus = 1.5f },
        new Milestone { level = 20, coneAngleMultiplier = 0.85f },
        new Milestone { level = 30, rangeBonus = 1.5f, coneAngleMultiplier = 0.85f },
    };

    [Serializable]
    public class Milestone
    {
        public int level = 10;
        public float rangeBonus = 0f;
        [Min(0.1f)] public float coneAngleMultiplier = 1f;
        [Min(0.1f)] public float fireRateMultiplier = 1f;
        [Tooltip("Added to armor penetration (0.1 = ignores another 10% of the target's armor)")]
        public float armorPenetrationBonus = 0f;
    }

    // What the turret actually uses at a given level
    public struct Resolved
    {
        public float damage, fireRate, projectileSpeed, range, coneAngle, armorPenetration, turnSpeed,
                     spreadAmount, stationaryConeMultiplier, stationarySpeed, settleTime, loosenTime,
                     knockback;
    }

    public Resolved AtLevel(int level)
    {
        level = Mathf.Clamp(level, 1, MaxLevel);

        var r = new Resolved
        {
            damage = baseDamage * damageMultiplier.Evaluate(level),
            fireRate = fireRate,
            projectileSpeed = projectileSpeed,
            range = range,
            coneAngle = coneAngle,
            armorPenetration = armorPenetration,
            turnSpeed = turnSpeed,
            spreadAmount = spreadAmount,
            stationaryConeMultiplier = stationaryConeMultiplier,
            stationarySpeed = stationarySpeed,
            settleTime = settleTime,
            loosenTime = loosenTime,
            knockback = knockback
        };

        foreach (var m in milestones)
        {
            if (level < m.level) continue;
            r.range += m.rangeBonus;
            r.coneAngle *= m.coneAngleMultiplier;
            r.fireRate *= m.fireRateMultiplier;
            r.armorPenetration += m.armorPenetrationBonus;
        }

        r.armorPenetration = Mathf.Clamp01(r.armorPenetration);
        return r;
    }
}
