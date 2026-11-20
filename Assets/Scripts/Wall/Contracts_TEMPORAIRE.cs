using System.Collections;

// TEMPORAIRE : copie des contrats du GDD (version finale : mur sans cible, casse a tout impact).
// A SUPPRIMER des que Benji pousse ses contrats sur main (sinon erreur "already contains a definition").

// Andy -> Sami : le boulet arrive au mur
public struct WallHit
{
    public float impactHeight;  // m
    public float impactSide;    // m, decalage gauche/droite par rapport au centre de la piste (axe Z)
    public float impactSpeed;   // m/s, pour les effets
    public bool rolling;        // un boulet qui roule ne casse pas le mur : fin de la partie
}

// Sami -> Andy, Nathan et Hugo : ce que le mur renvoie
public struct WallHitResult
{
    public bool broken;           // le mur est casse
    public float exitSpeedFactor; // part de la vitesse gardee (Elan : 0,6 au niveau 1), sinon rebond
}

public interface IWall
{
    float Distance { get; }
    float WallWidth { get; }        // m : largeur du couloir (murs invisibles)
    float WallHeight { get; }       // m : hauteur du couloir (plafond invisible)
    WallHitResult ApplyHit(WallHit hit);   // logique, instantanee
    IEnumerator PlayHit(WallHitResult r);  // animation
}

// Andy -> Nathan et Hugo : la fin d'une partie
public struct RunResult
{
    public float distance;
    public int wallsBroken;
    public int coinsPicked;
    public bool newRecord;
}
