using UnityEngine;

// Maquettes de jeu livrées avec le prototype. Les modèles d'art peuvent les remplacer.
internal static class JamGeometry
{
    private static readonly int BaseColor = Shader.PropertyToID("_BaseColor");
    private static readonly int ColorId = Shader.PropertyToID("_Color");
    private static readonly MaterialPropertyBlock block = new MaterialPropertyBlock();

    public static Transform Part(Transform parent, string name, PrimitiveType type, Vector3 position,
        Vector3 scale, Color color)
    {
        var obj = GameObject.CreatePrimitive(type);
        obj.name = name;
        Collider collider = obj.GetComponent<Collider>();
        if (collider != null) { collider.enabled = false; Object.Destroy(collider); }
        obj.transform.SetParent(parent, false);
        obj.transform.localPosition = position;
        obj.transform.localScale = scale;
        Renderer renderer = obj.GetComponent<Renderer>();
        if (AimTuning.Current.greyboxMaterial != null) renderer.sharedMaterial = AimTuning.Current.greyboxMaterial;
        Tint(renderer, color);
        return obj.transform;
    }

    public static void Tint(Renderer renderer, Color color)
    {
        renderer.GetPropertyBlock(block);
        block.SetColor(BaseColor, color);
        block.SetColor(ColorId, color);
        renderer.SetPropertyBlock(block);
    }

    public static Transform Group(Transform parent, string name)
    {
        var obj = new GameObject(name);
        obj.transform.SetParent(parent, false);
        return obj.transform;
    }
}
