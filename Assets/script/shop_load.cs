using UnityEngine;
using UnityEngine.SceneManagement;

public class SceneLoader : MonoBehaviour
{
    public void ChargerScene(string nomDeLaScene)
    {
        SceneManager.LoadScene(nomDeLaScene);
    }
}