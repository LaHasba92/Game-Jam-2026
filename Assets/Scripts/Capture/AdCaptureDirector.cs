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
            Vector3 to = new Vector3(boss.wallDistance - 22f, 8f, -2.1f);
            Vector3 position = Vector3.Lerp(from, to, t);
            Vector3 target = Vector3.Lerp(position + new Vector3(23f, -0.8f, 0.4f),
                new Vector3(boss.wallDistance, 10f, 0f), t);
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
            SetCamera(target + new Vector3(-12f, 2.8f, -2.7f), target, blend);
            return;
        }

        if (hasFired)
        {
            Vector3 ball = projectile.transform.position;
            float blend = 1f - Mathf.Exp(-15f * Time.unscaledDeltaTime);
            SetCamera(ball + new Vector3(-9f, 3.3f, -0.8f), ball + new Vector3(7f, 0.4f, 0f), blend);
            return;
        }

        Vector3 launcherPosition = launcher.transform.position;
        float cut = Mathf.Clamp01((Time.unscaledTime - takeTime) / 2f);
        Vector3 aimOffset = Vector3.Lerp(new Vector3(-7f, 3.25f, -3.6f), new Vector3(-7f, 3.1f, -2.7f), cut);
        SetCamera(launcherPosition + aimOffset, launcherPosition + new Vector3(6f, 0.8f, 0f), 1f);
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
