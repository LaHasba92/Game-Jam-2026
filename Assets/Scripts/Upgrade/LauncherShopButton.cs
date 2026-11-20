using System.IO;
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
    private string SavePath => Path.Combine(Application.persistentDataPath, saveFileName);
    private string OwnedKey => $"CanonPOW.Launcher.{launcherIndex}";

    public bool Owned => launcherIndex == 0 || PlayerPrefs.GetInt(OwnedKey) == 1;
    public bool Equipped => PlayerPrefs.GetInt(LauncherVisuals.SelectedKey, 0) == launcherIndex;

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

    private void Click()
    {
        if (!Owned)
        {
            UpgradeSaveData data = Load();
            if (data.coins < definition.price || data.highestReachedWall < definition.requiredWall)
            {
                return;
            }
            data.coins -= definition.price;
            File.WriteAllText(SavePath, JsonUtility.ToJson(data, true));
            PlayerPrefs.SetInt(OwnedKey, 1);
        }
        PlayerPrefs.SetInt(LauncherVisuals.SelectedKey, launcherIndex);   // acheter = equiper
        PlayerPrefs.Save();
        GameEvents.RaiseCoinsChanged(Load().coins);                      // les autres boutons se mettent a jour
    }

    private void Refresh()
    {
        if (definition == null)
        {
            return;
        }
        UpgradeSaveData data = Load();
        bool canBuy = data.coins >= definition.price && data.highestReachedWall >= definition.requiredWall;
        string text;
        if (Equipped)
        {
            text = $"{definition.displayName} - equipe";
        }
        else if (Owned)
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
            button.interactable = !Equipped && (Owned || canBuy);
        }
    }

    private UpgradeSaveData Load()
    {
        return File.Exists(SavePath)
            ? JsonUtility.FromJson<UpgradeSaveData>(File.ReadAllText(SavePath))
            : new UpgradeSaveData();
    }
}
