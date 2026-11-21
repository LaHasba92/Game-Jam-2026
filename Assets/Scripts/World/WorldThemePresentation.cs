using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;

// Le thème remplace le décor du tronçon, indépendamment de ses objets et de leur recyclage.
internal sealed class WorldThemePresentation
{
    private const int Variants = 3;
    private static readonly int LandscapeProperty = Shader.PropertyToID("_Landscape");

    private readonly Dictionary<(ThemeId theme, int variant), Mesh> meshes = new Dictionary<(ThemeId, int), Mesh>();
    private readonly float chunkLength;
    private readonly float corridorWidth;
    private readonly GameObject cityPrefab;
    private readonly GameObject forestPrefab;
    private readonly Light sun;
    private readonly Color originalSunColor;
    private readonly float originalSunIntensity;
    private readonly Material originalSky;
    private readonly AmbientMode originalAmbientMode;
    private readonly Color originalAmbientSky;
    private readonly Color originalAmbientEquator;
    private readonly Color originalAmbientGround;
    private readonly bool originalFog;
    private readonly FogMode originalFogMode;
    private readonly Color originalFogColor;
    private readonly float originalFogStart;
    private readonly float originalFogEnd;

    private Material neonMaterial;
    private Material neonSky;
    private Material landscapeMaterial;
    private Material landscapeSky;
    private Material currentMaterial;

    public ThemeId Selected { get; private set; }

    public WorldThemePresentation(GameObject cityPrefab, GameObject forestPrefab, float chunkLength, float corridorWidth)
    {
        this.cityPrefab = cityPrefab;
        this.forestPrefab = forestPrefab;
        this.chunkLength = chunkLength;
        this.corridorWidth = corridorWidth;

        sun = RenderSettings.sun;
        if (sun == null)
        {
            foreach (Light light in Object.FindObjectsByType<Light>(FindObjectsSortMode.None))
            {
                if (light.type != LightType.Directional) continue;
                sun = light;
                break;
            }
        }
        if (sun != null)
        {
            originalSunColor = sun.color;
            originalSunIntensity = sun.intensity;
        }

        originalSky = RenderSettings.skybox;
        originalAmbientMode = RenderSettings.ambientMode;
        originalAmbientSky = RenderSettings.ambientSkyColor;
        originalAmbientEquator = RenderSettings.ambientEquatorColor;
        originalAmbientGround = RenderSettings.ambientGroundColor;
        originalFog = RenderSettings.fog;
        originalFogMode = RenderSettings.fogMode;
        originalFogColor = RenderSettings.fogColor;
        originalFogStart = RenderSettings.fogStartDistance;
        originalFogEnd = RenderSettings.fogEndDistance;
    }

    public void Apply(ThemeId id)
    {
        Selected = id;
        RestoreSkyAndLight();
        currentMaterial = null;

        switch (id)
        {
            case ThemeId.Forest:
                SetLandscape(0f);
                SetLighting(0x9bb0a3, 31f, 158f, 0xffe6b8, 1.08f, 0xced9bb, 0x9cb29a, 0x757965);
                break;
            case ThemeId.Beach:
                SetLandscape(1f);
                SetLighting(0xa7d4de, 56f, 198f, 0xfff0d8, 1.1f, 0xd1edf2, 0xb8d6d3, 0xc9b79c);
                break;
            case ThemeId.Moon:
                SetLandscape(2f);
                SetLighting(0x2b3953, 41f, 169f, 0xd2e5ff, 0.97f, 0x798eaf, 0x5e6c84, 0x3e475a);
                break;
            case ThemeId.Cyberpunk:
                if (neonMaterial == null) neonMaterial = Resources.Load<Material>("WorldThemes/DistrictNeon");
                if (neonSky == null) neonSky = Resources.Load<Material>("WorldThemes/DistrictNeonSky");
                currentMaterial = neonMaterial;
                if (neonSky != null) RenderSettings.skybox = neonSky;
                SetLighting(0x392a5c, 42f, 165f, 0xb3caff, 0.67f, 0x7165a0, 0x464581, 0x2a2148);
                break;
        }
    }

