using UnityEngine;

// Applique le theme equipe : decor de tous les troncons, style de mur, ciel, lumiere et brouillard.
// A mettre dans le prefab Gameplay. Le theme equipe est garde dans les PlayerPrefs (comme le lanceur).
public class ThemeManager : MonoBehaviour
{
    public const string SelectedKey = "CanonPOW.SelectedTheme";

    [SerializeField] private ThemeDefinition[] themes;   // 0 = Ville, 1 = Foret... (ordre du troncon multi-themes)
    [SerializeField] private WallController wall;
    [SerializeField] private Light sun;                   // vide = trouvee toute seule dans la scene

    private int applied = -1;

    public int SelectedIndex => Mathf.Clamp(PlayerPrefs.GetInt(SelectedKey, 0), 0, Mathf.Max(0, themes.Length - 1));

    private void Update()
    {
        // Equipe dans la boutique ? on applique tout de suite (simple lecture d'un int)
        if (SelectedIndex != applied)
        {
            Apply(SelectedIndex);
        }
    }

    // Les troncons sont crees au demarrage par le WorldStreamer : on revient les mettre a jour une fois.
    private void Start()
    {
        applied = -1;
        if (sun == null)
        {
            // La lumiere est dans la scene (pas dans le prefab) : on la cherche toute seule
            sun = RenderSettings.sun;
            if (sun == null)
            {
                foreach (Light l in FindObjectsByType<Light>(FindObjectsSortMode.None))
                {
                    if (l.type == LightType.Directional) { sun = l; break; }
                }
            }
        }
    }

    public void Apply(int index)
    {
        applied = index;
        if (themes == null || themes.Length == 0 || themes[index] == null)
        {
            return;
        }
        ThemeDefinition theme = themes[index];

        foreach (ThemedChunk chunk in FindObjectsByType<ThemedChunk>(FindObjectsInactive.Include, FindObjectsSortMode.None))
        {
            chunk.Show(index);
        }

        if (wall != null)
        {
            wall.ThemeWallPrefab = theme.wallPrefab;   // pris en compte au prochain mur place
        }

        if (theme.skybox != null)
        {
            RenderSettings.skybox = theme.skybox;
        }
        RenderSettings.ambientLight = theme.ambientColor;
        RenderSettings.fog = theme.fog;
        RenderSettings.fogColor = theme.fogColor;
        RenderSettings.fogMode = FogMode.ExponentialSquared;
        RenderSettings.fogDensity = theme.fogDensity;
        if (sun != null)
        {
            sun.color = theme.sunColor;
            sun.intensity = theme.sunIntensity;
        }
        Debug.Log($"[Theme] {theme.displayName}");
    }
}
