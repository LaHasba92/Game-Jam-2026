using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

// Compose l'accueil et le bilan avec les contrôles et les données des prefabs existants.
internal sealed class GamePanelPresentation
{
    private readonly List<TMP_Text> titles = new List<TMP_Text>();
    private readonly List<TMP_Text> details = new List<TMP_Text>();
    private readonly List<Image> surfaces = new List<Image>();
    private readonly List<Image> rules = new List<Image>();
    private readonly List<Image> shades = new List<Image>();
    private readonly List<Button> secondaryButtons = new List<Button>();
    private readonly List<Button> primaryButtons = new List<Button>();
    private RectTransform homeSafe;
    private RectTransform resultSafe;
    private TMP_Text resultWalls;
    private Button claimButton;
    private TMP_Text claimLabel;
    private RunRewardManager runRewards;
    private Rect lastSafe;
    private Vector2 lastScreen;

    public GamePanelPresentation(GameObject home, GameObject result, UIStateSwitcher ui)
    {
        BuildHome(home, ui);
        BuildResult(result);
        ApplyPalette(ThemeCatalog.Selected);
        ThemeCatalog.Changed += ApplyPalette;
    }

    public void Refresh()
    {
        if (claimButton != null && claimButton.gameObject.activeInHierarchy && runRewards != null)
        {
            claimButton.interactable = runRewards.CanClaimDouble;
            string label = !runRewards.CanClaimDouble && runRewards.LastReward > 0
                ? "Gain doublé"
                : "Doubler le gain";
            if (claimLabel.text != label) claimLabel.text = label;
        }
        Rect safe = Screen.safeArea;
        var screen = new Vector2(Screen.width, Screen.height);
        if (safe == lastSafe && screen == lastScreen) return;
        if (homeSafe != null) GameUiLayout.FitSafeArea(homeSafe, safe, screen);
        if (resultSafe != null) GameUiLayout.FitSafeArea(resultSafe, safe, screen);
        lastSafe = safe;
        lastScreen = screen;
    }

    public void ShowResult(RunResult run)
    {
        if (resultWalls != null) resultWalls.SetText("{0}", run.wallsBroken);
    }

    public void Dispose() => ThemeCatalog.Changed -= ApplyPalette;

    private void BuildHome(GameObject root, UIStateSwitcher ui)
    {
        if (root == null || !root.TryGetComponent(out CanvasScaler scaler)) return;
        TMP_Text title = Find<TMP_Text>(root, "Title");
        TMP_Text record = Find<TMP_Text>(root, "Score_text");
        Button play = Find<Button>(root, "START");
        Button shop = Find<Button>(root, "Shop");
        if (title == null || record == null || play == null || shop == null) return;

        GameUiLayout.Scale(scaler);
        RemoveVisual(root, "border");
        AddShade(root.transform, 0.28f);
        homeSafe = GameUiLayout.Frame("Accueil", root.transform);
        MoveText(title, homeSafe, 130f, TextAlignmentOptions.Center);
        title.text = "CANON POW!";
        title.fontStyle = FontStyles.Bold | FontStyles.Italic;
        GameUiLayout.Place(title.rectTransform, 0f, 0.76f, 1f, 0.9f, 45f, 0f, 45f);

        Image line = GameUiLayout.Block("Soulignement", homeSafe, false);
        GameUiLayout.Place(line.rectTransform, 0.42f, 0.76f, 0.58f, 0.76f, 0f, -3f, 0f, -4f);
        rules.Add(line);
        Image recordCard = GameUiLayout.Block("Record", homeSafe);
        GameUiLayout.Place(recordCard.rectTransform, 0.14f, 0.655f, 0.86f, 0.724f);
        surfaces.Add(recordCard);
        MoveText(record, recordCard.rectTransform, 38f, TextAlignmentOptions.Center);
        GameUiLayout.Place(record.rectTransform, 0f, 0f, 1f, 1f, 28f, 5f, 28f, 5f);

        Image actions = GameUiLayout.Block("Actions", homeSafe);
        GameUiLayout.Place(actions.rectTransform, 0f, 0.045f, 1f, 0.25f, 48f, 0f, 48f);
        surfaces.Add(actions);
        MoveButton(play, actions.rectTransform, "Jouer", true);
        GameUiLayout.Place((RectTransform)play.transform, 0f, 0.51f, 1f, 1f, 26f, 8f, 26f, 26f);
        MoveButton(shop, actions.rectTransform, "Boutique", false);
        GameUiLayout.Place((RectTransform)shop.transform, 0f, 0f, 1f, 0.51f, 26f, 26f, 26f, 9f);
        shop.onClick = new Button.ButtonClickedEvent();
        shop.onClick.AddListener(ui.OpenShop);
    }

