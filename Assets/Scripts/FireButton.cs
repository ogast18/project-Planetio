using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.UI;

// A hold-to-fire button. While it is held, the turret fires even if no enemy is in the cone
// (for shooting walls). Put this on a UI Image. In the editor the Space key does the same.
[RequireComponent(typeof(Image))]
public class FireButton : MonoBehaviour, IPointerDownHandler, IPointerUpHandler
{
    [Header("Look")]
    public Color idleColor = new Color(1f, 0.45f, 0.2f, 0.45f);
    public Color heldColor = new Color(1f, 0.45f, 0.2f, 0.95f);

    bool held;
    int activePointer = int.MinValue;
    Image image;

    public bool IsHeld
    {
        get
        {
            if (held) return true;
            var kb = Keyboard.current;
            return kb != null && kb.spaceKey.isPressed;   // editor testing
        }
    }

    void Awake()
    {
        image = GetComponent<Image>();
        image.color = idleColor;
    }

    public void OnPointerDown(PointerEventData e)
    {
        if (held) return;
        held = true;
        activePointer = e.pointerId;
        image.color = heldColor;
    }

    // Also called when the finger slides off the button, so it never gets stuck on.
    public void OnPointerUp(PointerEventData e)
    {
        if (!held || e.pointerId != activePointer) return;
        Release();
    }

    void OnDisable()
    {
        Release();
    }

    void Release()
    {
        held = false;
        activePointer = int.MinValue;
        if (image != null) image.color = idleColor;
    }
}
