using UnityEngine;
using TMPro;

public class score_manager : MonoBehaviour
{
    [SerializeField] private TMP_Text texteScore;
    private int scoreActuel;

    void Start()
    {
        // Charge le score sauvegard� (0 par d�faut lors de la premi�re partie)
        scoreActuel = PlayerPrefs.GetInt("ScoreSauvegarde", 0);
        MettreAJourAffichage();
    }

    public void AjouterPoints(int points)
    {
        scoreActuel += points;
        // Sauvegarde imm�diatement le nouveau score sur l'appareil
        PlayerPrefs.SetInt("ScoreSauvegarde", scoreActuel);
        PlayerPrefs.Save();
        MettreAJourAffichage();
    }

    private void MettreAJourAffichage()
    {
        // Conserve la surbrillance jaune � chaque mise � jour du score
        if (texteScore == null) return;
        texteScore.text = "<mark=#FFFF0066>Score : " + scoreActuel + "</mark>";
    }
}