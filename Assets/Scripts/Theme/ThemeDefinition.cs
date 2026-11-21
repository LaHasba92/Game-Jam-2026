using UnityEngine;

// Un theme = un univers visuel : decor de la piste, style de mur, ciel et lumiere.
// Clic droit > Create > CanonPOW > Theme. L'ordre des themes dans le ThemeManager doit suivre
// l'ordre des decors dans le troncon multi-themes (0 = Ville, 1 = Foret...).
[CreateAssetMenu(menuName = "CanonPOW/Theme", fileName = "Theme_Ville")]
public class ThemeDefinition : ScriptableObject
{
    public string displayName = "Ville";
    [Min(0)] public int price;                 // 0 = gratuit (le theme de depart)

    [Header("Murs")]
    public GameObject wallPrefab;              // vide = les murs des niveaux / le mur par defaut

    [Header("Ciel et lumiere")]
    public Material skybox;                    // vide = on garde le ciel actuel
    public Color ambientColor = new Color(0.55f, 0.6f, 0.7f);
    public Color sunColor = Color.white;
    [Min(0f)] public float sunIntensity = 1.1f;
    public bool fog = true;
    public Color fogColor = new Color(0.7f, 0.8f, 0.9f);
    [Range(0f, 0.05f)] public float fogDensity = 0.008f;
}
