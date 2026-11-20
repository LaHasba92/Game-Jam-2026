using System;

// Le « standard téléphonique » du jeu (GDD version jam, section 8).
// Un script émet un événement ; tous ceux qui sont abonnés le reçoivent,
// sans que les scripts se connaissent entre eux.
//
// Émettre :       GameEvents.RaiseRunEnded(result);
// S'abonner :     GameEvents.OnRunEnded += MaMethode;   (dans OnEnable)
// Se désabonner : GameEvents.OnRunEnded -= MaMethode;   (dans OnDisable)
public static class GameEvents
{
    // ─── Événements du GDD ───

    public static event Action<ShotInfo> OnShotFired;        // Andy : le joueur lâche
    public static event Action<WallHitResult> OnWallHit;     // Sami : le boulet touche un mur
    public static event Action<int> OnWallBroken;            // Sami : numéro du mur cassé (1, 2, 3…)
    public static event Action<int, IWall> OnLevelLoaded;    // Sami : mur placé (numéro, le mur) — à aligner avec Sami
    public static event Action<RunResult> OnRunEnded;        // Andy : fin de la partie
    public static event Action<int> OnCoinsChanged;          // Nathan : total de pièces

    // À ajouter quand Nathan aura défini le type des stats :
    // OnUpgradeBought (stat ou lanceur, niveau)

    // ─── Ajouts de Benji (pas dans le GDD, à valider avec l'équipe) ───

    // UI et caméras savent dans quel état on est (ancien, nouveau)
    public static event Action<GameState, GameState> OnStateChanged;

    // Pub « relancer » : Andy relance le boulet depuis le dernier mur cassé
    public static event Action OnReviveRequested;

    // ─── Méthodes pour émettre ───
    // Le « ?. » veut dire : n'appelle que si quelqu'un est abonné (sinon erreur)

    public static void RaiseShotFired(ShotInfo shot) => OnShotFired?.Invoke(shot);
    public static void RaiseWallHit(WallHitResult result) => OnWallHit?.Invoke(result);
    public static void RaiseWallBroken(int wallNumber) => OnWallBroken?.Invoke(wallNumber);
    public static void RaiseLevelLoaded(int wallNumber, IWall wall) => OnLevelLoaded?.Invoke(wallNumber, wall);
    public static void RaiseRunEnded(RunResult result) => OnRunEnded?.Invoke(result);
    public static void RaiseCoinsChanged(int total) => OnCoinsChanged?.Invoke(total);

    public static void RaiseStateChanged(GameState from, GameState to) => OnStateChanged?.Invoke(from, to);
    public static void RaiseReviveRequested() => OnReviveRequested?.Invoke();
}
