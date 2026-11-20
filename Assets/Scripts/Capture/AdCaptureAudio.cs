#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using UnityEngine;

// Les mêmes échantillons sont joués dans Unity et mélangés à chaque image du MP4.
public sealed class AdCaptureAudio : MonoBehaviour
{
    public enum Cue { Whoosh, Slingshot, Cannon, Break, WeakFail, Steel, Confirm, End }

    public const int SampleRate = 48000;
    private const int Channels = 2;
    private const int MaxVoices = 24;
    private const float Tau = 2f * Mathf.PI;
    private static AdCaptureAudio instance;

    private readonly Dictionary<Cue, Sound> sounds = new Dictionary<Cue, Sound>();
    private readonly List<Voice> voices = new List<Voice>(MaxVoices);
    private AudioSource source;
    private bool mixing;

    private sealed class Sound
    {
        public AudioClip Clip;
        public float[] Samples;
    }

    private struct Voice
    {
        public Sound Sound;
        public int Position;
    }

    public static void PlayCue(Cue cue)
    {
        if (!Application.isPlaying) return;

        AdCaptureAudio audio = GetInstance();
        Sound sound = audio.sounds[cue];
        audio.source.PlayOneShot(sound.Clip);
        if (!audio.mixing) return;

        if (audio.voices.Count == MaxVoices) audio.voices.RemoveAt(0);
        audio.voices.Add(new Voice { Sound = sound });
    }

    public static void ResetMix()
    {
        if (!Application.isPlaying) return;

        AdCaptureAudio audio = GetInstance();
        audio.source.Stop();
        audio.voices.Clear();
        audio.mixing = true;
    }

    public static void FillAudioFrame(float[] stereo, int frameRate)
    {
        Array.Clear(stereo, 0, stereo.Length);
        if (instance == null || !instance.mixing || frameRate <= 0) return;

        int samplesThisFrame = Mathf.Min(stereo.Length / Channels, SampleRate / frameRate) * Channels;
        instance.Mix(stereo, samplesThisFrame);
    }

    private static AdCaptureAudio GetInstance()
    {
        if (instance != null) return instance;

        var root = new GameObject("CanonPOW — Sons de capture");
        instance = root.AddComponent<AdCaptureAudio>();
        instance.source = root.AddComponent<AudioSource>();
        instance.source.playOnAwake = false;
        instance.source.spatialBlend = 0f;
        instance.source.ignoreListenerPause = true;
        foreach (Cue cue in Enum.GetValues(typeof(Cue)))
        {
            instance.sounds.Add(cue, CreateSound(cue));
        }
        return instance;
    }

    private void Mix(float[] stereo, int count)
    {
        for (int voiceIndex = voices.Count - 1; voiceIndex >= 0; --voiceIndex)
        {
            Voice voice = voices[voiceIndex];
            float[] samples = voice.Sound.Samples;
            int length = Mathf.Min(count, samples.Length - voice.Position);
            for (int i = 0; i < length; ++i)
            {
                stereo[i] += samples[voice.Position + i];
            }

            voice.Position += length;
            if (voice.Position == samples.Length) voices.RemoveAt(voiceIndex);
            else voices[voiceIndex] = voice;
        }

        for (int i = 0; i < count; ++i)
        {
            float magnitude = Mathf.Abs(stereo[i]);
            if (magnitude > 0.75f)
            {
                float excess = magnitude - 0.75f;
                stereo[i] = Mathf.Sign(stereo[i]) * (0.75f + excess / (1f + 4f * excess));
            }
        }
    }

    private void OnDestroy()
    {
        foreach (Sound sound in sounds.Values)
        {
            Destroy(sound.Clip);
        }
        if (instance == this) instance = null;
    }

