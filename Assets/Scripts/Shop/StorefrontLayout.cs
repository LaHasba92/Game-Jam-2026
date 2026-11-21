using System;
using UnityEngine;
using TMPro;
using UnityEngine.UI;

internal sealed class StorefrontLayout
{
    private const float HeaderHeight = 276f;
    private const float ActionWidth = 242f;
    private const float ActionHeight = 114f;
    private const float Gap = 18f;

    private readonly Canvas canvas;
    private readonly RectTransform safeArea;
    private readonly RectTransform sheet;
    private readonly Image background;
    private readonly Image closeButton;
    private readonly TMP_Text back;
    private readonly TMP_Text title;
    private readonly Image walletCard;
    private readonly TMP_Text wallet;
    private readonly TMP_Text notice;
    private readonly Image[] tabs;
    private readonly TMP_Text[] tabLabels;
    private readonly ScrollRect scroll;
    private readonly RectTransform content;
    private Rect lastSafeArea;
    private Vector2Int lastScreen;
    private float lastScale;
    private ThemePalette palette;

    public Color Accent => palette.Accent;
    public float ScrollPosition => scroll.verticalNormalizedPosition;

    public StorefrontLayout(RectTransform parent, string[] labels, Action<int> select, Action close)
    {
        canvas = parent.GetComponent<Canvas>();
        GameUiLayout.Scale(parent.GetComponent<CanvasScaler>());

        background = parent.gameObject.AddComponent<Image>();
        background.raycastTarget = true;
        safeArea = GameUiLayout.Frame("Zone sûre", parent);
        Stretch(safeArea, 0f, 0f, 0f, 0f);
        sheet = GameUiLayout.Frame("Boutique", safeArea);
        sheet.anchorMin = new Vector2(.5f, 0f);
        sheet.anchorMax = new Vector2(.5f, 1f);
        sheet.offsetMin = new Vector2(-490f, 18f);
        sheet.offsetMax = new Vector2(490f, -18f);

        closeButton = MakeButton(sheet, "Retour", close);
        Place(closeButton.rectTransform, new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(208f, 108f),
            new Vector2(0f, -16f));
        back = ButtonLabel(closeButton, "‹ Retour", 32);
        title = MakeText(sheet, "Boutique", 62, TextAlignmentOptions.Center, true);
        StretchTop(title.rectTransform, 120f, 216f, 338f, 8f);
        walletCard = Box(sheet, "Solde d’or", Color.white);
        Place(walletCard.rectTransform, Vector2.one, Vector2.one, new Vector2(218f, 90f), new Vector2(-116f, -24f));
        wallet = ButtonLabel(walletCard, "—", 36);

        RectTransform tabBar = GameUiLayout.Frame("Onglets", sheet);
        StretchTop(tabBar, 112f, 0f, 0f, 142f);
        var tabLayout = tabBar.gameObject.AddComponent<HorizontalLayoutGroup>();
        tabLayout.spacing = 9f;
        tabLayout.childControlHeight = true;
        tabLayout.childForceExpandHeight = true;
        tabLayout.childControlWidth = true;
        tabLayout.childForceExpandWidth = true;
        tabs = new Image[labels.Length];
        tabLabels = new TMP_Text[labels.Length];
        for (int i = 0; i < labels.Length; ++i)
        {
            int index = i;
            tabs[i] = MakeButton(tabBar, labels[i], () => select(index));
            tabs[i].gameObject.AddComponent<LayoutElement>().flexibleWidth = 1f;
            tabLabels[i] = ButtonLabel(tabs[i], labels[i], 33);
        }

        RectTransform viewport = GameUiLayout.Frame("Offres", sheet);
        Stretch(viewport, 0f, 68f, 0f, HeaderHeight);
        viewport.gameObject.AddComponent<RectMask2D>();
        var touchArea = viewport.gameObject.AddComponent<Image>();
        touchArea.color = Color.clear;
        scroll = viewport.gameObject.AddComponent<ScrollRect>();
        scroll.viewport = viewport;
        scroll.horizontal = false;
        scroll.movementType = ScrollRect.MovementType.Clamped;
        scroll.scrollSensitivity = 80f;
        content = GameUiLayout.Frame("Liste", viewport);
        content.pivot = new Vector2(.5f, 1f);
        StretchTop(content, 0f, 0f, 0f, 0f);
        var list = content.gameObject.AddComponent<VerticalLayoutGroup>();
        list.spacing = Gap;
        list.childForceExpandWidth = true;
        list.childControlWidth = true;
        list.childControlHeight = true;
        list.childForceExpandHeight = false;
        list.padding = new RectOffset(0, 0, 4, 18);
        var size = content.gameObject.AddComponent<ContentSizeFitter>();
        size.verticalFit = ContentSizeFitter.FitMode.PreferredSize;
        scroll.content = content;

        notice = MakeText(sheet, "", 30, TextAlignmentOptions.MidlineLeft, false);
        StretchBottom(notice.rectTransform, 60f, 12f, 12f, 0f);
    }

