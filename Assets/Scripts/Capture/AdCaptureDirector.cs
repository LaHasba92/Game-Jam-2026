#if UNITY_EDITOR
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

// Plans indépendants à rejouer dans la vue Game pour le montage de la pub.
[DefaultExecutionOrder(200)]
public sealed class AdCaptureDirector : MonoBehaviour
{
    public enum Take { Intro, Miss, Shop, Canon, EndCard, LunarEstablishing }
    public enum Trailer { Full, Fronde, Steel, Moon }

    public const int SequenceSeconds = 30;
    public const float SlowPreviewSpeed = 0.5f;
    private const float TransitionHalfDuration = 0.18f;
    private const float FullCanonLeadIn = 6.25f;
    private const float CanonEvolutionStart = 0.3f;
    private const float CanonEvolutionDuration = 5f;
    private const float OpeningFlightEnd = 2.9f;

    private sealed class TrailerEdit
    {
        public readonly int Duration;
        public readonly string FileLabel;
        public readonly ThemeId Theme;
        public readonly float MissLeadIn;
        public readonly float CanonLeadIn;
        public readonly bool PlayEvolution;
        public readonly (float Start, Take Plan)[] Cuts;

        public TrailerEdit(int duration, string fileLabel, ThemeId theme, float missLeadIn, float canonLeadIn,
            bool playEvolution, params (float Start, Take Plan)[] cuts)
        {
            Duration = duration;
            FileLabel = fileLabel;
            Theme = theme;
            MissLeadIn = missLeadIn;
            CanonLeadIn = canonLeadIn;
            PlayEvolution = playEvolution;
            Cuts = cuts;
        }
    }

    private static readonly TrailerEdit FullEdit = new TrailerEdit(SequenceSeconds, null, ThemeId.City, 2f, FullCanonLeadIn, true,
        (0f, Take.Intro), (3f, Take.Miss), (10f, Take.Shop), (15f, Take.Canon), (28f, Take.EndCard));
    private static readonly TrailerEdit FrondeEdit = new TrailerEdit(22, "De-la-fronde-au-canon", ThemeId.City, 0.45f, FullCanonLeadIn, true,
        (0f, Take.Miss), (5f, Take.Shop), (9f, Take.Canon), (20f, Take.EndCard));
    private static readonly TrailerEdit SteelEdit = new TrailerEdit(15, "A-quelques-PV", ThemeId.City, 2f, 2.8f, false,
        (0f, Take.Intro), (3f, Take.Canon), (13f, Take.EndCard));
    private static readonly TrailerEdit MoonEdit = new TrailerEdit(15, "Defi-lunaire", ThemeId.Moon, 2f, 1.7f, false,
        (0f, Take.LunarEstablishing), (4.1f, Take.Canon), (13f, Take.EndCard));

    private static AdCaptureDirector current;
    public static bool IsPlayingTake => current != null && current.hasTake;
    public static bool HoldBossImpact => current != null && current.take == Take.Canon;
    public static bool HideIntroBossHud => current != null && current.hasTake && current.take == Take.Intro;
    public static float PresentationDelta => current != null ? current.frameDelta : Time.unscaledDeltaTime;

    private readonly Dictionary<Canvas, bool> sceneCanvases = new Dictionary<Canvas, bool>();
    private readonly AdCaptureEvolution evolution = new AdCaptureEvolution();
    private Camera view;
    private CameraDirector gameCamera;
    private GameManager game;
    private LauncherController launcher;
    private LauncherVisuals models;
    private ProjectileController projectile;
    private LevelManager levels;
    private WallController wall;
    private WorldStreamer world;
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
    private float canonEvolutionEnd;
    private float sequenceStart = -1f;
    private bool manualTransition;
    private int shopCue;
    private float impactTime = -1f;
    private bool openingShot;
    private bool feedbackEnabled;
    private bool launcherEnabled;
    private bool cameraEnabled;
    private bool hasFired;
    private bool hasTake;
    private Take take;
    private Trailer trailer;
    private TrailerEdit edit = FullEdit;

