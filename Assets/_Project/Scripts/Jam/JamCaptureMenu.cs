using UnityEngine;
using UnityEngine.InputSystem;

// Panneau opérateur caché. Les commandes réutilisent les entrées du jeu, la sauvegarde est isolée.
public class JamCaptureMenu : MonoBehaviour
{
    public static bool IsPanelOpen { get; private set; }
    private bool threeTouches;
    private float healthRatio;
    private Vector2 scroll;
    private RunHud hud;
    private GameManager game;
    private LauncherController launcher;
    private ProjectileController projectile;
    private RunUpgrades upgrades;
    private JamCameraFx cameraFx;
    private AimTuning cfg;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    private static void ResetMenu() => IsPanelOpen = false;

    // Bouton du menu de test de Nathan. Une fois la scène Game chargée, son HUD reprend le panneau.
    public static void OpenFromTestScene()
    {
        if (UnityEngine.SceneManagement.SceneManager.GetActiveScene().name != "Game")
        {
            if (!Application.CanStreamedLevelBeLoaded("Game")) return;
            UnityEngine.SceneManagement.SceneManager.LoadScene("Game");
        }
        IsPanelOpen = true;
    }

    public void Bind(RunHud ui, GameManager manager, LauncherController aim, ProjectileController ball, RunUpgrades store)
    {
        hud = ui; game = manager; launcher = aim; projectile = ball; upgrades = store;
        cfg = AimTuning.Current;
        healthRatio = cfg.previewHealth;
    }
    private void OnDisable()
    {
        IsPanelOpen = false;
        if (JamSave.Preview) JamSave.SetPreview(false);
    }

    private void Update()
    {
        int fingers = 0;
        if (Touchscreen.current != null)
            foreach (var finger in Touchscreen.current.touches)
                if (finger.press.isPressed) fingers++;
        bool chord = fingers >= 3;
        if ((Keyboard.current?.f8Key.wasPressedThisFrame ?? false) || (chord && !threeTouches))
            IsPanelOpen = !IsPanelOpen;
        threeTouches = chord;
    }

    private void ToHome()
    {
        launcher.CancelStaging();
        projectile.ResetToLauncher();
        game.ChangeState(GameState.Home);
        GameEvents.RaiseCoinsChanged(RunProgress.Coins);
        if (cameraFx == null) cameraFx = FindFirstObjectByType<JamCameraFx>();
        if (cameraFx != null) cameraFx.GetComponent<CameraDirector>().FrameTake(projectile.transform.position, true);
        var world = FindFirstObjectByType<WorldStreamer>();
        if (world != null) world.RewindTo(projectile.Distance);
    }

    private void Take(int index)
    {
        ToHome();
        upgrades.SelectForCapture(index);
        game.Play();
        IsPanelOpen = false;
    }

    private void OnGUI()
    {
        if (!IsPanelOpen || hud == null || game == null || launcher == null || projectile == null || upgrades == null) return;
        var matrix = GUI.matrix;
        float scale = Mathf.Min(Screen.width / 560f, Screen.height / 810f);
        GUI.matrix = Matrix4x4.Scale(new Vector3(scale, scale, 1f));
        GUILayout.BeginArea(new Rect(12f, 12f, 536f, 780f), GUI.skin.box);
        scroll = GUILayout.BeginScrollView(scroll);
        GUILayout.Label("TRICHE ET CAPTURE   ·   F8 / trois doigts");
        if (!JamSave.Preview)
        {
            GUILayout.Label("Les prises utilisent une copie temporaire de ta progression.");
            if (Tap("Entrer en capture"))
            {
                JamSave.SetPreview(true);
                ToHome();
                hud.RefreshCaptureMask();
            }
        }
        else
        {
            if (Tap("Viser librement")) Take(upgrades.SelectedIndex);
            if (Tap("Casse de la fronde")) { Take(cfg.slingIndex); launcher.StageFailure(); }
            if (Tap("Explosion du canon")) { Take(cfg.cannonIndex); launcher.StageFailure(); }
            if (Tap("Mur intact")) { Take(cfg.slingIndex); projectile.StageWallImpact(false); }
            if (Tap("Préparer le premier achat du canon"))
            {
                ToHome();
                LauncherDefinition canon = upgrades.LauncherAt(cfg.cannonIndex);
                if (canon != null)
                {
                    upgrades.PrepareCapturePurchase(cfg.cannonIndex);
                    RunProgress.ReachWall(canon.requiredWall);
                    RunProgress.Credit(Mathf.Max(0, canon.price - RunProgress.Coins));
                    hud.OpenShop();
                    IsPanelOpen = false;
                }
            }
            if (Tap("Boutique")) { ToHome(); hud.OpenShop(); IsPanelOpen = false; }
            if (Tap("Transition fronde → canon"))
            {
                ToHome();
                upgrades.SelectForCapture(cfg.cannonIndex);
                GameEvents.RaiseCannonReveal();
                IsPanelOpen = false;
            }
            GUILayout.Label($"PV du Mur géant   {healthRatio:P0}");
            healthRatio = Mathf.RoundToInt(GUILayout.HorizontalSlider(healthRatio, 0f, 1f, GUILayout.Height(28)) * 100f) / 100f;
            if (Tap("Appliquer les PV")) { ToHome(); BossProgress.SetPreviewHealth(cfg.giantWall, healthRatio); }
            if (healthRatio <= 0f) GUILayout.Label("0 % = mur détruit. Monte les PV pour filmer un impact.");
            if (Tap("Impact sur le Mur géant") && healthRatio > 0f)
            {
                BossProgress.SetPreviewHealth(cfg.giantWall, healthRatio);
                Take(cfg.cannonIndex);
                projectile.StageWallImpact(true);
            }
            if (Tap("Travelling vers le Mur géant"))
            {
                ToHome();
                BossProgress.SetPreviewHealth(cfg.giantWall, healthRatio);
                cameraFx?.OpeningShot();
                IsPanelOpen = false;
            }
            if (Tap("Caméra libre")) { ToHome(); cameraFx?.FreeCamera(); IsPanelOpen = false; }
            if (Tap("Quitter la capture"))
            {
                JamSave.SetPreview(false);
                ToHome();
                IsPanelOpen = false;
            }
        }
        if (Tap("Fermer le panneau")) IsPanelOpen = false;
        GUILayout.EndScrollView();
        GUILayout.EndArea();
        GUI.matrix = matrix;
    }
    private static bool Tap(string text) => GUILayout.Button(text, GUILayout.Height(45f));
}