    public void Resize()
    {
        var screen = new Vector2Int(Screen.width, Screen.height);
        if (screen.x <= 0 || screen.y <= 0) return;
        Rect area = Screen.safeArea;
        if (area.width <= 0 || area.height <= 0)
            area = new Rect(0f, 0f, screen.x, screen.y);
        float scale = Mathf.Max(.01f, canvas.scaleFactor);
        if (lastScreen == screen && lastSafeArea == area && Mathf.Approximately(lastScale, scale)) return;
        lastScreen = screen;
        lastSafeArea = area;
        lastScale = scale;
        safeArea.anchorMin = new Vector2(area.xMin / screen.x, area.yMin / screen.y);
        safeArea.anchorMax = new Vector2(area.xMax / screen.x, area.yMax / screen.y);
        sheet.SetSizeWithCurrentAnchors(RectTransform.Axis.Horizontal, Mathf.Max(1f, Mathf.Min(1040f, area.width / scale - 76f)));
    }

    public void SetPalette(ThemePalette skin, int activeTab, bool trailsUnlocked)
    {
        palette = skin;
        background.color = palette.Background;
        title.color = palette.Text;
        closeButton.color = palette.Surface;
        back.color = palette.Text;
        walletCard.color = palette.Surface;
        wallet.color = palette.Accent;
        notice.color = palette.Text;

        for (int i = 0; i < tabs.Length; ++i)
        {
            bool active = i == activeTab;
            tabs[i].color = active ? palette.Accent : palette.Surface;
            tabLabels[i].color = active ? palette.Background : i == 3 && !trailsUnlocked ? palette.Muted : palette.Text;
        }
    }

    public void ClearContent()
    {
        for (int i = content.childCount - 1; i >= 0; --i)
        {
            GameObject old = content.GetChild(i).gameObject;
            old.SetActive(false);
            UnityEngine.Object.Destroy(old);
        }
    }

    public void SetScrollPosition(float value)
    {
        LayoutRebuilder.ForceRebuildLayoutImmediate(content);
        scroll.StopMovement();
        scroll.verticalNormalizedPosition = value;
    }

    public void SetWallet(string label) => wallet.text = label;
    public void SetNotice(string label) => notice.text = label ?? string.Empty;

    public void Heading(string name, string detail)
    {
        bool hasDetail = !string.IsNullOrEmpty(detail);
        RectTransform section = AddListItem(name, hasDetail ? 156f : 82f);
        TMP_Text label = MakeText(section, name, 45, TextAlignmentOptions.MidlineLeft, true);
        StretchTop(label.rectTransform, 68f, 8f, 8f, 2f);
        label.color = palette.Text;
        if (!hasDetail) return;
        TMP_Text description = MakeText(section, detail, 34, TextAlignmentOptions.TopLeft, false);
        Stretch(description.rectTransform, 9f, 3f, 9f, 72f);
        description.color = palette.Muted;
    }

    public void Row(string name, string detail, string action, bool enabled, Action onClick,
        Color accent, bool selected = false)
    {
        RectTransform row = AddListItem(name, 162f);
        RectTransform card = MakeCard(row, selected ? accent : palette.Border);
        WriteCardDetails(card, name, detail, action, enabled, onClick, accent, selected);
    }

    public void ThemeCard(ThemeOption option, string action, bool enabled, Action onClick, bool selected)
    {
        RectTransform row = AddListItem(option.Name, 420f);
        Color accent = option.Palette.Accent;
        RectTransform card = MakeCard(row, selected ? accent : palette.Border);
        Texture2D illustration = Resources.Load<Texture2D>("Shop/" + option.Id);
        if (illustration != null)
        {
            RectTransform graphic = GameUiLayout.Frame("Décor", card);
            StretchTop(graphic, 238f, 10f, 10f, 10f);
            var image = graphic.gameObject.AddComponent<RawImage>();
            image.texture = illustration;
            image.raycastTarget = false;
        }
        RectTransform details = GameUiLayout.Frame("Infos du thème", card);
        StretchBottom(details, 164f, 0f, 0f, 0f);
        WriteCardDetails(details, option.Name, option.Description, action, enabled, onClick, accent, selected);
    }

