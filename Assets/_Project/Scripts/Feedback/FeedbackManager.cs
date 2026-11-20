using System.Collections;
using UnityEngine;

// Le CameraDirector gère déjà la secousse. Ici on suspend uniquement le temps du jeu.
public class FeedbackManager : MonoBehaviour
{
    [SerializeField] private ProjectileController projectile;
    [SerializeField, Min(0f)] private float freezeSeconds = 0.05f;
    private Coroutine freeze;
    private static int timeHolds;
    private static float previousScale;

    private void OnEnable()
    {
        GameEvents.OnShotFired += OnShot;
        GameEvents.OnWallHit += OnWallHit;
    }

    private void OnDisable()
    {
        GameEvents.OnShotFired -= OnShot;
        GameEvents.OnWallHit -= OnWallHit;
        if (freeze != null)
        {
            StopCoroutine(freeze);
            freeze = null;
            ReleaseTime();
        }
    }

    private void OnShot(ShotInfo shot)
    {
#if UNITY_ANDROID && !UNITY_EDITOR
        Handheld.Vibrate();
#endif
    }

    private void OnWallHit(WallHitResult hit)
    {
        if (!hit.broken || freezeSeconds <= 0f || freeze != null || Time.timeScale <= 0f ||
            (projectile != null && projectile.IsSkipping))
            return;

        freeze = StartCoroutine(Freeze());
    }

    private IEnumerator Freeze()
    {
        HoldTime();
        yield return new WaitForSecondsRealtime(freezeSeconds);
        freeze = null;
        ReleaseTime();
    }

    // Le gel d'impact et la pause UI peuvent se chevaucher.
    internal static void HoldTime()
    {
        if (timeHolds == 0) previousScale = Time.timeScale;
        timeHolds++;
        Time.timeScale = 0f;
    }

    internal static void ReleaseTime()
    {
        timeHolds--;
        if (timeHolds == 0 && Time.timeScale == 0f) Time.timeScale = previousScale;
    }
}
