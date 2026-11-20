#if UNITY_EDITOR
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

// Plans indépendants à rejouer dans la vue Game pour le montage de la pub.
[DefaultExecutionOrder(200)]
public sealed class AdCaptureDirector : MonoBehaviour
{
    public enum Take { Intro, Slingshot, Miss, Shop, Canon, EndCard }

    private static AdCaptureDirector current;
    public static bool HoldBossImpact => current != null && current.take == Take.Canon;

    private readonly Dictionary<Canvas, bool> sceneCanvases = new Dictionary<Canvas, bool>();
    private readonly Dictionary<RectTransform, (Vector2 position, Vector3 scale)> shopLayout =
        new Dictionary<RectTransform, (Vector2 position, Vector3 scale)>();
    private Camera view;
    private CameraDirector gameCamera;
    private GameManager game;
    private LauncherController launcher;
    private LauncherVisuals models;
    private ProjectileController projectile;
    private LevelManager levels;
    private WallController wall;
    private UIStateSwitcher ui;
    private FeedbackManager feedback;
    private AdCaptureOverlay overlay;
    private LevelDefinition boss;
    private float bossHeight;
    private int bossIndex;
    private float originalFieldOfView;
    private float takeTime;
    private float impactTime = -1f;
    private bool feedbackEnabled;
    private bool launcherEnabled;
    private bool cameraEnabled;
    private bool hasFired;
    private Take take;

    public static void PlayTake(Take next)
    {
        if (current == null)
        {
            var root = new GameObject("CanonPOW — Capture pub");
            var director = root.AddComponent<AdCaptureDirector>();
            if (!director.Connect())
            {
                Destroy(root);
                return;
            }
            current = director;
        }

        current.StartTake(next);
    }

    public static void Exit()
    {
        if (current != null) current.enabled = false;
    }

    private bool Connect()
    {
        view = Camera.main;
        game = GameManager.Instance;
        launcher = FindFirstObjectByType<LauncherController>();
        models = FindFirstObjectByType<LauncherVisuals>();
        projectile = FindFirstObjectByType<ProjectileController>();
        levels = FindFirstObjectByType<LevelManager>();
        wall = FindFirstObjectByType<WallController>();
        ui = FindFirstObjectByType<UIStateSwitcher>();
        feedback = FindFirstObjectByType<FeedbackManager>();

        if (view == null || game == null || launcher == null || models == null ||
            projectile == null || levels == null || wall == null || ui == null)
        {
            Debug.LogError("Capture pub : ouvre la scène Game et lance Play avant de choisir un plan.");
            return false;
        }

        boss = levels.GetCaptureBoss(out bossIndex);
        if (boss == null)
        {
            Debug.LogError("Capture pub : le mur d’acier doit être dans la liste de LevelManager.");
            return false;
        }
        bossHeight = boss.wallPrefab != null && boss.wallPrefab.TryGetComponent<WallVisualSet>(out var bossVisual)
            ? bossVisual.WallHeight
            : 8f;

        foreach (Canvas canvas in FindObjectsByType<Canvas>(FindObjectsInactive.Include, FindObjectsSortMode.None))
            sceneCanvases.Add(canvas, canvas.enabled);

        // Une prise peut aussi commencer juste après un essai du mur en jeu.
        foreach (BossWallDisplay existing in FindObjectsByType<BossWallDisplay>(FindObjectsSortMode.None))
            existing.Clear();

        originalFieldOfView = view.fieldOfView;
        gameCamera = view.GetComponent<CameraDirector>();
        cameraEnabled = gameCamera != null && gameCamera.enabled;
        feedbackEnabled = feedback != null && feedback.enabled;
        launcherEnabled = launcher.enabled;
        overlay = gameObject.AddComponent<AdCaptureOverlay>();
        projectile.RunEnded += OnShotEnded;
        return true;
    }

    private void Update()
    {
        if (Input.GetKeyDown(KeyCode.F6)) StartTake(Take.Intro);
        else if (Input.GetKeyDown(KeyCode.F7)) StartTake(Take.Slingshot);
        else if (Input.GetKeyDown(KeyCode.F8)) StartTake(Take.Miss);
        else if (Input.GetKeyDown(KeyCode.F9)) StartTake(Take.Shop);
        else if (Input.GetKeyDown(KeyCode.F10)) StartTake(Take.Canon);
        else if (Input.GetKeyDown(KeyCode.F11)) StartTake(Take.EndCard);
        else if (Input.GetKeyDown(KeyCode.F12)) AdCaptureRecorder.Toggle();
    }

