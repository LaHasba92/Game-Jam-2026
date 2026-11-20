using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
using UnityEngine.InputSystem;

// CameraDirector appelle OverrideView une fois par image, avant son suivi habituel.
public class JamCameraFx : MonoBehaviour
{
    private AimTuning cfg;
    private Volume volume;
    private VolumeProfile profile;
    private UniversalAdditionalCameraData data;
    private bool previousPost;
    private LayerMask originalMask;
    private float shakeUntil;
    private Vector3 flyStart;
    private float flyAt;
    private bool tour, free;
    private WorldStreamer world;

    private void Awake()
    {
        cfg = AimTuning.Current;
        world = FindFirstObjectByType<WorldStreamer>();
        data = GetComponent<Camera>().GetUniversalAdditionalCameraData();
        previousPost = data.renderPostProcessing;
        var host = new GameObject("Désaturation à l'impact");
        host.transform.SetParent(transform, false);
        originalMask = data.volumeLayerMask;
        int mask = originalMask.value;
        int layer = 0;
        while (layer < 31 && (mask & (1 << layer)) == 0) layer++;
        host.layer = mask == 0 ? 0 : layer;
        if (mask == 0) data.volumeLayerMask = 1 << host.layer;
        volume = host.AddComponent<Volume>();
        volume.isGlobal = true;
        volume.priority = cfg.volumePriority;
        volume.weight = 0f;
        profile = ScriptableObject.CreateInstance<VolumeProfile>();
        profile.Add<ColorAdjustments>(false).saturation.Override(-100f * cfg.desaturation);
        volume.sharedProfile = profile;
    }

    private void OnEnable()
    {
        GameEvents.OnLauncherBroken += Shake;
        GameEvents.OnCannonExploded += Shake;
        GameEvents.OnGameOver += GameOver;
        GameEvents.OnStateChanged += State;
    }
    private void OnDisable()
    {
        GameEvents.OnLauncherBroken -= Shake;
        GameEvents.OnCannonExploded -= Shake;
        GameEvents.OnGameOver -= GameOver;
        GameEvents.OnStateChanged -= State;
        if (volume != null) volume.weight = 0f;
        if (data != null) data.renderPostProcessing = previousPost;
    }
    private void OnDestroy()
    {
        if (data != null) data.volumeLayerMask = originalMask;
        if (profile != null) Destroy(profile);
    }
    private void Shake() => shakeUntil = Time.unscaledTime + cfg.shakeSeconds;
    private void GameOver(RunResult run)
    {
        if (run.reason != RunEndReason.WallIntact && run.reason != RunEndReason.BossSurvived) return;
        data.renderPostProcessing = true;
        volume.weight = 1f;
        SfxManager.Play(cfg.tap, cfg.soundVolume);
    }
    private void State(GameState from, GameState to)
    {
        if (to == GameState.Result) return;
        volume.weight = 0f;
        data.renderPostProcessing = previousPost;
        shakeUntil = 0f;
        tour = free = false;
    }

    public Vector3 ShakeOffset => Time.unscaledTime < shakeUntil ? Random.insideUnitSphere * cfg.cameraShake : Vector3.zero;
    public void OpeningShot()
    {
        if (!JamSave.Preview) return;
        free = false; tour = true;
        flyAt = Time.unscaledTime;
        var launcher = FindFirstObjectByType<LauncherController>();
        Vector3 origin = launcher != null && launcher.LaunchPoint != null ? launcher.LaunchPoint.position : Vector3.zero;
        flyStart = origin + GetComponent<CameraDirector>().AimOffset;
        transform.position = flyStart;
        if (world != null) world.RewindTo(flyStart.x);
    }
    public void FreeCamera() { if (JamSave.Preview) { free = true; tour = false; } }

    public bool OverrideView()
    {
        if (!JamSave.Preview) { tour = free = false; return false; }
        if (tour)
        {
            var boss = cfg.giantWall;
            Vector3 focus = new Vector3(boss.wallDistance, boss.bossSize.y * 0.5f, 0f);
            Vector3 stop = new Vector3(boss.wallDistance - cfg.cameraStopBefore, cfg.cameraAltitude, 0f);
            float t = Mathf.SmoothStep(0f, 1f, (Time.unscaledTime - flyAt) / Mathf.Max(0.01f, cfg.flySeconds));
            transform.position = Vector3.Lerp(flyStart, stop, t);
            transform.LookAt(focus);
            if (world != null) world.AdvanceTo(transform.position.x);
            return true; // conserve le cadre final jusqu'au prochain plan
        }
        if (!free) return false;
        var k = Keyboard.current;
        Vector3 move = Vector3.zero;
        if (k != null)
        {
            if (k.upArrowKey.isPressed) move += transform.forward;
            if (k.downArrowKey.isPressed) move -= transform.forward;
            if (k.leftArrowKey.isPressed) move -= transform.right;
            if (k.rightArrowKey.isPressed) move += transform.right;
        }
        Pointer pointer = Touchscreen.current != null && Touchscreen.current.primaryTouch.press.isPressed
            ? (Pointer)Touchscreen.current : Mouse.current;
        if (!JamCaptureMenu.IsPanelOpen && pointer != null && pointer.press.isPressed)
        {
            Vector2 delta = pointer.delta.ReadValue();
            transform.Rotate(Vector3.up, delta.x * cfg.cameraSensitivity, Space.World);
            transform.Rotate(Vector3.right, -delta.y * cfg.cameraSensitivity);
            if (pointer is Touchscreen) move += transform.forward;
        }
        transform.position += move * (cfg.freeCameraSpeed * Time.unscaledDeltaTime);
        if (world != null) world.AdvanceTo(transform.position.x);
        return true;
    }
}