    private void BuildResult(GameObject root)
    {
        if (root == null || !root.TryGetComponent(out CanvasScaler scaler)) return;
        TMP_Text distance = Find<TMP_Text>(root, "score");
        TMP_Text earned = Find<TMP_Text>(root, "money");
        TMP_Text best = Find<TMP_Text>(root, "best_score");
        Button retry = Find<Button>(root, "REJOUER");
        Button shop = Find<Button>(root, "Boutique");
        Button doubleReward = Find<Button>(root, "ClaimX2");
        if (distance == null || earned == null || best == null || retry == null || shop == null ||
            doubleReward == null) return;

        GameUiLayout.Scale(scaler);
        RemoveVisual(root, "congrat");
        RemoveVisual(root, "background");
        AddShade(root.transform, 0.72f);
        resultSafe = GameUiLayout.Frame("Bilan", root.transform);
        Image card = GameUiLayout.Block("Carte résultat", resultSafe);
        GameUiLayout.Place(card.rectTransform, 0f, 0.135f, 1f, 0.87f, 48f, 0f, 48f);
        surfaces.Add(card);
        RectTransform content = card.rectTransform;

        TMP_Text heading = GameUiLayout.Text("Titre", content, "TIR TERMINÉ", 76f);
        GameUiLayout.Place(heading.rectTransform, 0f, 0.86f, 1f, 1f, 50f, 0f, 44f, 32f);
        titles.Add(heading);
        Image line = GameUiLayout.Block("Soulignement", content, false);
        GameUiLayout.Place(line.rectTransform, 0f, 0.852f, 0f, 0.852f, 50f, -4f, -148f, -4f);
        rules.Add(line);

        AddMetricLabel(content, "DISTANCE", 0f, 0.57f, 0.736f);
        AddMetricLabel(content, "MURS CASSÉS", 0.57f, 1f, 0.736f);
        MoveText(distance, content, 86f, TextAlignmentOptions.MidlineLeft);
        GameUiLayout.Place(distance.rectTransform, 0f, 0.63f, 0.57f, 0.742f, 50f, 0f, 10f);
        resultWalls = GameUiLayout.Text("Murs cassés", content, "0", 86f);
        GameUiLayout.Place(resultWalls.rectTransform, 0.57f, 0.63f, 1f, 0.742f, 50f, 0f, 50f);
        titles.Add(resultWalls);

        AddMetricLabel(content, "GAIN DE LA PARTIE", 0f, 1f, 0.526f);
        MoveText(earned, content, 49f, TextAlignmentOptions.MidlineLeft);
        GameUiLayout.Place(earned.rectTransform, 0f, 0.45f, 1f, 0.534f, 50f, 0f, 50f);
        MoveText(best, content, 34f, TextAlignmentOptions.MidlineLeft);
        GameUiLayout.Place(best.rectTransform, 0f, 0.366f, 1f, 0.435f, 50f, 0f, 50f);
        titles.Remove(best);
        details.Add(best);

        MoveButton(retry, content, "Rejouer", true);
        GameUiLayout.Place((RectTransform)retry.transform, 0f, 0.207f, 1f, 0.32f, 44f, 0f, 44f);
        MoveButton(shop, content, "Boutique", false);
        GameUiLayout.Place((RectTransform)shop.transform, 0f, 0.065f, 0.5f, 0.181f, 44f, 0f, 13f);
        claimLabel = MoveButton(doubleReward, content, "Doubler le gain", false);
        claimButton = doubleReward;
        runRewards = root.GetComponent<RunRewardManager>();
        GameUiLayout.Place((RectTransform)doubleReward.transform, 0.5f, 0.065f, 1f, 0.181f, 13f, 0f, 44f);
    }

