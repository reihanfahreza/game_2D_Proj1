using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Layar judul (P6): latar BG_Title_World bergeser sin(t / 6) x 240, 4 starter beranimasi 0,35 s,
/// menu teks Lanjutkan (jika ada save) / Mulai baru / Keluar.
/// </summary>
public class TitleInstaller : SceneInstaller
{
    [SerializeField] private TextMenu menu;
    [SerializeField] private AudioClip titleMusic;
    [SerializeField] private RectTransform background;
    [SerializeField] private Image[] starterImages;
    [SerializeField] private MonsterData[] starters;

    private readonly List<string> options = new List<string>();
    private GameServices services;
    private bool leaving;

    public TextMenu Menu => menu;
    public IReadOnlyList<string> Options => options;

    public override void Install(GameServices s)
    {
        services = s;
        options.Clear();
        if (s.Save.HasSave()) options.Add("Lanjutkan");
        options.Add("Mulai baru");
        options.Add("Keluar");
        menu.Picked += OnPicked;
        menu.Open(options.ToArray());
        s.Audio.PlayWorldMusic(titleMusic);
    }

    void Update()
    {
        if (background != null)
            background.anchoredPosition = new Vector2(Mathf.Sin(Time.unscaledTime / 6f) * 240f, 0f);

        int frame = Mathf.FloorToInt(Time.unscaledTime / 0.35f);
        for (int i = 0; i < starterImages.Length && i < starters.Length; i++)
            starterImages[i].sprite = starters[i].Frames[(frame + i) % 2];
    }

    private void OnPicked(int index)
    {
        if (leaving || services == null) return;
        switch (options[index])
        {
            case "Lanjutkan": Continue(); break;
            case "Mulai baru": NewGame(); break;
            case "Keluar": Quit(); break;
        }
    }

    private void Continue()
    {
        SaveData data = services.Save.Load();
        if (data == null)
        {
            NewGame();
            return;
        }
        leaving = true;
        SaveMapper.Apply(data, services);
        services.Save.PendingLoad = data;           // posisi dipakai World
        services.Scenes.GoTo(SceneFlow.WorldScene);
    }

    private void NewGame()
    {
        leaving = true;
        services.Party.Reset();                      // Ramuan 3, tanpa partner
        services.Flags.Load(new List<string>());     // narasi diputar
        services.Save.PendingLoad = null;
        services.Scenes.GoTo(SceneFlow.WorldScene);
    }

    private void Quit()
    {
#if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false;
#else
        Application.Quit();
#endif
    }
}
