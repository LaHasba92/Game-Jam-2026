using System;
using System.Globalization;
using System.IO;
using UnityEngine;

[RequireComponent(typeof(Canvas))]
public sealed class Storefront : MonoBehaviour
{
    private enum Tab { Stats, Themes, Gold, Trails }

    private static readonly string[] TabLabels = { "Stats", "Thèmes", "Or + bonus", "Traînées" };
    private static readonly CultureInfo French = CultureInfo.GetCultureInfo("fr-FR");

    private UpgradeShopButton[] upgrades;
    private LauncherShopButton[] launchers;
    private StorefrontLayout layout;
    private Tab tab;
    private bool dirty;
    private bool resetScroll;
    private string notice;

    private void Awake()
    {
        upgrades = GetComponentsInChildren<UpgradeShopButton>(true);
        launchers = GetComponentsInChildren<LauncherShopButton>(true);
        Array.Sort(upgrades, (first, second) => first.StatType.CompareTo(second.StatType));

        // Les composants du prefab restent la source des fiches et des prix.
        foreach (Transform child in transform)
            child.gameObject.SetActive(false);

        layout = new StorefrontLayout((RectTransform)transform, TabLabels, SelectTab, Close);
    }

    private void OnEnable()
    {
        GameEvents.OnCoinsChanged += OnCoinsChanged;
        ThemeCatalog.Changed += OnThemeChanged;
        PremiumStore.Changed += Refresh;
        if (tab == Tab.Gold) PremiumStore.Prepare();
        resetScroll = true;
        Refresh();
    }

    private void OnDisable()
    {
        GameEvents.OnCoinsChanged -= OnCoinsChanged;
        ThemeCatalog.Changed -= OnThemeChanged;
        PremiumStore.Changed -= Refresh;
        notice = null;
    }

    private void OnCoinsChanged(int coins) => Refresh();
    private void OnThemeChanged(ThemeId theme) => Refresh();
    private void Refresh() => dirty = true;

    private void SelectTab(int index)
    {
        var next = (Tab)index;
        if (next == tab) return;
        tab = next;
        notice = null;
        resetScroll = true;
        if (tab == Tab.Gold) PremiumStore.Prepare();
        Refresh();
    }

    private void LateUpdate()
    {
        layout.Resize();
        if (!dirty) return;
        dirty = false;

        float scrollPosition = resetScroll ? 1f : layout.ScrollPosition;
        resetScroll = false;
        layout.ClearContent();
        layout.SetPalette(ThemeCatalog.Palette, (int)tab, ProjectileTrails.IsUnlocked);
        try
        {
            UpgradeSaveData save = UpgradeSaveStore.Load();
            layout.SetWallet(Or(save.coins));
            switch (tab)
            {
                case Tab.Stats: BuildStats(save); break;
                case Tab.Themes: BuildThemes(save); break;
                case Tab.Gold: BuildGold(save.premiumDoubleRewards); break;
                case Tab.Trails: BuildTrails(); break;
            }
        }
        catch (Exception exception) when (IsSaveError(exception))
        {
            Debug.LogWarning(exception.Message, this);
            layout.SetWallet("—");
            layout.Heading("Sauvegarde indisponible", "Impossible de lire la progression pour le moment.");
        }

        layout.SetNotice(tab == Tab.Gold ? PremiumStore.Status : notice);
        layout.SetScrollPosition(scrollPosition);
    }

    private void BuildStats(UpgradeSaveData save)
    {
        layout.Heading("Lanceurs", ProjectileTrails.IsUnlocked ? "" : "Débloque le canon pour accéder aux traînées.");
        foreach (LauncherShopButton launcher in launchers)
        {
            LauncherDefinition item = launcher.Definition;
            if (item == null) continue;
            string requirement = launcher.Owned || save.highestReachedWall >= item.requiredWall
                ? "Puissance multipliée par " + item.powerMultiplier.ToString("0.#", French)
                : $"Atteins le mur {item.requiredWall} · Record {save.highestReachedWall}";
            string action = launcher.Equipped ? "Équipé" : launcher.Owned ? "Équiper" : Or(item.price);
            layout.Row(item.displayName, requirement, action, launcher.CanSelect(save),
                () => Transact(launcher.TrySelect, "Lanceur équipé."), layout.Accent, launcher.Equipped);
        }

        layout.Heading("Améliorations", "Tes niveaux et le gain au prochain achat.");
        foreach (UpgradeShopButton upgrade in upgrades)
        {
            UpgradeDefinition item = upgrade.Definition;
            if (item == null) continue;
            int level = upgrade.GetLevel(save);
            bool canImprove = upgrade.CanImprove(level);
            string value = FormatStat(upgrade.StatType, item.GetValue(level));
            if (canImprove) value += " → " + FormatStat(upgrade.StatType, item.GetValue(level + 1));
            layout.Row(StatName(upgrade.StatType, item.displayName), $"Niv. {level}  ·  {value}",
                canImprove ? Or(item.GetCost(level)) : "Niveau max", upgrade.CanBuy(save),
                () => Transact(upgrade.TryBuy, "Amélioration achetée."), layout.Accent);
        }
    }

