#if UNITY_EDITOR
using System;
using System.Collections;
using System.IO;
using UnityEditor.Media;
using UnityEngine;

// Enregistre la vue Game, y compris les Canvas, dans un MP4 vidéo pour le montage.
public sealed class AdCaptureRecorder : MonoBehaviour
{
    private const int FrameRate = 30;
    private const int MaxFrames = 45 * FrameRate;
    private static AdCaptureRecorder current;

    private MediaEncoder encoder;
    private RenderTexture screenTexture;
    private Texture2D frameTexture;
    private float previousCaptureDeltaTime;
    private bool clockChanged;
    private int frames;
    private string path;

    public static string Folder
    {
        get
        {
            string videos = Environment.GetFolderPath(Environment.SpecialFolder.MyVideos);
            return Path.Combine(string.IsNullOrEmpty(videos) ? Application.persistentDataPath : videos,
                "CanonPOW-Captures");
        }
    }

    public static void Toggle()
    {
        if (current != null)
        {
            StopCurrent();
            return;
        }

        int width = Screen.width;
        int height = Screen.height;
        if (width != 1080 || height != 1920)
        {
            Debug.LogWarning($"Capture pub : sélectionne une Game View en 1080 × 1920. Taille actuelle : {width} × {height}.");
            return;
        }

        current = new GameObject("CanonPOW — Enregistrement MP4").AddComponent<AdCaptureRecorder>();
        current.StartRecording(width, height);
    }

    public static void StopCurrent()
    {
        if (current == null) return;
        var recorder = current;
        current = null;
        recorder.enabled = false;
        Destroy(recorder.gameObject);
    }

    private void StartRecording(int width, int height)
    {
        try
        {
            Directory.CreateDirectory(Folder);
            path = Path.Combine(Folder, $"CanonPOW-{DateTime.Now:yyyyMMdd-HHmmss-fff}.mp4");
            encoder = new MediaEncoder(path, new VideoTrackAttributes
            {
                frameRate = new MediaRational(FrameRate),
                width = (uint)width,
                height = (uint)height,
                includeAlpha = false
            });
            screenTexture = new RenderTexture(width, height, 0, RenderTextureFormat.ARGB32);
            screenTexture.Create();
            frameTexture = new Texture2D(width, height, TextureFormat.RGBA32, false);
            previousCaptureDeltaTime = Time.captureDeltaTime;
            Time.captureFramerate = FrameRate;
            clockChanged = true;
            Debug.Log($"Capture pub : enregistrement lancé. F12 pour arrêter. {path}");
            StartCoroutine(RecordFrames());
        }
        catch (Exception exception)
        {
            Debug.LogError($"Capture pub : impossible d’ouvrir l’encodeur MP4. {exception.Message}");
            StopCurrent();
        }
    }

    private IEnumerator RecordFrames()
    {
        var endOfFrame = new WaitForEndOfFrame();
        while (frames < MaxFrames)
        {
            yield return endOfFrame;
            if (Screen.width != frameTexture.width || Screen.height != frameTexture.height)
            {
                Debug.LogWarning("Capture pub : la taille de la vue Game a changé. L’enregistrement est arrêté.");
                break;
            }

            if (!SaveFrame())
                break;

            ++frames;
        }
        StopCurrent();
    }

    private bool SaveFrame()
    {
        RenderTexture previous = RenderTexture.active;
        try
        {
            ScreenCapture.CaptureScreenshotIntoRenderTexture(screenTexture);
            RenderTexture.active = screenTexture;
            frameTexture.ReadPixels(new Rect(0f, 0f, frameTexture.width, frameTexture.height), 0, 0, false);
            if (encoder.AddFrame(frameTexture)) return true;
            Debug.LogError("Capture pub : l’encodeur MP4 a refusé une image.");
        }
        catch (Exception exception)
        {
            Debug.LogError($"Capture pub : arrêt de l’enregistrement. {exception.Message}");
        }
        finally
        {
            RenderTexture.active = previous;
        }
        return false;
    }

    private void OnDisable()
    {
        StopAllCoroutines();
        if (current == this) current = null;
        if (clockChanged)
            Time.captureDeltaTime = previousCaptureDeltaTime;
        if (screenTexture != null)
        {
            screenTexture.Release();
            Destroy(screenTexture);
        }
        if (frameTexture != null) Destroy(frameTexture);

        try
        {
            encoder?.Dispose();
            if (frames > 0) Debug.Log($"Capture pub : MP4 enregistré ({frames} images) dans {path}");
        }
        catch (Exception exception)
        {
            Debug.LogError($"Capture pub : la finalisation du MP4 a échoué. {exception.Message}");
        }
    }
}
#endif
