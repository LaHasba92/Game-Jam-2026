using System.Collections.Generic;
using UnityEngine;

// Le sol infini : quelques troncons de 50 m recycles devant le boulet, avec les objets de la piste.
// Axe X = la piste. Les objets sont places au hasard selon les regles du GDD (section 2).
public class WorldStreamer : MonoBehaviour
{
    [System.Serializable]
    public class SpawnRule
    {
        public GroundObjectType type;
        public float minGap = 30f;        // m entre deux apparitions
        public float maxGap = 60f;
        public float fromDistance = 0f;   // m : n'apparait pas avant
        public float minHeight = 0f;      // m : 0 = pose au sol
        public float maxHeight = 0f;
        public int minCount = 1;          // > 1 = une ligne d'objets (les pieces)
        public int maxCount = 1;
        public float spacing = 1.5f;      // m entre les objets d'une ligne
    }

    [SerializeField] private Transform target;            // le boulet (en sandbox : la camera)
    [SerializeField] private LevelManager levelManager;   // pour remettre la piste a zero a chaque partie
    [SerializeField] private GameObject chunkPrefab;      // un troncon de sol de 50 m, pivot au debut
    [SerializeField] private float chunkLength = 50f;
    [SerializeField] private int chunksAhead = 6;         // 300 m de sol visibles devant
    [SerializeField] private int chunksBehind = 1;
    [SerializeField] private float corridorWidth = 6f;    // = WallWidth : les objets restent dans le couloir
    [SerializeField] private float clearBeforeWall = 10f;  // m sans objet devant chaque mur
    [SerializeField] private float clearAfterWall = 5f;    // m sans objet derriere chaque mur
    [SerializeField] private GroundObject[] objectPrefabs; // un prefab par type
    [SerializeField] private SpawnRule[] rules =
    {
        new SpawnRule { type = GroundObjectType.Trampoline, minGap = 30, maxGap = 60 },
        new SpawnRule { type = GroundObjectType.Booster, minGap = 60, maxGap = 120, fromDistance = 100 },
        new SpawnRule { type = GroundObjectType.Balloon, minGap = 150, maxGap = 300, fromDistance = 190, minHeight = 2, maxHeight = 6 },
        new SpawnRule { type = GroundObjectType.Sticky, minGap = 150, maxGap = 300, fromDistance = 530 },
    };

    // Valeur de secours pour les prefabs créés avant l'ajout des pièces.
    private static readonly SpawnRule CoinRule = new SpawnRule
    {
        type = GroundObjectType.Coin, minGap = 20f, maxGap = 40f,
        minHeight = 0.35f, maxHeight = 0.35f, minCount = 5, maxCount = 10
    };
    private SpawnRule[] activeRules;
    private float longestGroup;         // m : la plus longue ligne possible, pour filtrer les troncons

    private class Chunk
    {
        public Transform transform;
        public float start;
        public readonly List<GroundObject> objects = new List<GroundObject>();
    }

    private readonly List<Chunk> chunks = new List<Chunk>();          // du plus proche au plus loin
    private readonly Dictionary<GroundObjectType, Stack<GroundObject>> pool = new Dictionary<GroundObjectType, Stack<GroundObject>>();
    private float[] nextSpawn;          // prochaine distance d'apparition, par regle
    private System.Random random;
    private int trackSeed;
    private readonly HashSet<Vector3> collectedCoins = new HashSet<Vector3>();

    private void OnEnable()  { if (levelManager != null) levelManager.WallPlaced += OnWallPlaced; }
    private void OnDisable() { if (levelManager != null) levelManager.WallPlaced -= OnWallPlaced; }

    private void Awake()
    {
        if (chunkPrefab == null)
        {
            Debug.LogError("WorldStreamer : prefab de sol manquant.", this);
            enabled = false;
            return;
        }
        for (int i = 0; i < Mathf.Max(2, chunksBehind + chunksAhead); ++i)
        {
            chunks.Add(new Chunk { transform = Instantiate(chunkPrefab, transform).transform });
        }
        ResetTrack();
    }

    private void OnWallPlaced(LevelDefinition wall)
    {
        // Nouvelle partie : premier mur de la liste (ne depend pas du levelNumber saisi dans la fiche).
        if (levelManager.WallsBroken == 0)
        {
            ResetTrack();
        }
    }

