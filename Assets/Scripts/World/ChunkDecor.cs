using UnityEngine;

// A mettre a la racine d'un troncon de sol (le Chunk Prefab du WorldStreamer).
// Le troncon contient plusieurs variantes de decor (arbres, rochers, feu de camp... de Louis) :
// a chaque fois que le WorldStreamer deplace le troncon plus loin, on en affiche une au hasard.
// Il ne touche pas au WorldStreamer : il remarque seul que le troncon a bouge.
public class ChunkDecor : MonoBehaviour
{
    [SerializeField] private GameObject[] variants;      // enfants du troncon : Decor_A, Decor_B, Decor_C...
    [SerializeField] private bool mirrorRandomly = true; // retourne parfois le decor gauche/droite : plus de variete

    private float lastX = float.NaN;

    private void LateUpdate()
    {
        float x = transform.position.x;
        if (Mathf.Approximately(x, lastX))
        {
            return;
        }
        lastX = x;
        Randomize(x);
    }

    private void Randomize(float x)
    {
        if (variants == null || variants.Length == 0)
        {
            return;
        }
        // Graine tiree de la position : un troncon place au meme endroit garde le meme decor.
        System.Random random = new System.Random(Mathf.RoundToInt(x) * 7919 + 13);
        int chosen = random.Next(variants.Length);
        for (int i = 0; i < variants.Length; ++i)
        {
            if (variants[i] != null)
            {
                variants[i].SetActive(i == chosen);
            }
        }
        if (mirrorRandomly && variants[chosen] != null)
        {
            Vector3 scale = variants[chosen].transform.localScale;
            scale.z = Mathf.Abs(scale.z) * (random.Next(2) == 0 ? 1f : -1f);
            variants[chosen].transform.localScale = scale;
        }
    }
}
