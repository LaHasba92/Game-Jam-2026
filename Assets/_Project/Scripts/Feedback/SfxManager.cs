using UnityEngine;

// Joue tous les effets sonores du jeu en écoutant GameEvents.
// Aucun autre script n'a besoin de connaître ce fichier : il s'abonne aux événements.
//
// Installation : un GameObject « SfxManager » dans la scène Game, ce script dessus,
// puis glisser les sons dans les cases de l'Inspector (plusieurs variantes par case = tirage au hasard).
//
// Depuis n'importe quel script :  SfxManager.Play(monClip);
// Pour la jauge (Andy) :          SfxManager.SetTension(valeur0a1);
public class SfxManager : MonoBehaviour
{
    public static SfxManager Instance { get; private set; }

    [Header("Visée et tir (GDD)")]
    [Tooltip("Son de tension qui monte pendant la visée (en boucle)")]
    public AudioClip tensionLoop;
    [Tooltip("Claquement au lâcher")]
    public AudioClip[] shot;

    [Header("Murs")]
    [Tooltip("« POW ! » : mur cassé")]
    public AudioClip[] wallBreak;
    [Tooltip("Mur touché sans casser (boulet qui roule)")]
    public AudioClip[] wallBounce;

    [Header("Partie")]
    public AudioClip[] coin;
    public AudioClip runEnd;
    public AudioClip newRecord;

    [Header("Interface")]
    public AudioClip uiClick;

    [Header("Réglages")]
    [Range(0f, 1f)] public float volume = 1f;
    [Tooltip("Variation de hauteur aléatoire, évite l'effet « machine »")]
    [Range(0f, 0.2f)] public float pitchVariation = 0.06f;
    [Tooltip("Hauteur de la tension : départ et arrivée")]
    public float tensionPitchMin = 0.8f, tensionPitchMax = 1.6f;
    [Tooltip("Durée de montée si personne n'appelle SetTension")]
    public float tensionRiseTime = 1.5f;
    [Tooltip("Sans jauge (personne n'appelle SetTension) : la tension joue seule pendant tout l'état Visée")]
    public bool autoTension = false;

    const int PoolSize = 8;
    AudioSource[] pool;
    int next;
    AudioSource tensionSource;
    bool aiming, tensionDriven;
    float aimTime;
    int lastCoins = -1;
    int lastTensionFrame = -10;
    AudioClip lastClip;
    int lastClipFrame = -1;
    int lastWallSoundFrame = -1;

