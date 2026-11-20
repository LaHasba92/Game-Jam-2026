using UnityEngine;
using UnityEngine.InputSystem;

// Le vrai boulet : vol calcule (BallPhysics), pilotage gauche/droite en vol, objets du sol,
// impact sur les murs, fin de partie et bouton « Passer ».
public class ProjectileController : MonoBehaviour
{
    [SerializeField] private WallController wall;
    [SerializeField] private WorldStreamer world;
    [SerializeField] private RunUpgrades upgrades;
    [SerializeField] private LevelManager levelManager;      // pour recommencer au mur 1 a chaque partie
    [SerializeField] private BallPhysics.Params physics = new BallPhysics.Params();

    [Header("Ameliorations (plus tard : lues dans l'UpgradeManager de Nathan)")]
    [SerializeField] private float elan = 0.6f;
    [Header("Pilotage en vol")]
    [SerializeField] private float steeringSpeed = 12f;       // m/s de cote au maximum
    [SerializeField] private float steeringResponse = 60f;    // m/s2 : vitesse a laquelle on atteint cette vitesse
    [SerializeField] private float steeringPixels = 120f;     // glissement du doigt pour aller au maximum
    [Header("Fin de partie")]
    [SerializeField] private float stopSpeed = 1f;            // m/s
    [SerializeField] private float stopDelay = 0.5f;          // s
    [SerializeField] private float skipAfter = 5f;            // s de vol avant le bouton « Passer »

    public void SetUpgrades(float drag, float bounce, float momentum)
    {
        if (upgrades != null) return;
        physics.drag = drag;
        physics.bounce = bounce;
        elan = momentum;
    }

    public bool IsFlying { get; private set; }
    public bool CanLaunch => isActiveAndEnabled && !IsFlying && wall != null && wall.HasWall;
    public int CoinsPicked { get; private set; }
    public int LastReward { get; private set; }
    public bool HasShot => lastLaunchVelocity.sqrMagnitude > 0f;
    public int WallsBroken { get; private set; }
    public float Distance => position.x;
    public bool CanSkip => IsFlying && flightTime >= skipAfter;
    public Vector3 Velocity => velocity;
    public BallPhysics.Params Physics => physics;
    public float CorridorHalfWidth => wall.WallWidth * 0.5f;
    public float Ceiling => wall.WallHeight;

    // Temporaire : sera remplace par GameEvents.OnRunEnded.
    public event System.Action<RunResult> RunEnded;

    private Vector3 startPosition;
    private Vector3 position;
    private Vector3 velocity;
    private bool rolling;
    private float slowTimer;
    private float flightTime;
    private GroundObject lastObject;
    private Pointer pointer;
    private Vector2 pointerStart;
    private Vector2 steering;
    private bool steeringActive;
    private int creditedReward;
    private bool hasPlayed;   // sans GameManager (sandbox) : la partie suivante repart du mur 1
    private Vector3 lastLaunchVelocity;
    private float lastBrokenWallX;
    private Coroutine drop;
    private void StopDrop()
    {
        if (drop != null) StopCoroutine(drop);
        drop = null;
    }

    public void FailBeforeLaunch(RunEndReason reason, Vector3 from, Vector3 rescueVelocity)
    {
        StopDrop();
        ResetToLauncher();
        position = from;
        transform.position = from;
        lastLaunchVelocity = rescueVelocity;
        EndRun(reason);
        drop = StartCoroutine(DropBall(from));
    }

    private System.Collections.IEnumerator DropBall(Vector3 from)
    {
        AimTuning tuning = AimTuning.Current;
        Vector3 end = new Vector3(from.x + tuning.ballDropForward, physics.radius, from.z);
        for (float t = 0f; t < 1f; t += Time.deltaTime / Mathf.Max(0.01f, tuning.ballDropSeconds))
        {
            transform.position = Vector3.Lerp(from, end, t * t);
            yield return null;
        }
        transform.position = end;
        drop = null;
    }

    private void OnEnable()
    {
        GameEvents.OnStateChanged += OnStateChanged;
        GameEvents.OnReviveRequested += Revive;
    }

    private void OnDisable()
    {
        GameEvents.OnStateChanged -= OnStateChanged;
        GameEvents.OnReviveRequested -= Revive;
    }

    private void OnStateChanged(GameState from, GameState to)
    {
        if (to == GameState.Aiming)
        {
            ResetToLauncher();
        }
    }

    // Pub « relancer » : le boulet repart du dernier mur casse avec sa vitesse de depart.
    private void Revive()
    {
        if (!HasShot) return;
        StopDrop();
        position = WallsBroken == 0 ? startPosition : new Vector3(lastBrokenWallX + 1.5f, 2f, 0f);
        if (world != null) world.RewindTo(position.x);
        velocity = lastLaunchVelocity;
        rolling = false;
        slowTimer = 0f;
        flightTime = 0f;
        lastObject = null;
        ResetSteering();
        transform.position = position;
        IsFlying = true;
    }

