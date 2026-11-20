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
    private float previousScale = 1f;
    private Coroutine advert;
    private bool doubled;
    private float impactUntil;

    private void OnEnable()
    {
        GameEvents.OnStateChanged += OnStateChanged;
        GameEvents.OnCoinsChanged += ShowWallet;
        GameEvents.OnWallBroken += ShowImpact;
        if (launcher != null) launcher.ShowDebugGauge = false;
        ShowWallet(RunProgress.Coins);
        OnStateChanged(GameState.Home, game != null ? game.CurrentState : GameState.Home);
    }

    private void OnDisable()
    {
        GameEvents.OnStateChanged -= OnStateChanged;
        GameEvents.OnCoinsChanged -= ShowWallet;
        GameEvents.OnWallBroken -= ShowImpact;
        if (launcher != null) launcher.ShowDebugGauge = true;
        if (advert != null) StopCoroutine(advert);
        advert = null;
        Resume();
    }

    private void Update()
    {
        if (game == null || launcher == null || projectile == null) return;
        if (Keyboard.current?.escapeKey.wasPressedThisFrame == true)
        {
            if (paused) Resume();
            else if (shop != null && shop.activeSelf) CloseShop();
            else Pause();
        }

        if (game.CurrentState == GameState.Aiming)
        {
            gaugeFill.transform.parent.gameObject.SetActive(launcher.IsAiming);
            gaugeFill.fillAmount = launcher.Gauge;
            gaugeFill.color = launcher.IsPerfect ? new Color(0.35f, 1f, 0.45f) : new Color(1f, 0.76f, 0.18f);
            string name = upgrades != null ? upgrades.LauncherAt(upgrades.SelectedIndex)?.displayName ?? "Fronde" : "Fronde";
            aimStats.text = $"{name}  ·  {(launcher.IsAiming ? "Relâche pour tirer" : "Glisse en bas pour viser")}";
        }
        else if (game.CurrentState == GameState.Flying)
        {
            float distance = projectile.Distance;
            flightStats.text = $"{distance:0} m  ·  {projectile.WallsBroken} murs  ·  +{projectile.CoinsPicked} pièces\nRecord : {recordAtStart:0} m";
            recordFill.fillAmount = recordAtStart > 0f ? Mathf.Clamp01(distance / recordAtStart) : 0f;
            skip.gameObject.SetActive(projectile.CanSkip && !paused);
            if (Time.time >= impactUntil) impact.text = string.Empty;
        }
        else if (shop.activeSelf)
        {
            freeUpgrade.interactable = advert == null && upgrades.CanGetFreeUpgrade;
        }
    }

    private void OnStateChanged(GameState from, GameState to)
    {
        Resume();
        home.SetActive(to == GameState.Home);
        aiming.SetActive(to == GameState.Aiming);
        flying.SetActive(to == GameState.Flying);
        result.SetActive(to == GameState.Result);
        shop.SetActive(false);

        if (to == GameState.Aiming)
        {
            recordAtStart = RunProgress.Record;
            doubled = false;
            impact.text = string.Empty;
        }
        if (to != GameState.Result || game == null) return;

        RunResult run = game.LastRun;
        int reward = projectile != null ? projectile.LastReward : RunProgress.Reward(run);
        resultStats.text = $"{(run.newRecord ? "Nouveau record !\n" : "")}{run.distance:0} m  ·  {run.wallsBroken} murs\n+{reward} pièces, dont {run.coinsPicked} ramassées";
        Label(doubleReward).text = doubled ? "Gain doublé" : "×2 avec une pub";
        doubleReward.interactable = !doubled && reward > 0;
        revive.gameObject.SetActive(game.CanRevive && projectile != null && projectile.HasShot);
        revive.interactable = true;
        Label(revive).text = "Relancer avec une pub";
    }

    private void ShowImpact(int wall)
    {
        if (projectile == null || projectile.IsSkipping) return;
        impact.text = wall == 1 ? "POW !" : $"COMBO ×{wall}";
        impactUntil = Time.time + 0.8f;
    }

    private void ShowWallet(int total) => wallet.text = $"{total} pièces";

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
        if (paused || Time.timeScale <= 0f || game == null ||
            (game.CurrentState != GameState.Aiming && game.CurrentState != GameState.Flying)) return;
        paused = true;
        previousScale = Time.timeScale;
        Time.timeScale = 0f;
        pause.SetActive(true);
    }

    public void Resume()
    {
        if (paused)
        {
            if (Time.timeScale == 0f) Time.timeScale = previousScale;
            paused = false;
        }
        if (pause != null) pause.SetActive(false);
    }

    public void DoubleReward()
    {
        if (advert != null || !doubleReward.interactable || game == null ||
            game.CurrentState != GameState.Result || projectile == null) return;

        advert = StartCoroutine(ShowAdvert(doubleReward, () =>
        {
            doubled = true;
            RunProgress.Credit(projectile.LastReward);
            Label(doubleReward).text = "Gain doublé";
        }));
    }

    public void Revive()
    {
        if (advert != null || game == null || !game.CanRevive || projectile == null || !projectile.HasShot) return;
        advert = StartCoroutine(ShowAdvert(revive, game.Revive));
    }

    public void FreeUpgrade()
    {
        if (advert != null || !upgrades.CanGetFreeUpgrade) return;
        advert = StartCoroutine(ShowAdvert(freeUpgrade, () =>
        {
            upgrades.GetFreeUpgrade();
            Label(freeUpgrade).text = "Amélioration gratuite";
            RefreshShop();
        }));
    }

    private IEnumerator ShowAdvert(Button button, UnityEngine.Events.UnityAction complete)
    {
        button.interactable = false;
        Label(button).text = "Pub simulée…";
        yield return new WaitForSecondsRealtime(2f);
        advert = null;
        complete();
    }

    public void OpenShop()
    {
        if (upgrades == null || game == null || game.CurrentState != GameState.Home) return;
        home.SetActive(false);
        shop.SetActive(true);
        RefreshShop();
    }

    public void CloseShop()
    {
        if (advert != null) return;
        shop.SetActive(false);
        home.SetActive(game != null && game.CurrentState == GameState.Home);
    }

    public void BuyUpgrade(int index)
    {
        upgrades.BuyUpgrade(index);
        RefreshShop();
    }

    public void SelectLauncher(int index)
    {
        upgrades.SelectLauncher(index);
        RefreshShop();
    }

    private void RefreshShop()
    {
        for (int i = 0; i < upgradeButtons.Length; i++)
        {
            UpgradeDefinition item = upgrades.UpgradeAt(i);
            Button button = upgradeButtons[i];
            button.gameObject.SetActive(item != null);
            if (item == null) continue;
            int level = upgrades.Level(i);
            int cost = item.GetCost(level);
            Label(button).text = $"{item.displayName} niv. {level}  ·  {cost} pièces";
            button.interactable = cost <= RunProgress.Coins;
        }

        for (int i = 0; i < launcherButtons.Length; i++)
        {
            LauncherDefinition item = upgrades.LauncherAt(i);
            Button button = launcherButtons[i];
            button.gameObject.SetActive(item != null);
            if (item == null) continue;
            bool selected = i == upgrades.SelectedIndex;
            bool owned = upgrades.OwnsLauncher(i);
            Label(button).text = selected ? $"{item.displayName}  ·  équipé"
                : owned ? item.displayName : $"{item.displayName}  ·  {item.price} pièces  ·  mur {item.requiredWall}";
            button.interactable = !selected && (owned ||
                (item.price <= RunProgress.Coins && item.requiredWall <= RunProgress.HighestWall));
        }
    }

    private static Text Label(Button button) => button.GetComponentInChildren<Text>(true);
}
