using System;
using UnityEngine;

/// <summary>Service (P4): partner pemain + Ramuan. P5 menambah event untuk HUD, P7 menambah waktu bermain.</summary>
public class PlayerParty
{
    public const int StartPotions = 3;

    public MonsterInstance Partner { get; private set; }
    public int Potions { get; private set; } = StartPotions;
    public float Playtime { get; private set; }

    public event Action PartnerChanged;
    public event Action PotionsChanged;

    public void SetPartner(MonsterInstance monster)
    {
        Partner = monster;
        PartnerChanged?.Invoke();
    }

    public void SetPotions(int amount)
    {
        Potions = Mathf.Max(0, amount);
        PotionsChanged?.Invoke();
    }

    public void AddPotions(int amount) => SetPotions(Potions + amount);

    public bool UsePotion()
    {
        if (Potions <= 0) return false;
        SetPotions(Potions - 1);
        return true;
    }

    public void AddPlaytime(float seconds) => Playtime += seconds;
    public void SetPlaytime(float seconds) => Playtime = Mathf.Max(0f, seconds);

    /// <summary>Mulai baru: Ramuan 3, tanpa partner.</summary>
    public void Reset()
    {
        Partner = null;
        Potions = StartPotions;
        Playtime = 0f;
        PartnerChanged?.Invoke();
        PotionsChanged?.Invoke();
    }
}
