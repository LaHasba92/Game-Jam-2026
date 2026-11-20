using UnityEngine;

// Pool sans collisions : huit éclats de maquette et leur fumée sont réutilisés à chaque plan.
public sealed class JamBurst
{
    private readonly Transform root;
    private readonly Transform[] parts, puffs;
    private readonly Vector3[] directions;
    private readonly AimTuning tuning;
    private float start, spread, size;
    private bool running;

    public JamBurst(Transform parent, AimTuning settings, GameObject chunk, Color color, int count, GameObject[] variants = null)
    {
        tuning = settings;
        root = JamGeometry.Group(parent, "Éclats et poussière");
        parts = new Transform[count];
        puffs = new Transform[count];
        directions = new Vector3[count];
        for (int i = 0; i < count; i++)
        {
            float a = i * Mathf.PI * 2f / count;
            directions[i] = new Vector3(Mathf.Cos(a), 1f + (i % 3) * 0.3f, Mathf.Sin(a));
            GameObject model = variants != null && variants.Length > 0 ? variants[i % variants.Length] : chunk;
            parts[i] = model != null ? Object.Instantiate(model, root).transform :
                JamGeometry.Part(root, "Éclat", PrimitiveType.Cube, Vector3.zero, Vector3.one, color);
            puffs[i] = JamGeometry.Part(root, "Nuage", PrimitiveType.Sphere, Vector3.zero, Vector3.one, settings.smoke);
        }
        root.gameObject.SetActive(false);
    }

    public void Play(Vector3 point, float reach, float pieceSize, Color cloud)
    {
        start = Time.unscaledTime;
        root.position = point;
        spread = reach;
        size = pieceSize;
        root.gameObject.SetActive(true);
        foreach (Transform puff in puffs) JamGeometry.Tint(puff.GetComponent<Renderer>(), cloud);
        running = true;
        Tick();
    }

    public void Tick()
    {
        if (!running) return;
        float t = (Time.unscaledTime - start) / Mathf.Max(0.01f, tuning.burstSeconds);
        if (t >= 1f) { Stop(); return; }
        for (int i = 0; i < parts.Length; i++)
        {
            Vector3 d = directions[i];
            parts[i].position = root.position + (d * t + Vector3.down * (t * t * 1.4f)) * spread;
            parts[i].localRotation = Quaternion.Euler(120f * t, i * 39f + 190f * t, 150f * t);
            parts[i].localScale = Vector3.one * (size * (1f - t));
            puffs[i].position = root.position + (d * 0.32f + Vector3.up * t) * spread * t;
            puffs[i].localScale = Vector3.one * (spread * 0.38f * Mathf.Sin(Mathf.PI * t));
        }
    }

    public void Stop()
    {
        running = false;
        root.gameObject.SetActive(false);
    }
}
