using System;
using System.Collections.Generic;
using System.IO;
using UnityEngine;

public enum ThemeId { City, Forest, Cyberpunk }

public readonly struct ThemePalette
{
    public readonly Color Background;
    public readonly Color Surface;
    public readonly Color Text;
    public readonly Color Muted;
    public readonly Color Accent;
    public readonly Color Secondary;
    public readonly Color Border;
    public readonly Color Danger;

    internal ThemePalette(int background, int surface, int text, int muted, int accent, int secondary,
                          int border, int danger)
    {
        Background = Rgb(background);
        Surface = Rgb(surface);
        Text = Rgb(text);
        Muted = Rgb(muted);
        Accent = Rgb(accent);
        Secondary = Rgb(secondary);
        Border = Rgb(border);
        Danger = Rgb(danger);
    }

    internal static Color32 Rgb(int rgb) => new Color32((byte)(rgb >> 16), (byte)(rgb >> 8), (byte)rgb, 255);
}

public readonly struct ThemeOption
{
    public ThemeId Id { get; }
    public string Name { get; }
    public string Description { get; }
    public int Price { get; }
    public ThemePalette Palette { get; }

    internal ThemeOption(ThemeId id, string name, string description, int price, ThemePalette palette)
    {
        Id = id;
        Name = name;
        Description = description;
        Price = price;
        Palette = palette;
    }
}

public static class ThemeCatalog
{
    private const string LegacySelectedKey = "CanonPOW.SelectedTheme";
    private const string LegacyOwnedPrefix = "CanonPOW.Theme.";
    private const int DefaultThemeBit = 1 << (int)ThemeId.City;

    public static IReadOnlyList<ThemeOption> Options { get; } = Array.AsReadOnly(new[]
    {
        new ThemeOption(ThemeId.City, "Ville", "Avenue en plein jour", 0,
            new ThemePalette(0x111b29, 0x1c2b3d, 0xf7f4ea, 0xb6c1cf, 0xffc255, 0x72e4ad, 0x485c72, 0xff8580)),
        new ThemeOption(ThemeId.Forest, "Forêt", "Une piste au milieu des pins", 3000,
            new ThemePalette(0x10231f, 0x1b332b, 0xf1f8e9, 0xb7cbbd, 0xc0eb79, 0x72e2c4, 0x498267, 0xff9385)),
        new ThemeOption(ThemeId.Cyberpunk, "District Néon", "Une avenue nocturne aux néons cyan et magenta", 5000,
            new ThemePalette(0x111021, 0x1d1933, 0xf4f2ff, 0xbab2d8, 0x55edff, 0xff7fd5, 0x6956a7, 0xff8caa)),
    });

    public static ThemeId Selected => TryLoad(out UpgradeSaveData data) ? ReadSelection(data) : ThemeId.City;
    public static ThemePalette Palette => Options[(int)Selected].Palette;
    public static event Action<ThemeId> Changed;

    public static bool IsOwned(ThemeId id) => id == ThemeId.City ||
        (IsKnown(id) && TryLoad(out UpgradeSaveData data) && Owns(ReadOwnership(data), id));

    public static bool TryEquip(ThemeId id)
    {
        if (!IsKnown(id) || !TryLoad(out UpgradeSaveData data)) return false;
        ThemeId previous = ReadSelection(data);
        int ownership = ReadOwnership(data);
        bool purchase = !Owns(ownership, id);
        int price = Options[(int)id].Price;
        if (purchase && data.coins < price) return false;
        if (!purchase && id == previous) return true;

        if (purchase) data.coins -= price;
        data.ownedThemes = ownership | (1 << (int)id);
        data.selectedTheme = (int)id;
        data.hasMigratedThemes = true;
        try
        {
            UpgradeSaveStore.Save(data);
        }
        catch (Exception exception) when (exception is IOException || exception is UnauthorizedAccessException)
        {
            Debug.LogWarning($"Thèmes : impossible d’enregistrer la sélection. {exception.Message}");
            return false;
        }

        if (purchase) GameEvents.RaiseCoinsChanged(data.coins);
        Changed?.Invoke(id);
        return true;
    }

    private static ThemeId ReadSelection(UpgradeSaveData data)
    {
        var selection = (ThemeId)(data.hasMigratedThemes
            ? data.selectedTheme
            : PlayerPrefs.GetInt(LegacySelectedKey, (int)ThemeId.City));
        return IsKnown(selection) && Owns(ReadOwnership(data), selection) ? selection : ThemeId.City;
    }

    private static int ReadOwnership(UpgradeSaveData data)
    {
        if (data.hasMigratedThemes) return data.ownedThemes | DefaultThemeBit;

        // Les profils issus de la branche de Sami gardaient les thèmes dans PlayerPrefs.
        return DefaultThemeBit | (PlayerPrefs.GetInt(LegacyOwnedPrefix + (int)ThemeId.Forest) == 1
            ? 1 << (int)ThemeId.Forest
            : 0);
    }

    private static bool IsKnown(ThemeId id) => (uint)id < (uint)Options.Count;
    private static bool Owns(int mask, ThemeId id) => (mask & (1 << (int)id)) != 0;

    private static bool TryLoad(out UpgradeSaveData data)
    {
        try
        {
            data = UpgradeSaveStore.Load();
            return true;
        }
        catch (Exception exception) when (exception is IOException || exception is UnauthorizedAccessException ||
                                          exception is InvalidDataException || exception is ArgumentException)
        {
            data = null;
            Debug.LogWarning($"Thèmes : impossible de lire la sauvegarde. {exception.Message}");
            return false;
        }
    }
}
