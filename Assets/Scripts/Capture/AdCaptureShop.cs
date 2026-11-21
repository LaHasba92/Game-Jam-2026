#if UNITY_EDITOR
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

/// <summary>Met en scène deux achats dans le plan de la boutique de la pub.</summary>
public sealed class AdCaptureShop : MonoBehaviour
{
    public const float PowerPurchaseTime = 1.2f;
    public const float CanonPurchaseTime = 3.2f;

    private static readonly Color Navy = new Color32(15, 23, 38, 255);
    private static readonly Color Card = new Color32(27, 42, 61, 255);
    private static readonly Color CardDark = new Color32(21, 33, 49, 255);
    private static readonly Color Line = new Color32(58, 78, 102, 255);
    private static readonly Color Gold = new Color32(255, 193, 64, 255);
    private static readonly Color Mint = new Color32(110, 231, 159, 255);
    private static readonly Color Ivory = new Color32(255, 249, 231, 255);
    private static readonly Color Muted = new Color32(171, 189, 209, 255);

    private Canvas canvas;
    private Font font;
    private Sprite rounded;
    private Sprite disc;
    private Image powerOutline;
    private Image canonOutline;
    private Image powerButton;
    private Image canonButton;
    private Text coins;
    private Text powerValue;
    private Text powerLevel;
    private Text powerAction;
    private Text canonAction;
    private Image pulse;
    private int previousStep = -1;

    public void Show(float elapsed)
    {
        EnsureCanvas();
        canvas.enabled = true;
        int step = elapsed < PowerPurchaseTime ? 0 : elapsed < CanonPurchaseTime ? 1 : 2;
        if (step != previousStep)
        {
            coins.text = step == 0 ? "20 000" : step == 1 ? "19 950" : "4 950";
            powerLevel.text = step == 0 ? "NIVEAU 1" : "NIVEAU 2";
            powerValue.text = step == 0 ? "25  →  26,5 m/s" : "26,5 m/s";
            powerAction.text = step == 0 ? "AMÉLIORER  ·  50" : "AMÉLIORÉ";
            powerAction.color = step == 0 ? Navy : Mint;
            powerButton.color = step == 0 ? Gold : CardDark;
            powerOutline.color = step == 0 ? Gold : Line;
            canonAction.text = step < 2 ? "ACHETER  ·  15 000" : "CANON ÉQUIPÉ";
            canonAction.color = step == 0 ? Ivory : Navy;
            canonButton.color = step == 0 ? Line : step == 1 ? Gold : Mint;
            canonOutline.color = step == 0 ? Line : step == 1 ? Gold : Mint;
            previousStep = step;
        }

        powerButton.rectTransform.localScale = Vector3.one * ButtonScale(elapsed - PowerPurchaseTime);
        canonButton.rectTransform.localScale = Vector3.one * ButtonScale(elapsed - CanonPurchaseTime);
        float age = elapsed - (step == 2 ? CanonPurchaseTime : PowerPurchaseTime);
        pulse.gameObject.SetActive(age >= 0f && age < 0.45f);
        if (pulse.gameObject.activeSelf)
        {
            pulse.rectTransform.position = (step == 2 ? canonButton : powerButton).rectTransform.position;
            float t = age / 0.45f;
            pulse.rectTransform.localScale = Vector3.one * Mathf.Lerp(0.35f, 3.2f, t);
            Color tint = step == 2 ? Mint : Ivory;
            tint.a = (1f - t) * 0.35f;
            pulse.color = tint;
        }
    }

    public void Clear()
    {
        if (canvas != null) canvas.enabled = false;
        previousStep = -1;
    }

    private void OnDisable() => Clear();

    private void OnDestroy()
    {
        if (canvas != null) Destroy(canvas.gameObject);
        if (rounded != null)
        {
            Destroy(rounded.texture);
            Destroy(rounded);
        }
        if (disc != null)
        {
            Destroy(disc.texture);
            Destroy(disc);
        }
    }

    private static float ButtonScale(float timeFromTap)
    {
        if (timeFromTap < 0f || timeFromTap > 0.28f) return 1f;
        return 1f - 0.075f * Mathf.Sin(Mathf.PI * timeFromTap / 0.28f);
    }

    private void EnsureCanvas()
    {
        if (canvas != null) return;

        var root = new GameObject("CanonPOW Boutique pub", typeof(RectTransform), typeof(Canvas), typeof(CanvasScaler));
        SceneManager.MoveGameObjectToScene(root, gameObject.scene);
        canvas = root.GetComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 810;
        CanvasScaler scaler = root.GetComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1080f, 1920f);
        scaler.screenMatchMode = CanvasScaler.ScreenMatchMode.Expand;

