using System.IO;
using TMPro;
using UnityEngine;

// Les pieces de fin de partie (GDD section 5) : 0,5 piece par metre x Argent, + un bonus par mur
// qui augmente (10, 20, 30...). Il ajoute les pieces a la sauvegarde de Nathan et les affiche
// sur l'ecran de resultat. « Claim X2 » double le gain (fausse pub), une fois par partie.
// A mettre sur l'ecran de resultat (Panel_Result), avec le texte « Money earned » dans la case.
public class RunRewardManager : MonoBehaviour
{
    [SerializeField] private TMP_Text moneyText;              // le texte « Money earned »
    [SerializeField] private UpgradeDefinition money;         // la fiche Argent de Nathan
    [SerializeField] private float coinsPerMeter = 0.5f;
    [SerializeField] private int wallBonusBase = 10;
    [SerializeField] private int wallBonusStep = 10;
    [SerializeField] private string saveFileName = "upgrade-test.json";   // la sauvegarde de Nathan

    private int lastReward;
    private bool doubled;

    private string SavePath => Path.Combine(Application.persistentDataPath, saveFileName);

    private void Awake()
    {
        // L'ecran de resultat est cache pendant le jeu : on s'abonne des le debut, et on reste abonne.
        GameEvents.OnRunEnded += HandleRunEnded;
    }

    private void OnDestroy()
    {
        GameEvents.OnRunEnded -= HandleRunEnded;
    }

    private void HandleRunEnded(RunResult result)
    {
        UpgradeSaveData data = Load();
        float multiplier = money != null ? money.GetValue(Mathf.Max(1, data.moneyLevel)) : 1f;

        int wallBonus = 0;
        for (int n = 1; n <= result.wallsBroken; ++n)
        {
            wallBonus += wallBonusBase + wallBonusStep * (n - 1);
        }
        lastReward = Mathf.RoundToInt(Mathf.Max(0f, result.distance) * coinsPerMeter * multiplier) + wallBonus;
        doubled = false;

        data.coins += lastReward;
        data.highestBrokenWall = Mathf.Max(data.highestBrokenWall, result.wallsBroken);
        data.highestReachedWall = Mathf.Max(data.highestReachedWall, result.wallsBroken + 1);
        Save(data);
        Show();
    }

    // Bouton « Claim X2 » (On Click) : la fausse pub de Hugo appellera ceci a la fin de la pub.
    public void ClaimDouble()
    {
        if (doubled || lastReward <= 0)
        {
            return;
        }
        doubled = true;
        UpgradeSaveData data = Load();
        data.coins += lastReward;
        Save(data);
        Show();
    }

    private void Show()
    {
        int gain = doubled ? lastReward * 2 : lastReward;
        if (moneyText != null)
        {
            moneyText.text = $"+{gain} pieces";
        }
        GameEvents.RaiseCoinsChanged(Load().coins);
    }

    private UpgradeSaveData Load()
    {
        return File.Exists(SavePath)
            ? JsonUtility.FromJson<UpgradeSaveData>(File.ReadAllText(SavePath))
            : new UpgradeSaveData();
    }

    private void Save(UpgradeSaveData data)
    {
        File.WriteAllText(SavePath, JsonUtility.ToJson(data, true));
    }
}
