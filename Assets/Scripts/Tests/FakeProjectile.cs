using UnityEngine;

// SANDBOX SEULEMENT : un faux boulet qui suit la physique du GDD, pour tester le mur, le sol
// et les objets sans attendre Andy. Le vrai boulet sera le ProjectileController d'Andy.
// Fleches = viser (haut/bas = angle, borne par maxElevation, gauche/droite = direction). Espace = tirer (dans la jauge).
// R = nouvelle partie. Axe X = la piste, Y = la hauteur, Z = gauche/droite.
public class FakeProjectile : MonoBehaviour
{
    [Header("Liens")]
    [SerializeField] private WallController wall;
    [SerializeField] private LevelManager levelManager;
    [SerializeField] private WorldStreamer world;
    [SerializeField] private Transform followCamera;          // facultatif : la camera suit le boulet

    [Header("Valeurs du GDD (niveau 1) : a changer pour simuler des ameliorations")]
    [SerializeField] private float basePower = 25f;           // Puissance, m/s
    [SerializeField] private float launcherMultiplier = 1f;   // fronde x1 ... canon x3
    [SerializeField] private float drag = 0.10f;              // Aerodynamisme
    [SerializeField] private float bounce = 0.45f;            // Rebond : vitesse VERTICALE gardee au rebond
    [SerializeField] private float groundFriction = 0.9f;     // vitesse HORIZONTALE gardee a chaque rebond
    [SerializeField] private float elan = 0.6f;               // Elan (envoye au mur)
    [SerializeField] private float gravity = 9.81f;
    [SerializeField] private float maxSpeed = 250f;
    [SerializeField] private float rollThreshold = 0.3f;      // m de rebond en dessous duquel on roule
    [SerializeField] private float rollBrake = 1.5f;          // m/s2 : le boulet roule longtemps
    [SerializeField] private float ballRadius = 0.3f;
    [SerializeField] private float maxElevation = 30f;        // amelioration Angle : 30 deg au niveau 1
    [SerializeField] private float steering = 10f;            // m/s2 : pilotage gauche/droite en vol

    [Header("Gains (proposition, a regler par Nathan)")]
    [SerializeField] private float coinsPerMeter = 0.5f;
    [SerializeField] private int wallBonusBase = 10;          // bonus du mur 1
    [SerializeField] private int wallBonusStep = 10;          // +10 a chaque mur : 10, 20, 30...

    private enum State { Aiming, Flying, Ended }
    private State state;
    private Vector3 position;
    private Vector3 velocity;
    private float elevation = 20f;   // degres, 5 a maxElevation
    private float side;              // degres, -15 a 15
    private float gauge;             // 0 a 1, va et vient
    private float slowTimer;
    private bool rolling;
    private int wallsBroken;
    private int wallBonus;            // somme des bonus des murs casses
    private GroundObject lastObject; // pour ne pas declencher deux fois le meme objet
    private string lastResult = "";

    private void Start()
    {
        ResetBall();
    }

    private void ResetBall()
    {
        state = State.Aiming;
        position = new Vector3(0f, 1f, 0f);
        velocity = Vector3.zero;
        rolling = false;
        wallsBroken = 0;
        wallBonus = 0;
        slowTimer = 0f;
        lastObject = null;
        transform.position = position;
    }

    private void Update()
    {
        if (Input.GetKeyDown(KeyCode.R))
        {
            levelManager.StartNewRun();   // murs et sol remis a zero
            ResetBall();
            return;
        }
        if (state != State.Aiming)
        {
            return;
        }

        elevation = Mathf.Clamp(elevation + Input.GetAxisRaw("Vertical") * 40f * Time.deltaTime, 5f, maxElevation);
        side = Mathf.Clamp(side + Input.GetAxisRaw("Horizontal") * 20f * Time.deltaTime, -15f, 15f);
        gauge = Mathf.PingPong(Time.time / 1.2f, 1f);   // aller simple en 1,2 s

        if (Input.GetKeyDown(KeyCode.Space))
        {
            float gaugeMultiplier = Mathf.Lerp(0.5f, 1.25f, gauge);
            float speed = basePower * gaugeMultiplier * launcherMultiplier;
            Vector3 direction = Quaternion.Euler(0f, side, elevation) * Vector3.right;
            velocity = direction * speed;
            state = State.Flying;
            lastResult = "";
        }
    }

