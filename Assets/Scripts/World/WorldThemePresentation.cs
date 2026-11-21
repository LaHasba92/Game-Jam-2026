using UnityEngine;
using UnityEngine.Rendering;

// Le thème remplace le décor du tronçon, indépendamment de ses objets et de leur recyclage.
internal sealed class WorldThemePresentation
{
    private const int DistrictVariants = 3;
    private readonly Mesh[] districtMeshes = new Mesh[DistrictVariants];
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
        if (id == ThemeId.City) return;

        RenderSettings.fog = true;
        RenderSettings.fogMode = FogMode.Linear;
        if (id == ThemeId.Forest)
        {
            RenderSettings.fogColor = ThemePalette.Rgb(0x9baa9a);
            RenderSettings.fogStartDistance = 58f;
            RenderSettings.fogEndDistance = 180f;
            if (sun != null) sun.color = ThemePalette.Rgb(0xffe6b8);
            return;
        }

        if (neonSky == null) neonSky = Resources.Load<Material>("WorldThemes/DistrictNeonSky");
        if (neonSky != null) RenderSettings.skybox = neonSky;
        RenderSettings.fogColor = ThemePalette.Rgb(0x392a5c);
        RenderSettings.fogStartDistance = 42f;
        RenderSettings.fogEndDistance = 165f;
        RenderSettings.ambientMode = AmbientMode.Trilight;
        RenderSettings.ambientSkyColor = ThemePalette.Rgb(0x7165a0);
        RenderSettings.ambientEquatorColor = ThemePalette.Rgb(0x464581);
        RenderSettings.ambientGroundColor = ThemePalette.Rgb(0x2a2148);
        if (sun != null)
        {
            sun.color = ThemePalette.Rgb(0xb3caff);
            sun.intensity = Mathf.Max(0.55f, originalSunIntensity * 0.67f);
        }
    }

    public GameObject CreateVisual(Transform parent)
    {
        if (Selected != ThemeId.Cyberpunk)
            return Object.Instantiate(Selected == ThemeId.Forest ? forestPrefab : cityPrefab, parent);

        if (neonMaterial == null) neonMaterial = Resources.Load<Material>("WorldThemes/DistrictNeon");
        if (neonMaterial == null)
        {
            Debug.LogWarning("Thèmes : matériau du District Néon introuvable.");
            return Object.Instantiate(cityPrefab, parent);
        }

        var district = new GameObject("District Néon", typeof(MeshFilter), typeof(MeshRenderer));
        district.transform.SetParent(parent, false);
        var renderer = district.GetComponent<MeshRenderer>();
        renderer.sharedMaterial = neonMaterial;
        renderer.shadowCastingMode = ShadowCastingMode.Off;
        return district;
    }

    public void UpdateDistrict(GameObject visual, float start)
    {
        if (Selected != ThemeId.Cyberpunk || visual == null || neonMaterial == null) return;
        int ordinal = Mathf.RoundToInt(start / chunkLength);
        int index = ((ordinal % DistrictVariants) + DistrictVariants) % DistrictVariants;
        if (districtMeshes[index] == null)
            districtMeshes[index] = NeonDistrictMesh.Create(chunkLength, corridorWidth, index);
        visual.GetComponent<MeshFilter>().sharedMesh = districtMeshes[index];
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

    public void ReleaseMeshes()
    {
        foreach (Mesh mesh in districtMeshes)
            if (mesh != null) Object.Destroy(mesh);
    }
}