    // Piste neuve : troncons replaces depuis le lanceur, nouveaux objets au hasard.
    public void ResetTrack()
    {
        trackSeed = Random.Range(0, int.MaxValue);
        collectedCoins.Clear();
        BuildTrack();
    }

    public void RewindTo(float distance)
    {
        if (chunks.Count == 0 || distance >= chunks[0].start) return;
        BuildTrack();
        AdvanceTo(distance);
    }

    private void BuildTrack()
    {
        random = new System.Random(trackSeed);
        var configured = new List<SpawnRule>(rules ?? System.Array.Empty<SpawnRule>());
        configured.RemoveAll(rule => rule == null);
        if (!configured.Exists(rule => rule.type == GroundObjectType.Coin))
            configured.Add(CoinRule);
        activeRules = configured.ToArray();
        nextSpawn = new float[activeRules.Length];
        longestGroup = 0f;
        for (int r = 0; r < activeRules.Length; ++r)
        {
            SpawnRule rule = activeRules[r];
            longestGroup = Mathf.Max(longestGroup, (Mathf.Max(1, rule.maxCount) - 1) * Mathf.Abs(rule.spacing));
            // La première ligne standard tient devant le premier mur, même si elle est réduite à cinq pièces.
            float first = rule == CoinRule ? RandomRange(20f, 30f) : RandomRange(10f, Mathf.Max(10f, rule.maxGap));
            nextSpawn[r] = Mathf.Max(rule.fromDistance, first);
        }
        for (int i = 0; i < chunks.Count; ++i)
        {
            PlaceChunk(chunks[i], (i - chunksBehind) * chunkLength);
        }
    }

    private void Update()
    {
        if (target == null)
        {
            return;
        }
        AdvanceTo(target.position.x);
    }

    public void AdvanceTo(float x)
    {
        if (chunks.Count < 2 || chunkLength <= 0f) return;
        // Le troncon le plus en arriere est trop loin derriere : on le renvoie tout devant.
        while (x - chunks[0].start > chunkLength * (chunksBehind + 1))
        {
            Chunk recycled = chunks[0];
            chunks.RemoveAt(0);
            PlaceChunk(recycled, chunks[chunks.Count - 1].start + chunkLength);
            chunks.Add(recycled);
        }
    }

    private void PlaceChunk(Chunk chunk, float start)
    {
        ReleaseObjects(chunk);
        chunk.start = start;
        chunk.transform.position = new Vector3(start, 0f, 0f);
        if (start < 0f)
        {
            return;   // pas d'objets derriere le lanceur
        }

        float end = start + chunkLength;
        for (int r = 0; r < activeRules.Length; ++r)
        {
            SpawnRule rule = activeRules[r];
            while (nextSpawn[r] < end)
            {
                if (nextSpawn[r] >= start)
                {
                    SpawnGroup(chunk, rule, nextSpawn[r]);
                }
                nextSpawn[r] += Mathf.Max(1f, RandomRange(rule.minGap, rule.maxGap));
            }
        }
    }

    private void SpawnGroup(Chunk chunk, SpawnRule rule, float distance)
    {
        int minimum = Mathf.Max(1, rule.minCount);
        int count = random.Next(minimum, Mathf.Max(minimum, rule.maxCount) + 1);
        float height = RandomRange(rule.minHeight, rule.maxHeight);
        float half = corridorWidth * 0.5f - 0.5f;
        float side = RandomRange(-half, half);
        float groupEnd = distance + (count - 1) * rule.spacing;
        // Réduit une ligne de pièces si elle rencontre la zone de sécurité d'un mur.
        while (rule.type == GroundObjectType.Coin && count > minimum && IsNearAWall(distance, groupEnd))
        {
            count--;
            groupEnd = distance + (count - 1) * rule.spacing;
        }
        if (IsNearAWall(distance, groupEnd))
        {
            return;   // on ne pose rien contre un mur (ni a travers)
        }
        for (int i = 0; i < count; ++i)
        {
            GroundObject obj = Get(rule.type);
            if (obj == null) return;
            obj.transform.position = new Vector3(distance + i * rule.spacing, height, side);
            obj.gameObject.SetActive(rule.type != GroundObjectType.Coin || !collectedCoins.Contains(obj.transform.position));
            chunk.objects.Add(obj);
        }
    }

