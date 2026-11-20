using UnityEngine;

public struct BossImpact
{
    public int damage, remaining;
    public Vector3 position;
}

public static class BossProgress
{
    // Clef distincte des soldes existants. Une ancienne sauvegarde commence à PV complets.
    private const string Key = "CanonPOW.GiantWall.HP.v1";
    public static int Health(LevelDefinition wall) =>
        Mathf.Clamp(JamSave.GetInt(Key, wall.bossMaxHealth), 0, wall.bossMaxHealth);

    public static BossImpact Hit(LevelDefinition wall, float speed, Vector3 point)
    {
        int before = Health(wall);
        int damage = JamRules.Damage(speed, wall.damagePerSpeed, before);
        var impact = new BossImpact { damage = damage, remaining = before - damage, position = point };
        if (damage == 0) return impact;
        JamSave.SetInt(Key, impact.remaining);
        JamSave.Save();
        GameEvents.RaiseBossWallDamaged(impact);
        return impact;
    }

    public static void SetPreviewHealth(LevelDefinition wall, float ratio)
    {
        if (JamSave.Preview) JamSave.SetInt(Key, Mathf.RoundToInt(wall.bossMaxHealth * Mathf.Clamp01(ratio)));
    }
}
