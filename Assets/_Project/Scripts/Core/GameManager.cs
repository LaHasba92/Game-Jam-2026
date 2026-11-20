using UnityEngine;

// Les 4 états du jeu (GDD version jam, section 6) :
// Accueil → Visée → Vol → Résultat
public enum GameState
{
    Home,    // Accueil : titre, bouton TIRER, boutique
    Aiming,  // Visée : le joueur vise en 3D et lâche dans la jauge
    Flying,  // Vol : le boulet enchaîne les murs, le joueur le dirige
    Result   // Résultat : distance, murs cassés, pièces, REJOUER
}

public class GameManager : MonoBehaviour
{
    // ─── Singleton : un seul GameManager, accessible partout ───
    // Les autres scripts écrivent : GameManager.Instance.WallsBroken
    public static GameManager Instance { get; private set; }

    [Header("Test sans les autres scripts")]
    [Tooltip("Touche N + boutons à l'écran. À décocher quand Andy et Sami sont branchés.")]
    [SerializeField] private bool keyboardDebug = true;

    // ─── État du jeu (lisible par tous, modifiable seulement ici) ───
    public GameState CurrentState { get; private set; } = GameState.Home;
    public int WallsBroken { get; private set; } = 0;      // murs cassés dans la partie en cours
    public bool HasRevived { get; private set; } = false;  // pub « relancer » déjà utilisée ?
    public RunResult LastRun { get; private set; }         // résultat de la dernière partie, pour l'UI

    // L'UI (Hugo) affiche le bouton « relancer » seulement si c'est vrai
    public bool CanRevive => CurrentState == GameState.Result && !HasRevived;

    // ────────────────────────────────────────────────────────────
    // Cycle de vie Unity
    // ────────────────────────────────────────────────────────────

