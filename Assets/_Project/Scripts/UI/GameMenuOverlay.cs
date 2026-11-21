using System;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

internal sealed class GameMenuOverlay : MonoBehaviour
{
    private enum Page { Menu, Settings, GoldBoost }

    private const string VolumeKey = "CanonPOW.MasterVolume";
    private static readonly Color Gold = new Color(1f, 0.77f, 0.33f);

    private RunRewardManager rewards;
    private RectTransform safeFrame;
    private RectTransform panel;
    private GameObject content;
    private GameObject shade;
    private GameObject badge;
    private RectTransform menuPage;
    private RectTransform settingsPage;
    private RectTransform goldPage;
    private Image surface;
    private Image sliderTrack;
    private Image sliderFill;
    private Image sliderHandle;
    private TMP_Text heading;
    private TMP_Text volumeLabel;
    private TMP_Text volumePercent;
    private TMP_Text boostDetails;
    private TMP_Text boostNotice;
    private TMP_Text claimLabel;
    private TMP_Text bonusLabel;
    private Button toggle;
    private Button settings;
    private Button shop;
    private Button goldBoost;
    private Button back;
    private Button claim;
    private Slider volume;
    private readonly Image[] iconBars = new Image[3];
    private Rect lastSafeArea;
    private Vector2 lastScreen;
    private Page page;
    private bool volumeChanged;
    private bool claimFailed;
    private bool boostReady;
    private float nextBoostRefresh;
    private ThemePalette palette;

    public bool IsOpen => shade.activeSelf;

    public static GameMenuOverlay Create(UIStateSwitcher ui, RunRewardManager rewards)
    {
        var root = new GameObject("Menu rapide", typeof(RectTransform), typeof(Canvas),
            typeof(CanvasScaler), typeof(GraphicRaycaster)) { layer = 5 };
        SceneManager.MoveGameObjectToScene(root, ui.gameObject.scene);
        Canvas canvas = root.GetComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 100;
        GameUiLayout.Scale(root.GetComponent<CanvasScaler>());

        GameMenuOverlay overlay = root.AddComponent<GameMenuOverlay>();
        overlay.Build(ui, rewards);
        return overlay;
    }

    private void Build(UIStateSwitcher ui, RunRewardManager rewardManager)
    {
        rewards = rewardManager;
        content = GameUiLayout.Frame("Interface", transform).gameObject;
        GameUiLayout.Place((RectTransform)content.transform, 0f, 0f, 1f, 1f);
        Button backdrop = GameUiLayout.MakeButton("Écran du menu", content.transform);
        Image dimmer = (Image)backdrop.targetGraphic;
        dimmer.sprite = null;
        dimmer.type = Image.Type.Simple;
        dimmer.color = new Color(0.01f, 0.025f, 0.06f, 0.62f);
        dimmer.raycastTarget = true;
        backdrop.transition = Selectable.Transition.None;
        backdrop.navigation = new Navigation { mode = Navigation.Mode.None };
        backdrop.onClick.AddListener(Close);
        shade = backdrop.gameObject;
        GameUiLayout.Place(dimmer.rectTransform, 0f, 0f, 1f, 1f);

        safeFrame = GameUiLayout.Frame("Zone sûre", content.transform);
        toggle = GameUiLayout.MakeButton("Menu", safeFrame);
        GameUiLayout.Place((RectTransform)toggle.transform, 1f, 1f, 1f, 1f, -144f, -146f, 40f, 42f);
        toggle.onClick.AddListener(Toggle);

        for (int i = 0; i < iconBars.Length; i++)
        {
            Image bar = GameUiLayout.Block("Barre", toggle.transform);
            float top = 28f + i * 21f;
            GameUiLayout.Place(bar.rectTransform, 0f, 1f, 1f, 1f, 25f, -top - 7f, 25f, top);
            iconBars[i] = bar;
        }

        Image notice = GameUiLayout.Block("Bonus disponible", toggle.transform);
        notice.color = new Color(0.94f, 0.22f, 0.28f);
        GameUiLayout.Place(notice.rectTransform, 1f, 1f, 1f, 1f, -28f, -27f, -12f, -13f);
        TMP_Text exclamation = GameUiLayout.Text("Notification", notice.transform, "!", 28f, TextAlignmentOptions.Center);
        exclamation.color = Color.white;
        GameUiLayout.Place(exclamation.rectTransform, 0f, 0f, 1f, 1f);
        badge = notice.gameObject;

        surface = GameUiLayout.Block("Panneau", safeFrame);
        surface.raycastTarget = true;
        panel = surface.rectTransform;
        GameUiLayout.Place(panel, 1f, 1f, 1f, 1f, -624f, -580f, 40f, 166f);
        heading = GameUiLayout.Text("Titre", panel, "Menu", 54f);
        GameUiLayout.Place(heading.rectTransform, 0f, 1f, 1f, 1f, 30f, -102f, 178f, 20f);
        back = CreateButton("Retour", panel, 22f, 74f, () => SetPage(Page.Menu), out _);
        GameUiLayout.Place((RectTransform)back.transform, 1f, 1f, 1f, 1f, -164f, -96f, 23f, 22f);

        menuPage = CreatePage("Raccourcis");
        settings = CreateButton("Paramètres", menuPage, 0f, 86f, () => SetPage(Page.Settings), out _);
        shop = CreateButton("Boutique", menuPage, 99f, 86f, ui.OpenShop, out _);
        goldBoost = CreateButton("Bonus d’or  ×1,25", menuPage, 198f, 86f, () => SetPage(Page.GoldBoost), out bonusLabel);

        settingsPage = CreatePage("Paramètres");
        BuildVolume();

        goldPage = CreatePage("Bonus d’or");
        boostDetails = GameUiLayout.Text("Description du bonus", goldPage,
            "Multiplie par 1,25 les gains en or\nde ta prochaine partie.", 32f);
        boostDetails.textWrappingMode = TextWrappingModes.Normal;
        GameUiLayout.Place(boostDetails.rectTransform, 0f, 1f, 1f, 1f, 8f, -90f, 8f, 0f);
        claim = CreateButton("Activer le bonus", goldPage, 112f, 90f, ClaimBoost, out claimLabel);
        boostNotice = GameUiLayout.Text("État du bonus", goldPage, "", 31f, TextAlignmentOptions.Center);
        GameUiLayout.Place(boostNotice.rectTransform, 0f, 1f, 1f, 1f, 0f, -270f, 0f, 215f);

        SetPage(Page.Menu);
        Close();
        ApplyPalette(ThemeCatalog.Selected);
        ThemeCatalog.Changed += ApplyPalette;
        RefreshBoost();
        Resize();
    }

