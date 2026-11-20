using UnityEngine;

// Un mur de la sequence = un fichier de reglages (clic droit > Create > CanonPOW > Level Definition).
// Nathan regle les valeurs dans l'Inspector, sans toucher au code.
[CreateAssetMenu(menuName = "CanonPOW/Level Definition", fileName = "Level_01")]
public class LevelDefinition : ScriptableObject
{
    public int levelNumber = 1;
    public float wallDistance = 50f;     // m, depuis le lanceur
    [Header("Mur géant (zéro PV pour un mur ordinaire)")]
    [Min(0)] public int bossMaxHealth;
    [Min(0f)] public float damagePerSpeed = 5f;
    public float[] crackThresholds = { 0.75f, 0.50f, 0.25f };
    public Vector3 bossSize = new Vector3(3f, 14f, 14f);
    public GameObject[] bossStageModels; // intact, 75 %, 50 %, 25 %
    public GameObject bossDebris;
    public GameObject[] bossDebrisVariants;
    [Range(6, 40)] public int bossDebrisCount = 24;
    public float burstRadius = 12f, fragmentSize = 1.6f;
    public float labelOffset = 1f, labelRange = 85f, damageLabelSeconds = 1.2f, damageRise = 3f;
    [Range(0.05f, 0.6f)] public float barWidthFraction = 0.24f;
    public bool IsBoss => bossMaxHealth > 0;
    public int zone = 1;                 // 1 Campagne, 2 Ville, 3 Chateau
    public string wallStyle = "Bois";
    public GameObject wallPrefab;        // le mur de ce niveau (avec WallVisualSet) ; vide = mur par defaut
}
