// Les regles du mur, sans rien d'affiche : on peut les tester sans scene.
// La position de l'impact n'affecte pas les dégâts et un boulet qui roule ne casse aucun mur.
public static class WallRules
{
    public static int HealthAfterImpact(WallHit hit, int health, int maxHealth, float fullHealthBreakSpeed)
    {
        if (maxHealth <= 0) return 0;
        health = System.Math.Clamp(health, 0, maxHealth);
        if (hit.rolling || !(hit.impactSpeed > 0f)) return health;

        double breakSpeed = fullHealthBreakSpeed >= 1f ? fullHealthBreakSpeed : 1f;
        if (hit.impactSpeed >= breakSpeed) return 0;

        int damage = (int)System.Math.Min(maxHealth - 1d, maxHealth * (double)hit.impactSpeed / breakSpeed);
        return System.Math.Max(0, health - damage);
    }

    // elan : part de la vitesse gardee apres la casse (amelioration Elan, 0,6 au niveau 1).
    // reboundFactor : vitesse apres un echec (negatif = le boulet repart en arriere).
    public static WallHitResult Evaluate(WallHit hit, float elan, float reboundFactor, int remainingHealth = 0)
    {
        WallHitResult result = new WallHitResult();
        result.broken = !hit.rolling && remainingHealth <= 0;
        result.exitSpeedFactor = result.broken ? elan : reboundFactor;
        return result;
    }
}
