using UnityEngine;

// Le boss se trouve réellement au bout de la piste, même loin du boulet et hors capture.
public class BossWallView : MonoBehaviour
{
    private LevelDefinition wall;
    private Transform body;
    private GameObject[] stages;
    private JamBurst rubble;
    private Camera view;
    private int shown = -1, shownHealth = -1, damage;
    private float hitTime = float.NegativeInfinity;
    private Vector3 hitPoint;
    private GUIStyle label;

    private void Awake()
    {
        AimTuning tuning = AimTuning.Current;
        wall = tuning.giantWall;
        body = JamGeometry.Group(transform, "Mur géant");
        body.position = new Vector3(wall.wallDistance, 0f, 0f);
        stages = new GameObject[wall.crackThresholds.Length + 1];
        for (int i = 0; i < stages.Length; i++)
        {
            GameObject art = wall.bossStageModels != null && i < wall.bossStageModels.Length ? wall.bossStageModels[i] : null;
            stages[i] = art != null ? Instantiate(art, body) : BuildStage(i);
            stages[i].SetActive(false);
        }
        rubble = new JamBurst(transform, tuning, wall.bossDebris, tuning.metal, wall.bossDebrisCount, wall.bossDebrisVariants);
    }

    private GameObject BuildStage(int stage)
    {
        Transform set = JamGeometry.Group(body, $"Fissures {stage}");
        Vector3 s = wall.bossSize;
        JamGeometry.Part(set, "Pierre", PrimitiveType.Cube, new Vector3(s.x / 2f, s.y / 2f, 0f), s, AimTuning.Current.metal);
        // Trois lignes brisées qui apparaissent par palier sur la face avant.
        for (int crack = 0; crack < stage; crack++)
        {
            for (int segment = 0; segment < 3; segment++)
            {
                float z = (crack - 1f) * s.z * 0.23f + (segment % 2) * s.z * 0.04f;
                var part = JamGeometry.Part(set, "Fissure", PrimitiveType.Cube,
                    new Vector3(-0.03f, s.y * (0.78f - segment * 0.15f), z),
                    new Vector3(0.08f, s.y * 0.19f, s.z * 0.019f), Color.black);
                part.localRotation = Quaternion.Euler(segment % 2 == 0 ? 22f : -22f, 0f, 0f);
            }
        }
        return set.gameObject;
    }

    private void OnEnable()
    {
        GameEvents.OnBossWallDamaged += OnHit;
        GameEvents.OnStateChanged += ResetTake;
    }
    private void OnDisable()
    {
        GameEvents.OnBossWallDamaged -= OnHit;
        GameEvents.OnStateChanged -= ResetTake;
    }
    private void ResetTake(GameState from, GameState to)
    {
        if (to != GameState.Aiming && to != GameState.Home) return;
        hitTime = float.NegativeInfinity;
        rubble.Stop();
    }

    private void OnHit(BossImpact hit)
    {
        hitPoint = hit.position;
        damage = hit.damage;
        hitTime = Time.unscaledTime;
        if (hit.remaining == 0)
            rubble.Play(body.position + Vector3.up * wall.bossSize.y / 2f, wall.burstRadius,
                wall.fragmentSize, AimTuning.Current.dust);
        Refresh(hit.remaining);
    }

    private void Refresh(int hp)
    {
        shownHealth = hp;
        int next = hp > 0 ? JamRules.CrackStage(hp, wall.bossMaxHealth, wall.crackThresholds) : -1;
        if (shown == next) return;
        if (shown >= 0) stages[shown].SetActive(false);
        shown = next;
        if (shown >= 0) stages[shown].SetActive(true);
    }

    private void LateUpdate()
    {
        int hp = BossProgress.Health(wall);
        if (hp != shownHealth) Refresh(hp);
        rubble.Tick();
        if (view == null) view = Camera.main;
    }

    private void OnGUI()
    {
        if (view == null || Event.current.type != EventType.Repaint) return;
        Vector3 top = body.position + Vector3.up * (wall.bossSize.y + wall.labelOffset);
        Vector3 p = view.WorldToScreenPoint(top);
        float width = Screen.width * wall.barWidthFraction;
        if (label == null) label = new GUIStyle(GUI.skin.label) { alignment = TextAnchor.MiddleCenter };
        label.fontSize = Mathf.Max(14, Mathf.RoundToInt(width * 0.12f));
        if (shownHealth > 0 && p.z > 0f && p.z < wall.labelRange)
        {
            Rect bar = new Rect(p.x - width / 2f, Screen.height - p.y, width, label.fontSize);
            Color color = GUI.color;
            GUI.color = Color.black;
            GUI.DrawTexture(bar, Texture2D.whiteTexture);
            bar.width *= (float)shownHealth / wall.bossMaxHealth;
            GUI.color = AimTuning.Current.green;
            GUI.DrawTexture(bar, Texture2D.whiteTexture);
            GUI.color = color;
            GUI.Label(new Rect(p.x - width, bar.y - label.fontSize * 2f, width * 2f, label.fontSize * 2f),
                $"{shownHealth} / {wall.bossMaxHealth} PV", label);
        }
        float t = (Time.unscaledTime - hitTime) / Mathf.Max(0.01f, wall.damageLabelSeconds);
        if (t >= 1f) return;
        Vector3 pop = view.WorldToScreenPoint(hitPoint + Vector3.up * (wall.labelOffset + t * wall.damageRise));
        if (pop.z > 0f) GUI.Label(new Rect(pop.x - width, Screen.height - pop.y, width * 2f, label.fontSize * 2f),
            $"−{damage}", label);
    }
}
