using UnityEditor;
using UnityEngine;

// Outil d'editeur : cree les 10 fichiers LevelDefinition du GDD (section 4) en un clic.
// Menu : CanonPOW > Creer les 10 niveaux. Ce fichier DOIT etre dans un dossier « Editor ».
public static class LevelAssetsCreator
{
    private static readonly float[] Distances = { 50, 57.5f, 66, 76, 87, 101, 116, 133, 153, 176 };
    private static readonly int[] Zones = { 1, 1, 1, 1, 2, 2, 2, 3, 3, 3 };
    private static readonly string[] Styles =
    {
        "Bois", "Bois", "Bois renforce", "Bois renforce", "Briques",
        "Briques", "Beton", "Pierre", "Pierre", "Acier (boss)",
    };

    [MenuItem("CanonPOW/Creer les 10 niveaux")]
    private static void CreateLevels()
    {
        const string folder = "Assets/Data/Levels";
        EnsureFolder(folder);

        for (int i = 0; i < 10; ++i)
        {
            string path = $"{folder}/Level_{i + 1:00}.asset";
            if (AssetDatabase.LoadAssetAtPath<LevelDefinition>(path) != null)
            {
                continue;   // ne jamais ecraser les reglages de Nathan
            }
            LevelDefinition level = ScriptableObject.CreateInstance<LevelDefinition>();
            level.levelNumber = i + 1;
            level.wallDistance = Distances[i];
            level.zone = Zones[i];
            level.wallStyle = Styles[i];
            AssetDatabase.CreateAsset(level, path);
        }
        AssetDatabase.SaveAssets();
        Debug.Log($"Niveaux crees dans {folder}");
    }
    // Cree le dossier (et ses parents) en passant par Unity, pour qu'il le connaisse tout de suite.
    private static void EnsureFolder(string path)
    {
        if (AssetDatabase.IsValidFolder(path))
        {
            return;
        }
        string parent = System.IO.Path.GetDirectoryName(path).Replace('\\', '/');
        EnsureFolder(parent);
        AssetDatabase.CreateFolder(parent, System.IO.Path.GetFileName(path));
    }
}
