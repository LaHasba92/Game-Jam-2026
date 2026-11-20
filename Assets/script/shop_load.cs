using UnityEngine;
using UnityEngine.SceneManagement;

public class shop_load : MonoBehaviour
{
    public void ChargerScene(string nomDeLaScene)
    {
        SceneManager.LoadScene(nomDeLaScene);
    }
}