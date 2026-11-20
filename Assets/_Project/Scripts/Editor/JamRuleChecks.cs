using System;
using UnityEditor;
using UnityEngine;

public static class JamRuleChecks
{
    [MenuItem("CanonPOW/Tests/Vérifier les règles de la jam")]
    private static void Run()
    {
        if (EditorApplication.isPlayingOrWillChangePlaymode || JamSave.Preview)
        {
            Debug.LogWarning("Lance cette vérification hors du mode Play.");
            return;
        }
        AimTuning tuning = AimTuning.Current;
        Verify(tuning != null && tuning.giantWall != null, "Fiches de réglages et du Mur géant");
        LevelDefinition wall = tuning.giantWall;
        Verify(wall.IsBoss && wall.wallDistance > 0f && wall.damagePerSpeed > 0f, "Objectif de la piste");
        Verify(tuning.creak && tuning.snap && tuning.whistle && tuning.boom && tuning.tap && tuning.unlock,
            "Six sons de la jam");
        Verify(tuning.maximumHold > tuning.optimalHold && tuning.warningTime > 0f, "Fenêtres du canon");
        Verify(JamRules.IsRed(1f, tuning.redWidth) && !JamRules.IsRed(0f, tuning.redWidth), "Bornes de la jauge");
        Verify(JamRules.Damage(70f, 5f, 3000) == 350 && JamRules.Damage(70f, 5f, 60) == 60,
            "Dégâts et limite des PV");
        Verify(wall.crackThresholds != null && wall.crackThresholds.Length == 3, "Trois paliers de fissures");
        int previous = BossProgress.Health(wall);
        int wallet = RunProgress.Coins;
        JamSave.SetPreview(true);
        try
        {
            BossProgress.SetPreviewHealth(wall, 1f);
            BossImpact hit = BossProgress.Hit(wall, 70f, Vector3.zero);
            Verify(hit.damage > 0 && BossProgress.Health(wall) == wall.bossMaxHealth - hit.damage,
                "La perte de PV est enregistrée");
            BossProgress.SetPreviewHealth(wall, 0f);
            Verify(BossProgress.Hit(wall, 70f, Vector3.zero).damage == 0, "Pas de double destruction");
            RunProgress.Credit(123);
        }
        finally { JamSave.SetPreview(false); }
        Verify(BossProgress.Health(wall) == previous && RunProgress.Coins == wallet, "Fin de capture");
        Debug.Log("CanonPOW : vérifications de la jam réussies.");
    }

    private static void Verify(bool success, string name)
    {
        if (!success) throw new InvalidOperationException("CanonPOW : " + name);
    }
}
