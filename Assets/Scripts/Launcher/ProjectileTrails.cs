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
    private static AudioSource previewSource;

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
        ProjectileTrail.Water => "Ruban d’eau et gouttes en cascade",
        ProjectileTrail.Aura => "Étincelles violettes et résonance douce",
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

    public static void Preview(ProjectileTrail trail)
    {
        if (!Application.isPlaying || !IsUnlocked || !IsValid(trail)) return;
        StopPreview();
        AudioClip clip = LoadAudio(trail);
        if (clip == null) return;

        if (previewSource == null)
        {
            previewSource = new GameObject("CanonPOW — Écoute de traînée").AddComponent<AudioSource>();
            previewSource.playOnAwake = false;
            previewSource.spatialBlend = 0f;
            previewSource.volume = 0.63f;
        }
        previewSource.clip = clip;
        previewSource.Play();
    }

    public static void StopPreview()
    {
        if (previewSource != null) previewSource.Stop();
    }

    internal static AudioClip LoadAudio(ProjectileTrail trail) => trail == ProjectileTrail.None
        ? null
        : Resources.Load<AudioClip>("ProjectileTrails/" + trail);

    private static bool IsValid(ProjectileTrail trail) => trail >= ProjectileTrail.None && trail <= ProjectileTrail.Aura;
}