    private void Awake()
    {
        startPosition = transform.position;
        ResetToLauncher();
    }

    // Debut d'une partie : le boulet revient sur le lanceur (appele par le GameManager de Benji).
    public void ResetToLauncher()
    {
        StopDrop();
        IsFlying = false;
        WallsBroken = 0;
        CoinsPicked = 0;
        creditedReward = 0;
        LastReward = 0;
        if (upgrades != null)
        {
            physics.drag = upgrades.Value(1, physics.drag);
            physics.bounce = upgrades.Value(4, physics.bounce);
        }
        ResetSteering();
        position = startPosition;
        velocity = Vector3.zero;
        rolling = false;
        slowTimer = 0f;
        flightTime = 0f;
        lastObject = null;
        lastBrokenWallX = 0f;
        lastLaunchVelocity = Vector3.zero;
        transform.position = position;
    }

    // Appele par le LauncherController au lacher.
    public void Launch(Vector3 from, Vector3 launchVelocity)
    {
        if (!CanLaunch) return;
        if (hasPlayed && levelManager != null && GameManager.Instance == null)
        {
            levelManager.StartNewRun();   // en attendant le GameManager de Benji
        }
        hasPlayed = true;
        ResetToLauncher();
        position = from;
        velocity = launchVelocity;
        transform.position = position;
        lastLaunchVelocity = launchVelocity;
        IsFlying = true;
        ResetSteering();
    }

    private void ResetSteering()
    {
        pointer = null;
        steering = Vector2.zero;
        steeringActive = false;
    }

    private void Update()
    {
        if (!IsFlying || Time.timeScale <= 0f) return;

        Pointer pressed = GameplayPointer.PressedThisFrame();
        if (pressed != null && pointer == null)
        {
            pointer = pressed;
            pointerStart = pointer.position.ReadValue();
        }

        if (pointer != null && (!pointer.added || !pointer.press.isPressed))
            pointer = null;

        Keyboard keyboard = Keyboard.current;
        steering = Vector2.zero;
        if (keyboard != null)
        {
            steering.x = (keyboard.rightArrowKey.isPressed || keyboard.dKey.isPressed ? 1f : 0f)
                       - (keyboard.leftArrowKey.isPressed || keyboard.aKey.isPressed ? 1f : 0f);
            steering.y = (keyboard.upArrowKey.isPressed || keyboard.wKey.isPressed ? 1f : 0f)
                       - (keyboard.downArrowKey.isPressed || keyboard.sKey.isPressed ? 1f : 0f);
        }

        if (pointer != null && steering == Vector2.zero)
            steering = Vector2.ClampMagnitude((pointer.position.ReadValue() - pointerStart) / Mathf.Max(1f, steeringPixels), 1f);

        steeringActive = pointer != null || steering != Vector2.zero;
        if (CanSkip && keyboard?.spaceKey.wasPressedThisFrame == true)
            Skip();
    }

    private void FixedUpdate()
    {
        if (IsFlying)
        {
            Simulate(Time.fixedDeltaTime, true);
            transform.position = position;
        }
    }

    // Un pas de vol. readInput = faux pendant « Passer » (on ne pilote pas un vol accelere).
    private void Simulate(float dt, bool readInput)
    {
        flightTime += dt;
        if (readInput && !rolling && steeringActive)
        {
            // Droite de l'écran = -Z. Le geste vertical agit sur Y.
            velocity.z = Mathf.MoveTowards(velocity.z, -steering.x * steeringSpeed, steeringResponse * dt);
            if (Mathf.Abs(steering.y) > 0.01f)
                velocity.y = Mathf.MoveTowards(velocity.y, steering.y * steeringSpeed, steeringResponse * dt);
        }

        Vector3 previous = position;
        bool wasRolling = rolling;
        BallPhysics.Step(ref position, ref velocity, ref rolling, physics, dt, CorridorHalfWidth, Ceiling);
        float wallX = wall.Distance - physics.radius;
        bool reachedWall = previous.x <= wallX && position.x >= wallX;
        if (reachedWall && position.x > previous.x)
            position = Vector3.Lerp(previous, position, (wallX - previous.x) / (position.x - previous.x));

        if (world != null)
        {
            // Le saut de simulation de « Passer » recycle aussi le sol et ses objets.
            if (!readInput) world.AdvanceTo(position.x);
            int collected = world.CollectCoins(previous, position, physics.radius);
            CoinsPicked += collected;
            if (collected > 0 && readInput) SfxManager.PlayCoin();
        }

        if (reachedWall)
        {
            bool rollingAtWall = wasRolling || (rolling && position.y <= physics.radius + 0.001f);
            HandleWall(rollingAtWall, readInput);
            if (!IsFlying || Time.timeScale <= 0f) return;
        }
        else
        {
            HandleGroundObjects();
        }

        slowTimer = Mathf.Abs(velocity.x) < stopSpeed ? slowTimer + dt : 0f;
        if (slowTimer >= stopDelay)
        {
            EndRun();
        }
    }

