using UnityEngine;

// Les 5 états du jeu (GDD, section 10) :
// Accueil → Visée → Absorption → Vol → Résultat
public enum GameState
{
    Home,       // Accueil : titre, bouton TIRER, boutique
    Aiming,     // Visée : le joueur vise, lâche, le projectile file vers le mur
    Absorbing,  // Absorption : le mur se résout, le projectile grossit
    Flying,     // Vol : rebonds, Boost, bouton « Passer »
    Result      // Résultat : distance, pièces, REJOUER
}

public class GameManager : MonoBehaviour
{
    // ─── Singleton : un seul GameManager, accessible partout ───
    // Les autres scripts écrivent : GameManager.Instance.CurrentState
    public static GameManager Instance { get; private set; }

    // ─── Réglages visibles dans l'Inspector ───
    [Header("Zones (GDD section 6)")]
    [SerializeField] private float zone2Distance = 500f;
    [SerializeField] private float zone3Distance = 2000f;

    [Header("Test sans les autres scripts")]
    [Tooltip("Touche N = simule l'étape suivante. À décocher quand Andy et Sami sont branchés.")]
    [SerializeField] private bool keyboardDebug = true;

    // ─── État du jeu (lisible par tous, modifiable seulement ici) ───
    public GameState CurrentState { get; private set; } = GameState.Home;
    public int CurrentZone { get; private set; } = 1;
    public float BestDistance { get; private set; } = 0f; // plus tard : lu depuis SaveSystem (Nathan)

    // ────────────────────────────────────────────────────────────
    // Cycle de vie Unity
    // ────────────────────────────────────────────────────────────

