#if UNITY_EDITOR
using UnityEngine;

// Cherche un vrai tir à reproduire dans la capture, sans changer les distances ni les PV.
public static class AdCaptureFlight
{
    private const int TargetRemainingHealth = 4;
    private const int MinRemainingHealth = 2;
    private const int MaxRemainingHealth = 7;
    private const float MaxFlightSeconds = 12f;
    private const float WallTop = 8f;

    public static bool TryFind(bool canon, Vector3 origin, ProjectileController projectile, LevelManager levels,
                               int bossIndex, out Vector3 velocity)
    {
        velocity = Vector3.zero;
        if (projectile == null || levels == null) return false;

        LevelDefinition boss = null;
        if (canon)
        {
            boss = levels.GetCaptureBoss(out int configuredIndex);
            if (boss == null || bossIndex != configuredIndex || bossIndex < 2) return false;
        }

        int wallCount = canon ? bossIndex + 1 : 3;
        float[] distances = new float[wallCount];
        for (int i = 0; i < distances.Length; ++i)
        {
            distances[i] = levels.WallDistance(i);
        }

        return canon
            ? TryFindNearMiss(origin, projectile, levels, distances, boss, out velocity)
            : TryFindShortFlight(origin, projectile, levels, distances, out velocity);
    }

    private static bool TryFindShortFlight(Vector3 origin, ProjectileController projectile, LevelManager levels,
                                          float[] distances, out Vector3 velocity)
    {
        velocity = Vector3.zero;
        float[] elevations = { 24f, 20f, 28f, 16f, 30f, 12f };
        float[] speeds = { 30f, 29f, 31f, 28f, 32f, 26f, 34f, 24f, 36f };
        foreach (float elevation in elevations)
        {
            foreach (float speed in speeds)
            {
                Vector3 candidate = LaunchVelocity(speed, elevation);
                int wallIndex = FirstFailure(origin, candidate, projectile, levels, distances, 0.6f, out _);
                if (wallIndex == 2)
                {
                    velocity = candidate;
                    return true;
                }
                if (wallIndex == 1 && velocity == Vector3.zero) velocity = candidate;
            }
        }
        return velocity != Vector3.zero;
    }

    private static bool TryFindNearMiss(Vector3 origin, ProjectileController projectile, LevelManager levels,
                                       float[] distances, LevelDefinition boss, out Vector3 velocity)
    {
        velocity = Vector3.zero;
        int bossIndex = distances.Length - 1;
        float bestScore = float.PositiveInfinity;
        float maxSpeed = projectile.Physics.maxSpeed;
        // La piste fournie donne son quasi-succès au voisinage de 150 m/s et 5°.
        for (int elevation = 2; elevation <= 20; ++elevation)
        {
            for (float speed = 70f; speed <= maxSpeed; speed += 1.5f)
            {
                Vector3 candidate = LaunchVelocity(speed, elevation);
                int wallIndex = FirstFailure(origin, candidate, projectile, levels, distances, 0.85f, out WallHit hit);
                if (wallIndex != bossIndex || hit.rolling) continue;

                int health = WallRules.HealthAfterImpact(hit, boss.maxHealth, boss.maxHealth, boss.fullHealthBreakSpeed);
                if (health < MinRemainingHealth || health > MaxRemainingHealth) continue;

                float score = Mathf.Abs(health - TargetRemainingHealth) * 3f + Mathf.Abs(hit.impactHeight - 2.5f);
                if (score >= bestScore) continue;
                bestScore = score;
                velocity = candidate;

                if (score < 0.5f) return true;
            }
        }
        return velocity != Vector3.zero;
    }

    private static int FirstFailure(Vector3 position, Vector3 velocity, ProjectileController projectile,
                                    LevelManager levels, float[] distances, float elan, out WallHit hit)
    {
        BallPhysics.Params physics = projectile.Physics;
        float dt = Time.fixedDeltaTime;
        if (dt <= 0f)
        {
            hit = default;
            return -1;
        }

        float halfWidth = projectile.CorridorHalfWidth;
        float ceiling = projectile.Ceiling;
        int steps = Mathf.Min(1800, Mathf.CeilToInt(MaxFlightSeconds / dt));
        int wallIndex = 0;
        bool rolling = false;

        for (int step = 0; step < steps; ++step)
        {
            BallPhysics.Step(ref position, ref velocity, ref rolling, physics, dt, halfWidth, ceiling);
            if (position.x + physics.radius >= distances[wallIndex])
            {
                // Avec le plafond ouvert, le jeu détecte aussi les impacts au-dessus du modèle.
                // La capture exige un impact visible sur chaque mur.
                if (position.y > WallTop - physics.radius) break;
                hit = new WallHit
                {
                    impactHeight = position.y,
                    impactSide = position.z,
                    impactSpeed = velocity.magnitude,
                    rolling = rolling
                };
                if (!levels.CanBreakIntactWall(wallIndex, hit)) return wallIndex;
                position.x = distances[wallIndex] + 1.5f;
                velocity *= elan;
                if (++wallIndex == distances.Length) break;
            }
            if (velocity.x < 1f) break;
        }

        hit = default;
        return -1;
    }

    private static Vector3 LaunchVelocity(float speed, float elevation)
    {
        float radians = elevation * Mathf.Deg2Rad;
        return new Vector3(Mathf.Cos(radians) * speed, Mathf.Sin(radians) * speed, 0f);
    }
}
#endif
