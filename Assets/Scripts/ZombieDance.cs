using UnityEngine;

public class ZombieDance : MonoBehaviour
{
    [Header("Partie visuelle du zombie (facultatif)")]
    [SerializeField] private Transform body;
    [SerializeField] private Transform leftArm;
    [SerializeField] private Transform rightArm;

    [Header("Danse")]
    [Min(0.1f)] public float speed = 2f;
    [Range(0f, 30f)] public float swayAngle = 12f;
    [Range(0f, 35f)] public float twistAngle = 15f;
    [Min(0f)] public float bounceHeight = 0.08f;
    [Range(0f, 80f)] public float armAngle = 35f;
    public bool randomStart = true;

    private Vector3 initialPosition;
    private Quaternion initialRotation;
    private Quaternion leftArmRotation;
    private Quaternion rightArmRotation;
    private float phase;

    private void Start()
    {
        if (body == null)
        {
            body = transform;
        }

        initialPosition = body.localPosition;
        initialRotation = body.localRotation;
        if (leftArm != null) leftArmRotation = leftArm.localRotation;
        if (rightArm != null) rightArmRotation = rightArm.localRotation;
        phase = randomStart ? Random.Range(0f, Mathf.PI * 2f) : 0f;
    }

    private void LateUpdate()
    {
        float t = Time.time * speed * Mathf.PI + phase;
        float side = Mathf.Sin(t);
        float bounce = (1f - Mathf.Cos(t * 2f)) * 0.5f;

        body.localPosition = initialPosition + Vector3.up * (bounce * bounceHeight);
        body.localRotation = initialRotation * Quaternion.Euler(0f, side * twistAngle,
            side * swayAngle);

        if (leftArm != null)
        {
            leftArm.localRotation = leftArmRotation *
                Quaternion.Euler(Mathf.Sin(t) * armAngle, 0f, 0f);
        }
        if (rightArm != null)
        {
            rightArm.localRotation = rightArmRotation *
                Quaternion.Euler(-Mathf.Sin(t) * armAngle, 0f, 0f);
        }
    }
}