        font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        rounded = Shape(24f, true);
        disc = Shape(48f, false);

        Image backdrop = Box("Fond", root.transform, Navy, 0f, 0f, 0f, 0f);
        RectTransform frame = backdrop.rectTransform;
        frame.anchorMin = Vector2.zero;
        frame.anchorMax = Vector2.one;
        frame.offsetMin = Vector2.zero;
        frame.offsetMax = Vector2.zero;

        Image stripe = Box("Bande", frame, new Color32(22, 34, 52, 255), 40f, 452f, 1800f, 190f);
        stripe.rectTransform.localEulerAngles = Vector3.forward * 9f;

        Label(frame, "CANON POW!", 34, Gold, -260f, 742f, 380f, 58f, TextAnchor.MiddleLeft);
        Label(frame, "BOUTIQUE", 96, Ivory, -80f, 632f, 740f, 132f, TextAnchor.MiddleLeft);
        Box("Liseré", frame, Gold, -386f, 540f, 128f, 7f);
        Image wallet = RoundBox("Portefeuille", frame, Card, 291f, 742f, 310f, 84f);
        Circle("Pièce", wallet.rectTransform, Gold, -97f, 0f, 35f);
        Circle("Centre pièce", wallet.rectTransform, new Color32(203, 125, 32, 255), -97f, 0f, 19f);
        coins = Label(wallet.rectTransform, string.Empty, 44, Ivory, 35f, 0f, 200f, 66f);

