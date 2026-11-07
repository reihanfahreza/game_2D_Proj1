using System;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Data hidup satu monster (P4). Bukan MonoBehaviour: bisa dibawa ke battle dan disimpan ke file.
/// Pyrit Lv 3 (starter): MaxHP 40 x 1,2 + 6 = 54, Attack 14 x 1,2 = 17, ExpToNext 30.
/// </summary>
public class MonsterInstance : IDamageable
{
    public MonsterInstance(MonsterData data, int level)
    {
        Data = data;
        Level = Mathf.Max(1, level);
        CurrentHP = MaxHP;
    }

    /// <summary>Dipakai saat memuat save.</summary>
    public MonsterInstance(MonsterData data, int level, int exp, int hp) : this(data, level)
    {
        Exp = Mathf.Max(0, exp);
        CurrentHP = Mathf.Clamp(hp, 0, MaxHP);
    }

    public MonsterData Data { get; private set; }
    public int Level { get; private set; }
    public int Exp { get; private set; }
    public int CurrentHP { get; private set; }

    public event Action Changed;   // P5: HUD

    private float Scale => 1f + (Level - 1) * 0.1f;
    public int MaxHP => Mathf.RoundToInt(Data.BaseHP * Scale) + Level * 2;
    public int Attack => Mathf.RoundToInt(Data.BaseATK * Scale);
    public int Defense => Mathf.RoundToInt(Data.BaseDEF * Scale);
    public int Speed => Mathf.RoundToInt(Data.BaseSPD * Scale);
    public int ExpToNext => Level * 10;
    public bool IsFainted => CurrentHP <= 0;

    public void TakeDamage(int amount)
    {
        CurrentHP = Mathf.Max(0, CurrentHP - amount);
        Changed?.Invoke();
    }

    /// <summary>Mengembalikan jumlah HP yang benar-benar pulih.</summary>
    public int Heal(int amount)
    {
        int before = CurrentHP;
        CurrentHP = Mathf.Min(MaxHP, CurrentHP + Mathf.Max(0, amount));
        Changed?.Invoke();
        return CurrentHP - before;
    }

    public void HealFull()
    {
        CurrentHP = MaxHP;
        Changed?.Invoke();
    }

    /// <summary>
    /// Tambah EXP: naik level selama EXP cukup; bila Level &gt;= EvolveLevel, Data = Data.EvolvesTo.
    /// Contoh: Pyrit Lv4 + 100 EXP -> naik ke Lv 5 -> berevolusi menjadi Pyrago -> Pyrago naik ke Lv 6.
    /// </summary>
    public List<GrowthEvent> GainExp(int amount)
    {
        var events = new List<GrowthEvent>();
        Exp += Mathf.Max(0, amount);

        while (Exp >= ExpToNext)
        {
            Exp -= ExpToNext;
            MonsterData from = Data;
            int oldMax = MaxHP;
            Level++;
            CurrentHP = Mathf.Min(MaxHP, CurrentHP + (MaxHP - oldMax));
            events.Add(new GrowthEvent(GrowthType.LevelUp, from, from, Level, $"{from.DisplayName} naik ke Lv {Level}!"));

            if (Data.EvolvesTo != null && Data.EvolveLevel > 0 && Level >= Data.EvolveLevel)
            {
                int beforeMax = MaxHP;
                Data = Data.EvolvesTo;
                CurrentHP = Mathf.Min(MaxHP, CurrentHP + (MaxHP - beforeMax));
                events.Add(new GrowthEvent(GrowthType.Evolve, from, Data, Level,
                    $"{from.DisplayName} berevolusi menjadi {Data.DisplayName}!"));
            }
        }

        Changed?.Invoke();
        return events;
    }
}
