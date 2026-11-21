using System.Collections.Generic;
using UnityEngine;

[DisallowMultipleComponent]
internal sealed class ProjectileTrailEffects : MonoBehaviour
{
    private enum Shape { Ribbon, Flame, Bubble, Droplet, Spark, Glow }

    private readonly Dictionary<Shape, Material> materials = new Dictionary<Shape, Material>();
    private TrailRenderer ribbon;
    private TrailRenderer core;
    private ParticleSystem spray;
    private ParticleSystem glints;
    private AudioSource flightAudio;
    private Shader shader;
    private ProjectileTrail configured;
    private bool emitting;

    internal void Begin(ProjectileTrail style)
    {
        Stop(true);
        if (style == ProjectileTrail.None) return;
        if (ribbon == null && !Initialize()) return;
        if (configured != style) Configure(style);

        emitting = true;
        ribbon.emitting = true;
        core.emitting = core.widthMultiplier > 0f;
        spray.Play(false);
        spray.Emit(4);
        glints.Play(false);
        flightAudio.volume = 0f;
        flightAudio.pitch = 1f;
        if (flightAudio.clip != null) flightAudio.Play();
    }

    internal void Stop(bool clear)
    {
        emitting = false;
        if (ribbon == null) return;
        ribbon.emitting = false;
        core.emitting = false;
        var behavior = clear ? ParticleSystemStopBehavior.StopEmittingAndClear : ParticleSystemStopBehavior.StopEmitting;
        spray.Stop(false, behavior);
        glints.Stop(false, behavior);
        if (!clear) return;

        ribbon.Clear();
        core.Clear();
        flightAudio.Stop();
    }

    internal void SetSpeed(float speed)
    {
        if (emitting && flightAudio.isPlaying)
            flightAudio.pitch = Mathf.Lerp(0.94f, 1.08f, Mathf.InverseLerp(15f, 130f, speed));
    }

    private void Update()
    {
        if (flightAudio == null || !flightAudio.isPlaying) return;
        float target = emitting ? 0.34f : 0f;
        flightAudio.volume = Mathf.MoveTowards(flightAudio.volume, target, 2.4f * Time.unscaledDeltaTime);
        if (!emitting && flightAudio.volume <= 0f) flightAudio.Stop();
    }

    private void OnDisable() => Stop(true);

    private void OnDestroy()
    {
        foreach (Material material in materials.Values)
        {
            Destroy(material.mainTexture);
            Destroy(material);
        }
    }

    private bool Initialize()
    {
        shader = Resources.Load<Shader>("ProjectileTrails/TrailEffect");
        if (shader == null || !shader.isSupported) return false;

        ribbon = MakeRibbon("Traînée — ruban", 0);
        core = MakeRibbon("Traînée — cœur", 1);
        spray = MakeParticles("Traînée — particules", 2);
        glints = MakeParticles("Traînée — éclats", 3);
        flightAudio = MakeChild("Traînée — son").AddComponent<AudioSource>();
        flightAudio.playOnAwake = false;
        flightAudio.loop = true;
        flightAudio.spatialBlend = 0.3f;
        flightAudio.minDistance = 6f;
        flightAudio.maxDistance = 45f;
        return true;
    }

