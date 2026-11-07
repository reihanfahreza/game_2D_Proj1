using System;
using System.Collections.Generic;

/// <summary>State cerita (P6): has_starter, gift, intro_seen, boss_defeated. Ikut disimpan ke save.</summary>
public class StoryFlags
{
    public const string HasStarter = "has_starter";
    public const string Gift = "gift";
    public const string IntroSeen = "intro_seen";
    public const string BossDefeated = "boss_defeated";

    private readonly HashSet<string> flags = new HashSet<string>();

    /// <summary>Argumen = flag yang berubah; null saat semua flag dimuat ulang.</summary>
    public event Action<string> Changed;

    public bool Has(string key) => flags.Contains(key);

    public void Set(string key)
    {
        if (flags.Add(key)) Changed?.Invoke(key);
    }

    public void Load(IEnumerable<string> keys)
    {
        flags.Clear();
        if (keys != null)
            foreach (string key in keys) flags.Add(key);
        Changed?.Invoke(null);
    }

    public List<string> ToList() => new List<string>(flags);
}
