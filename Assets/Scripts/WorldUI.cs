using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

// Draws health bars and floating damage numbers over objects in the world.
// It creates its own overlay canvas the first time something needs it, so there is nothing to set up.
// Everything is drawn behind your sticks and never blocks touches.
public class WorldUI : MonoBehaviour
{
    static WorldUI instance;

    public static WorldUI Instance
    {
        get
        {
            if (instance == null)
            {
                var go = new GameObject("WorldUI", typeof(Canvas), typeof(CanvasScaler));
                instance = go.AddComponent<WorldUI>();
            }
            return instance;
        }
    }

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    static void ResetStatics() { instance = null; }

    [Header("Health bar")]
    public float barHeight = 14f;                                   // canvas units
    public Color enemyBarColor = new Color(0.90f, 0.20f, 0.20f);    // red
    public Color allyBarColor = new Color(0.25f, 0.85f, 0.35f);     // green
    public Color barBackColor = new Color(0f, 0f, 0f, 0.65f);

    [Header("Damage numbers")]
    public int fontSize = 36;
    public float lifetime = 0.9f;                                   // seconds
    public float riseDistance = 70f;                                // canvas units
    public Color damageToEnemyColor = new Color(1f, 0.92f, 0.3f);   // yellow
    public Color damageToAllyColor = new Color(1f, 0.45f, 0.25f);   // orange-red

    class Bar
    {
        public Health health;
        public RectTransform root;
        public RectTransform fill;
    }

    class Floating
    {
        public Health owner;
        public Vector3 lastPos;
        public float startOffset;
        public float xJitter;
        public float age;
        public RectTransform rect;
        public Text text;
        public Color baseColor;
    }

    readonly List<Bar> bars = new List<Bar>();
    readonly List<Floating> floats = new List<Floating>();

    Canvas canvas;
    Camera cam;
    Font font;

    void Awake()
    {
        canvas = GetComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = -10;     // behind the sticks

        // Same scaling as the stick canvas, so sizes feel consistent.
        var scaler = GetComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920f, 1080f);
        scaler.screenMatchMode = CanvasScaler.ScreenMatchMode.MatchWidthOrHeight;
        scaler.matchWidthOrHeight = 0.5f;

        font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
    }

    // ---- Health bars -----------------------------------------------------

    public void AddBar(Health h)
    {
        var root = new GameObject("HealthBar", typeof(RectTransform));
        root.transform.SetParent(transform, false);
        var rt = (RectTransform)root.transform;
        rt.sizeDelta = new Vector2(h.barWidth, barHeight);

        Image back = NewImage("Back", rt, barBackColor);
        Stretch(back.rectTransform);

        Image fillImg = NewImage("Fill", rt, h.team == Team.Enemy ? enemyBarColor : allyBarColor);
        RectTransform fr = fillImg.rectTransform;
        fr.anchorMin = Vector2.zero;
        fr.anchorMax = Vector2.one;
        fr.offsetMin = new Vector2(2f, 2f);
        fr.offsetMax = new Vector2(0f, -2f);

        bars.Add(new Bar { health = h, root = rt, fill = fr });
    }

    // ---- Damage numbers --------------------------------------------------

    public void ShowDamage(Health h, float amount)
    {
        if (h == null) return;

        var go = new GameObject("DamageText", typeof(RectTransform));
        go.transform.SetParent(transform, false);
        var rt = (RectTransform)go.transform;
        rt.sizeDelta = new Vector2(240f, 70f);

        var text = go.AddComponent<Text>();
        text.font = font;
        text.fontSize = fontSize;
        text.fontStyle = FontStyle.Bold;
        text.alignment = TextAnchor.MiddleCenter;
        text.horizontalOverflow = HorizontalWrapMode.Overflow;
        text.verticalOverflow = VerticalWrapMode.Overflow;
        text.raycastTarget = false;
        text.text = "-" + (amount >= 1f ? Mathf.RoundToInt(amount).ToString() : amount.ToString("0.#"));

        Color c = h.team == Team.Enemy ? damageToEnemyColor : damageToAllyColor;
        text.color = c;

        var outline = go.AddComponent<Outline>();
        outline.effectColor = new Color(0f, 0f, 0f, 0.85f);
        outline.effectDistance = new Vector2(2f, -2f);

        floats.Add(new Floating
        {
            owner = h,
            lastPos = h.transform.position,
            startOffset = h.barOffset + 32f,            // starts just above the health bar
            xJitter = Random.Range(-18f, 18f),          // so numbers from fast hits don't sit exactly on top of each other
            rect = rt,
            text = text,
            baseColor = c
        });
    }

    // ---- Per-frame update ------------------------------------------------

    void LateUpdate()
    {
        if (cam == null) cam = Camera.main;
        if (cam == null) return;

        float s = canvas.scaleFactor;

        for (int i = bars.Count - 1; i >= 0; i--)
        {
            Bar b = bars[i];
            if (b.health == null)
            {
                Destroy(b.root.gameObject);
                bars.RemoveAt(i);
                continue;
            }

            Vector3 sp = cam.WorldToScreenPoint(b.health.transform.position);
            bool visible = sp.z > 0f;
            b.root.gameObject.SetActive(visible);
            if (!visible) continue;

            b.root.position = new Vector3(sp.x, sp.y + b.health.barOffset * s, 0f);

            float ratio = b.health.maxHealth > 0f ? b.health.Current / b.health.maxHealth : 0f;
            b.fill.anchorMax = new Vector2(Mathf.Clamp01(ratio), 1f);
            b.fill.gameObject.SetActive(ratio > 0.03f);
        }

        for (int i = floats.Count - 1; i >= 0; i--)
        {
            Floating f = floats[i];
            f.age += Time.deltaTime;
            float t = f.age / lifetime;

            if (t >= 1f)
            {
                Destroy(f.rect.gameObject);
                floats.RemoveAt(i);
                continue;
            }

            // Follow the target while it lives; stay where it died otherwise.
            if (f.owner != null) f.lastPos = f.owner.transform.position;

            Vector3 sp = cam.WorldToScreenPoint(f.lastPos);
            f.rect.gameObject.SetActive(sp.z > 0f);

            float eased = 1f - (1f - t) * (1f - t);                 // fast start, slow finish
            float rise = riseDistance * eased;
            f.rect.position = new Vector3(sp.x + f.xJitter * s, sp.y + (f.startOffset + rise) * s, 0f);

            Color c = f.baseColor;
            c.a = t < 0.5f ? 1f : 1f - (t - 0.5f) / 0.5f;           // fade out in the second half
            f.text.color = c;
        }
    }

    // ---- Helpers ---------------------------------------------------------

    static Image NewImage(string name, RectTransform parent, Color color)
    {
        var go = new GameObject(name, typeof(RectTransform), typeof(Image));
        go.transform.SetParent(parent, false);
        var img = go.GetComponent<Image>();
        img.color = color;
        img.raycastTarget = false;
        return img;
    }

    static void Stretch(RectTransform rt)
    {
        rt.anchorMin = Vector2.zero;
        rt.anchorMax = Vector2.one;
        rt.offsetMin = Vector2.zero;
        rt.offsetMax = Vector2.zero;
    }
}
