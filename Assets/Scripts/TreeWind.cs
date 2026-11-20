using UnityEngine;

/// <summary>
/// Adds gentle, non-repeating wind sway to a tree GameObject.
/// Attach to a parent pivot placed at the base of the trunk.
/// </summary>
[DisallowMultipleComponent]
public class TreeWind : MonoBehaviour
{
    [Header("Gentle wind")]
    [SerializeField, Range(0f, 10f)] private float swayAngle = 2f;
    [SerializeField, Range(0.01f, 5f)] private float swaySpeed = 0.65f;

    [Header("Wind gusts")]
    [SerializeField, Range(0f, 10f)] private float gustAngle = 1.5f;
    [SerializeField, Range(0.01f, 3f)] private float gustSpeed = 0.2f;

    [Header("Direction")]
    [SerializeField] private Vector2 windDirection = new Vector2(1f, 0.3f);
    [SerializeField] private bool randomizeEachTree = true;

    private Quaternion initialRotation;
    private float phase;
    private float noiseOffset;

    private void Awake()
    {
        initialRotation = transform.localRotation;
        phase = randomizeEachTree ? Random.Range(0f, Mathf.PI * 2f) : 0f;
        noiseOffset = randomizeEachTree ? Random.Range(0f, 1000f) : 0f;
    }

    private void Update()
    {
        float time = Time.time;
        float wave = Mathf.Sin(time * swaySpeed * Mathf.PI * 2f + phase);
        float gust = Mathf.PerlinNoise(noiseOffset, time * gustSpeed) * 2f - 1f;
        float angle = wave * swayAngle + gust * gustAngle;

        Vector2 direction = windDirection.sqrMagnitude > 0.0001f
            ? windDirection.normalized
            : Vector2.right;

        // Local X and Z tilt; no unwanted spinning around the trunk.
        Quaternion tilt = Quaternion.Euler(direction.y * angle, 0f, -direction.x * angle);
        transform.localRotation = initialRotation * tilt;
    }

    private void OnDisable()
    {
        transform.localRotation = initialRotation;
    }
}
