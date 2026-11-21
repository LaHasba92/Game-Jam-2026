using System;
using System.IO;
using TMPro;
using UnityEngine;

// Les pieces de fin de partie (GDD section 5) : 0,5 piece par metre x Argent, + un bonus par mur
// qui augmente (10, 20, 30...). Il ajoute les pieces a la sauvegarde de Nathan et les affiche
// sur l'ecran de resultat. « Claim X2 » double le gain (fausse pub), une fois par partie.
// A mettre sur l'ecran de resultat (Panel_Result), avec le texte « Money earned » dans la case.
public class RunRewardManager : MonoBehaviour
{
    private const int GoldBoostCooldownSeconds = 5 * 60;
    private const double GoldBoostMultiplier = 1.25;

    [SerializeField] private TMP_Text moneyText;              // le texte « Money earned »
    [SerializeField] private UpgradeDefinition money;         // la fiche Argent de Nathan
    [SerializeField] private float coinsPerMeter = 0.5f;
    [SerializeField] private int wallBonusBase = 10;
    [SerializeField] private int wallBonusStep = 10;
    [SerializeField] private string saveFileName = UpgradeSaveStore.DefaultFileName;

    private int baseReward;
    private bool doubled;
    private bool usedBonusThisRun;
    private long goldBoostNextClaimAt;
    private long goldBoostForThisRun;

    public int LastReward => doubled ? baseReward * 2 : baseReward;
    public bool CanClaimDouble => !doubled && baseReward > 0;
    public bool UsedPremiumBonus { get; private set; }
    internal bool GoldBoostSaveAvailable { get; private set; }
    internal bool GoldBoostQueued { get; private set; }
    internal bool GoldBoostForNextRun => GoldBoostQueued && goldBoostForThisRun != goldBoostNextClaimAt;
    internal long GoldBoostWaitSeconds => Math.Max(0, goldBoostNextClaimAt - UtcSeconds);
    internal bool CanClaimGoldBoost => GoldBoostSaveAvailable && !GoldBoostForNextRun && GoldBoostWaitSeconds == 0;

    private static long UtcSeconds => DateTimeOffset.UtcNow.ToUnixTimeSeconds();

    private void Awake()
    {
        // L'ecran de resultat est cache pendant le jeu : on s'abonne des le debut, et on reste abonne.
        GameEvents.OnRunEnded += HandleRunEnded;
        GameEvents.OnStateChanged += HandleStateChanged;
        try
        {
            ReadGoldBoost(UpgradeSaveStore.Load(saveFileName));
        }
        catch (Exception exception) when (IsSaveError(exception))
        {
            Debug.LogWarning(exception.Message, this);
        }
    }

    private void OnDestroy()
    {
        GameEvents.OnRunEnded -= HandleRunEnded;
        GameEvents.OnStateChanged -= HandleStateChanged;
    }

    private void HandleStateChanged(GameState previous, GameState next)
    {
        if (next != GameState.Aiming || previous == GameState.Aiming) return;
#if UNITY_EDITOR
        if (AdCaptureDirector.IsPlayingTake) return;
#endif
        goldBoostForThisRun = GoldBoostQueued ? goldBoostNextClaimAt : 0L;
        usedBonusThisRun = false;
        UsedPremiumBonus = false;
        baseReward = 0;
        doubled = false;
    }

    private void HandleRunEnded(RunResult result)
    {
#if UNITY_EDITOR
        if (AdCaptureDirector.IsPlayingTake) return;
#endif
        UpgradeSaveData data = UpgradeSaveStore.Load(saveFileName);
        float multiplier = money != null ? money.GetValue(Mathf.Max(1, data.moneyLevel)) : 1f;

        int wallBonus = 0;
        for (int n = 1; n <= result.wallsBroken; ++n)
        {
            wallBonus += wallBonusBase + wallBonusStep * (n - 1);
        }
        int reward = Mathf.Clamp(
            Mathf.RoundToInt(Mathf.Max(0f, result.distance) * coinsPerMeter * multiplier) + wallBonus,
            0, int.MaxValue / 2);
        if (goldBoostForThisRun > 0)
        {
            reward = (int)Math.Min(int.MaxValue / 2,
                Math.Round(reward * GoldBoostMultiplier, MidpointRounding.AwayFromZero));
            // Un bonus activé pendant ce tir est conservé pour le suivant.
            if (data.goldBoostNextClaimAt == goldBoostForThisRun) data.goldBoostQueued = false;
        }
        bool useBonus = !usedBonusThisRun && reward > 0 && data.premiumDoubleRewards > 0;
        if (useBonus) data.premiumDoubleRewards--;

        data.coins = (int)Math.Min(int.MaxValue, (long)data.coins + (useBonus ? 2 * reward : reward));
        data.highestBrokenWall = Mathf.Max(data.highestBrokenWall, result.wallsBroken);
        data.highestReachedWall = Mathf.Max(data.highestReachedWall, result.wallsBroken + 1);
        UpgradeSaveStore.Save(data, saveFileName);
        ReadGoldBoost(data);
        baseReward = reward;
        doubled = useBonus;
        UsedPremiumBonus = useBonus;
        usedBonusThisRun |= useBonus;
        Show(data.coins);
    }

    internal bool TryClaimGoldBoost()
    {
        try
        {
            UpgradeSaveData data = UpgradeSaveStore.Load(saveFileName);
            ReadGoldBoost(data);
            if (!CanClaimGoldBoost) return false;

            data.goldBoostNextClaimAt = UtcSeconds + GoldBoostCooldownSeconds;
            data.goldBoostQueued = true;
            UpgradeSaveStore.Save(data, saveFileName);
            ReadGoldBoost(data);
            return true;
        }
        catch (Exception exception) when (IsSaveError(exception))
        {
            Debug.LogWarning(exception.Message, this);
            return false;
        }
    }

    private void ReadGoldBoost(UpgradeSaveData data)
    {
        goldBoostNextClaimAt = data.goldBoostNextClaimAt;
        GoldBoostQueued = data.goldBoostQueued;
        GoldBoostSaveAvailable = true;
    }

    private static bool IsSaveError(Exception exception) =>
        exception is IOException or InvalidDataException or UnauthorizedAccessException or ArgumentException;

    // Bouton « Claim X2 » (On Click) : la fausse pub de Hugo appellera ceci a la fin de la pub.
    public void ClaimDouble()
    {
        if (!CanClaimDouble) return;

        UpgradeSaveData data = UpgradeSaveStore.Load(saveFileName);
        data.coins = (int)Math.Min(int.MaxValue, (long)data.coins + baseReward);
        UpgradeSaveStore.Save(data, saveFileName);
        doubled = true;
        Show(data.coins);
    }

    private void Show(int balance)
    {
        if (moneyText != null)
        {
            string goldBoost = goldBoostForThisRun > 0 ? "  ·  ×1,25" : "";
            string premium = UsedPremiumBonus ? "  ·  Bonus ×2" : "";
            moneyText.text = $"+{LastReward:N0} pièces{goldBoost}{premium}";
        }
        GameEvents.RaiseCoinsChanged(balance);
    }
}
