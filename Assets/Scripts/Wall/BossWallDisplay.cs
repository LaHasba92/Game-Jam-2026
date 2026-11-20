using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

/// <summary>Affiche les PV et l'issue de l'impact sur le mur d'acier.</summary>
[DefaultExecutionOrder(100)]
public sealed class BossWallDisplay : MonoBehaviour
{
    private static readonly Color ReadyColor = new Color32(255, 198, 75, 255);
    private static readonly Color BreakColor = new Color32(255, 225, 126, 255);
    private static readonly Color ResistColor = new Color32(255, 109, 82, 255);
    private const float DisplayRange = 70f;
    private const float ImpactDuration = 1.2f;

    private Canvas canvas;
    private RectTransform canvasRect;
    private RectTransform panel;
    private CanvasGroup panelGroup;
    private Image flash;
    private Image accent;
    private Image healthFill;
    private Text title;
    private Text counter;
    private Camera viewCamera;
    private Vector3 wallAnchor;
    private int maxHealth;
    private int currentHealth;
    private float impactTime;
    private float impactDuration = ImpactDuration;
    private Color impactColor;
    private bool shown;
    private bool impactPlaying;

    public void Show(Vector3 anchor, int maxHealth)
    {
        if (canvas == null) CreateDisplay();

        wallAnchor = anchor;
        this.maxHealth = Mathf.Max(1, maxHealth);
        currentHealth = this.maxHealth;
        shown = true;
        impactPlaying = false;
        canvas.enabled = false;
        panel.localScale = Vector3.one;
        panelGroup.alpha = 1f;
        flash.color = Color.clear;
        SetReadout("MUR D’ACIER", currentHealth, ReadyColor);
    }

    public void Impact(int health, bool broken, bool rolling)
    {
        if (!shown) return;

        impactTime = 0f;
        impactPlaying = true;
        currentHealth = health;
        impactColor = broken ? BreakColor : ResistColor;
        string message = broken ? "ACIER PULVÉRISÉ" : rolling ? "IMPACT EN ROULANT" : "IMPACT TROP FAIBLE";
        impactDuration = ImpactDuration;
#if UNITY_EDITOR
        if (!broken && AdCaptureDirector.HoldBossImpact)
        {
            impactDuration = 3.7f;
            if (!rolling) message = $"PLUS QUE {health} PV";
        }
#endif
        SetReadout(message, health, impactColor);
        canvas.enabled = true;
        RenderImpact();
    }

    public void Clear()
    {
        shown = false;
        impactPlaying = false;
        if (canvas != null) canvas.enabled = false;
    }

    private void OnDisable() => Clear();

    private void OnDestroy()
    {
        if (canvas != null) Destroy(canvas.gameObject);
    }

    private void LateUpdate()
    {
        if (!shown) return;

#if UNITY_EDITOR
        if (AdCaptureDirector.HideIntroBossHud)
        {
            canvas.enabled = false;
            return;
        }
#endif

        if (impactPlaying)
        {
#if UNITY_EDITOR
            impactTime += AdCaptureDirector.HoldBossImpact ? AdCaptureDirector.PresentationDelta : Time.unscaledDeltaTime;
#else
            impactTime += Time.unscaledDeltaTime;
#endif
            if (impactTime < impactDuration)
            {
                RenderImpact();
                return;
            }
            if (currentHealth == 0)
            {
                Clear();
                return;
            }

            impactPlaying = false;
            flash.color = Color.clear;
            panelGroup.alpha = 1f;
            SetReadout("MUR D’ACIER", currentHealth, ReadyColor);
        }

        if (GameManager.Instance != null && GameManager.Instance.CurrentState != GameState.Flying
            && GameManager.Instance.CurrentState != GameState.Aiming)
        {
            canvas.enabled = false;
            return;
        }

        if (viewCamera == null || !viewCamera.isActiveAndEnabled)
            viewCamera = Camera.main;

        if (viewCamera == null)
        {
            canvas.enabled = false;
            return;
        }

        Vector3 viewport = viewCamera.WorldToViewportPoint(wallAnchor);
        canvas.enabled = viewport.z > 0f && viewport.z <= DisplayRange &&
                         viewport.x >= 0f && viewport.x <= 1f && viewport.y >= 0f;
        if (canvas.enabled)
        {
            Vector2 screen = viewCamera.ViewportToScreenPoint(viewport);
            PositionPanel(screen, Vector3.one);
        }
    }

    private void SetReadout(string label, int health, Color color)
    {
        health = Mathf.Clamp(health, 0, maxHealth);
        title.text = label;
        title.color = color;
        counter.text = $"{health} / {maxHealth} PV";
        accent.color = color;
        healthFill.color = color;
        healthFill.rectTransform.anchorMax = new Vector2((float)health / maxHealth, 1f);
    }

