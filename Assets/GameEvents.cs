using System;

// Le « standard téléphonique » du jeu (GDD, section 12).
// Un script émet un événement ; tous ceux qui sont abonnés le reçoivent,
// sans que les scripts se connaissent entre eux.
//
// Émettre :   GameEvents.RaiseRunEnded(result);
// S'abonner : GameEvents.OnRunEnded += MaMethode;   (dans OnEnable)
// Se désabonner : GameEvents.OnRunEnded -= MaMethode; (dans OnDisable)
public static class GameEvents
{
    // ─── Événements ───

    // Ajout (pas dans le tableau du GDD) : UI et caméras savent dans quel état on est
    public static event Action<GameState, GameState> OnStateChanged;   // ancien, nouveau

    public static event Action<ShotInfo> OnShotFired;                  // LauncherController

    // Ajout (à valider avec Andy) : le projectile touche le mur → GameManager passe en Absorption
    public static event Action OnWallHit;                              // ProjectileController

    public static event Action<int, int> OnBlocksDestroyed;            // nombre, vague — WallImpact
    public static event Action<ImpactResult> OnWallImpactEnded;        // WallImpact
    public static event Action<RunResult> OnRunEnded;                  // ProjectileController
    public static event Action<int> OnCoinsChanged;                    // total — EconomyManager
    public static event Action<int> OnZoneReached;                     // numéro — GameManager
    // OnUpgradeBought (stat, niveau) : à ajouter quand Nathan aura défini le type des stats

    // ─── Méthodes pour émettre ───
    // Le « ?. » veut dire : n'appelle que si quelqu'un est abonné (sinon erreur)

    public static void RaiseStateChanged(GameState from, GameState to) => OnStateChanged?.Invoke(from, to);
    public static void RaiseShotFired(ShotInfo shot) => OnShotFired?.Invoke(shot);
    public static void RaiseWallHit() => OnWallHit?.Invoke();
    public static void RaiseBlocksDestroyed(int count, int wave) => OnBlocksDestroyed?.Invoke(count, wave);
    public static void RaiseWallImpactEnded(ImpactResult result) => OnWallImpactEnded?.Invoke(result);
    public static void RaiseRunEnded(RunResult result) => OnRunEnded?.Invoke(result);
    public static void RaiseCoinsChanged(int total) => OnCoinsChanged?.Invoke(total);
    public static void RaiseZoneReached(int zone) => OnZoneReached?.Invoke(zone);
}