    // Awake : appelé en tout premier, avant Start
    void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject); // un deuxième GameManager ? On le supprime.
            return;
        }
        Instance = this;
    }

    // OnEnable / OnDisable : on s'abonne aux événements, puis on se désabonne
    void OnEnable()
    {
        GameEvents.OnWallHit += HandleWallHit;
        GameEvents.OnWallImpactEnded += HandleWallImpactEnded;
        GameEvents.OnRunEnded += HandleRunEnded;
    }

    void OnDisable()
    {
        GameEvents.OnWallHit -= HandleWallHit;
        GameEvents.OnWallImpactEnded -= HandleWallImpactEnded;
        GameEvents.OnRunEnded -= HandleRunEnded;
    }

    void Start()
    {
        ChangeState(GameState.Home);
    }

    void Update()
    {
        if (keyboardDebug)
        {
            DebugKeys();
        }
    }

    // Boutons de test (sans paquet UI). Visibles seulement si Keyboard Debug est coché.
    void OnGUI()
    {
        if (!keyboardDebug) return;

        GUI.skin.button.fontSize = 32;
        GUI.skin.label.fontSize = 32;

        GUILayout.BeginArea(new Rect(20, 20, 450, 400));
        GUILayout.Label("État : " + CurrentState);

        if (GUILayout.Button("TIRER / REJOUER", GUILayout.Height(80)))
        {
            Play();
        }
        if (GUILayout.Button("ACCUEIL", GUILayout.Height(80)))
        {
            GoHome();
        }

        GUILayout.EndArea();
    }

    // ────────────────────────────────────────────────────────────
    // Le cœur : changer d'état
    // ────────────────────────────────────────────────────────────

    public void ChangeState(GameState newState)
    {
        GameState oldState = CurrentState;
        CurrentState = newState;
        Debug.Log($"[GameManager] {oldState} → {newState}");

        switch (newState)
        {
            case GameState.Home:
                // L'UI (Hugo) affiche l'accueil et la boutique
                break;
            case GameState.Aiming:
                // LauncherController (Andy) arme la fronde
                break;
            case GameState.Absorbing:
                // WallImpact (Sami) joue la résolution du mur
                break;
            case GameState.Flying:
                // ProjectileController (Andy) gère le vol
                break;
            case GameState.Result:
                // L'UI affiche l'écran de résultat
                break;
        }

        // On prévient tout le monde (UI, caméras…)
        GameEvents.RaiseStateChanged(oldState, newState);
    }

    // ────────────────────────────────────────────────────────────
    // Méthodes appelées par les boutons de l'UI
    // ────────────────────────────────────────────────────────────

    // Bouton « TIRER » (accueil) et « REJOUER » (résultat)
    public void Play()
    {
        if (CurrentState != GameState.Home && CurrentState != GameState.Result)
        {
            Debug.LogWarning($"[GameManager] Play() ignoré : on est en {CurrentState}");
            return;
        }
        ChangeState(GameState.Aiming);
    }

    // Retour à l'accueil depuis l'écran de résultat
    public void GoHome()
    {
        if (CurrentState != GameState.Result)
        {
            Debug.LogWarning($"[GameManager] GoHome() ignoré : on est en {CurrentState}");
            return;
        }
        ChangeState(GameState.Home);
    }

    // ────────────────────────────────────────────────────────────
    // Réactions aux événements des autres scripts
    // ────────────────────────────────────────────────────────────

    // Le projectile touche le mur (envoyé par ProjectileController)
    void HandleWallHit()
    {
        if (CurrentState != GameState.Aiming)
        {
            Debug.LogWarning($"[GameManager] Mur touché alors qu'on est en {CurrentState}");
            return;
        }
        ChangeState(GameState.Absorbing);
    }

    // Le mur a fini de se résoudre (envoyé par WallImpact)
    void HandleWallImpactEnded(ImpactResult result)
    {
        if (CurrentState != GameState.Absorbing)
        {
            Debug.LogWarning($"[GameManager] Fin d'impact alors qu'on est en {CurrentState}");
            return;
        }
        Debug.Log($"[GameManager] {result.blocksDestroyed} blocs, vitesse ×{result.speedMultiplier:0.00}");
        ChangeState(GameState.Flying);
    }

    // Le vol est fini, ou « Passer » (envoyé par ProjectileController)
    void HandleRunEnded(RunResult result)
    {
        if (CurrentState != GameState.Flying)
        {
            Debug.LogWarning($"[GameManager] Fin de vol alors qu'on est en {CurrentState}");
            return;
        }

        if (result.distance > BestDistance)
        {
            BestDistance = result.distance;
            Debug.Log($"[GameManager] Nouveau record : {BestDistance:0} m");
        }

        CheckZone(BestDistance);
        ChangeState(GameState.Result);
    }

    // Les zones s'ouvrent avec le record : 500 m → zone 2, 2 000 m → zone 3
    void CheckZone(float distance)
    {
        int newZone = 1;
        if (distance >= zone3Distance) newZone = 3;
        else if (distance >= zone2Distance) newZone = 2;

        if (newZone > CurrentZone)
        {
            CurrentZone = newZone;
            Debug.Log($"[GameManager] Zone {CurrentZone} atteinte !");
            GameEvents.RaiseZoneReached(CurrentZone);
        }
    }

    // ────────────────────────────────────────────────────────────
    // Test au clavier : N simule ce que les autres scripts enverront
    // ────────────────────────────────────────────────────────────

    void DebugKeys()
    {
        if (!Input.GetKeyDown(KeyCode.N)) return;

        switch (CurrentState)
        {
            case GameState.Home:
            case GameState.Result:
                Play();
                break;

            case GameState.Aiming:
                GameEvents.RaiseWallHit(); // faux « le projectile touche le mur »
                break;

            case GameState.Absorbing:
                ImpactResult fakeImpact = new ImpactResult();
                fakeImpact.kind = ImpactKind.RightColor;
                fakeImpact.blocksDestroyed = Random.Range(3, 20);
                fakeImpact.speedMultiplier = 1f + 0.04f * fakeImpact.blocksDestroyed;
                GameEvents.RaiseWallImpactEnded(fakeImpact);
                break;

            case GameState.Flying:
                RunResult fakeRun = new RunResult();
                fakeRun.distance = Random.Range(100f, 2500f);
                GameEvents.RaiseRunEnded(fakeRun);
                break;
        }
    }
}