        CreatePowerCard(frame);
        CreateCanonCard(frame);
        pulse = Circle("Retour du tap", frame, Gold, 0f, 0f, 84f);
        pulse.gameObject.SetActive(false);
    }

    private void CreatePowerCard(RectTransform frame)
    {
        powerOutline = RoundBox("Bordure Puissance", frame, Line, 0f, 303f, 912f, 350f);
        RectTransform card = RoundBox("Puissance", powerOutline.rectTransform, Card, 0f, 0f, 902f, 340f).rectTransform;
        RectTransform statIcon = RoundBox("Icône Puissance", card, CardDark, -328f, 63f, 138f, 138f).rectTransform;
        Box("Impact", statIcon, Gold, -7f, 8f, 23f, 79f).rectTransform.localEulerAngles = Vector3.forward * -35f;
        Box("Impact", statIcon, Gold, 9f, -13f, 22f, 74f).rectTransform.localEulerAngles = Vector3.forward * -35f;
        Label(card, "Puissance", 58, Ivory, 49f, 99f, 580f, 76f, TextAnchor.MiddleLeft);
        powerLevel = Label(card, string.Empty, 29, Mint, 49f, 39f, 580f, 60f, TextAnchor.MiddleLeft);
        powerValue = Label(card, string.Empty, 46, Ivory, 49f, -6f, 580f, 62f, TextAnchor.MiddleLeft);
        powerButton = RoundBox("Améliorer", card, Gold, 0f, -102f, 778f, 80f);
        powerAction = Label(powerButton.rectTransform, string.Empty, 38, Navy, 0f, 0f, 720f, 62f);
    }

    private void CreateCanonCard(RectTransform frame)
    {
        canonOutline = RoundBox("Bordure Canon", frame, Line, 0f, -271f, 912f, 696f);
        RectTransform card = RoundBox("Canon", canonOutline.rectTransform, Card, 0f, 0f, 902f, 686f).rectTransform;
        Label(card, "Canon", 72, Ivory, -111f, 248f, 560f, 94f, TextAnchor.MiddleLeft);
        RectTransform multiplier = RoundBox("Multiplicateur", card, Gold, 309f, 245f, 177f, 72f).rectTransform;
        Label(multiplier, "×3", 57, Navy, 0f, 0f, 160f, 64f);
        Label(card, "Puissance de tir", 34, Muted, -111f, 180f, 560f, 60f, TextAnchor.MiddleLeft);
        RectTransform stage = RoundBox("Aperçu", card, CardDark, 0f, -34f, 778f, 313f).rectTransform;
        DrawCanon(stage);
        canonButton = RoundBox("Acheter", card, Gold, 0f, -265f, 778f, 87f);
        canonAction = Label(canonButton.rectTransform, string.Empty, 39, Navy, 0f, 0f, 720f, 68f);
    }

    private void DrawCanon(RectTransform stage)
    {
        Circle("Halo", stage, new Color32(43, 63, 86, 255), 16f, 15f, 276f);
        Box("Ombre", stage, Navy, 5f, -110f, 546f, 8f);
        Box("Châssis", stage, new Color32(133, 87, 52, 255), -41f, -50f, 295f, 46f);
        Box("Renfort", stage, new Color32(184, 115, 60, 255), -41f, -32f, 295f, 11f);
        RectTransform barrel = RoundBox("Tube", stage, new Color32(100, 124, 148, 255), 47f, 17f, 344f, 97f).rectTransform;
        barrel.localEulerAngles = Vector3.forward * 12f;
        Box("Reflet du tube", barrel, new Color32(143, 166, 188, 255), 7f, 24f, 270f, 10f);
        RoundBox("Bague", barrel, new Color32(237, 161, 62, 255), 99f, 0f, 29f, 103f);
        RoundBox("Bouche", barrel, Line, 168f, 0f, 47f, 119f);
        Box("Reflet de bouche", barrel, Muted, 179f, 0f, 5f, 87f);
        Circle("Roue arrière", stage, Navy, -127f, -58f, 111f);
        Circle("Roue arrière", stage, new Color32(138, 89, 51, 255), -127f, -58f, 88f);
        Circle("Roue avant", stage, Navy, 31f, -69f, 120f);
        Circle("Roue avant", stage, new Color32(190, 125, 67, 255), 31f, -69f, 98f);
        Circle("Moyeu", stage, Gold, 31f, -69f, 31f);
        Circle("Moyeu", stage, Gold, -127f, -58f, 25f);
    }

    private Text Label(Transform parent, string content, int size, Color color,
        float x, float y, float width, float height, TextAnchor alignment = TextAnchor.MiddleCenter)
    {
        Text label = Rect(parent, "Texte", x, y, width, height).gameObject.AddComponent<Text>();
        label.font = font;
        label.fontStyle = FontStyle.Bold;
        label.fontSize = size;
        label.alignment = alignment;
        label.horizontalOverflow = HorizontalWrapMode.Overflow;
        label.verticalOverflow = VerticalWrapMode.Overflow;
        label.color = color;
        label.text = content;
        label.raycastTarget = false;
        return label;
    }

    private Image RoundBox(string name, Transform parent, Color color, float x, float y, float width, float height)
    {
        Image image = Box(name, parent, color, x, y, width, height);
        image.sprite = rounded;
        image.type = Image.Type.Sliced;
        return image;
    }

    private Image Circle(string name, Transform parent, Color color, float x, float y, float diameter)
    {
        Image image = Box(name, parent, color, x, y, diameter, diameter);
        image.sprite = disc;
        return image;
    }

    private static Image Box(string name, Transform parent, Color color, float x, float y, float width, float height)
    {
        Image image = Rect(parent, name, x, y, width, height).gameObject.AddComponent<Image>();
        image.color = color;
        image.raycastTarget = false;
        return image;
    }

    private static RectTransform Rect(Transform parent, string name, float x, float y, float width, float height)
    {
        var rect = (RectTransform)new GameObject(name, typeof(RectTransform)).transform;
        rect.SetParent(parent, false);
        rect.anchorMin = rect.anchorMax = new Vector2(0.5f, 0.5f);
        rect.sizeDelta = new Vector2(width, height);
        rect.anchoredPosition = new Vector2(x, y);
        return rect;
    }

    private static Sprite Shape(float radius, bool sliced)
    {
        const int size = 96;
        const float center = size / 2f;
        var image = new Texture2D(size, size, TextureFormat.RGBA32, false) { wrapMode = TextureWrapMode.Clamp };
        var pixels = new Color32[size * size];
        for (int y = 0; y < size; ++y)
        for (int x = 0; x < size; ++x)
        {
            float dx = Mathf.Max(0f, Mathf.Abs(x + 0.5f - center) - center + radius);
            float dy = Mathf.Max(0f, Mathf.Abs(y + 0.5f - center) - center + radius);
            float alpha = Mathf.Clamp01(radius - Mathf.Sqrt(dx * dx + dy * dy) + 0.5f);
            pixels[y * size + x] = new Color32(255, 255, 255, (byte)Mathf.RoundToInt(alpha * 255f));
        }
        image.SetPixels32(pixels);
        image.Apply(false, true);
        Vector4 border = sliced ? Vector4.one * (radius + 1f) : Vector4.zero;
        return Sprite.Create(image, new Rect(0f, 0f, size, size), new Vector2(0.5f, 0.5f),
            100f, 0, SpriteMeshType.FullRect, border);
    }
}
#endif
