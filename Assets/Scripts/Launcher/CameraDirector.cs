using UnityEngine;

// La camera du jeu, sans Cinemachine : derriere le lanceur pendant la visee, puis elle suit
// le boulet. Quand un mur casse, elle reste un moment sur l'explosion avant de repartir.
public class CameraDirector : MonoBehaviour
{
    [SerializeField] private ProjectileController projectile;
    [SerializeField] private WallController wall;
    [SerializeField] private Vector3 aimOffset = new Vector3(-8f, 4f, 0f);     // derriere le lanceur
    [SerializeField] private Vector3 followOffset = new Vector3(-10f, 4f, 0f); // derriere le boulet
    [SerializeField] private float lookAhead = 6f;
    [SerializeField] private float smoothing = 5f;
    [SerializeField] private float holdOnBreak = 1.2f;   // s a rester sur le mur qui explose
    [SerializeField] private float shakeOnBreak = 0.3f;  // m de tremblement

    private Vector3 holdPoint;
    private float holdUntil;
    private float shake;
    private Vector3 smoothedPosition;

    private JamCameraFx jam;
    private bool frameBoss;
    public Vector3 AimOffset => aimOffset;

    public void FrameTake(Vector3 focus, bool aiming)
    {
        frameBoss = false;
        holdUntil = 0f;
        smoothedPosition = focus + (aiming ? aimOffset : followOffset);
        transform.position = smoothedPosition;
        transform.LookAt(focus + Vector3.right * lookAhead);
    }
    private void Awake()
    {
        smoothedPosition = transform.position;
        if (AimTuning.Current != null && TryGetComponent<Camera>(out _))
            jam = GetComponent<JamCameraFx>() ?? gameObject.AddComponent<JamCameraFx>();
    }

    private void OnEnable()  { if (wall != null) wall.HitResolved += OnWallHit; }
    private void OnDisable() { if (wall != null) wall.HitResolved -= OnWallHit; }

    private void OnWallHit(WallHitResult result)
    {
        if (result.bossHit && projectile != null && !projectile.IsSkipping && AimTuning.Current != null)
        {
            var settings = AimTuning.Current;
            frameBoss = true;
            holdPoint = new Vector3(settings.giantWall.wallDistance, settings.giantWall.bossSize.y * 0.5f, 0f);
            holdUntil = Time.time + Mathf.Max(holdOnBreak, settings.bossResultDelay);
            if (result.broken) shake = shakeOnBreak;
        }
        else if (result.broken && projectile != null && !projectile.IsSkipping)
        {
            frameBoss = false;
            holdPoint = projectile.transform.position;
            holdUntil = Time.time + holdOnBreak;
            shake = shakeOnBreak;
        }
    }

    private void LateUpdate()
    {
        if (jam != null && jam.OverrideView()) { smoothedPosition = transform.position; return; }
        if (projectile == null || Time.deltaTime <= 0f) return;
        if (GameManager.Instance?.CurrentState == GameState.Aiming || GameManager.Instance?.CurrentState == GameState.Home)
        {
            holdUntil = 0f;
            shake = 0f;
        }

        Vector3 target;
        Vector3 lookAt;
        if (Time.time < holdUntil)
        {
            target = frameBoss
                ? new Vector3(holdPoint.x - AimTuning.Current.cameraStopBefore, AimTuning.Current.cameraAltitude, 0f)
                : holdPoint + followOffset;
            lookAt = holdPoint;
        }
        else if (projectile.IsFlying)
        {
            target = projectile.transform.position + followOffset;
            lookAt = projectile.transform.position + Vector3.right * lookAhead;
        }
        else
        {
            target = projectile.transform.position + aimOffset;
            lookAt = projectile.transform.position + Vector3.right * 20f;
        }

        float t = 1f - Mathf.Exp(-smoothing * Time.deltaTime);
        smoothedPosition = Vector3.Lerp(smoothedPosition, target, t);
        transform.position = smoothedPosition + Random.insideUnitSphere * shake + (jam != null ? jam.ShakeOffset : Vector3.zero);
        transform.rotation = Quaternion.Slerp(transform.rotation, Quaternion.LookRotation(lookAt - transform.position), t);
        shake = Mathf.MoveTowards(shake, 0f, Time.deltaTime);
    }
}