    private void Configure(ProjectileTrail style)
    {
        configured = style;
        flightAudio.clip = ProjectileTrails.LoadAudio(style);
        switch (style)
        {
            case ProjectileTrail.Fire:
                StyleRibbon(ribbon, 0.31f, 0.68f, new Color32(255, 145, 47, 232), new Color32(250, 44, 15, 162));
                StyleRibbon(core, 0.18f, 0.28f, new Color32(255, 247, 177, 255), new Color32(255, 160, 38, 148));
                StyleParticles(spray, Shape.Flame, new Color32(255, 234, 114, 240), new Color32(255, 126, 34, 210),
                    rate: 19f, perMeter: 0.65f, lifetime: 0.43f, size: 0.5f, gravity: -0.33f);
                StyleParticles(glints, Shape.Spark, new Color32(255, 233, 133, 255), new Color32(255, 115, 39, 229),
                    rate: 5f, perMeter: 0.48f, lifetime: 0.51f, size: 0.23f, gravity: -0.3f);
                break;
            case ProjectileTrail.Bubbles:
                StyleRibbon(ribbon, 0.26f, 0.35f, new Color32(137, 252, 223, 98), new Color32(108, 210, 255, 40));
                StyleRibbon(core, 0.1f, 0f, Color.clear, Color.clear);
                StyleParticles(spray, Shape.Bubble, new Color32(173, 255, 235, 238), new Color32(230, 163, 255, 223),
                    rate: 18f, perMeter: 0.62f, lifetime: 0.65f, size: 0.66f, gravity: -0.19f);
                StyleParticles(glints, Shape.Bubble, new Color32(196, 253, 255, 235), new Color32(125, 172, 255, 214),
                    rate: 8f, perMeter: 0.45f, lifetime: 0.49f, size: 0.28f, gravity: -0.25f);
                break;
            case ProjectileTrail.Water:
                StyleRibbon(ribbon, 0.30f, 0.73f, new Color32(129, 247, 255, 218), new Color32(27, 105, 237, 145));
                StyleRibbon(core, 0.21f, 0.25f, new Color32(229, 255, 255, 220), new Color32(33, 200, 250, 118));
                StyleParticles(spray, Shape.Droplet, new Color32(141, 246, 255, 238), new Color32(45, 153, 250, 216),
                    rate: 17f, perMeter: 0.65f, lifetime: 0.52f, size: 0.4f, gravity: 0.56f);
                StyleParticles(glints, Shape.Glow, new Color32(219, 255, 255, 225), new Color32(124, 218, 255, 198),
                    rate: 10f, perMeter: 0.46f, lifetime: 0.28f, size: 0.22f, gravity: 0.24f);
                break;
            case ProjectileTrail.Aura:
                StyleRibbon(ribbon, 0.37f, 0.82f, new Color32(209, 139, 255, 193), new Color32(85, 63, 238, 128));
                StyleRibbon(core, 0.22f, 0.22f, new Color32(252, 223, 255, 224), new Color32(206, 100, 246, 116));
                StyleParticles(spray, Shape.Glow, new Color32(240, 154, 255, 185), new Color32(106, 112, 255, 158),
                    rate: 15f, perMeter: 0.53f, lifetime: 0.48f, size: 0.78f, gravity: -0.04f);
                StyleParticles(glints, Shape.Spark, new Color32(255, 238, 255, 240), new Color32(182, 150, 255, 198),
                    rate: 8f, perMeter: 0.36f, lifetime: 0.54f, size: 0.49f, gravity: -0.04f);
                break;
        }
    }

    private GameObject MakeChild(string objectName)
    {
        var child = new GameObject(objectName);
        child.transform.SetParent(transform, false);
        return child;
    }

    private TrailRenderer MakeRibbon(string objectName, int order)
    {
        var renderer = MakeChild(objectName).AddComponent<TrailRenderer>();
        renderer.emitting = false;
        renderer.minVertexDistance = 0.12f;
        renderer.numCapVertices = 2;
        renderer.numCornerVertices = 2;
        renderer.textureMode = LineTextureMode.Stretch;
        renderer.sharedMaterial = MaterialFor(Shape.Ribbon);
        renderer.sortingOrder = order;
        return renderer;
    }

    private ParticleSystem MakeParticles(string objectName, int order)
    {
        GameObject child = MakeChild(objectName);
        child.SetActive(false);
        var particles = child.AddComponent<ParticleSystem>();
        var main = particles.main;
        main.playOnAwake = false;
        main.loop = true;
        main.maxParticles = 96;
        main.simulationSpace = ParticleSystemSimulationSpace.World;
        main.startRotation = new ParticleSystem.MinMaxCurve(-Mathf.PI, Mathf.PI);

        var shape = particles.shape;
        shape.enabled = true;
        shape.shapeType = ParticleSystemShapeType.Sphere;
        shape.radius = 0.24f;

        var fade = particles.colorOverLifetime;
        fade.enabled = true;
        var gradient = new Gradient();
        gradient.SetKeys(new[] { new GradientColorKey(Color.white, 0f), new GradientColorKey(Color.white, 1f) },
            new[] { new GradientAlphaKey(0.68f, 0f), new GradientAlphaKey(1f, 0.11f),
                new GradientAlphaKey(0.72f, 0.47f), new GradientAlphaKey(0f, 1f) });
        fade.color = gradient;

        var renderer = child.GetComponent<ParticleSystemRenderer>();
        renderer.renderMode = ParticleSystemRenderMode.Billboard;
        renderer.sortMode = ParticleSystemSortMode.Distance;
        renderer.sortingOrder = order;
        renderer.maxParticleSize = 0.09f;
        child.SetActive(true);
        return particles;
    }

