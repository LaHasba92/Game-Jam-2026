using UnityEngine;

// Sauvegarde de la boucle Game. Les écrans de test de la boutique ont leur propre sauvegarde.
public static class RunProgress
{
    private const string RecordKey = "CanonPOW.RecordDistance";
    private const string WalletKey = "CanonPOW.Wallet";
    private const string WallKey = "CanonPOW.HighestReachedWall";

    public static float Record => JamSave.GetFloat(RecordKey);
    public static int Coins => JamSave.GetInt(WalletKey);
    public static int HighestWall => JamSave.GetInt(WallKey);

    public static bool UpdateRecord(float distance)
    {
        if (float.IsNaN(distance) || float.IsInfinity(distance) || distance <= Record) return false;
        JamSave.SetFloat(RecordKey, distance);
        JamSave.Save();
        return true;
    }

    public static void ReachWall(int number)
    {
        if (number <= HighestWall) return;
        JamSave.SetInt(WallKey, number);
        JamSave.Save();
    }

    public static int Reward(RunResult run, float multiplier = 1f) =>
        EconomyManager.CalculateReward(run.distance, run.coinsPicked, run.wallsBroken, multiplier);

    public static void Credit(int amount)
    {
        if (amount <= 0) return;
        int current = Coins;
        int total = (int)System.Math.Min(int.MaxValue, (long)current + amount);
        if (total != current) SetCoins(total);
    }

    public static bool Spend(int amount)
    {
        int current = Coins;
        if (amount < 0 || amount > current) return false;
        if (amount > 0) SetCoins(current - amount);
        return true;
    }

    private static void SetCoins(int total)
    {
        JamSave.SetInt(WalletKey, total);
        JamSave.Save();
        GameEvents.RaiseCoinsChanged(total);
    }
}
