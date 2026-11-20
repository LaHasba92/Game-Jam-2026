// Les regles du mur, sans rien d'affiche : on peut les tester sans scene.
// Le mur casse quel que soit le point d'impact, sauf si le boulet arrive en roulant.
public static class WallRules
{
    // elan : part de la vitesse gardee apres la casse (amelioration Elan, 0,6 au niveau 1).
    // reboundFactor : vitesse apres un echec (negatif = le boulet repart en arriere).
    public static WallHitResult Evaluate(WallHit hit, float elan, float reboundFactor)
    {
        WallHitResult result = new WallHitResult();
        result.broken = !hit.rolling;
        result.exitSpeedFactor = result.broken ? elan : reboundFactor;
        return result;
    }
}
