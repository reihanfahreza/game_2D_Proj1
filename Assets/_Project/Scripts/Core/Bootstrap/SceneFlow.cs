using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>
/// Service perpindahan scene (P4): fade, muat additive di atas Scn_Bootstrap, lalu panggil Install().
/// Battle dimuat di atas dunia; selama battle isi Scn_World disembunyikan agar kamera, cahaya,
/// dan collider dunia tidak ikut aktif, lalu dimunculkan lagi di posisi yang sama.
/// </summary>
public class SceneFlow
{
    public const string TitleScene = "Scn_Title";
    public const string WorldScene = "Scn_World";
    public const string BattleScene = "Scn_Battle";
    public const string EndScene = "Scn_End";

    private readonly MonoBehaviour host;
    private readonly ScreenFader fader;
    private readonly GameServices services;
    private readonly List<GameObject> hiddenRoots = new List<GameObject>();

    public SceneFlow(MonoBehaviour host, ScreenFader fader, GameServices services)
    {
        this.host = host;
        this.fader = fader;
        this.services = services;
    }

    public string Current { get; private set; }
    public bool IsBusy { get; private set; }
    public bool InBattle { get; private set; }

    /// <summary>P6: WorldInstaller menahan layar tetap hitam selama narasi pembuka.</summary>
    public bool HoldFade { get; set; }

    /// <summary>P7: waktu bermain dihitung saat di dunia (termasuk battle).</summary>
    public bool InGame => Current == WorldScene;

    public void GoTo(string scene)
    {
        if (IsBusy)
        {
            Debug.LogWarning($"SceneFlow sedang sibuk, GoTo({scene}) diabaikan.");
            return;
        }
        host.StartCoroutine(GoToRoutine(scene));
    }

    private IEnumerator GoToRoutine(string scene)
    {
        IsBusy = true;
        services.Input.Lock();
        yield return fader.FadeOut(0.35f);
        Time.timeScale = 1f;
        services.Dialogue?.Cancel();

        if (InBattle)
        {
            yield return SceneManager.UnloadSceneAsync(BattleScene);
            InBattle = false;
            hiddenRoots.Clear();
        }
        if (!string.IsNullOrEmpty(Current) && SceneManager.GetSceneByName(Current).isLoaded)
            yield return SceneManager.UnloadSceneAsync(Current);

        services.Input.ResetLocks();
        services.Input.Lock();
        HoldFade = false;

        yield return SceneManager.LoadSceneAsync(scene, LoadSceneMode.Additive);
        Scene loaded = SceneManager.GetSceneByName(scene);
        SceneManager.SetActiveScene(loaded);
        Current = scene;
        Install(loaded);

        if (!HoldFade) yield return fader.FadeIn(0.35f);
        services.Input.Unlock();
        IsBusy = false;
    }

    /// <summary>Dipanggil WildMonster setelah BattleSession.Begin().</summary>
    public void StartBattle()
    {
        if (IsBusy || InBattle || Current != WorldScene) return;
        host.StartCoroutine(StartBattleRoutine());
    }

    private IEnumerator StartBattleRoutine()
    {
        IsBusy = true;
        services.Input.Lock();
        services.Audio?.PlayEncounter();
        yield return fader.FadeOut(0.45f);

        hiddenRoots.Clear();
        foreach (GameObject root in SceneManager.GetSceneByName(Current).GetRootGameObjects())
        {
            if (!root.activeSelf) continue;
            root.SetActive(false);
            hiddenRoots.Add(root);
        }

        yield return SceneManager.LoadSceneAsync(BattleScene, LoadSceneMode.Additive);
        Scene battle = SceneManager.GetSceneByName(BattleScene);
        SceneManager.SetActiveScene(battle);
        InBattle = true;
        Install(battle);
        yield return fader.FadeIn(0.3f);
        IsBusy = false;
    }

    /// <summary>Dipanggil BattleSystem saat battle selesai.</summary>
    public void EndBattle(BattleResult result)
    {
        if (!InBattle) return;
        host.StartCoroutine(EndBattleRoutine(result));
    }

    private IEnumerator EndBattleRoutine(BattleResult result)
    {
        IsBusy = true;
        yield return fader.FadeOut(0.45f);
        Time.timeScale = 1f;
        yield return SceneManager.UnloadSceneAsync(BattleScene);
        InBattle = false;

        foreach (GameObject root in hiddenRoots)
            if (root != null) root.SetActive(true);
        hiddenRoots.Clear();
        SceneManager.SetActiveScene(SceneManager.GetSceneByName(Current));

        services.Audio?.ResumeWorldMusic();
        services.Battle.Finish(result);   // callback monster + event Ended (kalah -> Batu Simpan, boss -> ending)
        yield return null;
        yield return fader.FadeIn(0.35f);
        services.Input.Unlock();
        IsBusy = false;
    }

    /// <summary>P6: lepas layar hitam setelah narasi pembuka.</summary>
    public IEnumerator ReleaseFade()
    {
        HoldFade = false;
        yield return fader.FadeIn(0.6f);
    }

    private void Install(Scene scene)
    {
        foreach (GameObject root in scene.GetRootGameObjects())
        {
            if (!root.TryGetComponent(out SceneInstaller installer)) continue;
            try { installer.Install(services); }
            catch (System.Exception e) { Debug.LogException(e); }
            return;
        }
        Debug.LogWarning($"Scene {scene.name} tidak punya SceneInstaller di root.");
    }
}
