using UnityEngine;

public class SlingshotAnimation : MonoBehaviour
{
    [Header("Points sur le lance-pierre")]
    public Transform leftFork;
    public Transform rightFork;
    public Transform pouch;

    [Header("Animation")]
    public float maxPullDistance = 0.6f;
    public float pullSensitivity = 0.005f;
    public float returnSpeed = 12f;
    public float bandWidth = 0.025f;
    public Material bandMaterial;

    private LineRenderer leftBand;
    private LineRenderer rightBand;
    private Vector3 restPosition;
    private Vector3 pullDirection;
    private float pullAmount;
    private float startMouseY;
    private bool pulling;

    private void Start()
    {
        if (leftFork == null || rightFork == null || pouch == null)
        {
            Debug.LogError("SlingshotAnimation : assigne Left Fork, Right Fork et Pouch dans l'Inspector.", this);
            enabled = false;
            return;
        }

        restPosition = pouch.localPosition;
        // Par defaut, la traction se fait vers l'arriere local (-Z).
        pullDirection = Vector3.back;
        leftBand = CreateBand("LeftBand");
        rightBand = CreateBand("RightBand");
        UpdateBands();
    }

    private LineRenderer CreateBand(string bandName)
    {
        GameObject band = new GameObject(bandName);
        band.transform.SetParent(transform, false);
        LineRenderer line = band.AddComponent<LineRenderer>();
        line.useWorldSpace = true;
        line.positionCount = 2;
        line.startWidth = bandWidth;
        line.endWidth = bandWidth;
        line.numCapVertices = 4;
        if (bandMaterial != null)
        {
            line.material = bandMaterial;
        }
        return line;
    }

    private void Update()
    {
        if (Input.GetMouseButtonDown(0))
        {
            pulling = true;
            startMouseY = Input.mousePosition.y;
        }

        if (pulling && Input.GetMouseButton(0))
        {
            float distance = (startMouseY - Input.mousePosition.y) * pullSensitivity;
            pullAmount = Mathf.Clamp(distance, 0f, maxPullDistance);
        }

        if (Input.GetMouseButtonUp(0))
        {
            pulling = false;
            // Ici, tu peux appeler la fonction qui lance ton projectile.
        }

        if (!pulling)
        {
            pullAmount = Mathf.MoveTowards(pullAmount, 0f, returnSpeed * Time.deltaTime);
        }

        pouch.localPosition = restPosition + pullDirection * pullAmount;
        UpdateBands();
    }

    private void UpdateBands()
    {
        leftBand.SetPosition(0, leftFork.position);
        leftBand.SetPosition(1, pouch.position);
        rightBand.SetPosition(0, rightFork.position);
        rightBand.SetPosition(1, pouch.position);
    }
}
