using UnityEditor;
using UnityEngine;

// Outil d'editeur : cree le troncon de sol et les 5 objets de la piste en greybox.
// Menu : CanonPOW > Creer le sol et les objets greybox.
public static class GreyboxWorldCreator
{
    private const string Folder = "Assets/Prefabs/World";

    [MenuItem("CanonPOW/Creer le sol et les objets greybox")]
    private static void Create()
    {
        EnsureFolder(Folder);

        // Troncon de 50 m, pivot au debut (x = 0), un peu plus large que le couloir de 6 m
        GameObject chunk = new GameObject("Chunk_Greybox");
        GameObject ground = Primitive(PrimitiveType.Cube, "Ground", chunk.transform,
            new Vector3(25f, -0.1f, 0f), new Vector3(49.8f, 0.2f, 10f), new Color(0.55f, 0.6f, 0.5f));
        Object.DestroyImmediate(ground.GetComponent<Collider>()); // le boulet d'Andy teste le sol par calcul (y <= 0)
        Save(chunk, "Chunk_Greybox");

        MakeObject(GroundObjectType.Trampoline, PrimitiveType.Cylinder, new Vector3(0f, 0.1f, 0f),
            new Vector3(2f, 0.1f, 2f), Color.cyan, 1f);
        MakeObject(GroundObjectType.Coin, PrimitiveType.Cylinder, Vector3.zero,
            new Vector3(0.6f, 0.05f, 0.6f), Color.yellow, 0.5f, Quaternion.Euler(0f, 0f, 90f)); // face vers le lanceur (axe X)
        MakeObject(GroundObjectType.Booster, PrimitiveType.Cube, new Vector3(0f, 0.05f, 0f),
            new Vector3(2f, 0.1f, 1.5f), new Color(1f, 0.5f, 0f), 1f);
        MakeObject(GroundObjectType.Balloon, PrimitiveType.Sphere, Vector3.zero,
            Vector3.one * 1.2f, Color.red, 0.8f);
        MakeObject(GroundObjectType.Sticky, PrimitiveType.Cylinder, new Vector3(0f, 0.03f, 0f),
            new Vector3(3f, 0.03f, 3f), new Color(0.2f, 0.35f, 0.1f), 1.5f);

        AssetDatabase.SaveAssets();
        Debug.Log($"Sol et objets greybox crees dans {Folder}");
    }

    private static void MakeObject(GroundObjectType type, PrimitiveType shape, Vector3 offset,
                                   Vector3 scale, Color color, float radius, Quaternion? rotation = null)
    {
        GameObject root = new GameObject(type.ToString());
        GroundObject obj = root.AddComponent<GroundObject>();
        obj.type = type;
        obj.radius = radius;
        GameObject visual = Primitive(shape, "Visual", root.transform, offset, scale, color);
        visual.transform.localRotation = rotation ?? Quaternion.identity;
        Object.DestroyImmediate(visual.GetComponent<Collider>()); // detection par calcul, pas par la physique
        Save(root, type.ToString());
    }

    private static GameObject Primitive(PrimitiveType shape, string name, Transform parent,
                                        Vector3 position, Vector3 scale, Color color)
    {
        GameObject go = GameObject.CreatePrimitive(shape);
        go.name = name;
        go.transform.SetParent(parent);
        go.transform.localPosition = position;
        go.transform.localScale = scale;
        Renderer renderer = go.GetComponent<Renderer>();
        Material material = new Material(renderer.sharedMaterial) { color = color };
        AssetDatabase.CreateAsset(material, AssetDatabase.GenerateUniqueAssetPath($"{Folder}/{parent.name}_{name}.mat"));
        renderer.sharedMaterial = material;
        return go;
    }

    private static void Save(GameObject root, string name)
    {
        PrefabUtility.SaveAsPrefabAsset(root, AssetDatabase.GenerateUniqueAssetPath($"{Folder}/{name}.prefab"));
        Object.DestroyImmediate(root);
    }

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
