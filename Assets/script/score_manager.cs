using UnityEngine;
using TMPro;

public class ScoreManager : MonoBehaviour
{
    [SerializeField] private TMP_Text texteScore;
    private int scoreActuel;

    void Start()
    {
        // Charge le score sauvegardé (0 par défaut lors de la première partie)
        scoreActuel = PlayerPrefs.GetInt("ScoreSauvegarde", 0);
        MettreAJourAffichage();
    }

    public void AjouterPoints(int points)
    {
        scoreActuel += points;
        // Sauvegarde immédiatement le nouveau score sur l'appareil
        PlayerPrefs.SetInt("ScoreSauvegarde", scoreActuel);
        PlayerPrefs.Save();
        MettreAJourAffichage();
    }

    private void MettreAJourAffichage()
    {
        // Conserve la surbrillance jaune à chaque mise à jour du score
        texteScore.text = "<mark=#FFFF0066>Score : " + scoreActuel + "</mark>";
    }
}