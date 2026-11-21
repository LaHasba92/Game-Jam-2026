using System.IO;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

// Un bouton de la boutique pour un theme (Ville, Foret...).
// Pas encore achete : « Foret - 3000 pieces ». Achete : « Foret - equiper ». Equipe : « Foret - equipe ».
// Les pieces viennent de la sauvegarde de Nathan ; l'achat et le theme equipe sont dans les PlayerPrefs.
public class ThemeShopButton : MonoBehaviour
{
    [SerializeField] private int themeIndex = 1;                 // 0 = Ville ... (ordre du ThemeManager)
    [SerializeField] private ThemeDefinition definition;         // ex. Theme_Foret
    [SerializeField] private TMP_Text label;
    [SerializeField] private string saveFileName = "upgrade-test.json";

    private Button button;
    private int lastSelected = -1;
    private string SavePath => Path.Combine(Application.persistentDataPath, saveFileName);
    private string OwnedKey => $"CanonPOW.Theme.{themeIndex}";

    public bool Owned => definition == null || definition.price == 0 || PlayerPrefs.GetInt(OwnedKey) == 1;
    public bool Equipped => PlayerPrefs.GetInt(ThemeManager.SelectedKey, 0) == themeIndex;

    private void Awake()
    {
        // Deux boutons sur le meme theme = l'un emmene sur le theme de l'autre : on previent tout de suite
        foreach (ThemeShopButton other in FindObjectsByType<ThemeShopButton>(FindObjectsInactive.Include, FindObjectsSortMode.None))
        {
            if (other != this && other.themeIndex == themeIndex)
            {
                Debug.LogWarning($"ThemeShopButton : « {name} » et « {other.name} » ont le meme Theme Index ({themeIndex}).", this);
            }
        }
        button = GetComponent<Button>();
        if (button != null)
        {
            button.onClick.AddListener(Click);   // pas besoin de le brancher a la main dans On Click
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

    // Le theme equipe peut changer ailleurs (autre bouton, triche, apres une partie) : on se remet a jour.
    private void Update()
    {
        int selected = PlayerPrefs.GetInt(ThemeManager.SelectedKey, 0);
        if (selected != lastSelected)
        {
            Refresh();
        }
    }

    private void Click()
    {
        bool changing = !Equipped;
        if (!Owned)
        {
            UpgradeSaveData data = Load();
            if (data.coins < definition.price)
            {
                return;
            }
            data.coins -= definition.price;
            File.WriteAllText(SavePath, JsonUtility.ToJson(data, true));
            PlayerPrefs.SetInt(OwnedKey, 1);
        }
        PlayerPrefs.SetInt(ThemeManager.SelectedKey, themeIndex);   // acheter = equiper
        PlayerPrefs.Save();
        GameEvents.RaiseCoinsChanged(Load().coins);                 // les autres boutons se mettent a jour
        if (changing)
        {
            BackToMenuAndStart();
        }
    }

    // Nouveau theme = on repart de zero : boulet sur le lanceur, mur 1, piste neuve, et retour au menu.
    private static void BackToMenuAndStart()
    {
        LevelManager levels = FindFirstObjectByType<LevelManager>();
        if (levels != null) levels.StartNewRun();             // mur 1 + piste remise a zero

        ProjectileController ball = FindFirstObjectByType<ProjectileController>();
        if (ball != null) ball.ResetToLauncher();

        if (GameManager.Instance != null && GameManager.Instance.CurrentState == GameState.Result)
        {
            GameManager.Instance.GoHome();                    // l'ecran de fin laisse place au menu
        }
        UIStateSwitcher ui = FindFirstObjectByType<UIStateSwitcher>();
        if (ui != null) ui.CloseShop();                       // ferme la boutique et affiche le menu
    }

    private void Refresh()
    {
        lastSelected = PlayerPrefs.GetInt(ThemeManager.SelectedKey, 0);
        if (definition == null)
        {
            return;
        }
        string text = Equipped ? $"{definition.displayName} - equipe"
                    : Owned ? $"{definition.displayName} - equiper"
                    : $"{definition.displayName} - {definition.price} pieces";
        if (label != null)
        {
            label.text = text;
        }
        if (button != null)
        {
            button.interactable = !Equipped && (Owned || Load().coins >= definition.price);
        }
    }

    private UpgradeSaveData Load()
    {
        return File.Exists(SavePath)
            ? JsonUtility.FromJson<UpgradeSaveData>(File.ReadAllText(SavePath))
            : new UpgradeSaveData();
    }
}
