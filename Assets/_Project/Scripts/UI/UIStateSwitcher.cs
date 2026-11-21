using UnityEngine;
using TMPro;

// Affiche le bon écran de Hugo selon l'état du jeu (GDD section 6) :
// Accueil → menu (+ boutique à la demande), Visée/Vol → HUD, Résultat → écran de fin.
// À mettre sur un objet vide « UI » dans la scène Game, puis glisser les panneaux dans les cases.
// Les cases de texte sont facultatives : laisse-les vides si l'écran n'en a pas.
public class UIStateSwitcher : MonoBehaviour
{
    private static UIStateSwitcher active;

    [Header("Panneaux (les Canvas de Hugo)")]
    [SerializeField] private GameObject menuPanel;     // accueil : titre, START, Shop
    [SerializeField] private GameObject shopPanel;     // boutique, ouverte par le bouton Shop
    [SerializeField] private GameObject hudPanel;      // pendant la visée et le vol (facultatif)
    [SerializeField] private GameObject resultPanel;   // écran de fin

    [Header("Gameplay (pour la distance et le bouton Passer)")]
    [SerializeField] private ProjectileController projectile;   // l'objet Ball du prefab Gameplay

    [Header("Textes du HUD (facultatif)")]
    [SerializeField] private TMP_Text hudDistanceText;
    [SerializeField] private TMP_Text hudWallsText;
    [SerializeField] private GameObject skipButton;             // bouton « Passer »

    [Header("Textes de l'écran de fin (facultatif)")]
    [SerializeField] private TMP_Text resultDistanceText;
    [SerializeField] private TMP_Text resultWallsText;

    private GameplayHud gameplayHud;
    private GamePanelPresentation panels;
    private GameMenuOverlay gameMenu;
    private bool shopOpen;

    internal static bool IsModalOpen => active != null &&
        (active.shopOpen || (active.gameMenu != null && active.gameMenu.IsOpen));

    void OnEnable()
    {
        active = this;
        if (gameMenu != null) gameMenu.gameObject.SetActive(true);
        GameEvents.OnStateChanged += HandleStateChanged;
    }

    void OnDisable()
    {
        GameEvents.OnStateChanged -= HandleStateChanged;
        if (active == this) active = null;
        if (gameMenu != null)
        {
            gameMenu.Close();
            gameMenu.gameObject.SetActive(false);
        }
    }

    void Start()
    {
        if (hudPanel == null && projectile != null)
            gameplayHud = GameplayHud.Create(gameObject.scene, projectile);
        panels = new GamePanelPresentation(menuPanel, resultPanel, this);
        gameMenu = GameMenuOverlay.Create(this, resultPanel != null ? resultPanel.GetComponent<RunRewardManager>() : null);

        // Au lancement, on affiche l'écran qui correspond à l'état actuel (l'Accueil)
        GameState state = GameManager.Instance != null ? GameManager.Instance.CurrentState : GameState.Home;
        Show(state);
    }

    void HandleStateChanged(GameState from, GameState to)
    {
        gameMenu?.Close();
        shopOpen = false;
        Show(to);
    }

    void Show(GameState state)
    {
        SetActive(menuPanel, !shopOpen && state == GameState.Home);
        SetActive(shopPanel, shopOpen);
        SetActive(hudPanel, !shopOpen && (state == GameState.Aiming || state == GameState.Flying));
        SetActive(resultPanel, !shopOpen && state == GameState.Result);
        if (gameplayHud != null) gameplayHud.Show(shopOpen ? GameState.Home : state);

        if (state == GameState.Result && GameManager.Instance != null)
        {
            RunResult run = GameManager.Instance.LastRun;
            SetText(resultDistanceText, $"{run.distance:N0} m");
            SetText(resultWallsText, $"{run.wallsBroken} mur(s) cassé(s)");
            panels?.ShowResult(run);
        }
    }

    void Update()
    {
        // Le HUD se met à jour en direct pendant le vol
        if (gameplayHud != null || projectile == null || GameManager.Instance == null) return;
        GameState state = GameManager.Instance.CurrentState;
        if (state != GameState.Aiming && state != GameState.Flying) return;

        SetText(hudDistanceText, $"{projectile.Distance:N0} m");
        SetText(hudWallsText, $"Murs : {GameManager.Instance.WallsBroken}");
        SetActive(skipButton, projectile.CanSkip);
    }

    // ─── Pour les boutons (On Click) ───

    public void OpenShop()
    {
        if (shopPanel == null) return;
        gameMenu?.Close();
        shopOpen = true;
        Show(GameManager.Instance != null ? GameManager.Instance.CurrentState : GameState.Home);
    }

    public void CloseShop()
    {
        shopOpen = false;
        Show(GameManager.Instance != null ? GameManager.Instance.CurrentState : GameState.Home);
    }

    public void Skip()
    {
        if (projectile != null && projectile.CanSkip)
        {
            projectile.Skip();
        }
    }

    // ─── Petits outils : ne plantent pas si une case est vide ───

    static void SetActive(GameObject panel, bool active)
    {
        if (panel != null && panel.activeSelf != active) panel.SetActive(active);
    }

    static void SetText(TMP_Text text, string value)
    {
        if (text != null) text.text = value;
    }

    private void LateUpdate()
    {
        panels?.Refresh();
    }

    private void OnDestroy()
    {
        if (gameplayHud != null) Destroy(gameplayHud.gameObject);
        if (gameMenu != null) Destroy(gameMenu.gameObject);
        panels?.Dispose();
    }
}
