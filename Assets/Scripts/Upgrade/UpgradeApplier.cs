using System.IO;
using UnityEngine;

// Le pont entre les ameliorations de Nathan et le gameplay : au debut de chaque partie
// (etat Visee), il lit les niveaux achetes dans la sauvegarde de Nathan et applique les
// valeurs de ses UpgradeDefinition au lanceur et au boulet.
// LECTURE SEULE : il n'ecrit jamais la sauvegarde (c'est le travail de Nathan).
public class UpgradeApplier : MonoBehaviour
{
    [Header("Gameplay")]
    [SerializeField] private LauncherController launcher;
    [SerializeField] private ProjectileController projectile;

    [Header("Fiches de Nathan (Assets/*.asset)")]
    [SerializeField] private UpgradeDefinition power;
    [SerializeField] private UpgradeDefinition aerodynamics;
    [SerializeField] private UpgradeDefinition momentum;      // Elan
    [SerializeField] private UpgradeDefinition bounce;
    [SerializeField] private UpgradeDefinition angle;         // facultatif : pas encore cree par Nathan
    [SerializeField] private LauncherDefinition[] launchers;   // 0 = Fronde ... 4 = Canon (Assets/_Project/Data/Launchers)

    [Header("Sauvegarde")]
    [SerializeField] private string saveFileName = "upgrade-test.json";   // le fichier de Nathan
    [SerializeField] private float defaultMaxAngle = 30f;                // si pas de fiche Angle

    private string SavePath => Path.Combine(Application.persistentDataPath, saveFileName);

    private void OnEnable()  { GameEvents.OnStateChanged += OnStateChanged; }
    private void OnDisable() { GameEvents.OnStateChanged -= OnStateChanged; }

    private void Start()
    {
        Apply();   // pour que la visee de la premiere partie soit deja a jour
    }

    private void OnStateChanged(GameState from, GameState to)
    {
        if (to == GameState.Aiming)
        {
            Apply();
        }
    }

    [ContextMenu("Appliquer maintenant")]
    public void Apply()
    {
        UpgradeSaveData data = Load();

        float powerValue = Value(power, data.powerLevel, 25f);
        float dragValue = Value(aerodynamics, data.aeroLevel, 0.10f);
        float elanValue = Value(momentum, data.momentumLevel, 0.6f);
        float bounceValue = Value(bounce, data.bounceLevel, 0.45f);
        float angleValue = angle != null ? angle.GetValue(1) : defaultMaxAngle;   // pas encore de niveau sauvegarde
        int selected = PlayerPrefs.GetInt(LauncherVisuals.SelectedKey, 0);   // le lanceur equipe en boutique
        float launcherValue = launchers != null && selected < launchers.Length && launchers[selected] != null
            ? launchers[selected].powerMultiplier
            : 1f;

        launcher.SetUpgrades(powerValue, launcherValue, angleValue);
        projectile.SetUpgrades(dragValue, bounceValue, elanValue);

        Debug.Log($"[Ameliorations] Puissance {powerValue:0.#} m/s, Aero {dragValue:0.###}, "
                + $"Elan {elanValue:0.##}, Rebond {bounceValue:0.###}, Angle {angleValue:0} deg, Lanceur x{launcherValue:0.#}");
    }

    private UpgradeSaveData Load()
    {
        if (!File.Exists(SavePath))
        {
            return new UpgradeSaveData();   // aucune sauvegarde : tout au niveau 1
        }
        return JsonUtility.FromJson<UpgradeSaveData>(File.ReadAllText(SavePath));
    }

    // Un niveau absent de la sauvegarde vaut 0 : on le compte comme le niveau 1.
    private static float Value(UpgradeDefinition definition, int level, float fallback)
    {
        return definition != null ? definition.GetValue(Mathf.Max(1, level)) : fallback;
    }
}
