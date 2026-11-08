using UnityEngine;

// Builds a simple 3D test arena from colored boxes when the game starts:
// a ground, a faint grid (so you can SEE movement) and a solid border with colliders.
// Put this on an empty object called "Map" at position (0, 0, 0).
public class MapBuilder : MonoBehaviour
{
    [Header("Size (world units, X by Z)")]
    public Vector2 size = new Vector2(60f, 40f);
    public float gridSpacing = 4f;
    public float borderThickness = 1.5f;
    public float wallHeight = 1.5f;

    [Header("Colors")]
    public Color groundColor = new Color(0.16f, 0.18f, 0.22f);
    public Color gridColor = new Color(0.23f, 0.26f, 0.31f);
    public Color borderColor = new Color(0.85f, 0.35f, 0.25f);

    public Vector2 Size => size;

    Material groundMat, gridMat, borderMat;

    void Awake()
    {
        // Borrow the project's default material so this works in URP and Built-in alike.
        var temp = GameObject.CreatePrimitive(PrimitiveType.Cube);
        Material baseMat = temp.GetComponent<Renderer>().sharedMaterial;
        Destroy(temp);

        groundMat = MakeMat(baseMat, groundColor);
        gridMat = MakeMat(baseMat, gridColor);
        borderMat = MakeMat(baseMat, borderColor);

        BuildGround();
        BuildGrid();
        BuildBorder();
    }

    void BuildGround()
    {
        // Top surface sits at y = 0.
        MakeBox("Ground", new Vector3(0f, -0.1f, 0f), new Vector3(size.x, 0.2f, size.y), groundMat, true);
    }

    void BuildGrid()
    {
        float halfW = size.x * 0.5f;
        float halfH = size.y * 0.5f;
        const float lineWidth = 0.06f;
        const float lineHeight = 0.02f;

        for (float x = -halfW; x <= halfW + 0.01f; x += gridSpacing)
            MakeBox("GridLineZ", new Vector3(x, lineHeight * 0.5f, 0f),
                    new Vector3(lineWidth, lineHeight, size.y), gridMat, false);

        for (float z = -halfH; z <= halfH + 0.01f; z += gridSpacing)
            MakeBox("GridLineX", new Vector3(0f, lineHeight * 0.5f, z),
                    new Vector3(size.x, lineHeight, lineWidth), gridMat, false);
    }

    void BuildBorder()
    {
        float t = borderThickness;
        float h = wallHeight;
        float halfW = size.x * 0.5f;
        float halfH = size.y * 0.5f;

        // The front and back walls are longer so the corners are covered.
        MakeBox("WallFront", new Vector3(0f, h * 0.5f,  halfH + t * 0.5f), new Vector3(size.x + 2f * t, h, t), borderMat, true);
        MakeBox("WallBack",  new Vector3(0f, h * 0.5f, -halfH - t * 0.5f), new Vector3(size.x + 2f * t, h, t), borderMat, true);
        MakeBox("WallLeft",  new Vector3(-halfW - t * 0.5f, h * 0.5f, 0f), new Vector3(t, h, size.y), borderMat, true);
        MakeBox("WallRight", new Vector3( halfW + t * 0.5f, h * 0.5f, 0f), new Vector3(t, h, size.y), borderMat, true);
    }

    Material MakeMat(Material baseMat, Color color)
    {
        var m = new Material(baseMat);
        m.color = color;
        if (m.HasProperty("_Smoothness")) m.SetFloat("_Smoothness", 0.1f);  // URP
        if (m.HasProperty("_Glossiness")) m.SetFloat("_Glossiness", 0.1f);  // Built-in
        return m;
    }

    GameObject MakeBox(string name, Vector3 pos, Vector3 scale, Material mat, bool keepCollider)
    {
        var go = GameObject.CreatePrimitive(PrimitiveType.Cube);
        go.name = name;
        go.transform.SetParent(transform, false);
        go.transform.localPosition = pos;
        go.transform.localScale = scale;
        var r = go.GetComponent<Renderer>();
        r.sharedMaterial = mat;
        if (!keepCollider)
        {
            // Grid lines are only decoration: no collider, and no cost in the shadow pass.
            Destroy(go.GetComponent<Collider>());
            r.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
        }
        return go;
    }
}
