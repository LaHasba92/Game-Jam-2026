#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

/// <summary>Affiche les informations du tir et le carton final dans la vue Game.</summary>
public sealed class AdCaptureOverlay : MonoBehaviour
{
    private const string StoreArt = "Assets/Scripts/Capture/Editor/Art/";
    private static readonly Color Navy = new Color32(15, 23, 38, 255);
    private static readonly Color Gold = new Color32(255, 193, 64, 255);
    private static readonly Color Mint = new Color32(110, 231, 159, 255);
    private static readonly Color Ivory = new Color32(255, 249, 231, 255);
    private static readonly Color Coral = new Color32(255, 126, 94, 255);
    private const float ComboDuration = 0.68f;

    private Canvas canvas;
    private CanvasGroup hookTitle;
    private CanvasGroup trailerHook;
    private RectTransform trailerHookPanel;
    private Text trailerHookTitle;
    private Text trailerHookSubtitle;
    private GameObject challenge;
    private GameObject gauge;
    private GameObject flightHud;
    private GameObject endCard;
    private RectTransform needle;
    private Text gaugeLabel;
    private Text wallLabel;
    private Text distanceLabel;
    private Text comboLabel;
    private CanvasGroup challengeGroup;
    private RectTransform weakBanner;
    private Font font;
    private int displayedWalls = -1;
    private int displayedMeters = -1;
    private float comboTime;
    private float weakTime;

    public void ShowHook(float elapsed)
    {
        EnsureCanvas();
        trailerHook.gameObject.SetActive(false);
        SetView(hookTitle.gameObject);

        float fadeIn = Mathf.SmoothStep(0f, 1f, Mathf.InverseLerp(0f, 0.32f, elapsed));
        float fadeOut = Mathf.SmoothStep(1f, 0f, Mathf.InverseLerp(2.2f, 2.65f, elapsed));
        hookTitle.alpha = fadeIn * fadeOut;
    }

    public void ShowTrailerHook(string title, string subtitle, float elapsed, float duration)
    {
        if (elapsed < 0f || elapsed >= duration || string.IsNullOrWhiteSpace(title))
        {
            if (trailerHook != null) trailerHook.gameObject.SetActive(false);
            return;
        }

        EnsureCanvas();
        hookTitle.gameObject.SetActive(false);
        trailerHook.gameObject.SetActive(true);
        canvas.enabled = true;

        bool hasSubtitle = !string.IsNullOrWhiteSpace(subtitle);
        trailerHookTitle.text = title;
        trailerHookTitle.rectTransform.anchoredPosition = new Vector2(5f, hasSubtitle ? 26f : 0f);
        trailerHookSubtitle.gameObject.SetActive(hasSubtitle);
        trailerHookSubtitle.text = subtitle ?? string.Empty;

        float enter = Mathf.SmoothStep(0f, 1f, Mathf.InverseLerp(0f, Mathf.Min(0.22f, duration * 0.25f), elapsed));
        float exit = Mathf.SmoothStep(0f, 1f, Mathf.InverseLerp(0f, Mathf.Min(0.26f, duration * 0.25f), duration - elapsed));
        trailerHook.alpha = enter * exit;
        trailerHookPanel.anchoredPosition = Vector2.down * (22f * (1f - enter) + 10f * (1f - exit));
    }

    public void ShowChallenge(float elapsed)
    {
        EnsureCanvas();
        SetView(challenge);

        float enter = Mathf.SmoothStep(0f, 1f, Mathf.InverseLerp(0.7f, 0.92f, elapsed));
        challengeGroup.alpha = enter;
        ((RectTransform)challengeGroup.transform).anchoredPosition = Vector2.down * (18f * (1f - enter));
    }

    public void ShowGauge(float value, bool perfect)
    {
        EnsureCanvas();
        SetView(gauge);

        Vector2 needleAnchor = new Vector2(Mathf.Clamp01(value), 0.5f);
        needle.anchorMin = needleAnchor;
        needle.anchorMax = needleAnchor;
        gaugeLabel.text = perfect ? "TIR PARFAIT !" : "PUISSANCE";
        gaugeLabel.color = perfect ? Mint : Ivory;
    }

    public void ShowFlight(int wallsBroken, float distance)
    {
        EnsureCanvas();
        SetView(flightHud);

        if (wallsBroken > displayedWalls && wallsBroken > 0 && !weakBanner.gameObject.activeSelf)
        {
            comboLabel.text = wallsBroken == 1 ? "POW!" : $"COMBO ×{wallsBroken}";
            comboTime = 0f;
            comboLabel.color = Gold;
            comboLabel.rectTransform.localScale = Vector3.one;
            comboLabel.rectTransform.anchoredPosition = Vector2.zero;
            comboLabel.gameObject.SetActive(true);
        }

        SetWallCount(wallsBroken);
        int meters = Mathf.FloorToInt(Mathf.Max(0f, distance));
        if (meters != displayedMeters)
        {
            displayedMeters = meters;
            distanceLabel.text = $"{meters} m";
        }
    }

