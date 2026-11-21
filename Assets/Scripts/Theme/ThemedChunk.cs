using UnityEngine;

// A la racine du troncon multi-themes : un enfant par theme (Ville, Foret...).
// Le ThemeManager n'en affiche qu'un. Chaque enfant garde son propre ChunkDecor (decor au hasard).
public class ThemedChunk : MonoBehaviour
{
    [SerializeField] private GameObject[] themeRoots;   // 0 = Ville, 1 = Foret... (meme ordre que le ThemeManager)

    public void Show(int themeIndex)
    {
        for (int i = 0; i < themeRoots.Length; ++i)
        {
            if (themeRoots[i] != null)
            {
                themeRoots[i].SetActive(i == themeIndex);
            }
        }
    }
}
