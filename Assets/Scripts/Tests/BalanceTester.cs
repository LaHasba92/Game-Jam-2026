using UnityEngine;

// Outil d'equilibrage : clic droit sur le composant > « Simuler 1000 parties ».
// Compare plusieurs profils de joueur (debut, milieu, fin) avec les vrais murs du LevelManager.
// A partager avec Nathan : il change les valeurs ici et voit tout de suite l'effet.
public class BalanceTester : MonoBehaviour
{
    [SerializeField] private LevelManager levelManager;
    [SerializeField] private int runs = 1000;
    [SerializeField] private RunSimulator.Settings[] profiles =
    {
        new RunSimulator.Settings { name = "Debut" },
        new RunSimulator.Settings { name = "Milieu", basePower = 35f, launcherMultiplier = 1.7f, drag = 0.07f, elan = 0.72f, maxElevation = 38f },
        new RunSimulator.Settings { name = "Fin", basePower = 50f, launcherMultiplier = 3f, drag = 0.04f, elan = 0.85f, bounce = 0.7f, maxElevation = 45f },
    };

    [ContextMenu("Simuler 1000 parties")]
    private void Simulate()
    {
        if (levelManager == null)
        {
            Debug.LogError("BalanceTester : glisse l'objet Wall dans la case Level Manager.");
            return;
        }
        string report = $"--- Equilibrage ({runs} parties par profil) ---";
        foreach (RunSimulator.Settings profile in profiles)
        {
            report += "\n" + RunSimulator.Report(profile, levelManager.WallDistance, runs);
        }
        Debug.Log(report);
    }
}
