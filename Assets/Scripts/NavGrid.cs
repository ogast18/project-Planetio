using System.Collections.Generic;
using UnityEngine;

// Simple pathfinding for walking enemies. It lays a grid over the map, marks the cells that
// walls and obstacles block (grown a little so enemies keep clear of them), and finds routes
// around them with A*. It creates itself the first time an enemy asks for a route, so there is
// nothing to add to the scene. Select the "NavGrid" object during Play mode to see blocked cells.
// Assumes walls and obstacles don't move (they are measured once, when the grid is built).
public class NavGrid : MonoBehaviour
{
    static NavGrid instance;

    public static NavGrid Instance
    {
        get
        {
            if (instance == null)
            {
                var go = new GameObject("NavGrid");
                instance = go.AddComponent<NavGrid>();   // Awake builds the grid
            }
            return instance;
        }
    }

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    static void ResetStatics() { instance = null; }

    [Header("Grid (tune these)")]
    public float cellSize = 0.2f;          // smaller = finer routes, more cells to search
    public float agentRadius = 0.5f;     // obstacles are grown by this much so enemies keep clear of walls
    public float checkBottom = 0.3f;     // height band above the ground that counts as "in the way"
    public float checkTop = 1.0f;
    public bool drawGizmos = true;

    int width, height;
    Vector3 origin;                      // world position of the grid's minimum corner
    bool[] blocked;

    // A* working memory, reused for every search so it creates no garbage.
    float[] gCost;
    int[] parent, seenStamp, closedStamp;
    int stamp;
    int[] heapNode = new int[256];
    float[] heapF = new float[256];
    int heapCount;

    readonly Collider[] overlapBuffer = new Collider[16];
    readonly List<Vector3> smoothTemp = new List<Vector3>();

    static readonly int[] stepX = { 1, -1, 0, 0, 1, 1, -1, -1 };
    static readonly int[] stepZ = { 0, 0, 1, -1, 1, -1, 1, -1 };

    // ---- Building the grid -------------------------------------------------

    void Awake()
    {
        var map = FindFirstObjectByType<MapBuilder>();
        if (map == null)
        {
            Debug.LogWarning("NavGrid: no MapBuilder found, so enemies will walk in straight lines.");
            return;
        }

        Vector2 size = map.Size;
        Vector3 c = map.transform.position;
        width = Mathf.CeilToInt(size.x / cellSize);
        height = Mathf.CeilToInt(size.y / cellSize);
        origin = new Vector3(c.x - width * cellSize * 0.5f, 0f, c.z - height * cellSize * 0.5f);

        int count = width * height;
        blocked = new bool[count];
        gCost = new float[count];
        parent = new int[count];
        seenStamp = new int[count];
        closedStamp = new int[count];

        Physics.SyncTransforms();   // make sure freshly created walls are known to the physics system

        float half = cellSize * 0.5f + agentRadius;
        Vector3 halfExtents = new Vector3(half, (checkTop - checkBottom) * 0.5f, half);
        float centerY = (checkTop + checkBottom) * 0.5f;

        for (int z = 0; z < height; z++)
        {
            for (int x = 0; x < width; x++)
            {
                Vector3 center = CellCenter(x, z);
                center.y = centerY;
                blocked[z * width + x] = IsSolid(center, halfExtents);
            }
        }
    }

    // Anything that is fixed in place (no Rigidbody) and solid blocks the cell. Moving things
    // (the vehicle, other enemies) don't, they are handled while walking.
    bool IsSolid(Vector3 center, Vector3 halfExtents)
    {
        int n = Physics.OverlapBoxNonAlloc(center, halfExtents, overlapBuffer, Quaternion.identity,
                                           ~0, QueryTriggerInteraction.Ignore);
        for (int i = 0; i < n; i++)
            if (overlapBuffer[i].attachedRigidbody == null) return true;
        return false;
    }

    // ---- Helpers -------------------------------------------------------------

    Vector3 CellCenter(int x, int z)
    {
        return new Vector3(origin.x + (x + 0.5f) * cellSize, 0f, origin.z + (z + 0.5f) * cellSize);
    }

    int NodeAt(Vector3 p)
    {
        int x = Mathf.Clamp(Mathf.FloorToInt((p.x - origin.x) / cellSize), 0, width - 1);
        int z = Mathf.Clamp(Mathf.FloorToInt((p.z - origin.z) / cellSize), 0, height - 1);
        return z * width + x;
    }

    static float Dist(Vector3 a, Vector3 b)
    {
        float ox = a.x - b.x, oz = a.z - b.z;
        return Mathf.Sqrt(ox * ox + oz * oz);
    }

    // The closest free cell to a position (used when an enemy is brushing a wall and stands in a blocked cell).
    int NearestFree(int node, Vector3 p)
    {
        if (!blocked[node]) return node;

        int cx = node % width, cz = node / width;
        int best = -1;
        float bestD = float.MaxValue;

        for (int dz = -5; dz <= 5; dz++)
        {
            for (int dx = -5; dx <= 5; dx++)
            {
                int x = cx + dx, z = cz + dz;
                if (x < 0 || z < 0 || x >= width || z >= height) continue;
                if (blocked[z * width + x]) continue;

                float d = Dist(CellCenter(x, z), p);
                if (d < bestD) { bestD = d; best = z * width + x; }
            }
        }
        return best;
    }

