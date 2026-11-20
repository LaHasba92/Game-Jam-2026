using System.Collections;
using UnityEngine;

// Le « POW » du jeu (GDD section 6, game feel) : il ecoute les GameEvents et ajoute
// l'arret sur image, le ralenti, le texte qui saute, le rebond du lanceur et la vibration.
// Il ne modifie aucun autre script : on peut le retirer, le jeu marche pareil.
public class FeedbackManager : MonoBehaviour
{
    [Header("Casse d'un mur")]
    [SerializeField] private float hitStop = 0.05f;        // s d'arret sur image
    [SerializeField] private float slowMotion = 0.3f;      // s de ralenti
    [SerializeField] private float slowMotionScale = 0.3f; // vitesse du temps pendant le ralenti
    [SerializeField] private bool vibrate = true;          // telephone seulement

    [Header("Texte qui saute")]
    [SerializeField] private float popupDuration = 0.8f;
    [SerializeField] private int popupFontSize = 90;
    [SerializeField] private Color popupColor = new Color(1f, 0.85f, 0.1f);

    [Header("HUD de debug (en attendant le HUD de Hugo)")]
    [SerializeField] private bool showHud = true;
    [SerializeField] private ProjectileController projectile;   // l'objet Ball
    [SerializeField] private int hudFontSize = 60;

    [Header("Lacher")]
    [SerializeField] private Transform launcherVisual;     // la fronde (facultatif)
    [SerializeField] private float launcherPunch = 1.1f;   // echelle 1,1 puis 1,0
    [SerializeField] private float punchDuration = 0.15f;

    private Coroutine timeRoutine;
    private int combo;   // murs casses depuis le dernier contact avec le sol
    private string popupText;
    private float popupStart = -10f;
    private Vector3 launcherBaseScale = Vector3.one;
    private GUIStyle popupStyle;
    private GUIStyle hudStyle;

    private void OnEnable()
    {
        if (projectile != null)
        {
            projectile.GroundTouched += ResetCombo;
        }
        GameEvents.OnWallBroken += OnWallBroken;
        GameEvents.OnShotFired += OnShotFired;
        GameEvents.OnRunEnded += OnRunEnded;
    }

    private void OnDisable()
    {
        if (projectile != null)
        {
            projectile.GroundTouched -= ResetCombo;
        }
        GameEvents.OnWallBroken -= OnWallBroken;
        GameEvents.OnShotFired -= OnShotFired;
        GameEvents.OnRunEnded -= OnRunEnded;
        Time.timeScale = 1f;   // ne jamais laisser le jeu ralenti
    }

    private void Start()
    {
        if (launcherVisual != null)
        {
            launcherBaseScale = launcherVisual.localScale;
        }
    }

    // --- Casse : arret sur image, ralenti, texte, vibration -----------------------------

    private void OnWallBroken(int wallNumber)
    {
        ++combo;
        ShowPopup(combo == 1 ? "POW !" : $"COMBO x{combo}");

        if (timeRoutine != null)
        {
            StopCoroutine(timeRoutine);
        }
        timeRoutine = StartCoroutine(HitStopThenSlowMotion());

        if (vibrate)
        {
#if UNITY_ANDROID || UNITY_IOS
            Handheld.Vibrate();   // n'existe que sur telephone
#endif
        }
    }

    private IEnumerator HitStopThenSlowMotion()
    {
        // « Realtime » : ces attentes comptent le vrai temps, meme quand le jeu est fige ou ralenti.
        Time.timeScale = 0f;
        yield return new WaitForSecondsRealtime(hitStop);

        Time.timeScale = slowMotionScale;
        yield return new WaitForSecondsRealtime(slowMotion);

        Time.timeScale = 1f;
        timeRoutine = null;
    }

    private void OnRunEnded(RunResult result)
    {
        ShowPopup(result.wallsBroken == 0 ? "Presque !" : $"{result.wallsBroken} MURS !");
    }

    private void ShowPopup(string text)
    {
        popupText = text;
        popupStart = Time.unscaledTime;
    }

    // --- Lacher : la fronde rebondit -------------------------------------------------------

    private void ResetCombo()
    {
        combo = 0;
    }

    private void OnShotFired(ShotInfo shot)
    {
        combo = 0;
        if (launcherVisual != null)
        {
            StartCoroutine(PunchLauncher());
        }
    }

    private IEnumerator PunchLauncher()
    {
        for (float t = 0f; t < punchDuration; t += Time.unscaledDeltaTime)
        {
            float k = Mathf.Lerp(launcherPunch, 1f, t / punchDuration);
            launcherVisual.localScale = launcherBaseScale * k;
            yield return null;
        }
        launcherVisual.localScale = launcherBaseScale;
    }

    // --- Affichage du texte (en attendant l'interface de Hugo) ----------------------------

    private void OnGUI()
    {
        DrawHud();
        float age = Time.unscaledTime - popupStart;
        if (age > popupDuration || string.IsNullOrEmpty(popupText))
        {
            return;
        }
        if (popupStyle == null)
        {
            popupStyle = new GUIStyle(GUI.skin.label)
            {
                alignment = TextAnchor.MiddleCenter,
                fontStyle = FontStyle.Bold,
            };
        }

        // Grossit vite (0 -> 120 %), revient a 100 %, puis s'efface
        float t = age / popupDuration;
        float scale = t < 0.15f ? Mathf.Lerp(0.3f, 1.2f, t / 0.15f) : Mathf.Lerp(1.2f, 1f, (t - 0.15f) / 0.2f);
        float alpha = t < 0.7f ? 1f : 1f - (t - 0.7f) / 0.3f;
        popupStyle.fontSize = Mathf.RoundToInt(popupFontSize * Mathf.Clamp(scale, 0.3f, 1.2f));

        Rect area = new Rect(0f, Screen.height * 0.25f, Screen.width, popupFontSize * 1.5f);
        GUI.color = new Color(0f, 0f, 0f, alpha * 0.6f);                      // ombre
        GUI.Label(new Rect(area.x + 4f, area.y + 4f, area.width, area.height), popupText, popupStyle);
        GUI.color = new Color(popupColor.r, popupColor.g, popupColor.b, alpha);
        GUI.Label(area, popupText, popupStyle);
        GUI.color = Color.white;
    }

    // Murs casses en gros en haut de l'ecran, et la distance dessous, pendant la visee et le vol.
    private void DrawHud()
    {
        if (!showHud || projectile == null)
        {
            return;
        }
        GameState state = GameManager.Instance != null ? GameManager.Instance.CurrentState : GameState.Flying;
        if (state != GameState.Aiming && state != GameState.Flying)
        {
            return;
        }
        if (hudStyle == null)
        {
            hudStyle = new GUIStyle(GUI.skin.label) { alignment = TextAnchor.UpperCenter, fontStyle = FontStyle.Bold };
        }
        hudStyle.fontSize = hudFontSize;
        GUI.color = Color.white;
        GUI.Label(new Rect(0f, 20f, Screen.width, hudFontSize * 1.3f), $"{projectile.WallsBroken} MURS", hudStyle);
        hudStyle.fontSize = hudFontSize / 2;
        GUI.Label(new Rect(0f, 20f + hudFontSize * 1.3f, Screen.width, hudFontSize), $"{Mathf.Max(0f, projectile.Distance):N0} m", hudStyle);
    }
}
