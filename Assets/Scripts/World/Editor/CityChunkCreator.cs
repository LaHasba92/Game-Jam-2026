using UnityEditor;
using UnityEngine;

// Outil d'editeur : fabrique le troncon « ville » (route, voies, trottoirs, lampadaires, immeubles),
// uniquement avec des cubes et quelques materiaux partages : zero asset a importer, tres leger.
// Menu : CanonPOW > Creer le troncon ville. Resultat : Assets/Prefabs/World/Chunk_Ville.
// Le troncon fait 50 m (axe X). La route fait 6 m (= le couloir du boulet et la largeur des murs).
public static class CityChunkCreator
{
    private const float Length = 50f;
    private const float RoadHalf = 3f;          // route de 6 m : la balle ne sort pas de la route
    private const float SidewalkWidth = 2.5f;   // trottoir de chaque cote
    private const float SidewalkHeight = 0.2f;
    private const float LampSpacing = 10f;      // un lampadaire tous les 10 m, des deux cotes, en quinconce
    private const int Variants = 4;             // 4 rangees d'immeubles differentes, tirees au hasard
    private const string Folder = "Assets/Prefabs/World/City";

    [MenuItem("CanonPOW/Creer le troncon ville")]
    private static void Create()
    {
        EnsureFolder(Folder);
        Material asphalt = Mat("M_Asphalte", new Color(0.18f, 0.19f, 0.21f));
        Material line = Mat("M_Marquage", new Color(0.95f, 0.95f, 0.9f));
        Material sidewalk = Mat("M_Trottoir", new Color(0.62f, 0.62f, 0.6f));
        Material curb = Mat("M_Bordure", new Color(0.8f, 0.8f, 0.78f));
        Material pole = Mat("M_Lampadaire", new Color(0.25f, 0.27f, 0.3f));
        Material light = Mat("M_Lumiere", new Color(1f, 0.9f, 0.6f), emissive: true);
        Material window = Mat("M_Fenetres", new Color(0.35f, 0.5f, 0.65f));
        Material[] facades =
        {
            Mat("M_Facade_Creme", new Color(0.93f, 0.85f, 0.7f)),
            Mat("M_Facade_Brique", new Color(0.75f, 0.42f, 0.35f)),
            Mat("M_Facade_Bleu", new Color(0.55f, 0.68f, 0.8f)),
            Mat("M_Facade_Vert", new Color(0.6f, 0.75f, 0.6f)),
            Mat("M_Facade_Gris", new Color(0.7f, 0.7f, 0.72f)),
        };

        GameObject root = new GameObject("Chunk_Ville");

        // Route + voies (pointilles au centre, ligne continue sur les bords)
        Box("Route", root.transform, new Vector3(Length * 0.5f, -0.05f, 0f), new Vector3(Length, 0.1f, RoadHalf * 2f), asphalt);
        Transform lines = new GameObject("Marquages").transform;
        lines.SetParent(root.transform, false);
        for (float x = 1.25f; x < Length; x += 5f)   // tiret 2,5 m + espace 2,5 m : le motif se raccorde d'un troncon a l'autre
        {
            Box("Tiret", lines, new Vector3(x, 0.005f, 0f), new Vector3(2.5f, 0.02f, 0.15f), line);
        }
        Box("BordGauche", lines, new Vector3(Length * 0.5f, 0.005f, RoadHalf - 0.2f), new Vector3(Length, 0.02f, 0.12f), line);
        Box("BordDroit", lines, new Vector3(Length * 0.5f, 0.005f, -RoadHalf + 0.2f), new Vector3(Length, 0.02f, 0.12f), line);

        // Trottoirs + bordures, des deux cotes
        foreach (float side in new[] { 1f, -1f })
        {
            float z = side * (RoadHalf + SidewalkWidth * 0.5f);
            Box(side > 0 ? "TrottoirGauche" : "TrottoirDroit", root.transform,
                new Vector3(Length * 0.5f, SidewalkHeight * 0.5f - 0.1f, z), new Vector3(Length, SidewalkHeight + 0.1f, SidewalkWidth), sidewalk);
            Box("Bordure", root.transform, new Vector3(Length * 0.5f, SidewalkHeight * 0.5f, side * (RoadHalf + 0.1f)),
                new Vector3(Length, SidewalkHeight + 0.02f, 0.2f), curb);
        }

        // Lampadaires : tous les 10 m, gauche puis droite en quinconce (meme motif sur tous les troncons)
        Transform lamps = new GameObject("Lampadaires").transform;
        lamps.SetParent(root.transform, false);
        for (int i = 0; i < Mathf.RoundToInt(Length / LampSpacing); ++i)
        {
            float side = i % 2 == 0 ? 1f : -1f;
            float x = LampSpacing * (i + 0.5f);
            float z = side * (RoadHalf + 0.5f);
            Transform lamp = new GameObject("Lampadaire").transform;
            lamp.SetParent(lamps, false);
            lamp.localPosition = new Vector3(x, SidewalkHeight, z);
            Box("Mat", lamp, new Vector3(0f, 2.75f, 0f), new Vector3(0.18f, 5.5f, 0.18f), pole);
            Box("Bras", lamp, new Vector3(0f, 5.45f, -side * 0.6f), new Vector3(0.12f, 0.12f, 1.3f), pole);
            Box("Lampe", lamp, new Vector3(0f, 5.3f, -side * 1.15f), new Vector3(0.45f, 0.18f, 0.35f), light);
        }

        // Immeubles : plusieurs variantes, tirees au hasard par ChunkDecor a chaque recyclage
        GameObject[] variants = new GameObject[Variants];
        for (int v = 0; v < Variants; ++v)
        {
            Transform variant = new GameObject($"Immeubles_{(char)('A' + v)}").transform;
            variant.SetParent(root.transform, false);
            System.Random random = new System.Random(100 + v * 31);
            foreach (float side in new[] { 1f, -1f })
            {
                float x = 0f;
                while (x < Length - 1f)
                {
                    float width = Mathf.Min(Range(random, 7f, 13f), Length - x);
                    float depth = Range(random, 9f, 15f);
                    float height = Range(random, 10f, 38f);
                    float zFront = side * (RoadHalf + SidewalkWidth);
                    Material facade = facades[random.Next(facades.Length)];
                    Transform building = Box("Immeuble", variant,
                        new Vector3(x + width * 0.5f, height * 0.5f, zFront + side * depth * 0.5f),
                        new Vector3(width - 0.4f, height, depth), facade).transform;
                    // Bandes de fenetres sur la facade cote route : quelques cubes plats, pas de texture
                    for (float y = 3f; y < height - 1.5f; y += 3.2f)
                    {
                        Box("Fenetres", building.parent,
                            new Vector3(x + width * 0.5f, y, zFront + side * -0.02f),
                            new Vector3(width - 1.6f, 1.3f, 0.06f), window);
                    }
                    x += width;
                }
            }
            variants[v] = variant.gameObject;
        }

        ChunkDecor decor = root.AddComponent<ChunkDecor>();
        SerializedObject so = new SerializedObject(decor);
        SerializedProperty list = so.FindProperty("variants");
        list.arraySize = Variants;
        for (int i = 0; i < Variants; ++i) list.GetArrayElementAtIndex(i).objectReferenceValue = variants[i];
        so.FindProperty("mirrorRandomly").boolValue = false;   // les fenetres doivent rester cote route
        so.ApplyModifiedPropertiesWithoutUndo();

        string path = AssetDatabase.GenerateUniqueAssetPath("Assets/Prefabs/World/Chunk_Ville.prefab");
        PrefabUtility.SaveAsPrefabAsset(root, path);
        Object.DestroyImmediate(root);
        AssetDatabase.SaveAssets();
        Selection.activeObject = AssetDatabase.LoadAssetAtPath<GameObject>(path);
        Debug.Log($"Troncon ville cree : {path}");
    }

