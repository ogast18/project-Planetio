using UnityEngine;

// Smooth top-down camera (3D version) that follows the vehicle and (optionally) stays inside the map.
// Put this on the Main Camera. Works with Perspective or Orthographic projection.
[RequireComponent(typeof(Camera))]
public class CameraFollow : MonoBehaviour
{
    [Header("References")]
    public Transform target;           // drag the Vehicle here
    public MapBuilder map;             // optional: keeps the view inside the map

    [Header("View (tune these)")]
    [Range(30f, 90f)]
    public float pitch = 90f;          // 90 = straight down, lower = tilted toward the horizon
    public float distance = 22f;       // how far the camera is from the point it looks at
    public Vector2 offset = Vector2.zero; // shifts the look-at point on the ground (x, z)

    [Header("Feel (tune these)")]
    public float smoothTime = 0.12f;   // smaller = tighter follow, 0 = glued to the vehicle

    Camera cam;
    Vector3 focus;                     // the point on the ground the camera looks at
    Vector3 velocity;

    void Awake()
    {
        cam = GetComponent<Camera>();
    }

    void Start()
    {
        if (target == null) return;
        focus = GetGoal();             // start exactly on the vehicle
        Apply();
    }

    // LateUpdate so the vehicle has already moved this frame.
    void LateUpdate()
    {
        if (target == null) return;

        Vector3 goal = GetGoal();
        focus = smoothTime > 0f
            ? Vector3.SmoothDamp(focus, goal, ref velocity, smoothTime)
            : goal;
        Apply();
    }

    void Apply()
    {
        // Screen up = world +Z when yaw is 0, so the sticks match the screen.
        Quaternion rot = Quaternion.Euler(pitch, 0f, 0f);
        transform.rotation = rot;
        transform.position = focus - rot * Vector3.forward * distance;
    }

    Vector3 GetGoal()
    {
        Vector3 goal = new Vector3(target.position.x + offset.x, 0f, target.position.z + offset.y);

        if (map != null)
        {
            // Half of what the camera sees on the ground, in world units
            // (exact when looking straight down, a close estimate when tilted).
            float halfH = cam.orthographic
                ? cam.orthographicSize
                : distance * Mathf.Tan(cam.fieldOfView * 0.5f * Mathf.Deg2Rad);
            float halfW = halfH * cam.aspect;

            Vector3 c = map.transform.position;
            float maxX = Mathf.Max(0f, map.Size.x * 0.5f - halfW);
            float maxZ = Mathf.Max(0f, map.Size.y * 0.5f - halfH);

            goal.x = Mathf.Clamp(goal.x, c.x - maxX, c.x + maxX);
            goal.z = Mathf.Clamp(goal.z, c.z - maxZ, c.z + maxZ);
        }

        return goal;
    }
}