    private void StartTake(Take next)
    {
        StopAllCoroutines();
        StopFeedback();
        Time.timeScale = 1f;
        RestoreShopLayout();
        launcher.ClearCaptureAim();
        launcher.enabled = false;
        if (gameCamera != null) gameCamera.enabled = false;
        view.fieldOfView = 54f;
        overlay.Clear();
        hasFired = false;
        impactTime = -1f;

        game.ChangeState(GameState.Home);
        game.Play(); // remet le projectile, la piste et les murs au départ
        take = next;
        takeTime = Time.unscaledTime;
        models.SetCaptureModel(next == Take.Canon ? 4 : next == Take.Shop ? -1 : 0);
        SetSceneUi(next == Take.Shop);

        switch (next)
        {
            case Take.Intro:
                wall.HideAllWalls();
                wall.ShowWall(boss);
                break;
            case Take.Shop:
                game.ChangeState(GameState.Home);
                ui.OpenShop();
                FitShopToFrame();
                break;
            case Take.Miss:
            case Take.Canon:
                StartCoroutine(PlayShot(next == Take.Canon));
                break;
            case Take.EndCard:
                overlay.ShowEndCard();
                break;
        }
    }

    private IEnumerator PlayShot(bool canon)
    {
        projectile.SetUpgrades(canon ? 0.04f : 0.10f, canon ? 0.70f : 0.45f, canon ? 0.85f : 0.60f);
        Vector3 origin = launcher.CaptureLaunchOrigin;
        if (!AdCaptureFlight.TryFind(canon, origin, projectile, levels, bossIndex, out Vector3 velocity))
        {
            Debug.LogError("Capture pub : aucune trajectoire trouvée. Vérifie les distances et les PV des murs.");
            yield break;
        }

        if (canon) yield return new WaitForSecondsRealtime(2f);

        const float aimDuration = 1.2f;
        float target = canon ? 0.96f : 0.71f;
        for (float elapsed = 0f; elapsed < aimDuration; elapsed += Time.unscaledDeltaTime)
        {
            float gauge = Mathf.Lerp(0.12f, target, Mathf.Clamp01(elapsed / 0.87f));
            overlay.ShowGauge(gauge, gauge >= 0.88f);
            yield return null;
        }

        overlay.Clear();
        hasFired = true;
        if (feedback != null) feedback.enabled = feedbackEnabled;
        projectile.LaunchAdTake(origin, velocity);
        GameEvents.RaiseShotFired(new ShotInfo
        {
            angle = Mathf.Atan2(velocity.y, velocity.x) * Mathf.Rad2Deg,
            gaugeMultiplier = canon ? 1.25f : 0.9f,
            launcherIndex = canon ? 4 : 0,
        });
    }

    private void OnShotEnded(RunResult result)
    {
        if (take != Take.Canon || result.wallsBroken != bossIndex) return;

        impactTime = Time.unscaledTime;
        StopFeedback();
        Time.timeScale = 0.35f;
    }

    private void LateUpdate()
    {
        if (take == Take.EndCard) return;

        if (take == Take.Intro)
        {
            float t = Mathf.SmoothStep(0f, 1f, Mathf.Clamp01((Time.unscaledTime - takeTime) / 3f));
            Vector3 from = new Vector3(-7f, 3.7f, -1.6f);
            float wallDistance = FitDistance(Mathf.Max(1f, wall.WallWidth) * 0.6f, bossHeight * 0.58f);
            Vector3 to = new Vector3(boss.wallDistance - wallDistance, bossHeight * 0.46f, -0.8f);
            Vector3 position = Vector3.Lerp(from, to, t);
            Vector3 target = Vector3.Lerp(position + new Vector3(23f, -0.8f, 0.4f),
                new Vector3(boss.wallDistance, bossHeight * 0.5f, 0f), t);
            SetCamera(position, target, 1f);
            return;
        }

        if (impactTime >= 0f)
        {
            float elapsed = Time.unscaledTime - impactTime;
            if (elapsed >= 3f) Time.timeScale = 1f;
            Vector3 ball = projectile.transform.position;
            Vector3 target = new Vector3(boss.wallDistance, ball.y + 2.5f, ball.z);
            float blend = 1f - Mathf.Exp(-3.8f * Time.unscaledDeltaTime);
            float distance = Mathf.Max(12f, FitDistance(wall.WallWidth * 0.61f, 0f));
            SetCamera(target + new Vector3(-distance, 2.8f, -1.6f), target, blend);
            return;
        }

        if (hasFired)
        {
            Vector3 ball = projectile.transform.position;
            float blend = 1f - Mathf.Exp(-15f * Time.unscaledDeltaTime);
            float distance = Mathf.Max(9f, FitDistance(wall.WallWidth * 0.61f, 0f));
            SetCamera(ball + new Vector3(-distance, 3.3f, -0.45f), ball + new Vector3(7f, 0.4f, 0f), blend);
            return;
        }

        Vector3 launcherPosition = launcher.transform.position;
        float cut = Mathf.Clamp01((Time.unscaledTime - takeTime) / 2f);
        float aimDistance = Mathf.Max(7f, FitDistance(2.4f, 0f));
        Vector3 aimOffset = new Vector3(-aimDistance, 2.9f, Mathf.Lerp(-2.5f, -1.7f, cut));
        float lead = take == Take.Slingshot ? 0.6f : 2.4f;
        SetCamera(launcherPosition + aimOffset, launcherPosition + new Vector3(lead, 0.7f, 0f), 1f);
    }

