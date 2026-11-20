using UnityEngine;

// Les déformations sont limitées aux visuels. Le point de départ physique reste stable.
public class JamLauncherFx : MonoBehaviour
{
    private AimTuning cfg;
    private LauncherController launcher;
    private Transform prop, sling, cannon, band;
    private Renderer[] metal;
    private MaterialPropertyBlock[] originalColors;
    private bool tinted;
    private JamBurst woodChips, ironChips, confetti;
    private AudioSource alarm;
    private float warning, revealStart = float.NegativeInfinity, flashUntil, sootUntil;
    private bool hidden, lastLanding;
    private string caption;
    private float captionUntil;
    private GUIStyle title;
    private ChargeKind warningKind;

    private void Awake()
    {
        cfg = AimTuning.Current;
        launcher = GetComponent<LauncherController>();
        Transform original = transform.Find("Lanceur_Slingshot");
        prop = JamGeometry.Group(transform, "Visuel lanceur");
        sling = JamGeometry.Group(prop, "Fronde");
        if (original != null) original.SetParent(sling, true);
        else
        {
            JamGeometry.Part(sling, "Manche", PrimitiveType.Cube, Vector3.down * 0.2f,
                new Vector3(0.3f, 1.5f, 0.3f), cfg.wood);
            for (int side = -1; side <= 1; side += 2)
                JamGeometry.Part(sling, "Branche", PrimitiveType.Cube, new Vector3(0f, 0.6f, side * 0.45f),
                    new Vector3(0.22f, 1f, 0.22f), cfg.wood);
        }
        band = JamGeometry.Group(sling, "Élastique tendu");
        for (int side = -1; side <= 1; side += 2)
        {
            var strip = JamGeometry.Part(band, "Brin", PrimitiveType.Cube, new Vector3(-0.22f, 0.8f, side * 0.25f),
                new Vector3(0.07f, 0.08f, 0.75f), cfg.metal);
            strip.localRotation = Quaternion.Euler(0, side * 40f, 0);
        }
        cannon = JamGeometry.Group(prop, "Canon");
        if (cfg.cannonModel != null) Instantiate(cfg.cannonModel, cannon);
        else
        {
            var tube = JamGeometry.Part(cannon, "Tube", PrimitiveType.Cylinder, Vector3.zero,
                new Vector3(0.85f, 0.95f, 0.85f), cfg.metal);
            tube.localRotation = Quaternion.Euler(0, 0, 78f);
            for (int side = -1; side <= 1; side += 2)
            {
                var wheel = JamGeometry.Part(cannon, "Roue", PrimitiveType.Cylinder, new Vector3(0f, -0.5f, side * 0.43f),
                    new Vector3(0.8f, 0.13f, 0.8f), cfg.wood);
                wheel.localRotation = Quaternion.Euler(90f, 0f, 0f);
            }
        }
        metal = cannon.GetComponentsInChildren<Renderer>(true);
        originalColors = new MaterialPropertyBlock[metal.Length];
        for (int i = 0; i < metal.Length; i++)
        {
            originalColors[i] = new MaterialPropertyBlock();
            metal[i].GetPropertyBlock(originalColors[i]);
        }
        woodChips = new JamBurst(transform, cfg, cfg.slingChunk, cfg.wood, cfg.debrisCount, cfg.slingChunkVariants);
        ironChips = new JamBurst(transform, cfg, cfg.cannonChunk, cfg.metal, cfg.debrisCount, cfg.cannonChunkVariants);
        confetti = new JamBurst(transform, cfg, null, cfg.green, cfg.debrisCount);
        alarm = gameObject.AddComponent<AudioSource>();
        alarm.playOnAwake = false;
        alarm.loop = true;
        alarm.spatialBlend = 0f;
    }

    private void OnEnable()
    {
        GameEvents.OnChargeWarning += Warn;
        GameEvents.OnLauncherBroken += Snap;
        GameEvents.OnCannonExploded += Boom;
        GameEvents.OnLauncherUnlocked += Unlock;
        GameEvents.OnCannonReveal += Reveal;
        GameEvents.OnStateChanged += State;
    }
    private void OnDisable()
    {
        GameEvents.OnChargeWarning -= Warn;
        GameEvents.OnLauncherBroken -= Snap;
        GameEvents.OnCannonExploded -= Boom;
        GameEvents.OnLauncherUnlocked -= Unlock;
        GameEvents.OnCannonReveal -= Reveal;
        GameEvents.OnStateChanged -= State;
        if (alarm != null) alarm.Stop();
    }

    private void Warn(ChargeKind kind, float strength)
    {
        warningKind = kind;
        warning = strength;
        if (strength <= 0f) { alarm.Stop(); return; }
        AudioClip clip = kind == ChargeKind.Cannon ? cfg.whistle : cfg.creak;
        if (clip == null) return;
        if (alarm.clip != clip || !alarm.isPlaying) { alarm.clip = clip; alarm.Play(); }
        alarm.volume = cfg.soundVolume;
        alarm.pitch = Mathf.Lerp(cfg.lowPitch, cfg.highPitch, strength);
    }