    // True if a straight line from a to b only crosses free cells. Cells within endIgnore of b are not
    // checked, because the target itself may be standing in a blocked cell.
    bool LineClear(Vector3 a, Vector3 b, float endIgnore = 0f)
    {
        float fx = b.x - a.x, fz = b.z - a.z;
        float len = Mathf.Sqrt(fx * fx + fz * fz);
        if (len < 0.001f) return true;

        int steps = Mathf.CeilToInt(len / (cellSize * 0.4f));
        int startNode = NodeAt(a);

        for (int i = 1; i <= steps; i++)
        {
            float t = i / (float)steps;
            if (endIgnore > 0f && len * (1f - t) < endIgnore) break;

            int node = NodeAt(new Vector3(a.x + fx * t, 0f, a.z + fz * t));
            if (node != startNode && blocked[node]) return false;
        }
        return true;
    }

    // ---- Finding a route -----------------------------------------------------

    // Fills 'result' with waypoints leading from 'from' to within arriveRadius of 'to'.
    // needLineOfSight: also require a clear view of the target at the end (used for chasing the tank).
    // Returns false if there is no grid (the caller should then just walk straight).
    public bool FindPath(Vector3 from, Vector3 to, float arriveRadius, bool needLineOfSight, List<Vector3> result)
    {
        result.Clear();
        if (blocked == null) return false;

        int start = NearestFree(NodeAt(from), from);
        if (start < 0) return false;

        stamp++;
        heapCount = 0;
        gCost[start] = 0f;
        parent[start] = -1;
        seenStamp[start] = stamp;
        Push(start, Dist(CellCenter(start % width, start / width), to));

        int best = start;
        float bestH = float.MaxValue;

        while (heapCount > 0)
        {
            int cur = Pop();
            if (closedStamp[cur] == stamp) continue;
            closedStamp[cur] = stamp;

            int cx = cur % width, cz = cur / width;
            Vector3 cc = CellCenter(cx, cz);
            float h = Dist(cc, to);

            // Remember the closest we ever got, in case the target can't be reached.
            if (h < bestH) { bestH = h; best = cur; }

            if (h <= arriveRadius && (!needLineOfSight || LineClear(cc, to, 1.2f)))
            {
                best = cur;
                break;
            }

            for (int d = 0; d < 8; d++)
            {
                int nx = cx + stepX[d], nz = cz + stepZ[d];
                if (nx < 0 || nz < 0 || nx >= width || nz >= height) continue;

                int nb = nz * width + nx;
                if (blocked[nb]) continue;

                // No cutting diagonally across the corner of a wall.
                bool diagonal = d >= 4;
                if (diagonal && (blocked[cz * width + nx] || blocked[nz * width + cx])) continue;

                float ng = gCost[cur] + (diagonal ? 1.4142f : 1f) * cellSize;
                if (seenStamp[nb] != stamp || ng < gCost[nb])
                {
                    seenStamp[nb] = stamp;
                    gCost[nb] = ng;
                    parent[nb] = cur;
                    Push(nb, ng + Dist(CellCenter(nx, nz), to));
                }
            }
        }

        // Walk back from the best cell to the start.
        for (int n = best; n >= 0; n = parent[n])
            result.Add(CellCenter(n % width, n / width));
        result.Reverse();

        if (result.Count > 1) result.RemoveAt(0);   // we are already standing in the first cell
        Smooth(from, result);
        return true;
    }

    // Removes unnecessary waypoints so enemies cut straight across open ground instead of following the grid.
    void Smooth(Vector3 from, List<Vector3> path)
    {
        if (path.Count < 2) return;

        smoothTemp.Clear();
        Vector3 anchor = from;
        int i = 0;

        while (i < path.Count)
        {
            int j = path.Count - 1;
            while (j > i && !LineClear(anchor, path[j])) j--;

            smoothTemp.Add(path[j]);
            anchor = path[j];
            i = j + 1;
        }

        path.Clear();
        path.AddRange(smoothTemp);
    }

    // ---- Binary heap (smallest f first) --------------------------------------

    void Push(int node, float f)
    {
        if (heapCount == heapNode.Length)
        {
            System.Array.Resize(ref heapNode, heapCount * 2);
            System.Array.Resize(ref heapF, heapCount * 2);
        }

        int i = heapCount++;
        while (i > 0)
        {
            int p = (i - 1) / 2;
            if (heapF[p] <= f) break;
            heapNode[i] = heapNode[p];
            heapF[i] = heapF[p];
            i = p;
        }
        heapNode[i] = node;
        heapF[i] = f;
    }

    int Pop()
    {
        int top = heapNode[0];
        heapCount--;

        if (heapCount > 0)
        {
            int node = heapNode[heapCount];
            float f = heapF[heapCount];
            int i = 0;

            while (true)
            {
                int l = 2 * i + 1;
                if (l >= heapCount) break;
                int r = l + 1;
                int c = (r < heapCount && heapF[r] < heapF[l]) ? r : l;
                if (heapF[c] >= f) break;
                heapNode[i] = heapNode[c];
                heapF[i] = heapF[c];
                i = c;
            }
            heapNode[i] = node;
            heapF[i] = f;
        }
        return top;
    }

    // ---- Debug view ----------------------------------------------------------

    void OnDrawGizmosSelected()
    {
        if (!drawGizmos || blocked == null) return;

        Gizmos.color = new Color(1f, 0.2f, 0.2f, 0.35f);
        Vector3 size = new Vector3(cellSize * 0.9f, 0.1f, cellSize * 0.9f);

        for (int z = 0; z < height; z++)
            for (int x = 0; x < width; x++)
                if (blocked[z * width + x])
                    Gizmos.DrawCube(CellCenter(x, z) + Vector3.up * 0.05f, size);
    }
}