    public static void PlayTake(Take next)
    {
        if (CaptureBusy()) return;
        AdCaptureDirector director = GetOrCreate();
        if (director != null) director.ChooseTake(next);
    }

    public static void PlaySequence(bool record, float speed = 1f) => PlayTrailer(Trailer.Full, record, speed);

    public static void PlayTrailer(Trailer variant, bool record, float speed = 1f)
    {
        if (CaptureBusy()) return;
        AdCaptureDirector director = GetOrCreate();
        if (director == null) return;

        TrailerEdit selection = variant switch
        {
            Trailer.Fronde => FrondeEdit,
            Trailer.Steel => SteelEdit,
            Trailer.Moon => MoonEdit,
            _ => FullEdit
        };
        if (record)
            AdCaptureRecorder.Begin(selection.Duration, () => director.StartSequence(variant, selection, 1f), selection.FileLabel);
        else
            director.StartSequence(variant, selection, speed);
    }

    private static bool CaptureBusy()
    {
        if (!AdCaptureRecorder.IsBusy) return false;
        Debug.LogWarning("Capture pub : termine l’export en cours ou arrête-le avec F12.");
        return true;
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
        world = FindFirstObjectByType<WorldStreamer>();
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
        return true;
    }

    private void Update()
    {
        bool recording = AdCaptureRecorder.IsRecording;
        float playbackSpeed = recording ? 1f : previewSpeed;
        frameDelta = recording ? 1f / AdCaptureRecorder.FrameRate : Time.unscaledDeltaTime * playbackSpeed;
        if (hasTake) Time.timeScale = playbackSpeed;
        clock += frameDelta;

        if (Input.GetKeyDown(KeyCode.F12))
            AdCaptureRecorder.Toggle();
        else if (!AdCaptureRecorder.IsBusy)
        {
            if (Input.GetKeyDown(KeyCode.F5)) StartSequence(Trailer.Full, FullEdit, SlowPreviewSpeed);
            else if (Input.GetKeyDown(KeyCode.F6)) ChooseTake(Take.Intro);
            else if (Input.GetKeyDown(KeyCode.F7) || Input.GetKeyDown(KeyCode.F8)) ChooseTake(Take.Miss);
            else if (Input.GetKeyDown(KeyCode.F9)) ChooseTake(Take.Shop);
            else if (Input.GetKeyDown(KeyCode.F10)) ChooseTake(Take.Canon);
            else if (Input.GetKeyDown(KeyCode.F11)) ChooseTake(Take.EndCard);
        }

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

        if (sequenceStart >= 0f && take == edit.Cuts[0].Plan)
            ShowTrailerHook(clock - takeTime);
    }

    private void ChooseTake(Take next)
    {
        sequenceStart = -1f;
        trailer = Trailer.Full;
        edit = FullEdit;
        previewSpeed = 1f;
        manualTransition = next != Take.Intro;
        StartTake(next);
        transition.Show(manualTransition ? 0.5f : 0f);
    }

    private void StartSequence(Trailer variant, TrailerEdit selection, float speed)
    {
        trailer = variant;
        edit = selection;
        sequenceStart = clock;
        previewSpeed = speed;
        manualTransition = false;
        StartTake(edit.Cuts[0].Plan);
        transition.Show(0f);
    }

    private void UpdateSequence()
    {
        float elapsed = clock - sequenceStart;
        Take next = edit.Cuts[0].Plan;
        for (int i = 1; i < edit.Cuts.Length && elapsed >= edit.Cuts[i].Start; ++i)
            next = edit.Cuts[i].Plan;
        if (next != take) StartTake(next);

        float wipe = 0f;
        for (int i = 1; i < edit.Cuts.Length; ++i)
        {
            float offset = elapsed - edit.Cuts[i].Start;
            if (Mathf.Abs(offset) >= TransitionHalfDuration) continue;
            wipe = (offset + TransitionHalfDuration) / (2f * TransitionHalfDuration);
            break;
        }
        transition.Show(wipe);
        if (elapsed >= edit.Duration)
        {
            sequenceStart = -1f;
            previewSpeed = 1f;
            Time.timeScale = 1f;
        }
    }

