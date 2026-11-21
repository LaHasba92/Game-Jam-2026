using System.Collections;
using UnityEngine;

// Affiche le modele 3D du lanceur equipe : fronde, ..., canon.
// A mettre sur l'objet Launcher. « Models » : un modele par lanceur, dans l'ordre
// (0 = Fronde ... 4 = Canon). Une case vide = on garde le modele precedent.
// Il lit le lanceur equipe dans PlayerPrefs (meme cle que RunUpgrades de Dev).
//
// Changement de lanceur = animation d'evolution (facon Pokemon) : l'ancien lanceur devient blanc et
// tourne de plus en plus vite, sa silhouette alterne avec celle du nouveau, puis flash et apparition.
// L'animation se joue au debut de la partie suivante (START), quand le lanceur est a l'ecran.
public class LauncherVisuals : MonoBehaviour
{
    public const string SelectedKey = "CanonPOW.SelectedLauncher";

    [SerializeField] private GameObject[] models;     // enfants de Launcher : Lanceur_Slingshot, Canon...

    [Header("Evolution")]
    [SerializeField] private float duration = 5f;             // s
    [SerializeField] private float maxSpinSpeed = 1440f;      // degres/s a la fin
    [SerializeField] private float startSwapInterval = 0.4f;  // s entre deux alternances au debut...
    [SerializeField] private float endSwapInterval = 0.04f;   // ...et a la fin (de plus en plus vite)
    [SerializeField] private Color glowColor = Color.white;

    [Header("Aura et rayons de lumiere")]
    [SerializeField] private Color auraColor = new Color(1f, 0.92f, 0.55f);
    [SerializeField] private int rayCount = 14;
    [SerializeField] private float rayMaxLength = 7f;          // m, a la fin de l'animation
    [SerializeField] private float lightMaxIntensity = 25f;    // la lueur autour du lanceur
    [SerializeField] private float auraHeight = 1.2f;          // hauteur du centre de l'aura au-dessus du sol

    [Header("Son")]
    [SerializeField] private AudioClip evolutionSound;         // SFX_Evolution
    [SerializeField, Range(0f, 1f)] private float soundVolume = 0.9f;
    [SerializeField] private bool matchSoundLength = true;     // l'animation dure autant que le son

    public static bool IsEvolving { get; private set; }

    private int shown = -1;
    private int pending = -1;          // nouveau lanceur en attente d'evolution
    private void OnEnable()  { GameEvents.OnStateChanged += OnStateChanged; }
    private void OnDisable() { GameEvents.OnStateChanged -= OnStateChanged; IsEvolving = false; }

    private void Update()
    {
        int selected = PlayerPrefs.GetInt(SelectedKey, 0);   // un simple int : pas cher a lire
        if (shown < 0)
        {
            Show(ModelFor(selected), selected);               // premier affichage : pas d'animation
        }
        else if (selected != shown && selected != pending && !IsEvolving)
        {
            if (ModelFor(selected) == ModelFor(shown))
            {
                shown = selected;                             // meme modele (case vide) : rien a animer
            }
            else
            {
                pending = selected;                           // on attend que le lanceur soit a l'ecran
            }
        }
    }

    // START / REJOUER : le joueur regarde le lanceur, c'est le moment de l'evolution
    private void OnStateChanged(GameState from, GameState to)
    {
        if (to == GameState.Aiming && pending >= 0 && !IsEvolving)
        {
            StartCoroutine(Evolve(ModelFor(shown), ModelFor(pending), pending));
            pending = -1;
        }
    }

