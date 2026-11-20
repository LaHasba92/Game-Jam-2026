using UnityEngine;

// Lit les fiches de Nathan. Les niveaux et lanceurs débloqués suivent le portefeuille de Game.
public class RunUpgrades : MonoBehaviour
{
    [SerializeField] private UpgradeDefinition[] upgrades;
    [SerializeField] private LauncherDefinition[] launchers;
    private const string SelectedKey = "CanonPOW.SelectedLauncher";

    private const string FreeUpgradeKey = "CanonPOW.LastFreeUpgrade";
    public bool CanGetFreeUpgrade => UpgradeCount > 0 &&
        (!long.TryParse(PlayerPrefs.GetString(FreeUpgradeKey), out long ticks) ||
         System.DateTime.UtcNow.Ticks - ticks >= System.TimeSpan.TicksPerMinute * 3);

    public int UpgradeCount => upgrades?.Length ?? 0;
    public int LauncherCount => launchers?.Length ?? 0;
    public int SelectedIndex => Mathf.Clamp(PlayerPrefs.GetInt(SelectedKey), 0, Mathf.Max(0, LauncherCount - 1));
    public float LauncherMultiplier => LauncherAt(SelectedIndex)?.powerMultiplier ?? 1f;
    public UpgradeDefinition UpgradeAt(int index) => index >= 0 && index < UpgradeCount ? upgrades[index] : null;
    public LauncherDefinition LauncherAt(int index) => index >= 0 && index < LauncherCount ? launchers[index] : null;
    public int Level(int index) => Mathf.Max(1, PlayerPrefs.GetInt($"CanonPOW.Upgrade.{index}", 1));
    public bool OwnsLauncher(int index) => index == 0 || PlayerPrefs.GetInt($"CanonPOW.Launcher.{index}") == 1;

    public float Value(int index, float fallback)
    {
        UpgradeDefinition definition = UpgradeAt(index);
        return definition != null ? definition.GetValue(Level(index)) : fallback;
    }

    public void BuyUpgrade(int index)
    {
        UpgradeDefinition definition = UpgradeAt(index);
        int level = Level(index);
        if (definition == null || !RunProgress.Spend(definition.GetCost(level))) return;
        RaiseLevel(index);
    }

    public void GetFreeUpgrade()
    {
        if (!CanGetFreeUpgrade) return;
        int lowest = -1;
        for (int i = 0; i < UpgradeCount; i++)
        {
            if (UpgradeAt(i) != null && (lowest < 0 || Level(i) < Level(lowest))) lowest = i;
        }
        if (lowest < 0) return;

        PlayerPrefs.SetString(FreeUpgradeKey, System.DateTime.UtcNow.Ticks.ToString());
        RaiseLevel(lowest);
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
