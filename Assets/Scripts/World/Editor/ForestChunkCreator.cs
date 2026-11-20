using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using UnityEngine.SceneManagement;

// Outil d'editeur : fabrique le troncon « foret » a partir de la scene de Louis (newscene).
// 1. Ouvre la scene de Louis.  2. Menu CanonPOW > Creer le troncon foret (scene de Louis).
// Il copie ses zones de decor (DecorationZoneLeftN / DecorationZoneRightN), les range de chaque cote
// de la piste dans un troncon de 50 m, enleve les colliders, et cree le prefab Chunk_Foret.
public static class ForestChunkCreator
{
    private const float ChunkLength = 50f;
    private const float TrackHalfWidth = 5f;     // la piste fait 6 m : rien a moins de 5 m du centre
    private const float GroundWidth = 60f;
    private const string Folder = "Assets/Prefabs/World";

    [MenuItem("CanonPOW/Creer le troncon foret (scene de Louis)")]
    private static void Create()
    {
        Scene scene = SceneManager.GetActiveScene();
        List<GameObject> lefts = FindAll(scene, "DecorationZoneLeft");
        List<GameObject> rights = FindAll(scene, "DecorationZoneRight");
        if (lefts.Count == 0 && rights.Count == 0)
        {
            EditorUtility.DisplayDialog("Troncon foret",
                "Aucune zone « DecorationZoneLeft / Right » dans la scene ouverte.\nOuvre d'abord la scene de Louis (newscene).", "OK");
            return;
        }

        GameObject root = new GameObject("Chunk_Foret");

        // Le sol : un cube plat de 50 m, avec le materiau du sol de Louis s'il existe
        GameObject ground = GameObject.CreatePrimitive(PrimitiveType.Cube);
        ground.name = "Ground";
        ground.transform.SetParent(root.transform);
        ground.transform.localPosition = new Vector3(ChunkLength * 0.5f, -0.1f, 0f);
        ground.transform.localScale = new Vector3(ChunkLength - 0.2f, 0.2f, GroundWidth);
        Object.DestroyImmediate(ground.GetComponent<Collider>());
        Renderer louisGround = FindRenderer(scene, "Ground");
        if (louisGround != null)
        {
            ground.GetComponent<Renderer>().sharedMaterial = louisGround.sharedMaterial;
        }

        // Une variante par paire de zones (gauche n + droite n)
        int count = Mathf.Max(lefts.Count, rights.Count);
        List<GameObject> variants = new List<GameObject>();
        for (int i = 0; i < count; ++i)
        {
            GameObject variant = new GameObject($"Decor_{(char)('A' + i)}");
            variant.transform.SetParent(root.transform, false);
            if (i < lefts.Count) PlaceZone(lefts[i], variant.transform, +1f);
            if (i < rights.Count) PlaceZone(rights[i], variant.transform, -1f);
            variants.Add(variant);
        }

        ChunkDecor decor = root.AddComponent<ChunkDecor>();
        SerializedObject so = new SerializedObject(decor);
        SerializedProperty list = so.FindProperty("variants");
        list.arraySize = variants.Count;
        for (int i = 0; i < variants.Count; ++i)
        {
            list.GetArrayElementAtIndex(i).objectReferenceValue = variants[i];
        }
        so.ApplyModifiedPropertiesWithoutUndo();

        EnsureFolder(Folder);
        string path = AssetDatabase.GenerateUniqueAssetPath($"{Folder}/Chunk_Foret.prefab");
        PrefabUtility.SaveAsPrefabAsset(root, path);
        Object.DestroyImmediate(root);
        Debug.Log($"Troncon foret cree : {path} ({variants.Count} variantes de decor)");
        Selection.activeObject = AssetDatabase.LoadAssetAtPath<GameObject>(path);
    }

    // Copie une zone de Louis et la range d'un cote de la piste (side = +1 gauche, -1 droite).
    private static void PlaceZone(GameObject zone, Transform parent, float side)
    {
        GameObject copy = Object.Instantiate(zone);
        copy.name = zone.name;
        foreach (Collider c in copy.GetComponentsInChildren<Collider>(true))
        {
            Object.DestroyImmediate(c);   // le boulet est calcule par notre code : pas de collider
        }

        Bounds bounds = BoundsOf(copy);
        // Trop long pour 50 m ? on le reduit (sans jamais l'agrandir)
        float scale = Mathf.Min(1f, ChunkLength / Mathf.Max(0.01f, bounds.size.x));
        copy.transform.localScale *= scale;
        bounds = BoundsOf(copy);

        // Bas du decor au sol (y = 0), centre au milieu du troncon, bord interieur a 5 m de la piste
        Vector3 offset = new Vector3(
            ChunkLength * 0.5f - bounds.center.x,
            -bounds.min.y,
            side > 0 ? TrackHalfWidth - bounds.min.z : -TrackHalfWidth - bounds.max.z);
        copy.transform.position += offset;
        copy.transform.SetParent(parent, true);
    }

    private static Bounds BoundsOf(GameObject go)
    {
        Renderer[] renderers = go.GetComponentsInChildren<Renderer>(true);
        if (renderers.Length == 0)
        {
            return new Bounds(go.transform.position, Vector3.one);
        }
        Bounds b = renderers[0].bounds;
        foreach (Renderer r in renderers)
        {
            b.Encapsulate(r.bounds);
        }
        return b;
    }

    private static List<GameObject> FindAll(Scene scene, string prefix)
    {
        List<GameObject> found = new List<GameObject>();
        foreach (GameObject root in scene.GetRootGameObjects())
        {
            foreach (Transform t in root.GetComponentsInChildren<Transform>(true))
            {
                if (t.name.StartsWith(prefix) && !found.Contains(t.gameObject))
                {
                    found.Add(t.gameObject);
                }
            }
        }
        found.Sort((a, b) => string.CompareOrdinal(a.name, b.name));
        return found;
    }

    private static Renderer FindRenderer(Scene scene, string prefix)
    {
        foreach (GameObject root in scene.GetRootGameObjects())
        {
            foreach (Renderer r in root.GetComponentsInChildren<Renderer>(true))
            {
                if (r.name.StartsWith(prefix)) return r;
            }
        }
        return null;
    }

    private static void EnsureFolder(string path)
    {
        if (AssetDatabase.IsValidFolder(path)) return;
        string parent = System.IO.Path.GetDirectoryName(path).Replace('\\', '/');
        EnsureFolder(parent);
        AssetDatabase.CreateFolder(parent, System.IO.Path.GetFileName(path));
    }
}
