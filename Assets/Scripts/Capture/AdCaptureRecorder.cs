#if UNITY_EDITOR
using System;
using System.Collections;
using System.IO;
using Unity.Collections;
using UnityEditor.Media;
using UnityEngine;

// Enregistre la vue Game et les effets sonores de la pub dans un MP4.
public sealed class AdCaptureRecorder : MonoBehaviour
{
    public const int FrameRate = 30;
    private const int OutputWidth = 1080;
    private const int OutputHeight = 1920;
    private const float OutputAspect = OutputWidth / (float)OutputHeight;
    private const float AspectTolerance = 0.02f;
    private const int Channels = 2;
    private static AdCaptureRecorder current;

    public static bool IsRecording => current != null && current.encoder != null;

    private MediaEncoder encoder;
    private RenderTexture screenTexture;
    private RenderTexture outputTexture;
    private Vector2 blitScale;
    private Vector2 blitOffset;
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

    public static bool Begin(int seconds)
    {
        int width = Screen.width;
        int height = Screen.height;
        float aspect = height > 0 ? width / (float)height : 0f;
        if (Mathf.Abs(aspect / OutputAspect - 1f) > AspectTolerance)
        {
            Debug.LogWarning($"Capture pub : choisis le preset 9:16 en mode Aspect dans la vue Game. Taille actuelle : {width} × {height}.");
            return false;
        }

        StopCurrent();
        current = new GameObject("CanonPOW — Enregistrement MP4").AddComponent<AdCaptureRecorder>();
        current.StartRecording(width, height, seconds * FrameRate);
        return IsRecording;
    }

    public static void StopCurrent()
    {
        if (current == null) return;
        var recorder = current;
        current = null;
        recorder.enabled = false;
        Destroy(recorder.gameObject);
    }

    private void StartRecording(int width, int height, int maxFrames)
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
                includeAlpha = false
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
            screenTexture = new RenderTexture(width, height, 0, RenderTextureFormat.ARGB32)
            {
                filterMode = FilterMode.Bilinear,
                wrapMode = TextureWrapMode.Clamp
            };
            screenTexture.Create();
            if (width != OutputWidth || height != OutputHeight)
            {
                outputTexture = new RenderTexture(OutputWidth, OutputHeight, 0, RenderTextureFormat.ARGB32);
                outputTexture.Create();
                float sourceAspect = width / (float)height;
                // Un léger recadrage absorbe l'arrondi de la vue Game sans déformer l'image.
                blitScale = new Vector2(Mathf.Min(1f, OutputAspect / sourceAspect),
                    Mathf.Min(1f, sourceAspect / OutputAspect));
                blitOffset = (Vector2.one - blitScale) * 0.5f;
            }
            frameTexture = new Texture2D(OutputWidth, OutputHeight, TextureFormat.RGBA32, false);
            previousCaptureDeltaTime = Time.captureDeltaTime;
            Time.captureFramerate = FrameRate;
            clockChanged = true;
            Debug.Log($"Capture pub : vue {width} × {height}, export {OutputWidth} × {OutputHeight}. F12 pour arrêter. {path}");
            StartCoroutine(RecordFrames(maxFrames));
        }
        catch (Exception exception)
        {
            Debug.LogError($"Capture pub : impossible d’ouvrir l’encodeur MP4. {exception.Message}");
            StopCurrent();
        }
    }

    private IEnumerator RecordFrames(int maxFrames)
    {
        var endOfFrame = new WaitForEndOfFrame();
        while (frames < maxFrames)
        {
            yield return endOfFrame;
            if (Screen.width != screenTexture.width || Screen.height != screenTexture.height)
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
            if (outputTexture != null)
            {
                GL.sRGBWrite = QualitySettings.activeColorSpace == ColorSpace.Linear && outputTexture.sRGB;
                Graphics.Blit(screenTexture, outputTexture, blitScale, blitOffset);
            }
            RenderTexture.active = outputTexture != null ? outputTexture : screenTexture;
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
        if (outputTexture != null)
        {
            outputTexture.Release();
            Destroy(outputTexture);
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
    }
}
#endif