    private void SetLandscape(float landscape)
    {
        if (landscapeMaterial == null) landscapeMaterial = CreateMaterial("WorldThemes/Landscape");
        if (landscapeSky == null) landscapeSky = CreateMaterial("WorldThemes/LandscapeSky");
        currentMaterial = landscapeMaterial;
        if (landscapeMaterial != null) landscapeMaterial.SetFloat(LandscapeProperty, landscape);
        if (landscapeSky != null)
        {
            landscapeSky.SetFloat(LandscapeProperty, landscape);
            RenderSettings.skybox = landscapeSky;
        }
    }

    private static Material CreateMaterial(string path)
    {
        Material source = Resources.Load<Material>(path);
        return source == null ? null : new Material(source);
    }

    private void SetLighting(int fog, float near, float far, int light, float intensity, int sky, int equator, int ground)
    {
        RenderSettings.fog = true;
        RenderSettings.fogMode = FogMode.Linear;
        RenderSettings.fogColor = ThemePalette.Rgb(fog);
        RenderSettings.fogStartDistance = near;
        RenderSettings.fogEndDistance = far;
        RenderSettings.ambientMode = AmbientMode.Trilight;
        RenderSettings.ambientSkyColor = ThemePalette.Rgb(sky);
        RenderSettings.ambientEquatorColor = ThemePalette.Rgb(equator);
        RenderSettings.ambientGroundColor = ThemePalette.Rgb(ground);
        if (sun != null)
        {
            sun.color = ThemePalette.Rgb(light);
            sun.intensity = Mathf.Max(0.55f, originalSunIntensity * intensity);
        }
    }

    public GameObject CreateVisual(Transform parent)
    {
        if (Selected == ThemeId.City)
            return Object.Instantiate(cityPrefab, parent);

        if (currentMaterial == null)
        {
            Debug.LogWarning($"Thèmes : matériau de {Selected} introuvable.");
            return Object.Instantiate(Selected == ThemeId.Forest ? forestPrefab : cityPrefab, parent);
        }

        var visual = new GameObject(Selected.ToString(), typeof(MeshFilter), typeof(MeshRenderer));
        visual.transform.SetParent(parent, false);
        var renderer = visual.GetComponent<MeshRenderer>();
        renderer.sharedMaterial = currentMaterial;
        renderer.shadowCastingMode = ShadowCastingMode.Off;
        return visual;
    }

    public void UpdateVisual(GameObject visual, float start)
    {
        if (currentMaterial == null || visual == null) return;
        int ordinal = Mathf.RoundToInt(start / chunkLength);
        int variant = ((ordinal % Variants) + Variants) % Variants;
        var key = (Selected, variant);
        if (!meshes.TryGetValue(key, out Mesh mesh))
        {
            mesh = Selected == ThemeId.Cyberpunk
                ? NeonDistrictMesh.Create(chunkLength, corridorWidth, variant)
                : LandscapeChunkMesh.Create(Selected, chunkLength, corridorWidth, variant);
            meshes.Add(key, mesh);
        }
        visual.GetComponent<MeshFilter>().sharedMesh = mesh;
    }

    public void RestoreSkyAndLight()
    {
        RenderSettings.skybox = originalSky;
        RenderSettings.ambientMode = originalAmbientMode;
        RenderSettings.ambientSkyColor = originalAmbientSky;
        RenderSettings.ambientEquatorColor = originalAmbientEquator;
        RenderSettings.ambientGroundColor = originalAmbientGround;
        RenderSettings.fog = originalFog;
        RenderSettings.fogMode = originalFogMode;
        RenderSettings.fogColor = originalFogColor;
        RenderSettings.fogStartDistance = originalFogStart;
        RenderSettings.fogEndDistance = originalFogEnd;
        if (sun != null)
        {
            sun.color = originalSunColor;
            sun.intensity = originalSunIntensity;
        }
    }

    public void Release()
    {
        foreach (Mesh mesh in meshes.Values)
            Object.Destroy(mesh);
        if (landscapeMaterial != null) Object.Destroy(landscapeMaterial);
        if (landscapeSky != null) Object.Destroy(landscapeSky);
    }
}
