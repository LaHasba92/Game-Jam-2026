using System.Collections;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;

// Interface d'une partie dans la scène Game. L'installateur assigne les références uGUI.
public class RunHud : MonoBehaviour
{
    [SerializeField] private GameManager game;
    [SerializeField] private LauncherController launcher;
    [SerializeField] private ProjectileController projectile;
    [SerializeField] private RunUpgrades upgrades;
    [SerializeField] private GameObject home, aiming, flying, result, shop, pause;
    [SerializeField] private Text wallet, flightStats, aimStats, resultStats, impact;
    [SerializeField] private Image gaugeFill, recordFill;
    [SerializeField] private Button skip, doubleReward, revive, freeUpgrade;
    [SerializeField] private Button[] upgradeButtons, launcherButtons;

    private float recordAtStart;
    private bool paused;
    private bool previousDebugGauge;
    private Coroutine advert;
    private Button advertButton;
    private string advertLabel;
    private bool advertInteractable;
    private int doubledReward;
    private bool recordPassed;
    private float impactUntil;

    private void Awake() => recordAtStart = RunProgress.Record;

    private void OnEnable()
    {
        GameEvents.OnStateChanged += OnStateChanged;
        GameEvents.OnCoinsChanged += ShowWallet;
        GameEvents.OnWallBroken += ShowImpact;
        if (launcher != null)
        {
            previousDebugGauge = launcher.ShowDebugGauge;
            launcher.ShowDebugGauge = false;
        }
        ShowWallet(RunProgress.Coins);
        GameState current = game != null ? game.CurrentState : GameState.Home;
        OnStateChanged(current, current);
    }

    private void OnDisable()
    {
        GameEvents.OnStateChanged -= OnStateChanged;
        GameEvents.OnCoinsChanged -= ShowWallet;
        GameEvents.OnWallBroken -= ShowImpact;
        if (launcher != null) launcher.ShowDebugGauge = previousDebugGauge;
        CancelAdvert();
        SetLabel(impact, string.Empty);
        Resume();
    }

    private void Update()
    {
        if (game == null) return;
        if (Keyboard.current?.escapeKey.wasPressedThisFrame == true)
        {
            if (paused) Resume();
            else if (shop != null && shop.activeSelf) CloseShop();
            else Pause();
        }

        if (game.CurrentState == GameState.Aiming)
        {
            if (launcher == null) return;
            if (gaugeFill != null)
            {
                Transform bar = gaugeFill.transform.parent;
                if (bar != null) SetVisible(bar.gameObject, launcher.IsAiming);
                gaugeFill.fillAmount = launcher.Gauge;
                gaugeFill.color = launcher.IsPerfect ? new Color(0.35f, 1f, 0.45f) : new Color(1f, 0.76f, 0.18f);
            }
            LauncherDefinition selected = upgrades != null ? upgrades.LauncherAt(upgrades.SelectedIndex) : null;
            string name = selected != null ? selected.displayName : "Fronde";
            SetLabel(aimStats, $"{name}  ·  {(launcher.IsAiming ? "Relâche pour tirer" : "Glisse en bas pour viser")}");
        }
        else if (game.CurrentState == GameState.Flying)
        {
            if (projectile == null) return;
            float distance = projectile.Distance;
            if (recordAtStart > 0f && distance > recordAtStart) recordPassed = true;
            string recordStatus = recordPassed ? "Record dépassé !" : $"Record : {recordAtStart:0} m";
            SetLabel(flightStats, $"{distance:0} m  ·  {projectile.WallsBroken} murs  ·  +{projectile.CoinsPicked} pièces\n{recordStatus}");
            if (recordFill != null)
                recordFill.fillAmount = recordAtStart > 0f ? Mathf.Clamp01(distance / recordAtStart) : 0f;
            if (skip != null) SetVisible(skip.gameObject, projectile.CanSkip && !paused);
            if (Time.unscaledTime >= impactUntil) SetLabel(impact, string.Empty);
        }
        else if (shop != null && shop.activeSelf && freeUpgrade != null)
        {
            freeUpgrade.interactable = advert == null && upgrades != null && upgrades.CanGetFreeUpgrade;
        }
    }

