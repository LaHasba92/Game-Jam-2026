using UnityEditor;
using UnityEngine;

// Outil d'editeur : cree un prefab de mur greybox de 30 m (intact + 45 morceaux),
// avec la meme structure que les futurs modeles de Louis. Menu : CanonPOW > Creer le mur greybox.
public static class GreyboxWallCreator
{
    private const float Height = 30f;   // = le plafond du couloir : on voit le mur que le boulet casse
    private const float Width = 6f;
    private const float PieceSize = 2f;

    [MenuItem("CanonPOW/Creer le mur greybox")]
    private static void CreateWall()
    {
        const string folder = "Assets/Prefabs/Walls";
        EnsureFolder(folder);

        GameObject root = new GameObject("Wall_Greybox");
        WallVisualSet set = root.AddComponent<WallVisualSet>();

        // Intact : un seul cube, face avant en x = 0
        GameObject intact = GameObject.CreatePrimitive(PrimitiveType.Cube);
        intact.name = "Intact";
        intact.transform.SetParent(root.transform);
        intact.transform.localPosition = new Vector3(0.5f, Height * 0.5f, 0f);
        intact.transform.localScale = new Vector3(1f, Height, Width);

        // Broken : des cubes de 2 m, chacun avec un Rigidbody
        GameObject broken = new GameObject("Broken");
        broken.transform.SetParent(root.transform);
        int columns = Mathf.RoundToInt(Width / PieceSize);
        int rows = Mathf.RoundToInt(Height / PieceSize);
        for (int r = 0; r < rows; ++r)
        {
            for (int c = 0; c < columns; ++c)
            {
                GameObject piece = GameObject.CreatePrimitive(PrimitiveType.Cube);
                piece.name = $"Piece_{r}_{c}";
                piece.transform.SetParent(broken.transform);
                piece.transform.localPosition = new Vector3(
                    0.5f, PieceSize * (r + 0.5f), -Width * 0.5f + PieceSize * (c + 0.5f));
                piece.transform.localScale = new Vector3(1f, PieceSize, PieceSize) * 0.98f;
                Rigidbody body = piece.AddComponent<Rigidbody>();
                body.isKinematic = true;
            }
        }
        broken.SetActive(false);

        // Brancher les references du script (champs prives [SerializeField])
        SerializedObject so = new SerializedObject(set);
        so.FindProperty("intact").objectReferenceValue = intact;
        so.FindProperty("broken").objectReferenceValue = broken.transform;
        so.FindProperty("wallHeight").floatValue = Height;
        so.FindProperty("wallWidth").floatValue = Width;
        so.ApplyModifiedPropertiesWithoutUndo();

        string path = AssetDatabase.GenerateUniqueAssetPath($"{folder}/Wall_Greybox.prefab");
        PrefabUtility.SaveAsPrefabAsset(root, path);
        Object.DestroyImmediate(root);
        Debug.Log($"Mur greybox cree : {path}");
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
