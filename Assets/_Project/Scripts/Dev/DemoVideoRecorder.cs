using System;
using System.Collections;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading;
using UnityEngine;
using UnityEngine.Experimental.Rendering;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.LowLevel;
using UnityEngine.UI;

/// <summary>
/// Perekam video demo (alat bantu). Hanya aktif bila game dijalankan dengan argumen -recordvideo:
///   PulauMonster.exe -recordvideo -screen-width 1280 -screen-height 720 -screen-fullscreen 0
/// Memainkan alur demo dengan input keyboard tiruan dan merekam potongan-potongan pentingnya dengan
/// tempo santai (setiap pesan ditunggu sampai selesai dibaca): frame JPG + audio WAV + daftar durasi
/// (frames.txt) di persistentDataPath/video, untuk dirangkai ffmpeg. Label nama &amp; NIM tampil di atas layar.
/// </summary>
public class DemoVideoRecorder : MonoBehaviour
{
    private const string Watermark = "M Reihan Fahreza\nNIM 4352511014";
    private const float CharsPerSecond = 55f;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    private static void Boot()
    {
        if (!Environment.GetCommandLineArgs().Contains("-recordvideo")) return;
        string save = Path.Combine(Application.persistentDataPath, "save_0.json");
        if (File.Exists(save)) File.Delete(save);           // mulai dari keadaan bersih
        var go = new GameObject("DemoVideoRecorder");
        DontDestroyOnLoad(go);
        go.AddComponent<DemoVideoRecorder>();
    }

    private static readonly FieldInfo CooldownField = typeof(WildMonster).GetField("cooldown", BindingFlags.NonPublic | BindingFlags.Instance);
    private static readonly FieldInfo LevelField = typeof(WildMonster).GetField("level", BindingFlags.NonPublic | BindingFlags.Instance);
    private static readonly FieldInfo MessageField = typeof(BattleView).GetField("messageText", BindingFlags.NonPublic | BindingFlags.Instance);

    private string outDir;
    private readonly StringBuilder frameList = new StringBuilder();
    private readonly List<string> log = new List<string>();
    private BlockingCollection<(byte[] data, GraphicsFormat format, int width, int height, string path)> queue;
    private Thread encoder;
    private DemoAudioTap tap;
    private int sampleRate;
    private bool recording;
    private bool finishing;
    private string segmentName;
    private double segmentVideoStart;
    private double videoTime;
    private int frameIndex;
    private string pendingFrame;
    private double pendingFrameTime;
    private string lastFrame;
    private float startTime;
    private string battleMessage;
    private float battleMessageAt;

    void Start()
    {
        QualitySettings.vSyncCount = 0;
        Application.targetFrameRate = 30;
        Application.runInBackground = true;
        InputSystem.settings.backgroundBehavior = InputSettings.BackgroundBehavior.IgnoreFocus;

        outDir = Path.Combine(Application.persistentDataPath, "video");
        if (Directory.Exists(outDir)) Directory.Delete(outDir, true);
        Directory.CreateDirectory(outDir);

        queue = new BlockingCollection<(byte[], GraphicsFormat, int, int, string)>(90);
        encoder = new Thread(EncodeLoop) { IsBackground = true };
        encoder.Start();

        sampleRate = AudioSettings.outputSampleRate;
        AudioListener listener = FindAnyObjectByType<AudioListener>();
        tap = listener.gameObject.AddComponent<DemoAudioTap>();

        CreateWatermark();
        startTime = Time.realtimeSinceStartup;
        StartCoroutine(CaptureLoop());
        StartCoroutine(Run());
    }

    void Update()
    {
        // pengaman: jangan pernah berjalan lebih dari 6 menit
        if (!finishing && Time.realtimeSinceStartup - startTime > 360f)
        {
            Note("GAGAL  batas waktu 6 menit");
            StartCoroutine(Finish());
        }
    }

    // ---------------------------------------------------------------- alur demo