    private RectTransform CreatePage(string name)
    {
        RectTransform pageFrame = GameUiLayout.Frame(name, panel);
        GameUiLayout.Place(pageFrame, 0f, 0f, 1f, 1f, 24f, 16f, 24f, 112f);
        return pageFrame;
    }

    private static Button CreateButton(string label, Transform parent, float top, float height, Action action,
        out TMP_Text text)
    {
        Button button = GameUiLayout.MakeButton(label, parent);
        GameUiLayout.Place((RectTransform)button.transform, 0f, 1f, 1f, 1f, 0f, -top - height, 0f, top);
        text = GameUiLayout.Text("Libellé", button.transform, label, 37f, TextAlignmentOptions.Center);
        GameUiLayout.Place(text.rectTransform, 0f, 0f, 1f, 1f, 14f, 0f, 14f, 0f);
        button.onClick.AddListener(() => action());
        return button;
    }

    private void BuildVolume()
    {
        volumeLabel = GameUiLayout.Text("Volume du jeu", settingsPage, "Volume du jeu", 40f);
        GameUiLayout.Place(volumeLabel.rectTransform, 0f, 1f, 0.73f, 1f, 8f, -76f, 0f, 10f);
        volumePercent = GameUiLayout.Text("Niveau du volume", settingsPage, "", 40f, TextAlignmentOptions.MidlineRight);
        GameUiLayout.Place(volumePercent.rectTransform, 0.73f, 1f, 1f, 1f, 0f, -76f, 8f, 10f);

        Image touchArea = GameUiLayout.Block("Curseur de volume", settingsPage, false);
        touchArea.color = Color.clear;
        touchArea.raycastTarget = true;
        GameUiLayout.Place(touchArea.rectTransform, 0f, 1f, 1f, 1f, 5f, -174f, 5f, 91f);
        volume = touchArea.gameObject.AddComponent<Slider>();

        sliderTrack = GameUiLayout.Block("Piste", volume.transform);
        GameUiLayout.Place(sliderTrack.rectTransform, 0f, 0.5f, 1f, 0.5f, 18f, -10f, 18f, -10f);
        RectTransform fillArea = GameUiLayout.Frame("Zone de remplissage", sliderTrack.transform);
        GameUiLayout.Place(fillArea, 0f, 0f, 1f, 1f, 10f, 0f, 10f);
        sliderFill = GameUiLayout.Block("Volume", fillArea);
        GameUiLayout.Place(sliderFill.rectTransform, 0f, 0f, 1f, 1f);

        RectTransform handleArea = GameUiLayout.Frame("Zone de déplacement", volume.transform);
        GameUiLayout.Place(handleArea, 0f, 0f, 1f, 1f, 28f, 0f, 28f);
        sliderHandle = GameUiLayout.Block("Poignée", handleArea);
        GameUiLayout.Place(sliderHandle.rectTransform, 0f, 0.5f, 0f, 0.5f, -25f, -25f, -25f, -25f);
        volume.fillRect = sliderFill.rectTransform;
        volume.handleRect = sliderHandle.rectTransform;
        volume.targetGraphic = sliderHandle;
        volume.minValue = 0f;
        volume.maxValue = 100f;
        volume.wholeNumbers = true;

        AudioListener.volume = Mathf.Clamp01(PlayerPrefs.GetFloat(VolumeKey, 1f));
        volume.SetValueWithoutNotify(AudioListener.volume * 100f);
        volumePercent.SetText("{0:0} %", volume.value);
        volume.onValueChanged.AddListener(SetVolume);
    }