    // Vrai si le segment [from, to] touche la zone interdite autour d'un mur.
    private bool IsNearAWall(float from, float to)
    {
        if (levelManager == null)
        {
            return false;
        }
        // Garde-fou : une fiche mal reglee (distance 0) ne doit pas bloquer le jeu dans une boucle infinie.
        for (int i = 0; i < 1000; ++i)
        {
            float wallX = levelManager.WallDistance(i);
            if (wallX - clearBeforeWall > to)
            {
                return false;   // les murs suivants sont encore plus loin
            }
            if (to >= wallX - clearBeforeWall && from <= wallX + clearAfterWall)
            {
                return true;
            }
        }
        return false;
    }

    // --- Pour Andy : trouver les objets que le boulet touche ---------------------------------

    // Renvoie l'objet actif le plus proche de « position » s'il est a portee (radius de l'objet + ballRadius).
    public bool TryGetObjectAt(Vector3 position, float ballRadius, out GroundObject found)
    {
        found = null;
        float best = float.MaxValue;
        foreach (Chunk chunk in chunks)
        {
            if (position.x < chunk.start - 5f || position.x > chunk.start + chunkLength + 5f)
            {
                continue;
            }
            foreach (GroundObject obj in chunk.objects)
            {
                if (!obj.gameObject.activeSelf || obj.type == GroundObjectType.Coin)
                {
                    continue;
                }
                float d = Vector3.Distance(position, obj.transform.position);
                if (d <= obj.radius + ballRadius && d < best)
                {
                    best = d;
                    found = obj;
                }
            }
        }
        return found != null;
    }

    // Ramasse toutes les pièces traversées, même entre deux pas de simulation.
    public int CollectCoins(Vector3 from, Vector3 to, float ballRadius)
    {
        int count = 0;
        Vector3 path = to - from;
        float lengthSquared = path.sqrMagnitude;
        float minX = Mathf.Min(from.x, to.x) - ballRadius - 5f;
        float maxX = Mathf.Max(from.x, to.x) + ballRadius + 5f;
        foreach (Chunk chunk in chunks)
        {
            // Seuls les troncons proches du boulet : « Passer » appelle ceci des milliers de fois dans une frame.
            if (maxX < chunk.start || minX > chunk.start + chunkLength + longestGroup) continue;
            foreach (GroundObject obj in chunk.objects)
            {
                if (obj.type != GroundObjectType.Coin || !obj.gameObject.activeSelf) continue;
                Vector3 offset = obj.transform.position - from;
                float t = lengthSquared > 0f ? Mathf.Clamp01(Vector3.Dot(offset, path) / lengthSquared) : 0f;
                float reach = obj.radius + ballRadius;
                if ((offset - path * t).sqrMagnitude > reach * reach) continue;
                collectedCoins.Add(obj.transform.position);
                Consume(obj);
                count++;
            }
        }
        return count;
    }

    // Une piece ramassee (ou un ballon utilise) disparait jusqu'au prochain recyclage.
    public void Consume(GroundObject obj)
    {
        obj.gameObject.SetActive(false);
    }

    // --- Pool ---------------------------------------------------------------------------------

    private GroundObject Get(GroundObjectType type)
    {
        if (pool.TryGetValue(type, out Stack<GroundObject> stack) && stack.Count > 0)
        {
            return stack.Pop();
        }
        foreach (GroundObject prefab in objectPrefabs ?? System.Array.Empty<GroundObject>())
        {
            if (prefab != null && prefab.type == type)
            {
                return Instantiate(prefab, transform);
            }
        }
        Debug.LogError($"WorldStreamer : aucun prefab pour le type {type}");
        return null;
    }

    private void ReleaseObjects(Chunk chunk)
    {
        foreach (GroundObject obj in chunk.objects)
        {
            obj.gameObject.SetActive(false);
            if (!pool.TryGetValue(obj.type, out Stack<GroundObject> stack))
            {
                stack = new Stack<GroundObject>();
                pool[obj.type] = stack;
            }
            stack.Push(obj);
        }
        chunk.objects.Clear();
    }

    private float RandomRange(float min, float max)
    {
        return min + (float)random.NextDouble() * (max - min);
    }
}