    private void OnStateChanged(GameState from, GameState to)
    {
        CancelAdvert();
        Resume();
        SetVisible(home, to == GameState.Home);
        SetVisible(aiming, to == GameState.Aiming);
        SetVisible(flying, to == GameState.Flying);
        SetVisible(result, to == GameState.Result);
        SetVisible(shop, false);
        if (skip != null) SetVisible(skip.gameObject, false);
        SetLabel(impact, string.Empty);

        if (to == GameState.Aiming && from != GameState.Aiming)
        {
            recordAtStart = RunProgress.Record;
            doubledReward = 0;
            recordPassed = false;
        }
        if (to == GameState.Result && game != null) RefreshResult();
    }

    private void RefreshResult()
    {
        RunResult run = game.LastRun;
        int reward = projectile != null ? projectile.LastReward : 0;
        long total = (long)reward + doubledReward;
        SetLabel(resultStats, $"{(run.newRecord || (recordAtStart > 0f && run.distance > recordAtStart) ? "Nouveau record !\n" : "")}{run.distance:0} m  ·  {run.wallsBroken} murs\n+{total} pièces, dont {run.coinsPicked} ramassées");
        if (doubleReward != null)
        {
            doubleReward.interactable = projectile != null && reward > doubledReward;
            SetLabel(Label(doubleReward), doubledReward == 0 ? "×2 avec une pub" :
                reward > doubledReward ? "×2 sur le gain supplémentaire" : "Gain doublé");
        }
        if (revive != null)
        {
            SetVisible(revive.gameObject, game.CanRevive && projectile != null && projectile.HasShot);
            revive.interactable = true;
            SetLabel(Label(revive), "Relancer avec une pub");
        }
    }

    private void ShowImpact(int wall)
    {
        if (game == null || game.CurrentState != GameState.Flying || projectile == null || projectile.IsSkipping) return;
        SetLabel(impact, wall < 3 ? "POW !" : $"COMBO ×{wall}");
        impactUntil = Time.unscaledTime + 0.8f;
    }

    private void ShowWallet(int total) => SetLabel(wallet, $"{total} pièces");

    public void Play()
    {
        if (advert == null && game != null) game.Play();
    }

    public void GoHome()
    {
        if (advert == null && game != null) game.GoHome();
    }

    public void Skip()
    {
        if (!paused && projectile != null) projectile.Skip();
    }

    public void Pause()
    {
        if (!isActiveAndEnabled || paused || pause == null || game == null ||
            (game.CurrentState != GameState.Aiming && game.CurrentState != GameState.Flying)) return;
        paused = true;
        FeedbackManager.HoldTime();
        pause.SetActive(true);
    }

    public void Resume()
    {
        if (paused)
        {
            FeedbackManager.ReleaseTime();
            paused = false;
        }
        if (pause != null) pause.SetActive(false);
    }

    public void DoubleReward()
    {
        if (!isActiveAndEnabled || advert != null || doubleReward == null || !doubleReward.interactable ||
            game == null || game.CurrentState != GameState.Result || projectile == null) return;

        int reward = projectile.LastReward;
        int bonus = reward - doubledReward;
        if (bonus <= 0) return;

        advert = StartCoroutine(ShowAdvert(doubleReward, () =>
        {
            doubledReward = reward;
            RunProgress.Credit(bonus);
            if (game != null && game.CurrentState == GameState.Result) RefreshResult();
        }));
    }

    public void Revive()
    {
        if (!isActiveAndEnabled || advert != null || revive == null || game == null ||
            !game.CanRevive || projectile == null || !projectile.HasShot) return;
        advert = StartCoroutine(ShowAdvert(revive, () =>
        {
            if (game != null && projectile != null && projectile.HasShot) game.Revive();
        }));
    }

