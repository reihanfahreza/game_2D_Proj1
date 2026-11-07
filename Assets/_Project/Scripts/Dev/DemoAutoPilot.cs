using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.LowLevel;

/// <summary>
/// Alat uji otomatis. Hanya aktif bila game dijalankan dengan argumen -autopilot:
///   PulauMonster.exe -autopilot
/// Memainkan alur demo (judul -> narasi -> partner -> battle -> gua -> Raijugar -> DEMO SELESAI -> judul),
/// menyimpan screenshot + report.txt ke persistentDataPath/autopilot, lalu keluar.
/// Tanpa argumen itu komponen ini mati dan tidak mengubah apa pun.
/// </summary>
public class DemoAutoPilot : MonoBehaviour
{
    [SerializeField] private GameBootstrap bootstrap;

    private string outDir;
    private readonly List<string> report = new List<string>();
    private int errors;
    private int failures;
    private bool aborted;

    void Awake()
    {
        if (!Environment.GetCommandLineArgs().Contains("-autopilot"))
        {
            enabled = false;
            return;
        }
        outDir = Path.Combine(Application.persistentDataPath, "autopilot");
        Directory.CreateDirectory(outDir);
        foreach (string file in Directory.GetFiles(outDir)) File.Delete(file);
        string save = Path.Combine(Application.persistentDataPath, "save_0.json");
        if (File.Exists(save)) File.Delete(save);       // mulai dari keadaan bersih
        Application.logMessageReceived += OnLog;
    }

    void Start()
    {
        if (enabled) StartCoroutine(Run());
    }

    void OnDestroy() => Application.logMessageReceived -= OnLog;

    private void OnLog(string message, string stack, LogType type)
    {
        if (type != LogType.Error && type != LogType.Exception && type != LogType.Assert) return;
        errors++;
        report.Add($"[{type}] {message}\n{stack}");
    }

