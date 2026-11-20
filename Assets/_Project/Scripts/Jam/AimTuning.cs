using UnityEngine;

// Fiche livrée dans Resources pour la scène Game, les copies et les builds.
[CreateAssetMenu(menuName = "CanonPOW/Réglages de la jam")]
public class AimTuning : ScriptableObject
{
    private static AimTuning current;
    public static AimTuning Current => current != null ? current : current = Resources.Load<AimTuning>("CanonPowJam");

    public LevelDefinition giantWall;
    [Header("Lanceurs (indices de la boutique, à partir de zéro)")]
    public int slingIndex, giantSlingIndex = 1, cannonIndex = 4;
    public bool giantSlingCanBreak;
    [Range(0.01f, 0.45f)] public float redWidth = 0.12f;
    [Min(0.02f)] public float optimalHold = 1.4f, maximumHold = 2.4f, warningTime = 0.8f;
    [Range(0f, 1f)] public float warningAtEntry = 0.35f;
    public float minimumWarning = 0.15f;
    public float shakeMeters = 0.07f, shakeHertz = 18f, cannonSwell = 0.2f, elasticStretch = 0.6f;
    public Color red = new Color(1f, 0.12f, 0.09f), green = new Color(0.27f, 1f, 0.42f);
    [Header("Mise en scène")]
    [Range(6, 10)] public int debrisCount = 8;
    public float burstSeconds = 0.85f, burstSpread = 3f, pieceSize = 0.23f;
    public float transitionSeconds = 2f, transitionHeight = 4f, revealBounce = 0.65f;
    [Range(0.05f, 0.4f)] public float confettiPart = 0.2f;
    [Range(0.45f, 0.9f)] public float landingPart = 0.65f;
    public float flashSeconds = 0.12f;
    public float ballDropSeconds = 0.55f, ballDropForward = 0.8f;
    public float cameraShake = 0.15f, shakeSeconds = 0.35f, resultDelay = 0.65f, bossResultDelay = 1.3f;
    [Range(0.01f, 1f)] public float missTimeScale = 0.28f;
    [Range(0f, 1f)] public float desaturation = 0.85f, sootAlpha = 0.32f, flashAlpha = 0.45f;
    public Color wood = new Color(0.66f, 0.36f, 0.15f), metal = new Color(0.2f, 0.31f, 0.46f);
    public Color dust = new Color(0.86f, 0.78f, 0.65f), smoke = new Color(0.52f, 0.58f, 0.64f);
    public Material greyboxMaterial;
    public GameObject cannonModel, slingChunk, cannonChunk;
    public GameObject[] slingChunkVariants, cannonChunkVariants;
    public AudioClip creak, snap, whistle, boom, tap, unlock;
    [Range(0f, 1f)] public float soundVolume = 0.65f;
    public float lowPitch = 0.85f, highPitch = 1.55f;
    [Header("Capture")]
    public float flySeconds = 3f, cameraAltitude = 4f, cameraStopBefore = 16f;
    public float volumePriority = 100f, freeCameraSpeed = 18f, cameraSensitivity = 0.12f;
    public float captureApproachDistance = 12f, captureImpactSpeed = 70f;
    public float capturePower = 130f, captureElan = 0.85f;
    [Range(0f, 1f)] public float previewHealth = 0.02f;
    public ChargeKind Kind(int index)
    {
        if (index == cannonIndex) return ChargeKind.Cannon;
        if (index == slingIndex || (giantSlingCanBreak && index == giantSlingIndex)) return ChargeKind.Sling;
        return ChargeKind.Standard;
    }

    private void OnValidate()
    {
        maximumHold = Mathf.Max(optimalHold + 0.02f, maximumHold);
        warningTime = Mathf.Clamp(warningTime, 0.02f, maximumHold);
    }
}
