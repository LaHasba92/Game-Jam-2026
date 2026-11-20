using UnityEngine;

public enum GroundObjectType { Trampoline, Coin, Booster, Balloon, Sticky }

// A mettre sur chaque prefab d'objet de la piste. Le boulet d'Andy lit « type » pour savoir quoi faire
// (rebond R = 1,1 / piece ramassee / +25 % de vitesse / remonte de 15 m / vitesse divisee par 2).
public class GroundObject : MonoBehaviour
{
    public GroundObjectType type;
    [Tooltip("Rayon de detection, en m")]
    public float radius = 1f;
}