    private void BuildThemes(UpgradeSaveData save)
    {
        layout.Heading("Thèmes", "Change le décor de la piste et les couleurs du HUD.");
        foreach (ThemeOption option in ThemeCatalog.Options)
        {
            bool selected = ThemeCatalog.Selected == option.Id;
            bool owned = ThemeCatalog.IsOwned(option.Id);
            string action = selected ? "Actif" : owned ? "Équiper" : Or(option.Price);
            layout.ThemeCard(option, action, !selected && (owned || save.coins >= option.Price),
                () => Transact(() => ThemeCatalog.TryEquip(option.Id), "Thème équipé."), selected);
        }
    }

    private void BuildGold(int bonuses)
    {
        string balance = bonuses > 0 ? $"Bonus ×2 disponibles · {bonuses}" : "";
        layout.Heading("Or et bonus", balance);
        foreach (PremiumOffer offer in PremiumStore.Offers)
        {
            string detail = offer.BonusTokens > 0
                ? $"{Or(offer.Gold)} + {offer.BonusTokens} bonus de gains ×2"
                : Or(offer.Gold);
            layout.Row(offer.Name, detail, PremiumStore.Price(offer.Id) ?? "Indisponible", PremiumStore.CanBuy(offer.Id),
                () => PremiumStore.Buy(offer.Id), layout.Accent);
        }
    }

    private void BuildTrails()
    {
        if (!ProjectileTrails.IsUnlocked)
        {
            layout.Heading("Traînées verrouillées", "Achète le canon dans Stats pour personnaliser le boulet et son effet sonore.");
            layout.Row("Canon", "Voir les conditions de déblocage.", "Voir les stats", true,
                () => SelectTab((int)Tab.Stats), layout.Accent);
            return;
        }

        layout.Heading("Traînées", "Équipe un effet pour le prochain tir.");
        foreach (ProjectileTrail trail in ProjectileTrails.Options)
        {
            bool equipped = ProjectileTrails.Equipped == trail;
            layout.Row(ProjectileTrails.Name(trail), ProjectileTrails.Description(trail),
                equipped ? "Équipée" : "Équiper", !equipped,
                () => Transact(() => ProjectileTrails.TryEquip(trail), "Traînée équipée."),
                ProjectileTrails.Accent(trail), equipped);
        }
    }

    private void Transact(Func<bool> transaction, string success)
    {
        try
        {
            notice = transaction() ? success : "L’offre a changé. Consulte le prix et les conditions.";
        }
        catch (Exception exception) when (IsSaveError(exception))
        {
            Debug.LogWarning(exception.Message, this);
            notice = "Impossible d’enregistrer cet achat.";
        }
        Refresh();
    }

    private void Close()
    {
        var ui = FindFirstObjectByType<UIStateSwitcher>();
        if (ui != null) ui.CloseShop();
        else gameObject.SetActive(false);
    }

    private static string Or(int amount) => amount.ToString("N0", French) + " or";

    private static string StatName(UpgradeShopButton.Stat stat, string fallback) => stat switch
    {
        UpgradeShopButton.Stat.Momentum => "Élan",
        UpgradeShopButton.Stat.Money => "Gains",
        UpgradeShopButton.Stat.Aerodynamics => "Aérodynamisme",
        _ => fallback
    };

    private static string FormatStat(UpgradeShopButton.Stat stat, float value) => stat switch
    {
        UpgradeShopButton.Stat.Power => value.ToString("0.#", French) + " m/s",
        UpgradeShopButton.Stat.Aerodynamics => value.ToString("0.###", French),
        UpgradeShopButton.Stat.Momentum or UpgradeShopButton.Stat.Bounce => value.ToString("P0", French),
        _ => "×" + value.ToString("0.##", French)
    };

    private static bool IsSaveError(Exception exception) =>
        exception is IOException or InvalidDataException or UnauthorizedAccessException or ArgumentException;
}
