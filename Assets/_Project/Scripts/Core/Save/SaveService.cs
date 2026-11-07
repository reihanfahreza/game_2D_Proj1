using System;
using System.IO;
using UnityEngine;

/// <summary>
/// Simpan/muat JSON di persistentDataPath/save_0.json
/// (Windows: AppData/LocalLow/Politeknik Negeri Batam/Pulau Monster).
/// </summary>
public class SaveService
{
    public const int CurrentVersion = 1;

    private readonly string path;

    public SaveService(string fileName = "save_0.json")
    {
        path = Path.Combine(Application.persistentDataPath, fileName);
    }

    public string FilePath => path;

    /// <summary>Diisi TitleInstaller saat Lanjutkan; dipakai WorldInstaller untuk posisi pemain.</summary>
    public SaveData PendingLoad { get; set; }

    public bool HasSave() => File.Exists(path);

    public bool Save(SaveData data)
    {
        try
        {
            data.version = CurrentVersion;
            File.WriteAllText(path, JsonUtility.ToJson(data, true));
            return true;
        }
        catch (Exception e)
        {
            Debug.LogWarning($"Gagal menyimpan ke {path}: {e.Message}");
            return false;
        }
    }

    public SaveData Load()
    {
        try
        {
            if (!HasSave()) return null;
            return JsonUtility.FromJson<SaveData>(File.ReadAllText(path));
        }
        catch (Exception e)
        {
            Debug.LogWarning($"Gagal memuat {path}: {e.Message}");
            return null;
        }
    }

    public void Delete()
    {
        if (HasSave()) File.Delete(path);
    }
}
