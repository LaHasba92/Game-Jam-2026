using UnityEngine;

public class TestScript : MonoBehaviour
{
    [SerializeField] private float speed = 90f; // degres par seconde

    private void Start()
    {
        Debug.Log($"Bonjour, je suis {name}");
    }

    private void Update()
    {
        transform.Rotate(0f, speed * Time.deltaTime, 0f);

        if (Input.GetKeyDown(KeyCode.Space))
        {
            speed = -speed;
            Debug.Log($"Changement de sens : {speed}");
        }
    }
}