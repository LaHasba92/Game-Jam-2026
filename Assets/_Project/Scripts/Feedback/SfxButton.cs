using UnityEngine;
using UnityEngine.UI;

// À poser sur n'importe quel bouton uGUI : il joue le clic de SfxManager.
// Rien à brancher dans l'Inspector.
[RequireComponent(typeof(Button))]
public class SfxButton : MonoBehaviour
{
    void OnEnable()
    {
        GetComponent<Button>().onClick.AddListener(SfxManager.PlayClick);
    }

    void OnDisable()
    {
        GetComponent<Button>().onClick.RemoveListener(SfxManager.PlayClick);
    }
}
