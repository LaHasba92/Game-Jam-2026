using System.IO;
using UnityEngine;

public class PowerPurchaseTest : MonoBehaviour
{
    [SerializeField] private UpgradeDefinition power;
    [SerializeField] private UpgradeDefinition aerodynamics;
    [SerializeField] private UpgradeDefinition money;
    [SerializeField] private LauncherDefinition giantSlingshot;
    [SerializeField] private UpgradeDefinition momentum;
    [SerializeField] private UpgradeDefinition bounce;

    private int bounceLevel = 1;
    private int coins = 230;
    private int powerLevel = 1;
    private int aeroLevel = 1;
    private int moneyLevel = 1;
    private int highestReachedWall;
    private int highestBrokenWall;
    private bool ownsGiantSlingshot;
    private string message = "";
    private int momentumLevel = 1;

    private string SavePath =>
        Path.Combine(Application.persistentDataPath, "upgrade-test.json");

   

    private void Awake()
    {
        if (!File.Exists(SavePath))
            return;

        string json = File.ReadAllText(SavePath);
        UpgradeSaveData data = JsonUtility.FromJson<UpgradeSaveData>(json);

        if (data == null) return;
        coins = data.coins;
        powerLevel = Mathf.Max(1, data.powerLevel);
        aeroLevel = Mathf.Max(1, data.aeroLevel);
        moneyLevel = Mathf.Max(1, data.moneyLevel);
        momentumLevel = Mathf.Max(1, data.momentumLevel);
        ownsGiantSlingshot = data.ownsGiantSlingshot;
        highestBrokenWall = data.highestBrokenWall;
        bounceLevel = Mathf.Max(1, data.bounceLevel);


        // Une ancienne sauvegarde peut contenir uniquement le dernier mur cassé.
        highestReachedWall = Mathf.Max(data.highestReachedWall, highestBrokenWall);
    }

    private void SaveProgress()
    {
        UpgradeSaveData data = new UpgradeSaveData();
        data.coins = coins;
        data.powerLevel = powerLevel;
        data.aeroLevel = aeroLevel;
        data.moneyLevel = moneyLevel;
        data.momentumLevel = momentumLevel;
        data.highestReachedWall = highestReachedWall;
        data.highestBrokenWall = highestBrokenWall;
        data.ownsGiantSlingshot = ownsGiantSlingshot;
        data.bounceLevel = bounceLevel;

        string json = JsonUtility.ToJson(data, true);
        File.WriteAllText(SavePath, json);
        Debug.Log($"Sauvegarde de test écrite dans {SavePath}");
    }

    private void OnGUI()
    {
        GUILayout.Label($"Pièces : {coins}");

        DrawLauncher();
        DrawUpgrade(power, ref powerLevel);
        DrawUpgrade(aerodynamics, ref aeroLevel);
        DrawUpgrade(money, ref moneyLevel);
        DrawUpgrade(momentum, ref momentumLevel);
        DrawUpgrade(bounce, ref bounceLevel);

        if (GUILayout.Button("Test : +10 000 pièces"))
        {
            coins += 10000;
            SaveProgress();
            message = "10 000 pièces ajoutées.";
        }

        GUILayout.Label(message);
    }

    private void DrawUpgrade(UpgradeDefinition upgrade, ref int level)
    {
        if (upgrade == null)
        {
            GUILayout.Label("Fiche d’amélioration manquante dans l’Inspector.");
            return;
        }

        int cost = upgrade.GetCost(level);
        float value = upgrade.GetValue(level);

        GUILayout.Label($"{upgrade.displayName} | Niveau {level} | Valeur {value:0.####}");

        if (!GUILayout.Button($"Acheter {upgrade.displayName} ({cost} pièces)"))
            return;

        if (coins < cost)
        {
            message = "Pas assez de pièces.";
            return;
        }

        coins -= cost;
        level++;
        SaveProgress();
        message = $"{upgrade.displayName} passe au niveau {level}.";
    }

    private void DrawLauncher()
    {
        if (giantSlingshot == null)
        {
            GUILayout.Label("Glisse la fiche GiantSlingshot dans l’Inspector.");
            return;
        }

        if (GUILayout.Button("Test : terminer un tir"))
        {
            // Simule un tir de 100 m avec 10 pièces ramassées et 2 murs cassés.
            float multiplier = money != null ? money.GetValue(moneyLevel) : 1f;
            int reward = EconomyManager.CalculateReward(100f, 10, 2, multiplier);

            coins += reward;
            SaveProgress();
            message = $"Fin du tir : +{reward} pièces.";
        }

        GUILayout.Label($"Mur le plus avancé atteint : {highestReachedWall}");

        // Ce bouton permet de tester le déblocage sans simuler la casse du mur.
        if (GUILayout.Button("Test : atteindre le mur suivant"))
        {
            highestReachedWall++;
            SaveProgress();
            message = $"Mur {highestReachedWall} atteint.";
        }

        string launcherName = giantSlingshot.displayName;
        int price = giantSlingshot.price;

        if (!GUILayout.Button($"Acheter {launcherName} ({price} pièces)"))
            return;

        if (ownsGiantSlingshot)
        {
            message = "Tu possèdes déjà ce lanceur.";
            return;
        }

        if (highestReachedWall < giantSlingshot.requiredWall)
        {
            message = $"Atteins d’abord le mur {giantSlingshot.requiredWall}.";
            return;
        }

        if (coins < price)
        {
            message = "Pas assez de pièces.";
            return;
        }

        coins -= price;
        ownsGiantSlingshot = true;
        SaveProgress();
        message = $"{launcherName} acheté !";
    }

    [ContextMenu("Tester les gains de fin de partie")]
    private void TestRunReward()
    {
        // Même partie pour les deux essais : 100 m, 10 pièces ramassées, 2 murs cassés.
        for (int level = 1; level <= 2; level++)
        {
            float multiplier = money != null ? money.GetValue(level) : 1f;
            int reward = EconomyManager.CalculateReward(100f, 10, 2, multiplier);
            Debug.Log($"Argent niveau {level} : gain de {reward} pièces.");
        }
    }
}