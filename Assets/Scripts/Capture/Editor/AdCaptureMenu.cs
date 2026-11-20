using System.IO;
using UnityEditor;

internal static class AdCaptureMenu
{
    [MenuItem("CanonPOW/Capture pub/01 - Travelling vers le mur (F6)")]
    private static void Intro() => Play(AdCaptureDirector.Take.Intro);

    [MenuItem("CanonPOW/Capture pub/02 - Lance-pierre en bois (F7)")]
    private static void Slingshot() => Play(AdCaptureDirector.Take.Slingshot);

    [MenuItem("CanonPOW/Capture pub/03 - Tir rate (F8)")]
    private static void Miss() => Play(AdCaptureDirector.Take.Miss);

    [MenuItem("CanonPOW/Capture pub/04 - Boutique (F9)")]
    private static void Shop() => Play(AdCaptureDirector.Take.Shop);

    [MenuItem("CanonPOW/Capture pub/05 - Canon puis mur a quelques PV (F10)")]
    private static void Canon() => Play(AdCaptureDirector.Take.Canon);

    [MenuItem("CanonPOW/Capture pub/06 - Carton final (F11)")]
    private static void EndCard() => Play(AdCaptureDirector.Take.EndCard);

    [MenuItem("CanonPOW/Capture pub/Quitter le mode capture")]
    private static void Exit() => AdCaptureDirector.Exit();

    [MenuItem("CanonPOW/Capture pub/Enregistrer ou arrêter le MP4 (F12)")]
    private static void Record()
    {
        if (EditorApplication.isPlaying)
            AdCaptureRecorder.Toggle();
        else
            EditorUtility.DisplayDialog("Capture pub", "Clique sur Play avant d’enregistrer la vue Game.", "OK");
    }

    [MenuItem("CanonPOW/Capture pub/Ouvrir le dossier des MP4")]
    private static void OpenFolder()
    {
        Directory.CreateDirectory(AdCaptureRecorder.Folder);
        EditorUtility.RevealInFinder(AdCaptureRecorder.Folder);
    }

    private static void Play(AdCaptureDirector.Take take)
    {
        if (EditorApplication.isPlaying)
            AdCaptureDirector.PlayTake(take);
        else
            EditorUtility.DisplayDialog("Capture pub", "Ouvre la scène Game, clique sur Play, puis choisis ton plan.", "OK");
    }
}
