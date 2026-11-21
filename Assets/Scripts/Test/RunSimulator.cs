using System;

// Simule une partie complete SANS Unity (pas de scene, pas d'image) : meme physique que le faux boulet.
// Sert a l'equilibrage : 1 000 parties en une fraction de seconde. C# pur => testable partout.
// Simplification : pas d'objets au sol (trampolines, boosters...) ni de pilotage.
public static class RunSimulator
{
    [Serializable]
    public class Settings
    {
        public string name = "Debut";
        public float basePower = 25f;          // Puissance
        public float launcherMultiplier = 1f;  // fronde x1 ... canon x3
        public float drag = 0.10f;             // Aerodynamisme
        public float bounce = 0.45f;           // Rebond (vitesse verticale gardee)
        public float elan = 0.6f;              // Elan
        public float maxElevation = 30f;       // Angle
        public float groundFriction = 0.9f;
        public float rollBrake = 1.5f;
        public float coinsPerMeter = 0.5f;
        public int wallBonusBase = 10;
        public int wallBonusStep = 10;
    }

    public struct Result
    {
        public int walls;
        public float distance;
        public int coins;
    }

    private const float Gravity = 9.81f;
    private const float Radius = 0.3f;
    private const float Dt = 0.02f;

    // wallDistance(i) = distance du mur n° i (0 = premier), fournie par LevelManager.
    public static Result Simulate(Settings s, Func<int, float> wallDistance, Random random,
        Func<int, WallHit, bool> canBreakWall = null)
    {
        // Le joueur moyen : vise dans les 10 derniers degres permis, lache la jauge vers 85 %.
        double angle = (s.maxElevation - 10.0 * random.NextDouble()) * Math.PI / 180.0;
        double gauge = Math.Min(1.0, Math.Max(0.0, Gauss(random, 0.85, 0.12)));
        double speed = s.basePower * (0.5 + 0.75 * gauge) * s.launcherMultiplier;

        double vx = speed * Math.Cos(angle), vy = speed * Math.Sin(angle);
        double x = 0, y = 1;
        bool rolling = false;
        int walls = 0;
        int bonus = 0;
        double slow = 0;

        for (int step = 0; step < 100000; ++step)
        {
            vx *= 1 - s.drag * Dt;
            vy *= 1 - s.drag * Dt;
            if (!rolling) vy -= Gravity * Dt;
            x += vx * Dt;
            y += vy * Dt;

            if (y <= Radius)
            {
                y = Radius;
                if (rolling)
                {
                    vx = Math.Max(0, vx - s.rollBrake * Dt);
                }
                else if (vy < 0)
                {
                    vx *= s.groundFriction;
                    vy = -vy * s.bounce;
                    if (vy * vy / (2 * Gravity) < 0.3) { rolling = true; vy = 0; }
                }
            }

            double wallX = wallDistance(walls);
            if (x + Radius >= wallX)
            {
                WallHit hit = new WallHit { impactSpeed = (float)Math.Sqrt(vx * vx + vy * vy), rolling = rolling };
                if (!(canBreakWall?.Invoke(walls, hit) ?? !rolling)) break;
                ++walls;
                bonus += s.wallBonusBase + s.wallBonusStep * (walls - 1);
                vx *= s.elan;
                vy *= s.elan;
                x = wallX + 1.5;
            }

            slow = Math.Abs(vx) < 1 ? slow + Dt : 0;
            if (slow >= 0.5) break;
        }

        return new Result
        {
            walls = walls,
            distance = (float)x,
            coins = (int)Math.Round(x * s.coinsPerMeter) + bonus,
        };
    }

    // Moyennes sur n parties (meme graine = memes resultats, pour comparer deux reglages).
    public static string Report(Settings s, Func<int, float> wallDistance, int n = 1000, int seed = 1,
        Func<int, WallHit, bool> canBreakWall = null)
    {
        Random random = new Random(seed);
        double walls = 0, distance = 0, coins = 0;
        int zero = 0, best = 0;
        for (int i = 0; i < n; ++i)
        {
            Result r = Simulate(s, wallDistance, random, canBreakWall);
            walls += r.walls; distance += r.distance; coins += r.coins;
            if (r.walls == 0) ++zero;
            best = Math.Max(best, r.walls);
        }
        return $"{s.name,-8} : murs {walls / n:0.0} (max {best}, {100.0 * zero / n:0} % a zero)  "
             + $"distance {distance / n:0} m  pieces {coins / n:0}";
    }

    private static double Gauss(Random random, double mean, double sd)
    {
        double u1 = 1.0 - random.NextDouble(), u2 = random.NextDouble();
        return mean + sd * Math.Sqrt(-2.0 * Math.Log(u1)) * Math.Cos(2.0 * Math.PI * u2);
    }
}
