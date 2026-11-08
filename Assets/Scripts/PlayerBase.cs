using UnityEngine;

// Marks the object as THE base that enemies walk toward.
// Put this on the Base object. It uses the Health component for hit points and its health bar.
[RequireComponent(typeof(Health))]
public class PlayerBase : MonoBehaviour
{
    public static PlayerBase Instance { get; private set; }
    public Health Health { get; private set; }

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    static void ResetStatics() { Instance = null; }

    void Awake()
    {
        Instance = this;
        Health = GetComponent<Health>();

        // The base is always on the player's side: green bar, and the turret never targets it.
        Health.team = Team.Player;
    }

    void OnDestroy()
    {
        if (Instance == this) Instance = null;
    }
}
