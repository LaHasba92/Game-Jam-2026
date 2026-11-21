using TMPro;
using UnityEngine;
using UnityEngine.UI;

// Un bouton de la boutique pour un lanceur (fronde, lance-pierre, baliste, catapulte, canon).
// Pas encore achete : « Canon - 15000 pieces - mur 8 », grise si pas assez de pieces ou mur pas atteint.
// Achete : « Canon - equiper ». Equipe : « Canon - equipe ».
// Les pieces et le record de murs viennent de la sauvegarde de Nathan ; l'achat et le lanceur equipe
// sont gardes avec les memes cles que RunUpgrades (Dev), pour rester compatibles.
public class LauncherShopButton : MonoBehaviour
{
    [SerializeField] private int launcherIndex = 4;              // 0 = Fronde ... 4 = Canon
    [SerializeField] private LauncherDefinition definition;      // ex. Canon.asset
    [SerializeField] private TMP_Text label;
    [SerializeField] private string saveFileName = "upgrade-test.json";

    private Button button;
    public LauncherDefinition Definition => definition;
    public bool Owned => LauncherInventory.IsOwned(launcherIndex, Load());
    public bool Equipped => PlayerPrefs.GetInt(LauncherVisuals.SelectedKey, 0) == launcherIndex;
    public bool CanSelect(UpgradeSaveData data) =>
        definition != null && (uint)launcherIndex < 32 && !Equipped &&
        (LauncherInventory.IsOwned(launcherIndex, data) ||
            (definition.price >= 0 && data.coins >= definition.price && data.highestReachedWall >= definition.requiredWall));

    private void Awake()
    {
        button = GetComponent<Button>();
        if (button != null)
        {
            button.onClick.AddListener(Click);
        }
    }

    private void OnEnable()
    {
        GameEvents.OnCoinsChanged += HandleCoinsChanged;
        Refresh();
    }

    private void OnDisable()
    {
        GameEvents.OnCoinsChanged -= HandleCoinsChanged;
    }

    private void HandleCoinsChanged(int total)
    {
        Refresh();
    }

    private void Click() => TrySelect();

    public bool TrySelect()
    {
        UpgradeSaveData data = Load();
        if (!CanSelect(data)) return false;

        bool purchasing = !LauncherInventory.IsOwned(launcherIndex, data);
        if (purchasing)
        {
            data.coins -= definition.price;
            data.purchasedLauncherMask |= 1 << launcherIndex;
            UpgradeSaveStore.Save(data, saveFileName);
        }
        PlayerPrefs.SetInt(LauncherVisuals.SelectedKey, launcherIndex);   // acheter = equiper
        PlayerPrefs.Save();
        if (purchasing && launcherIndex == LauncherInventory.CannonIndex)
            ProjectileTrails.PlayFireUnlock();
        GameEvents.RaiseCoinsChanged(data.coins);
        return true;
    }

    private void Refresh()
    {
        if (definition == null)
        {
            return;
        }
        UpgradeSaveData data = Load();
        string text;
        if (Equipped)
        {
            text = $"{definition.displayName} - equipe";
        }
        else if (LauncherInventory.IsOwned(launcherIndex, data))
        {
            text = $"{definition.displayName} - equiper";
        }
        else
        {
            text = $"{definition.displayName} - {definition.price} pieces - mur {definition.requiredWall}";
        }
        if (label != null)
        {
            label.text = text;
        }
        if (button != null)
        {
            button.interactable = CanSelect(data);
        }
    }

    private UpgradeSaveData Load() => UpgradeSaveStore.Load(saveFileName);
}