    void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(this); return; }
        Instance = this;
        DontDestroyOnLoad(gameObject);

        pool = new AudioSource[PoolSize];
        for (int i = 0; i < PoolSize; i++)
        {
            pool[i] = gameObject.AddComponent<AudioSource>();
            pool[i].playOnAwake = false;
            pool[i].spatialBlend = 0f; // son 2D : même volume partout, idéal sur mobile
        }

        tensionSource = gameObject.AddComponent<AudioSource>();
        tensionSource.playOnAwake = false;
        tensionSource.loop = true;
        tensionSource.spatialBlend = 0f;

        // Sinon le premier gain de la session (lastCoins = -1) ne sonnerait pas.
        lastCoins = RunProgress.Coins;
    }

    void OnEnable()
    {
        if (Instance != this) return;
        GameEvents.OnStateChanged += HandleState;
        GameEvents.OnShotFired += HandleShot;
        GameEvents.OnWallHit += HandleWallHit;
        GameEvents.OnCoinsChanged += HandleCoins;
        GameEvents.OnRunEnded += HandleRunEnded;
    }

    void OnDisable()
    {
        GameEvents.OnStateChanged -= HandleState;
        GameEvents.OnShotFired -= HandleShot;
        GameEvents.OnWallHit -= HandleWallHit;
        GameEvents.OnCoinsChanged -= HandleCoins;
        GameEvents.OnRunEnded -= HandleRunEnded;
        if (tensionSource != null) StopTension();
    }

    void OnDestroy()
    {
        if (Instance == this) Instance = null;
    }

    void Update()
    {
        if (Instance != this) return;

        // Pause (timeScale 0) : la tension se coupe, la jauge la relancera au prochain geste.
        if (Time.timeScale <= 0f)
        {
            if (tensionSource.isPlaying) tensionSource.Stop();
            return;
        }

        if (tensionDriven)
        {
            // La jauge n'appelle plus SetTension (doigt levé, visée annulée) : silence.
            if (tensionSource.isPlaying && Time.frameCount - lastTensionFrame > 1) tensionSource.Stop();
        }
        else if (aiming && autoTension)
        {
            // Sans jauge, la tension monte toute seule avec le temps
            aimTime += Time.deltaTime;
            ApplyTension(Mathf.Clamp01(aimTime / Mathf.Max(0.01f, tensionRiseTime)));
            if (!tensionSource.isPlaying) PlayTensionLoop();
        }
    }

    // ─── API publique ───

    public static void Play(AudioClip clip, float volumeScale = 1f)
    {
        if (Instance != null && Instance.isActiveAndEnabled) Instance.PlayClip(clip, volumeScale);
    }

    public static void Play(AudioClip[] variants, float volumeScale = 1f)
    {
        if (variants == null || variants.Length == 0) return;
        Play(variants[Random.Range(0, variants.Length)], volumeScale);
    }

    // 0 = élastique détendu, 1 = tension max. À appeler chaque frame par la jauge.
    public static void SetTension(float t)
    {
        if (Instance == null || !Instance.isActiveAndEnabled) return;
        Instance.DriveTension(t);
    }

    public static void PlayClick() => Play(Instance != null ? Instance.uiClick : null);

    // ─── Réactions aux événements ───

    void HandleState(GameState from, GameState to)
    {
        if (to == GameState.Aiming) StartTension();
        else StopTension();
    }

    void HandleShot(ShotInfo info)
    {
        StopTension();
        Play(shot);
    }

    void HandleWallHit(WallHitResult result)
    {
        // « Passer » simule tous les murs dans la même frame : un seul son.
        if (lastWallSoundFrame == Time.frameCount) return;
        lastWallSoundFrame = Time.frameCount;
        Play(result.broken ? wallBreak : wallBounce);
    }

    public static void PlayCoin()
    {
        if (Instance != null) Play(Instance.coin, 0.7f);
    }

    void HandleCoins(int total)
    {
        // On ne joue le son que si le total monte (un achat le fait baisser)
        if (lastCoins >= 0 && total > lastCoins) Play(coin, 0.7f);
        lastCoins = total;
    }

    void HandleRunEnded(RunResult result)
    {
        Play(result.newRecord && newRecord != null ? newRecord : runEnd);
    }

    // ─── Interne ───

    void PlayClip(AudioClip clip, float volumeScale)
    {
        if (clip == null) return;
        // Le même son deux fois dans la frame (pièces sur plusieurs pas physiques, double clic) : une seule fois.
        if (clip == lastClip && Time.frameCount == lastClipFrame) return;
        lastClip = clip;
        lastClipFrame = Time.frameCount;
        AudioSource src = pool[next];
        next = (next + 1) % PoolSize;
        src.pitch = 1f + Random.Range(-pitchVariation, pitchVariation);
        src.PlayOneShot(clip, volume * volumeScale);
    }

    // Entrée dans l'état Visée. Le son démarre avec la jauge (SetTension), pas avant que le doigt se pose.
    void StartTension()
    {
        aiming = true;
        tensionDriven = false;
        aimTime = 0f;
        ApplyTension(0f);
        if (autoTension) PlayTensionLoop();
    }

    void DriveTension(float t)
    {
        tensionDriven = true;
        lastTensionFrame = Time.frameCount;
        ApplyTension(t);
        // t = 0 vient aussi de l'annulation de la visée : on ne relance pas le son pour ça.
        bool canAim = GameManager.Instance == null || GameManager.Instance.CurrentState == GameState.Aiming;
        if (t > 0f && canAim && Time.timeScale > 0f && !tensionSource.isPlaying) PlayTensionLoop();
    }

    void PlayTensionLoop()
    {
        if (tensionLoop == null) return;
        tensionSource.clip = tensionLoop;
        tensionSource.Play();
    }

    void StopTension()
    {
        aiming = false;
        tensionSource.Stop();
    }

    void ApplyTension(float t)
    {
        t = Mathf.Clamp01(t);
        tensionSource.pitch = Mathf.Lerp(tensionPitchMin, tensionPitchMax, t);
        tensionSource.volume = volume * Mathf.Lerp(0.3f, 1f, t);
    }
}
