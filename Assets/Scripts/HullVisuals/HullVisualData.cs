using UnityEngine;

public enum HullMovementType { Wheeled, Tracked, Flying }

/// <summary>One look of a hull. The design with the highest fromLevel that is
/// still <= the hull level is used (e.g. 1, 10, 20, 30 for the milestone designs).</summary>
[System.Serializable]
public struct HullVisualStage
{
    [Min(1)] public int fromLevel;
    public GameObject visualPrefab;      // prefab whose root has a HullVisual component
}

public static class HullVisualLookup
{
    /// <summary>Stages may be in any order.</summary>
    public static GameObject Pick(HullVisualStage[] stages, int level)
    {
        if (stages == null || stages.Length == 0) return null;
        GameObject best = null;
        int bestFrom = int.MinValue;
        foreach (var s in stages)
        {
            if (s.visualPrefab != null && s.fromLevel <= level && s.fromLevel > bestFrom)
            {
                best = s.visualPrefab;
                bestFrom = s.fromLevel;
            }
        }
        // level lower than every stage: fall back to the lowest one
        if (best == null)
        {
            int lowest = int.MaxValue;
            foreach (var s in stages)
                if (s.visualPrefab != null && s.fromLevel < lowest) { best = s.visualPrefab; lowest = s.fromLevel; }
        }
        return best;
    }
}

/* ---- ADD THESE TO YOUR EXISTING HullStats ScriptableObject ----

    [Header("Look & movement")]
    public HullMovementType movementType;
    public HullVisualStage[] visualStages;
    public GameObject GetVisualPrefab(int level) => HullVisualLookup.Pick(visualStages, level);

-------------------------------------------------------------------*/