    private IEnumerator Evolve(int fromModel, int toModel, int newIndex)
    {
        IsEvolving = true;
        LauncherController launcher = GetComponent<LauncherController>();
        if (launcher != null) launcher.enabled = false;       // pas de tir pendant l'animation

        GameObject from = models[fromModel];
        GameObject to = models[toModel];
        Quaternion fromRotation = from.transform.localRotation;
        Quaternion toRotation = to.transform.localRotation;
        Vector3 toScale = to.transform.localScale;

        float length = duration;
        if (evolutionSound != null)
        {
            AudioSource.PlayClipAtPoint(evolutionSound, Camera.main != null ? Camera.main.transform.position : transform.position, soundVolume);
            if (matchSoundLength) length = Mathf.Max(1f, evolutionSound.length - 0.5f);   // son de 5 s : flash a 4,5 s, puis 0,5 s d'apparition
        }
        Aura aura = new Aura(transform, auraColor, rayCount, auraHeight);

        float swapTimer = 0f;
        bool showingNew = false;
        float angle = 0f;
        for (float t = 0f; t < length; t += Time.unscaledDeltaTime)
        {
            float k = t / length;                              // 0 -> 1
            aura.Update(k, rayMaxLength, lightMaxIntensity, Time.unscaledDeltaTime);
            angle += Mathf.Lerp(90f, maxSpinSpeed, k * k) * Time.unscaledDeltaTime;   // accelere
            Tint(from, Mathf.Clamp01(k * 3f));                 // devient blanc dans le premier tiers
            Tint(to, 1f);

            // Les silhouettes alternent de plus en plus vite
            swapTimer += Time.unscaledDeltaTime;
            float interval = Mathf.Lerp(startSwapInterval, endSwapInterval, k);
            if (k > 0.25f && swapTimer >= interval)
            {
                swapTimer = 0f;
                showingNew = !showingNew;
            }
            from.SetActive(!showingNew);
            to.SetActive(showingNew);
            from.transform.localRotation = fromRotation * Quaternion.Euler(0f, angle, 0f);
            to.transform.localRotation = toRotation * Quaternion.Euler(0f, angle, 0f);
            yield return null;
        }

        aura.Flash(lightMaxIntensity * 2f, rayMaxLength * 1.4f);

        // Flash final : le nouveau lanceur apparait, blanc, puis reprend ses couleurs en grossissant un peu
        from.SetActive(false);
        Tint(from, 0f);
        from.transform.localRotation = fromRotation;
        to.SetActive(true);
        to.transform.localRotation = toRotation;
        for (float t = 0f; t < 0.5f; t += Time.unscaledDeltaTime)
        {
            float k = t / 0.5f;
            Tint(to, k < 0.4f ? 1f : 0f);   // reste blanc un instant, puis reprend ses couleurs
            aura.FadeOut(1f - k);
            to.transform.localScale = toScale * Mathf.Lerp(1.3f, 1f, k);
            yield return null;
        }
        Tint(to, 0f);
        to.transform.localScale = toScale;
        aura.Destroy();

        Show(toModel, newIndex);
        if (launcher != null) launcher.enabled = true;
        IsEvolving = false;
    }

    // amount 0 = couleurs normales ; au-dela de 0,5 le modele passe en silhouette blanche unie.
    // On echange temporairement ses materiaux (les originaux sont gardes et remis a la fin).
    private readonly System.Collections.Generic.Dictionary<Renderer, Material[]> originals =
        new System.Collections.Generic.Dictionary<Renderer, Material[]>();
    private Material whiteMaterial;

    private void Tint(GameObject model, float amount)
    {
        if (whiteMaterial == null)
        {
            Shader unlit = Shader.Find("Universal Render Pipeline/Unlit");
            whiteMaterial = new Material(unlit != null ? unlit : Shader.Find("Unlit/Color"));
            whiteMaterial.color = glowColor;
            if (whiteMaterial.HasProperty("_BaseColor")) whiteMaterial.SetColor("_BaseColor", glowColor);
        }
        bool white = amount > 0.5f;
        foreach (Renderer r in model.GetComponentsInChildren<Renderer>(true))
        {
            if (white)
            {
                if (!originals.ContainsKey(r))
                {
                    originals[r] = r.sharedMaterials;
                    Material[] whites = new Material[r.sharedMaterials.Length];
                    for (int i = 0; i < whites.Length; ++i) whites[i] = whiteMaterial;
                    r.sharedMaterials = whites;
                }
            }
            else if (originals.TryGetValue(r, out Material[] saved))
            {
                r.sharedMaterials = saved;
                originals.Remove(r);
            }
        }
    }

