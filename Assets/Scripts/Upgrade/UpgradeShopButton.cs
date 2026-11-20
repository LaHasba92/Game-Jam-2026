using System.IO;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

// Un bouton de la boutique qui achete une amelioration de Nathan (GDD section 5).
// A mettre sur chaque bouton de la boutique de Hugo. Il affiche le nom, le niveau, le gain
// (« 26,5 -> 28,1 ») et le prix, se grise s'il manque des pieces, et sauvegarde l'achat.
public class UpgradeShopButton : MonoBehaviour
{
    public enum Stat { Power, Aerodynamics, Momentum, Bounce, Money }

    [SerializeField] private Stat stat;
    [SerializeField] private UpgradeDefinition definition;   // la fiche de Nathan qui correspond
    [SerializeField] private TMP_Text titleText;             // ex. « Puissance niv. 4 »
    [SerializeField] private TMP_Text priceText;             // ex. « 82 pieces »
    [SerializeField] private string saveFileName = "upgrade-test.json";

    private Button button;
    private string SavePath => Path.Combine(Application.persistentDataPath, saveFileName);

    private void Awake()
    {
        button = GetComponent<Button>();
        if (button != null)
        {
            button.onClick.AddListener(Buy);   // pas besoin de le brancher a la main dans On Click
        }
    }

    private void OnEnable()
    {
        GameEvents.OnCoinsChanged += HandleCoinsChanged;
        Refresh();   // la boutique s'ouvre : on affiche les valeurs a jour
    }

    private void OnDisable()
    {
        GameEvents.OnCoinsChanged -= HandleCoinsChanged;
    }

    private void HandleCoinsChanged(int total)
    {
        Refresh();
    }

    public void Buy()
    {
        UpgradeSaveData data = Load();
        int level = GetLevel(data);
        int cost = definition.GetCost(level);
        if (data.coins < cost)
        {
            return;
        }
        data.coins -= cost;
        SetLevel(data, level + 1);
        File.WriteAllText(SavePath, JsonUtility.ToJson(data, true));
        GameEvents.RaiseCoinsChanged(data.coins);   // toutes les autres cases se mettent a jour
    }

    private void Refresh()
    {
        if (definition == null)
        {
            return;
        }
        UpgradeSaveData data = Load();
        int level = GetLevel(data);
        int cost = definition.GetCost(level);
        float now = definition.GetValue(level);
        float next = definition.GetValue(level + 1);

        if (titleText != null)
        {
            titleText.text = $"{definition.displayName} niv. {level}\n{Format(now)} -> {Format(next)}";
        }
        if (priceText != null)
        {
            priceText.text = data.coins >= cost ? $"{cost} pieces" : $"{cost} pieces (manque {cost - data.coins})";
        }
        if (button != null)
        {
            button.interactable = data.coins >= cost;   // bouton grise s'il manque des pieces (GDD)
        }
    }

    // Les petites valeurs (Aerodynamisme 0,095) gardent 3 decimales, sinon 2.
    private static string Format(float value)
    {
        return value < 1f ? value.ToString("0.###") : value.ToString("0.##");
    }

    // Un niveau absent de la sauvegarde vaut 0 : on le compte comme le niveau 1.
    private int GetLevel(UpgradeSaveData d)
    {
        int level = stat switch
        {
            Stat.Power => d.powerLevel,
            Stat.Aerodynamics => d.aeroLevel,
            Stat.Momentum => d.momentumLevel,
            Stat.Bounce => d.bounceLevel,
            _ => d.moneyLevel,
        };
        return Mathf.Max(1, level);
    }

    private void SetLevel(UpgradeSaveData d, int level)
    {
        switch (stat)
        {
            case Stat.Power: d.powerLevel = level; break;
            case Stat.Aerodynamics: d.aeroLevel = level; break;
            case Stat.Momentum: d.momentumLevel = level; break;
            case Stat.Bounce: d.bounceLevel = level; break;
            default: d.moneyLevel = level; break;
        }
    }

    private UpgradeSaveData Load()
    {
        return File.Exists(SavePath)
            ? JsonUtility.FromJson<UpgradeSaveData>(File.ReadAllText(SavePath))
            : new UpgradeSaveData();
    }
}
