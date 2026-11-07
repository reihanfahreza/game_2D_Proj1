using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Installer Scn_World (P2 -> P7). Menyambungkan service ke isi scene:
/// Player, interaksi, monster liar, NPC, Batu Simpan, area, HUD.
/// P6: narasi pembuka (HoldFade) dan lanjutkan dari save. P7: ending setelah boss kalah.
/// </summary>
public class WorldInstaller : SceneInstaller
{
    [SerializeField] private PlayerMovement player;
    [SerializeField] private PlayerInteractor interactor;
    [SerializeField] private PartnerFollower follower;
    [SerializeField] private PartnerHud hud;
    [SerializeField] private PixelBurst burst;
    [SerializeField] private Vector2 respawnPoint = new Vector2(21.5f, 6f);   // depan Batu Simpan

    [SerializeField, TextArea] private string[] introLines =
    {
        "Di ujung Pulau Batam ada desa kecil tempat manusia dan monster hidup berdampingan.",
        "Hari ini kamu cukup umur untuk memilih partner pertamamu. Tetua Rahmat sudah menunggu.",
    };

    [SerializeField, TextArea] private string[] endingLines =
    {
        "Raijugar menunduk dan membiarkanmu lewat...",
        "Kabar kemenanganmu cepat menyebar ke seluruh Pulau Monster.",
    };

    private GameServices services;
    private readonly List<AreaZone> occupied = new List<AreaZone>();

    public PlayerMovement Player => player;

    public override void Install(GameServices s)
    {
        services = s;
        s.Player = player.transform;
        player.Construct(s.Input);
        interactor.Construct(s.Input);
        follower.Construct(s.Party, player);
        hud.Construct(s.Party);

        foreach (WildMonster wild in FindInScene<WildMonster>())
            wild.Construct(s.Battle, s.Scenes, s.Party, player.transform, s.Flags, s.Input, s.Dialogue);
        foreach (Npc npc in FindInScene<Npc>()) npc.Construct(s);
        foreach (SaveStone stone in FindInScene<SaveStone>()) stone.Construct(s, player.transform, burst);
        foreach (FlagToggle toggle in FindInScene<FlagToggle>()) toggle.Construct(s.Flags);
        foreach (AreaZone zone in FindInScene<AreaZone>())
        {
            zone.Construct(s.Audio);
            zone.Entered += OnAreaEntered;
            zone.Exited += OnAreaExited;
        }
        s.Battle.Ended += OnBattleEnded;

        if (s.Save.PendingLoad != null)
        {
            player.Teleport(s.Save.PendingLoad.playerPosition, Vector2.down);
            s.Save.PendingLoad = null;
            s.Toast.Show("Perjalanan dilanjutkan");
        }
        else if (!s.Flags.Has(StoryFlags.IntroSeen))
        {
            s.Scenes.HoldFade = true;     // tahan fade-in, narasi di atas layar hitam
            StartCoroutine(Intro());
        }
    }

    void OnDestroy()
    {
        if (services == null) return;
        services.Battle.Ended -= OnBattleEnded;
        services.Player = null;   // dunia ditutup: Player ikut hilang (jangan sentuh player.transform, bisa sudah dihancurkan)
    }

    private IEnumerator Intro()
    {
        services.Dialogue.Run(Lines(introLines));
        yield return null;
        while (services.Dialogue.IsRunning) yield return null;
        services.Flags.Set(StoryFlags.IntroSeen);
        yield return services.Scenes.ReleaseFade();   // fade-in
        services.Toast.Show("Temui Tetua Rahmat (kanan jalan desa)");
    }

    private IEnumerator Lines(string[] lines)
    {
        foreach (string line in lines) yield return services.Dialogue.Narrate(line);
    }

    // Event Ended dari BattleSession (P4) membawa wasBoss
    private void OnBattleEnded(BattleResult result, bool wasBoss)
    {
        if (result == BattleResult.Lose)
        {
            player.Teleport(respawnPoint, Vector2.up);
            services.Party.Partner?.HealFull();
            services.Toast.Show("Partner dipulihkan di depan Batu Simpan");
        }
        else if (result == BattleResult.Win && wasBoss)
        {
            StartCoroutine(Ending());
        }
    }

    private IEnumerator Ending()
    {
        while (services.Scenes.IsBusy) yield return null;
        services.Dialogue.Run(Lines(endingLines));
        yield return null;
        while (services.Dialogue.IsRunning) yield return null;
        Vector3 pos = player.transform.position;
        services.Save.Save(SaveMapper.Capture(services, pos));   // judul -> Lanjutkan membuka save setelah ending
        services.Scenes.GoTo(SceneFlow.EndScene);
    }

    private void OnAreaEntered(AreaZone zone)
    {
        occupied.Remove(zone);
        occupied.Add(zone);
        ApplyArea(zone);
    }

    private void OnAreaExited(AreaZone zone)
    {
        bool wasCurrent = occupied.Count > 0 && occupied[occupied.Count - 1] == zone;
        occupied.Remove(zone);
        if (wasCurrent && occupied.Count > 0 && gameObject.activeInHierarchy) ApplyArea(occupied[occupied.Count - 1]);
    }

    private void ApplyArea(AreaZone zone)
    {
        hud.ShowArea(zone.AreaName);
        zone.PlayMusic();
    }

    private List<T> FindInScene<T>() where T : Component
    {
        var list = new List<T>();
        foreach (GameObject root in gameObject.scene.GetRootGameObjects())
            list.AddRange(root.GetComponentsInChildren<T>(true));
        return list;
    }
}
