using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

internal sealed class GameplayHud : MonoBehaviour
{
    private ProjectileController projectile;
    private LauncherController launcher;
    private FeedbackManager feedback;
    private GameState state;
    private ThemeId theme;
    private ThemePalette palette;
    private RectTransform safeFrame;
    private Rect lastSafeArea;
    private Vector2 lastScreen;
    private RectTransform flight;
    private Image flightCard;
    private Image flightRule;
    private Image divider;
    private TMP_Text distanceLabel;
    private TMP_Text distance;
    private TMP_Text wallsLabel;
    private TMP_Text walls;
    private RectTransform aim;
    private Image aimCard;
    private TMP_Text aimLabel;
    private TMP_Text aimValue;
    private Image track;
    private Image gaugeFill;
    private Image targetZone;
    private Image targetMark;
    private TMP_Text aimAngles;
    private TMP_Text aimInstruction;
    private Button skip;
    private TMP_Text skipLabel;
    private CanvasGroup popup;
    private Image popupCard;
    private TMP_Text popupLabel;
    private float popupStart;
    private float popupDuration;
    private int lastDistance = -1;
    private int lastWalls = -1;

    public static GameplayHud Create(Scene scene, ProjectileController projectile)
    {
        var root = new GameObject("HUD", typeof(RectTransform), typeof(Canvas), typeof(CanvasScaler),
            typeof(GraphicRaycaster)) { layer = 5 };
        SceneManager.MoveGameObjectToScene(root, scene);
        Canvas canvas = root.GetComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 55;
        GameUiLayout.Scale(root.GetComponent<CanvasScaler>());
        var hud = root.AddComponent<GameplayHud>();
        hud.Build(projectile);
        return hud;
    }

    public void Show(GameState next)
    {
        state = next;
        popupDuration = 0f;
        UpdateVisibility();
    }

    private void Build(ProjectileController ball)
    {
        projectile = ball;
        Transform gameplay = ball.transform.root;
        launcher = gameplay.GetComponentInChildren<LauncherController>();
        feedback = gameplay.GetComponentInChildren<FeedbackManager>();
        if (launcher != null) launcher.UseLegacyGauge = false;
        if (feedback != null)
        {
            feedback.UseLegacyHud = false;
            feedback.PopupShown += ShowPopup;
        }

        safeFrame = GameUiLayout.Frame("Zone sûre", transform);
        BuildFlight();
        BuildAim();
        ApplyPalette(ThemeCatalog.Selected);
        ThemeCatalog.Changed += ApplyPalette;
        UpdateVisibility();
    }

