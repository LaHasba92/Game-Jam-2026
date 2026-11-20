using System.Collections.Generic;
using UnityEngine;

// La capture superpose ses valeurs en mémoire aux préférences du joueur.
public static class JamSave
{
    private static readonly Dictionary<string, int> ints = new Dictionary<string, int>();
    private static readonly Dictionary<string, float> floats = new Dictionary<string, float>();
    private static readonly Dictionary<string, string> strings = new Dictionary<string, string>();
    public static bool Preview { get; private set; }

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    private static void ResetPreview() => SetPreview(false);

    public static void SetPreview(bool enabled)
    {
        ints.Clear();
        floats.Clear();
        strings.Clear();
        Preview = enabled;
    }
    public static int GetInt(string key, int fallback = 0) =>
        Preview && ints.TryGetValue(key, out int value) ? value : PlayerPrefs.GetInt(key, fallback);
    public static float GetFloat(string key) =>
        Preview && floats.TryGetValue(key, out float value) ? value : PlayerPrefs.GetFloat(key);
    public static string GetString(string key) =>
        Preview && strings.TryGetValue(key, out string value) ? value : PlayerPrefs.GetString(key);
    public static void SetInt(string key, int value)
    {
        if (Preview) ints[key] = value;
        else PlayerPrefs.SetInt(key, value);
    }
    public static void SetFloat(string key, float value)
    {
        if (Preview) floats[key] = value;
        else PlayerPrefs.SetFloat(key, value);
    }
    public static void SetString(string key, string value)
    {
        if (Preview) strings[key] = value;
        else PlayerPrefs.SetString(key, value);
    }
    public static void Save() { if (!Preview) PlayerPrefs.Save(); }
}