    private float FitDistance(float halfWidth, float halfHeight)
    {
        float verticalTangent = Mathf.Tan(view.fieldOfView * 0.5f * Mathf.Deg2Rad);
        float horizontalTangent = verticalTangent * view.aspect;
        return Mathf.Max(halfWidth / horizontalTangent, halfHeight / verticalTangent);
    }

    private void SetCamera(Vector3 position, Vector3 target, float blend)
    {
        Transform cameraTransform = view.transform;
        cameraTransform.position = Vector3.Lerp(cameraTransform.position, position, blend);
        cameraTransform.rotation = Quaternion.Slerp(cameraTransform.rotation,
            Quaternion.LookRotation(target - cameraTransform.position), blend);
    }

    private void StopFeedback()
    {
        if (feedback == null) return;
        feedback.StopAllCoroutines();
        feedback.enabled = false;
    }

    private void SetSceneUi(bool visible)
    {
        foreach (KeyValuePair<Canvas, bool> pair in sceneCanvases)
            if (pair.Key != null) pair.Key.enabled = visible && pair.Value;
    }

    private void FitShopToFrame()
    {
        RectTransform panel = ui.CaptureShopPanel;
        if (panel == null) return;

        Canvas.ForceUpdateCanvases();
        var corners = new Vector3[4];
        Vector2 minimum = Vector2.one * float.PositiveInfinity;
        Vector2 maximum = Vector2.one * float.NegativeInfinity;
        foreach (Transform child in panel)
        {
            if (child is not RectTransform rect || !rect.gameObject.activeSelf) continue;
            shopLayout.Add(rect, (rect.anchoredPosition, rect.localScale));
            rect.GetWorldCorners(corners);
            foreach (Vector3 corner in corners)
            {
                Vector2 point = panel.InverseTransformPoint(corner);
                minimum = Vector2.Min(minimum, point);
                maximum = Vector2.Max(maximum, point);
            }
        }
        if (shopLayout.Count == 0) return;

        Vector2 center = (minimum + maximum) * 0.5f;
        Vector2 size = Vector2.Max(maximum - minimum, Vector2.one);
        Vector2 available = Vector2.Max(panel.rect.size - Vector2.one * 64f, Vector2.one);
        float scale = Mathf.Min(1f, Mathf.Min(available.x / size.x, available.y / size.y));
        foreach (RectTransform rect in shopLayout.Keys)
        {
            Vector2 position = rect.localPosition;
            rect.anchoredPosition += panel.rect.center + (position - center) * scale - position;
            rect.localScale *= scale;
        }
    }

    private void RestoreShopLayout()
    {
        foreach (var pair in shopLayout)
        {
            if (pair.Key == null) continue;
            pair.Key.anchoredPosition = pair.Value.position;
            pair.Key.localScale = pair.Value.scale;
        }
        shopLayout.Clear();
    }

    private void OnDisable()
    {
        if (current != this) return;

        current = null;
        StopAllCoroutines();
        AdCaptureRecorder.StopCurrent();
        if (!Application.isPlaying || game == null || projectile == null || models == null ||
            launcher == null || view == null || overlay == null)
            return;

        Time.timeScale = 1f;
        RestoreShopLayout();
        projectile.RunEnded -= OnShotEnded;
        projectile.ResetToLauncher();
        models.SetCaptureModel(-1);
        launcher.enabled = launcherEnabled;
        view.fieldOfView = originalFieldOfView;
        if (gameCamera != null) gameCamera.enabled = cameraEnabled;
        if (feedback != null) feedback.enabled = feedbackEnabled;
        SetSceneUi(true);
        overlay.Clear();
        game.ChangeState(GameState.Home);
        game.Play();
        game.ChangeState(GameState.Home);
        Destroy(gameObject);
    }
}
#endif
