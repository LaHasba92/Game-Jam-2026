#if UNITY_EDITOR
using System;
using System.Reflection;
using UnityEditor;
using UnityEngine;

internal static class AdCaptureGameView
{
    private const BindingFlags InstanceMembers = BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance;
    private const BindingFlags StaticMembers = BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Static;

    private static EditorWindow captureView;
    private static int originalSizeIndex;
    private static object originalSizeGroup;

    public static bool TrySetResolution(int width, int height)
    {
        if (width < 10 || height < 10) return false;

        try
        {
            Type viewType = typeof(EditorWindow).Assembly.GetType("UnityEditor.GameView");
            if (viewType == null) return false;

            var view = viewType.BaseType?.GetMethod("GetMainPlayModeView", StaticMembers)?.Invoke(null, null) as EditorWindow;
            if (view == null || !viewType.IsInstanceOfType(view))
                view = EditorWindow.GetWindow(viewType, false, null, false);
            if (view == null) return false;

            view.Focus();
            object currentSize = viewType.GetProperty("currentGameViewSize", InstanceMembers)?.GetValue(view);
            if (IsFixedResolution(currentSize, width, height)) return true;

            // Unity 6000.3 reuses a named size and updates the Game View's render target.
            MethodInfo setResolution = viewType.GetMethod("SetCustomResolution", InstanceMembers);
            PropertyInfo selectedSize = viewType.GetProperty("selectedSizeIndex", InstanceMembers);
            PropertyInfo sizeGroup = viewType.GetProperty("currentSizeGroupType", StaticMembers);
            if (setResolution == null || selectedSize == null || sizeGroup == null ||
                viewType.GetMethod("SizeSelectionCallback", InstanceMembers) == null)
                return false;

            if (captureView != view)
            {
                Restore();
                originalSizeIndex = (int)selectedSize.GetValue(view);
                originalSizeGroup = sizeGroup.GetValue(null);
                captureView = view;
            }

            string preset = $"CanonPOW Capture {width}x{height}";
            setResolution.Invoke(view, new object[] { new Vector2(width, height), preset });
            view.Repaint();
            return true;
        }
        catch (Exception exception)
        {
            Restore();
            Debug.LogWarning($"Capture pub : sélection de la résolution impossible ({exception.GetBaseException().Message}).");
            return false;
        }
    }

    public static void Restore()
    {
        EditorWindow view = captureView;
        captureView = null;
        object sizeGroup = originalSizeGroup;
        originalSizeGroup = null;
        if (view == null) return;

        try
        {
            Type viewType = view.GetType();
            object currentGroup = viewType.GetProperty("currentSizeGroupType", StaticMembers)?.GetValue(null);
            if (!Equals(currentGroup, sizeGroup)) return;

            viewType.GetMethod("SizeSelectionCallback", InstanceMembers)?.Invoke(view, new object[] { originalSizeIndex, null });
            view.Repaint();
        }
        catch (Exception exception)
        {
            Debug.LogWarning($"Capture pub : restauration de la résolution impossible ({exception.GetBaseException().Message}).");
        }
    }

    private static bool IsFixedResolution(object size, int width, int height)
    {
        if (size == null) return false;

        Type sizeType = size.GetType();
        object kind = sizeType.GetProperty("sizeType", InstanceMembers)?.GetValue(size);
        return kind?.ToString() == "FixedResolution" &&
               Equals(sizeType.GetProperty("width", InstanceMembers)?.GetValue(size), width) &&
               Equals(sizeType.GetProperty("height", InstanceMembers)?.GetValue(size), height);
    }
}
#endif
