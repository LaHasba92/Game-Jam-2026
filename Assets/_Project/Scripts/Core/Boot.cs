using UnityEngine;
using UnityEngine.SceneManagement;

// Scène Boot (GDD section 7) : charge la sauvegarde, puis ouvre la scène Game.
// À mettre sur un objet vide « Boot » dans la scène Boot.
public class Boot : MonoBehaviour
{
    [SerializeField] private string gameSceneName = "Game";

    void Start()
    {
        // Plus tard : SaveSystem.Load() (Nathan), puis GameManager.SetLevel(niveau sauvegardé)
        SceneManager.LoadScene(gameSceneName);
    }
}
