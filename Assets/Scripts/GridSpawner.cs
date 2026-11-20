using System.Collections.Generic;
using UnityEngine;

public class GridSpawner : MonoBehaviour
{
    [SerializeField] private GameObject blockPrefab;
    [SerializeField] private int width = 6;
    [SerializeField] private int height = 6;
    [SerializeField] private float spacing = 1.1f;
    [SerializeField] private Color[] colors = { Color.red, Color.blue, Color.yellow };

    // Morceau 1 : ce qu'on memorise
    private GameObject[,] blocks;
    private int[,] colorOf;
    private Dictionary<GameObject, Vector2Int> cellOf = new Dictionary<GameObject, Vector2Int>();

    // Les 4 voisines : droite, gauche, haut, bas
    private static readonly Vector2Int[] Directions =
    {
        Vector2Int.right, Vector2Int.left, Vector2Int.up, Vector2Int.down,
    };

    private void Start()
    {
        blocks = new GameObject[width, height];
        colorOf = new int[width, height];

        for (int x = 0; x < width; ++x)
        {
            for (int y = 0; y < height; ++y)
            {
                GameObject block = Instantiate(blockPrefab, transform);
                block.transform.localPosition = new Vector3(x * spacing, y * spacing + 0.5f, 0f);
                block.name = $"Block_{x}_{y}";

                int colorIndex = Random.Range(0, colors.Length);
                block.GetComponent<Renderer>().material.color = colors[colorIndex];

                blocks[x, y] = block;
                colorOf[x, y] = colorIndex;
                cellOf[block] = new Vector2Int(x, y);
            }
        }
    }

    private void Update()
    {
        if (Input.GetMouseButtonDown(0))
        {
            Ray ray = Camera.main.ScreenPointToRay(Input.mousePosition);

            // Morceau 2 : retrouver la case du bloc touche
            if (Physics.Raycast(ray, out RaycastHit hit)
                && cellOf.TryGetValue(hit.collider.gameObject, out Vector2Int cell))
            {
                // Morceaux 3 et 4 : trouver le groupe, puis le cacher
                List<Vector2Int> group = FindGroup(cell);
                foreach (Vector2Int c in group)
                {
                    blocks[c.x, c.y].SetActive(false);
                }
                Debug.Log($"Case {cell} : {group.Count} blocs detruits");
            }
        }
    }

    // Morceau 3 : le flood fill (parcours en largeur)
    private List<Vector2Int> FindGroup(Vector2Int start)
    {
        List<Vector2Int> group = new List<Vector2Int>();
        int color = colorOf[start.x, start.y];

        bool[,] seen = new bool[width, height];
        Queue<Vector2Int> queue = new Queue<Vector2Int>();
        queue.Enqueue(start);
        seen[start.x, start.y] = true;

        while (queue.Count > 0)
        {
            Vector2Int current = queue.Dequeue();
            group.Add(current);

            foreach (Vector2Int direction in Directions)
            {
                Vector2Int n = current + direction;
                if (IsInside(n)
                    && !seen[n.x, n.y]
                    && blocks[n.x, n.y].activeSelf     // encore visible
                    && colorOf[n.x, n.y] == color)     // meme couleur
                {
                    seen[n.x, n.y] = true;
                    queue.Enqueue(n);
                }
            }
        }
        return group;
    }

    private bool IsInside(Vector2Int cell)
    {
        return cell.x >= 0 && cell.x < width && cell.y >= 0 && cell.y < height;
    }
}