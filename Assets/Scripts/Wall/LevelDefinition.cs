using UnityEngine;

// Un mur de la sequence = un fichier de reglages (clic droit > Create > CanonPOW > Level Definition).
// Nathan regle les valeurs dans l'Inspector, sans toucher au code.
[CreateAssetMenu(menuName = "CanonPOW/Level Definition", fileName = "Level_01")]
public class LevelDefinition : ScriptableObject
{
    public int levelNumber = 1;
    public float wallDistance = 50f;     // m, depuis le lanceur
    public int zone = 1;                 // 1 Campagne, 2 Ville, 3 Chateau
    public string wallStyle = "Bois";
    public GameObject wallPrefab;        // le mur de ce niveau (avec WallVisualSet) ; vide = mur par defaut

    [Header("Mur renforcé")]
    [Tooltip("PV du mur. Une valeur de 0 utilise la règle de casse des murs classiques.")]
    [Min(0)] public int maxHealth;
    [Tooltip("Vitesse en m/s nécessaire pour détruire ce mur à pleine vie en un impact.")]
    [Min(1f)] public float fullHealthBreakSpeed = 30f;
}
