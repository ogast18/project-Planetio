using UnityEngine;

// Smooth top-down camera that follows the vehicle and (optionally) stays inside the map.
// Put this on the Main Camera.
[RequireComponent(typeof(Camera))]
public class CameraFollow : MonoBehaviour
{
    [Header("References")]
    public Transform target;           // drag the Vehicle here
    public MapBuilder map;             // optional: keeps the view inside the map

    [Header("Feel (tune these)")]
    public float smoothTime = 0.12f;   // smaller = tighter follow, 0 = glued to the vehicle
    public Vector2 offset = Vector2.zero;

    Camera cam;
    Vector3 velocity;

    void Awake()
    {
        cam = GetComponent<Camera>();
    }

    void Start()
    {
        if (target != null) transform.position = GetGoal();   // start exactly on the vehicle
    }

    // LateUpdate so the vehicle has already moved this frame.
    void LateUpdate()
    {
        if (target == null) return;

        Vector3 goal = GetGoal();
        transform.position = smoothTime > 0f
            ? Vector3.SmoothDamp(transform.position, goal, ref velocity, smoothTime)
            : goal;
    }

    Vector3 GetGoal()
    {
        Vector3 goal = new Vector3(
            target.position.x + offset.x,
            target.position.y + offset.y,
            transform.position.z);

        if (map != null)
        {
            // Half of what the camera can see, in world units.
            float halfH = cam.orthographicSize;
            float halfW = halfH * cam.aspect;

            Vector3 c = map.transform.position;
            float maxX = Mathf.Max(0f, map.Size.x * 0.5f - halfW);
            float maxY = Mathf.Max(0f, map.Size.y * 0.5f - halfH);

            goal.x = Mathf.Clamp(goal.x, c.x - maxX, c.x + maxX);
            goal.y = Mathf.Clamp(goal.y, c.y - maxY, c.y + maxY);
        }

        return goal;
    }
}
