using UnityEngine;
using UnityEngine.UI;

// A mettre sur un bouton d'un ecran de Hugo (un prefab) : choisir l'action dans la liste.
// Le bouton trouve tout seul le GameManager et le UIStateSwitcher de la scene au moment du clic :
// pas besoin de les glisser dans On Click (un prefab ne peut pas pointer vers un objet de la scene).
[RequireComponent(typeof(Button))]
public class UIButtonAction : MonoBehaviour
{
    public enum Action { Play, OpenShop, CloseShop, GoHome, Skip }

    [SerializeField] private Action action;

    private void Awake()
    {
        GetComponent<Button>().onClick.AddListener(Run);
    }

    private void Run()
    {
        UIStateSwitcher ui = FindFirstObjectByType<UIStateSwitcher>();
        switch (action)
        {
            case Action.Play:
                if (GameManager.Instance != null) GameManager.Instance.Play();
                break;
            case Action.GoHome:
                if (GameManager.Instance != null) GameManager.Instance.GoHome();
                break;
            case Action.OpenShop:
                if (ui != null) ui.OpenShop();
                break;
            case Action.CloseShop:
                if (ui != null) ui.CloseShop();
                break;
            case Action.Skip:
                if (ui != null) ui.Skip();
                break;
        }
    }
}
