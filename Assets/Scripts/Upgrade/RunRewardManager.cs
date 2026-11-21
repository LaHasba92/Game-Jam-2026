using System;
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
    [SerializeField] private string saveFileName = UpgradeSaveStore.DefaultFileName;

    private int baseReward;
    private bool doubled;
    private bool usedBonusThisRun;

    public int LastReward => doubled ? baseReward * 2 : baseReward;
    public bool CanClaimDouble => !doubled && baseReward > 0;
    public bool UsedPremiumBonus { get; private set; }

    private void Awake()
    {
        // L'ecran de resultat est cache pendant le jeu : on s'abonne des le debut, et on reste abonne.
        GameEvents.OnRunEnded += HandleRunEnded;
        GameEvents.OnStateChanged += HandleStateChanged;
    }

    private void OnDestroy()
    {
        GameEvents.OnRunEnded -= HandleRunEnded;
        GameEvents.OnStateChanged -= HandleStateChanged;
    }

    private void HandleStateChanged(GameState previous, GameState next)
    {
        if (next != GameState.Aiming) return;
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
        bool useBonus = !usedBonusThisRun && reward > 0 && data.premiumDoubleRewards > 0;
        if (useBonus) data.premiumDoubleRewards--;

        data.coins = (int)Math.Min(int.MaxValue, (long)data.coins + (useBonus ? 2 * reward : reward));
        data.highestBrokenWall = Mathf.Max(data.highestBrokenWall, result.wallsBroken);
        data.highestReachedWall = Mathf.Max(data.highestReachedWall, result.wallsBroken + 1);
        UpgradeSaveStore.Save(data, saveFileName);
        baseReward = reward;
        doubled = useBonus;
        UsedPremiumBonus = useBonus;
        usedBonusThisRun |= useBonus;
        Show(data.coins);
    }

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
            string bonus = UsedPremiumBonus ? "  ·  Bonus ×2" : "";
            moneyText.text = $"+{LastReward:N0} pièces{bonus}";
        }
        GameEvents.RaiseCoinsChanged(balance);
    }
}
