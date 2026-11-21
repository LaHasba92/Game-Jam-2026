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
                "Remettre a zero les ameliorations, les pieces, les lanceurs (retour a la fronde) et le meilleur score ?",
                "Reinitialiser", "Annuler"))
        {
            return;
        }

        string path = Path.Combine(Application.persistentDataPath, SaveFileName);
        if (File.Exists(path))
        {
            File.Delete(path);
        }
        // Toutes les PlayerPrefs du projet : meilleur score, lanceur achete et equipe (on repart a la fronde),
        // niveaux et pieces du systeme de Benji (RunUpgrades / RunProgress), bonus gratuit...
        PlayerPrefs.DeleteAll();
        PlayerPrefs.Save();
        Debug.Log($"Sauvegarde reinitialisee ({path} supprime, PlayerPrefs effacees : retour a la fronde).");
    }

    [MenuItem("CanonPOW/Ouvrir le dossier de sauvegarde")]
    private static void OpenFolder()
    {
        EditorUtility.RevealInFinder(Application.persistentDataPath);
    }
}