    private void WriteCardDetails(RectTransform card, string name, string detail, string action, bool enabled,
        Action onClick, Color accent, bool selected)
    {
        Image marker = Box(card, "Repère", accent);
        Place(marker.rectTransform, new Vector2(0f, .5f), new Vector2(0f, .5f), new Vector2(7f, 88f), Vector2.right * 18f);

        TMP_Text heading = MakeText(card, name, 40, TextAlignmentOptions.MidlineLeft, true);
        StretchTop(heading.rectTransform, 56f, 42f, ActionWidth + 38f, 20f);
        heading.color = palette.Text;

        TMP_Text description = MakeText(card, detail, 32, TextAlignmentOptions.TopLeft, false);
        Stretch(description.rectTransform, 42f, 12f, ActionWidth + 42f, 79f);
        description.color = palette.Muted;

        Image button = MakeButton(card, action, onClick);
        Place(button.rectTransform, new Vector2(1f, .5f), new Vector2(1f, .5f),
            new Vector2(ActionWidth, ActionHeight), Vector2.left * 17f);
        button.color = enabled && !selected ? palette.Accent : palette.Background;
        button.GetComponent<Button>().interactable = enabled;
        ButtonLabel(button, action, 35).color = enabled && !selected ? palette.Background : selected ? accent : palette.Muted;
    }

    private RectTransform MakeCard(RectTransform row, Color border)
    {
        Image rim = Box(row, "Contour", border);
        Stretch(rim.rectTransform, 0f, 0f, 0f, 0f);
        Image surface = Box(rim.rectTransform, "Carte", palette.Surface);
        Stretch(surface.rectTransform, 2.5f, 2.5f, 2.5f, 2.5f);
        return surface.rectTransform;
    }

    private RectTransform AddListItem(string name, float height)
    {
        RectTransform item = GameUiLayout.Frame(name, content);
        item.gameObject.AddComponent<LayoutElement>().preferredHeight = height;
        return item;
    }

    private Image MakeButton(RectTransform parent, string name, Action onClick)
    {
        Image image = Box(parent, name, Color.white);
        image.raycastTarget = true;
        var button = image.gameObject.AddComponent<Button>();
        button.targetGraphic = image;
        var colors = button.colors;
        colors.highlightedColor = new Color(.89f, .89f, .89f);
        colors.pressedColor = new Color(.75f, .75f, .75f);
        colors.disabledColor = Color.white;
        button.colors = colors;
        button.onClick.AddListener(() => onClick());
        return image;
    }

    private TMP_Text ButtonLabel(Image image, string label, int size)
    {
        TMP_Text text = MakeText(image.rectTransform, label, size, TextAlignmentOptions.Center, true);
        Stretch(text.rectTransform, 12f, 3f, 12f, 3f);
        return text;
    }

    private static TMP_Text MakeText(RectTransform parent, string label, int size, TextAlignmentOptions align, bool bold)
    {
        TMP_Text text = GameUiLayout.Text("Texte", parent, label, size, align);
        if (!bold)
        {
            text.fontStyle = FontStyles.Normal;
            text.textWrappingMode = TextWrappingModes.Normal;
        }
        return text;
    }

    private static Image Box(RectTransform parent, string name, Color color)
    {
        Image image = GameUiLayout.Block(name, parent);
        image.color = color;
        return image;
    }

    private static void Place(RectTransform rect, Vector2 anchor, Vector2 pivot, Vector2 size, Vector2 position)
    {
        rect.anchorMin = rect.anchorMax = anchor;
        rect.pivot = pivot;
        rect.sizeDelta = size;
        rect.anchoredPosition = position;
    }

    private static void Stretch(RectTransform rect, float left, float bottom, float right, float top)
    {
        rect.anchorMin = Vector2.zero;
        rect.anchorMax = Vector2.one;
        rect.offsetMin = new Vector2(left, bottom);
        rect.offsetMax = new Vector2(-right, -top);
    }

    private static void StretchTop(RectTransform rect, float height, float left, float right, float top)
    {
        rect.anchorMin = Vector2.up;
        rect.anchorMax = Vector2.one;
        rect.offsetMin = new Vector2(left, -top - height);
        rect.offsetMax = new Vector2(-right, -top);
    }

    private static void StretchBottom(RectTransform rect, float height, float left, float right, float bottom)
    {
        rect.anchorMin = Vector2.zero;
        rect.anchorMax = Vector2.right;
        rect.offsetMin = new Vector2(left, bottom);
        rect.offsetMax = new Vector2(-right, bottom + height);
    }
}