    private static Sound CreateSound(Cue cue)
    {
        float duration = Duration(cue);
        int sampleCount = Mathf.CeilToInt(duration * SampleRate);
        var samples = new float[sampleCount * Channels];
        var noise = new System.Random(2305 + (int)cue);
        float body = 0f;
        float edge = 0f;
        for (int i = 0; i < sampleCount; ++i)
        {
            float time = (float)i / SampleRate;
            float white = 2f * (float)noise.NextDouble() - 1f;
            body += 0.035f * (white - body);
            edge += 0.22f * (white - edge);
            float fade = Mathf.Clamp01((duration - time) / 0.03f);
            float sample = Synthesize(cue, time, duration, body, edge, white) * fade;
            float pan = cue == Cue.Whoosh ? Mathf.Lerp(-0.32f, 0.32f, time / duration) : 0f;
            samples[2 * i] = sample * (1f - pan);
            samples[2 * i + 1] = sample * (1f + pan);
        }

        AudioClip clip = AudioClip.Create("CanonPOW Capture — " + cue, sampleCount, Channels, SampleRate, false);
        clip.SetData(samples, 0);
        return new Sound { Clip = clip, Samples = samples };
    }

    private static float Duration(Cue cue)
    {
        switch (cue)
        {
            case Cue.Whoosh: return 0.74f;
            case Cue.Slingshot: return 0.26f;
            case Cue.Cannon: return 0.43f;
            case Cue.Break: return 0.25f;
            case Cue.WeakFail: return 0.52f;
            case Cue.Steel: return 0.72f;
            case Cue.Confirm: return 0.35f;
            default: return 0.88f;
        }
    }

    private static float Synthesize(Cue cue, float t, float duration, float body, float edge, float white)
    {
        switch (cue)
        {
            case Cue.Whoosh:
                float sweep = Mathf.Sin(Mathf.PI * t / duration);
                return (0.52f * body + 0.34f * (edge - body)) * sweep * sweep;

            case Cue.Slingshot:
                return (0.11f * white + 0.36f * edge) * Burst(t, 0.002f, 36f)
                    + 0.25f * Tone(t, 240f, -460f) * Burst(t, 0.003f, 16f);

            case Cue.Cannon:
                return 0.43f * Tone(t, 92f, -135f) * Burst(t, 0.004f, 12f)
                    + (0.75f * body + 0.24f * white) * Burst(t, 0.002f, 14f);

            case Cue.Break:
                return (0.65f * edge + 0.18f * white) * Burst(t, 0.002f, 29f)
                    + 0.35f * (edge - body) * Burst(t - 0.057f, 0.002f, 38f)
                    + 0.14f * Tone(t, 129f, -195f) * Burst(t, 0.004f, 21f);

            case Cue.WeakFail:
                return 0.24f * Tone(t, 155f, -190f) * Burst(t, 0.005f, 11f)
                    + (0.38f * body + 0.15f * edge) * Burst(t, 0.003f, 18f)
                    + 0.12f * Tone(t - 0.105f, 89f, -65f) * Burst(t - 0.105f, 0.005f, 11f);

            case Cue.Steel:
                float ring = 0.18f * Tone(t, 467f) + 0.10f * Tone(t, 731f) + 0.08f * Tone(t, 1193f);
                return ring * Burst(t, 0.002f, 8f)
                    + (0.56f * edge + 0.11f * white) * Burst(t, 0.002f, 26f)
                    + 0.27f * Tone(t, 103f, -115f) * Burst(t, 0.004f, 16f);

            case Cue.Confirm:
                return 0.22f * Bell(t, 659.25f, 15f) + 0.26f * Bell(t - 0.073f, 987.77f, 13f);

            default:
                return 0.18f * Bell(t, 523.25f, 9f) + 0.19f * Bell(t - 0.08f, 659.25f, 9f)
                    + 0.20f * Bell(t - 0.16f, 783.99f, 8f) + 0.22f * Bell(t - 0.26f, 1046.50f, 7f);
        }
    }

    private static float Burst(float time, float attack, float decay)
    {
        return time <= 0f ? 0f : Mathf.Min(1f, time / attack) * Mathf.Exp(-decay * time);
    }

    private static float Tone(float time, float frequency, float sweepPerSecond = 0f)
    {
        return Mathf.Sin(Tau * time * (frequency + 0.5f * sweepPerSecond * time));
    }

    private static float Bell(float time, float frequency, float decay)
    {
        return (0.78f * Tone(time, frequency) + 0.22f * Tone(time, frequency * 2.01f)) * Burst(time, 0.005f, decay);
    }
}
#endif
