using UnityEngine;
using UnityEngine.UI;

/// <summary>Layar DEMO SELESAI (P7): partner akhir + level, waktu bermain, Enter / klik kembali ke judul.</summary>
public class EndInstaller : SceneInstaller
{
    [SerializeField] private Image partnerImage;
    [SerializeField] private Text partnerText;
    [SerializeField] private Text timeText;
    [SerializeField] private Button clickCatcher;
    [SerializeField] private AudioClip endMusic;

    private GameServices services;
    private MonsterInstance partner;
    private float ready;
    private bool leaving;

    public override void Install(GameServices s)
    {
        services = s;
        partner = s.Party.Partner;
        int total = Mathf.FloorToInt(s.Party.Playtime);
        partnerText.text = partner != null ? $"{partner.Data.DisplayName} · Lv {partner.Level}" : "";
        partnerImage.enabled = partner != null;
        timeText.text = $"Waktu bermain {total / 60} menit {total % 60} detik";
        clickCatcher.onClick.AddListener(Leave);
        ready = Time.unscaledTime + 0.5f;            // jeda 0,5 s
        s.Audio.PlayWorldMusic(endMusic);
    }

    void Update()
    {
        if (partner != null)                          // 2 frame, 0,35 s
            partnerImage.sprite = partner.Data.Frames[Mathf.FloorToInt(Time.unscaledTime / 0.35f) % 2];
        if (MenuInput.Confirm) Leave();
    }

    public void Leave()
    {
        if (leaving || services == null || Time.unscaledTime < ready) return;
        leaving = true;
        services.Scenes.GoTo(SceneFlow.TitleScene);
    }
}
