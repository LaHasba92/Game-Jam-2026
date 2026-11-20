using System.Collections;
using System.Collections.Generic;
using UnityEngine;

// Les contrats entre systèmes (GDD, section 12), recopiés tels quels.
// Chacun code contre ces types, même si le vrai code des autres n'existe pas encore.

public enum BlockColor { Red, Blue, Yellow, Green, Purple }
public enum ImpactKind { RightColor, WrongColor, Pow }

// Données de OnShotFired (GDD : couleur, case, M_jauge, PERFECT, Tir POW)
public struct ShotInfo
{
    public BlockColor color;
    public Vector2Int cell;
    public float gaugeMultiplier;
    public bool perfect;
    public bool powShot;
}

// Une étape de l'animation du mur (explosion ou chute) : Sami la remplit
public struct WallStep
{
}

// Sami → Andy : ce que le mur renvoie au projectile
public struct ImpactResult
{
    public ImpactKind kind;
    public int blocksDestroyed;   // réactions comprises
    public int comboWaves;        // 0 = aucune réaction
    public int wallScore;
    public float speedMultiplier; // M_impact × (1 + b_zone × blocs)
    public float duration;        // durée de l'absorption, 1,5 s au plus
    public List<WallStep> steps;  // explosions et chutes, rejouées par PlayImpact
}

public interface IWall
{
    bool TryGetCell(Vector3 worldPoint, out Vector2Int cell);
    IReadOnlyCollection<Vector2Int> PreviewGroup(Vector2Int cell, BlockColor color); // groupe allumé
    ImpactResult ResolveImpact(Vector2Int cell, BlockColor projectile, bool powShot); // logique, instantanée
    IEnumerator PlayImpact(ImpactResult result);                                      // animation
}

// Andy → Nathan et Hugo : la fin d'un tir
public struct RunResult
{
    public float distance;
    public int blocks;
    public bool perfect;
    public int score;
    public int coinsPicked;
    public bool newRecord;
}
