using UnityEngine;

// Enchaine les murs pendant une partie : un mur casse, le suivant apparait aussitot plus loin.
// Une nouvelle partie recommence au premier mur. Le but : aller le plus loin possible.
public class LevelManager : MonoBehaviour
{
    [SerializeField] private LevelDefinition[] walls;   // Wall_01, Wall_02... dans l'ordre (reglages de Nathan)
    [SerializeField] private WallController wall;

    [Header("Apres le dernier mur de la liste : murs generes a l'infini")]
    [SerializeField] private float distanceGrowth = 1.15f;  // D_n = 50 x 1,15^(n-1) (simulation : 2 a 9 murs selon les ameliorations)

    private int wallIndex;              // 0 = premier mur de la partie
    private LevelDefinition generated;  // reutilise pour les murs au-dela de la liste

    public int WallsBroken => wallIndex;   // pour le score et les pieces (100 x n)

    // Temporaire : sera remplace par GameEvents.OnLevelLoaded.
    public event System.Action<LevelDefinition> WallPlaced;

    private void OnEnable()
    {
        wall.HitResolved += OnWallHit;
        GameEvents.OnStateChanged += OnStateChanged;
    }

    private void OnDisable()
    {
        wall.HitResolved -= OnWallHit;
        GameEvents.OnStateChanged -= OnStateChanged;
    }

    // Bouton TIRER ou REJOUER du GameManager : nouvelle partie depuis le mur 1
    private void OnStateChanged(GameState from, GameState to)
    {
        if (to == GameState.Aiming && from != GameState.Aiming)
        {
            StartNewRun();
        }
    }

    private void Start()
    {
        if (walls == null || walls.Length == 0)
        {
            Debug.LogError("LevelManager : la liste Walls est vide. Glisse Level_01 a Level_10 dedans.");
            enabled = false;
            return;
        }
        StartNewRun();
    }

    // Appelee au debut de chaque partie (plus tard : par le GameManager de Benji).
    public void StartNewRun()
    {
        wallIndex = 0;
        wall.HideAllWalls();
        PlaceWall();
    }

    private void OnWallHit(WallHitResult result)
    {
        if (result.broken)
        {
            ++wallIndex;
            GameEvents.RaiseWallBroken(wallIndex);   // numero du mur casse : 1, 2, 3...
            PlaceWall();   // tout de suite : le boulet continue sa course vers le mur suivant
        }
    }

    private void PlaceWall()
    {
        LevelDefinition next = GetWall(wallIndex);
        wall.ShowWall(next);
        Debug.Log($"Mur n°{wallIndex + 1} a {next.wallDistance:0} m");
        WallPlaced?.Invoke(next);
        GameEvents.RaiseLevelLoaded(wallIndex + 1, wall);
    }

    // Distance du mur n° index (0 = premier), sans le placer : sert au WorldStreamer
    // pour ne pas poser d'objets contre un mur.
    public float WallDistance(int index)
    {
        if (index < walls.Length)
        {
            return walls[index].wallDistance;
        }
        int extra = index - (walls.Length - 1);
        return walls[walls.Length - 1].wallDistance * Mathf.Pow(distanceGrowth, extra);
    }

    // Les murs de la liste, puis des murs calcules a partir du dernier.
    private LevelDefinition GetWall(int index)
    {
        if (index < walls.Length)
        {
            return walls[index];
        }
        if (generated == null)
        {
            generated = ScriptableObject.CreateInstance<LevelDefinition>();
        }
        LevelDefinition last = walls[walls.Length - 1];
        int extra = index - (walls.Length - 1);
        generated.levelNumber = index + 1;
        generated.wallDistance = last.wallDistance * Mathf.Pow(distanceGrowth, extra);
        generated.zone = last.zone;
        generated.wallPrefab = last.wallPrefab;
        return generated;
    }

}
