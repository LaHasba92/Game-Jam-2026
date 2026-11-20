using UnityEngine;

public class texte_pulse : MonoBehaviour
{
    [Header("Réglages de l'animation")]
    [SerializeField] private float vitesse = 3f;
    [SerializeField] private float amplitude = 0.1f; // 0.1 = grossit de 10%

    private Vector3 tailleInitiale;

    void Start()
    {
        // On sauvegarde la taille de départ du texte
        tailleInitiale = transform.localScale;
    }

    void Update()
    {
        // Time.unscaledTime permet à l'animation de continuer même si le jeu est en pause (Time.timeScale = 0)
        float facteur = 1f + Mathf.Sin(Time.unscaledTime * vitesse) * amplitude;
        transform.localScale = tailleInitiale * facteur;
    }
}