    public void ShowWeakOutcome(int wallsBroken)
    {
        EnsureCanvas();
        SetView(flightHud);
        SetWallCount(wallsBroken);
        comboLabel.gameObject.SetActive(false);
        if (weakBanner.gameObject.activeSelf) return;

        weakTime = 0f;
        weakBanner.localScale = Vector3.one * 0.88f;
        weakBanner.gameObject.SetActive(true);
    }

    public void ShowEndCard()
    {
        EnsureCanvas();
        trailerHook.gameObject.SetActive(false);
        SetView(endCard);
    }

    public void Clear()
    {
        if (canvas == null) return;

        canvas.enabled = false;
        hookTitle.gameObject.SetActive(false);
        trailerHook.gameObject.SetActive(false);
        challenge.SetActive(false);
        gauge.SetActive(false);
        flightHud.SetActive(false);
        endCard.SetActive(false);
        comboLabel.gameObject.SetActive(false);
        weakBanner.gameObject.SetActive(false);
        displayedWalls = -1;
        displayedMeters = -1;
        distanceLabel.text = "0 m";
    }

    private void OnDisable() => Clear();

    private void OnDestroy()
    {
        if (canvas != null) Destroy(canvas.gameObject);
    }

    private void Update()
    {
        if (canvas == null || !canvas.enabled || !flightHud.activeSelf) return;

        if (comboLabel.gameObject.activeSelf)
        {
            comboTime += AdCaptureDirector.PresentationDelta;
            float punch = Mathf.Sin(Mathf.Clamp01(comboTime / 0.26f) * Mathf.PI);
            comboLabel.rectTransform.localScale = Vector3.one * (1f + 0.16f * punch);
            comboLabel.rectTransform.anchoredPosition = Vector2.up * (38f * comboTime);
            Color fade = Gold;
            fade.a = 1f - Mathf.InverseLerp(0.37f, ComboDuration, comboTime);
            comboLabel.color = fade;
            if (comboTime >= ComboDuration) comboLabel.gameObject.SetActive(false);
        }

        if (weakBanner.gameObject.activeSelf && weakTime < 0.24f)
        {
            weakTime += AdCaptureDirector.PresentationDelta;
            float progress = Mathf.Clamp01(weakTime / 0.24f);
            float scale = Mathf.Lerp(0.88f, 1f, progress) + Mathf.Sin(progress * Mathf.PI) * 0.08f;
            weakBanner.localScale = Vector3.one * scale;
        }
    }

    private void SetView(GameObject view)
    {
        hookTitle.gameObject.SetActive(view == hookTitle.gameObject);
        challenge.SetActive(view == challenge);
        gauge.SetActive(view == gauge);
        flightHud.SetActive(view == flightHud);
        endCard.SetActive(view == endCard);
        canvas.enabled = true;
    }

    private void SetWallCount(int wallsBroken)
    {
        if (displayedWalls == wallsBroken) return;
        displayedWalls = wallsBroken;
        wallLabel.text = wallsBroken == 1 ? "1 MUR" : $"{wallsBroken} MURS";
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
        CreateHook(root.transform);
        CreateChallenge(root.transform);
        CreateGauge(root.transform);
        CreateFlightHud(root.transform);
        CreateEndCard(root.transform);
        CreateTrailerHook(root.transform);
        Clear();
    }

    private void CreateHook(Transform parent)
    {
        RectTransform panel = ImageAt("Accroche", parent, new Color32(15, 23, 38, 203),
            new Vector2(0.5f, 0.22f), new Vector2(432f, 91f), Vector2.zero).rectTransform;
        hookTitle = panel.gameObject.AddComponent<CanvasGroup>();
        ImageAt("Accent", panel, Gold, new Vector2(0f, 0.5f), new Vector2(5f, 91f), Vector2.right * 2.5f);
        TextAt("Un seul tir", panel, "UN SEUL TIR.", Ivory, 55, new Vector2(394f, 80f), Vector2.zero);
    }

    private void CreateTrailerHook(Transform parent)
    {
        trailerHookPanel = ImageAt("Accroche du montage", parent, new Color32(15, 23, 38, 232),
            new Vector2(0.5f, 0.34f), new Vector2(820f, 176f), Vector2.zero).rectTransform;
        trailerHook = trailerHookPanel.gameObject.AddComponent<CanvasGroup>();
        ImageAt("Accent", trailerHookPanel, Gold, new Vector2(0f, 0.5f), new Vector2(8f, 176f), Vector2.right * 4f);

        trailerHookTitle = TextAt("Titre", trailerHookPanel, string.Empty, Ivory, 68,
            new Vector2(748f, 84f), new Vector2(5f, 26f));
        trailerHookTitle.horizontalOverflow = HorizontalWrapMode.Wrap;
        trailerHookTitle.verticalOverflow = VerticalWrapMode.Truncate;
        trailerHookTitle.resizeTextForBestFit = true;
        trailerHookTitle.resizeTextMinSize = 42;
        trailerHookTitle.resizeTextMaxSize = 68;

        trailerHookSubtitle = TextAt("Sous-titre", trailerHookPanel, string.Empty, Gold, 36,
            new Vector2(748f, 52f), new Vector2(5f, -43f));
        trailerHookSubtitle.horizontalOverflow = HorizontalWrapMode.Wrap;
        trailerHookSubtitle.verticalOverflow = VerticalWrapMode.Truncate;
        trailerHookSubtitle.resizeTextForBestFit = true;
        trailerHookSubtitle.resizeTextMinSize = 25;
        trailerHookSubtitle.resizeTextMaxSize = 36;
    }

