using System;
using UnityEngine;

/// <summary>Menerjemahkan service hidup &lt;-&gt; SaveData. Capture: Party + Flags + posisi. Apply: partner via Database.GetById.</summary>
public static class SaveMapper
{
    public static SaveData Capture(GameServices s, Vector3 playerPosition)
    {
        var data = new SaveData
        {
            savedAt = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss"),
            scene = SceneFlow.WorldScene,
            playerPosition = playerPosition,
            potions = s.Party.Potions,
            playtime = s.Party.Playtime,
            flags = s.Flags.ToList(),
        };

        MonsterInstance partner = s.Party.Partner;
        if (partner != null)
        {
            data.partnerId = partner.Data.Id;
            data.partnerLevel = partner.Level;
            data.partnerExp = partner.Exp;
            data.partnerHp = partner.CurrentHP;
        }
        return data;
    }

    public static void Apply(SaveData data, GameServices s)
    {
        s.Party.Reset();
        MonsterData species = string.IsNullOrEmpty(data.partnerId) ? null : s.Database.GetById(data.partnerId);
        if (species != null)
        {
            int hp = Mathf.Max(1, data.partnerHp);
            s.Party.SetPartner(new MonsterInstance(species, data.partnerLevel, data.partnerExp, hp));
        }
        s.Party.SetPotions(data.potions);
        s.Party.SetPlaytime(data.playtime);
        s.Flags.Load(data.flags);
    }
}
