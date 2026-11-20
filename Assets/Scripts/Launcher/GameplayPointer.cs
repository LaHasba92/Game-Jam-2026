using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;

// Garde le même pointeur du début à la fin du geste.
internal static class GameplayPointer
{
    private static readonly List<RaycastResult> Hits = new List<RaycastResult>();

    public static Pointer PressedThisFrame()
    {
        Touchscreen touch = Touchscreen.current;
        if (touch != null && touch.primaryTouch.press.wasPressedThisFrame)
            return OverInterface(touch) ? null : touch;

        Mouse mouse = Mouse.current;
        return mouse != null && mouse.leftButton.wasPressedThisFrame && !OverInterface(mouse) ? mouse : null;
    }

    private static bool OverInterface(Pointer pointer)
    {
        EventSystem events = EventSystem.current;
        if (events == null) return false;
        events.RaycastAll(new PointerEventData(events) { position = pointer.position.ReadValue() }, Hits);
        bool blocked = Hits.Exists(hit => hit.module is UnityEngine.UI.GraphicRaycaster);
        Hits.Clear();
        return blocked;
    }
}
