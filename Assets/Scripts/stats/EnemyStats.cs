using UnityEngine;

// One asset per enemy type (Warrior, Thrower, Detonator...).
// The attack type decides how the enemy fights; the fields below it are only used by the types that need them.
[CreateAssetMenu(fileName = "NewEnemy", menuName = "Planetio/Enemy Stats")]
public class EnemyStats : ScriptableObject
{
    public enum AttackType
    {
        Melee,      // walks up to the target and hits it every attackInterval
        Ranged,     // stops at a distance and shoots projectiles every attackInterval
        Kamikaze    // runs into the target, explodes (area damage) and dies
    }

    [Header("Behaviour")]
    public AttackType attackType = AttackType.Melee;

    [Header("Durability")]
    [Min(1)] public float baseHealth = 100f;
    [Range(0f, 90f), Tooltip("Percentage of damage that is ignored")]
    public float armorPercent = 0f;
    [Range(0f, 1f), Tooltip("0 = full knockback, 1 = immune to knockback")]
    public float knockbackResistance = 0f;

    [Header("Movement")]
    public float speed = 2.5f;              // units per second
    public float turnSpeed = 360f;          // degrees per second (only affects how fast it faces where it walks)

    [Range(0f, 3f), Tooltip("How strongly it avoids spots where enemies died for nothing and prefers spots where they dealt damage. 0 = ignores what was learned (detonators), 1 = normal, higher = very cautious")]
    public float riskAversion = 1f;

    [Header("Choosing a target")]
    public float chaseRange = 8f;           // the vehicle this close makes it go after the vehicle
    public float loseRange = 12f;           // ...and it gives up beyond this distance

    [Header("Attack")]
    [Min(0), Tooltip("Melee hit, projectile hit, or explosion damage")]
    public float damage = 10f;
    [Min(0.1f), Tooltip("Seconds between attacks (Melee and Ranged only)")]
    public float attackInterval = 1f;
    [Min(0), Tooltip("Gap to the target's edge when it attacks. Melee: ~1. Ranged: shooting distance, e.g. 7. Kamikaze: ~0.4 (explodes when this close)")]
    public float attackRange = 1.2f;

    [Header("Ranged only")]
    public Projectile projectilePrefab;     // a bullet prefab (can be a copy of the turret's, colored differently)
    [Min(1)] public float projectileSpeed = 8f;   // slow bullets can be dodged
    [Range(0f, 90f), Tooltip("Total width of the random spread, in degrees. 0 = always straight at the target")]
    public float projectileSpread = 5f;
    [Min(0.1f), Tooltip("Bullet size. Scales the bullet and the width it hits with. 1 = the prefab's own size")]
    public float projectileSize = 1f;

    [Header("Kamikaze only")]
    [Min(0.1f)] public float explosionRadius = 2.5f;   // everything of the other team inside this takes 'damage'

    // What the enemy actually uses (with wave / planet multipliers applied)
    public struct Resolved
    {
        public AttackType attackType;
        public float maxHealth, armorPercent, knockbackResistance, speed, turnSpeed, riskAversion, chaseRange, loseRange,
                     damage, attackInterval, attackRange, projectileSpeed, projectileSpread, projectileSize, explosionRadius;
    }

    // healthMultiplier / damageMultiplier: how much stronger this enemy is than the base asset
    // (later set by waves and planets; 1 = unchanged).
    public Resolved Get(float healthMultiplier = 1f, float damageMultiplier = 1f)
    {
        return new Resolved
        {
            attackType = attackType,
            maxHealth = baseHealth * healthMultiplier,
            armorPercent = armorPercent,
            knockbackResistance = knockbackResistance,
            speed = speed,
            turnSpeed = turnSpeed,
            riskAversion = riskAversion,
            chaseRange = chaseRange,
            loseRange = loseRange,
            damage = damage * damageMultiplier,
            attackInterval = attackInterval,
            attackRange = attackRange,
            projectileSpeed = projectileSpeed,
            projectileSpread = projectileSpread,
            projectileSize = projectileSize,
            explosionRadius = explosionRadius
        };
    }
}