    private IEnumerator Run()
    {
        GameServices s = FindAnyObjectByType<GameBootstrap>().Services;

        yield return WaitFor(() => s.Scenes.Current == SceneFlow.TitleScene && !s.Scenes.IsBusy, 20f, "layar judul");
        yield return new WaitForSecondsRealtime(0.6f);

        // 1. Layar judul -> Mulai baru
        BeginSegment("judul");
        yield return new WaitForSecondsRealtime(2.6f);
        yield return Tap(Key.Enter);
        yield return new WaitForSecondsRealtime(0.7f);
        EndSegment();

        // narasi pembuka (tidak direkam)
        yield return WaitFor(() => s.Dialogue.IsRunning, 10f, "narasi pembuka");
        while (s.Dialogue.IsRunning) { yield return Tap(Key.Enter); yield return new WaitForSecondsRealtime(0.25f); }
        yield return WaitFor(() => s.Scenes.Current == SceneFlow.WorldScene && !s.Scenes.IsBusy, 10f, "dunia");
        yield return new WaitForSecondsRealtime(1.1f);

        WorldInstaller world = FindAnyObjectByType<WorldInstaller>();
        PlayerMovement player = world.Player;
        PlayerInteractor interactor = player.GetComponent<PlayerInteractor>();

        // 2a. Lari ke Tetua Rahmat lalu bicara (baris pertama dibaca sampai selesai)
        BeginSegment("ke-tetua");
        yield return new WaitForSecondsRealtime(0.4f);
        yield return HoldUntil(() => player.transform.position.x >= 25.3f, 3f, Key.LeftShift, Key.D);
        yield return Hold(0.25f, Key.W);
        yield return Tap(Key.E);
        yield return new WaitForSecondsRealtime(0.2f);
        if (!s.Dialogue.IsRunning) FindAnyObjectByType<StarterGiver>().Interact(interactor);
        yield return WaitDialogueTyped(s);
        yield return new WaitForSecondsRealtime(0.9f);
        EndSegment();

        // baris kedua Tetua (tidak direkam)
        yield return AdvanceUntil(() => s.Dialogue.ChoicesOpen, 15f);

        // 2b. Pilih partner -> Yakin? -> dapat Pyrit
        BeginSegment("pilih-partner");
        yield return new WaitForSecondsRealtime(1.4f);
        yield return Tap(Key.Enter);                                   // Pyrit (Pyro)
        yield return WaitFor(() => !s.Dialogue.ChoicesOpen, 2f, "pilihan ditutup");
        yield return WaitFor(() => s.Dialogue.ChoicesOpen, 5f, "pertanyaan Yakin?");
        yield return new WaitForSecondsRealtime(1.1f);
        yield return Tap(Key.Enter);                                   // Ya
        yield return WaitFor(() => s.Dialogue.CurrentText.Contains("mendapat"), 5f, "Kamu mendapat Pyrit");
        yield return WaitDialogueTyped(s);
        yield return new WaitForSecondsRealtime(1.5f);
        EndSegment();

        // sisa dialog Tetua (tidak direkam)
        while (s.Dialogue.IsRunning) { yield return Tap(Key.Enter); yield return new WaitForSecondsRealtime(0.25f); }
        s.Party.Partner.GainExp(20);                                   // supaya menang pertama langsung naik level
        yield return new WaitForSecondsRealtime(0.3f);
        CalmAll(40f);                                                  // monster tidak mengejar saat lewat

        // 3. Lewat celah pagar ke Hutan Rimbun
        BeginSegment("hutan");
        yield return new WaitForSecondsRealtime(0.3f);
        yield return HoldUntil(() => player.transform.position.x <= 20.15f, 3f, Key.LeftShift, Key.A);
        yield return HoldUntil(() => player.transform.position.y >= 14.5f, 3f, Key.LeftShift, Key.W);
        yield return Hold(0.7f, Key.LeftShift, Key.A);
        yield return new WaitForSecondsRealtime(0.3f);
        EndSegment();

        // 4. Battle melawan Sprig Lv 2 (dua Jurus cukup), tempo santai
        WildMonster sprig = FindObjectsByType<WildMonster>(FindObjectsSortMode.None)
            .FirstOrDefault(w => !w.IsBoss && w.Species.DisplayName == "Sprig");
        if (sprig == null) { Note("GAGAL  Sprig tidak ada"); yield return Finish(); yield break; }
        LevelField.SetValue(sprig, 2);
        CooldownField.SetValue(sprig, 0f);
        player.Teleport(sprig.Position + new Vector2(1.3f, 0f), Vector2.left);
        yield return new WaitForSecondsRealtime(0.15f);
        sprig.StartEncounter();
        yield return WaitFor(() => s.Scenes.InBattle, 6f, "battle dimulai");
        BattleInstaller battle = FindAnyObjectByType<BattleInstaller>();
        WatchBattle(battle);
        BeginSegment("battle");
        yield return ReadBattleUntil(battle, () => MenuOpen(battle), 15f, false);
        yield return new WaitForSecondsRealtime(1.1f);                 // menu terlihat dulu
        yield return Tap(Key.RightArrow);                              // Jurus
        yield return new WaitForSecondsRealtime(0.55f);
        yield return Tap(Key.Enter);
        yield return ReadBattleUntil(battle, () => MenuOpen(battle) || battle.Battle.Result != BattleResult.None, 20f, false);
        if (battle.Battle.Result == BattleResult.None)
        {
            yield return new WaitForSecondsRealtime(0.9f);
            yield return Tap(Key.Enter);                               // Jurus lagi (pilihan terakhir diingat)
        }
        yield return ReadBattleUntil(battle, () => MessageContains(battle, "naik ke Lv"), 25f, true);
        yield return WaitBattleTyped();
        yield return new WaitForSecondsRealtime(1.6f);
        EndSegment();

        // sisa pesan battle (tidak direkam)
        yield return SkipMessagesUntil(battle, () => !s.Scenes.InBattle, 20f, 0.35f);
        yield return WaitFor(() => !s.Scenes.InBattle && !s.Scenes.IsBusy, 15f, "kembali ke dunia");

        // 5. Gua Petir yang gelap -> Raijugar mengaum (dua baris)
        CalmAll(40f);
        WildMonster boss = FindObjectsByType<WildMonster>(FindObjectsSortMode.None).FirstOrDefault(w => w.IsBoss);
        if (boss == null) { Note("GAGAL  Raijugar tidak ada"); yield return Finish(); yield break; }
        CooldownField.SetValue(boss, 0f);
        player.Teleport(new Vector2(30.5f, 26.3f), Vector2.up);
        yield return new WaitForSecondsRealtime(1.6f);
        yield return WaitFor(() => !s.Battle.IsSafe, 5f, "waktu aman habis");
        BeginSegment("gua");
        yield return new WaitForSecondsRealtime(0.5f);
        yield return HoldUntil(() => s.Dialogue.IsRunning, 5f, Key.W);
        yield return WaitDialogueTyped(s);
        yield return new WaitForSecondsRealtime(1.0f);
        yield return Tap(Key.Enter);
        yield return new WaitForSecondsRealtime(0.3f);
        yield return WaitDialogueTyped(s);
        yield return new WaitForSecondsRealtime(1.0f);
        EndSegment();

        // battle boss (tidak direkam): HP boss dibuat 0 agar demo singkat
        while (s.Dialogue.IsRunning) { yield return Tap(Key.Enter); yield return new WaitForSecondsRealtime(0.25f); }
        yield return WaitFor(() => s.Scenes.InBattle && FindAnyObjectByType<BattleInstaller>() != null, 8f, "battle boss");
        battle = FindAnyObjectByType<BattleInstaller>();
        WatchBattle(battle);
        yield return SkipMessagesUntil(battle, () => MenuOpen(battle), 12f, 0.3f, false);
        battle.Battle.Enemy.TakeDamage(battle.Battle.Enemy.CurrentHP);
        yield return new WaitForSecondsRealtime(0.2f);
        yield return Tap(Key.Enter);                                   // Serang
        yield return SkipMessagesUntil(battle, () => MessageContains(battle, "mulai bersinar"), 25f, 0.3f);

        // 6. Evolusi Pyrit -> Pyrago
        BeginSegment("evolusi");
        yield return WaitBattleTyped();
        yield return new WaitForSecondsRealtime(0.6f);
        yield return Tap(Key.Enter);
        yield return WaitFor(() => MessageContains(battle, "berevolusi menjadi"), 6f, "evolusi selesai");
        yield return WaitBattleTyped();
        yield return new WaitForSecondsRealtime(1.5f);
        EndSegment();

        // 7. Narasi ending
        yield return SkipMessagesUntil(battle, () => !s.Scenes.InBattle, 25f, 0.3f);
        yield return WaitFor(() => s.Dialogue.IsRunning, 15f, "narasi ending");
        BeginSegment("ending");
        yield return WaitDialogueTyped(s);
        yield return new WaitForSecondsRealtime(1.2f);
        EndSegment();
        while (s.Dialogue.IsRunning) { yield return Tap(Key.Enter); yield return new WaitForSecondsRealtime(0.3f); }

        // 8. DEMO SELESAI (ikut merekam fade-in)
        yield return WaitFor(() => s.Scenes.Current == SceneFlow.EndScene, 15f, "layar DEMO SELESAI");
        BeginSegment("demo-selesai");
        yield return new WaitForSecondsRealtime(3.6f);
        EndSegment();

        yield return Finish();
    }

