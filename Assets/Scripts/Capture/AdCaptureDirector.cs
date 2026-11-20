#if UNITY_EDITOR
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

// Plans indépendants à rejouer dans la vue Game pour le montage de la pub.
[DefaultExecutionOrder(200)]
public sealed class AdCaptureDirector : MonoBehaviour
{
    public enum Take { Intro, Miss, Shop, Canon, EndCard }

    public const int SequenceSeconds = 30;
    public const float SlowPreviewSpeed = 0.5f;
    private const float TransitionHalfDuration = 0.18f;
    private const float CanonLeadIn = 6.25f;
    private const float TeaserImpactTime = 0.20f;
    private const float TeaserCutTime = 0.68f;
    private const float TeaserWipeHalfDuration = 0.11f;
    private static readonly float[] CutTimes = { 3f, 10f, 15f, 28f };

    private static AdCaptureDirector current;
    public static bool HoldBossImpact => current != null && current.take == Take.Canon;
    public static bool HideIntroBossHud => current != null && current.hasTake && current.take == Take.Intro;
    public static float PresentationDelta => current != null ? current.frameDelta : Time.unscaledDeltaTime;

    private readonly Dictionary<Canvas, bool> sceneCanvases = new Dictionary<Canvas, bool>();
    private Camera view;
    private CameraDirector gameCamera;
    private GameManager game;
    private LauncherController launcher;
    private LauncherVisuals models;
    private ProjectileController projectile;
    private LevelManager levels;
    private WallController wall;
    private WorldStreamer track;
    private FeedbackManager feedback;
    private AdCaptureOverlay overlay;
    private AdCaptureShop shop;
    private AdCaptureTransition transition;
    private LevelDefinition boss;
    private float bossHeight;
    private int bossIndex;
    private float originalFieldOfView;
    private float clock;
    private float frameDelta;
    private float previewSpeed = 1f;
    private float takeTime;
    private float sequenceStart = -1f;
    private bool manualTransition;
    private int shopCue;
    private float impactTime = -1f;
    private int teaserHealth;
    private WallHit teaserHit;
    private bool teaserHitPlayed;
    private bool teaserFinished;
    private bool trackPausedForTeaser;
    private bool feedbackEnabled;
    private bool launcherEnabled;
    private bool cameraEnabled;
    private bool hasFired;
    private bool hasTake;
    private Take take;

    public static void PlayTake(Take next)
    {
        AdCaptureDirector director = GetOrCreate();
        if (director != null) director.ChooseTake(next);
    }

    public static void PlaySequence(bool record, float speed = 1f)
    {
        AdCaptureDirector director = GetOrCreate();
        if (director == null || (record && !AdCaptureRecorder.Begin(SequenceSeconds))) return;
        director.StartSequence(record ? 1f : speed);
    }

    private static AdCaptureDirector GetOrCreate()
    {
        if (current != null) return current;

        var root = new GameObject("CanonPOW — Capture pub");
        var director = root.AddComponent<AdCaptureDirector>();
        if (director.Connect()) return current = director;
        Destroy(root);
        return null;
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
        track = FindFirstObjectByType<WorldStreamer>();
        feedback = FindFirstObjectByType<FeedbackManager>();

        if (view == null || game == null || launcher == null || models == null ||
            projectile == null || levels == null || wall == null)
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
        shop = gameObject.AddComponent<AdCaptureShop>();
        transition = gameObject.AddComponent<AdCaptureTransition>();
        projectile.RunEnded += OnShotEnded;
        GameEvents.OnWallBroken += OnWallBroken;
        return true;
    }

