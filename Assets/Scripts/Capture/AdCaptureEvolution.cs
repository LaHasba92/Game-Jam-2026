#if UNITY_EDITOR
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;

// Reprend l'évolution de Sami en fonction du temps du plan, sans modifier le lanceur équipé.
internal sealed class AdCaptureEvolution
{
    public const float RevealDuration = 0.5f;

    private const float SampleRate = 30f;
    private const int RayCount = 14;
    private const float RayMaxLength = 7f;
    private const float LightMaxIntensity = 25f;
    private static readonly Color AuraColor = new Color(1f, 0.92f, 0.55f);

    private readonly List<float> swaps = new List<float>();
    private ModelPose from;
    private ModelPose to;
    private Transform aura;
    private Transform[] rays;
    private Light glow;
    private Material whiteMaterial;
    private Material auraMaterial;
    private float spinDuration;

    public bool Begin(Transform anchor, GameObject fromModel, GameObject toModel, float duration)
    {
        Clear();
        if (anchor == null || fromModel == null || toModel == null || fromModel == toModel) return false;

        Shader unlit = Shader.Find("Universal Render Pipeline/Unlit");
        if (unlit == null) unlit = Shader.Find("Unlit/Color");
        if (unlit == null) return false;

        spinDuration = Mathf.Max(1f, duration - RevealDuration);
        whiteMaterial = CreateMaterial(unlit, Color.white);
        auraMaterial = CreateMaterial(unlit, AuraColor);
        from = new ModelPose(fromModel, whiteMaterial);
        to = new ModelPose(toModel, whiteMaterial);

        // Calcule les changements de silhouette sur la cadence d'export. On peut ensuite
        // afficher n'importe quelle image directement, sans dépendre des images précédentes.
        const float frameInterval = 1f / SampleRate;
        float swapTimer = 0f;
        for (int frame = 0; frame * frameInterval < spinDuration; ++frame)
        {
            float elapsed = frame * frameInterval;
            float progress = elapsed / spinDuration;
            swapTimer += frameInterval;
            if (progress <= 0.25f || swapTimer < Mathf.Lerp(0.4f, 0.04f, progress)) continue;

            swaps.Add(elapsed);
            swapTimer = 0f;
        }

        BuildAura(anchor);
        Sample(0f);
        return true;
    }

    public void Sample(float elapsed)
    {
        if (aura == null) return;

        elapsed = Mathf.Max(0f, elapsed);
        float progress = Mathf.Clamp01(elapsed / spinDuration);
        float easedSpin = progress * progress * progress / 3f;
        aura.localRotation = Quaternion.Euler(0f, spinDuration * (30f * progress + 370f * easedSpin), 0f);

        if (elapsed < spinDuration)
        {
            aura.gameObject.SetActive(true);
            float angle = spinDuration * (90f * progress + 1350f * easedSpin);
            int swapIndex = swaps.BinarySearch(elapsed);
            int swapCount = swapIndex >= 0 ? swapIndex + 1 : ~swapIndex;
            bool showNew = (swapCount & 1) != 0;
            from.Apply(!showNew, progress * 3f > 0.5f, angle, 1f);
            to.Apply(showNew, true, angle, 1f);

            float thickness = Mathf.Lerp(0.04f, 0.12f, progress);
            for (int i = 0; i < rays.Length; ++i)
            {
                float pulse = 0.75f + 0.25f * Mathf.Sin(elapsed * 18f + i * 1.7f);
                float appear = Mathf.Clamp01(progress * 2.5f - i / (float)rays.Length);
                SetRay(rays[i], RayMaxLength * progress * pulse * appear, thickness);
            }
            glow.intensity = LightMaxIntensity * progress * progress * (0.85f + 0.15f * Mathf.Sin(elapsed * 25f));
            return;
        }

        float revealTime = elapsed - spinDuration;
        float revealProgress = Mathf.Clamp01(revealTime / RevealDuration);
        from.Apply(false, false, 0f, 1f);
        to.Apply(true, revealProgress < 0.4f, 0f, Mathf.Lerp(1.3f, 1f, revealProgress));
        aura.gameObject.SetActive(revealProgress < 1f);
        if (revealProgress >= 1f) return;

        // Même atténuation cumulative que le flash d'origine, échantillonnée à 30 i/s.
        float fade = 1f;
        int revealFrame = Mathf.FloorToInt(revealTime * SampleRate);
        for (int frame = 1; frame <= revealFrame; ++frame)
            fade *= 1f - frame / (RevealDuration * SampleRate);

        foreach (Transform ray in rays)
            SetRay(ray, RayMaxLength * 1.4f * fade, 0.2f * fade);
        glow.intensity = LightMaxIntensity * 2f * fade;
    }

    public void Clear()
    {
        from?.Restore();
        to?.Restore();
        from = null;
        to = null;
        swaps.Clear();

        if (aura != null)
        {
            aura.gameObject.SetActive(false);
            Object.Destroy(aura.gameObject);
            aura = null;
        }
        if (whiteMaterial != null) Object.Destroy(whiteMaterial);
        if (auraMaterial != null) Object.Destroy(auraMaterial);
        whiteMaterial = null;
        auraMaterial = null;
    }

