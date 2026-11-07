using System.Collections;
using UnityEngine;

/// <summary>Pak Dagang (P6): hadiah 3 Ramuan sekali saja (flag gift).</summary>
public class MerchantNpc : Npc
{
    [SerializeField] private int giftPotions = 3;
    [SerializeField] private AudioClip giftClip;          // SFX_Heal

    protected override IEnumerator Talk()
    {
        if (Flags.Has(StoryFlags.Gift))
        {
            yield return Dialogue.Say(NpcName, "Ramuan memulihkan 40% HP partnermu saat battle. Pakai dengan bijak!");
            yield break;
        }

        yield return Dialogue.Say(NpcName, "Mau ke hutan? Jangan lupa bekal. Ini, ambil gratis!");
        Services.Party.AddPotions(giftPotions);
        Flags.Set(StoryFlags.Gift);
        Services.Audio?.PlaySfx(giftClip, 0f);
        yield return Dialogue.Say("", $"Kamu menerima {giftPotions} Ramuan!");
        Services.Toast?.Show($"Ramuan +{giftPotions}");
    }
}