    private static GameObject Box(string name, Transform parent, Vector3 position, Vector3 scale, Material material)
    {
        GameObject go = GameObject.CreatePrimitive(PrimitiveType.Cube);
        go.name = name;
        Object.DestroyImmediate(go.GetComponent<Collider>());   // decor seulement : le boulet est calcule par notre code
        go.transform.SetParent(parent, false);
        go.transform.localPosition = position;
        go.transform.localScale = scale;
        go.GetComponent<Renderer>().sharedMaterial = material;
        go.isStatic = false;
        return go;
    }

    // Un materiau URP Lit partage, cree une seule fois (moins de materiaux = moins d'appels de rendu)
    private static Material Mat(string name, Color color, bool emissive = false)
    {
        string path = $"{Folder}/{name}.mat";
        Material m = AssetDatabase.LoadAssetAtPath<Material>(path);
        if (m == null)
        {
            m = new Material(Shader.Find("Universal Render Pipeline/Lit"));
            AssetDatabase.CreateAsset(m, path);
        }
        m.SetColor("_BaseColor", color);
        m.SetFloat("_Smoothness", 0.15f);
        if (emissive)
        {
            m.EnableKeyword("_EMISSION");
            m.SetColor("_EmissionColor", color * 2f);
        }
        m.enableInstancing = true;
        EditorUtility.SetDirty(m);
        return m;
    }

    private static float Range(System.Random r, float min, float max) => min + (float)r.NextDouble() * (max - min);

    private static void EnsureFolder(string path)
    {
        if (AssetDatabase.IsValidFolder(path)) return;
        string parent = System.IO.Path.GetDirectoryName(path).Replace('\\', '/');
        EnsureFolder(parent);
        AssetDatabase.CreateFolder(parent, System.IO.Path.GetFileName(path));
    }
}
