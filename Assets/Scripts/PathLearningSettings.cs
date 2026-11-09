using UnityEngine;

// The numbers behind "enemies learn which routes pay off".
// Optional: create one asset named exactly "PathLearning" inside a folder called Resources
// (Assets/Resources/PathLearning.asset). Without it the defaults below are used.
// Every number is read live, so you can tune them in Play Mode.
[CreateAssetMenu(fileName = "PathLearning", menuName = "Planetio/Path Learning Settings")]
public class PathLearningSettings : ScriptableObject
{
    [Header("Learning")]
    public bool learningEnabled = true;

    [Header("Bad spots (make a spot costly to walk through)")]
    [Min(0), Tooltip("Risk added where an enemy died without having dealt damage")]
    public float deathRisk = 1f;
    [Min(0.1f), Tooltip("An enemy that dealt this many of its own attacks' worth of damage before dying is not counted as wasted")]
    public float attacksToExcuse = 2f;
    [Min(0), Tooltip("Risk added where an enemy stood still for Idle Seconds without being able to attack")]
    public float idleRisk = 0.5f;
    [Min(1)] public float idleSeconds = 4f;
    [Min(0.5f), Tooltip("How far a lesson spreads around the spot, in world units")]
    public float riskRadius = 2.5f;

    [Header("Good spots (make a spot cheaper again)")]
    [Min(0), Tooltip("Risk removed per point of damage dealt, around where the enemy attacked from")]
    public float reliefPerDamage = 0.02f;

    [Header("Memory")]
    [Min(0.5f), Tooltip("A spot can cost at most 1 + Max Risk x (the enemy's Risk Aversion) times as much to cross")]
    public float maxRisk = 3f;
    [Min(1), Tooltip("Seconds for what was learned to fade to half. Short = adapts fast, long = remembers")]
    public float halfLifeSeconds = 90f;

    public static PathLearningSettings Load()
    {
        var s = Resources.Load<PathLearningSettings>("PathLearning");
        if (s == null) s = CreateInstance<PathLearningSettings>();   // defaults
        return s;
    }
}