    private IEnumerator Run()
    {
        GameServices s = bootstrap.Services;
        Note($"Autopilot mulai {DateTime.Now:yyyy-MM-dd HH:mm:ss}, layar {Screen.width}x{Screen.height}");

        yield return WaitFor(() => s.Scenes.Current == SceneFlow.TitleScene && !s.Scenes.IsBusy, 20f, "layar judul");
        if (aborted) { yield return Finish(); yield break; }
        yield return new WaitForSecondsRealtime(1.5f);
        yield return Shot("01_title");

        TitleInstaller title = FindAnyObjectByType<TitleInstaller>();
        title.Menu.Pick(title.Options.ToList().IndexOf("Mulai baru"));
        yield return WaitFor(() => s.Dialogue.IsRunning, 10f, "narasi pembuka");
        if (aborted) { yield return Finish(); yield break; }
        yield return new WaitForSecondsRealtime(2.2f);
        yield return Shot("02_narration");
        yield return AdvanceDialogue(s);
        yield return WaitFor(() => s.Scenes.Current == SceneFlow.WorldScene && !s.Scenes.IsBusy, 10f, "dunia");
        if (aborted) { yield return Finish(); yield break; }
        yield return new WaitForSecondsRealtime(1.5f);
        yield return Shot("03_world_start");

        WorldInstaller world = FindAnyObjectByType<WorldInstaller>();
        PlayerMovement player = world.Player;
        PlayerInteractor interactor = player.GetComponent<PlayerInteractor>();

        // Penjaga menahan pemain sebelum punya partner
        GuardNpc guard = FindAnyObjectByType<GuardNpc>();
        player.Teleport(new Vector2(20f, 10.2f), Vector2.up);
        yield return new WaitForSecondsRealtime(0.4f);
        guard.Interact(interactor);
        yield return WaitFor(() => s.Dialogue.IsRunning, 3f, "dialog Penjaga");
        yield return new WaitForSecondsRealtime(1.8f);
        yield return Shot("04_guard_blocks");
        yield return AdvanceDialogue(s);

        // Tetua: pilih partner -> Yakin?
        StarterGiver elder = FindAnyObjectByType<StarterGiver>();
        player.Teleport(new Vector2(25.5f, 5.9f), Vector2.up);
        yield return new WaitForSecondsRealtime(0.5f);
        elder.Interact(interactor);
        yield return AdvanceUntilChoices(s);
        yield return new WaitForSecondsRealtime(0.6f);
        yield return Shot("05_choose_partner");
        s.Dialogue.Pick(0);                                   // Pyrit (Pyro)
        yield return AdvanceUntilChoices(s);
        yield return new WaitForSecondsRealtime(0.6f);
        yield return Shot("06_yakin");
        s.Dialogue.Pick(0);                                   // Ya
        yield return AdvanceUntilText(s, "mendapat");
        yield return new WaitForSecondsRealtime(0.8f);
        yield return Shot("07_got_partner");
        yield return AdvanceDialogue(s);
        Check(s.Party.Partner != null, "partner didapat");
        Check(s.Flags.Has(StoryFlags.HasStarter), "flag has_starter");

        // Pak Dagang: 3 Ramuan
        MerchantNpc merchant = FindAnyObjectByType<MerchantNpc>();
        player.Teleport(new Vector2(13f, 5.3f), Vector2.down);
        yield return new WaitForSecondsRealtime(0.4f);
        merchant.Interact(interactor);
        yield return AdvanceDialogue(s);
        Check(s.Party.Potions == PlayerParty.StartPotions + 3, "Ramuan bertambah 3");

        // Batu Simpan
        SaveStone stone = FindAnyObjectByType<SaveStone>();
        player.Teleport(new Vector2(22.5f, 6.1f), Vector2.up);
        yield return new WaitForSecondsRealtime(0.4f);
        stone.Interact(interactor);
        yield return new WaitForSecondsRealtime(1.4f);
        yield return Shot("08_save_stone");
        yield return AdvanceDialogue(s);
        Check(s.Save.HasSave(), "save_0.json tersimpan");

        // Lewat celah pagar (Penjaga sudah bergeser)
        player.Teleport(new Vector2(20f, 13f), Vector2.up);
        yield return new WaitForSecondsRealtime(1.2f);
        yield return Shot("09_forest");

        // Menu jeda (Esc) -> Simpan sekarang -> toast Tersimpan
        PauseMenu pause = FindAnyObjectByType<PauseMenu>();
        yield return PressKey(Key.Escape);
        yield return WaitSoft(() => pause.IsOpen, 3f, "menu jeda terbuka dengan Esc");
        if (pause.IsOpen)
        {
            yield return new WaitForSecondsRealtime(0.4f);
            yield return Shot("09b_pause_menu");
            pause.Menu.Pick(1);                               // Simpan sekarang
            yield return new WaitForSecondsRealtime(0.4f);
            Check(!pause.IsOpen && !s.Input.IsLocked, "menu jeda tertutup, input terbuka lagi");
            Check(s.Toast.Current == "Tersimpan", "toast Tersimpan");
        }

        // Battle melawan Sprig di rumput barat
        WildMonster wild = FindObjectsByType<WildMonster>(FindObjectsSortMode.None)
            .FirstOrDefault(w => !w.IsBoss && w.Species.DisplayName == "Sprig");
        if (wild == null) { Fail("Sprig tidak ditemukan"); yield return Finish(); yield break; }
        player.Teleport(wild.Position + new Vector2(0f, -1.3f), Vector2.up);
        yield return new WaitForSecondsRealtime(0.5f);
        wild.StartEncounter();
        yield return WaitFor(() => s.Scenes.InBattle && !s.Scenes.IsBusy, 10f, "battle dimulai");
        if (aborted) { yield return Finish(); yield break; }
        BattleInstaller battle = FindAnyObjectByType<BattleInstaller>();
        yield return WaitFor(() => battle.Battle.AwaitingChoice && battle.View.Menu.IsOpen, 20f, "menu battle");
        yield return new WaitForSecondsRealtime(0.5f);
        yield return Shot("10_battle_menu");
        battle.View.Menu.Pick(1);                             // Jurus
        yield return new WaitForSecondsRealtime(2.3f);
        yield return Shot("11_battle_skill");
        yield return FightUntilEnd(s, () => FindAnyObjectByType<BattleInstaller>(), true);
        yield return WaitFor(() => !s.Scenes.InBattle && !s.Scenes.IsBusy, 20f, "kembali ke dunia");
        if (aborted) { yield return Finish(); yield break; }
        yield return new WaitForSecondsRealtime(1f);
        yield return Shot("12_after_battle");
        Note($"Partner setelah battle: {s.Party.Partner.Data.DisplayName} Lv{s.Party.Partner.Level} EXP {s.Party.Partner.Exp}");

        // Kalah: HP partner dibuat 1, lawan Pyrago (lebih cepat) -> pingsan -> kembali ke depan Batu Simpan
        WildMonster strong = FindObjectsByType<WildMonster>(FindObjectsSortMode.None)
            .FirstOrDefault(w => !w.IsBoss && !w.IsHidden && w.Species.DisplayName == "Pyrago");
        if (strong != null)
        {
            yield return new WaitForSecondsRealtime(2.4f);   // lewati waktu aman
            s.Party.Partner.TakeDamage(s.Party.Partner.CurrentHP - 1);
            player.Teleport(strong.Position + new Vector2(0f, -1.3f), Vector2.up);
            yield return new WaitForSecondsRealtime(0.3f);
            strong.StartEncounter();
            yield return WaitFor(() => s.Scenes.InBattle && !s.Scenes.IsBusy, 10f, "battle untuk uji kalah");
            if (aborted) { yield return Finish(); yield break; }
            yield return FightUntilEnd(s, () => FindAnyObjectByType<BattleInstaller>(), false);
            yield return WaitFor(() => !s.Scenes.InBattle && !s.Scenes.IsBusy, 20f, "kembali ke dunia setelah kalah");
            if (aborted) { yield return Finish(); yield break; }
            yield return new WaitForSecondsRealtime(0.8f);
            yield return Shot("12b_after_lose");
            Check(Vector2.Distance(player.transform.position, new Vector2(21.5f, 6f)) < 0.75f, "kalah -> kembali ke depan Batu Simpan");
            Check(s.Party.Partner.CurrentHP == s.Party.Partner.MaxHP, "HP partner pulih setelah kalah");
        }

        // Gua Petir: gelap + kristal + cahaya pemain
        player.Teleport(new Vector2(30.5f, 26.6f), Vector2.up);
        yield return new WaitForSecondsRealtime(2.2f);
        yield return Shot("13_cave");

        // Boss: partner dibuat kuat supaya uji ending cepat
        s.Party.SetPartner(new MonsterInstance(s.Database.GetByName("Infernox"), 14));
        WildMonster boss = FindObjectsByType<WildMonster>(FindObjectsSortMode.None).FirstOrDefault(w => w.IsBoss);
        if (boss == null) { Fail("Raijugar tidak ditemukan"); yield return Finish(); yield break; }
        player.Teleport(boss.Position + new Vector2(0f, -1.7f), Vector2.up);
        yield return new WaitForSecondsRealtime(2.6f);        // lewati waktu aman 2,2 s
        boss.StartEncounter();
        yield return WaitFor(() => s.Dialogue.IsRunning, 5f, "auman Raijugar");
        yield return new WaitForSecondsRealtime(1.0f);
        yield return Shot("14_boss_roar");
        yield return AdvanceDialogue(s);
        yield return WaitFor(() => s.Scenes.InBattle && !s.Scenes.IsBusy, 10f, "battle boss");
        if (aborted) { yield return Finish(); yield break; }
        battle = FindAnyObjectByType<BattleInstaller>();
        yield return WaitFor(() => battle.Battle.AwaitingChoice && battle.View.Menu.IsOpen, 20f, "menu battle boss");
        yield return new WaitForSecondsRealtime(0.4f);
        yield return Shot("15_boss_battle");
        yield return FightUntilEnd(s, () => FindAnyObjectByType<BattleInstaller>(), false);

        yield return WaitFor(() => s.Dialogue.IsRunning, 25f, "narasi ending");
        yield return new WaitForSecondsRealtime(2f);
        yield return Shot("16_ending_narration");
        yield return AdvanceDialogue(s);
        yield return WaitFor(() => s.Scenes.Current == SceneFlow.EndScene && !s.Scenes.IsBusy, 15f, "layar DEMO SELESAI");
        if (aborted) { yield return Finish(); yield break; }
        yield return new WaitForSecondsRealtime(1.5f);
        yield return Shot("17_demo_selesai");
        Check(s.Flags.Has(StoryFlags.BossDefeated), "flag boss_defeated");

        FindAnyObjectByType<EndInstaller>().Leave();
        yield return WaitFor(() => s.Scenes.Current == SceneFlow.TitleScene && !s.Scenes.IsBusy, 15f, "kembali ke judul");
        yield return new WaitForSecondsRealtime(1.2f);
        yield return Shot("18_title_continue");
        Check(FindAnyObjectByType<TitleInstaller>().Options.Contains("Lanjutkan"), "menu Lanjutkan muncul setelah ending");

        // Lanjutkan: muat save_0.json -> dunia di posisi tersimpan, flag & partner kembali
        TitleInstaller title2 = FindAnyObjectByType<TitleInstaller>();
        title2.Menu.Pick(title2.Options.ToList().IndexOf("Lanjutkan"));
        yield return WaitFor(() => s.Scenes.Current == SceneFlow.WorldScene && !s.Scenes.IsBusy, 15f, "Lanjutkan ke dunia");
        if (aborted) { yield return Finish(); yield break; }
        yield return new WaitForSecondsRealtime(1.2f);
        yield return Shot("19_continue");
        Check(s.Flags.Has(StoryFlags.BossDefeated) && s.Flags.Has(StoryFlags.HasStarter), "flag termuat dari save");
        Check(s.Party.Partner != null && s.Party.Partner.Data.DisplayName == "Infernox", "partner termuat dari save");
        Check(s.Toast.Current == "Perjalanan dilanjutkan", "toast Perjalanan dilanjutkan");

        yield return Finish();
    }

