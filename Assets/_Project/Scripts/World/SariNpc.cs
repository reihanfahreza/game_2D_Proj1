using System.Collections;

/// <summary>
/// Sari (tugas P6 no. 2): sebelum boss kalah memakai DialogueData tips elemen (versi dasar Npc),
/// setelah flag boss_defeated isi Talk() berubah.
/// </summary>
public class SariNpc : Npc
{
    protected override IEnumerator Talk()
    {
        if (Flags.Has(StoryFlags.BossDefeated))
        {
            yield return Dialogue.Say(NpcName, "Kamu benar-benar mengalahkan Raijugar?! Seluruh desa membicarakanmu!");
            yield return Dialogue.Say(NpcName, "Lain kali ajak aku ke Gua Petir, ya.");
            yield break;
        }
        yield return base.Talk();
    }
}
