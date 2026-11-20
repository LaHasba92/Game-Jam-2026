using UnityEngine;

// La visee : poser le doigt dans la moitie basse de l'ecran, glisser pour viser
// (gauche/droite = direction, haut/bas = angle, borne par l'amelioration Angle),
// la jauge de puissance va et vient, relacher = tirer. Fonctionne a la souris et au doigt.
public class LauncherController : MonoBehaviour
{
    [SerializeField] private ProjectileController projectile;
    [SerializeField] private Transform launchPoint;            // d'ou part le boulet
    [SerializeField] private float dotSpacing = 0.08f;         // s entre deux points des pointilles
    [SerializeField] private float dotSize = 0.15f;            // m
    [SerializeField] private Color dotColor = Color.white;

    [Header("Ameliorations (plus tard : lues dans l'UpgradeManager de Nathan)")]
    [SerializeField] private float basePower = 25f;            // Puissance, m/s
    [SerializeField] private float launcherMultiplier = 1f;    // fronde x1 ... canon x3
    [SerializeField] private float maxElevation = 30f;         // Angle

    [Header("Reglages de visee (AimTuning)")]
    [SerializeField] private float minElevation = 5f;
    [SerializeField] private float maxSide = 15f;              // degres a gauche et a droite
    [SerializeField] private float pixelsForFullAim = 300f;    // glissement pour aller d'un bord a l'autre
    [SerializeField] private bool invertVertical = true;       // vrai = facon fronde : tirer vers le bas monte l'angle
    [SerializeField] private float gaugePeriod = 1.2f;         // s pour aller de 0 a 100 %
    [SerializeField] private float minGaugeMultiplier = 0.5f;
    [SerializeField] private float maxGaugeMultiplier = 1.25f;
    [SerializeField] private float perfectZone = 0.12f;        // les 12 derniers % de la jauge = x1,25
    [SerializeField] private float previewDuration = 1.5f;     // s de trajectoire montree

    // Appele par UpgradeApplier au debut de chaque partie (valeurs des ameliorations de Nathan)
    public void SetUpgrades(float power, float launcherPower, float maxAngle)
    {
        basePower = power;
        launcherMultiplier = launcherPower;
        maxElevation = maxAngle;
        Elevation = Mathf.Min(Elevation, maxElevation);
    }

    public bool IsAiming { get; private set; }
    public float Gauge { get; private set; }                    // 0 a 1, pour l'interface de Hugo
    public bool IsPerfect => Gauge >= 1f - perfectZone;
    public float Elevation { get; private set; } = 20f;
    public float Side { get; private set; }

#if UNITY_EDITOR
    public Vector3 CaptureLaunchOrigin => launchPoint.position;

    public void ClearCaptureAim()
    {
        IsAiming = false;
        SetPreviewVisible(false);
    }
#endif

    // Temporaire : sera remplace par GameEvents.OnShotFired.
    public event System.Action<float> ShotFired;   // la puissance (multiplicateur de jauge)

    private readonly System.Collections.Generic.List<Transform> dots = new System.Collections.Generic.List<Transform>();
    private Vector3 pointerStart;
    private float aimStartTime;
    private float startElevation;
    private float startSide;

    private void Update()
    {
        if (projectile.IsFlying)
        {
            SetPreviewVisible(false);
            return;
        }

        bool canAim = GameManager.Instance == null || GameManager.Instance.CurrentState == GameState.Aiming;
        if (!IsAiming && canAim && Input.GetMouseButtonDown(0) && Input.mousePosition.y < Screen.height * 0.5f)
        {
            IsAiming = true;
            pointerStart = Input.mousePosition;
            aimStartTime = Time.time;
            startElevation = Elevation;
            startSide = Side;
        }
        if (!IsAiming)
        {
            return;
        }

        // Glisser = viser, en partant de la visee precedente
        Vector3 delta = Input.mousePosition - pointerStart;
        float vertical = invertVertical ? -delta.y : delta.y;
        float range = maxElevation - minElevation;
        Elevation = Mathf.Clamp(startElevation + vertical / pixelsForFullAim * range, minElevation, maxElevation);
        Side = Mathf.Clamp(startSide - delta.x / pixelsForFullAim * maxSide * 2f, -maxSide, maxSide);

        Gauge = Mathf.PingPong((Time.time - aimStartTime) / gaugePeriod, 1f);
        DrawPreview();

        if (Input.GetMouseButtonUp(0))
        {
            Fire();
        }
    }

