using System.Collections;

// Les contrats entre systèmes (GDD version jam, section 8), recopiés tels quels.
// Axes : X = la piste, Y = la hauteur, Z = gauche/droite ; le lanceur est en x = 0.

// Andy -> Sami : le boulet arrive au mur
public struct WallHit
{
    public float impactHeight; // m
    public float impactSide;   // m, axe Z, 0 = centre de la piste
    public float impactSpeed;  // m/s, pour les effets
    public bool rolling;       // un boulet qui roule ne casse pas le mur
}

// Sami -> Andy, Nathan et Hugo
public struct WallHitResult
{
    public bool broken;
    public float exitSpeedFactor; // Élan (0,6 au niveau 1) si casse, sinon rebond
    public bool bossHit;         // faux par défaut, les murs ordinaires gardent leur contrat
}

public interface IWall
{
    float Distance { get; }
    float WallWidth { get; }               // largeur du couloir
    float WallHeight { get; }              // hauteur du couloir
    WallHitResult ApplyHit(WallHit hit);   // logique, instantanée
    IEnumerator PlayHit(WallHitResult r);  // animation
}

// Andy -> Nathan et Hugo : la fin d'une partie
public struct RunResult
{
    public float distance;
    public int wallsBroken;
    public int coinsPicked;
    public bool newRecord;
    public RunEndReason reason;
    public float distanceToNextWall;
}

// Données de OnShotFired (GDD : visée 3D + jauge + lanceur)
public struct ShotInfo
{
    public float direction;       // degrés, gauche/droite (axe horizontal du doigt)
    public float angle;           // degrés, angle de tir (axe vertical du doigt)
    public float gaugeMultiplier; // ×0,5 à ×1,25, donné par la jauge
    public int launcherIndex;     // 1 = fronde … 5 = canon
}
