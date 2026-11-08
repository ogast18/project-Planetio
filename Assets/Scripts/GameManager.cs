using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

// The game loop: shows how many enemies are left, decides when the level is won or lost,
// and offers a Restart button on the end screen.
//   Lost: the base is destroyed (or the vehicle, if Vehicle Death Loses is on).
//   Won:  every enemy has been spawned and defeated (needs Total Enemies above 0 on the spawner).
// Put this on an empty object called "GameManager". It builds its own UI.
public class GameManager : MonoBehaviour
{
    public enum State { Playing, Won, Lost }

    [Header("Rules (tune these)")]
    public bool vehicleDeathLoses = true;
    public float endScreenDelay = 1f;      // seconds you can still see the last hit before the end screen

    [Header("Texts")]
    public string winTitle = "VICTORY";
    public string winText = "All enemies defeated";
    public string loseTitle = "DEFEAT";
    public string loseBaseText = "The base was destroyed";
    public string loseVehicleText = "Your tank was destroyed";

    public State Current { get; private set; } = State.Playing;

    Health vehicleHealth;
    bool hadBase, hadVehicle;

    Text hudText;
    GameObject endScreen;
    Text endTitle, endSubtitle;
    int lastRemaining = -2;
    Font font;

    void Start()
    {
        Time.timeScale = 1f;

        hadBase = PlayerBase.Instance != null;

        var vehicle = FindFirstObjectByType<VehicleController>();
        vehicleHealth = vehicle != null ? vehicle.GetComponent<Health>() : null;
        hadVehicle = vehicleHealth != null;

        font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        BuildUI();
    }

    void Update()
    {
        if (Current != State.Playing) return;

        // Lost?
        if (hadBase && (PlayerBase.Instance == null || !PlayerBase.Instance.Health.IsAlive))
        {
            End(State.Lost, loseTitle, loseBaseText);
            return;
        }
        if (vehicleDeathLoses && hadVehicle && (vehicleHealth == null || !vehicleHealth.IsAlive))
        {
            End(State.Lost, loseTitle, loseVehicleText);
            return;
        }

        // Count what is left, and check for victory.
        int remaining = 0;
        bool anyFinite = false, anyEndless = false, allFinished = true;

        foreach (EnemySpawner s in EnemySpawner.All)
        {
            if (s.totalEnemies <= 0) { anyEndless = true; continue; }
            anyFinite = true;
            remaining += s.Remaining;
            if (!s.IsFinished) allFinished = false;
        }

        // Only rewrite the HUD text when the number changes.
        int shown = (anyFinite && !anyEndless) ? remaining : -1;
        if (shown != lastRemaining)
        {
            lastRemaining = shown;
            hudText.text = shown >= 0 ? "Enemies left: " + shown : "";
        }

        if (anyFinite && !anyEndless && allFinished)
            End(State.Won, winTitle, winText);
    }

    void End(State result, string title, string subtitle)
    {
        Current = result;
        StartCoroutine(ShowEndScreen(title, subtitle));
    }

    IEnumerator ShowEndScreen(string title, string subtitle)
    {
        // Let the game run for a moment so you can see what happened.
        yield return new WaitForSecondsRealtime(endScreenDelay);

        endTitle.text = title;
        endSubtitle.text = subtitle;
        endTitle.color = Current == State.Won ? new Color(0.45f, 1f, 0.55f) : new Color(1f, 0.4f, 0.35f);
        endScreen.SetActive(true);
        Time.timeScale = 0f;     // freeze the game behind the screen
    }

    public void Restart()
    {
        Time.timeScale = 1f;
        SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex);
    }

    void OnDestroy()
    {
        // Never leave the game frozen behind us.
        Time.timeScale = 1f;
    }

    // ---- UI (built in code, nothing to set up) --------------------------------------

    void BuildUI()
    {
        var canvasGo = new GameObject("GameUI", typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
        canvasGo.transform.SetParent(transform, false);

        var canvas = canvasGo.GetComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 100;                   // above the sticks, so the end screen blocks them

        var scaler = canvasGo.GetComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920f, 1080f);
        scaler.screenMatchMode = CanvasScaler.ScreenMatchMode.MatchWidthOrHeight;
        scaler.matchWidthOrHeight = 0.5f;

        // Enemies-left counter at the top centre.
        hudText = NewText("Hud", canvasGo.transform, "", 44, TextAnchor.UpperCenter);
        var hudRt = hudText.rectTransform;
        hudRt.anchorMin = hudRt.anchorMax = hudRt.pivot = new Vector2(0.5f, 1f);
        hudRt.anchoredPosition = new Vector2(0f, -28f);
        hudRt.sizeDelta = new Vector2(800f, 70f);

        // End screen: a dark full-screen panel (it also catches touches so the sticks can't be used behind it).
        endScreen = new GameObject("EndScreen", typeof(RectTransform), typeof(Image));
        endScreen.transform.SetParent(canvasGo.transform, false);
        var panelRt = (RectTransform)endScreen.transform;
        panelRt.anchorMin = Vector2.zero;
        panelRt.anchorMax = Vector2.one;
        panelRt.offsetMin = panelRt.offsetMax = Vector2.zero;
        endScreen.GetComponent<Image>().color = new Color(0f, 0f, 0f, 0.7f);

        endTitle = NewText("Title", endScreen.transform, "", 120, TextAnchor.MiddleCenter);
        SetCentered(endTitle.rectTransform, new Vector2(0f, 140f), new Vector2(1400f, 170f));

        endSubtitle = NewText("Subtitle", endScreen.transform, "", 52, TextAnchor.MiddleCenter);
        SetCentered(endSubtitle.rectTransform, new Vector2(0f, 20f), new Vector2(1400f, 90f));

        // Restart button.
        var btnGo = new GameObject("RestartButton", typeof(RectTransform), typeof(Image), typeof(Button));
        btnGo.transform.SetParent(endScreen.transform, false);
        SetCentered((RectTransform)btnGo.transform, new Vector2(0f, -140f), new Vector2(420f, 120f));
        var btnImage = btnGo.GetComponent<Image>();
        btnImage.color = new Color(0.2f, 0.65f, 0.35f);
        var button = btnGo.GetComponent<Button>();
        button.targetGraphic = btnImage;
        button.onClick.AddListener(Restart);

        Text label = NewText("Label", btnGo.transform, "RESTART", 56, TextAnchor.MiddleCenter);
        var labelRt = label.rectTransform;
        labelRt.anchorMin = Vector2.zero;
        labelRt.anchorMax = Vector2.one;
        labelRt.offsetMin = labelRt.offsetMax = Vector2.zero;

        endScreen.SetActive(false);
    }

    Text NewText(string name, Transform parent, string content, int size, TextAnchor anchor)
    {
        var go = new GameObject(name, typeof(RectTransform), typeof(Text));
        go.transform.SetParent(parent, false);

        var t = go.GetComponent<Text>();
        t.font = font;
        t.fontSize = size;
        t.fontStyle = FontStyle.Bold;
        t.alignment = anchor;
        t.horizontalOverflow = HorizontalWrapMode.Overflow;
        t.verticalOverflow = VerticalWrapMode.Overflow;
        t.color = Color.white;
        t.text = content;
        t.raycastTarget = false;
        return t;
    }

    static void SetCentered(RectTransform rt, Vector2 position, Vector2 size)
    {
        rt.anchorMin = rt.anchorMax = rt.pivot = new Vector2(0.5f, 0.5f);
        rt.anchoredPosition = position;
        rt.sizeDelta = size;
    }
}