    private void State(GameState from, GameState to)
    {
        if (to == GameState.Home || to == GameState.Aiming || to == GameState.Flying)
        {
            hidden = false;
            captionUntil = sootUntil = flashUntil = 0f;
            revealStart = float.NegativeInfinity;
            woodChips.Stop(); ironChips.Stop(); confetti.Stop();
            Warn(ChargeKind.Standard, 0f);
        }
    }

    private void Snap()
    {
        hidden = true;
        woodChips.Play(transform.position, cfg.burstSpread, cfg.pieceSize, cfg.dust);
        alarm.PlayOneShot(cfg.snap, cfg.soundVolume);
        Title("CRAC !", cfg.resultDelay);
    }
    private void Boom()
    {
        hidden = true;
        ironChips.Play(transform.position, cfg.burstSpread, cfg.pieceSize, cfg.smoke);
        sootUntil = Time.unscaledTime + cfg.resultDelay;
        alarm.PlayOneShot(cfg.boom, cfg.soundVolume);
        Title("BOUM !", cfg.resultDelay);
    }
    private void Unlock(int index)
    {
        if (index == cfg.cannonIndex) GameEvents.RaiseCannonReveal();
    }
    private void Reveal()
    {
        hidden = false;
        revealStart = Time.unscaledTime;
        lastLanding = false;
        confetti.Play(transform.position, cfg.burstSpread, cfg.pieceSize, cfg.dust);
        alarm.PlayOneShot(cfg.unlock, cfg.soundVolume);
        Title("CANON DÉBLOQUÉ !", cfg.transitionSeconds);
    }
    private void Title(string value, float duration)
    {
        caption = value;
        captionUntil = Time.unscaledTime + duration;
    }

    private void LateUpdate()
    {
        woodChips.Tick(); ironChips.Tick(); confetti.Tick();
        float t = (Time.unscaledTime - revealStart) / Mathf.Max(0.01f, cfg.transitionSeconds);
        bool revealing = t < 1f;
        bool showCannon = revealing ? t >= cfg.confettiPart : launcher.Kind == ChargeKind.Cannon;
        sling.gameObject.SetActive(!hidden && !showCannon);
        cannon.gameObject.SetActive(!hidden && showCannon);
        float wave = Mathf.Sin(Time.time * cfg.shakeHertz * Mathf.PI * 2f);
        prop.localPosition = Vector3.forward * (cfg.shakeMeters * warning * wave);
        float swell = warningKind == ChargeKind.Cannon ? warning * cfg.cannonSwell : 0f;
        cannon.localScale = Vector3.one * (1f + swell);
        bool red = warningKind == ChargeKind.Cannon && warning > 0f;
        if (red || tinted)
        {
            for (int i = 0; i < metal.Length; i++)
            {
                if (red) JamGeometry.Tint(metal[i], Color.Lerp(cfg.metal, cfg.red, warning));
                else metal[i].SetPropertyBlock(originalColors[i]);
            }
        }
        tinted = red;
        band.localScale = new Vector3(1f + cfg.elasticStretch * warning, 1f, 1f);
        float lift = 0f;
        if (revealing && showCannon)
        {
            if (t < cfg.landingPart)
            {
                float fall = Mathf.InverseLerp(cfg.confettiPart, cfg.landingPart, t);
                lift = cfg.transitionHeight * (1f - fall * fall);
            }
            else
            {
                if (!lastLanding)
                {
                    lastLanding = true;
                    flashUntil = Time.unscaledTime + cfg.flashSeconds;
                    ironChips.Play(transform.position, cfg.burstSpread, cfg.pieceSize, cfg.dust);
                }
                float bounce = Mathf.InverseLerp(cfg.landingPart, 1f, t);
                lift = Mathf.Sin(bounce * Mathf.PI) * cfg.revealBounce;
            }
        }
        cannon.localPosition = Vector3.up * lift;
        sling.localPosition = revealing && !showCannon ? Vector3.up * cfg.transitionHeight * (t / cfg.confettiPart) : Vector3.zero;
    }

    private void OnGUI()
    {
        if (Event.current.type != EventType.Repaint) return;
        Color before = GUI.color;
        if (Time.unscaledTime < sootUntil || Time.unscaledTime < flashUntil)
        {
            GUI.color = Time.unscaledTime < flashUntil ? new Color(1f, 1f, 1f, cfg.flashAlpha)
                : new Color(0.06f, 0.06f, 0.08f, cfg.sootAlpha);
            GUI.DrawTexture(new Rect(0, 0, Screen.width, Screen.height), Texture2D.whiteTexture);
            GUI.color = before;
        }
        if (Time.unscaledTime >= captionUntil) return;
        if (title == null) title = new GUIStyle(GUI.skin.label) { alignment = TextAnchor.MiddleCenter, fontStyle = FontStyle.Bold };
        title.fontSize = Mathf.Max(20, Screen.width / 13);
        GUI.Label(new Rect(0, Screen.height * 0.23f, Screen.width, title.fontSize * 2), caption, title);
    }
}