    private void BuildFlight()
    {
        flight = GameUiLayout.Frame("Vol", safeFrame);
        GameUiLayout.Place(flight, 0f, 0f, 1f, 1f);
        flightCard = GameUiLayout.Block("Progression", flight);
        GameUiLayout.Place(flightCard.rectTransform, 0f, 1f, 1f, 1f, 48f, -210f, 48f, 42f);

        flightRule = GameUiLayout.Block("Soulignement", flightCard.rectTransform, false);
        GameUiLayout.Place(flightRule.rectTransform, 0f, 1f, 0f, 1f, 30f, -5f, -109f);
        divider = GameUiLayout.Block("Séparateur", flightCard.rectTransform, false);
        GameUiLayout.Place(divider.rectTransform, 0.57f, 0f, 0.57f, 1f, 0f, 30f, -2f, 30f);

        distanceLabel = GameUiLayout.Text("Légende distance", flightCard.rectTransform, "DISTANCE", 29f);
        GameUiLayout.Place(distanceLabel.rectTransform, 0f, 1f, 0.57f, 1f, 34f, -68f, 28f, 24f);
        distance = GameUiLayout.Text("Distance", flightCard.rectTransform, "0 m", 60f);
        GameUiLayout.Place(distance.rectTransform, 0f, 0f, 0.57f, 0f, 32f, 15f, 22f, -106f);

        wallsLabel = GameUiLayout.Text("Légende murs", flightCard.rectTransform, "MURS CASSÉS", 29f);
        GameUiLayout.Place(wallsLabel.rectTransform, 0.57f, 1f, 1f, 1f, 32f, -68f, 32f, 24f);
        walls = GameUiLayout.Text("Murs cassés", flightCard.rectTransform, "0", 60f);
        GameUiLayout.Place(walls.rectTransform, 0.57f, 0f, 1f, 0f, 30f, 15f, 30f, -106f);

        skip = GameUiLayout.MakeButton("Passer", flight);
        GameUiLayout.Place((RectTransform)skip.transform, 1f, 0f, 1f, 0f, -291f, 42f, 48f, -155f);
        skip.onClick.AddListener(() =>
        {
            if (projectile != null && projectile.CanSkip) projectile.Skip();
        });
        skipLabel = GameUiLayout.Text("Libellé", skip.transform, "Passer", 38f, TextAlignmentOptions.Center);
        GameUiLayout.Place(skipLabel.rectTransform, 0f, 0f, 1f, 1f, 20f, 5f, 20f, 5f);

        popupCard = GameUiLayout.Block("Combo", flight);
        GameUiLayout.Place(popupCard.rectTransform, 0.16f, 0.60f, 0.84f, 0.60f, 0f, -61f, 0f, -61f);
        popup = popupCard.gameObject.AddComponent<CanvasGroup>();
        popup.interactable = false;
        popup.blocksRaycasts = false;
        popup.alpha = 0f;
        popupLabel = GameUiLayout.Text("Impact", popupCard.rectTransform, "", 55f, TextAlignmentOptions.Center);
        GameUiLayout.Place(popupLabel.rectTransform, 0f, 0f, 1f, 1f, 25f, 5f, 25f, 5f);
    }

    private void BuildAim()
    {
        aim = GameUiLayout.Frame("Visée", safeFrame);
        GameUiLayout.Place(aim, 0f, 0f, 1f, 1f);
        aimCard = GameUiLayout.Block("Puissance du tir", aim);
        GameUiLayout.Place(aimCard.rectTransform, 0f, 0f, 1f, 0f, 48f, 42f, 48f, -253f);
        aimLabel = GameUiLayout.Text("Légende", aimCard.rectTransform, "PUISSANCE", 31f);
        GameUiLayout.Place(aimLabel.rectTransform, 0f, 1f, 0.55f, 1f, 32f, -70f, 0f, 22f);
        aimValue = GameUiLayout.Text("Valeur", aimCard.rectTransform, "", 42f, TextAlignmentOptions.MidlineRight);
        GameUiLayout.Place(aimValue.rectTransform, 0.55f, 1f, 1f, 1f, 0f, -72f, 31f, 19f);

        track = GameUiLayout.Block("Jauge", aimCard.rectTransform, false);
        GameUiLayout.Place(track.rectTransform, 0f, 0f, 1f, 0f, 32f, 82f, 32f, -123f);
        targetZone = GameUiLayout.Block("Zone parfaite", track.rectTransform, false);
        gaugeFill = GameUiLayout.Block("Remplissage", track.rectTransform, false);
        targetMark = GameUiLayout.Block("Repère parfait", track.rectTransform, false);
        float threshold = launcher != null ? launcher.PerfectThreshold : 0.88f;
        GameUiLayout.Place(targetZone.rectTransform, threshold, 0f, 1f, 1f);
        GameUiLayout.Place(targetMark.rectTransform, threshold, 0f, threshold, 1f, 0f, -4f, -4f, -4f);

        aimAngles = GameUiLayout.Text("Angles", aimCard.rectTransform, "", 30f);
        aimInstruction = GameUiLayout.Text("Commande", aimCard.rectTransform, "Maintiens et glisse pour viser", 30f);
        GameUiLayout.Place(aimAngles.rectTransform, 0f, 0f, 1f, 0f, 32f, 17f, 32f, -63f);
        GameUiLayout.Place(aimInstruction.rectTransform, 0f, 0f, 1f, 0f, 32f, 17f, 32f, -63f);
    }

