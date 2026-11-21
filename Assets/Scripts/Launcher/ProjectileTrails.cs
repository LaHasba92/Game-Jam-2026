using System;
using System.Collections.Generic;
using UnityEngine;

public enum ProjectileTrail
{
    None = 0,
    Fire = 1,
    Bubbles = 2,
    Water = 3,
    Aura = 4
}

public static class ProjectileTrails
{
    private const string EquippedKey = "CanonPOW.ProjectileTrail";
    private const string AudioPath = "ProjectileTrails/";
    private static AudioSource unlockAudio;

    public static IReadOnlyList<ProjectileTrail> Options { get; } =
        Array.AsReadOnly(new[] { ProjectileTrail.None, ProjectileTrail.Fire, ProjectileTrail.Bubbles,
            ProjectileTrail.Water, ProjectileTrail.Aura });

    public static bool IsUnlocked => LauncherInventory.IsOwned(LauncherInventory.CannonIndex);

    public static ProjectileTrail Equipped
    {
        get
        {
            var trail = (ProjectileTrail)PlayerPrefs.GetInt(EquippedKey, 0);
            return IsUnlocked && IsValid(trail) ? trail : ProjectileTrail.None;
        }
    }

    public static bool TryEquip(ProjectileTrail trail)
    {
        if (!IsUnlocked || !IsValid(trail)) return false;
        if (Equipped != trail)
        {
            PlayerPrefs.SetInt(EquippedKey, (int)trail);
            PlayerPrefs.Save();
        }
        return true;
    }

    public static string Name(ProjectileTrail trail) => trail switch
    {
        ProjectileTrail.Fire => "Feu",
        ProjectileTrail.Bubbles => "Bulles",
        ProjectileTrail.Water => "Eau",
        ProjectileTrail.Aura => "Aura",
        _ => "Classique"
    };

    public static string Description(ProjectileTrail trail) => trail switch
    {
        ProjectileTrail.Fire => "Flamme ambrée et crépitements",
        ProjectileTrail.Bubbles => "Bulles irisées et petits claquements",
        ProjectileTrail.Water => "Ruban d’eau et bulles douces",
        ProjectileTrail.Aura => "Étincelles violettes et tintements cristallins",
        _ => "Boulet d’origine"
    };

    public static Color Accent(ProjectileTrail trail) => trail switch
    {
        ProjectileTrail.Fire => new Color32(255, 111, 52, 255),
        ProjectileTrail.Bubbles => new Color32(131, 249, 201, 255),
        ProjectileTrail.Water => new Color32(76, 188, 255, 255),
        ProjectileTrail.Aura => new Color32(206, 128, 255, 255),
        _ => new Color32(181, 195, 217, 255)
    };

    internal static void PlayFireUnlock()
    {
        if (!Application.isPlaying) return;
        AudioClip clip = Resources.Load<AudioClip>(AudioPath + "FireUnlock");
        if (clip == null) return;

        if (unlockAudio == null)
        {
            unlockAudio = new GameObject("CanonPOW — Déblocage de traînée").AddComponent<AudioSource>();
            unlockAudio.playOnAwake = false;
            unlockAudio.spatialBlend = 0f;
        }
        unlockAudio.PlayOneShot(clip, 0.7f);
    }

    internal static AudioClip LoadAudio(ProjectileTrail trail) => trail switch
    {
        ProjectileTrail.Fire => Resources.Load<AudioClip>(AudioPath + "Fire"),
        ProjectileTrail.Bubbles or ProjectileTrail.Water => Resources.Load<AudioClip>(AudioPath + "Water"),
        ProjectileTrail.Aura => Resources.Load<AudioClip>(AudioPath + "Aura"),
        _ => null
    };

    private static bool IsValid(ProjectileTrail trail) => trail >= ProjectileTrail.None && trail <= ProjectileTrail.Aura;
}