    private void BuildAura(Transform anchor)
    {
        aura = new GameObject("Aura de l’évolution").transform;
        aura.SetParent(anchor, false);
        aura.localPosition = Vector3.up * 1.2f;

        rays = new Transform[RayCount];
        for (int i = 0; i < rays.Length; ++i)
        {
            Transform pivot = new GameObject("Pivot du rayon").transform;
            pivot.SetParent(aura, false);
            pivot.localRotation = Quaternion.Euler((i % 3 - 1) * 25f, 360f * i / rays.Length, 0f);

            GameObject beam = GameObject.CreatePrimitive(PrimitiveType.Cube);
            beam.name = "Rayon";
            beam.transform.SetParent(pivot, false);
            Collider collider = beam.GetComponent<Collider>();
            collider.enabled = false;
            Object.Destroy(collider);
            Renderer renderer = beam.GetComponent<Renderer>();
            renderer.sharedMaterial = auraMaterial;
            renderer.shadowCastingMode = ShadowCastingMode.Off;
            rays[i] = beam.transform;
        }

        glow = new GameObject("Lueur de l’évolution").AddComponent<Light>();
        glow.transform.SetParent(aura, false);
        glow.type = LightType.Point;
        glow.color = AuraColor;
        glow.range = 10f;
    }

    private static void SetRay(Transform ray, float length, float thickness)
    {
        ray.localScale = new Vector3(thickness, thickness, Mathf.Max(0.001f, length));
        ray.localPosition = new Vector3(0f, 0f, length * 0.5f);
    }

    private static Material CreateMaterial(Shader shader, Color color)
    {
        var material = new Material(shader) { color = color };
        if (material.HasProperty("_BaseColor")) material.SetColor("_BaseColor", color);
        return material;
    }

    private sealed class ModelPose
    {
        private readonly GameObject model;
        private readonly bool wasActive;
        private readonly Vector3 position;
        private readonly Quaternion rotation;
        private readonly Vector3 scale;
        private readonly Vector3 pivot;
        private readonly Vector3 parentSpacePivot;
        private readonly Renderer[] renderers;
        private readonly Material[][] materials;
        private readonly Material[][] silhouettes;
        private bool isWhite;

        public ModelPose(GameObject model, Material white)
        {
            this.model = model;
            wasActive = model.activeSelf;
            Transform root = model.transform;
            position = root.localPosition;
            rotation = root.localRotation;
            scale = root.localScale;
            renderers = model.GetComponentsInChildren<Renderer>(true);
            materials = new Material[renderers.Length][];
            silhouettes = new Material[renderers.Length][];

            Bounds bounds = default;
            bool hasBounds = false;
            for (int i = 0; i < renderers.Length; ++i)
            {
                Renderer renderer = renderers[i];
                materials[i] = renderer.sharedMaterials;
                silhouettes[i] = new Material[materials[i].Length];
                for (int j = 0; j < silhouettes[i].Length; ++j)
                    silhouettes[i][j] = white;

                if (!renderer.enabled || !(renderer is MeshRenderer || renderer is SkinnedMeshRenderer)) continue;
                Matrix4x4 toRoot = root.worldToLocalMatrix * renderer.transform.localToWorldMatrix;
                Bounds source = renderer.localBounds;
                for (int corner = 0; corner < 8; ++corner)
                {
                    Vector3 localCorner = new Vector3(
                        (corner & 1) != 0 ? source.max.x : source.min.x,
                        (corner & 2) != 0 ? source.max.y : source.min.y,
                        (corner & 4) != 0 ? source.max.z : source.min.z);
                    Vector3 point = toRoot.MultiplyPoint3x4(localCorner);
                    if (hasBounds) bounds.Encapsulate(point);
                    else
                    {
                        bounds = new Bounds(point, Vector3.zero);
                        hasBounds = true;
                    }
                }
            }

            // Le mesh du canon est décalé de 11,5 m par rapport à son transform importé.
            // Pivoter autour du bas du mesh le garde devant la caméra pendant la rotation.
            pivot = hasBounds ? new Vector3(bounds.center.x, bounds.min.y, bounds.center.z) : Vector3.zero;
            parentSpacePivot = position + rotation * Vector3.Scale(scale, pivot);
        }

        public void Apply(bool visible, bool white, float angle, float size)
        {
            if (model == null) return;

            Transform root = model.transform;
            root.localRotation = Quaternion.Euler(0f, angle, 0f) * rotation;
            root.localScale = scale * size;
            root.localPosition = parentSpacePivot - root.localRotation * Vector3.Scale(root.localScale, pivot);
            SetSilhouette(white);
            if (model.activeSelf != visible) model.SetActive(visible);
        }

        public void Restore()
        {
            if (model == null) return;

            Transform root = model.transform;
            root.localPosition = position;
            root.localRotation = rotation;
            root.localScale = scale;
            SetSilhouette(false);
            if (model.activeSelf != wasActive) model.SetActive(wasActive);
        }

        private void SetSilhouette(bool white)
        {
            if (white == isWhite) return;

            for (int i = 0; i < renderers.Length; ++i)
                if (renderers[i] != null)
                    renderers[i].sharedMaterials = white ? silhouettes[i] : materials[i];
            isWhite = white;
        }
    }
}
#endif
