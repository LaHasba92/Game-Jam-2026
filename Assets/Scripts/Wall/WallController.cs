using System.Collections;
using System.Collections.Generic;
using UnityEngine;

// Le mur actuel : place le bon prefab, decide casse / rate (le mur casse a tout impact en l'air).
// Axe X = la piste (distance), axe Y = la hauteur. Le lanceur est en X = 0.
// Pendant une partie, les murs s'enchainent : un mur casse, le suivant apparait plus loin.
public class WallController : MonoBehaviour, IWall
{
    [SerializeField] private GameObject defaultWallPrefab; // si le mur n'a pas de prefab
    [SerializeField] private Transform missMarker;         // petite croix « Rate ici » (facultatif)
    [SerializeField] private float reboundFactor = -0.3f;  // a valider avec Andy
    [SerializeField] private float corridorHeight = 0f;    // m : plafond invisible ; 0 = pas de plafond

    // Amelioration Elan : part de la vitesse gardee apres la casse. Plus tard, lue dans l'UpgradeManager de Nathan.
    public float Elan { get; set; } = 0.6f;

    // Temporaire : sera remplace par GameEvents.OnWallHit quand Benji l'aura pousse.
    public event System.Action<WallHitResult> HitResolved;

    public float Distance => current.wallDistance;
    public float WallWidth => currentSet.WallWidth;        // le couloir (murs invisibles d'Andy)
    public float WallHeight => corridorHeight;             // le plafond invisible ; 0 = aucun

    // Pool : plusieurs exemplaires par style, parce que l'ancien mur explose encore
    // pendant que le suivant est deja place plus loin.
    private readonly Dictionary<GameObject, List<WallVisualSet>> pool = new Dictionary<GameObject, List<WallVisualSet>>();
    private LevelDefinition current;
    private WallVisualSet currentSet;
    private WallHit lastHit;
    private WallVisualSet brokenSet;   // le mur qui vient d'etre casse, en attendant son explosion
    private Vector3 brokenImpactPoint;

    // Appelee par LevelManager pour chaque nouveau mur.
    public void ShowWall(LevelDefinition wall)
    {
        // l'ancien mur : on le cache, sauf s'il est en train d'exploser (il se cachera tout seul)
        if (currentSet != null && currentSet != brokenSet && !currentSet.IsBusy)
        {
            currentSet.gameObject.SetActive(false);
        }

        current = wall;
        currentSet = GetFreeSet(wall.wallPrefab != null ? wall.wallPrefab : defaultWallPrefab);
        currentSet.gameObject.SetActive(true);
        currentSet.transform.position = new Vector3(Distance, 0f, 0f);
        currentSet.ResetWall();
        if (missMarker != null)
        {
            missMarker.gameObject.SetActive(false);
        }
    }

    // Nouvelle partie : on cache tous les murs, y compris ceux qui explosent encore.
    public void HideAllWalls()
    {
        foreach (List<WallVisualSet> sets in pool.Values)
        {
            foreach (WallVisualSet set in sets)
            {
                set.ResetWall();
                set.gameObject.SetActive(false);
            }
        }
        currentSet = null;
        brokenSet = null;
    }

    private WallVisualSet GetFreeSet(GameObject prefab)
    {
        if (!pool.TryGetValue(prefab, out List<WallVisualSet> sets))
        {
            sets = new List<WallVisualSet>();
            pool[prefab] = sets;
        }
        foreach (WallVisualSet set in sets)
        {
            if (!set.IsBusy && set != currentSet && set != brokenSet)
            {
                return set;
            }
        }
        WallVisualSet created = Instantiate(prefab, transform).GetComponent<WallVisualSet>();
        sets.Add(created);
        return created;
    }

    public WallHitResult ApplyHit(WallHit hit)
    {
        lastHit = hit;
        WallHitResult result = WallRules.Evaluate(hit, Elan, reboundFactor);
        if (result.broken)
        {
            // On retient CE mur avant de prevenir les autres : LevelManager va aussitot
            // placer le mur suivant, et PlayHit doit faire exploser celui-ci, pas le nouveau.
            brokenSet = currentSet;
            brokenImpactPoint = new Vector3(Distance, hit.impactHeight, hit.impactSide);
        }
        Debug.Log(result.broken
            ? $"MUR CASSE a {Distance:0} m !"
            : "Le boulet roule : pas de casse");
        HitResolved?.Invoke(result);
        return result;
    }

    public IEnumerator PlayHit(WallHitResult r)
    {
        if (r.broken)
        {
            if (brokenSet != null)
            {
                brokenSet.Break(brokenImpactPoint);
                brokenSet = null;
            }
            yield break;
        }

        Vector3 impactPoint = new Vector3(Distance, lastHit.impactHeight, lastHit.impactSide);

        // Echec : marque a l'endroit touche, et le mur tremble a peine.
        if (missMarker != null)
        {
            missMarker.position = impactPoint + Vector3.left * 0.1f;
            missMarker.gameObject.SetActive(true);
        }
        Transform wall = currentSet.transform;
        Vector3 start = wall.position;
        for (float t = 0f; t < 0.2f; t += Time.deltaTime)
        {
            wall.position = start + Random.insideUnitSphere * 0.05f;
            yield return null;
        }
        wall.position = start;
    }
}
