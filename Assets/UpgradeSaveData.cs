using System.Collections.Generic;

[System.Serializable]
public class UpgradeSaveData
{
    public int coins;
    public int powerLevel;
    public int aeroLevel;
    public int moneyLevel;
    public int highestReachedWall;
    public int highestBrokenWall;
    public bool ownsGiantSlingshot;
    public int momentumLevel;
    public int bounceLevel;
    public int purchasedLauncherMask;
    public int ownedThemes;
    public int selectedTheme;
    public bool hasMigratedThemes;
    public int premiumDoubleRewards;
    public long goldBoostNextClaimAt;
    public bool goldBoostQueued;
    public List<string> fulfilledPurchases;
}