    public void FreeUpgrade()
    {
        if (!isActiveAndEnabled || advert != null || shop == null || !shop.activeSelf ||
            freeUpgrade == null || upgrades == null || !upgrades.CanGetFreeUpgrade) return;
        advert = StartCoroutine(ShowAdvert(freeUpgrade, () =>
        {
            if (upgrades != null) upgrades.GetFreeUpgrade();
            RefreshShop();
        }));
    }

    private IEnumerator ShowAdvert(Button button, UnityEngine.Events.UnityAction complete)
    {
        advertButton = button;
        Text label = Label(button);
        advertLabel = label != null ? label.text : string.Empty;
        advertInteractable = button.interactable;
        button.interactable = false;
        SetLabel(label, "Pub simulée…");
        yield return new WaitForSecondsRealtime(2f);
        advert = null;
        RestoreAdvertButton();
        complete();
    }

    private void CancelAdvert()
    {
        if (advert != null) StopCoroutine(advert);
        advert = null;
        RestoreAdvertButton();
    }

    private void RestoreAdvertButton()
    {
        if (advertButton != null)
        {
            advertButton.interactable = advertInteractable;
            SetLabel(Label(advertButton), advertLabel);
        }
        advertButton = null;
    }

    public void OpenShop()
    {
        if (upgrades == null || shop == null || game == null || game.CurrentState != GameState.Home) return;
        SetVisible(home, false);
        shop.SetActive(true);
        RefreshShop();
    }

    public void CloseShop()
    {
        if (advert != null) return;
        SetVisible(shop, false);
        SetVisible(home, game != null && game.CurrentState == GameState.Home);
    }

    public void BuyUpgrade(int index)
    {
        if (upgrades == null || advert != null) return;
        upgrades.BuyUpgrade(index);
        RefreshShop();
    }

    public void SelectLauncher(int index)
    {
        if (upgrades == null || advert != null) return;
        upgrades.SelectLauncher(index);
        RefreshShop();
    }

    private void RefreshShop()
    {
        if (upgrades == null) return;
        if (freeUpgrade != null) freeUpgrade.interactable = advert == null && upgrades.CanGetFreeUpgrade;
        for (int i = 0; i < (upgradeButtons?.Length ?? 0); i++)
        {
            UpgradeDefinition item = upgrades.UpgradeAt(i);
            Button button = upgradeButtons[i];
            if (button == null) continue;
            button.gameObject.SetActive(item != null);
            if (item == null) continue;
            int level = upgrades.Level(i);
            int cost = item.GetCost(level);
            SetLabel(Label(button), $"{item.displayName} niv. {level}  ·  {cost} pièces");
            button.interactable = cost <= RunProgress.Coins;
        }

        for (int i = 0; i < (launcherButtons?.Length ?? 0); i++)
        {
            LauncherDefinition item = upgrades.LauncherAt(i);
            Button button = launcherButtons[i];
            if (button == null) continue;
            button.gameObject.SetActive(item != null);
            if (item == null) continue;
            bool selected = i == upgrades.SelectedIndex;
            bool owned = upgrades.OwnsLauncher(i);
            SetLabel(Label(button), selected ? $"{item.displayName}  ·  équipé"
                : owned ? item.displayName : $"{item.displayName}  ·  {item.price} pièces  ·  mur {item.requiredWall}");
            button.interactable = !selected && (owned ||
                (item.price <= RunProgress.Coins && item.requiredWall <= RunProgress.HighestWall));
        }
    }

    private static void SetVisible(GameObject panel, bool visible)
    {
        if (panel != null) panel.SetActive(visible);
    }

    private static void SetLabel(Text label, string value)
    {
        if (label != null) label.text = value;
    }

    private static Text Label(Button button) => button.GetComponentInChildren<Text>(true);
}
