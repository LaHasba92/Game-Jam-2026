using System.IO;
using UnityEditor;
using UnityEngine;

// Outil d'editeur : remet le joueur a zero (ameliorations, pieces, records, meilleur score).
// Menu : CanonPOW > Reinitialiser la sauvegarde. Utile avant un playtest ou la video.
public static class SaveResetTool
{
    [MenuItem("CanonPOW/Reinitialiser la sauvegarde")]
    private static void ResetSave()
    {
        if (!EditorUtility.DisplayDialog("Reinitialiser la sauvegarde",
                "Remettre à zéro la progression, l’or, les lanceurs, les thèmes et les traînées ?",
                "Reinitialiser", "Annuler"))
        {
            return;
        }

        string path = Path.Combine(Application.persistentDataPath, UpgradeSaveStore.DefaultFileName);
        if (File.Exists(path))
        {
            File.Delete(path);
        }
        PlayerPrefs.DeleteKey("ScoreSauvegarde");   // le meilleur score de score_manager
        PlayerPrefs.DeleteKey(LauncherVisuals.SelectedKey);
        for (int i = 1; i <= LauncherInventory.CannonIndex; ++i)
            PlayerPrefs.DeleteKey(LauncherInventory.OwnedKey(i));
        PlayerPrefs.DeleteKey("CanonPOW.ProjectileTrail");
        PlayerPrefs.DeleteKey("CanonPOW.SelectedTheme");
        PlayerPrefs.DeleteKey("CanonPOW.Theme.1");
        PlayerPrefs.Save();
        Debug.Log($"Sauvegarde reinitialisee ({path} supprime, meilleur score efface).");
    }

    [MenuItem("CanonPOW/Ouvrir le dossier de sauvegarde")]
    private static void OpenFolder()
    {
        EditorUtility.RevealInFinder(Application.persistentDataPath);
    }
}
