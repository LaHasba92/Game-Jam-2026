using UnityEngine;
using UnityEngine.InputSystem;

// La visee : poser le doigt dans la moitie basse de l'ecran, glisser pour viser
// (gauche/droite = direction, haut/bas = angle, borne par l'amelioration Angle),
// la jauge de puissance va et vient, relacher = tirer. Fonctionne a la souris et au doigt.
public class LauncherController : MonoBehaviour
{
    [SerializeField] private ProjectileController projectile;
    [SerializeField] private RunUpgrades upgrades;
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

    // La sandbox Sami fournit ces valeurs. Dans Game, la boutique lit les fiches équipées.
    public void SetUpgrades(float power, float launcherPower, float maxAngle)
    {
        if (upgrades != null) return;
        basePower = power;
        launcherMultiplier = launcherPower;
        maxElevation = maxAngle;
        Elevation = Mathf.Min(Elevation, maxElevation);
    }

    public bool IsAiming { get; private set; }
    public float Gauge { get; private set; }                    // 0 a 1, pour l'interface de Hugo
    private AimTuning Tuning => AimTuning.Current;
    public ChargeKind Kind => Tuning != null ? Tuning.Kind(upgrades != null ? upgrades.SelectedIndex : 0) : ChargeKind.Standard;
    public float SafeTop => Kind == ChargeKind.Sling ? 1f - Tuning.redWidth : 1f;
    public float GreenStart => Mathf.Max(0f, SafeTop - perfectZone);
    public bool IsRed => Kind == ChargeKind.Sling && JamRules.IsRed(Gauge, Tuning.redWidth);
    public bool IsPerfect => Gauge >= GreenStart && !IsRed;
    public float WarningStrength { get; private set; }
    private float warningSince;
    private bool pendingFailure, stagedAim;
    public Transform LaunchPoint => launchPoint;
    public int SelectedSlot => upgrades != null ? upgrades.SelectedIndex : 0;
    public float Elevation { get; private set; } = 20f;
    public float Side { get; private set; }

    // Temporaire : sera remplace par GameEvents.OnShotFired.
    public event System.Action<float> ShotFired;   // la puissance (multiplicateur de jauge)

    private readonly System.Collections.Generic.List<Transform> dots = new System.Collections.Generic.List<Transform>();
    private Pointer pointer;
    private Vector2 pointerStart;
    public bool ShowDebugGauge { get; set; } = true;
    private float aimStartTime;
    private float startElevation;
    private float startSide;

    private void Start()
    {
        if (Tuning != null && GetComponent<JamLauncherFx>() == null) gameObject.AddComponent<JamLauncherFx>();
    }

    private void OnDisable()
    {
        StopAllCoroutines();
        pendingFailure = stagedAim = false;
        CancelAim();
    }

    private void CancelAim()
    {
        IsAiming = false;
        if (WarningStrength > 0f) GameEvents.RaiseChargeWarning(Kind, 0f);
        WarningStrength = 0f;
        pointer = null;
        SetPreviewVisible(false);
        SfxManager.SetTension(0f);
    }

    private void Update()
    {
        if (pendingFailure || stagedAim) return;
        if (Time.timeScale <= 0f || JamCaptureMenu.IsPanelOpen)
        {
            if (IsAiming) CancelAim();
            return;
        }
        bool canAim = GameManager.Instance == null || GameManager.Instance.CurrentState == GameState.Aiming;
        if (!canAim || projectile == null || !projectile.CanLaunch || launchPoint == null)
        {
            if (IsAiming) CancelAim();
            return;
        }

        if (!IsAiming)
        {
            pointer = GameplayPointer.PressedThisFrame();
            if (pointer == null) return;
            pointerStart = pointer.position.ReadValue();
            if (pointerStart.y >= Screen.height * 0.5f)
            {
                pointer = null;
                return;
            }

            IsAiming = true;
            aimStartTime = Time.time;
            startElevation = Elevation;
            startSide = Side;
        }

        if (!pointer.added)
        {
            CancelAim();
            return;
        }

        Vector2 delta = pointer.position.ReadValue() - pointerStart;
        float vertical = invertVertical ? -delta.y : delta.y;
        float aimPixels = Mathf.Max(1f, pixelsForFullAim);
        float range = maxElevation - minElevation;
        Elevation = Mathf.Clamp(startElevation + vertical / aimPixels * range, minElevation, maxElevation);
        Side = Mathf.Clamp(startSide - delta.x / aimPixels * maxSide * 2f, -maxSide, maxSide);

        if (pointer is Touchscreen screen && screen.primaryTouch.phase.ReadValue() == UnityEngine.InputSystem.TouchPhase.Canceled)
        {
            CancelAim();
            return;
        }
        float elapsed = Time.time - aimStartTime;
        UpdateCharge(elapsed);
        if (Kind == ChargeKind.Cannon && elapsed >= Tuning.maximumHold)
        {
            BeginFailure(Kind);
            return;
        }

        if (pointer.press.wasReleasedThisFrame)
        {
            bool canceled = pointer is Touchscreen touch &&
                            touch.primaryTouch.phase.ReadValue() == UnityEngine.InputSystem.TouchPhase.Canceled;
            if (canceled) CancelAim();
            else if (IsRed) BeginFailure(Kind);
            else Fire();
        }
        else if (!pointer.press.isPressed)
        {
            CancelAim();
        }
        else
        {
            DrawPreview();
        }
    }

