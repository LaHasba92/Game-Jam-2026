using System;
using System.IO;
using UnityEngine;

public static class LauncherInventory
{
    public const int CannonIndex = 4;

    public static string OwnedKey(int index) => $"CanonPOW.Launcher.{index}";

    public static bool IsOwned(int index, UpgradeSaveData save = null)
    {
        if (index == 0) return true;
        if ((uint)index >= 32) return false;
        if (PlayerPrefs.GetInt(OwnedKey(index), 0) == 1) return true;

        try
        {
            save ??= UpgradeSaveStore.Load();
            return (save.purchasedLauncherMask & (1 << index)) != 0;
        }
        catch (Exception exception) when (exception is IOException or InvalidDataException or
                                          ArgumentException or UnauthorizedAccessException)
        {
            return false;
        }
    }
}
