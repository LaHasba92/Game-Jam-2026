using UnityEditor;
using UnityEngine;

// Outil d'editeur : repare les materiaux blancs ou roses des packs d'assets (arbres de Louis...).
// Selectionne un ou plusieurs DOSSIERS (ou materiaux) dans Project, puis
// menu CanonPOW > Reparer les materiaux (selection).
// Il passe les materiaux en URP Lit et recopie leur texture et leur couleur (_MainTex -> _BaseMap).
public static class MaterialFixer
{
    [MenuItem("CanonPOW/Reparer les materiaux (selection)")]
    private static void Fix()
    {
        Shader lit = Shader.Find("Universal Render Pipeline/Lit");
        if (lit == null)
        {
            Debug.LogError("URP Lit introuvable : URP est-il installe ?");
            return;
        }

        string[] folders = FolderSelection();
        if (folders.Length == 0)
        {
            EditorUtility.DisplayDialog("Reparer les materiaux",
                "Selectionne d'abord le dossier du pack (ex. Fantasy Forest Environment Free Sample) dans Project.", "OK");
            return;
        }
        string[] guids = AssetDatabase.FindAssets("t:Material", folders);
        int fixedCount = 0;
        foreach (string guid in guids)
        {
            string path = AssetDatabase.GUIDToAssetPath(guid);
            if (path.StartsWith("Assets/Resources/WorldThemes/", System.StringComparison.Ordinal))
                continue;
            Material m = AssetDatabase.LoadAssetAtPath<Material>(path);
            if (m == null || m.shader == null || ShouldSkip(m.shader.name))
            {
                continue;   // shader special (textes, interface, ciel, particules) : on n'y touche pas
            }
            bool isUrp = m.shader.name.StartsWith("Universal Render Pipeline");
            if (isUrp && m.HasProperty("_BaseMap") && m.GetTexture("_BaseMap") != null)
            {
                continue;   // deja en URP avec sa texture : rien a faire
            }
            // On recupere la texture et la couleur AVANT de changer de shader
            Texture tex = m.HasProperty("_BaseMap") ? m.GetTexture("_BaseMap") : null;
            if (tex == null && m.HasProperty("_MainTex")) tex = m.GetTexture("_MainTex");
            if (tex == null) tex = GuessTexture(path, m.name);   // texture perdue : on cherche une image au meme nom
            Color color = m.HasProperty("_BaseColor") ? m.GetColor("_BaseColor")
                        : m.HasProperty("_Color") ? m.GetColor("_Color") : Color.white;
            bool cutout = m.shader.name.ToLower().Contains("leaf") || m.shader.name.ToLower().Contains("cutout")
                          || m.shader.name.ToLower().Contains("foliage") || m.name.ToLower().Contains("leaf");
            Texture normal = m.HasProperty("_BumpMap") ? m.GetTexture("_BumpMap") : null;

            Undo.RecordObject(m, "Reparer materiau");
            m.shader = lit;
            m.SetTexture("_BaseMap", tex);
            m.SetColor("_BaseColor", color);
            if (normal != null)
            {
                m.SetTexture("_BumpMap", normal);
                m.EnableKeyword("_NORMALMAP");
            }
            if (cutout)
            {
                // Feuillage : les zones transparentes de la texture sont decoupees
                m.SetFloat("_AlphaClip", 1f);
                m.SetFloat("_Cutoff", 0.5f);
                m.EnableKeyword("_ALPHATEST_ON");
                m.SetFloat("_Cull", 0f);   // visible des deux cotes
            }
            EditorUtility.SetDirty(m);
            ++fixedCount;
            Debug.Log($"Repare : {path}", m);
        }
        AssetDatabase.SaveAssets();
        Debug.Log($"{fixedCount} materiau(x) passe(s) en URP Lit.");
    }

    // Les dossiers selectionnes (ou le dossier des materiaux selectionnes)
    private static string[] FolderSelection()
    {
        System.Collections.Generic.List<string> folders = new System.Collections.Generic.List<string>();
        foreach (Object o in Selection.objects)
        {
            string path = AssetDatabase.GetAssetPath(o);
            if (AssetDatabase.IsValidFolder(path)) folders.Add(path);
            else if (!string.IsNullOrEmpty(path)) folders.Add(System.IO.Path.GetDirectoryName(path).Replace('\\', '/'));
        }
        return folders.ToArray();
    }

    // Cherche, pres du materiau, une texture dont le nom ressemble au sien (ex. « Leaves » -> « Leaves_Albedo »).
    private static Texture GuessTexture(string materialPath, string materialName)
    {
        string folder = System.IO.Path.GetDirectoryName(System.IO.Path.GetDirectoryName(materialPath)).Replace('\\', '/');
        string key = materialName.ToLower().Replace("_mat", "").Replace("mat_", "").Replace(" ", "");
        foreach (string guid in AssetDatabase.FindAssets("t:Texture2D", new[] { folder }))
        {
            string p = AssetDatabase.GUIDToAssetPath(guid);
            string n = System.IO.Path.GetFileNameWithoutExtension(p).ToLower().Replace(" ", "");
            if (n.Contains(key) && !n.Contains("normal") && !n.Contains("_n") && !n.Contains("rough") && !n.Contains("metal"))
            {
                return AssetDatabase.LoadAssetAtPath<Texture>(p);
            }
        }
        return null;
    }

    private static bool ShouldSkip(string shader)
    {
        string s = shader.ToLower();
        return s.StartsWith("canonpow/") || s.Contains("textmeshpro") || s.StartsWith("ui/")
            || s.Contains("sprite") || s.Contains("skybox") || s.Contains("particle") || s.StartsWith("shader graphs");
    }
}
