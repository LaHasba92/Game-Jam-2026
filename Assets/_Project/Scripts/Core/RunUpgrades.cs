using UnityEngine;

// Lit les fiches de Nathan. Les niveaux et lanceurs débloqués suivent le portefeuille de Game.
public class RunUpgrades : MonoBehaviour
{
    [SerializeField] private UpgradeDefinition[] upgrades;
    [SerializeField] private LauncherDefinition[] launchers;
    private const string SelectedKey = "CanonPOW.SelectedLauncher";

    private const string FreeUpgradeKey = "CanonPOW.LastFreeUpgrade";
    // Toutes les 3 min (GDD), seulement s'il reste une stat qui peut encore monter.
    public bool CanGetFreeUpgrade => FreeUpgradeTimerReady() && LeastUpgraded() >= 0;

    public int UpgradeCount => upgrades?.Length ?? 0;
    public int LauncherCount => launchers?.Length ?? 0;
    public int SelectedIndex => Mathf.Clamp(PlayerPrefs.GetInt(SelectedKey), 0, Mathf.Max(0, LauncherCount - 1));
    public float LauncherMultiplier => LauncherAt(SelectedIndex)?.powerMultiplier ?? 1f;
    public UpgradeDefinition UpgradeAt(int index) => index >= 0 && index < UpgradeCount ? upgrades[index] : null;
    public LauncherDefinition LauncherAt(int index) => index >= 0 && index < LauncherCount ? launchers[index] : null;
    public int Level(int index) => Mathf.Max(1, PlayerPrefs.GetInt($"CanonPOW.Upgrade.{index}", 1));
    public bool OwnsLauncher(int index) => index == 0 || PlayerPrefs.GetInt($"CanonPOW.Launcher.{index}") == 1;

    // Vrai quand le niveau suivant ne change plus la valeur : plafond du GDD atteint (Élan 85 %, Rebond 0,85, Aéro 0,02).
    public bool IsMaxed(int index)
    {
        UpgradeDefinition definition = UpgradeAt(index);
        if (definition == null) return true;
        int level = Level(index);
        return Mathf.Approximately(definition.GetValue(level + 1), definition.GetValue(level));
    }

    public float Value(int index, float fallback)
    {
        UpgradeDefinition definition = UpgradeAt(index);
        return definition != null ? definition.GetValue(Level(index)) : fallback;
    }

    public void BuyUpgrade(int index)
    {
        UpgradeDefinition definition = UpgradeAt(index);
        int level = Level(index);
        if (definition == null || IsMaxed(index) || !RunProgress.Spend(definition.GetCost(level))) return;
        RaiseLevel(index);
    }

    public void GetFreeUpgrade()
    {
        if (!FreeUpgradeTimerReady()) return;
        int lowest = LeastUpgraded();
        if (lowest < 0) return;

        PlayerPrefs.SetString(FreeUpgradeKey, System.DateTime.UtcNow.Ticks.ToString());
        RaiseLevel(lowest);
    }

    // La stat la moins améliorée parmi celles qui ne sont pas au plafond, -1 si aucune.
    private int LeastUpgraded()
    {
        int lowest = -1;
        for (int i = 0; i < UpgradeCount; i++)
        {
            if (!IsMaxed(i) && (lowest < 0 || Level(i) < Level(lowest))) lowest = i;
        }
        return lowest;
    }

    private static bool FreeUpgradeTimerReady()
    {
        if (!long.TryParse(PlayerPrefs.GetString(FreeUpgradeKey), out long ticks)) return true;
        long elapsed = System.DateTime.UtcNow.Ticks - ticks;
        // Horloge du téléphone remise en arrière : on ne bloque pas le bonus pendant des jours.
        return elapsed < 0 || elapsed >= System.TimeSpan.TicksPerMinute * 3;
    }

    private void RaiseLevel(int index)
    {
        PlayerPrefs.SetInt($"CanonPOW.Upgrade.{index}", Level(index) + 1);
        PlayerPrefs.Save();
    }

    public void SelectLauncher(int index)
    {
        LauncherDefinition definition = LauncherAt(index);
        if (definition == null) return;
        if (!OwnsLauncher(index))
        {
            if (RunProgress.HighestWall < definition.requiredWall || !RunProgress.Spend(definition.price)) return;
            PlayerPrefs.SetInt($"CanonPOW.Launcher.{index}", 1);
        }

        PlayerPrefs.SetInt(SelectedKey, index);
        PlayerPrefs.Save();
    }
}
