using UnityEngine;

[CreateAssetMenu(menuName = "CanonPOW/Amelioration")]
public class UpgradeDefinition : ScriptableObject
{
    public string displayName;
    public float baseValue;
    public float ValueMultiplier = 1f;
    public float valuePerLevel = 0f;
    public float minimumValue = 0f;
    public float maximumValue = 0f;

    [Min(1)] public int baseCost = 1;
    [Min(1f)] public float costGrowth = 1f;

    public int GetCost(int level)
    {
        return Mathf.RoundToInt(baseCost * Mathf.Pow(costGrowth, level - 1));
    }

    public float GetValue(int level)
    {
        int upgrades = level - 1;
        float value = baseValue * Mathf.Pow(ValueMultiplier, upgrades);
        value += valuePerLevel * upgrades;
        value = Mathf.Max(minimumValue, value);

        if (maximumValue > 0f)
            value = Mathf.Min(value, maximumValue);

        return value;
    }

    [ContextMenu("Tester les niveaux 1 et 3")]
    private void TestFirstLevels()
    {
        for (int level = 1; level <= 3; level++)
        {
            Debug.Log($"{displayName} | Niveau {level} | " +
                    $"Valeur{GetValue(level):0.####} | Prix {GetCost(level)} pièces");
        }
    }

    [ContextMenu("Tester Élan à 20 m/s")]
    private void TestMomentum()
    {
        int[] levels = { 1, 2, 9, 10, 11 };

        foreach (int level in levels)
        {
            float speedAfterWall = 20f * GetValue(level);
            Debug.Log($"Élan niveau {level} : {speedAfterWall:0.##} m/s après le mur");
        }
    }
}