    private void CreateChallenge(Transform parent)
    {
        RectTransform panel = ImageAt("À toi", parent, new Color32(15, 23, 38, 242),
            new Vector2(0.5f, 0.205f), new Vector2(624f, 102f), Vector2.zero).rectTransform;
        challenge = panel.gameObject;
        challengeGroup = challenge.AddComponent<CanvasGroup>();
        ImageAt("Accent", panel, Gold, new Vector2(0f, 0.5f), new Vector2(9f, 102f), Vector2.right * 4.5f);
        TextAt("Invitation", panel, "À TOI DE LE FINIR.", Ivory, 54, new Vector2(584f, 90f), Vector2.zero);
    }

    private void CreateFlightHud(Transform parent)
    {
        RectTransform hud = RectAt("Vol", parent, Vector2.zero, Vector2.zero, Vector2.zero);
        hud.anchorMin = Vector2.zero;
        hud.anchorMax = Vector2.one;
        flightHud = hud.gameObject;

        RectTransform stats = ImageAt("Progression", hud, Navy, new Vector2(0.5f, 0.916f),
            new Vector2(446f, 86f), Vector2.zero).rectTransform;
        ImageAt("Accent", stats, Gold, new Vector2(0.5f, 1f), new Vector2(446f, 5f), Vector2.down * 2.5f);
        ImageAt("Séparateur", stats, new Color32(83, 96, 109, 255),
            new Vector2(0.5f, 0.5f), new Vector2(2f, 33f), Vector2.zero);
        wallLabel = TextAt("Murs", stats, "0 MURS", Gold, 39, new Vector2(190f, 64f), new Vector2(-106f, 0f));
        distanceLabel = TextAt("Distance", stats, "0 m", Ivory, 39, new Vector2(190f, 64f), new Vector2(106f, 0f));

        RectTransform combo = RectAt("Combo", hud, new Vector2(0.5f, 0.78f), new Vector2(700f, 112f), Vector2.zero);
        comboLabel = TextAt("Casse", combo, string.Empty, Gold, 84, new Vector2(686f, 106f), Vector2.zero);
        AddShadow(comboLabel, new Vector2(5f, -6f));
        comboLabel.gameObject.SetActive(false);

        weakBanner = ImageAt("Tir trop faible", hud, Navy, new Vector2(0.5f, 0.685f),
            new Vector2(558f, 109f), Vector2.zero).rectTransform;
        ImageAt("Accent", weakBanner, Coral, new Vector2(0f, 0.5f), new Vector2(8f, 109f), Vector2.right * 4f);
        TextAt("Résultat du tir", weakBanner, "TIR TROP FAIBLE", Coral, 50, new Vector2(520f, 90f), Vector2.zero);
        weakBanner.gameObject.SetActive(false);
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
        AddShadow(pow, new Vector2(9f, -11f));
        ImageAt("Soulignement", logo, Gold, new Vector2(0.5f, 0f),
            new Vector2(330f, 13f), new Vector2(0f, 18f));

        RectTransform stores = RectAt("Plateformes", frame, new Vector2(0.5f, 0.20f),
            new Vector2(840f, 114f), Vector2.zero);
        CreateStoreBadge(stores, "App Store", "AppStore.png", -205f);
        CreateStoreBadge(stores, "Google Play", "GooglePlay.png", 205f, 192f / 250f);
        endCard.SetActive(false);
    }

    private void CreateStoreBadge(RectTransform parent, string label, string file, float x, float contentHeightFraction = 1f)
    {
        Texture2D texture = AssetDatabase.LoadAssetAtPath<Texture2D>(StoreArt + file);
        Vector2 position = new Vector2(x, 0f);
        if (texture == null)
        {
            TextAt(label, parent, label, Ivory, 49, new Vector2(348f, 76f), position);
            return;
        }

        float scale = Mathf.Min(375f / texture.width, 106f / (texture.height * contentHeightFraction));
        Vector2 size = new Vector2(texture.width, texture.height) * scale;
        var badge = RectAt(label, parent, new Vector2(0.5f, 0.5f), size, position)
            .gameObject.AddComponent<RawImage>();
        badge.texture = texture;
        badge.raycastTarget = false;
    }

    private static void AddShadow(Text text, Vector2 offset)
    {
        Shadow shadow = text.gameObject.AddComponent<Shadow>();
        shadow.effectColor = new Color32(7, 12, 21, 255);
        shadow.effectDistance = offset;
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
