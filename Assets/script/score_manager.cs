using UnityEngine;
using TMPro;

public class score_manager : MonoBehaviour
{
    [SerializeField] private TMP_Text texteScore;
    private int scoreActuel;

    // Le meilleur score = le plus de murs casses en une partie (GDD : le but du jeu)
    // On s'abonne dans Awake (et pas OnEnable) : les ecrans sont caches pendant le vol,
    // et un objet cache ne recevrait pas la fin de partie.
    void Awake()     { GameEvents.OnRunEnded += HandleRunEnded; }
    void OnDestroy() { GameEvents.OnRunEnded -= HandleRunEnded; }

    // A chaque fois que l'ecran reapparait (menu, fin), on relit le record a jour.
    void OnEnable()
    {
        scoreActuel = PlayerPrefs.GetInt("ScoreSauvegarde", 0);
        MettreAJourAffichage();
    }

    void HandleRunEnded(RunResult result)
    {
#if UNITY_EDITOR
        if (AdCaptureDirector.IsPlayingTake) return;
#endif
        scoreActuel = PlayerPrefs.GetInt("ScoreSauvegarde", 0);
        if (result.wallsBroken > scoreActuel)
        {
            scoreActuel = result.wallsBroken;
            PlayerPrefs.SetInt("ScoreSauvegarde", scoreActuel);
            PlayerPrefs.Save();
        }
        MettreAJourAffichage();
    }

    void Start()
    {
        // Charge le score sauvegarde (0 par defaut lors de la premiere partie)
        scoreActuel = PlayerPrefs.GetInt("ScoreSauvegarde", 0);
        MettreAJourAffichage();
    }

    public void AjouterPoints(int points)
    {
        scoreActuel += points;
        // Sauvegarde immediatement le nouveau score sur l'appareil
        PlayerPrefs.SetInt("ScoreSauvegarde", scoreActuel);
        PlayerPrefs.Save();
        MettreAJourAffichage();
    }

    private void MettreAJourAffichage()
    {
        if (texteScore == null)
        {
            Debug.LogWarning($"score_manager sur « {name} » : la case Texte Score est vide.", this);
            return;
        }
        texteScore.text = "Record   " + scoreActuel + (scoreActuel == 1 ? " mur" : " murs");
    }
}
