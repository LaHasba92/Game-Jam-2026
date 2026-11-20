using UnityEngine;

// Affiche le modele 3D du lanceur equipe : fronde, ..., canon.
// A mettre sur l'objet Launcher. « Models » : un modele par lanceur, dans l'ordre
// (0 = Fronde ... 4 = Canon). Une case vide = on garde le modele precedent.
// Il lit le lanceur equipe dans PlayerPrefs (meme cle que RunUpgrades de Dev) : il marche
// avec ou sans RunUpgrades, donc pas de conflit le jour ou on fusionne Dev.
public class LauncherVisuals : MonoBehaviour
{
    public const string SelectedKey = "CanonPOW.SelectedLauncher";

    [SerializeField] private GameObject[] models;     // enfants de Launcher : Lanceur_Slingshot, Canon...

    private int shown = -1;

#if UNITY_EDITOR
    private int captureModel = -1;

    public void SetCaptureModel(int index)
    {
        captureModel = index;
        Show(captureModel >= 0 ? captureModel : PlayerPrefs.GetInt(SelectedKey, 0));
    }
#endif

    private void Update()
    {
        int selected = PlayerPrefs.GetInt(SelectedKey, 0);   // un simple int : pas cher a lire
#if UNITY_EDITOR
        if (captureModel >= 0) selected = captureModel;
#endif
        if (selected != shown)
        {
            Show(selected);
        }
    }

    private void Show(int index)
    {
        shown = index;
        int modelIndex = Mathf.Clamp(index, 0, models.Length - 1);
        while (modelIndex > 0 && models[modelIndex] == null)
        {
            --modelIndex;
        }
        for (int i = 0; i < models.Length; ++i)
        {
            if (models[i] != null)
            {
                models[i].SetActive(i == modelIndex);
            }
        }
    }
}
