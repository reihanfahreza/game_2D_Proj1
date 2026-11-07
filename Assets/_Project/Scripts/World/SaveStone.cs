using System.Collections;
using UnityEngine;

/// <summary>Batu Simpan (P6, kilau P7): pulihkan partner lalu simpan ke save_0.json.</summary>
public class SaveStone : MonoBehaviour, IInteractable
{
    [SerializeField] private AudioClip saveClip;

    private GameServices services;
    private Transform player;
    private PixelBurst burst;

    public void Construct(GameServices s, Transform playerTransform, PixelBurst pixelBurst)
    {
        services = s;
        player = playerTransform;
        burst = pixelBurst;
    }

    public void Interact(PlayerInteractor who)
    {
        if (services != null && !services.Dialogue.IsRunning) services.Dialogue.Run(Use());
    }

    private IEnumerator Use()
    {
        if (services.Audio != null) services.Audio.PlaySfx(saveClip, 0f);
        if (services.Party.Partner != null)
        {
            services.Party.Partner.HealFull();
            if (burst != null)
                burst.Play(player.position + Vector3.up * 0.5f, new Color32(110, 231, 242, 255), 16, 50f, 0.7f);
        }

        bool saved = services.Save.Save(SaveMapper.Capture(services, player.position));
        yield return services.Dialogue.Say("", saved
            ? "Batu Simpan bersinar. HP partner pulih dan perjalanan tersimpan."
            : "Batu Simpan bersinar. HP partner pulih, tetapi penyimpanan gagal.");
    }
}
