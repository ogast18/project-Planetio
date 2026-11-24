using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;

/// <summary>
/// Test menu: a small DEBUG button opens a panel to pick the hull, the turret and their levels while the game
/// runs. The hull and turret models swap by themselves (the visual slots watch the vehicle and the turret).
/// Put it on an empty object in the scene. By default it only shows in the Editor and Development Builds.
/// </summary>
public class DebugLoadoutMenu : MonoBehaviour
{
    [Header("What can be picked (filled in automatically in the Editor)")]
    [SerializeField] HullStats[] hulls;
    [SerializeField] TurretStats[] turrets;

    [Header("Targets (found automatically if left empty)")]
    [SerializeField] VehicleController vehicle;
    [SerializeField] TurretController turret;

    [Header("Options")]
    [Tooltip("Stops the game while the panel is open, so the sticks don't react to touches on it")]
    [SerializeField] bool pauseWhileOpen = true;
    [Tooltip("Only show in the Editor and Development Builds. Leave on so it never ships by accident")]
    [SerializeField] bool onlyInDevelopment = true;

    bool open;
    float previousTimeScale = 1f;
    Vector2 scroll;

    void Start()
    {
        if (vehicle == null) vehicle = FindFirstObjectByType<VehicleController>();
        if (turret == null) turret = FindFirstObjectByType<TurretController>();
    }

    void Update()
    {
        // F1 toggles it with a keyboard (Editor).
        var kb = Keyboard.current;
        if (kb != null && kb.f1Key.wasPressedThisFrame) SetOpen(!open);
    }

    void OnDisable()
    {
        if (open) SetOpen(false);
    }

    void SetOpen(bool value)
    {
        if (value == open) return;
        open = value;
        if (!pauseWhileOpen) return;

        if (open)
        {
            previousTimeScale = Time.timeScale;
            Time.timeScale = 0f;
        }
        else
        {
            Time.timeScale = previousTimeScale;
        }
    }

    // ---- Panel ------------------------------------------------------------------

    void OnGUI()
    {
        if (onlyInDevelopment && !Debug.isDebugBuild) return;

        // Scale the whole panel with the screen so the buttons are big enough for a phone.
        float scale = Mathf.Max(1f, Screen.height / 540f);
        GUI.matrix = Matrix4x4.Scale(new Vector3(scale, scale, 1f));
        float w = Screen.width / scale, h = Screen.height / scale;

        if (!open)
        {
            if (GUI.Button(new Rect(6f, 6f, 64f, 26f), "DEBUG")) SetOpen(true);
            return;
        }

        var panel = new Rect(6f, 6f, Mathf.Min(380f, w - 12f), h - 12f);
        GUI.Box(panel, "Loadout test");

        GUILayout.BeginArea(new Rect(panel.x + 8f, panel.y + 24f, panel.width - 16f, panel.height - 32f));
        scroll = GUILayout.BeginScrollView(scroll);

        if (vehicle != null) HullSection();
        else GUILayout.Label("No VehicleController found in the scene.");

        GUILayout.Space(10f);

        if (turret != null) TurretSection();
        else GUILayout.Label("No TurretController found in the scene.");

        GUILayout.Space(10f);
        if (GUILayout.Button("Close", GUILayout.Height(30f))) SetOpen(false);

        GUILayout.EndScrollView();
        GUILayout.EndArea();
    }

    void HullSection()
    {
        GUILayout.Label("HULL");
        if (hulls != null && hulls.Length > 0)
        {
            int current = System.Array.IndexOf(hulls, vehicle.stats);
            int picked = GUILayout.SelectionGrid(current, Names(hulls), 2, GUILayout.Height(30f * Rows(hulls.Length)));
            if (picked != current && picked >= 0) vehicle.stats = hulls[picked];
        }
        else GUILayout.Label("No hull assets listed (see the menu component).");

        vehicle.level = LevelRow(vehicle.level, HullStats.MaxLevel);

        if (vehicle.stats != null)
        {
            GameObject look = vehicle.stats.GetVisualPrefab(vehicle.level);
            GUILayout.Label($"Movement: {vehicle.stats.movementType}   Look: {(look ? look.name : "none")}");
        }
    }

    void TurretSection()
    {
        GUILayout.Label("TURRET");
        if (turrets != null && turrets.Length > 0)
        {
            int current = System.Array.IndexOf(turrets, turret.stats);
            int picked = GUILayout.SelectionGrid(current, Names(turrets), 2, GUILayout.Height(30f * Rows(turrets.Length)));
            if (picked != current && picked >= 0) turret.stats = turrets[picked];
        }
        else GUILayout.Label("No turret assets listed (see the menu component).");

        turret.level = LevelRow(turret.level, TurretStats.MaxLevel);

        if (turret.stats != null)
        {
            GameObject look = turret.stats.GetVisualPrefab(turret.level);
            GUILayout.Label($"Mode: {turret.stats.fireMode}   Look: {(look ? look.name : "none")}");
        }
    }

    // "Level 12 / 30" with -10 -1 +1 +10 buttons and a slider.
    static int LevelRow(int level, int max)
    {
        GUILayout.Label($"Level {level} / {max}");
        GUILayout.BeginHorizontal();
        if (GUILayout.Button("-10", GUILayout.Height(30f))) level -= 10;
        if (GUILayout.Button("-1", GUILayout.Height(30f))) level -= 1;
        if (GUILayout.Button("+1", GUILayout.Height(30f))) level += 1;
        if (GUILayout.Button("+10", GUILayout.Height(30f))) level += 10;
        GUILayout.EndHorizontal();
        level = Mathf.RoundToInt(GUILayout.HorizontalSlider(level, 1f, max, GUILayout.Height(24f)));
        return Mathf.Clamp(level, 1, max);
    }

    static string[] Names(Object[] assets)
    {
        var names = new string[assets.Length];
        for (int i = 0; i < assets.Length; i++) names[i] = assets[i] != null ? assets[i].name : "(empty)";
        return names;
    }

    static int Rows(int count) { return (count + 1) / 2; }

#if UNITY_EDITOR
    // Fills the lists with every hull and turret asset in the project when the component is added.
    void Reset() { CollectAssets(); }

    [ContextMenu("Find all hull and turret assets")]
    void CollectAssets()
    {
        hulls = FindAssets<HullStats>();
        turrets = FindAssets<TurretStats>();
        UnityEditor.EditorUtility.SetDirty(this);
    }

    static T[] FindAssets<T>() where T : ScriptableObject
    {
        var list = new List<T>();
        foreach (string guid in UnityEditor.AssetDatabase.FindAssets("t:" + typeof(T).Name))
        {
            var asset = UnityEditor.AssetDatabase.LoadAssetAtPath<T>(UnityEditor.AssetDatabase.GUIDToAssetPath(guid));
            if (asset != null) list.Add(asset);
        }
        return list.ToArray();
    }
#endif
}
