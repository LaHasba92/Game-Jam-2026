#if UNITY_EDITOR
using System;
using System.Collections;
using System.IO;
using Unity.Collections;
using UnityEditor;
using UnityEditor.Media;
using UnityEngine;

// Enregistre la vue Game et les effets sonores de la pub dans un MP4.
public sealed class AdCaptureRecorder : MonoBehaviour
{
    public const int FrameRate = 30;
    private const int OutputWidth = 1080;
    private const int OutputHeight = 1920;
    private const float ResolutionTimeout = 10f;
    private const int Channels = 2;
    private static AdCaptureRecorder current;

    public static bool IsRecording => current != null && current.encoder != null;

    private MediaEncoder encoder;
    private RenderTexture screenTexture;
    private RenderTexture uprightTexture;
    private Texture2D frameTexture;
    private float[] audioMix;
    private NativeArray<float> audioFrame;
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

        Begin(45);
    }

    public static void Begin(int seconds, Action onStarted = null)
    {
        StopCurrent();
        current = new GameObject("CanonPOW — Enregistrement MP4").AddComponent<AdCaptureRecorder>();
        current.StartCoroutine(current.PrepareRecording(seconds * FrameRate, onStarted));
    }

    public static void StopCurrent()
    {
        if (current == null) return;
        var recorder = current;
        current = null;
        recorder.enabled = false;
        Destroy(recorder.gameObject);
    }

    private static bool HasOutputResolution => Screen.width == OutputWidth && Screen.height == OutputHeight;

    private IEnumerator PrepareRecording(int maxFrames, Action onStarted)
    {
        if (AdCaptureGameView.TrySetResolution(OutputWidth, OutputHeight) || HasOutputResolution)
        {
            // La vue Game applique son nouveau rendu lors des prochaines frames.
            float deadline = Time.realtimeSinceStartup + ResolutionTimeout;
            int matchingFrames = 0;
            do
            {
                yield return null;
                matchingFrames = HasOutputResolution ? matchingFrames + 1 : 0;
                if (matchingFrames == 2)
                {
                    StartRecording(maxFrames, onStarted);
                    yield break;
                }
            } while (Time.realtimeSinceStartup < deadline);
        }

        Debug.LogWarning($"Capture pub : sélectionne 1080 × 1920 en résolution fixe dans la vue Game, puis relance l’export. Taille actuelle : {Screen.width} × {Screen.height}.");
        StopCurrent();
    }

    private void StartRecording(int maxFrames, Action onStarted)
    {
        try
        {
            Directory.CreateDirectory(Folder);
            path = Path.Combine(Folder, $"CanonPOW-{DateTime.Now:yyyyMMdd-HHmmss-fff}.mp4");
            var video = new VideoTrackAttributes
            {
                frameRate = new MediaRational(FrameRate),
                width = OutputWidth,
                height = OutputHeight,
                includeAlpha = false,
                bitRateMode = VideoBitrateMode.High
            };
            var audio = new AudioTrackAttributes
            {
                sampleRate = new MediaRational(AdCaptureAudio.SampleRate),
                channelCount = Channels,
                language = "fr"
            };
            encoder = new MediaEncoder(path, video, audio);
            audioMix = new float[AdCaptureAudio.SampleRate / FrameRate * Channels];
            audioFrame = new NativeArray<float>(audioMix.Length, Allocator.Persistent);
            AdCaptureAudio.ResetMix();
            screenTexture = new RenderTexture(OutputWidth, OutputHeight, 0, RenderTextureFormat.ARGB32)
            {
                filterMode = FilterMode.Point,
                wrapMode = TextureWrapMode.Clamp
            };
            screenTexture.Create();
            // La capture de la vue Game et l’encodeur ont des conventions Y opposées sous DirectX.
            if (SystemInfo.graphicsUVStartsAtTop)
            {
                uprightTexture = new RenderTexture(OutputWidth, OutputHeight, 0, RenderTextureFormat.ARGB32);
                uprightTexture.Create();
            }
            frameTexture = new Texture2D(OutputWidth, OutputHeight, TextureFormat.RGBA32, false);
            previousCaptureDeltaTime = Time.captureDeltaTime;
            Time.captureFramerate = FrameRate;
            clockChanged = true;
            onStarted?.Invoke();
            Debug.Log($"Capture pub : enregistrement natif {OutputWidth} × {OutputHeight} à {FrameRate} i/s. F12 pour arrêter. {path}");
            StartCoroutine(RecordFrames(maxFrames));
        }
        catch (Exception exception)
        {
            Debug.LogError($"Capture pub : impossible de démarrer l’enregistrement MP4. {exception.Message}");
            StopCurrent();
        }
    }

    private IEnumerator RecordFrames(int maxFrames)
    {
        var endOfFrame = new WaitForEndOfFrame();
        while (frames < maxFrames)
        {
            yield return endOfFrame;
            if (!HasOutputResolution)
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
        bool previousSrgbWrite = GL.sRGBWrite;
        try
        {
            ScreenCapture.CaptureScreenshotIntoRenderTexture(screenTexture);
            if (uprightTexture != null)
            {
                GL.sRGBWrite = QualitySettings.activeColorSpace == ColorSpace.Linear && uprightTexture.sRGB;
                Graphics.Blit(screenTexture, uprightTexture, new Vector2(1f, -1f), new Vector2(0f, 1f));
            }
            RenderTexture.active = uprightTexture != null ? uprightTexture : screenTexture;
            frameTexture.ReadPixels(new Rect(0f, 0f, frameTexture.width, frameTexture.height), 0, 0, false);
            if (!encoder.AddFrame(frameTexture))
            {
                Debug.LogError("Capture pub : l’encodeur MP4 a refusé une image.");
                return false;
            }
            AdCaptureAudio.FillAudioFrame(audioMix, FrameRate);
            audioFrame.CopyFrom(audioMix);
            if (!encoder.AddSamples(audioFrame))
            {
                Debug.LogError("Capture pub : l’encodeur MP4 a refusé les échantillons audio.");
                return false;
            }
            return true;
        }
        catch (Exception exception)
        {
            Debug.LogError($"Capture pub : arrêt de l’enregistrement. {exception.Message}");
        }
        finally
        {
            GL.sRGBWrite = previousSrgbWrite;
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
        if (uprightTexture != null)
        {
            uprightTexture.Release();
            Destroy(uprightTexture);
        }
        if (frameTexture != null) Destroy(frameTexture);
        if (audioFrame.IsCreated) audioFrame.Dispose();

        try
        {
            encoder?.Dispose();
            if (frames > 0) Debug.Log($"Capture pub : MP4 enregistré ({frames} images) dans {path}");
        }
        catch (Exception exception)
        {
            Debug.LogError($"Capture pub : la finalisation du MP4 a échoué. {exception.Message}");
        }
        finally
        {
            AdCaptureGameView.Restore();
        }
    }
}
#endif