    private void SetVolume(float value)
    {
        AudioListener.volume = value / 100f;
        PlayerPrefs.SetFloat(VolumeKey, AudioListener.volume);
        volumePercent.SetText("{0:0} %", value);
        volumeChanged = true;
    }

    private void SaveVolume()
    {
        if (!volumeChanged) return;
        PlayerPrefs.Save();
        volumeChanged = false;
    }

    private void OnApplicationPause(bool paused)
    {
        if (paused) SaveVolume();
    }

    private void Toggle()
    {
        if (IsOpen)
        {
            Close();
            return;
        }
        SetPage(Page.Menu);
        shade.SetActive(true);
        panel.gameObject.SetActive(true);
    }

    public void Close()
    {
        shade.SetActive(false);
        panel.gameObject.SetActive(false);
        SaveVolume();
    }

    private void SetPage(Page next)
    {
        page = next;
        claimFailed = false;
        menuPage.gameObject.SetActive(next == Page.Menu);
        settingsPage.gameObject.SetActive(next == Page.Settings);
        goldPage.gameObject.SetActive(next == Page.GoldBoost);
        back.gameObject.SetActive(next != Page.Menu);
        heading.text = next == Page.Settings ? "Paramètres" : next == Page.GoldBoost ? "Bonus d’or" : "Menu";
        RefreshBoost();
    }

    private void ClaimBoost()
    {
        claimFailed = rewards == null || !rewards.TryClaimGoldBoost();
        RefreshBoost();
    }

    private void RefreshBoost()
    {
        nextBoostRefresh = Time.unscaledTime + 0.2f;
        boostReady = rewards != null && rewards.CanClaimGoldBoost;
        if (badge.activeSelf != boostReady) badge.SetActive(boostReady);
        bonusLabel.color = boostReady ? Gold : palette.Text;
        if (!IsOpen || page != Page.GoldBoost) return;

        claim.interactable = boostReady;
        bool queued = rewards != null && rewards.GoldBoostForNextRun;
        string action = queued ? "Bonus activé" : boostReady ? "Activer le bonus" : "Bonus d’or ×1,25";
        if (claimLabel.text != action) claimLabel.text = action;

        long seconds = rewards != null ? rewards.GoldBoostWaitSeconds : 0L;
        string status;
        if (claimFailed || rewards == null || !rewards.GoldBoostSaveAvailable)
            status = "Sauvegarde indisponible";
        else if (queued)
            status = "Prêt pour la prochaine partie";
        else if (seconds > 0)
            status = $"Disponible dans {seconds / 60:00}:{seconds % 60:00}";
        else
            status = "À récupérer toutes les 5 minutes";
        if (boostNotice.text != status) boostNotice.text = status;
    }

    private void ApplyPalette(ThemeId selected)
    {
        palette = ThemeCatalog.Options[(int)selected].Palette;
        surface.color = palette.Surface;
        Color menuColor = palette.Border;
        menuColor.a = 1f;
        GameUiLayout.TintButton(toggle, menuColor);
        foreach (Image bar in iconBars) bar.color = palette.Text;
        GameUiLayout.TintButton(settings, menuColor);
        GameUiLayout.TintButton(shop, menuColor);
        GameUiLayout.TintButton(goldBoost, menuColor);
        GameUiLayout.TintButton(back, menuColor);
        GameUiLayout.TintButton(claim, Gold);
        settings.GetComponentInChildren<TMP_Text>(true).color = palette.Text;
        shop.GetComponentInChildren<TMP_Text>(true).color = palette.Text;
        back.GetComponentInChildren<TMP_Text>(true).color = palette.Text;
        heading.color = palette.Text;
        claimLabel.color = palette.Background;
        boostDetails.color = palette.Text;
        boostNotice.color = palette.Muted;
        volumeLabel.color = palette.Text;
        volumePercent.color = palette.Accent;
        sliderTrack.color = menuColor;
        sliderFill.color = palette.Accent;
        sliderHandle.color = palette.Text;
        bonusLabel.color = boostReady ? Gold : palette.Text;
    }

    private void LateUpdate()
    {
#if UNITY_EDITOR
        bool capture = AdCaptureDirector.IsPlayingTake;
        if (capture && IsOpen) Close();
        if (content.activeSelf == capture) content.SetActive(!capture);
#endif
        Resize();
        if (IsOpen && Input.GetKeyDown(KeyCode.Escape)) Close();
        if (Time.unscaledTime >= nextBoostRefresh) RefreshBoost();
    }

    private void Resize()
    {
        var screen = new Vector2(Screen.width, Screen.height);
        Rect safe = Screen.safeArea;
        if (safe == lastSafeArea && screen == lastScreen) return;
        GameUiLayout.FitSafeArea(safeFrame, safe, screen);
        lastSafeArea = safe;
        lastScreen = screen;
    }

    private void OnDestroy()
    {
        ThemeCatalog.Changed -= ApplyPalette;
        SaveVolume();
    }
}
