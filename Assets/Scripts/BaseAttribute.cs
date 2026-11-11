using UnityEngine;

// One upgrade for the base (armored walls, advanced repair system...).
// It changes the base's numbers and can bring its own visual (a prefab that appears on the base).
// Add the asset to the "Attributes" list on the PlayerBase to install it.
// Every effect is optional: 0 (or 1 for the multiplier) means "no effect".
[CreateAssetMenu(fileName = "NewBaseAttribute", menuName = "Planetio/Base Attribute")]
public class BaseAttribute : ScriptableObject
{
    [TextArea] public string description;

    [Header("Visual (optional)")]
    [Tooltip("Appears on the base while this attribute is installed, e.g. spiked walls or a crane room. Best without colliders for now")]
    public GameObject visualPrefab;

    [Header("Effects")]
    [Tooltip("Added to the base's armor percent (armored walls)")]
    public float armorBonus = 0f;
    [Min(0.1f), Tooltip("Multiplies the base's max health")]
    public float maxHealthMultiplier = 1f;
    [Tooltip("Added to the base's health restored per second during breaks (percent of max health)")]
    public float breakRegenBonus = 0f;
    [Tooltip("Added to the vehicle's repair per second during breaks (percent of its max health) (advanced repair system)")]
    public float vehicleRepairBonus = 0f;
    [Tooltip("Added to how close the vehicle must be to be repaired")]
    public float repairRangeBonus = 0f;
}
