using System.IO;
using TMPro;
using UnityEngine;

// Affiche les pieces que le joueur possede (« 1 250 pieces »).
// A mettre sur un texte TextMeshPro du menu, de la boutique ou du HUD. Il se met a jour tout seul.
public class CoinsDisplay : MonoBehaviour
{
    [SerializeField] private TMP_Text text;                 // vide = le texte du meme objet
    [SerializeField] private string format = "{0:N0} pieces";
    [SerializeField] private string saveFileName = "upgrade-test.json";   // la sauvegarde de Nathan

    private void Awake()
    {
        if (text == null)
        {
            text = GetComponent<TMP_Text>();
        }
        GameEvents.OnCoinsChanged += Show;   // Awake : marche meme si l'ecran est cache
    }

    private void OnDestroy()
    {
        GameEvents.OnCoinsChanged -= Show;
    }

    private void OnEnable()
    {
        Show(ReadCoins());   // l'ecran reapparait : on relit le total
    }

    private void Show(int coins)
    {
        if (text != null)
        {
            text.text = string.Format(format, coins);
        }
    }

    private int ReadCoins()
    {
        string path = Path.Combine(Application.persistentDataPath, saveFileName);
        return File.Exists(path) ? JsonUtility.FromJson<UpgradeSaveData>(File.ReadAllText(path)).coins : 0;
    }
}
