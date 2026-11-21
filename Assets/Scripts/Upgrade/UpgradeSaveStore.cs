using System;
using System.IO;
using System.Text;
using UnityEngine;

public static class UpgradeSaveStore
{
    public const string DefaultFileName = "upgrade-test.json";

    public static UpgradeSaveData Load(string fileName = DefaultFileName)
    {
        string path = Path.Combine(Application.persistentDataPath, fileName);
        if (!File.Exists(path)) return new UpgradeSaveData();

        return JsonUtility.FromJson<UpgradeSaveData>(File.ReadAllText(path))
            ?? throw new InvalidDataException("La sauvegarde de progression est illisible.");
    }

    public static void Save(UpgradeSaveData data, string fileName = DefaultFileName)
    {
        if (data == null) throw new ArgumentNullException(nameof(data));

        string path = Path.Combine(Application.persistentDataPath, fileName);
        string directory = Path.GetDirectoryName(path);
        Directory.CreateDirectory(directory);
        string pendingPath = Path.Combine(directory, Path.GetFileName(path) + ".tmp");
        string json = JsonUtility.ToJson(data, true);

        // Les écritures sont synchrones sur le thread principal. Le fichier précédent
        // reste lisible si l'écriture temporaire échoue.
        using (var stream = new FileStream(pendingPath, FileMode.Create, FileAccess.Write, FileShare.None))
        {
            byte[] bytes = Encoding.UTF8.GetBytes(json);
            stream.Write(bytes, 0, bytes.Length);
            stream.Flush(true);
        }
        if (File.Exists(path))
            File.Replace(pendingPath, path, null);
        else
            File.Move(pendingPath, path);
    }
}