    // ---------------------------------------------------------------- input tiruan

    private static void SetKeys(params Key[] keys)
    {
        Keyboard keyboard = Keyboard.current;
        if (keyboard == null) keyboard = InputSystem.AddDevice<Keyboard>();
        InputSystem.QueueStateEvent(keyboard, new KeyboardState(keys));
    }

    private IEnumerator Tap(Key key)
    {
        SetKeys(key);
        yield return null;
        yield return null;
        SetKeys();
        yield return null;
    }

    private IEnumerator Hold(float seconds, params Key[] keys)
    {
        SetKeys(keys);
        yield return new WaitForSecondsRealtime(seconds);
        SetKeys();
        yield return null;
    }

    private IEnumerator HoldUntil(Func<bool> done, float timeout, params Key[] keys)
    {
        SetKeys(keys);
        float end = Time.realtimeSinceStartup + timeout;
        while (!Safe(done) && Time.realtimeSinceStartup < end) yield return null;
        SetKeys();
        yield return null;
    }

    private IEnumerator AdvanceUntil(Func<bool> done, float timeout)
    {
        float end = Time.realtimeSinceStartup + timeout;
        while (!Safe(done) && Time.realtimeSinceStartup < end)
        {
            yield return Tap(Key.Enter);
            yield return new WaitForSecondsRealtime(0.3f);
        }
    }