    private void AddMetricLabel(Transform parent, string text, float left, float right, float bottom)
    {
        TMP_Text label = GameUiLayout.Text(text, parent, text, 30f);
        GameUiLayout.Place(label.rectTransform, left, bottom, right, bottom + 0.064f, 50f, 0f, 16f);
        details.Add(label);
    }

    private void AddShade(Transform root, float opacity)
    {
        Image shade = GameUiLayout.Block("Voile", root, false);
        shade.color = new Color(0f, 0f, 0f, opacity);
        GameUiLayout.Place(shade.rectTransform, 0f, 0f, 1f, 1f);
        shades.Add(shade);
    }

    private void MoveText(TMP_Text text, Transform parent, float size, TextAlignmentOptions align)
    {
        text.transform.SetParent(parent, false);
        GameUiLayout.StyleText(text, size, align);
        titles.Add(text);
    }

    private TMP_Text MoveButton(Button button, Transform parent, string label, bool primary)
    {
        button.transform.SetParent(parent, false);
        if (button.targetGraphic is Image target)
        {
            target.overrideSprite = null;
            target.material = null;
        }
        TMP_Text text = button.GetComponentInChildren<TMP_Text>(true);
        if (text == null) text = GameUiLayout.Text("Libellé", button.transform, label, 42f);
        text.transform.SetParent(button.transform, false);
        foreach (Transform child in button.transform)
            if (child != text.transform) child.gameObject.SetActive(false);
        text.gameObject.SetActive(true);
        text.text = label;
        GameUiLayout.StyleText(text, primary ? 52f : 40f, TextAlignmentOptions.Center);
        GameUiLayout.Place(text.rectTransform, 0f, 0f, 1f, 1f, 14f, 5f, 14f, 5f);
        if (primary) primaryButtons.Add(button);
        else secondaryButtons.Add(button);
        return text;
    }

    private void ApplyPalette(ThemeId selected)
    {
        ThemePalette palette = ThemeCatalog.Options[(int)selected].Palette;
        foreach (TMP_Text text in titles) if (text != null) text.color = palette.Text;
        foreach (TMP_Text text in details) if (text != null) text.color = palette.Muted;
        foreach (Image image in rules) if (image != null) image.color = palette.Accent;
        foreach (Image image in surfaces)
        {
            if (image == null) continue;
            Color color = palette.Surface;
            color.a = 0.97f;
            image.color = color;
        }
        foreach (Image shade in shades)
        {
            if (shade == null) continue;
            Color color = palette.Background;
            color.a = shade.color.a;
            shade.color = color;
        }
        ApplyButtons(primaryButtons, palette.Accent, palette.Background);
        ApplyButtons(secondaryButtons, palette.Border, palette.Text);
    }

    private static void ApplyButtons(IEnumerable<Button> buttons, Color background, Color foreground)
    {
        foreach (Button button in buttons)
        {
            if (button == null) continue;
            GameUiLayout.TintButton(button, background);
            TMP_Text label = button.GetComponentInChildren<TMP_Text>(true);
            if (label != null) label.color = foreground;
        }
    }

    private static T Find<T>(GameObject root, string name) where T : Component
    {
        Transform child = root.transform.Find(name);
        return child != null ? child.GetComponent<T>() : null;
    }

    private static void RemoveVisual(GameObject root, string name)
    {
        Transform child = root.transform.Find(name);
        if (child != null) child.gameObject.SetActive(false);
    }
}