    private void HandleGroundObjects()
    {
        if (world == null || !world.TryGetObjectAt(position, physics.radius, out GroundObject obj))
        {
            lastObject = null;
            return;
        }
        if (obj == lastObject) return;
        lastObject = obj;
        switch (obj.type)
        {
            case GroundObjectType.Trampoline:   // rebond R = 1,1 a 45 degres
                Vector3 flat = new Vector3(velocity.x, 0f, velocity.z);
                float speed = flat.magnitude * 1.1f;
                velocity = flat.normalized * (speed * 0.707f);
                velocity.y = speed * 0.707f;
                rolling = false;
                break;
            case GroundObjectType.Booster:      // +25 % de vitesse horizontale
                velocity.x *= 1.25f;
                velocity.z *= 1.25f;
                break;
            case GroundObjectType.Balloon:      // remonte de 15 m
                velocity.y = Mathf.Sqrt(2f * physics.gravity * 15f);
                rolling = false;
                world.Consume(obj);
                break;
            case GroundObjectType.Sticky:       // vitesse divisee par 2
                velocity *= 0.5f;
                break;
        }
    }

    private void HandleWall(bool rollingAtWall, bool showImpact)
    {
        transform.position = position;
        float wallX = wall.Distance;   // a lire AVANT ApplyHit : le mur suivant est place aussitot
        RunProgress.ReachWall(WallsBroken + 1);
        wall.Elan = upgrades != null ? upgrades.Value(2, elan) : elan;
        WallHitResult result = wall.ApplyHit(new WallHit
        {
            impactHeight = position.y,
            impactSide = position.z,
            impactSpeed = velocity.magnitude,
            rolling = rollingAtWall,
        });
        if (showImpact || result.broken) StartCoroutine(wall.PlayHit(result));

        if (result.bossHit)
        {
            if (result.broken) ++WallsBroken;
            EndRun(result.broken ? RunEndReason.BossDefeated : RunEndReason.BossSurvived);
            return;
        }
        if (result.broken)
        {
            ++WallsBroken;
            lastBrokenWallX = wallX;
            velocity *= result.exitSpeedFactor;
            position.x = wallX + 1.5f;
        }
        else
        {
            EndRun(RunEndReason.WallIntact);
        }
    }

    public void StageWallImpact(bool boss)
    {
        if (!JamSave.Preview || !CanLaunch || levelManager == null) return;
        if (boss && !levelManager.SelectBossForCapture()) return;
        AimTuning tuning = AimTuning.Current;
        float y = boss ? Mathf.Max(physics.radius, tuning.cameraAltitude) : physics.radius;
        float x = Mathf.Max(0f, wall.Distance - tuning.captureApproachDistance);
        Launch(new Vector3(x, y, 0f), Vector3.right * tuning.captureImpactSpeed);
        CameraDirector director = FindFirstObjectByType<CameraDirector>();
        if (director != null) director.FrameTake(position, false);
        rolling = !boss;
        WallsBroken = levelManager.WallsBroken;
        if (WallsBroken > 0) lastBrokenWallX = levelManager.WallDistance(WallsBroken - 1);
        if (world != null) world.RewindTo(position.x);
        GameEvents.RaiseShotFired(new ShotInfo { gaugeMultiplier = 1f, launcherIndex = upgrades.SelectedIndex + 1 });
    }

    // « Passer » : calcule la fin du vol d'un coup, sans affichage.
    public void Skip()
    {
        if (!CanSkip || Time.timeScale <= 0f) return;
        IsSkipping = true;
        for (int i = 0; i < 100000 && IsFlying; ++i)
        {
            Simulate(Time.fixedDeltaTime, false);
        }
        if (IsFlying) EndRun();
        IsSkipping = false;
        transform.position = position;
    }

    public bool IsSkipping { get; private set; }

    private void EndRun(RunEndReason reason = RunEndReason.Distance)
    {
        IsFlying = false;
        float distance = JamRules.Misfire(reason) ? 0f : position.x;
        float next = levelManager != null ? levelManager.WallDistance(WallsBroken + 1) : wall.Distance;
        RunResult result = new RunResult
        {
            reason = reason,
            distanceToNextWall = float.IsInfinity(next) ? 0f : Mathf.Max(0f, next - distance),
            distance = distance,
            wallsBroken = WallsBroken,
            coinsPicked = CoinsPicked,
            newRecord = RunProgress.UpdateRecord(distance),
        };
        Debug.Log($"FIN : {WallsBroken} murs casses, {position.x:0} m");
        int reward = RunProgress.Reward(result, upgrades != null ? upgrades.Value(3, 1f) : 1f);
        LastReward = Mathf.Max(creditedReward, reward);
        RunProgress.Credit(Mathf.Max(0, reward - creditedReward));
        creditedReward = Mathf.Max(creditedReward, reward);
        RunEnded?.Invoke(result);
        GameEvents.RaiseRunEnded(result);   // le GameManager passe en Resultat
    }
}