    /// <summary>Tunggu baris dialog selesai diketik (teks berhenti bertambah).</summary>
    private IEnumerator WaitDialogueTyped(GameServices s)
    {
        string last = null;
        float stableSince = Time.realtimeSinceStartup;
        float end = Time.realtimeSinceStartup + 6f;
        while (Time.realtimeSinceStartup < end)
        {
            string now = s.Dialogue.CurrentText;
            if (now != last) { last = now; stableSince = Time.realtimeSinceStartup; }
            else if (!string.IsNullOrEmpty(now) && Time.realtimeSinceStartup - stableSince > 0.15f) yield break;
            yield return null;
        }
    }

    private void WatchBattle(BattleInstaller battle)
    {
        battleMessage = null;
        battle.Battle.MessageShown += message =>
        {
            battleMessage = message;
            battleMessageAt = Time.realtimeSinceStartup;
        };
    }

    private IEnumerator WaitBattleTyped()
    {
        if (battleMessage == null) yield break;
        float typed = battleMessageAt + battleMessage.Length / CharsPerSecond + 0.05f;
        while (Time.realtimeSinceStartup < typed) yield return null;
    }

    private static bool MenuOpen(BattleInstaller battle) => battle.Battle.AwaitingChoice && battle.View.Menu.IsOpen;

