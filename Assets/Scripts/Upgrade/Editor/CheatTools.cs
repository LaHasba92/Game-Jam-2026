using System.IO;
using UnityEditor;
using UnityEngine;

// Menu triche pour tester sans jouer des heures (GDD : « menu triche »).
// Menu : CanonPOW > Triche. Marche en mode edition ET pendant le Play (l'affichage se met a jour).
public static class CheatTools
{
    private const string SaveFileName = "upgrade-test.json";   // la sauvegarde de Nathan

    [MenuItem("CanonPOW/Triche/+1 000 pieces")]
    private static void Add1000() => AddCoins(1000);

    [MenuItem("CanonPOW/Triche/+10 000 pieces")]
    private static void Add10000() => AddCoins(10000);

    [MenuItem("CanonPOW/Triche/+100 000 pieces")]
    private static void Add100000() => AddCoins(100000);

    [MenuItem("CanonPOW/Triche/Debloquer tous les murs (record = mur 10)")]
    private static void UnlockWalls()
    {
        UpgradeSaveData data = Load();
        data.highestReachedWall = Mathf.Max(data.highestReachedWall, 10);
        data.highestBrokenWall = Mathf.Max(data.highestBrokenWall, 9);
        Save(data, "record de murs = 10");
    }

    [MenuItem("CanonPOW/Triche/Remettre les pieces a 0")]
    private static void ZeroCoins()
    {
        UpgradeSaveData data = Load();
        data.coins = 0;
        Save(data, "pieces = 0");
    }

    private static void AddCoins(int amount)
    {
        UpgradeSaveData data = Load();
        data.coins += amount;
        Save(data, $"+{amount} pieces (total {data.coins})");
    }

    private static string SavePath => Path.Combine(Application.persistentDataPath, SaveFileName);

    private static UpgradeSaveData Load()
    {
        return File.Exists(SavePath)
            ? JsonUtility.FromJson<UpgradeSaveData>(File.ReadAllText(SavePath))
            : new UpgradeSaveData();
    }

    private static void Save(UpgradeSaveData data, string message)
    {
        File.WriteAllText(SavePath, JsonUtility.ToJson(data, true));
        if (Application.isPlaying)
        {
            GameEvents.RaiseCoinsChanged(data.coins);   // la boutique et les compteurs se mettent a jour
        }
        Debug.Log($"[Triche] {message}");
    }
}
