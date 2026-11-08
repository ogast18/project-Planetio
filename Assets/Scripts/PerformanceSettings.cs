using UnityEngine;

// Sets the frame rate and shows a small FPS counter. It starts by itself, so you don't need
// to add it to any scene. Phones default to 30 FPS, which makes sticks feel laggy on their own.
public class PerformanceSettings : MonoBehaviour
{
    // Change these two lines to taste.
    const int TargetFps = 60;          // try 90 or 120 on high refresh rate phones
    const bool ShowFps = true;         // set to false when you no longer want the counter

    float smoothDelta = 1f / 60f;
    float labelTimer;
    string label = "";
    GUIStyle style;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
    static void Boot()
    {
        QualitySettings.vSyncCount = 0;
        Application.targetFrameRate = TargetFps;

        var go = new GameObject("PerformanceSettings");
        DontDestroyOnLoad(go);
        go.AddComponent<PerformanceSettings>();
    }

    void Update()
    {
        smoothDelta += (Time.unscaledDeltaTime - smoothDelta) * 0.1f;

        // Refresh the text a few times per second so it doesn't create garbage every frame.
        labelTimer -= Time.unscaledDeltaTime;
        if (labelTimer <= 0f)
        {
            labelTimer = 0.25f;
            label = (1f / Mathf.Max(0.0001f, smoothDelta)).ToString("0") + " FPS   "
                  + (smoothDelta * 1000f).ToString("0.0") + " ms";
        }
    }

    void OnGUI()
    {
        if (!ShowFps) return;

        if (style == null)
        {
            style = new GUIStyle(GUI.skin.label)
            {
                fontSize = Mathf.Max(18, Screen.height / 30),
                fontStyle = FontStyle.Bold
            };
            style.normal.textColor = Color.white;
        }

        GUI.Label(new Rect(Screen.width * 0.02f, Screen.height * 0.02f, Screen.width * 0.5f, Screen.height * 0.1f),
                  label, style);
    }
}