    private void StartTake(Take next)
    {
        StopAllCoroutines();
        evolution.Clear();
        AdCaptureAudio.StopEvolution();
        StopFeedback();
        Time.timeScale = AdCaptureRecorder.IsRecording ? 1f : previewSpeed;
        if (world != null) world.SetCaptureTheme(edit.Theme);
        hasTake = true;
        launcher.ClearCaptureAim();
        launcher.enabled = false;
        if (gameCamera != null) gameCamera.enabled = false;
        view.fieldOfView = 54f;
        overlay.Clear();
        shop.Clear();
        shopCue = 0;
        hasFired = false;
        openingShot = false;
        impactTime = -1f;
        canonEvolutionEnd = CanonEvolutionStart + CanonEvolutionDuration;

        game.ChangeState(GameState.Home);
        game.Play(); // remet le projectile, la piste et les murs au départ
        take = next;
        takeTime = clock;
        models.SetCaptureModel(next == Take.Canon && !edit.PlayEvolution ? 4 : 0);
        SetSceneUi(false);
        AdCaptureAudio.PlayCue(next == Take.EndCard ? AdCaptureAudio.Cue.End : AdCaptureAudio.Cue.Whoosh);

        switch (next)
        {
            case Take.Intro:
                StartOpeningShot();
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
        if (!TryPrepareShot(canon, origin, out Vector3 velocity))
        {
            Debug.LogError("Capture pub : aucune trajectoire trouvée. Vérifie les distances et les PV des murs.");
            yield break;
        }

        if (canon && edit.PlayEvolution) yield return PlayEvolution();

        float leadIn = canon ? edit.CanonLeadIn : edit.MissLeadIn;
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

    private IEnumerator PlayEvolution()
    {
        while (clock - takeTime < CanonEvolutionStart) yield return null;

        if (!models.TryGetCaptureModel(0, out GameObject slingshot) ||
            !models.TryGetCaptureModel(4, out GameObject cannon) ||
            !evolution.Begin(launcher.transform, slingshot, cannon, CanonEvolutionDuration))
        {
            Debug.LogWarning("Capture pub : la transformation exige les deux modèles du lanceur et le shader Unlit.");
            canonEvolutionEnd = CanonEvolutionStart + 0.5f;
            models.SetCaptureModel(4);
            yield break;
        }

        float started = clock;
        canonEvolutionEnd = started - takeTime + CanonEvolutionDuration;
        AdCaptureAudio.PlayCue(AdCaptureAudio.Cue.Evolution, AdCaptureRecorder.IsRecording ? 1f : previewSpeed);
        while (clock - started < CanonEvolutionDuration)
        {
            evolution.Sample(clock - started);
            yield return null;
        }

        evolution.Clear();
        models.SetCaptureModel(4);
    }

    private bool TryPrepareShot(bool canon, Vector3 origin, out Vector3 velocity)
    {
        projectile.SetUpgrades(canon ? 0.04f : 0.10f, canon ? 0.70f : 0.45f, canon ? 0.85f : 0.60f);
        return AdCaptureFlight.TryFind(canon, origin, projectile, levels, bossIndex, out velocity);
    }

    private void StartOpeningShot()
    {
        int previousWall = bossIndex - 1;
        if (!levels.StartAdAtWall(previousWall))
        {
            wall.ShowWall(boss);
            return;
        }

        // Ouvre sur un tir déjà en vol. Le mur en pierre cède, puis le boss entre dans le cadre.
        float wallX = levels.WallDistance(previousWall);
        projectile.SetUpgrades(0.10f, 0.45f, 0.60f);
        projectile.LaunchAdTake(new Vector3(Mathf.Max(0f, wallX - 22f), 2.6f, 0f),
            new Vector3(22f, 4.9f, 0f));
        openingShot = true;
        GameEvents.RaiseShotFired(new ShotInfo { angle = 12.6f, gaugeMultiplier = 1f, launcherIndex = 0 });
    }

    private void UpdateIntro()
    {
        float elapsed = clock - takeTime;
        if (trailer == Trailer.Full) overlay.ShowHook(elapsed);
        if (openingShot && elapsed >= OpeningFlightEnd) projectile.HoldAdTake();
    }

    private void ShowTrailerHook(float elapsed)
    {
        switch (trailer)
        {
            case Trailer.Fronde:
                overlay.ShowTrailerHook("DEUX MURS. C’EST TOUT ?", "Voyons ce que donne le canon.", elapsed, 2.05f);
                break;
            case Trailer.Steel:
                overlay.ShowTrailerHook($"{bossIndex} MURS. UN SEUL TIR.", "Reste le mur d’acier.", elapsed, 2.65f);
                break;
            case Trailer.Moon:
                overlay.ShowTrailerHook("MÊME SUR LA LUNE ?", "Un tir pour atteindre le mur d’acier.", elapsed, 3.3f);
                break;
        }
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
            if (openingShot)
            {
                FollowOpeningShot(elapsed);
                return;
            }

            float t = Mathf.SmoothStep(0f, 1f, Mathf.InverseLerp(0f, edit.Cuts[1].Start, elapsed));
            Vector3 from = new Vector3(-7f, 3.7f, -1.6f);
            float wallDistance = FitDistance(Mathf.Max(1f, wall.WallWidth) * 0.6f, bossHeight * 0.58f);
            Vector3 to = new Vector3(boss.wallDistance - wallDistance, bossHeight * 0.46f, -0.8f);
            Vector3 position = Vector3.Lerp(from, to, t);
            Vector3 target = Vector3.Lerp(position + new Vector3(23f, -0.8f, 0.4f),
                new Vector3(boss.wallDistance, bossHeight * 0.5f, 0f), t);
            SetCamera(position, target, 1f);
            return;
        }

        if (take == Take.LunarEstablishing)
        {
            FrameLunarEstablishing(clock - takeTime);
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
            if (edit.PlayEvolution)
                FrameCanonEvolution(launcherPosition, aimDistance, clock - takeTime);
            else
                FrameEquippedCanon(launcherPosition, aimDistance, clock - takeTime);
            return;
        }

        float cut = Mathf.Clamp01((clock - takeTime) / edit.MissLeadIn);
        Vector3 aimOffset = new Vector3(-aimDistance, 2.9f, Mathf.Lerp(-1.5f, -0.8f, cut));
        float lead = take == Take.Miss
            ? Mathf.SmoothStep(0.6f, 2.4f, (clock - takeTime - edit.MissLeadIn + 0.3f) / 0.6f)
            : 2.4f;
        SetCamera(launcherPosition + aimOffset, launcherPosition + new Vector3(lead, 0.7f, 0f), 1f);
    }

    private void FrameCanonEvolution(Vector3 launcherPosition, float aimDistance, float elapsed)
    {
        // La fronde occupe le premier plan. Le cadre s'ouvre avant que sa silhouette
        // alterne avec celle du canon, plus large, et accompagne les rayons.
        float widen = Mathf.SmoothStep(0f, 1f, Mathf.InverseLerp(0.45f, 1.35f, elapsed));
        float slide = Mathf.SmoothStep(0f, 1f, Mathf.InverseLerp(0f, canonEvolutionEnd, elapsed));
        float slingDistance = Mathf.Max(4.4f, FitDistance(0.95f, 1.65f) + 0.5f);
        float canonDistance = Mathf.Max(8.5f, FitDistance(2.13f, 1.55f) + 1.05f);
        Vector3 closePosition = new Vector3(-Mathf.Lerp(slingDistance, canonDistance, widen),
            Mathf.Lerp(1.35f, 2.35f, widen), Mathf.Lerp(-1.15f, -1.55f, slide));
        Vector3 closeTarget = new Vector3(0f, Mathf.Lerp(0.28f, 0.72f, widen), 0f);

        // La caméra reprend l'axe de tir une fois le canon révélé.
        float aim = Mathf.SmoothStep(0f, 1f, Mathf.InverseLerp(canonEvolutionEnd, edit.CanonLeadIn, elapsed));
        Vector3 position = Vector3.Lerp(closePosition, new Vector3(-aimDistance, 2.9f, -0.7f), aim);
        Vector3 target = Vector3.Lerp(closeTarget, new Vector3(2.4f, 0.7f, 0f), aim);
        SetCamera(launcherPosition + position, launcherPosition + target, 1f);
    }

    private void FrameEquippedCanon(Vector3 launcherPosition, float aimDistance, float elapsed)
    {
        float approach = Mathf.SmoothStep(0f, 1f, elapsed / edit.CanonLeadIn);
        float side = trailer == Trailer.Moon ? 1.5f : -1.5f;
        Vector3 from = new Vector3(-Mathf.Max(6f, aimDistance * 0.82f), 1.55f, side);
        Vector3 to = new Vector3(-aimDistance, 2.9f, side * 0.46f);
        Vector3 offset = Vector3.Lerp(from, to, approach);
        Vector3 focus = Vector3.Lerp(new Vector3(0.15f, 0.75f, 0f), new Vector3(2.4f, 0.7f, 0f), approach);
        SetCamera(launcherPosition + offset, launcherPosition + focus, 1f);
    }

    private void FrameLunarEstablishing(float elapsed)
    {
        float travel = Mathf.Clamp01(elapsed / edit.Cuts[1].Start);
        Vector3 position = launcher.transform.position + Vector3.Lerp(
            new Vector3(-13f, 1.45f, 1.45f), new Vector3(12f, 5.3f, 1.1f), travel);
        Vector3 focus = position + new Vector3(19f, Mathf.Lerp(3.35f, 2.5f, travel), -2.65f);
        SetCamera(position, focus, 1f);
    }

    private void FollowOpeningShot(float elapsed)
    {
        Vector3 ball = projectile.transform.position;
        float reveal = projectile.WallsBroken > 0
            ? Mathf.SmoothStep(0f, 1f, Mathf.InverseLerp(boss.wallDistance - 22f, boss.wallDistance - 5f, ball.x))
            : 0f;
        float distance = Mathf.Max(9f, FitDistance(wall.WallWidth * 0.61f, 0f));
        Vector3 position = ball + new Vector3(-distance - reveal * 5.5f, 3.2f + reveal * 3.8f, -0.7f);
        Vector3 focus = Vector3.Lerp(ball + new Vector3(8f, 0.65f, 0f),
            new Vector3(boss.wallDistance, Mathf.Min(8.5f, bossHeight * 0.4f), 0f), reveal);
        float blend = elapsed < 0.12f ? 1f : 1f - Mathf.Exp(-13f * frameDelta);
        SetCamera(position, focus, blend);
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
        StopAllCoroutines();
        evolution.Clear();
        AdCaptureAudio.StopEvolution();
        AdCaptureRecorder.StopCurrent();
        if (!Application.isPlaying || game == null || projectile == null || models == null ||
            launcher == null || view == null || overlay == null)
            return;

        Time.timeScale = 1f;
        projectile.RunEnded -= OnShotEnded;
        projectile.ResetToLauncher();
        models.SetCaptureModel(-1);
        if (hasTake && world != null) world.SetCaptureTheme(null);
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
