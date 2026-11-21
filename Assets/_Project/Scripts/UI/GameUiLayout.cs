using TMPro;
using UnityEngine;
using UnityEngine.UI;

internal static class GameUiLayout
{
    private static Sprite roundedPanel;

    public static void Scale(CanvasScaler scaler)
    {
        if (scaler == null) return;
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.screenMatchMode = CanvasScaler.ScreenMatchMode.Expand;
        scaler.referenceResolution = new Vector2(1080f, 1920f);
    }

    public static RectTransform Frame(string name, Transform parent)
    {
        var root = new GameObject(name, typeof(RectTransform)) { layer = parent.gameObject.layer };
        root.transform.SetParent(parent, false);
        return (RectTransform)root.transform;
    }

    public static Image Block(string name, Transform parent, bool rounded = true)
    {
        Image image = Frame(name, parent).gameObject.AddComponent<Image>();
        image.raycastTarget = false;
        if (rounded) Round(image);
        return image;
    }

    public static TMP_Text Text(string name, Transform parent, string value, float size,
        TextAlignmentOptions alignment = TextAlignmentOptions.MidlineLeft)
    {
        var text = Frame(name, parent).gameObject.AddComponent<TextMeshProUGUI>();
        text.text = value;
        StyleText(text, size, alignment);
        return text;
    }

    public static void StyleText(TMP_Text text, float size, TextAlignmentOptions alignment)
    {
        TMP_FontAsset font = TMP_Settings.defaultFontAsset;
        if (font != null)
        {
            text.font = font;
            text.fontSharedMaterial = font.material;
        }
        text.raycastTarget = false;
        text.enableVertexGradient = false;
        text.alignment = alignment;
        text.fontStyle = FontStyles.Bold;
        text.textWrappingMode = TextWrappingModes.NoWrap;
        text.overflowMode = TextOverflowModes.Ellipsis;
        text.enableAutoSizing = true;
        text.fontSize = size;
        text.fontSizeMin = size * 0.72f;
        text.fontSizeMax = size;
        text.margin = Vector4.zero;
    }

    public static Button MakeButton(string name, Transform parent)
    {
        Image image = Block(name, parent);
        Button button = image.gameObject.AddComponent<Button>();
        button.targetGraphic = image;
        return button;
    }

    public static void TintButton(Button button, Color background)
    {
        if (button.targetGraphic is not Image image) return;
        Round(image);
        image.raycastTarget = true;
        image.color = background;
        button.transition = Selectable.Transition.ColorTint;
        ColorBlock colors = ColorBlock.defaultColorBlock;
        colors.highlightedColor = new Color(1f, 1f, 1f, 0.9f);
        colors.pressedColor = new Color(0.78f, 0.78f, 0.78f);
        colors.disabledColor = new Color(0.58f, 0.58f, 0.58f, 0.7f);
        colors.fadeDuration = 0.08f;
        button.colors = colors;
    }

    public static void Place(RectTransform rect, float xMin, float yMin, float xMax, float yMax,
        float left = 0f, float bottom = 0f, float right = 0f, float top = 0f)
    {
        rect.localScale = Vector3.one;
        rect.localRotation = Quaternion.identity;
        rect.anchorMin = new Vector2(xMin, yMin);
        rect.anchorMax = new Vector2(xMax, yMax);
        rect.offsetMin = new Vector2(left, bottom);
        rect.offsetMax = new Vector2(-right, -top);
        Vector3 position = rect.anchoredPosition3D;
        position.z = 0f;
        rect.anchoredPosition3D = position;
    }

    public static void FitSafeArea(RectTransform rect, Rect safe, Vector2 screen)
    {
        if (safe.width <= 0f || safe.height <= 0f || screen.x <= 0f || screen.y <= 0f) return;
        // Limite la colonne aux proportions d'un écran portrait, y compris en aperçu sur PC.
        float halfWidth = Mathf.Min(safe.width * 0.5f, safe.height * 0.36f);
        Place(rect, (safe.center.x - halfWidth) / screen.x, safe.yMin / screen.y,
            (safe.center.x + halfWidth) / screen.x, safe.yMax / screen.y);
    }

    private static void Round(Image image)
    {
        if (roundedPanel == null) roundedPanel = CreateRoundedPanel();
        image.sprite = roundedPanel;
        image.type = Image.Type.Sliced;
        image.pixelsPerUnitMultiplier = 1f;
    }

    private static Sprite CreateRoundedPanel()
    {
        const int size = 48;
        const int radius = 18;
        var texture = new Texture2D(size, size, TextureFormat.RGBA32, false)
        {
            name = "CanonPOW — panneau arrondi",
            filterMode = FilterMode.Bilinear,
            wrapMode = TextureWrapMode.Clamp,
            hideFlags = HideFlags.DontSaveInEditor | HideFlags.DontSaveInBuild
        };
        var pixels = new Color32[size * size];
        for (int y = 0; y < size; y++)
        {
            float vertical = Mathf.Max(0f, Mathf.Abs(y + 0.5f - size * 0.5f) - (size * 0.5f - radius));
            for (int x = 0; x < size; x++)
            {
                float horizontal = Mathf.Max(0f, Mathf.Abs(x + 0.5f - size * 0.5f) - (size * 0.5f - radius));
                float alpha = Mathf.Clamp01(radius + 0.5f - Mathf.Sqrt(horizontal * horizontal + vertical * vertical));
                pixels[y * size + x] = new Color32(255, 255, 255, (byte)(alpha * 255f));
            }
        }
        texture.SetPixels32(pixels);
        texture.Apply(false, true);
        Sprite sprite = Sprite.Create(texture, new Rect(0f, 0f, size, size), new Vector2(0.5f, 0.5f),
            100f, 0, SpriteMeshType.FullRect, new Vector4(radius, radius, radius, radius));
        sprite.hideFlags = texture.hideFlags;
        return sprite;
    }
}