    void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject); // un deuxième GameManager ? On le supprime.
            return;
        }
        Instance = this;
    }

    void OnEnable()
    {
        GameEvents.OnShotFired += HandleShotFired;
        GameEvents.OnWallBroken += HandleWallBroken;
        GameEvents.OnRunEnded += HandleRunEnded;
    }

    void OnDisable()
    {
        GameEvents.OnShotFired -= HandleShotFired;
        GameEvents.OnWallBroken -= HandleWallBroken;
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
                // LauncherController (Andy) : visée 3D + jauge ; LevelManager (Sami) : mur 1 en place
                break;
            case GameState.Flying:
                // ProjectileController (Andy) gère le vol ; les murs s'enchaînent (Sami)
                break;
            case GameState.Result:
                // L'UI affiche distance, murs cassés, pièces, ×2 pub, relancer, REJOUER
                break;
        }

        // On prévient tout le monde (UI, caméras…)
        GameEvents.RaiseStateChanged(oldState, newState);
    }

    // ────────────────────────────────────────────────────────────
    // Méthodes appelées par les boutons de l'UI
    // ────────────────────────────────────────────────────────────

    // Bouton « TIRER » (accueil) et « REJOUER » (résultat) : nouvelle partie depuis le mur 1
    public void Play()
    {
        if (CurrentState != GameState.Home && CurrentState != GameState.Result)
        {
            Debug.LogWarning($"[GameManager] Play() ignoré : on est en {CurrentState}");
            return;
        }

        WallsBroken = 0;
        HasRevived = false;
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

    // Pub « relancer » (GDD section 5) : appelée par AdService (Hugo) après la fausse pub.
    // Le boulet repart du dernier mur cassé, une fois par partie.
    public void Revive()
    {
        if (!CanRevive)
        {
            Debug.LogWarning("[GameManager] Revive() ignoré : pas en Résultat, ou déjà utilisé");
            return;
        }

        HasRevived = true;
        Debug.Log($"[GameManager] Relance depuis le mur {WallsBroken}");
        ChangeState(GameState.Flying);
        GameEvents.RaiseReviveRequested(); // Andy relance le boulet
    }

    // ────────────────────────────────────────────────────────────
    // Réactions aux événements des autres scripts
    // ────────────────────────────────────────────────────────────

    // Le joueur lâche (envoyé par LauncherController)
    void HandleShotFired(ShotInfo shot)
    {
        if (CurrentState != GameState.Aiming)
        {
            Debug.LogWarning($"[GameManager] Tir reçu alors qu'on est en {CurrentState}");
            return;
        }
        ChangeState(GameState.Flying);
    }

    // Un mur explose (envoyé par WallController) : la partie continue
    void HandleWallBroken(int wallNumber)
    {
        if (CurrentState != GameState.Flying)
        {
            Debug.LogWarning($"[GameManager] Mur cassé alors qu'on est en {CurrentState}");
            return;
        }
        WallsBroken = wallNumber;
        Debug.Log($"[GameManager] POW ! Mur {wallNumber} cassé");
    }

    // La partie est finie : boulet arrivé en roulant, trop lent, ou « Passer »
    // (envoyé par ProjectileController)
    void HandleRunEnded(RunResult result)
    {
        if (CurrentState != GameState.Flying)
        {
            Debug.LogWarning($"[GameManager] Fin de partie alors qu'on est en {CurrentState}");
            return;
        }

        LastRun = result;
        Debug.Log($"[GameManager] Fin : {result.distance:0} m, {result.wallsBroken} mur(s) cassé(s)");
        ChangeState(GameState.Result);
    }

    // ────────────────────────────────────────────────────────────
    // Test : touche N et boutons à l'écran simulent les autres scripts
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
                FakeShot();
                break;
            case GameState.Flying:
                // N casse un mur ; une chance sur trois que la partie s'arrête à la place
                if (Random.value < 0.33f) FakeRunEnd();
                else FakeWallBroken();
                break;
        }
    }

    void FakeShot()
    {
        ShotInfo shot = new ShotInfo();
        shot.direction = 0f;
        shot.angle = 30f;
        shot.gaugeMultiplier = 1f;
        shot.launcherIndex = 1;
        GameEvents.RaiseShotFired(shot);
    }

    void FakeWallBroken()
    {
        GameEvents.RaiseWallBroken(WallsBroken + 1);
    }

    void FakeRunEnd()
    {
        RunResult run = new RunResult();
        run.wallsBroken = WallsBroken;
        // Mur n à 50 × 1,4^(n−1) m : on s'arrête un peu avant le mur suivant
        run.distance = 50f * Mathf.Pow(1.4f, WallsBroken) - Random.Range(1f, 10f);
        GameEvents.RaiseRunEnded(run);
    }

    // Boutons de test (sans paquet UI). Visibles seulement si Keyboard Debug est coché.
    void OnGUI()
    {
        if (!keyboardDebug) return;

        GUI.skin.button.fontSize = 32;
        GUI.skin.label.fontSize = 32;

        GUILayout.BeginArea(new Rect(20, 20, 500, 600));
        GUILayout.Label($"État : {CurrentState}");
        GUILayout.Label($"Murs cassés : {WallsBroken}");

        switch (CurrentState)
        {
            case GameState.Home:
                if (GUILayout.Button("TIRER", GUILayout.Height(80))) Play();
                break;

            case GameState.Aiming:
                if (GUILayout.Button("Lâcher (faux tir)", GUILayout.Height(80))) FakeShot();
                break;

            case GameState.Flying:
                if (GUILayout.Button("POW : casser un mur", GUILayout.Height(80))) FakeWallBroken();
                if (GUILayout.Button("Fin de partie", GUILayout.Height(80))) FakeRunEnd();
                break;

            case GameState.Result:
                if (GUILayout.Button("REJOUER", GUILayout.Height(80))) Play();
                if (CanRevive && GUILayout.Button("Relancer (pub)", GUILayout.Height(80))) Revive();
                if (GUILayout.Button("ACCUEIL", GUILayout.Height(80))) GoHome();
                break;
        }

        GUILayout.EndArea();
    }
}
