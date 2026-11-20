using UnityEngine;

// Sauvegarde de la boucle Game. Les écrans de test de la boutique ont leur propre sauvegarde.
public static class RunProgress
{
    private const string RecordKey = "CanonPOW.RecordDistance";
    private const string WalletKey = "CanonPOW.Wallet";
    private const string WallKey = "CanonPOW.HighestReachedWall";

    public static float Record => PlayerPrefs.GetFloat(RecordKey);
    public static int Coins => PlayerPrefs.GetInt(WalletKey);
    public static int HighestWall => PlayerPrefs.GetInt(WallKey);

    public static bool UpdateRecord(float distance)
    {
        if (distance <= Record) return false;
        PlayerPrefs.SetFloat(RecordKey, distance);
        PlayerPrefs.Save();
        return true;
    }

    public static void ReachWall(int number)
    {
        if (number <= HighestWall) return;
        PlayerPrefs.SetInt(WallKey, number);
        PlayerPrefs.Save();
    }

    public static int Reward(RunResult run, float multiplier = 1f) =>
        EconomyManager.CalculateReward(run.distance, run.coinsPicked, run.wallsBroken, multiplier);

    public static void Credit(int amount)
    {
        if (amount <= 0) return;
        SetCoins(Coins + amount);
    }

    public static bool Spend(int amount)
    {
        if (amount < 0 || amount > Coins) return false;
        SetCoins(Coins - amount);
        return true;
    }

    private static void SetCoins(int total)
    {
        PlayerPrefs.SetInt(WalletKey, total);
        PlayerPrefs.Save();
        GameEvents.RaiseCoinsChanged(total);
    }
}
