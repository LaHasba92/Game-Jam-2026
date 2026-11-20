#if UNITY_EDITOR
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

// Volet navy et or synchronisé sur les coupes du montage.
public sealed class AdCaptureTransition : MonoBehaviour
{
    private Canvas canvas;
    private RectTransform viewport;
    private RectTransform panel;

    public void Show(float progress)
    {
        if (progress <= 0f || progress >= 1f)
        {
            if (canvas != null) canvas.enabled = false;
            return;
        }

        if (canvas == null) CreateWipe();
        canvas.enabled = true;
        float width = viewport.rect.width;
        float t = Mathf.SmoothStep(0f, 1f, progress);
        panel.anchoredPosition = new Vector2(Mathf.Lerp(-width * 1.24f, width * 1.24f, t), 0f);
    }

    private void CreateWipe()
    {
        var root = new GameObject("CanonPOW — Transition", typeof(RectTransform), typeof(Canvas), typeof(CanvasScaler));
        SceneManager.MoveGameObjectToScene(root, gameObject.scene);
        canvas = root.GetComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 1000;
        viewport = (RectTransform)root.transform;
        var scaler = root.GetComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1080f, 1920f);
        scaler.matchWidthOrHeight = 0.5f;

        panel = AddImage("Volet", viewport, new Color32(15, 23, 38, 255));
        panel.anchorMin = new Vector2(-0.05f, 0f);
        panel.anchorMax = new Vector2(1.05f, 1f);
        panel.offsetMin = Vector2.zero;
        panel.offsetMax = Vector2.zero;

        RectTransform leadingEdge = AddImage("Liseré", panel, new Color32(255, 193, 64, 255));
        leadingEdge.anchorMin = new Vector2(1f, 0f);
        leadingEdge.anchorMax = Vector2.one;
        leadingEdge.offsetMin = new Vector2(-54f, 0f);
        leadingEdge.offsetMax = Vector2.zero;

        RectTransform trailingEdge = AddImage("Reflet", panel, new Color32(255, 226, 163, 255));
        trailingEdge.anchorMin = Vector2.zero;
        trailingEdge.anchorMax = new Vector2(0f, 1f);
        trailingEdge.offsetMin = Vector2.zero;
        trailingEdge.offsetMax = new Vector2(16f, 0f);
        Canvas.ForceUpdateCanvases();
    }

    private static RectTransform AddImage(string name, Transform parent, Color colour)
    {
        var item = new GameObject(name, typeof(RectTransform), typeof(Image));
        item.transform.SetParent(parent, false);
        Image image = item.GetComponent<Image>();
        image.color = colour;
        image.raycastTarget = false;
        return image.rectTransform;
    }

    private void OnDestroy()
    {
        if (canvas != null) Destroy(canvas.gameObject);
    }
}
#endif