    private IEnumerator PressKey(Key key)
    {
        Keyboard keyboard = Keyboard.current;
        if (keyboard == null) keyboard = InputSystem.AddDevice<Keyboard>();
        InputSystem.QueueStateEvent(keyboard, new KeyboardState(key));
        yield return null;
        yield return null;
        InputSystem.QueueStateEvent(keyboard, new KeyboardState());
        yield return null;
    }

    /// <summary>Seperti WaitFor, tapi tidak menghentikan autopilot bila gagal.</summary>
    private IEnumerator WaitSoft(Func<bool> condition, float timeout, string what)
    {
        float end = Time.realtimeSinceStartup + timeout;
        while (!condition() && Time.realtimeSinceStartup < end) yield return null;
        Check(condition(), what);
    }

    private IEnumerator FightUntilEnd(GameServices s, Func<BattleInstaller> find, bool preferSkill)
    {
        float end = Time.realtimeSinceStartup + 150f;
        while (s.Scenes.InBattle && Time.realtimeSinceStartup < end)
        {
            BattleInstaller b = find();
            if (b != null && b.Battle.AwaitingChoice && b.View.Menu.IsOpen)
            {
                yield return new WaitForSecondsRealtime(0.3f);
                b = find();
                if (b != null && b.Battle.AwaitingChoice && b.View.Menu.IsOpen)
                    b.View.Menu.Pick(preferSkill && b.Battle.SkillsLeft > 0 ? 1 : 0);
            }
            yield return null;
        }
        if (s.Scenes.InBattle) Fail("battle tidak selesai dalam 150 detik");
    }

