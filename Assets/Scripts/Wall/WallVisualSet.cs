using System.Collections;
using UnityEngine;

// A mettre a la racine d'un prefab de mur (un par style : bois, briques...).
// Le prefab contient : Intact (le mur entier) et Broken (les morceaux, avec Rigidbody).
// Pivot en bas au centre, face vers -X.
public class WallVisualSet : MonoBehaviour
{
    [SerializeField] private GameObject intact;
    [SerializeField] private Transform broken;
    [SerializeField] private float wallHeight = 8f;     // hauteur du modele, en m
    [SerializeField] private float wallWidth = 6f;      // largeur du modele, en m
    [Header("Explosion : identique sur tous les murs, vers les cotes et le haut")]
    [SerializeField] private float minSpeed = 14f;         // m/s des morceaux
    [SerializeField] private float maxSpeed = 24f;
    [SerializeField] private float sideStrength = 1f;      // part de la poussee vers les cotes (Z)
    [SerializeField] private float upStrength = 0.8f;      // part vers le haut
    [SerializeField] private float forwardStrength = 0.25f; // un peu vers l'avant, JAMAIS vers la camera
    [SerializeField] private float maxSpin = 12f;          // rad/s
    [SerializeField] private float shrinkPart = 0.4f;      // les morceaux rapetissent pendant les 40 % de fin
    [SerializeField] private float debrisLifetime = 2f;
    [SerializeField] private int seed = 7;                 // meme graine = meme explosion a chaque mur

    private Rigidbody[] pieces;
    private Vector3[] startPositions;
    private Vector3[] startScales;
    private Quaternion[] startRotations;
    private Coroutine hideRoutine;

    public float WallHeight => wallHeight;
    public float WallWidth => wallWidth;

    // Vrai pendant que les debris volent : ce mur ne peut pas encore etre reutilise.
    public bool IsBusy => hideRoutine != null;

    private void Awake()
    {
        // On memorise la place de chaque morceau pour pouvoir reconstruire le mur
        // (pool : on reutilise les memes morceaux, jamais de Instantiate pendant le jeu).
        pieces = broken.GetComponentsInChildren<Rigidbody>(true);
        startPositions = new Vector3[pieces.Length];
        startRotations = new Quaternion[pieces.Length];
        startScales = new Vector3[pieces.Length];
        for (int i = 0; i < pieces.Length; ++i)
        {
            startPositions[i] = pieces[i].transform.localPosition;
            startRotations[i] = pieces[i].transform.localRotation;
            startScales[i] = pieces[i].transform.localScale;
            pieces[i].useGravity = true;
        }

        // Les morceaux ne se cognent pas entre eux : ils s'eparpillent au lieu de s'empiler.
        Collider[] colliders = broken.GetComponentsInChildren<Collider>(true);
        for (int i = 0; i < colliders.Length; ++i)
        {
            for (int j = i + 1; j < colliders.Length; ++j)
            {
                Physics.IgnoreCollision(colliders[i], colliders[j]);
            }
        }
    }

    // Mur entier, morceaux ranges.
    public void ResetWall()
    {
        if (hideRoutine != null)
        {
            StopCoroutine(hideRoutine);
            hideRoutine = null;
        }
        intact.SetActive(true);
        broken.gameObject.SetActive(false);
        for (int i = 0; i < pieces.Length; ++i)
        {
            pieces[i].isKinematic = true;   // immobile tant que le mur n'est pas casse
            pieces[i].transform.localPosition = startPositions[i];
            pieces[i].transform.localRotation = startRotations[i];
            pieces[i].transform.localScale = startScales[i];
        }
        // Le moteur physique garde sa propre copie des positions : on la met a jour tout de suite,
        // sinon un morceau reutilise croit etre encore a l'endroit de l'explosion precedente.
        Physics.SyncTransforms();
    }

    // Explosion depuis le point d'impact (en coordonnees du monde).
    public void Break(Vector3 impactPoint)
    {
        intact.SetActive(false);
        broken.gameObject.SetActive(true);
        Physics.SyncTransforms();

        // Chaque morceau part de son cote du point d'impact (gauche ou droite), vers le haut,
        // un peu vers l'avant mais jamais vers la camera (-X). On donne directement une VITESSE :
        // le resultat ne depend ni de la masse, ni de la duree d'une image. Avec la meme graine,
        // l'explosion est exactement la meme sur tous les murs.
        System.Random random = new System.Random(seed);
        foreach (Rigidbody piece in pieces)
        {
            piece.isKinematic = false;
            Vector3 offset = piece.transform.position - impactPoint;
            float side = Mathf.Abs(offset.z) > 0.2f ? Mathf.Sign(offset.z) : (random.Next(2) == 0 ? -1f : 1f);
            Vector3 direction = new Vector3(
                forwardStrength * (0.5f + (float)random.NextDouble()),
                upStrength * (0.3f + (float)random.NextDouble()) + Mathf.Clamp(offset.y * 0.05f, -0.3f, 0.5f),
                side * sideStrength * (0.6f + (float)random.NextDouble())).normalized;
            float speed = Mathf.Lerp(minSpeed, maxSpeed, (float)random.NextDouble());
            piece.linearVelocity = direction * speed;
            piece.angularVelocity = new Vector3(
                (float)random.NextDouble() * 2f - 1f,
                (float)random.NextDouble() * 2f - 1f,
                (float)random.NextDouble() * 2f - 1f) * maxSpin;
        }
        hideRoutine = StartCoroutine(HideDebrisLater());
    }

    private IEnumerator HideDebrisLater()
    {
        // Les morceaux rapetissent a la fin, puis disparaissent : ils ne restent pas dans le champ.
        float shrinkStart = debrisLifetime * (1f - shrinkPart);
        for (float t = 0f; t < debrisLifetime; t += Time.deltaTime)
        {
            if (t > shrinkStart)
            {
                float k = 1f - (t - shrinkStart) / (debrisLifetime - shrinkStart);
                for (int i = 0; i < pieces.Length; ++i)
                {
                    pieces[i].transform.localScale = startScales[i] * k;
                }
            }
            yield return null;
        }
        broken.gameObject.SetActive(false);   // rendus au pool (GDD)
        hideRoutine = null;
        gameObject.SetActive(false);          // ce mur est libre pour un prochain
    }
}
