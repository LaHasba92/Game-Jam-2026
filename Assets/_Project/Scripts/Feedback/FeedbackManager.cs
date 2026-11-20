using System.Collections;
using UnityEngine;

// Le CameraDirector gère déjà la secousse. Ici on suspend uniquement le temps du jeu.
public class FeedbackManager : MonoBehaviour
{
    [SerializeField] private ProjectileController projectile;
    [SerializeField, Min(0f)] private float freezeSeconds = 0.05f;
    private Coroutine freeze;
    private float previousScale;

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
            RestoreTime();
        }
    }

    private void OnShot(ShotInfo shot)
    {
#if UNITY_ANDROID
        Handheld.Vibrate();
#endif
    }

    private void OnWallHit(WallHitResult hit)
    {
        if (!hit.broken || freezeSeconds <= 0f || Time.timeScale <= 0f ||
            (projectile != null && projectile.IsSkipping))
            return;

        freeze = StartCoroutine(Freeze());
    }

    private IEnumerator Freeze()
    {
        previousScale = Time.timeScale;
        Time.timeScale = 0f;
        yield return new WaitForSecondsRealtime(freezeSeconds);
        RestoreTime();
    }

    private void RestoreTime()
    {
        if (Time.timeScale == 0f) Time.timeScale = previousScale;
        freeze = null;
    }
}