    private void Update()
    {
        bool recording = AdCaptureRecorder.IsRecording;
        float playbackSpeed = recording ? 1f : previewSpeed;
        frameDelta = recording ? 1f / AdCaptureRecorder.FrameRate : Time.unscaledDeltaTime * playbackSpeed;
        if (hasTake) Time.timeScale = playbackSpeed;
        clock += frameDelta;

        if (Input.GetKeyDown(KeyCode.F5)) StartSequence(SlowPreviewSpeed);
        else if (Input.GetKeyDown(KeyCode.F6)) ChooseTake(Take.Intro);
        else if (Input.GetKeyDown(KeyCode.F7) || Input.GetKeyDown(KeyCode.F8)) ChooseTake(Take.Miss);
        else if (Input.GetKeyDown(KeyCode.F9)) ChooseTake(Take.Shop);
        else if (Input.GetKeyDown(KeyCode.F10)) ChooseTake(Take.Canon);
        else if (Input.GetKeyDown(KeyCode.F11)) ChooseTake(Take.EndCard);
        else if (Input.GetKeyDown(KeyCode.F12)) AdCaptureRecorder.Toggle();

        if (sequenceStart >= 0f) UpdateSequence();
        else if (manualTransition)
        {
            float fraction = (clock - takeTime) / TransitionHalfDuration;
            transition.Show(0.5f + 0.5f * fraction);
            if (fraction >= 1f) manualTransition = false;
        }

        if (take == Take.Shop)
        {
            float elapsed = clock - takeTime;
            shop.Show(elapsed);
            if (shopCue == 0 && elapsed >= AdCaptureShop.PowerPurchaseTime)
            {
                AdCaptureAudio.PlayCue(AdCaptureAudio.Cue.Confirm);
                shopCue = 1;
            }
            if (shopCue == 1 && elapsed >= AdCaptureShop.CanonPurchaseTime)
            {
                AdCaptureAudio.PlayCue(AdCaptureAudio.Cue.Confirm);
                shopCue = 2;
            }
        }

        if (hasFired && projectile.IsFlying)
            overlay.ShowFlight(projectile.WallsBroken, projectile.Distance);

        if (take == Take.Intro && hasTake)
            UpdateIntro();
        else if (impactTime >= 0f)
            overlay.ShowChallenge(clock - impactTime);
    }

    private void ChooseTake(Take next)
    {
        sequenceStart = -1f;
        previewSpeed = 1f;
        manualTransition = next != Take.Intro;
        StartTake(next);
        transition.Show(manualTransition ? 0.5f : 0f);
    }

    private void StartSequence(float speed)
    {
        sequenceStart = clock;
        previewSpeed = speed;
        manualTransition = false;
        StartTake(Take.Intro);
        transition.Show(0f);
    }

    private void UpdateSequence()
    {
        float elapsed = clock - sequenceStart;
        Take next = elapsed < CutTimes[0] ? Take.Intro
            : elapsed < CutTimes[1] ? Take.Miss
            : elapsed < CutTimes[2] ? Take.Shop
            : elapsed < CutTimes[3] ? Take.Canon
            : Take.EndCard;
        if (next != take) StartTake(next);

        float wipe = 0f;
        foreach (float cut in CutTimes)
        {
            float offset = elapsed - cut;
            if (Mathf.Abs(offset) >= TransitionHalfDuration) continue;
            wipe = (offset + TransitionHalfDuration) / (2f * TransitionHalfDuration);
            break;
        }
        transition.Show(wipe);
        if (elapsed >= SequenceSeconds)
        {
            sequenceStart = -1f;
            previewSpeed = 1f;
            Time.timeScale = 1f;
        }
    }

