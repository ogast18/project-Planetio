using UnityEngine;
using UnityEngine.EventSystems;

// A touch stick built on Unity UI pointer events (each stick tracks its own finger).
// Put this on a big transparent "zone" Image. The Background (ring) and the Handle (knob)
// are children: Handle inside Background, Background inside the zone.
// The same script will be used for the aim stick in Part 4.
public class VirtualStick : MonoBehaviour, IPointerDownHandler, IDragHandler, IPointerUpHandler
{
    [Header("References")]
    public RectTransform background;   // the ring
    public RectTransform handle;       // the knob (child of the background)

    [Header("Feel (tune these)")]
    public float radius = 110f;        // max knob travel, in canvas units
    public float deadZone = 0.1f;      // ignore tiny movements near the centre (0..1)
    public bool floating = true;       // the ring jumps to where the finger lands

    // Stick output, length 0..1. Zero while not touched.
    public Vector2 Value { get; private set; }
    public bool IsHeld { get; private set; }

    RectTransform zone;
    Vector3 homePosition;
    Vector2 center;
    int activePointer = int.MinValue;

    void Start()
    {
        zone = (RectTransform)transform;
        Canvas.ForceUpdateCanvases();
        homePosition = background.localPosition;
        ResetStick();
    }

    public void OnPointerDown(PointerEventData e)
    {
        if (IsHeld) return;                          // already controlled by another finger
        if (!ToLocal(e, out Vector2 local)) return;

        IsHeld = true;
        activePointer = e.pointerId;
        center = floating ? local : (Vector2)homePosition;
        background.localPosition = center;
        Move(local);
    }

    public void OnDrag(PointerEventData e)
    {
        if (!IsHeld || e.pointerId != activePointer) return;
        if (ToLocal(e, out Vector2 local)) Move(local);
    }

    public void OnPointerUp(PointerEventData e)
    {
        if (!IsHeld || e.pointerId != activePointer) return;
        ResetStick();
    }

    bool ToLocal(PointerEventData e, out Vector2 local)
    {
        return RectTransformUtility.ScreenPointToLocalPointInRectangle(
            zone, e.position, e.pressEventCamera, out local);
    }

    void Move(Vector2 local)
    {
        Vector2 offset = Vector2.ClampMagnitude(local - center, radius);
        handle.localPosition = offset;

        float m = offset.magnitude / radius;
        if (m < deadZone)
        {
            Value = Vector2.zero;
            return;
        }

        // Rescale so the output still starts at 0 right after the dead zone.
        float scaled = (m - deadZone) / (1f - deadZone);
        Value = offset.normalized * scaled;
    }

    void ResetStick()
    {
        IsHeld = false;
        activePointer = int.MinValue;
        Value = Vector2.zero;
        center = homePosition;
        background.localPosition = homePosition;
        handle.localPosition = Vector3.zero;
    }
}
