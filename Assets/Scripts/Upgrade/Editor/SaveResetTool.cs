using System.IO;
using UnityEditor;
using UnityEngine;

// Outil d'editeur : remet le joueur a zero (ameliorations, pieces, records, meilleur score).
// Menu : CanonPOW > Reinitialiser la sauvegarde. Utile avant un playtest ou la video.
public static class SaveResetTool
{
    private const string SaveFileName = "upgrade-test.json";   // la sauvegarde de Nathan

    [MenuItem("CanonPOW/Reinitialiser la sauvegarde")]
    private static void ResetSave()
    {
        if (!EditorUtility.DisplayDialog("Reinitialiser la sauvegarde",
                "Remettre a zero les ameliorations, les pieces, les lanceurs et le meilleur score ?",
                "Reinitialiser", "Annuler"))
        {
            return;
        }

        string path = Path.Combine(Application.persistentDataPath, SaveFileName);
        if (File.Exists(path))
        {
            File.Delete(path);
        }
        PlayerPrefs.DeleteKey("ScoreSauvegarde");   // le meilleur score de score_manager
        PlayerPrefs.Save();
        Debug.Log($"Sauvegarde reinitialisee ({path} supprime, meilleur score efface).");
    }

    [MenuItem("CanonPOW/Ouvrir le dossier de sauvegarde")]
    private static void OpenFolder()
    {
        EditorUtility.RevealInFinder(Application.persistentDataPath);
    }
}
