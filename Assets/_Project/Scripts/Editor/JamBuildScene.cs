using UnityEditor.Build;
using UnityEditor.Build.Reporting;
using UnityEngine.SceneManagement;

// Prépare aussi la copie temporaire compilée par Unity. La scène de Benji sur disque reste sous son contrôle.
public class JamBuildScene : IProcessSceneWithReport
{
    public int callbackOrder => 0;

    public void OnProcessScene(Scene scene, BuildReport report)
    {
        if (report == null || scene.path != "Assets/_Project/Scenes/Game.unity") return;
        Scene previous = SceneManager.GetActiveScene();
        try
        {
            SceneManager.SetActiveScene(scene);
            CanonPowSetup.InstallInScene(scene);
            foreach (var root in scene.GetRootGameObjects())
                if (root.GetComponentInChildren<RunHud>(true) != null &&
                    AimTuning.Current != null && AimTuning.Current.giantWall != null) return;

            throw new BuildFailedException("CanonPOW : la scène Game doit contenir le HUD et la fiche du Mur géant.");
        }
        finally
        {
            if (previous.IsValid() && previous.isLoaded) SceneManager.SetActiveScene(previous);
        }
    }
}
