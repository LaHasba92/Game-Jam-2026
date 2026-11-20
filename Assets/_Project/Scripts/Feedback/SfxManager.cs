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

    const int PoolSize = 8;
    AudioSource[] pool;
    int next;
    AudioSource tensionSource;
    bool aiming, tensionDriven;
    float aimTime;
    int lastCoins = -1;

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
        // Si la jauge n'envoie rien, la tension monte toute seule avec le temps
        if (aiming && !tensionDriven)
        {
            aimTime += Time.deltaTime;
            ApplyTension(Mathf.Clamp01(aimTime / Mathf.Max(0.01f, tensionRiseTime)));
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
        Instance.tensionDriven = true;
        Instance.ApplyTension(t);
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
        AudioSource src = pool[next];
        next = (next + 1) % PoolSize;
        src.pitch = 1f + Random.Range(-pitchVariation, pitchVariation);
        src.PlayOneShot(clip, volume * volumeScale);
    }

    void StartTension()
    {
        aiming = true;
        tensionDriven = false;
        aimTime = 0f;
        if (tensionLoop == null) return;
        tensionSource.clip = tensionLoop;
        ApplyTension(0f);
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
