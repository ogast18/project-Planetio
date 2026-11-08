using System;
using UnityEngine;

[CreateAssetMenu(fileName = "NewHull", menuName = "Planetio/Hull Stats")]
public class HullStats : ScriptableObject
{
    public const int MaxLevel = 30;

    [Header("Durability (level 1)")]
    [Min(1)] public float baseHealth = 100f;
    [Range(0f, 90f), Tooltip("Percentage of damage that is ignored")]
    public float armorPercent = 0f;

    [Header("Movement (level 1)")]
    public float maxSpeed = 6f;            // forward top speed, units per second
    public float maxReverseSpeed = 3.5f;   // reverse top speed
    public float acceleration = 14f;       // how fast it speeds up and brakes
    public float turnSpeed = 220f;         // degrees per second at full turn rate
    public float sideGrip = 8f;            // higher = less drifting

    [Header("Turning needs movement")]
    public float fullTurnSpeed = 2f;       // speed at which it turns at the full rate (0 speed = no turning)
    public float minDrive = 0.35f;         // minimum throttle when the target is not in front

    [Header("Forward / reverse choice")]
    public float reverseConeAngle = 90f;   // total width of the cone straight behind the hull that triggers reverse
    public float reverseSwitchMargin = 12f;// degrees of hysteresis at the cone edge

    [Header("Health growth")]
    [Tooltip("X = level (1-30), Y = health multiplier (1 = base health)")]
    public AnimationCurve healthMultiplier = AnimationCurve.Linear(1, 1, MaxLevel, 3);

    [Header("Milestones (cumulative)")]
    public Milestone[] milestones =
    {
        new Milestone { level = 10, speedMultiplier = 1.1f },
        new Milestone { level = 20, armorBonus = 5f },
        new Milestone { level = 30, speedMultiplier = 1.1f, turnSpeedMultiplier = 1.1f },
    };

    [Serializable]
    public class Milestone
    {
        public int level = 10;
        [Tooltip("Added to armor percent")] public float armorBonus = 0f;
        [Min(0.1f), Tooltip("Multiplies forward and reverse top speed")] public float speedMultiplier = 1f;
        [Min(0.1f)] public float accelerationMultiplier = 1f;
        [Min(0.1f)] public float turnSpeedMultiplier = 1f;
    }

    // What the vehicle actually uses at a given level
    public struct Resolved
    {
        public float maxHealth, armorPercent, maxSpeed, maxReverseSpeed, acceleration, turnSpeed,
                     sideGrip, fullTurnSpeed, minDrive, reverseConeAngle, reverseSwitchMargin;
    }

    public Resolved AtLevel(int level)
    {
        level = Mathf.Clamp(level, 1, MaxLevel);

        var r = new Resolved
        {
            maxHealth = baseHealth * healthMultiplier.Evaluate(level),
            armorPercent = armorPercent,
            maxSpeed = maxSpeed,
            maxReverseSpeed = maxReverseSpeed,
            acceleration = acceleration,
            turnSpeed = turnSpeed,
            sideGrip = sideGrip,
            fullTurnSpeed = fullTurnSpeed,
            minDrive = minDrive,
            reverseConeAngle = reverseConeAngle,
            reverseSwitchMargin = reverseSwitchMargin
        };

        foreach (var m in milestones)
        {
            if (level < m.level) continue;
            r.armorPercent += m.armorBonus;
            r.maxSpeed *= m.speedMultiplier;
            r.maxReverseSpeed *= m.speedMultiplier;
            r.acceleration *= m.accelerationMultiplier;
            r.turnSpeed *= m.turnSpeedMultiplier;
        }

        r.armorPercent = Mathf.Clamp(r.armorPercent, 0f, 90f);
        return r;
    }
}