    private void FixedUpdate()
    {
        if (state != State.Flying)
        {
            return;
        }
        float dt = Time.fixedDeltaTime;
        wall.Elan = elan;

        // Vol (formules du GDD) : frottement, gravite, vitesse max
        velocity *= 1f - drag * dt;
        if (!rolling)
        {
            velocity.y -= gravity * dt;
        }

        // Pilotage en vol : fleches gauche/droite (ou A/D), seulement en l'air
        if (!rolling)
        {
            velocity.z -= Input.GetAxisRaw("Horizontal") * steering * dt;
        }
        velocity = Vector3.ClampMagnitude(velocity, maxSpeed);
        position += velocity * dt;

        // Couloir : murs invisibles et plafond (taille du mur)
        float halfWidth = wall.WallWidth * 0.5f - ballRadius;
        if (Mathf.Abs(position.z) > halfWidth)
        {
            position.z = Mathf.Sign(position.z) * halfWidth;
            velocity.z = -velocity.z * 0.5f;
        }
        float ceiling = wall.WallHeight - ballRadius;
        if (wall.WallHeight > 0f && position.y > ceiling)   // 0 = pas de plafond
        {
            position.y = ceiling;
            velocity.y = -Mathf.Abs(velocity.y) * 0.5f;
        }

        // Sol : rebond, ou roulement quand le rebond devient trop petit
        if (position.y <= ballRadius)
        {
            position.y = ballRadius;
            if (rolling)
            {
                Vector3 flat = new Vector3(velocity.x, 0f, velocity.z);
                float speed = Mathf.Max(0f, flat.magnitude - rollBrake * dt);
                velocity = flat.normalized * speed;
            }
            else if (velocity.y < 0f)
            {
                // Le rebond ne coupe que la vitesse verticale ; l'horizontale perd juste 10 %
                velocity = new Vector3(velocity.x * groundFriction, -velocity.y * bounce, velocity.z * groundFriction);
                float bounceHeight = velocity.y * velocity.y / (2f * gravity);
                if (bounceHeight < rollThreshold)
                {
                    rolling = true;
                    velocity.y = 0f;
                }
            }
        }

        HandleGroundObjects();
        HandleWall();

        // Fin de partie : vitesse horizontale sous 1 m/s pendant 0,5 s
        slowTimer = Mathf.Abs(velocity.x) < 1f ? slowTimer + dt : 0f;
        if (slowTimer >= 0.5f)
        {
            state = State.Ended;
            Debug.Log($"FIN : {position.x:0} m, {wallsBroken} murs casses, {Coins()} pieces gagnees");
        }

        transform.position = position;
    }

    private void HandleGroundObjects()
    {
        if (world == null || !world.TryGetObjectAt(position, ballRadius, out GroundObject obj) || obj == lastObject)
        {
            return;
        }
        lastObject = obj;
        switch (obj.type)
        {
            case GroundObjectType.Trampoline:   // rebond R = 1,1 a 45 degres
                float speed = new Vector3(velocity.x, 0f, velocity.z).magnitude * 1.1f;
                velocity = new Vector3(velocity.x, 0f, velocity.z).normalized * (speed * 0.707f);
                velocity.y = speed * 0.707f;
                rolling = false;
                break;
            case GroundObjectType.Booster:      // +25 % de vitesse horizontale
                velocity.x *= 1.25f;
                velocity.z *= 1.25f;
                break;
            case GroundObjectType.Balloon:      // remonte de 15 m
                velocity.y = Mathf.Sqrt(2f * gravity * 15f);
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
        if (position.x + ballRadius < wall.Distance)
        {
            return;
        }
        float wallX = wall.Distance;   // a lire AVANT ApplyHit : le mur suivant est place aussitot
        WallHit hit = new WallHit
        {
            impactHeight = position.y,
            impactSide = position.z,
            impactSpeed = velocity.magnitude,
            rolling = rolling,
        };
        WallHitResult result = wall.ApplyHit(hit);
        StartCoroutine(wall.PlayHit(result));

        if (result.broken)
        {
            ++wallsBroken;
            wallBonus += wallBonusBase + wallBonusStep * (wallsBroken - 1);
            velocity *= result.exitSpeedFactor;   // Elan
            position.x = wallX + 1.5f;            // le boulet traverse le mur casse
            lastResult = $"MUR {wallsBroken} CASSE !";
        }
        else
        {
            velocity.x *= result.exitSpeedFactor; // rebond en arriere
            position.x = wallX - ballRadius - 0.05f;
            lastResult = "Le boulet roulait : pas de casse";
        }
    }

    private void LateUpdate()
    {
        if (followCamera == null)
        {
            return;
        }
        Vector3 wanted = transform.position + new Vector3(-10f, 4f, 0f);
        float t = 1f - Mathf.Exp(-5f * Time.deltaTime);
        followCamera.position = Vector3.Lerp(followCamera.position, wanted, t);
        followCamera.rotation = Quaternion.LookRotation(transform.position + Vector3.right * 6f - followCamera.position);
    }

    // Pieces de la partie : la distance + les bonus des murs (la valeur d'Argent s'appliquera plus tard)
    private int Coins()
    {
        return Mathf.RoundToInt(Mathf.Max(0f, position.x) * coinsPerMeter) + wallBonus;
    }

    // Petit affichage de test (OnGUI = interface de debug d'Unity, pas pour le vrai jeu)
    private void OnGUI()
    {
        GUI.skin.label.fontSize = 22;
        string info = state == State.Aiming
            ? $"Angle {elevation:0} deg  Direction {side:0} deg\nJauge {(gauge * 100f):0} %  (Espace pour tirer)"
            : $"Distance {position.x:0} m   Murs {wallsBroken}   Pieces {Coins()}\n{lastResult}";
        if (state == State.Ended)
        {
            info += "\nFIN DE LA PARTIE (R pour rejouer)";
        }
        GUI.Label(new Rect(20, 20, 900, 120), info);
    }
}
