using System;

// Valeurs des enums existants préservées, notamment le résultat ordinaire par défaut.
public enum RunEndReason { Distance, WallIntact, SlingBroken, CannonExploded, BossSurvived, BossDefeated }
public enum ChargeKind { Standard, Sling, Cannon }

public static class JamRules
{
    public static bool Misfire(RunEndReason reason) =>
        reason == RunEndReason.SlingBroken || reason == RunEndReason.CannonExploded;

    public static bool IsRed(float gauge, float redWidth) => gauge >= 1f - redWidth;

    public static float CannonDanger(float seconds, float limit, float warning)
    {
        float start = limit - warning;
        return Math.Clamp((seconds - start) / Math.Max(0.001f, warning), 0f, 1f);
    }

    public static int Damage(float speed, float perSpeed, int remaining)
    {
        if (!(speed > 0f) || !(perSpeed > 0f) || remaining <= 0) return 0;
        double raw = Math.Round((double)speed * perSpeed, MidpointRounding.AwayFromZero);
        return (int)Math.Min(remaining, Math.Max(1d, raw));
    }

    public static int CrackStage(int hp, int maximum, float[] thresholds)
    {
        int stage = 0;
        foreach (float threshold in thresholds)
            if ((float)hp / Math.Max(1, maximum) <= threshold) stage++;
        return stage;
    }
}