    private void StartTake(Take next)
    {
        StopAllCoroutines();
        RestoreTrack();
        StopFeedback();
        Time.timeScale = AdCaptureRecorder.IsRecording ? 1f : previewSpeed;
        hasTake = true;
        launcher.ClearCaptureAim();
        launcher.enabled = false;
        if (gameCamera != null) gameCamera.enabled = false;
        view.fieldOfView = 54f;
        overlay.Clear();
        shop.Clear();
        shopCue = 0;
        hasFired = false;
        impactTime = -1f;

        game.ChangeState(GameState.Home);
        game.Play(); // remet le projectile, la piste et les murs au départ
        take = next;
        takeTime = clock;
        models.SetCaptureModel(next == Take.Canon ? 4 : 0);
        SetSceneUi(false);
        if (next != Take.Intro)
            AdCaptureAudio.PlayCue(next == Take.EndCard ? AdCaptureAudio.Cue.End : AdCaptureAudio.Cue.Whoosh);

        switch (next)
        {
            case Take.Intro:
                wall.HideAllWalls();
                wall.ShowWall(boss);
                PrepareIntro();
                break;
            case Take.Shop:
                shop.Show(0f);
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
        Vector3 origin = launcher.CaptureLaunchOrigin;
        if (!TryPrepareShot(canon, origin, out Vector3 velocity, out _))
        {
            Debug.LogError("Capture pub : aucune trajectoire trouvée. Vérifie les distances et les PV des murs.");
            yield break;
        }

        float leadIn = canon ? CanonLeadIn : 2f;
        while (clock - takeTime < leadIn) yield return null;

        float aimDuration = canon ? 1.04f : 0.56f;
        float target = canon ? 0.96f : 0.71f;
        float aimStart = clock;
        while (clock - aimStart < aimDuration)
        {
            float elapsed = clock - aimStart;
            float gauge = Mathf.Lerp(0.12f, target, Mathf.Clamp01(elapsed / (aimDuration * 0.76f)));
            overlay.ShowGauge(gauge, gauge >= 0.88f);
            yield return null;
        }

        overlay.Clear();
        hasFired = true;
        AdCaptureAudio.PlayCue(canon ? AdCaptureAudio.Cue.Cannon : AdCaptureAudio.Cue.Slingshot);
        projectile.LaunchAdTake(origin, velocity);
        GameEvents.RaiseShotFired(new ShotInfo
        {
            angle = Mathf.Atan2(velocity.y, velocity.x) * Mathf.Rad2Deg,
            gaugeMultiplier = canon ? 1.25f : 0.9f,
            launcherIndex = canon ? 4 : 0,
        });
    }

    private bool TryPrepareShot(bool canon, Vector3 origin, out Vector3 velocity, out WallHit nearMiss)
    {
        projectile.SetUpgrades(canon ? 0.04f : 0.10f, canon ? 0.70f : 0.45f, canon ? 0.85f : 0.60f);
        return AdCaptureFlight.TryFind(canon, origin, projectile, levels, bossIndex, out velocity, out nearMiss);
    }

    private void PrepareIntro()
    {
        teaserHealth = 0;
        teaserHitPlayed = false;
        teaserFinished = false;
        if (TryPrepareShot(true, launcher.CaptureLaunchOrigin, out _, out teaserHit))
            teaserHealth = WallRules.HealthAfterImpact(teaserHit, boss.maxHealth, boss.maxHealth, boss.fullHealthBreakSpeed);

        if (teaserHealth == 0)
        {
            AdCaptureAudio.PlayCue(AdCaptureAudio.Cue.Whoosh);
            return;
        }

        // Le décor est déjà présent jusqu'au boss. On garde les tronçons en place pendant le flash-forward.
        trackPausedForTeaser = track != null && track.enabled;
        if (trackPausedForTeaser) track.enabled = false;
    }

    private void UpdateIntro()
    {
        float elapsed = clock - takeTime;
        overlay.ShowHook(teaserHealth, elapsed, TeaserImpactTime);
        if (teaserHealth == 0) return;

        if (elapsed < TeaserCutTime)
        {
            Vector3 end = new Vector3(boss.wallDistance - projectile.Physics.radius, teaserHit.impactHeight, teaserHit.impactSide);
            float travel = Mathf.Clamp01(elapsed / TeaserImpactTime);
            projectile.transform.position = end - Vector3.right * (1f - travel) * 5.4f;
            if (elapsed >= TeaserImpactTime && !teaserHitPlayed)
            {
                teaserHitPlayed = true;
                WallHitResult result = wall.ApplyHit(teaserHit);
                StartCoroutine(wall.PlayHit(result));
                AdCaptureAudio.PlayCue(AdCaptureAudio.Cue.Steel);
            }
        }
        else if (!teaserFinished)
        {
            teaserFinished = true;
            projectile.ResetToLauncher();
            RestoreTrack();
            AdCaptureAudio.PlayCue(AdCaptureAudio.Cue.Whoosh);
        }

        float offset = elapsed - TeaserCutTime;
        if (Mathf.Abs(offset) < TeaserWipeHalfDuration)
            transition.Show((offset + TeaserWipeHalfDuration) / (2f * TeaserWipeHalfDuration));
        else if (offset >= TeaserWipeHalfDuration && offset < 2f)
            transition.Show(0f);
    }

    private void RestoreTrack()
    {
        if (trackPausedForTeaser && track != null) track.enabled = true;
        trackPausedForTeaser = false;
    }

    private void OnWallBroken(int wallNumber)
    {
        if (!hasFired || (take != Take.Miss && take != Take.Canon)) return;

        AdCaptureAudio.PlayCue(AdCaptureAudio.Cue.Break);
    }

    private void OnShotEnded(RunResult result)
    {
        if (take == Take.Miss)
        {
            overlay.ShowFlight(result.wallsBroken, result.distance);
            overlay.ShowWeakOutcome(result.wallsBroken);
            AdCaptureAudio.PlayCue(AdCaptureAudio.Cue.WeakFail);
            return;
        }
        if (take != Take.Canon || result.wallsBroken != bossIndex) return;

        overlay.Clear(); // les PV du mur prennent le relais du HUD de vol
        impactTime = clock;
        AdCaptureAudio.PlayCue(AdCaptureAudio.Cue.Steel);
    }

    private void LateUpdate()
    {
        if (!hasTake || take == Take.EndCard) return;

        if (take == Take.Intro)
        {
            float elapsed = clock - takeTime;
            if (teaserHealth > 0 && elapsed < TeaserCutTime)
            {
                float recoil = teaserHitPlayed ? Mathf.Max(0f, 1f - (elapsed - TeaserImpactTime) / 0.24f) : 0f;
                Vector3 hitPoint = new Vector3(boss.wallDistance, teaserHit.impactHeight + 1f, teaserHit.impactSide);
                SetCamera(hitPoint + new Vector3(-12f - recoil * 0.7f, 0.9f + recoil * 0.12f, -1.1f), hitPoint, 1f);
                return;
            }

            float start = teaserHealth > 0 ? TeaserCutTime : 0f;
            float t = Mathf.SmoothStep(0f, 1f, Mathf.InverseLerp(start, CutTimes[0], elapsed));
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
            Vector3 ball = projectile.transform.position;
            Vector3 target = new Vector3(boss.wallDistance, ball.y + 2.5f, ball.z);
            float blend = 1f - Mathf.Exp(-3.8f * frameDelta);
            float distance = Mathf.Max(12f, FitDistance(wall.WallWidth * 0.61f, 0f));
            SetCamera(new Vector3(target.x - distance, target.y + 2.8f, -1.2f), target, blend);
            return;
        }

        if (hasFired)
        {
            Vector3 ball = projectile.transform.position;
            float blend = 1f - Mathf.Exp(-15f * frameDelta);
            float distance = Mathf.Max(9f, FitDistance(wall.WallWidth * 0.61f, 0f));
            SetCamera(ball + new Vector3(-distance, 3.3f, -0.45f), ball + new Vector3(7f, 0.4f, 0f), blend);
            return;
        }

        Vector3 launcherPosition = launcher.transform.position;
        float aimDistance = Mathf.Max(7f, FitDistance(2.4f, 0f));
        if (take == Take.Canon)
        {
            float reveal = Mathf.SmoothStep(0f, 1f, (clock - takeTime) / CanonLeadIn);
            Vector3 offset = Vector3.Lerp(new Vector3(-aimDistance * 0.82f, 2.15f, -1.5f),
                new Vector3(-aimDistance, 2.9f, -0.7f), reveal);
            Vector3 aim = new Vector3(Mathf.Lerp(0.4f, 2.4f, reveal), 0.7f, 0f);
            SetCamera(launcherPosition + offset, launcherPosition + aim, 1f);
            return;
        }

        float cut = Mathf.Clamp01((clock - takeTime) / 2f);
        Vector3 aimOffset = new Vector3(-aimDistance, 2.9f, Mathf.Lerp(-1.5f, -0.8f, cut));
        float lead = take == Take.Miss
            ? Mathf.SmoothStep(0.6f, 2.4f, (clock - takeTime - 1.7f) / 0.6f)
            : 2.4f;
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
        float roadWidth = wall.WallWidth;
        Transform cameraTransform = view.transform;
        position = CameraDirector.KeepOverRoad(position, roadWidth);
        target = CameraDirector.KeepOverRoad(target, roadWidth);
        cameraTransform.position = CameraDirector.KeepOverRoad(
            Vector3.Lerp(cameraTransform.position, position, blend), roadWidth);
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
        GameEvents.OnWallBroken -= OnWallBroken;
        StopAllCoroutines();
        RestoreTrack();
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
        shop.Clear();
        transition.Show(0f);
        game.ChangeState(GameState.Home);
        game.Play();
        game.ChangeState(GameState.Home);
        Destroy(gameObject);
    }
}
#endif