    private static void StyleRibbon(TrailRenderer renderer, float lifetime, float width, Color head, Color tail)
    {
        renderer.time = lifetime;
        renderer.widthCurve = new AnimationCurve(new Keyframe(0f, 0.62f), new Keyframe(0.17f, 1f),
            new Keyframe(0.62f, 0.55f), new Keyframe(1f, 0f));
        renderer.widthMultiplier = width;
        var colors = new Gradient();
        colors.SetKeys(new[] { new GradientColorKey(head, 0f), new GradientColorKey(tail, 0.62f) },
            new[] { new GradientAlphaKey(head.a, 0f), new GradientAlphaKey(tail.a, 0.59f),
                new GradientAlphaKey(0f, 1f) });
        renderer.colorGradient = colors;
    }

    private void StyleParticles(ParticleSystem particles, Shape shape, Color a, Color b, float rate,
        float perMeter, float lifetime, float size, float gravity)
    {
        var main = particles.main;
        main.startLifetime = new ParticleSystem.MinMaxCurve(lifetime * 0.55f, lifetime);
        main.startSize = new ParticleSystem.MinMaxCurve(size * 0.37f, size);
        main.startSpeed = new ParticleSystem.MinMaxCurve(0.28f, 1.62f);
        main.startColor = new ParticleSystem.MinMaxGradient(a, b);
        main.gravityModifier = gravity;
        var emission = particles.emission;
        emission.rateOverTime = rate;
        emission.rateOverDistance = perMeter;
        particles.GetComponent<ParticleSystemRenderer>().sharedMaterial = MaterialFor(shape);
    }

    private Material MaterialFor(Shape shape)
    {
        if (materials.TryGetValue(shape, out Material material)) return material;

        const int size = 64;
        var texture = new Texture2D(size, size, TextureFormat.RGBA32, false, true)
        {
            name = "Traînée — " + shape,
            filterMode = FilterMode.Bilinear,
            wrapMode = TextureWrapMode.Clamp
        };
        var pixels = new Color32[size * size];
        for (int j = 0; j < size; ++j)
        {
            for (int i = 0; i < size; ++i)
            {
                float x = 2f * (i + 0.5f) / size - 1f;
                float y = 2f * (j + 0.5f) / size - 1f;
                float radius = Mathf.Sqrt(x * x + y * y);
                float opacity = Opacity(shape, x, y, radius);
                pixels[j * size + i] = new Color32(255, 255, 255, (byte)Mathf.RoundToInt(255f * opacity));
            }
        }
        texture.SetPixels32(pixels);
        texture.Apply(false, true);
        material = new Material(shader) { mainTexture = texture };
        materials.Add(shape, material);
        return material;
    }

    private static float Opacity(Shape shape, float x, float y, float radius)
    {
        switch (shape)
        {
            case Shape.Ribbon:
                return Mathf.Pow(Mathf.Clamp01(1f - Mathf.Abs(y)), 1.35f);
            case Shape.Flame:
                float taperedX = x / Mathf.Lerp(1f, 0.4f, (y + 1f) * 0.5f);
                return Mathf.Pow(Mathf.Clamp01(1f - taperedX * taperedX - y * y), 1.35f);
            case Shape.Bubble:
                float rim = Mathf.Clamp01(1f - Mathf.Abs(radius - 0.75f) / 0.16f);
                float highlight = Mathf.Clamp01(1f - ((x + 0.30f) * (x + 0.30f) + (y - 0.40f) * (y - 0.40f)) * 21f);
                return Mathf.Clamp01(0.67f * rim + 0.92f * highlight + 0.12f * Mathf.Clamp01(1f - radius));
            case Shape.Droplet:
                float narrowX = x / Mathf.Lerp(0.67f, 0.18f, (y + 1f) * 0.5f);
                return Mathf.Clamp01((1f - narrowX * narrowX - y * y) * 1.6f);
            case Shape.Spark:
                float cross = Mathf.Min(Mathf.Abs(x), Mathf.Abs(y));
                return Mathf.Clamp01(1f - cross * 7f - radius) * Mathf.Clamp01(1f - radius * 0.85f);
            default:
                return Mathf.Pow(Mathf.Clamp01(1f - radius), 2.2f);
        }
    }
}