    private int ModelFor(int index)
    {
        int modelIndex = Mathf.Clamp(index, 0, models.Length - 1);
        while (modelIndex > 0 && models[modelIndex] == null)
        {
            --modelIndex;
        }
        return modelIndex;
    }

    private void Show(int modelIndex, int launcherIndex)
    {
        shown = launcherIndex;
        for (int i = 0; i < models.Length; ++i)
        {
            if (models[i] != null)
            {
                models[i].SetActive(i == modelIndex);
            }
        }
    }

    // L'aura : des rayons de lumiere (fines barres qui tournent et s'allongent) + une lumiere qui monte.
    // Tout est cree au debut de l'animation et detruit a la fin : rien a preparer dans la scene.
    private class Aura
    {
        private readonly Transform root;
        private readonly Transform[] rays;
        private readonly Light glow;
        private readonly Material material;
        private float spin;

        public Aura(Transform parent, Color color, int count, float height)
        {
            root = new GameObject("Aura_Evolution").transform;
            root.SetParent(parent, false);
            root.localPosition = new Vector3(0f, height, 0f);

            Shader unlit = Shader.Find("Universal Render Pipeline/Unlit");
            material = new Material(unlit != null ? unlit : Shader.Find("Unlit/Color"));
            material.color = color;
            if (material.HasProperty("_BaseColor")) material.SetColor("_BaseColor", color);

            rays = new Transform[count];
            for (int i = 0; i < count; ++i)
            {
                Transform pivot = new GameObject("Rayon").transform;
                pivot.SetParent(root, false);
                // Rayons repartis en etoile, un peu inclines vers le haut et vers le bas
                pivot.localRotation = Quaternion.Euler((i % 3 - 1) * 25f, 360f * i / count, 0f);
                GameObject beam = GameObject.CreatePrimitive(PrimitiveType.Cube);
                Object.Destroy(beam.GetComponent<Collider>());
                beam.GetComponent<Renderer>().sharedMaterial = material;
                beam.GetComponent<Renderer>().shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
                beam.transform.SetParent(pivot, false);
                rays[i] = beam.transform;
                SetRay(rays[i], 0f, 0.05f);
            }

            glow = new GameObject("Lueur").AddComponent<Light>();
            glow.transform.SetParent(root, false);
            glow.type = LightType.Point;
            glow.color = color;
            glow.range = 10f;
            glow.intensity = 0f;
        }

        // k : avancement de 0 a 1. Les rayons apparaissent, s'allongent, tournent de plus en plus vite.
        public void Update(float k, float maxLength, float maxIntensity, float dt)
        {
            spin += Mathf.Lerp(30f, 400f, k * k) * dt;
            root.localRotation = Quaternion.Euler(0f, spin, 0f);
            for (int i = 0; i < rays.Length; ++i)
            {
                // chaque rayon pulse un peu, avec un decalage : l'aura « vibre »
                float pulse = 0.75f + 0.25f * Mathf.Sin(Time.unscaledTime * 18f + i * 1.7f);
                float appear = Mathf.Clamp01(k * 2.5f - i / (float)rays.Length);   // ils arrivent un par un
                SetRay(rays[i], maxLength * k * pulse * appear, Mathf.Lerp(0.04f, 0.12f, k));
            }
            glow.intensity = maxIntensity * k * k * (0.85f + 0.15f * Mathf.Sin(Time.unscaledTime * 25f));
        }

        public void Flash(float intensity, float length)
        {
            glow.intensity = intensity;
            foreach (Transform ray in rays) SetRay(ray, length, 0.2f);
        }

        public void FadeOut(float amount)
        {
            glow.intensity *= amount;
            foreach (Transform ray in rays)
            {
                Vector3 s = ray.localScale;
                SetRay(ray, s.z * amount, s.x * amount);
            }
        }

        public void Destroy()
        {
            Object.Destroy(root.gameObject);
            Object.Destroy(material);
        }

        // Un rayon part du centre vers l'exterieur (axe Z du pivot)
        private static void SetRay(Transform ray, float length, float thickness)
        {
            ray.localScale = new Vector3(thickness, thickness, Mathf.Max(0.001f, length));
            ray.localPosition = new Vector3(0f, 0f, length * 0.5f);
        }
    }
}