    private IEnumerator AdvanceDialogue(GameServices s)
    {
        float end = Time.realtimeSinceStartup + 40f;
        while (s.Dialogue.IsRunning && Time.realtimeSinceStartup < end)
        {
            if (s.Dialogue.ChoicesOpen) s.Dialogue.Pick(0);
            else s.Dialogue.Advance();
            yield return new WaitForSecondsRealtime(0.25f);
        }
        yield return null;
    }

    private IEnumerator AdvanceUntilChoices(GameServices s)
    {
        float end = Time.realtimeSinceStartup + 20f;
        while (!s.Dialogue.ChoicesOpen && Time.realtimeSinceStartup < end)
        {
            if (s.Dialogue.IsRunning) s.Dialogue.Advance();
            yield return new WaitForSecondsRealtime(0.25f);
        }
        if (!s.Dialogue.ChoicesOpen) Fail("pilihan dialog tidak muncul");
    }

    private IEnumerator AdvanceUntilText(GameServices s, string fragment)
    {
        float end = Time.realtimeSinceStartup + 20f;
        while (s.Dialogue.IsRunning && !s.Dialogue.CurrentText.Contains(fragment) && Time.realtimeSinceStartup < end)
        {
            s.Dialogue.Advance();
            yield return new WaitForSecondsRealtime(0.25f);
        }
    }

    private IEnumerator WaitFor(Func<bool> condition, float timeout, string what)
    {
        float end = Time.realtimeSinceStartup + timeout;
        while (!condition())
        {
            if (Time.realtimeSinceStartup > end)
            {
                Fail($"timeout menunggu {what}");
                aborted = true;
                yield break;
            }
            yield return null;
        }
        Note($"OK  {what}");
    }

    private IEnumerator Shot(string name)
    {
        yield return new WaitForEndOfFrame();
        ScreenCapture.CaptureScreenshot(Path.Combine(outDir, name + ".png"));
        Note($"Screenshot {name}");
    }

    private void Check(bool ok, string what)
    {
        if (ok) Note($"OK  {what}");
        else Fail(what);
    }

    private void Fail(string what)
    {
        failures++;
        Note($"GAGAL  {what}");
    }

    private void Note(string line)
    {
        report.Add(line);
        Debug.Log($"[AutoPilot] {line}");
    }

    private IEnumerator Finish()
    {
        yield return new WaitForSecondsRealtime(1.5f);
        report.Insert(0, $"RINGKASAN: gagal cek = {failures}, error/exception di log = {errors}");
        File.WriteAllLines(Path.Combine(outDir, "report.txt"), report);
        Application.Quit();
    }
}
