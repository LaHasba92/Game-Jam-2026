using UnityEngine;

/// <summary>
/// Add to the campfire GameObject. Only the child particle systems and light animate;
/// the original FBX mesh and its transform are never modified.
/// </summary>
public class CampfireFlames : MonoBehaviour
{
    [Header("Position of the fire relative to the campfire")]
    [SerializeField] private Vector3 flameOffset = new Vector3(0f, 0.35f, 0f);
    [SerializeField, Min(0.1f)] private float flameHeight = 1.1f;
    [SerializeField, Min(0.05f)] private float flameWidth = 0.38f;

    [Header("Light")]
    [SerializeField] private bool addLight = true;
    [SerializeField, Min(0f)] private float lightIntensity = 2.5f;
    [SerializeField, Min(0.1f)] private float lightRange = 5f;

    private Light fireLight;
    private float flickerSeed;

    private void Start()
    {
        flickerSeed = Random.Range(0f, 100f);
        CreateFlames("Flames - orange", 65f, 0.8f,
            new Color(1f, 0.24f, 0.02f, 0.9f), flameWidth, flameHeight);
        CreateFlames("Flames - yellow", 45f, 0.65f,
            new Color(1f, 0.8f, 0.12f, 0.95f), flameWidth * 0.65f, flameHeight * 0.75f);
        CreateEmbers();

        if (addLight)
        {
            GameObject lightObject = new GameObject("Fire Light");
            lightObject.transform.SetParent(transform, false);
            lightObject.transform.localPosition = flameOffset + Vector3.up * 0.35f;
            fireLight = lightObject.AddComponent<Light>();
            fireLight.type = LightType.Point;
            fireLight.color = new Color(1f, 0.45f, 0.12f);
            fireLight.range = lightRange;
            fireLight.intensity = lightIntensity;
            fireLight.shadows = LightShadows.None;
        }
    }

    private void Update()
    {
        if (fireLight == null) return;
        float t = Time.time + flickerSeed;
        float flicker = 0.85f + 0.1f * Mathf.Sin(t * 13f)
            + 0.05f * Mathf.Sin(t * 23.7f);
        fireLight.intensity = lightIntensity * flicker;
    }

    private void CreateFlames(string objectName, float rate, float lifetime,
        Color color, float width, float height)
    {
        GameObject child = new GameObject(objectName);
        child.transform.SetParent(transform, false);
        child.transform.localPosition = flameOffset;

        ParticleSystem ps = child.AddComponent<ParticleSystem>();
        var main = ps.main;
        main.loop = true;
        main.playOnAwake = true;
        main.startLifetime = new ParticleSystem.MinMaxCurve(lifetime * 0.7f, lifetime);
        main.startSpeed = new ParticleSystem.MinMaxCurve(height / lifetime * 0.65f,
            height / lifetime);
        main.startSize = new ParticleSystem.MinMaxCurve(width * 0.35f, width * 0.7f);
        main.startColor = color;
        main.simulationSpace = ParticleSystemSimulationSpace.Local;
        main.maxParticles = 180;

        var emission = ps.emission;
        emission.rateOverTime = rate;

        var shape = ps.shape;
        shape.shapeType = ParticleSystemShapeType.Circle;
        shape.radius = width * 0.45f;
        shape.rotation = new Vector3(-90f, 0f, 0f);

        var size = ps.sizeOverLifetime;
        size.enabled = true;
        AnimationCurve sizeCurve = new AnimationCurve(
            new Keyframe(0f, 0.35f),
            new Keyframe(0.25f, 1f),
            new Keyframe(0.65f, 0.65f),
            new Keyframe(1f, 0f));
        size.size = new ParticleSystem.MinMaxCurve(1f, sizeCurve);

        var colorOverLife = ps.colorOverLifetime;
        colorOverLife.enabled = true;
        Gradient gradient = new Gradient();
        gradient.SetKeys(
            new[] {
                new GradientColorKey(Color.white, 0f),
                new GradientColorKey(new Color(1f, 0.7f, 0.3f), 0.55f),
                new GradientColorKey(new Color(0.8f, 0.1f, 0.01f), 1f)
            },
            new[] {
                new GradientAlphaKey(0f, 0f),
                new GradientAlphaKey(0.9f, 0.12f),
                new GradientAlphaKey(0.75f, 0.55f),
                new GradientAlphaKey(0f, 1f)
            });
        colorOverLife.color = gradient;

        var noise = ps.noise;
        noise.enabled = true;
        noise.strength = width * 0.75f;
        noise.frequency = 0.9f;
        noise.scrollSpeed = 1.2f;
        noise.damping = true;

        ParticleSystemRenderer renderer = child.GetComponent<ParticleSystemRenderer>();
        renderer.renderMode = ParticleSystemRenderMode.Stretch;
        renderer.velocityScale = 0.3f;
        renderer.lengthScale = 2.5f;
        renderer.material = CreateParticleMaterial();
        ps.Play();
    }

    private void CreateEmbers()
    {
        GameObject child = new GameObject("Flying Embers");
        child.transform.SetParent(transform, false);
        child.transform.localPosition = flameOffset;
        ParticleSystem ps = child.AddComponent<ParticleSystem>();
        var main = ps.main;
        main.loop = true;
        main.startLifetime = new ParticleSystem.MinMaxCurve(0.7f, 1.7f);
        main.startSpeed = new ParticleSystem.MinMaxCurve(0.7f, 1.7f);
        main.startSize = new ParticleSystem.MinMaxCurve(0.015f, 0.045f);
        main.startColor = new Color(1f, 0.5f, 0.1f);
        main.maxParticles = 70;
        var emission = ps.emission;
        emission.rateOverTime = 12f;
        var shape = ps.shape;
        shape.shapeType = ParticleSystemShapeType.Circle;
        shape.radius = flameWidth * 0.4f;
        shape.rotation = new Vector3(-90f, 0f, 0f);
        var noise = ps.noise;
        noise.enabled = true;
        noise.strength = 0.25f;
        noise.frequency = 0.5f;
        var colorOverLife = ps.colorOverLifetime;
        colorOverLife.enabled = true;
        Gradient fade = new Gradient();
        fade.SetKeys(
            new[] { new GradientColorKey(Color.white, 0f),
                    new GradientColorKey(new Color(1f, 0.2f, 0f), 1f) },
            new[] { new GradientAlphaKey(1f, 0f),
                    new GradientAlphaKey(0f, 1f) });
        colorOverLife.color = fade;
        child.GetComponent<ParticleSystemRenderer>().material = CreateParticleMaterial();
        ps.Play();
    }

    private Material CreateParticleMaterial()
    {
        Shader shader = Shader.Find(
            "Universal Render Pipeline/Particles/Unlit"
        );

        if (shader == null)
        {
            Debug.LogError("Shader URP introuvable !");
            return null;
        }

        Material material = new Material(shader);
        material.SetColor("_BaseColor", Color.white);

        return material;
    }
}
