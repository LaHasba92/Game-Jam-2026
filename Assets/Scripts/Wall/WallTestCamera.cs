using System.Collections;
using UnityEngine;

// SANDBOX SEULEMENT : une camera qui se place devant chaque nouveau mur, pour tester au clic.
// A mettre sur la Main Camera. Dans le vrai jeu, c'est le CameraDirector d'Andy qui decide.
public class WallTestCamera : MonoBehaviour
{
    [SerializeField] private LevelManager levelManager;
    [SerializeField] private float distanceInFront = 18f;  // m devant le mur (vers le lanceur)
    [SerializeField] private float height = 5f;            // m, hauteur de la camera
    [SerializeField] private float followSpeed = 1.5f;     // vitesse du glissement vers le mur suivant
    [SerializeField] private float delayBeforeMoving = 2f; // s a rester sur l'explosion avant de partir

    private bool firstWall = true;
    private Coroutine moveRoutine;

    private Vector3 targetPosition;
    private Quaternion targetRotation;

    private void OnEnable()
    {
        if (levelManager == null)   // case vide dans l'Inspector : on le cherche dans la scene
        {
            levelManager = FindFirstObjectByType<LevelManager>();
        }
        if (levelManager == null)
        {
            Debug.LogWarning("WallTestCamera : aucun LevelManager dans la scene.");
            return;
        }
        levelManager.WallPlaced += OnWallPlaced;
    }

    private void OnDisable()
    {
        if (levelManager != null)
        {
            levelManager.WallPlaced -= OnWallPlaced;
        }
    }

    private void OnWallPlaced(LevelDefinition wall)
    {
        // Premier mur d'une partie : on y va tout de suite. Sinon, on laisse voir l'explosion.
        if (moveRoutine != null)
        {
            StopCoroutine(moveRoutine);
        }
        bool newRun = firstWall || wall.levelNumber == 1;
        firstWall = false;
        moveRoutine = StartCoroutine(MoveTo(wall, newRun ? 0f : delayBeforeMoving));
    }

    private IEnumerator MoveTo(LevelDefinition wall, float delay)
    {
        yield return new WaitForSeconds(delay);
        // derriere, face au mur (vers +X), en regardant le milieu du mur
        targetPosition = new Vector3(wall.wallDistance - distanceInFront, height, 0f);
        Vector3 wallMiddle = new Vector3(wall.wallDistance, 4f, 0f);
        targetRotation = Quaternion.LookRotation(wallMiddle - targetPosition);
        moveRoutine = null;
    }

    private void LateUpdate()
    {
        if (targetRotation == default)   // aucun mur place pour l'instant
        {
            return;
        }
        // glisse doucement vers la nouvelle place (Lerp = un peu plus pres a chaque image)
        float t = 1f - Mathf.Exp(-followSpeed * Time.deltaTime);
        transform.position = Vector3.Lerp(transform.position, targetPosition, t);
        transform.rotation = Quaternion.Slerp(transform.rotation, targetRotation, t);
    }
}
