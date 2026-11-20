#if UNITY_EDITOR
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

/// <summary>Affiche la jauge et le carton final dans la vue Game.</summary>
public sealed class AdCaptureOverlay : MonoBehaviour
{
    private static readonly Color Navy = new Color32(15, 23, 38, 255);
    private static readonly Color Gold = new Color32(255, 193, 64, 255);
    private static readonly Color Mint = new Color32(110, 231, 159, 255);
    private static readonly Color Ivory = new Color32(255, 249, 231, 255);

    private Canvas canvas;
    private GameObject gauge;
    private GameObject endCard;
    private RectTransform needle;
    private Text gaugeLabel;
    private Font font;

    public void ShowGauge(float value, bool perfect)
    {
        EnsureCanvas();
        endCard.SetActive(false);
        gauge.SetActive(true);
        canvas.enabled = true;

        Vector2 needleAnchor = new Vector2(Mathf.Clamp01(value), 0.5f);
        needle.anchorMin = needleAnchor;
        needle.anchorMax = needleAnchor;
        gaugeLabel.text = perfect ? "TIR PARFAIT !" : "PUISSANCE";
        gaugeLabel.color = perfect ? Mint : Ivory;
    }

    public void ShowEndCard()
    {
        EnsureCanvas();
        gauge.SetActive(false);
        endCard.SetActive(true);
        canvas.enabled = true;
    }

    public void Clear()
    {
        if (canvas != null) canvas.enabled = false;
    }

    private void OnDisable() => Clear();

    private void OnDestroy()
    {
        if (canvas != null) Destroy(canvas.gameObject);
    }

    private void EnsureCanvas()
    {
        if (canvas != null) return;

        var root = new GameObject("CanonPOW Capture", typeof(RectTransform), typeof(Canvas), typeof(CanvasScaler));
        SceneManager.MoveGameObjectToScene(root, gameObject.scene);
        canvas = root.GetComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 800;
        canvas.enabled = false;

        CanvasScaler scaler = root.GetComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1080f, 1920f);
        scaler.matchWidthOrHeight = 0.5f;

        font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        CreateGauge(root.transform);
        CreateEndCard(root.transform);
    }

    private void CreateGauge(Transform parent)
    {
        Image panel = ImageAt("Jauge", parent, Navy, new Vector2(0.5f, 0.20f), new Vector2(760f, 150f), Vector2.zero);
        RectTransform box = panel.rectTransform;
        gauge = panel.gameObject;
        ImageAt("Bord supérieur", box, Gold, new Vector2(0.5f, 1f), new Vector2(760f, 5f), Vector2.down * 2.5f);
        gaugeLabel = TextAt("Puissance", box, string.Empty, Ivory, 48, new Vector2(720f, 63f), new Vector2(0f, 34f));

        Image track = ImageAt("Fond de la jauge", box, new Color32(58, 71, 87, 255),
            new Vector2(0.5f, 0.5f), new Vector2(658f, 35f), new Vector2(0f, -34f));

        // Zone cible sur la partie haute de la jauge normalisée.
        RectTransform target = ImageAt("Zone parfaite", track.rectTransform, Mint, Vector2.zero, Vector2.zero, Vector2.zero).rectTransform;
        target.anchorMin = new Vector2(0.88f, 0f);
        target.anchorMax = Vector2.one;
        target.offsetMin = Vector2.zero;
        target.offsetMax = Vector2.zero;

        needle = ImageAt("Curseur", track.rectTransform, Ivory, new Vector2(0f, 0.5f),
            new Vector2(12f, 51f), Vector2.zero).rectTransform;
    }

    private void CreateEndCard(Transform parent)
    {
        Image backdrop = ImageAt("Carton final", parent, Navy, Vector2.zero, Vector2.zero, Vector2.zero);
        RectTransform frame = backdrop.rectTransform;
        frame.anchorMin = Vector2.zero;
        frame.anchorMax = Vector2.one;
        frame.offsetMin = Vector2.zero;
        frame.offsetMax = Vector2.zero;
        endCard = backdrop.gameObject;

        ImageAt("Bande diagonale", frame, new Color32(26, 41, 60, 255), new Vector2(0.5f, 0.72f),
            new Vector2(1800f, 345f), Vector2.zero).rectTransform.localEulerAngles = new Vector3(0f, 0f, 12f);
        ImageAt("Accent diagonal", frame, new Color32(75, 53, 37, 255), new Vector2(0.5f, 0.28f),
            new Vector2(1800f, 14f), Vector2.zero).rectTransform.localEulerAngles = new Vector3(0f, 0f, 12f);

        RectTransform logo = RectAt("Logo", frame, new Vector2(0.5f, 0.59f), new Vector2(960f, 520f), Vector2.zero);
        logo.localEulerAngles = new Vector3(0f, 0f, 7f);
        TextAt("Canon", logo, "CANON", Ivory, 176, new Vector2(900f, 210f), new Vector2(0f, 118f));
        Text pow = TextAt("POW!", logo, "POW!", Gold, 276, new Vector2(940f, 315f), new Vector2(0f, -90f));
        Shadow shadow = pow.gameObject.AddComponent<Shadow>();
        shadow.effectColor = new Color32(7, 12, 21, 255);
        shadow.effectDistance = new Vector2(9f, -11f);
        ImageAt("Soulignement", logo, Gold, new Vector2(0.5f, 0f),
            new Vector2(330f, 13f), new Vector2(0f, 18f));

        RectTransform stores = RectAt("Plateformes", frame, new Vector2(0.5f, 0.20f),
            new Vector2(840f, 114f), Vector2.zero);
        CreateStoreBadge(stores, "App Store", -205f);
        CreateStoreBadge(stores, "Google Play", 205f);
        endCard.SetActive(false);
    }

    private void CreateStoreBadge(RectTransform parent, string label, float x)
    {
        Image tile = ImageAt(label, parent, Ivory, new Vector2(0.5f, 0.5f),
            new Vector2(375f, 106f), new Vector2(x, 0f));
        TextAt(label, tile.rectTransform, label, Navy, 49, new Vector2(348f, 76f), Vector2.zero);
    }

    private Text TextAt(string name, Transform parent, string value, Color color, int size, Vector2 dimensions, Vector2 position)
    {
        Text label = RectAt(name, parent, new Vector2(0.5f, 0.5f), dimensions, position).gameObject.AddComponent<Text>();
        label.font = font;
        label.fontStyle = FontStyle.Bold;
        label.fontSize = size;
        label.alignment = TextAnchor.MiddleCenter;
        label.horizontalOverflow = HorizontalWrapMode.Overflow;
        label.verticalOverflow = VerticalWrapMode.Overflow;
        label.color = color;
        label.text = value;
        label.raycastTarget = false;
        return label;
    }

    private static Image ImageAt(string name, Transform parent, Color color, Vector2 anchor, Vector2 size, Vector2 position)
    {
        Image image = RectAt(name, parent, anchor, size, position).gameObject.AddComponent<Image>();
        image.color = color;
        image.raycastTarget = false;
        return image;
    }

    private static RectTransform RectAt(string name, Transform parent, Vector2 anchor, Vector2 size, Vector2 position)
    {
        var rect = (RectTransform)new GameObject(name, typeof(RectTransform)).transform;
        rect.SetParent(parent, false);
        rect.anchorMin = anchor;
        rect.anchorMax = anchor;
        rect.sizeDelta = size;
        rect.anchoredPosition = position;
        return rect;
    }
}
#endif
