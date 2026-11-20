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

    private LevelDefinition FinalWall => AimTuning.Current != null ? AimTuning.Current.giantWall : null;

    private void Awake()
    {
        if (FinalWall != null && GetComponent<BossWallView>() == null) gameObject.AddComponent<BossWallView>();
    }

    private void OnEnable()
    {
        if (wall != null) wall.HitResolved += OnWallHit;
        GameEvents.OnStateChanged += OnStateChanged;
    }

    private void OnDisable()
    {
        if (wall != null) wall.HitResolved -= OnWallHit;
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
        if (wall == null || walls == null || walls.Length == 0 || System.Array.Exists(walls, entry => entry == null))
        {
            Debug.LogError("LevelManager : assigne le WallController et les fiches de mur.");
            enabled = false;
            return;
        }
        StartNewRun();
    }

    // Appelee au debut de chaque partie (plus tard : par le GameManager de Benji).
    public void StartNewRun()
    {
        if (!isActiveAndEnabled || wall == null || walls == null || walls.Length == 0 || walls[0] == null) return;
        wallIndex = 0;
        wall.HideAllWalls();
        PlaceWall();
    }

    public bool SelectBossForCapture()
    {
        LevelDefinition boss = FinalWall;
        if (!JamSave.Preview || boss == null || BossProgress.Health(boss) == 0 ||
            wall == null || walls == null || walls.Length == 0 || walls[walls.Length - 1] == null ||
            walls[walls.Length - 1].wallDistance <= 0f) return false;
        wallIndex = 0;
        while (OrdinaryDistance(wallIndex) < boss.wallDistance) wallIndex++;
        wall.HideAllWalls();
        PlaceWall();
        return true;
    }

    private void OnWallHit(WallHitResult result)
    {
        if (result.broken)
        {
            ++wallIndex;
            GameEvents.RaiseWallBroken(wallIndex);   // numero du mur casse : 1, 2, 3...
            if (result.bossHit) return;   // victoire : la prochaine partie reprend la piste normale
            PlaceWall();   // tout de suite : le boulet continue sa course vers le mur suivant
        }
    }

    private void OnDestroy()
    {
        if (generated != null) Destroy(generated);
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
        float natural = OrdinaryDistance(index);
        LevelDefinition boss = FinalWall;
        if (boss == null || BossProgress.Health(boss) == 0 || natural < boss.wallDistance) return natural;
        if (index > 0 && OrdinaryDistance(index - 1) >= boss.wallDistance) return float.PositiveInfinity;
        return boss.wallDistance;
    }

    private float OrdinaryDistance(int index)
    {
        if (walls == null || walls.Length == 0) return float.PositiveInfinity;
        if (index < walls.Length)
        {
            return walls[index] != null ? walls[index].wallDistance : float.PositiveInfinity;
        }
        int extra = index - (walls.Length - 1);
        return walls[walls.Length - 1] != null ? walls[walls.Length - 1].wallDistance * Mathf.Pow(Mathf.Max(1.01f, distanceGrowth), extra) : float.PositiveInfinity;
    }

    // Les murs de la liste, puis des murs calcules a partir du dernier.
    private LevelDefinition GetWall(int index)
    {
        LevelDefinition boss = FinalWall;
        if (boss != null && BossProgress.Health(boss) > 0 && OrdinaryDistance(index) >= boss.wallDistance)
            return boss;
        if (index < walls.Length)
        {
            return walls[index];
        }
        if (generated == null)
        {
            generated = ScriptableObject.CreateInstance<LevelDefinition>();
        }
        LevelDefinition last = walls[walls.Length - 1];
        generated.levelNumber = index + 1;
        generated.wallDistance = WallDistance(index);
        generated.zone = last.zone;
        generated.wallPrefab = last.wallPrefab;
        return generated;
    }

}
