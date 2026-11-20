using System.Collections;
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

    private void OnEnable()  { wall.HitResolved += OnWallHit; }
    private void OnDisable() { wall.HitResolved -= OnWallHit; }

    private void OnWallHit(WallHitResult result)
    {
        if (result.broken)
        {
            holdPoint = projectile.transform.position;
            holdUntil = Time.time + holdOnBreak;
            shake = shakeOnBreak;
        }
    }

    private void LateUpdate()
    {
        Vector3 target;
        Vector3 lookAt;
        if (Time.time < holdUntil)
        {
            target = holdPoint + followOffset;
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
        transform.position = Vector3.Lerp(transform.position, target, t) + Random.insideUnitSphere * shake;
        transform.rotation = Quaternion.Slerp(transform.rotation, Quaternion.LookRotation(lookAt - transform.position), t);
        shake = Mathf.MoveTowards(shake, 0f, Time.deltaTime);
    }
}
