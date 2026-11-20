
using System.Collections;
using UnityEngine;
using UnityEngine.UI;

public class SlingshotEvolution : MonoBehaviour
{
    [Header("Modeles")]
    public GameObject slingshotNormal;
    public GameObject slingshotEvolved;

    [Header("Effets")]
    public Image flashImage;
    public Light evolutionLight;

    [Header("Durees")]
    public float chargeDuration = 1f;
    public float flashDuration = 0.2f;
    public float revealDuration = 0.8f;

    private bool isEvolving = false;
    private bool hasEvolved = false;

    void Start()
    {
        slingshotNormal.SetActive(true);
        slingshotEvolved.SetActive(false);

        evolutionLight.intensity = 0f;
        SetFlashAlpha(0f);
    }

    void Update()
    {
        // Touche de test : E
        if (Input.GetKeyDown(KeyCode.E))
        {
            StartEvolution();
        }
    }

    public void StartEvolution()
    {
        if (isEvolving || hasEvolved)
        {
            return;
        }

        StartCoroutine(Evolve());
    }

    IEnumerator Evolve()
    {
        isEvolving = true;

        // Augmentation progressive de la lumiere
        float elapsed = 0f;

        while (elapsed < chargeDuration)
        {
            elapsed += Time.deltaTime;
            float t = Mathf.Clamp01(elapsed / chargeDuration);

            evolutionLight.intensity = Mathf.Lerp(0f, 15f, t);
            yield return null;
        }

        // Flash blanc
        SetFlashAlpha(1f);
        yield return new WaitForSeconds(flashDuration);

        // Remplacement du lance-pierre
        slingshotNormal.SetActive(false);
        slingshotEvolved.SetActive(true);

        // Disparition progressive du flash
        elapsed = 0f;

        while (elapsed < revealDuration)
        {
            elapsed += Time.deltaTime;
            float t = Mathf.Clamp01(elapsed / revealDuration);

            SetFlashAlpha(1f - t);
            evolutionLight.intensity = Mathf.Lerp(15f, 0f, t);

            yield return null;
        }

        SetFlashAlpha(0f);
        evolutionLight.intensity = 0f;

        hasEvolved = true;
        isEvolving = false;
    }

    void SetFlashAlpha(float alpha)
    {
        Color color = flashImage.color;
        color.a = alpha;
        flashImage.color = color;
    }
}
