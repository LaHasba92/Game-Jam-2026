using UnityEngine;

public enum GroundObjectType { Trampoline, Coin, Booster, BoostRing, Sticky }

// A mettre sur chaque prefab d'objet de la piste. Le boulet d'Andy lit « type » pour savoir quoi faire
// (rebond / pièce / accélérateur au sol / anneau de vitesse / surface collante).
public class GroundObject : MonoBehaviour
{
    public GroundObjectType type;
    [Tooltip("Rayon de detection, en m")]
    public float radius = 1f;

    [Tooltip("Couvre toute la largeur du couloir (trampolines, boosters)")]
    public bool fullWidth;
    [Tooltip("Longueur sur la piste (axe X), en m, pour les objets pleine largeur")]
    public float length = 2f;
    [Tooltip("Largeur du modele a l'echelle 1 (axe Z), en m : sert a l'etirer sur tout le couloir")]
    public float modelWidth = 2f;
}
