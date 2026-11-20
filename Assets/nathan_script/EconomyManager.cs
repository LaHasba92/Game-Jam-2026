using UnityEngine;

public static class EconomyManager
{
    public static int CalculateReward(float distance, int collectedCoins,
        int brokenWalls, float moneyMultiplier)
    {
        float baseReward = distance * 0.5f + collectedCoins;
        int wallBonus = brokenWalls * 20;

        return Mathf.FloorToInt(baseReward * moneyMultiplier + wallBonus);
    }
}