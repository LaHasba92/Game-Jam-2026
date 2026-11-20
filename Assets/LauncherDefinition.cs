using UnityEngine;

[CreateAssetMenu(menuName = "CanonPOW/Lanceur")]
public class LauncherDefinition : ScriptableObject
{
    public string displayName;
    [Min(0)] public int price;
    [Min(0)] public int requiredWall;
    public float powerMultiplier = 1f;
    public float extraAimSeconds;
}