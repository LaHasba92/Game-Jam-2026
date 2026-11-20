using UnityEngine;
using TMPro;

public class score_manager : MonoBehaviour
{
    [SerializeField] private TMP_Text texteScore;
    private int scoreActuel;

    // Le meilleur score = le plus de murs casses en une partie (GDD : le but du jeu)
    void OnEnable()  { GameEvents.OnRunEnded += HandleRunEnded; }
    void OnDisable() { GameEvents.OnRunEnded -= HandleRunEnded; }

    void HandleRunEnded(RunResult result)
    {
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
        // Conserve la surbrillance jaune e chaque mise e jour du score
        texteScore.text = "<mark=#FFFF0066>Meilleur score : " + scoreActuel + " murs</mark>";
    }
}