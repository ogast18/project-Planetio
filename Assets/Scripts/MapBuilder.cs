using UnityEngine;

// Builds a simple test arena from colored rectangles when the game starts:
// a ground, a faint grid (so you can SEE movement) and a solid border with colliders.
// Put this on an empty object called "Map" at position (0, 0, 0).
public class MapBuilder : MonoBehaviour
{
    [Header("Size (world units)")]
    public Vector2 size = new Vector2(60f, 40f);
    public float gridSpacing = 4f;
    public float borderThickness = 1.5f;

    [Header("Colors")]
    public Color groundColor = new Color(0.16f, 0.18f, 0.22f);
    public Color gridColor = new Color(1f, 1f, 1f, 0.07f);
    public Color borderColor = new Color(0.85f, 0.35f, 0.25f);

    public Vector2 Size => size;

    Sprite square;

    void Awake()
    {
        // A 1x1 white pixel sprite that is exactly 1 world unit wide, tinted per object.
        var tex = new Texture2D(1, 1);
        tex.SetPixel(0, 0, Color.white);
        tex.Apply();
        square = Sprite.Create(tex, new Rect(0, 0, 1, 1), new Vector2(0.5f, 0.5f), 1f);

        BuildGround();
        BuildGrid();
        BuildBorder();
    }

    void BuildGround()
    {
        MakeRect("Ground", Vector2.zero, size, groundColor, -10);
    }

    void BuildGrid()
    {
        float halfW = size.x * 0.5f;
        float halfH = size.y * 0.5f;
        const float lineWidth = 0.06f;

        for (float x = -halfW; x <= halfW + 0.01f; x += gridSpacing)
            MakeRect("GridV", new Vector2(x, 0f), new Vector2(lineWidth, size.y), gridColor, -9);

        for (float y = -halfH; y <= halfH + 0.01f; y += gridSpacing)
            MakeRect("GridH", new Vector2(0f, y), new Vector2(size.x, lineWidth), gridColor, -9);
    }

    void BuildBorder()
    {
        float t = borderThickness;
        float halfW = size.x * 0.5f;
        float halfH = size.y * 0.5f;

        // Top and bottom are wider so the corners are covered.
        MakeWall("WallTop",    new Vector2(0f,  halfH + t * 0.5f), new Vector2(size.x + 2f * t, t));
        MakeWall("WallBottom", new Vector2(0f, -halfH - t * 0.5f), new Vector2(size.x + 2f * t, t));
        MakeWall("WallLeft",   new Vector2(-halfW - t * 0.5f, 0f), new Vector2(t, size.y));
        MakeWall("WallRight",  new Vector2( halfW + t * 0.5f, 0f), new Vector2(t, size.y));
    }

    void MakeWall(string name, Vector2 pos, Vector2 scale)
    {
        GameObject wall = MakeRect(name, pos, scale, borderColor, -8);
        // Collider size 1x1 gets stretched by the object's scale, so it matches the shape.
        wall.AddComponent<BoxCollider2D>().size = Vector2.one;
    }

    GameObject MakeRect(string name, Vector2 pos, Vector2 scale, Color color, int order)
    {
        var go = new GameObject(name);
        go.transform.SetParent(transform, false);
        go.transform.localPosition = pos;
        go.transform.localScale = new Vector3(scale.x, scale.y, 1f);

        var sr = go.AddComponent<SpriteRenderer>();
        sr.sprite = square;
        sr.color = color;
        sr.sortingOrder = order;
        return go;
    }
}