    private Vector3 LaunchVelocity()
    {
        float gaugeMultiplier = IsPerfect
            ? maxGaugeMultiplier
            : Mathf.Lerp(minGaugeMultiplier, 1f, Gauge / (1f - perfectZone));
        float speed = basePower * gaugeMultiplier * launcherMultiplier;
        // Tout droit (+X), tourne vers le haut de l'angle de tir, puis sur le cote
        Vector3 direction = Quaternion.Euler(0f, Side, Elevation) * Vector3.right;
        return direction * speed;
    }

    private void Fire()
    {
        IsAiming = false;
        SetPreviewVisible(false);
        Vector3 launchVelocity = LaunchVelocity();
        projectile.Launch(launchPoint.position, launchVelocity);
        float gaugeMultiplier = launchVelocity.magnitude / (basePower * launcherMultiplier);
        ShotFired?.Invoke(gaugeMultiplier);
        GameEvents.RaiseShotFired(new ShotInfo
        {
            direction = Side,
            angle = Elevation,
            gaugeMultiplier = gaugeMultiplier,
            launcherIndex = 1,   // plus tard : le lanceur actif (Nathan)
        });
    }

    // Les pointilles : la meme physique que le vol, sur previewDuration secondes, sans rebond.
    // Des petites spheres creees une seule fois (pool), placees le long de la trajectoire.
    private void DrawPreview()
    {
        const float dt = 0.02f;
        int stepsPerDot = Mathf.Max(1, Mathf.RoundToInt(dotSpacing / dt));
        int dotCount = Mathf.CeilToInt(previewDuration / dotSpacing);
        Vector3 position = launchPoint.position;
        Vector3 velocity = LaunchVelocity();
        bool rolling = false;
        int shown = 0;
        for (int d = 0; d < dotCount; ++d)
        {
            bool touchedGround = false;
            for (int k = 0; k < stepsPerDot && !touchedGround; ++k)
            {
                touchedGround = BallPhysics.Step(ref position, ref velocity, ref rolling, projectile.Physics,
                                                 dt, projectile.CorridorHalfWidth, projectile.Ceiling);
            }
            if (touchedGround)   // on ne montre jamais les rebonds (GDD)
            {
                break;
            }
            Transform dot = GetDot(d);
            dot.position = position;
            dot.gameObject.SetActive(true);
            shown = d + 1;
        }
        for (int d = shown; d < dots.Count; ++d)
        {
            dots[d].gameObject.SetActive(false);
        }
    }

    private Transform GetDot(int index)
    {
        while (dots.Count <= index)
        {
            GameObject dot = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            Destroy(dot.GetComponent<Collider>());
            dot.name = "Dot";
            dot.transform.SetParent(transform);
            dot.transform.localScale = Vector3.one * dotSize;
            dot.GetComponent<Renderer>().material.color = dotColor;
            dots.Add(dot.transform);
        }
        return dots[index];
    }

    private void SetPreviewVisible(bool visible)
    {
        if (visible)
        {
            return;   // DrawPreview affiche les points qu'il faut
        }
        foreach (Transform dot in dots)
        {
            dot.gameObject.SetActive(false);
        }
    }

    // Affichage de debug en attendant l'interface de Hugo : jauge verticale sur le cote gauche.
    private void OnGUI()
    {
        if (!IsAiming)
        {
            return;
        }
        float height = Screen.height * 0.4f;
        float x = 20f, y = Screen.height * 0.3f, width = 30f;
        GUI.Box(new Rect(x, y, width, height), "");
        GUI.Box(new Rect(x, y, width, height * perfectZone), "");            // la zone PERFECT, en haut
        float filled = height * Gauge;
        GUI.color = IsPerfect ? Color.green : (Gauge > 0.6f ? Color.yellow : Color.gray);
        GUI.Box(new Rect(x, y + height - filled, width, filled), "");          // se remplit du bas vers le haut
        GUI.color = Color.white;
        if (IsPerfect)
        {
            GUI.Label(new Rect(x + width + 8f, y, 200f, 30f), "PERFECT");
        }
        GUI.Label(new Rect(x, y + height + 8f, 400f, 30f), $"Angle {Elevation:0} deg   Direction {Side:0} deg");
    }
}