    /// <summary>
    /// Tempo santai: Enter ditekan hanya setelah pesan battle selesai diketik + jeda baca.
    /// fight = true: bila menu aksi terbuka lebih dari 1 detik, pilih aksi terakhir (mis. serangan meleset).
    /// </summary>
    private IEnumerator ReadBattleUntil(BattleInstaller battle, Func<bool> done, float timeout, bool fight, float read = 0.65f)
    {
        float end = Time.realtimeSinceStartup + timeout;
        float menuSince = -1f;
        while (Time.realtimeSinceStartup < end)
        {
            if (Safe(done)) yield break;
            float now = Time.realtimeSinceStartup;
            if (battle.Battle.AwaitingChoice)
            {
                if (menuSince < 0f) menuSince = now;
                if (fight && battle.View.Menu.IsOpen && now - menuSince > 1.0f)
                {
                    menuSince = -1f;
                    yield return Tap(Key.Enter);
                    continue;
                }
            }
            else
            {
                menuSince = -1f;
                if (battleMessage != null && now >= battleMessageAt + battleMessage.Length / CharsPerSecond + read)
                {
                    battleMessage = null;
                    yield return Tap(Key.Enter);
                    continue;
                }
            }
            yield return null;
        }
        Note("timeout menunggu pesan battle (santai)");
    }

    /// <summary>Versi cepat untuk bagian yang tidak direkam.</summary>
    private IEnumerator SkipMessagesUntil(BattleInstaller battle, Func<bool> done, float timeout, float interval, bool fight = true)
    {
        float end = Time.realtimeSinceStartup + timeout;
        float next = Time.realtimeSinceStartup + interval;
        while (Time.realtimeSinceStartup < end)
        {
            if (Safe(done)) yield break;
            bool menuOpen = battle != null && battle.Battle.AwaitingChoice;
            if (Time.realtimeSinceStartup >= next && (!menuOpen || (fight && battle.View.Menu.IsOpen)))
            {
                next = Time.realtimeSinceStartup + (menuOpen ? 0.5f : interval);
                yield return Tap(Key.Enter);
                continue;
            }
            yield return null;
        }
        Note("timeout menunggu pesan battle");
    }

    private static bool Safe(Func<bool> condition)
    {
        try { return condition(); }
        catch { return false; }
    }

    private static bool MessageContains(BattleInstaller battle, string fragment)
    {
        if (battle == null) return false;
        var text = MessageField.GetValue(battle.View) as Text;
        return text != null && text.text.Contains(fragment);
    }

    private static void CalmAll(float seconds)
    {
        foreach (WildMonster wild in FindObjectsByType<WildMonster>(FindObjectsSortMode.None))
            CooldownField.SetValue(wild, seconds);
    }

    private IEnumerator WaitFor(Func<bool> condition, float timeout, string what)
    {
        float end = Time.realtimeSinceStartup + timeout;
        while (!Safe(condition))
        {
            if (Time.realtimeSinceStartup > end)
            {
                Note($"timeout menunggu {what}");
                yield break;
            }
            yield return null;
        }
    }

    // ---------------------------------------------------------------- perekaman

    private void BeginSegment(string name)
    {
        if (finishing) return;
        segmentName = name;
        segmentVideoStart = videoTime;
        pendingFrame = null;
        recording = true;
    }

    private void EndSegment()
    {
        double now = Time.realtimeSinceStartupAsDouble;
        recording = false;
        tap.active = false;
        if (pendingFrame != null) AddFrame(pendingFrame, now - pendingFrameTime);
        pendingFrame = null;

        // samakan panjang audio dengan panjang video
        int target = (int)Math.Round(videoTime * sampleRate) * tap.channels;
        lock (tap.samples)
        {
            if (tap.samples.Count > target) tap.samples.RemoveRange(target, tap.samples.Count - target);
            while (tap.samples.Count < target) tap.samples.Add(0f);
        }
        Note($"segmen {segmentName}: {segmentVideoStart:F2} -> {videoTime:F2} ({videoTime - segmentVideoStart:F2} s)");
    }

    private IEnumerator CaptureLoop()
    {
        var endOfFrame = new WaitForEndOfFrame();
        while (true)
        {
            yield return endOfFrame;
            if (!recording) continue;
            double now = Time.realtimeSinceStartupAsDouble;
            if (pendingFrame == null) tap.active = true;               // audio mulai bersama frame pertama segmen
            else AddFrame(pendingFrame, now - pendingFrameTime);

            Texture2D shot = ScreenCapture.CaptureScreenshotAsTexture();
            string file = $"f{frameIndex:D5}.jpg";
            frameIndex++;
            queue.Add((shot.GetRawTextureData(), shot.graphicsFormat, shot.width, shot.height, Path.Combine(outDir, file)));
            Destroy(shot);
            pendingFrame = file;
            pendingFrameTime = now;
        }
    }