    private void UpdateCharge(float elapsed)
    {
        Gauge = Kind == ChargeKind.Cannon ? Mathf.Clamp01(elapsed / Tuning.optimalHold)
            : Mathf.PingPong(elapsed / Mathf.Max(0.01f, gaugePeriod), 1f);
        float strength = Kind == ChargeKind.Cannon
            ? JamRules.CannonDanger(elapsed, Tuning.maximumHold, Tuning.warningTime)
            : IsRed ? Mathf.Lerp(Tuning.warningAtEntry, 1f, Mathf.InverseLerp(SafeTop, 1f, Gauge)) : 0f;
        if (strength > 0f && WarningStrength <= 0f) warningSince = Time.time;
        if (strength > 0f || WarningStrength > 0f) GameEvents.RaiseChargeWarning(Kind, strength);
        WarningStrength = strength;
        SfxManager.SetTension(Gauge);
    }

    private void BeginFailure(ChargeKind kind)
    {
        if (!pendingFailure) StartCoroutine(FinishFailure(kind));
    }

    private System.Collections.IEnumerator FinishFailure(ChargeKind kind)
    {
        pendingFailure = true;
        Vector3 rescueVelocity = LaunchVelocity();
        // Même si le joueur lâche à la première image rouge, on lui montre l'alerte.
        float remaining = Mathf.Max(0f, Tuning.minimumWarning - (Time.time - warningSince));
        if (remaining > 0f) yield return new WaitForSeconds(remaining);
        pendingFailure = false;
        CancelAim();
        if (GameManager.Instance != null && GameManager.Instance.CurrentState != GameState.Aiming) yield break;
        if (kind == ChargeKind.Sling) GameEvents.RaiseLauncherBroken();
        else GameEvents.RaiseCannonExploded();
        projectile.FailBeforeLaunch(kind == ChargeKind.Sling ? RunEndReason.SlingBroken : RunEndReason.CannonExploded,
            launchPoint.position, rescueVelocity);
    }

    public void StageFailure()
    {
        if (JamSave.Preview && !pendingFailure && !stagedAim && projectile.CanLaunch)
            StartCoroutine(ShowCharge());
    }

    public void CancelStaging()
    {
        StopAllCoroutines();
        stagedAim = pendingFailure = false;
        CancelAim();
    }

    private System.Collections.IEnumerator ShowCharge()
    {
        stagedAim = IsAiming = true;
        float elapsed = 0f;
        do
        {
            UpdateCharge(elapsed);
            if (IsRed || (Kind == ChargeKind.Cannon && elapsed >= Tuning.maximumHold)) break;
            elapsed += Time.deltaTime;
            yield return null;
        } while (JamSave.Preview && GameManager.Instance.CurrentState == GameState.Aiming);
        stagedAim = false;
        if (JamSave.Preview && GameManager.Instance.CurrentState == GameState.Aiming) BeginFailure(Kind);
        else CancelAim();
    }

    private float GaugeMultiplier => IsPerfect ? maxGaugeMultiplier
        : Mathf.Lerp(minGaugeMultiplier, 1f, Gauge / Mathf.Max(0.01f, GreenStart));

    private Vector3 LaunchVelocity()
    {
        float power = upgrades != null ? upgrades.Value(0, basePower) : basePower;
        float multiplier = upgrades != null ? upgrades.LauncherMultiplier : launcherMultiplier;
        float speed = power * GaugeMultiplier * multiplier;
        // Tout droit (+X), tourne vers le haut de l'angle de tir, puis sur le cote
        Vector3 direction = Quaternion.Euler(0f, Side, Elevation) * Vector3.right;
        return direction * speed;
    }

    private void Fire()
    {
        CancelAim();
        Vector3 launchVelocity = LaunchVelocity();
        projectile.Launch(launchPoint.position, launchVelocity);
        float gaugeMultiplier = GaugeMultiplier;
        ShotFired?.Invoke(gaugeMultiplier);
        GameEvents.RaiseShotFired(new ShotInfo
        {
            direction = Side,
            angle = Elevation,
            gaugeMultiplier = gaugeMultiplier,
            launcherIndex = upgrades != null ? upgrades.SelectedIndex + 1 : 1,
        });
    }

    // Les pointilles : la meme physique que le vol, sur previewDuration secondes, sans rebond.
    // Des petites spheres creees une seule fois (pool), placees le long de la trajectoire.
    private void DrawPreview()
    {
        const float dt = 0.02f;
        int stepsPerDot = Mathf.Max(1, Mathf.RoundToInt(dotSpacing / dt));
        int dotCount = Mathf.CeilToInt(previewDuration / Mathf.Max(dt, dotSpacing));
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
        if (!ShowDebugGauge || !IsAiming)
        {
            return;
        }
        float height = Screen.height * 0.4f;
        float x = 20f, y = Screen.height * 0.3f, width = 30f;
        GUI.Box(new Rect(x, y, width, height), "");
        GUI.Box(new Rect(x, y, width, height * perfectZone), "");            // la zone PERFECT, en haut
        float filled = height * Gauge;
        GUI.color = IsRed || WarningStrength > 0f ? Tuning.red : IsPerfect ? Color.green : (Gauge > 0.6f ? Color.yellow : Color.gray);
        GUI.Box(new Rect(x, y + height - filled, width, filled), "");          // se remplit du bas vers le haut
        GUI.color = Color.white;
        if (IsPerfect)
        {
            GUI.Label(new Rect(x + width + 8f, y, 200f, 30f), "PERFECT");
        }
        GUI.Label(new Rect(x, y + height + 8f, 400f, 30f), $"Angle {Elevation:0} deg   Direction {Side:0} deg");
    }
}
