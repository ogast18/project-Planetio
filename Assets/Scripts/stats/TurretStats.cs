using System;
using UnityEngine;
using UnityEngine.Serialization;

/// <summary>How a turret deals its damage.</summary>
public enum FireMode
{
    Projectile,   // bullets: cannon, minigun, shotgun
    Beam,         // laser: a thin line that damages the first target over time
    Flame         // flamethrower: hurts everything in the cone over time and sets it burning
}

[CreateAssetMenu(fileName = "NewTurret", menuName = "Planetio/Turret Stats")]
public class TurretStats : ScriptableObject
{
    public const int MaxLevel = 30;

    [Header("Base stats (level 1)")]
    [Min(0), Tooltip("Damage per bullet (for a shotgun: per pellet). For Beam and Flame turrets: damage per SECOND")]
    public float baseDamage = 25f;
    [FormerlySerializedAs("shotsPerSecond"), Min(0.1f)] public float fireRate = 1.5f;   // shots per second
    [FormerlySerializedAs("bulletSpeed"), Min(1)] public float projectileSpeed = 18f;  // units per second
    [Min(1)] public float range = 12f;
    [Range(1f, 360f)] public float coneAngle = 30f;    // total width while moving, in degrees
    [Range(0f, 1f), Tooltip("Fraction of the target's armor that is ignored. 0 = none, 0.5 = half the armor, 1 = ignores armor completely")]
    public float armorPenetration = 0f;
    [Min(1), Tooltip("Bullets fired per shot (shotgun pellets). Each flies at its own random angle inside the cone")]
    public int pelletsPerShot = 1;
    [Tooltip("Damage multiplier along a bullet's flight. X: 0 = at the muzzle, 1 = at the end of its range. Y: 1 = full damage. Flat at 1 = no falloff; a shotgun slopes down")]
    public AnimationCurve damageFalloff = AnimationCurve.Constant(0f, 1f, 1f);

    [Header("Bullet")]
    [Tooltip("The bullet object this weapon fires: its shape, color and trail. Empty = use the one on the turret object")]
    public Projectile projectilePrefab;
    [Min(0.1f), Tooltip("Bullet size. Scales the bullet and the width it hits with. 1 = the prefab's own size")]
    public float projectileSize = 1f;
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

    [Header("Fire mode")]
    [Tooltip("Projectile = bullets (cannon, minigun, shotgun). Beam = laser. Flame = flamethrower. Beam and Flame deal damage over time")]
    public FireMode fireMode = FireMode.Projectile;
    [Min(1), Tooltip("Beam and Flame: how many times per second damage is applied. Each tick deals damage-per-second divided by this")]
    public float ticksPerSecond = 4f;

    [Header("Beam (laser)")]
    [Min(0.02f), Tooltip("How wide the beam is for hitting things, in units. Thin = precise")]
    public float beamWidth = 0.15f;

    [Header("Flame")]
    [Min(0.1f), Tooltip("Seconds on the same target to reach the full damage bonus")]
    public float rampTime = 2f;
    [Min(1f), Tooltip("Damage multiplier after Ramp Time seconds on one target. 1 = no ramp")]
    public float rampMaxMultiplier = 2.5f;
    [Min(0.1f), Tooltip("Seconds for the bonus to fade once a target leaves the flames")]
    public float rampDecayTime = 1f;
    [Range(0f, 2f), Tooltip("Burn damage per second as a fraction of the flame's own damage per second. 0 = no burn")]
    public float burnDamageFraction = 0.25f;
    [Min(0f), Tooltip("Seconds the burn keeps going after the last flame hit")]
    public float burnDuration = 3f;

    [Header("Look")]
    [Tooltip("One entry per turret design. The entry with the highest From Level that is still <= the turret level is used (e.g. 1, 10, 20, 30)")]
    public HullVisualStage[] visualStages;
    public GameObject GetVisualPrefab(int level) => HullVisualLookup.Pick(visualStages, level);

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
        [Tooltip("Extra bullets per shot (e.g. +2 pellets for a shotgun)")]
        public int pelletBonus = 0;
    }

    // What the turret actually uses at a given level
    public struct Resolved
    {
        public float damage, fireRate, projectileSpeed, range, coneAngle, armorPenetration, turnSpeed,
                     spreadAmount, stationaryConeMultiplier, stationarySpeed, settleTime, loosenTime,
                     knockback;
        public int pelletsPerShot;
        public float projectileSize;
        public AnimationCurve damageFalloff;
        public FireMode fireMode;
        public float ticksPerSecond, beamWidth, rampTime, rampMaxMultiplier, rampDecayTime,
                     burnDamageFraction, burnDuration;
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
            knockback = knockback,
            pelletsPerShot = pelletsPerShot,
            damageFalloff = damageFalloff,
            projectileSize = projectileSize,
            fireMode = fireMode,
            ticksPerSecond = ticksPerSecond,
            beamWidth = beamWidth,
            rampTime = rampTime,
            rampMaxMultiplier = rampMaxMultiplier,
            rampDecayTime = rampDecayTime,
            burnDamageFraction = burnDamageFraction,
            burnDuration = burnDuration
        };

        foreach (var m in milestones)
        {
            if (level < m.level) continue;
            r.range += m.rangeBonus;
            r.coneAngle *= m.coneAngleMultiplier;
            r.fireRate *= m.fireRateMultiplier;
            r.armorPenetration += m.armorPenetrationBonus;
            r.pelletsPerShot += m.pelletBonus;
        }

        r.armorPenetration = Mathf.Clamp01(r.armorPenetration);
        return r;
    }
}
