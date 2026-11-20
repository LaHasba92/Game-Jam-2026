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

    private class Chunk
    {
        public Transform transform;
        public float start;
        public readonly List<GroundObject> objects = new List<GroundObject>();
    }

    private readonly List<Chunk> chunks = new List<Chunk>();          // du plus proche au plus loin
    private readonly Dictionary<GroundObjectType, Stack<GroundObject>> pool = new Dictionary<GroundObjectType, Stack<GroundObject>>();
    private float[] nextSpawn;          // prochaine distance d'apparition, par regle
    private System.Random random = new System.Random();

    private void OnEnable()  { if (levelManager != null) levelManager.WallPlaced += OnWallPlaced; }
    private void OnDisable() { if (levelManager != null) levelManager.WallPlaced -= OnWallPlaced; }

    private void Awake()
    {
        for (int i = 0; i < chunksBehind + chunksAhead; ++i)
        {
            chunks.Add(new Chunk { transform = Instantiate(chunkPrefab, transform).transform });
        }
        ResetTrack();
    }

    private void OnWallPlaced(LevelDefinition wall)
    {
        if (wall.levelNumber == 1)   // nouvelle partie
        {
            ResetTrack();
        }
    }

    // Piste neuve : troncons replaces depuis le lanceur, nouveaux objets au hasard.
    public void ResetTrack()
    {
        random = new System.Random();
        nextSpawn = new float[rules.Length];
        for (int r = 0; r < rules.Length; ++r)
        {
            nextSpawn[r] = Mathf.Max(rules[r].fromDistance, RandomRange(10f, rules[r].maxGap));
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
        // Le troncon le plus en arriere est trop loin derriere : on le renvoie tout devant.
        while (target.position.x - chunks[0].start > chunkLength * (chunksBehind + 1))
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
        for (int r = 0; r < rules.Length; ++r)
        {
            SpawnRule rule = rules[r];
            while (nextSpawn[r] < end)
            {
                if (nextSpawn[r] >= start)
                {
                    SpawnGroup(chunk, rule, nextSpawn[r]);
                }
                nextSpawn[r] += RandomRange(rule.minGap, rule.maxGap);
            }
        }
    }

    private void SpawnGroup(Chunk chunk, SpawnRule rule, float distance)
    {
        int count = random.Next(rule.minCount, rule.maxCount + 1);
        float height = RandomRange(rule.minHeight, rule.maxHeight);
        float half = corridorWidth * 0.5f - 0.5f;
        float side = RandomRange(-half, half);
        float groupEnd = distance + (count - 1) * rule.spacing;
        if (IsNearAWall(distance, groupEnd))
        {
            return;   // on ne pose rien contre un mur (ni a travers)
        }
        for (int i = 0; i < count; ++i)
        {
            GroundObject obj = Get(rule.type);
            obj.transform.position = new Vector3(distance + i * rule.spacing, height, side);
            obj.gameObject.SetActive(true);
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
        for (int i = 0; ; ++i)
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
                if (!obj.gameObject.activeSelf)
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
        foreach (GroundObject prefab in objectPrefabs)
        {
            if (prefab.type == type)
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
