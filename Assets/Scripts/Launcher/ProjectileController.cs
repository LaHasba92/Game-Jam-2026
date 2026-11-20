using UnityEngine;

// Le vrai boulet : vol calcule (BallPhysics), pilotage gauche/droite en vol, objets du sol,
// impact sur les murs, fin de partie et bouton « Passer ».
public class ProjectileController : MonoBehaviour
{
    [SerializeField] private WallController wall;
    [SerializeField] private WorldStreamer world;
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

    public bool IsFlying { get; private set; }
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
    private float pointerStartX;
    private bool hasPlayed;   // une partie a deja eu lieu : la suivante repart du mur 1

    private void Awake()
    {
        startPosition = transform.position;
        ResetToLauncher();
    }

    // Debut d'une partie : le boulet revient sur le lanceur (appele par le GameManager de Benji).
    public void ResetToLauncher()
    {
        IsFlying = false;
        WallsBroken = 0;
        position = startPosition;
        velocity = Vector3.zero;
        rolling = false;
        slowTimer = 0f;
        flightTime = 0f;
        lastObject = null;
        transform.position = position;
    }

    // Appele par le LauncherController au lacher.
    public void Launch(Vector3 from, Vector3 launchVelocity)
    {
        if (hasPlayed && levelManager != null)
        {
            levelManager.StartNewRun();   // en attendant le GameManager de Benji
        }
        hasPlayed = true;
        ResetToLauncher();
        position = from;
        velocity = launchVelocity;
        IsFlying = true;
        pointerStartX = Input.mousePosition.x;
    }

    private void Update()
    {
        if (!IsFlying)
        {
            return;
        }
        // Le doigt (ou la souris) se repose pour piloter : on part de la ou il touche
        if (Input.GetMouseButtonDown(0))
        {
            pointerStartX = Input.mousePosition.x;
        }
        if (CanSkip && Input.GetKeyDown(KeyCode.Space))
        {
            Skip();
        }
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
        if (readInput && !rolling)
        {
            // Pilotage direct : la vitesse de cote suit le doigt tout de suite (droite de l'ecran = -Z).
            // Sans doigt, on garde sa vitesse de cote actuelle.
            float input = SteeringInput();
            if (Mathf.Abs(input) > 0.01f || Input.GetMouseButton(0))
            {
                velocity.z = Mathf.MoveTowards(velocity.z, -input * steeringSpeed, steeringResponse * dt);
            }
        }

        BallPhysics.Step(ref position, ref velocity, ref rolling, physics, dt, CorridorHalfWidth, Ceiling);
        HandleGroundObjects();
        HandleWall();

        slowTimer = Mathf.Abs(velocity.x) < stopSpeed ? slowTimer + dt : 0f;
        if (slowTimer >= stopDelay)
        {
            EndRun();
        }
    }

    // -1 a 1 : glissement du doigt depuis l'endroit ou il s'est pose, ou fleches / A-D au clavier.
    private float SteeringInput()
    {
        float keys = Input.GetAxisRaw("Horizontal");
        if (Mathf.Abs(keys) > 0.01f)
        {
            return keys;
        }
        if (!Input.GetMouseButton(0))
        {
            return 0f;
        }
        return Mathf.Clamp((Input.mousePosition.x - pointerStartX) / steeringPixels, -1f, 1f);
    }

    private void HandleGroundObjects()
    {
        if (world == null || !world.TryGetObjectAt(position, physics.radius, out GroundObject obj) || obj == lastObject)
        {
            return;
        }
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

    private void HandleWall()
    {
        if (position.x + physics.radius < wall.Distance)
        {
            return;
        }
        float wallX = wall.Distance;   // a lire AVANT ApplyHit : le mur suivant est place aussitot
        wall.Elan = elan;
        WallHitResult result = wall.ApplyHit(new WallHit
        {
            impactHeight = position.y,
            impactSide = position.z,
            impactSpeed = velocity.magnitude,
            rolling = rolling,
        });
        StartCoroutine(wall.PlayHit(result));

        if (result.broken)
        {
            ++WallsBroken;
            velocity *= result.exitSpeedFactor;
            position.x = wallX + 1.5f;
        }
        else
        {
            velocity.x *= result.exitSpeedFactor;
            position.x = wallX - physics.radius - 0.05f;
        }
    }

    // « Passer » : calcule la fin du vol d'un coup, sans affichage.
    public void Skip()
    {
        for (int i = 0; i < 100000 && IsFlying; ++i)
        {
            Simulate(Time.fixedDeltaTime, false);
        }
        transform.position = position;
    }

    private void EndRun()
    {
        IsFlying = false;
        RunResult result = new RunResult
        {
            distance = position.x,
            wallsBroken = WallsBroken,
        };
        Debug.Log($"FIN : {WallsBroken} murs casses, {position.x:0} m");
        RunEnded?.Invoke(result);
    }
}