    private void LateUpdate()
    {
        Rect safe = Screen.safeArea;
        var screen = new Vector2(Screen.width, Screen.height);
        if (safe != lastSafeArea || screen != lastScreen)
        {
            GameUiLayout.FitSafeArea(safeFrame, safe, screen);
            lastSafeArea = safe;
            lastScreen = screen;
        }

        UpdateVisibility();
        if (flight.gameObject.activeSelf) UpdateFlight();
        if (aim.gameObject.activeSelf) UpdateAim();
    }

    private void UpdateVisibility()
    {
        bool capture = false;
#if UNITY_EDITOR
        capture = AdCaptureDirector.IsPlayingTake;
#endif
        SetActive(flight.gameObject, !capture && state == GameState.Flying);
        SetActive(aim.gameObject, !capture && state == GameState.Aiming && launcher != null);
    }

    private void UpdateFlight()
    {
        if (projectile == null) return;
        int meters = Mathf.Max(0, Mathf.RoundToInt(projectile.Distance));
        if (meters != lastDistance)
        {
            distance.text = $"{meters:N0} m";
            lastDistance = meters;
        }
        if (projectile.WallsBroken != lastWalls)
        {
            walls.SetText("{0}", projectile.WallsBroken);
            lastWalls = projectile.WallsBroken;
        }
        SetActive(skip.gameObject, projectile.CanSkip);

        float progress = popupDuration > 0f ? (Time.unscaledTime - popupStart) / popupDuration : 1f;
        popup.alpha = 1f - Mathf.InverseLerp(0.58f, 1f, progress);
        float size = Mathf.Lerp(0.84f, 1.06f, Mathf.Clamp01(progress * 8f)) -
            Mathf.Lerp(0f, 0.06f, Mathf.InverseLerp(0.13f, 0.37f, progress));
        popup.transform.localScale = Vector3.one * size;
    }

    private void UpdateAim()
    {
        bool aiming = launcher.IsAiming;
        float charge = aiming ? launcher.Gauge : 0f;
        bool perfect = aiming && launcher.IsPerfect;
        gaugeFill.rectTransform.anchorMax = new Vector2(charge, 1f);
        gaugeFill.color = perfect ? PerfectColor() : palette.Accent;
        aimValue.color = perfect ? PerfectColor() : palette.Text;
        if (perfect)
            aimValue.text = "PARFAIT";
        else
            aimValue.SetText("{0:0} %", charge * 100f);
        aimAngles.SetText("Angle {0:0}°    Direction {1:0}°", launcher.Elevation, launcher.Side);
        SetActive(aimAngles.gameObject, aiming);
        SetActive(aimInstruction.gameObject, !aiming);
    }

    private void ShowPopup(string message, float duration)
    {
        popupLabel.text = message;
        popupStart = Time.unscaledTime;
        popupDuration = duration;
    }

    private void ApplyPalette(ThemeId selected)
    {
        theme = selected;
        palette = ThemeCatalog.Options[(int)selected].Palette;
        Color panel = palette.Surface;
        panel.a = 0.94f;
        flightCard.color = aimCard.color = popupCard.color = panel;
        flightRule.color = walls.color = popupLabel.color = palette.Accent;
        divider.color = track.color = palette.Border;
        aimValue.color = distance.color = skipLabel.color = palette.Text;
        aimLabel.color = wallsLabel.color = distanceLabel.color = aimAngles.color = aimInstruction.color = palette.Muted;
        targetZone.color = Color.Lerp(palette.Border, PerfectColor(), 0.42f);
        targetMark.color = PerfectColor();
        GameUiLayout.TintButton(skip, palette.Surface);
    }

    private Color PerfectColor()
    {
        return theme == ThemeId.Cyberpunk ? palette.Secondary : (Color)new Color32(113, 239, 166, 255);
    }

    private static void SetActive(GameObject target, bool active)
    {
        if (target.activeSelf != active) target.SetActive(active);
    }

    private void OnDestroy()
    {
        ThemeCatalog.Changed -= ApplyPalette;
        if (launcher != null) launcher.UseLegacyGauge = true;
        if (feedback != null)
        {
            feedback.UseLegacyHud = true;
            feedback.PopupShown -= ShowPopup;
        }
    }
}