    private void AddFrame(string file, double duration)
    {
        frameList.Append("file '").Append(file).Append("'\nduration ")
                 .Append(duration.ToString("0.000000", CultureInfo.InvariantCulture)).Append('\n');
        videoTime += duration;
        lastFrame = file;
    }

    private void EncodeLoop()
    {
        foreach (var item in queue.GetConsumingEnumerable())
        {
            byte[] jpg = ImageConversion.EncodeArrayToJPG(item.data, item.format, (uint)item.width, (uint)item.height, 0, 92);
            File.WriteAllBytes(item.path, jpg);
        }
    }

    private IEnumerator Finish()
    {
        if (finishing) yield break;
        finishing = true;
        if (recording) EndSegment();
        yield return null;
        queue.CompleteAdding();
        while (encoder.IsAlive) yield return null;

        if (lastFrame != null) frameList.Append("file '").Append(lastFrame).Append("'\n");   // syarat concat demuxer ffmpeg
        File.WriteAllText(Path.Combine(outDir, "frames.txt"), frameList.ToString());
        WriteWav(Path.Combine(outDir, "audio.wav"));
        Note($"selesai: {frameIndex} frame, {videoTime:F2} s video, audio {sampleRate} Hz x {tap.channels}");
        File.WriteAllLines(Path.Combine(outDir, "log.txt"), log);
        yield return new WaitForSecondsRealtime(0.5f);
        Application.Quit();
    }

    private void WriteWav(string path)
    {
        float[] data;
        lock (tap.samples) data = tap.samples.ToArray();
        int channels = tap.channels;
        using (var bw = new BinaryWriter(File.Create(path)))
        {
            int bytes = data.Length * 2;
            bw.Write(Encoding.ASCII.GetBytes("RIFF"));
            bw.Write(36 + bytes);
            bw.Write(Encoding.ASCII.GetBytes("WAVEfmt "));
            bw.Write(16);
            bw.Write((short)1);
            bw.Write((short)channels);
            bw.Write(sampleRate);
            bw.Write(sampleRate * channels * 2);
            bw.Write((short)(channels * 2));
            bw.Write((short)16);
            bw.Write(Encoding.ASCII.GetBytes("data"));
            bw.Write(bytes);
            foreach (float f in data) bw.Write((short)Mathf.Clamp(Mathf.RoundToInt(f * 32767f), -32768, 32767));
        }
    }

    private void Note(string line)
    {
        log.Add($"{Time.realtimeSinceStartup - startTime:F1}s  {line}");
        Debug.Log($"[DemoVideo] {line}");
    }

    // ---------------------------------------------------------------- label nama & NIM

    private void CreateWatermark()
    {
        Text sample = FindAnyObjectByType<Text>(FindObjectsInactive.Include);
        var root = new GameObject("DemoWatermark", typeof(RectTransform));
        DontDestroyOnLoad(root);
        var canvas = root.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 200;
        var scaler = root.AddComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1280f, 720f);
        scaler.matchWidthOrHeight = 0.5f;

        var panel = new GameObject("Panel", typeof(RectTransform), typeof(Image));
        panel.transform.SetParent(root.transform, false);
        var image = panel.GetComponent<Image>();
        image.color = new Color(0.07f, 0.06f, 0.15f, 0.62f);
        image.raycastTarget = false;
        UiTheme.Place((RectTransform)panel.transform, 140f, 2f, 80f, 18f);

        var label = new GameObject("Label", typeof(RectTransform), typeof(Text));
        label.transform.SetParent(panel.transform, false);
        var rt = (RectTransform)label.transform;
        rt.anchorMin = Vector2.zero;
        rt.anchorMax = Vector2.one;
        rt.offsetMin = rt.offsetMax = Vector2.zero;
        var text = label.GetComponent<Text>();
        if (sample != null) text.font = sample.font;
        text.fontSize = 24;
        text.lineSpacing = 0.9f;
        text.alignment = TextAnchor.MiddleCenter;
        text.color = UiTheme.Cream;
        text.text = Watermark;
        text.raycastTarget = false;
    }
}
