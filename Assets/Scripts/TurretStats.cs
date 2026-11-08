using System;
using UnityEngine;

[CreateAssetMenu(fileName = "NewTurret", menuName = "Planetio/Turret Stats")]
public class TurretStats : ScriptableObject
{
    public const int MaxLevel = 30;

    [Header("Base stats (level 1)")]
    [Min(0)] public float baseDamage = 25f;
    [Min(0.1f)] public float shotsPerSecond = 1.5f;
    [Range(0f, 1f)] public float armorPenetration = 0f;
    [Min(1)] public float range = 12f;
    [Range(1f, 180f)] public float coneAngle = 30f;
    [Range(0.1f, 1f), Tooltip("Cone multiplier while the vehicle stands still")]
    public float standingConeFactor = 0.6f;
    [Min(1)] public float bulletSpeed = 25f;
    [Min(1)] public float turnSpeed = 270f;

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
    }

    // What the turret actually uses at a given level
    public struct Resolved
    {
        public float damage, shotsPerSecond, armorPenetration, range,
                     coneAngle, standingConeFactor, bulletSpeed, turnSpeed;
    }

    public Resolved AtLevel(int level)
    {
        level = Mathf.Clamp(level, 1, MaxLevel);

        var r = new Resolved
        {
            damage = baseDamage * damageMultiplier.Evaluate(level),
            shotsPerSecond = shotsPerSecond,
            armorPenetration = armorPenetration,
            range = range,
            coneAngle = coneAngle,
            standingConeFactor = standingConeFactor,
            bulletSpeed = bulletSpeed,
            turnSpeed = turnSpeed
        };

        foreach (var m in milestones)
        {
            if (level < m.level) continue;
            r.range += m.rangeBonus;
            r.coneAngle *= m.coneAngleMultiplier;
            r.shotsPerSecond *= m.fireRateMultiplier;
        }
        return r;
    }
}