using System.Collections;
using UnityEngine;

/// <summary>
/// Tetua Rahmat (P6): pilih partner -> "Yakin?" -> Ya / Pilih lagi. Partner baru di Lv 3.
/// Setelah memberi partner, flag has_starter membuat Penjaga Batas bergeser.
/// </summary>
public class StarterGiver : Npc
{
    [SerializeField] private MonsterData[] starters;
    [SerializeField] private int starterLevel = 3;
    [SerializeField] private AudioClip getClip;          // SFX_LevelUp
    [SerializeField] private GameObject questMarker;     // tanda ! di atas Tetua sebelum punya partner

    protected override void OnConstructed()
    {
        Flags.Changed += OnFlag;
        RefreshMarker();
    }

    protected override void OnDestroy()
    {
        if (Flags != null) Flags.Changed -= OnFlag;
    }

    private void OnFlag(string key) => RefreshMarker();

    private void RefreshMarker()
    {
        if (questMarker != null) questMarker.SetActive(!Flags.Has(StoryFlags.HasStarter));
    }

    protected override IEnumerator Talk()
    {
        if (Flags.Has(StoryFlags.HasStarter))
        {
            yield return AfterStarter();
            yield break;
        }

        yield return Dialogue.Say(NpcName, "Ah, kamu datang juga! Hutan di utara penuh monster liar.");
        yield return Dialogue.Say(NpcName, "Tidak ada yang boleh masuk hutan tanpa partner. Pilih satu dari tiga ini.");

        string[] options = new string[starters.Length];
        for (int i = 0; i < starters.Length; i++)
            options[i] = $"{starters[i].DisplayName} ({starters[i].Element})";

        MonsterData pick = null;
        while (pick == null)              // ulangi sampai "Ya"
        {
            int choice = 0, sure = 0;
            yield return Dialogue.Choose(NpcName, "Siapa yang akan menjadi partnermu?", options, c => choice = c);
            MonsterData m = starters[choice];
            yield return Dialogue.Choose(NpcName, $"{m.DisplayName} tipe {m.Element}. Yakin?",
                new[] { "Ya", "Pilih lagi" }, c => sure = c);
            if (sure == 0) pick = m;
        }

        Services.Party.SetPartner(new MonsterInstance(pick, starterLevel)); // Lv 3
        Flags.Set(StoryFlags.HasStarter);                                   // Penjaga bergeser
        Services.Audio?.PlaySfx(getClip, 0f);

        yield return Dialogue.Say("", $"Kamu mendapat {pick.DisplayName}!");
        yield return Dialogue.Say(NpcName, "Rawat dia baik-baik. Menang bertarung membuatnya naik level, lalu berevolusi.");
        yield return Dialogue.Say(NpcName, "Batu Simpan di sebelahku menyimpan perjalananmu dan memulihkan partnermu.");
        Services.Toast?.Show("Penjaga Batas sudah membuka jalan ke hutan");
    }

    private IEnumerator AfterStarter()
    {
        MonsterInstance partner = Services.Party.Partner;
        string name = partner != null ? partner.Data.DisplayName : "partnermu";
        if (Flags.Has(StoryFlags.BossDefeated))
        {
            yield return Dialogue.Say(NpcName, "Raijugar sudah tunduk padamu. Desa ini bangga punya pelatih sepertimu!");
            yield break;
        }
        yield return Dialogue.Say(NpcName, $"Bagaimana kabar {name}? Latih dia di rumput tinggi.");
        yield return Dialogue.Say(NpcName, "Kalau sudah kuat, Gua Petir di timur laut menunggumu. Raijugar berdiam di sana.");
    }

    /// <summary>Warna khas elemen; dipakai juga oleh BattleFeel (P7) untuk percikan.</summary>
    public static Color ElementColor(Element element)
    {
        switch (element)
        {
            case Element.Pyro: return new Color32(255, 122, 61, 255);
            case Element.Aqua: return new Color32(79, 180, 255, 255);
            case Element.Sprout: return new Color32(124, 192, 97, 255);
            default: return new Color32(255, 216, 74, 255);   // Volt
        }
    }
}
