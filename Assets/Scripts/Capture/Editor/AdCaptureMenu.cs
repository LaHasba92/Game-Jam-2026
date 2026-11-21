using System.IO;
using UnityEditor;

internal static class AdCaptureMenu
{
    private const string Trailers = "CanonPOW/Capture pub/Trailers/";
    private const string Fronde = Trailers + "01 - De la fronde au canon (25 s)/";
    private const string Steel = Trailers + "02 - À quelques PV (15 s)/";
    private const string Moon = Trailers + "03 - Défi lunaire (15 s)/";

    [MenuItem("CanonPOW/Capture pub/00 - Exporter la pub complète (30 s)")]
    private static void Export() => RunSequence(true);

    [MenuItem("CanonPOW/Capture pub/00 - Prévisualiser à 0,5× (F5)")]
    private static void PreviewSlow() => RunSequence(false, AdCaptureDirector.SlowPreviewSpeed);

    [MenuItem("CanonPOW/Capture pub/00 - Prévisualiser à 1×")]
    private static void PreviewNormal() => RunSequence(false);

    [MenuItem(Fronde + "Exporter le MP4")]
    private static void ExportFronde() => RunTrailer(AdCaptureDirector.Trailer.Fronde, true);

    [MenuItem(Fronde + "Prévisualiser à 1×")]
    private static void PreviewFronde() => RunTrailer(AdCaptureDirector.Trailer.Fronde, false);

    [MenuItem(Steel + "Exporter le MP4")]
    private static void ExportSteel() => RunTrailer(AdCaptureDirector.Trailer.Steel, true);

    [MenuItem(Steel + "Prévisualiser à 1×")]
    private static void PreviewSteel() => RunTrailer(AdCaptureDirector.Trailer.Steel, false);

    [MenuItem(Moon + "Exporter le MP4")]
    private static void ExportMoon() => RunTrailer(AdCaptureDirector.Trailer.Moon, true);

    [MenuItem(Moon + "Prévisualiser à 1×")]
    private static void PreviewMoon() => RunTrailer(AdCaptureDirector.Trailer.Moon, false);

    [MenuItem("CanonPOW/Capture pub/01 - Accroche, un seul tir (F6)")]
    private static void Intro() => Play(AdCaptureDirector.Take.Intro);

    [MenuItem("CanonPOW/Capture pub/02 - Lance-pierre et tir raté (F7 ou F8)")]
    private static void Miss() => Play(AdCaptureDirector.Take.Miss);

    [MenuItem("CanonPOW/Capture pub/04 - Boutique (F9)")]
    private static void Shop() => Play(AdCaptureDirector.Take.Shop);

    [MenuItem("CanonPOW/Capture pub/05 - Transformation du canon et tir (F10)")]
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

    private static void RunSequence(bool record, float speed = 1f)
    {
        if (EditorApplication.isPlaying)
            AdCaptureDirector.PlaySequence(record, speed);
        else
            EditorUtility.DisplayDialog("Capture pub", "Ouvre la scène Game et clique sur Play.", "OK");
    }

    private static void RunTrailer(AdCaptureDirector.Trailer trailer, bool record)
    {
        if (EditorApplication.isPlaying)
            AdCaptureDirector.PlayTrailer(trailer, record);
        else
            EditorUtility.DisplayDialog("Capture pub", "Ouvre la scène Game et clique sur Play.", "OK");
    }
}