    private void RenderImpact()
    {
        Rect safe = Screen.safeArea;
        float pulse = 1f + 0.16f * (1f - Mathf.Clamp01(impactTime / 0.26f));
        PositionPanel(new Vector2(safe.center.x, safe.yMin + safe.height * 0.73f), Vector3.one * pulse);
        panelGroup.alpha = 1f - Mathf.InverseLerp(impactDuration - 0.48f, impactDuration, impactTime);
        Color tint = impactColor;
        tint.a = 0.19f * (1f - Mathf.Clamp01(impactTime / 0.17f));
        flash.color = tint;
    }

    private void PositionPanel(Vector2 screen, Vector3 scale)
    {
        Rect safe = Screen.safeArea;
        float margin = 18f * canvas.scaleFactor;
        Vector2 halfSize = panel.sizeDelta * (canvas.scaleFactor * scale.x * 0.5f);
        float horizontalInset = Mathf.Min(halfSize.x + margin, safe.width * 0.5f);
        screen.x = Mathf.Clamp(screen.x, safe.xMin + horizontalInset, safe.xMax - horizontalInset);
        screen.y = Mathf.Clamp(screen.y, safe.yMin + safe.height * 0.18f, safe.yMin + safe.height * 0.83f);
        RectTransformUtility.ScreenPointToLocalPointInRectangle(canvasRect, screen, null, out Vector2 local);
        panel.anchoredPosition = local;
        panel.localScale = scale;
    }

    private void CreateDisplay()
    {
        var root = new GameObject("PV du mur d’acier", typeof(RectTransform), typeof(Canvas), typeof(CanvasScaler));
        SceneManager.MoveGameObjectToScene(root, gameObject.scene);
        canvas = root.GetComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 100;
        canvasRect = (RectTransform)root.transform;

        CanvasScaler scaler = root.GetComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1080f, 1920f);
        scaler.matchWidthOrHeight = 0.5f;

        flash = CreateImage("Flash", canvasRect, Color.clear);
        Stretch(flash.rectTransform, Vector2.zero, Vector2.zero);
        panel = CreateImage("Panneau", canvasRect, new Color32(18, 24, 34, 245)).rectTransform;
        panel.sizeDelta = new Vector2(580f, 104f);
        panelGroup = panel.gameObject.AddComponent<CanvasGroup>();
        panelGroup.interactable = false;
        panelGroup.blocksRaycasts = false;

        accent = CreateImage("Accent", panel, ReadyColor);
        accent.rectTransform.anchorMin = new Vector2(0f, 0f);
        accent.rectTransform.anchorMax = new Vector2(0f, 1f);
        accent.rectTransform.offsetMin = Vector2.zero;
        accent.rectTransform.offsetMax = new Vector2(8f, 0f);

        Font font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        title = CreateText("Titre", panel, font, TextAnchor.MiddleLeft, new Vector2(26f, 43f), new Vector2(-200f, -10f));
        counter = CreateText("Compteur", panel, font, TextAnchor.MiddleRight, new Vector2(394f, 43f), new Vector2(-24f, -10f));

        Image track = CreateImage("Fond de la jauge", panel, new Color32(58, 70, 86, 255));
        track.rectTransform.anchorMin = Vector2.zero;
        track.rectTransform.anchorMax = Vector2.right;
        track.rectTransform.offsetMin = new Vector2(26f, 18f);
        track.rectTransform.offsetMax = new Vector2(-24f, 33f);

        healthFill = CreateImage("PV restants", track.rectTransform, ReadyColor);
        Stretch(healthFill.rectTransform, Vector2.zero, Vector2.zero);
    }

    private static Image CreateImage(string name, Transform parent, Color color)
    {
        var obj = new GameObject(name, typeof(RectTransform), typeof(Image));
        obj.transform.SetParent(parent, false);
        Image image = obj.GetComponent<Image>();
        image.color = color;
        image.raycastTarget = false;
        return image;
    }

    private static Text CreateText(string name, Transform parent, Font font, TextAnchor alignment, Vector2 min, Vector2 max)
    {
        var obj = new GameObject(name, typeof(RectTransform), typeof(Text));
        obj.transform.SetParent(parent, false);
        Text text = obj.GetComponent<Text>();
        text.font = font;
        text.fontStyle = FontStyle.Bold;
        text.fontSize = 32;
        text.resizeTextForBestFit = true;
        text.resizeTextMinSize = 20;
        text.resizeTextMaxSize = 32;
        text.alignment = alignment;
        text.raycastTarget = false;
        Stretch(text.rectTransform, min, max);
        return text;
    }

    private static void Stretch(RectTransform rect, Vector2 min, Vector2 max)
    {
        rect.anchorMin = Vector2.zero;
        rect.anchorMax = Vector2.one;
        rect.offsetMin = min;
        rect.offsetMax = max;
    }
}
