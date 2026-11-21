using UnityEditor;
using UnityEngine;

// Outil d'editeur : assemble les troncons Ville et Foret dans un seul troncon multi-themes.
// Menu : CanonPOW > Creer le troncon multi-themes. Resultat : Assets/Prefabs/World/Chunk_Themes.
// Ensuite, mettre Chunk_Themes dans le « Chunk Prefab » du WorldStreamer.
public static class ThemedChunkCreator
{
    // L'ordre ici = l'ordre des themes dans le ThemeManager
    private static readonly string[] Sources =
    {
        "Assets/Prefabs/World/Chunk_Ville.prefab",
        "Assets/Prefabs/World/Chunk_Foret.prefab",
    };

    [MenuItem("CanonPOW/Creer le troncon multi-themes")]
    private static void Create()
    {
        GameObject root = new GameObject("Chunk_Themes");
        GameObject[] roots = new GameObject[Sources.Length];
        for (int i = 0; i < Sources.Length; ++i)
        {
            GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(Sources[i]);
            if (prefab == null)
            {
                Debug.LogWarning($"Troncon introuvable : {Sources[i]} (theme {i} vide)");
                continue;
            }
            // Instance liee au prefab : si on refait Chunk_Ville, Chunk_Themes suit
            GameObject child = (GameObject)PrefabUtility.InstantiatePrefab(prefab, root.transform);
            child.transform.localPosition = Vector3.zero;
            child.SetActive(i == 0);
            roots[i] = child;
        }

        ThemedChunk themed = root.AddComponent<ThemedChunk>();
        SerializedObject so = new SerializedObject(themed);
        SerializedProperty list = so.FindProperty("themeRoots");
        list.arraySize = roots.Length;
        for (int i = 0; i < roots.Length; ++i) list.GetArrayElementAtIndex(i).objectReferenceValue = roots[i];
        so.ApplyModifiedPropertiesWithoutUndo();

        string path = AssetDatabase.GenerateUniqueAssetPath("Assets/Prefabs/World/Chunk_Themes.prefab");
        PrefabUtility.SaveAsPrefabAsset(root, path);
        Object.DestroyImmediate(root);
        Selection.activeObject = AssetDatabase.LoadAssetAtPath<GameObject>(path);
        Debug.Log($"Troncon multi-themes cree : {path}");
    }
}
