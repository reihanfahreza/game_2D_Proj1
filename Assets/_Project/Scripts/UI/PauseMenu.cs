using UnityEngine;

/// <summary>
/// Menu jeda Esc di Scn_Bootstrap (P6). Buka jika: scene Scn_World, tidak sibuk, tidak battle, gate tidak terkunci.
/// Buka -> Input.Lock(); tutup (Esc / Lanjut) -> Unlock(). Simpan sekarang -> SaveMapper + toast Tersimpan.
/// </summary>
public class PauseMenu : MonoBehaviour
{
    [SerializeField] private GameObject root;
    [SerializeField] private TextMenu menu;

    private static readonly string[] Options = { "Lanjut", "Simpan sekarang", "Kembali ke judul" };

    private GameServices services;

    public bool IsOpen { get; private set; }
    public TextMenu Menu => menu;

    public void Construct(GameServices s)
    {
        services = s;
        menu.Picked += OnPicked;
        menu.Cancelled += Close;
        root.SetActive(false);
    }

    void Update()
    {
        if (services != null && !IsOpen && MenuInput.Pause && CanOpen()) Open();
    }

    private bool CanOpen() =>
        services.Scenes.Current == SceneFlow.WorldScene && !services.Scenes.IsBusy && !services.Scenes.InBattle
        && !services.Input.IsLocked && !services.Dialogue.IsRunning;

    private void Open()
    {
        IsOpen = true;
        services.Input.Lock();
        root.SetActive(true);
        menu.Open(Options);
    }

    public void Close()
    {
        if (!IsOpen) return;
        IsOpen = false;
        menu.Close();
        root.SetActive(false);
        services.Input.Unlock();
    }

    private void OnPicked(int index)
    {
        switch (index)
        {
            case 0:
                Close();
                break;
            case 1:
                bool saved = services.Player != null &&
                             services.Save.Save(SaveMapper.Capture(services, services.Player.position));
                services.Toast.Show(saved ? "Tersimpan" : "Gagal menyimpan");
                Close();
                break;
            case 2:
                Close();
                services.Scenes.GoTo(SceneFlow.TitleScene);
                break;
        }
    }
}
